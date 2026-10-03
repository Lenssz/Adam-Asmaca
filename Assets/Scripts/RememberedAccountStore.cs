using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

// Only a random PlayFab CustomId is persisted; never a password or session ticket.
public static class RememberedAccountStore
{
    [Serializable] public class Record { public string customId, playerId, username; }
    static string Key => "paper-remember-"+PlayFab.PlayFabSettings.staticSettings.TitleId;
    public static bool Supported {
        get {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN || (UNITY_ANDROID && !UNITY_EDITOR)
            return true;
#else
            return false;
#endif
        }
    }
    public static bool TryRead(out Record record)
    {
        record=null;
        try {
            string json=null;
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            if(!File.Exists(PathName))return false;
            json=Encoding.UTF8.GetString(Protect(File.ReadAllBytes(PathName),false));
#elif UNITY_ANDROID && !UNITY_EDITOR
            using(var bridge=new AndroidJavaClass("com.adamasmaca.security.RememberedAccount"))json=bridge.CallStatic<string>("read",Context,Key);
#endif
            if(string.IsNullOrEmpty(json))return false;
            record=JsonUtility.FromJson<Record>(json);
            if(record==null || string.IsNullOrEmpty(record.playerId) || !System.Text.RegularExpressions.Regex.IsMatch(record.customId??"", "^remember-[a-f0-9]{64}$")) { Clear();record=null;return false; }
            return true;
        } catch { Clear();record=null;return false; }
    }
    public static bool Save(Record record)
    {
        try {
            var json=JsonUtility.ToJson(record);
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            var bytes=Encoding.UTF8.GetBytes(json);
            try { Directory.CreateDirectory(Path.GetDirectoryName(PathName));File.WriteAllBytes(PathName,Protect(bytes,true)); } finally { Array.Clear(bytes,0,bytes.Length); }
            return true;
#elif UNITY_ANDROID && !UNITY_EDITOR
            using(var bridge=new AndroidJavaClass("com.adamasmaca.security.RememberedAccount"))return bridge.CallStatic<bool>("save",Context,Key,json);
#else
            return false;
#endif
        } catch { return false; }
    }
    public static void Clear()
    {
        try {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            if(File.Exists(PathName))File.Delete(PathName);
#elif UNITY_ANDROID && !UNITY_EDITOR
            using(var bridge=new AndroidJavaClass("com.adamasmaca.security.RememberedAccount"))bridge.CallStatic("clear",Context,Key);
#endif
        } catch { }
    }
#if UNITY_ANDROID && !UNITY_EDITOR
    static AndroidJavaObject Context { get { using(var player=new AndroidJavaClass("com.unity3d.player.UnityPlayer"))return player.GetStatic<AndroidJavaObject>("currentActivity"); } }
#endif
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    static string PathName => Path.Combine(Application.persistentDataPath,Key+".bin");
    [StructLayout(LayoutKind.Sequential)] struct Blob { public int size;public IntPtr data; }
    [DllImport("crypt32.dll",SetLastError=true)] static extern bool CryptProtectData(ref Blob input,string description,IntPtr entropy,IntPtr reserved,IntPtr prompt,uint flags,out Blob output);
    [DllImport("crypt32.dll",SetLastError=true)] static extern bool CryptUnprotectData(ref Blob input,IntPtr description,IntPtr entropy,IntPtr reserved,IntPtr prompt,uint flags,out Blob output);
    [DllImport("kernel32.dll")] static extern IntPtr LocalFree(IntPtr data);
    static byte[] Protect(byte[] bytes,bool encrypt)
    {
        var input=new Blob{size=bytes.Length,data=Marshal.AllocHGlobal(bytes.Length)};Blob output=default;
        try {
            Marshal.Copy(bytes,0,input.data,bytes.Length);
            bool ok=encrypt?CryptProtectData(ref input,null,IntPtr.Zero,IntPtr.Zero,IntPtr.Zero,1,out output):CryptUnprotectData(ref input,IntPtr.Zero,IntPtr.Zero,IntPtr.Zero,IntPtr.Zero,1,out output);
            if(!ok)throw new InvalidOperationException("Protected storage unavailable.");
            var result=new byte[output.size];Marshal.Copy(output.data,result,0,result.Length);return result;
        } finally {
            Marshal.Copy(new byte[input.size],0,input.data,input.size);Marshal.FreeHGlobal(input.data);
            if(output.data!=IntPtr.Zero){Marshal.Copy(new byte[output.size],0,output.data,output.size);LocalFree(output.data);}
        }
    }
#endif
}
