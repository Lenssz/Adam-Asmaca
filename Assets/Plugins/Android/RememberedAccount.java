package com.adamasmaca.security;

import android.content.Context;
import android.security.keystore.KeyGenParameterSpec;
import android.security.keystore.KeyProperties;
import android.util.Base64;
import java.nio.charset.StandardCharsets;
import java.security.KeyStore;
import javax.crypto.Cipher;
import javax.crypto.KeyGenerator;
import javax.crypto.SecretKey;
import javax.crypto.spec.GCMParameterSpec;

public final class RememberedAccount {
    private static SecretKey key(String alias) throws Exception {
        KeyStore store=KeyStore.getInstance("AndroidKeyStore");store.load(null);
        if(store.containsAlias(alias))return ((KeyStore.SecretKeyEntry)store.getEntry(alias,null)).getSecretKey();
        KeyGenerator generator=KeyGenerator.getInstance(KeyProperties.KEY_ALGORITHM_AES,"AndroidKeyStore");
        generator.init(new KeyGenParameterSpec.Builder(alias,KeyProperties.PURPOSE_ENCRYPT|KeyProperties.PURPOSE_DECRYPT)
            .setBlockModes(KeyProperties.BLOCK_MODE_GCM).setEncryptionPaddings(KeyProperties.ENCRYPTION_PADDING_NONE).setRandomizedEncryptionRequired(true).build());
        return generator.generateKey();
    }
    public static boolean save(Context context,String alias,String value) {
        try {
            Cipher cipher=Cipher.getInstance("AES/GCM/NoPadding");cipher.init(Cipher.ENCRYPT_MODE,key(alias));
            byte[] encrypted=cipher.doFinal(value.getBytes(StandardCharsets.UTF_8)),iv=cipher.getIV();
            byte[] record=new byte[iv.length+encrypted.length];System.arraycopy(iv,0,record,0,iv.length);System.arraycopy(encrypted,0,record,iv.length,encrypted.length);
            return context.getSharedPreferences("paper-account",Context.MODE_PRIVATE).edit().putString(alias,Base64.encodeToString(record,Base64.NO_WRAP)).commit();
        } catch(Exception ignored) { return false; }
    }
    public static String read(Context context,String alias) {
        try {
            String saved=context.getSharedPreferences("paper-account",Context.MODE_PRIVATE).getString(alias,null);if(saved==null)return null;
            byte[] record=Base64.decode(saved,Base64.NO_WRAP);if(record.length<28)return null;
            Cipher cipher=Cipher.getInstance("AES/GCM/NoPadding");cipher.init(Cipher.DECRYPT_MODE,key(alias),new GCMParameterSpec(128,record,0,12));
            return new String(cipher.doFinal(record,12,record.length-12),StandardCharsets.UTF_8);
        } catch(Exception ignored) { return null; }
    }
    public static void clear(Context context,String alias) {
        context.getSharedPreferences("paper-account",Context.MODE_PRIVATE).edit().remove(alias).commit();
        try {KeyStore store=KeyStore.getInstance("AndroidKeyStore");store.load(null);store.deleteEntry(alias);}catch(Exception ignored) {}
    }
}
