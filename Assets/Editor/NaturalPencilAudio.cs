using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class NaturalPencilAudio
{
    public const string Folder = "Assets/Audio/Pencil";
    public static AudioClip[] Build()
    {
        string sourcePath = Folder + "/pencil-write-source.ogg";
        AssetDatabase.ImportAsset(sourcePath, ImportAssetOptions.ForceSynchronousImport);
        var importer = (AudioImporter)AssetImporter.GetAtPath(sourcePath);
        var settings = importer.defaultSampleSettings;
        settings.loadType = AudioClipLoadType.DecompressOnLoad;
        settings.compressionFormat = AudioCompressionFormat.PCM;
        importer.defaultSampleSettings = settings;
        importer.SaveAndReimport();
        var source = AssetDatabase.LoadAssetAtPath<AudioClip>(sourcePath);
        source.LoadAudioData();
        float[] data = new float[source.samples * source.channels];
        if (!source.GetData(data, 0)) throw new Exception("Real pencil recording could not be decoded.");
        float[] mono = new float[source.samples];
        float previousInput = 0, highpass = 0, lowpass = 0;
        float hp = 1 / (1 + 2 * Mathf.PI * 160 / source.frequency);
        float lp = 1 - Mathf.Exp(-2 * Mathf.PI * 9000 / source.frequency);
        for (int i = 0; i < mono.Length; i++)
        {
            float input = 0;
            for (int c = 0; c < source.channels; c++) input += data[i * source.channels + c] / source.channels;
            highpass = hp * (highpass + input - previousInput); previousInput = input;
            lowpass += lp * (highpass - lowpass); mono[i] = lowpass;
        }
        // Select three separate high-energy writing windows, avoiding silence at either end.
        int count = Mathf.Min(source.frequency, mono.Length / 3);
        if (count < source.frequency / 10) throw new Exception("Writing recording is too short.");
        AudioClip[] result = new AudioClip[3];
        for (int variant = 0; variant < 3; variant++)
        {
            int first = variant * mono.Length / 3;
            int last = Mathf.Min(mono.Length - count, (variant + 1) * mono.Length / 3 - count);
            int best = first; double bestEnergy = 0;
            for (int offset = first; offset <= last; offset += Mathf.Max(1, source.frequency / 20))
            {
                double energy = 0;
                for (int i = 0; i < count; i++) energy += mono[offset + i] * mono[offset + i];
                if (energy > bestEnergy) { bestEnergy = energy; best = offset; }
            }
            float gain = Mathf.Min(12, .12f / Mathf.Max(.0001f, Mathf.Sqrt((float)(bestEnergy / count))));
            var samples = new float[count];
            int fade = Mathf.Min(count / 8, source.frequency / 50);
            for (int i = 0; i < count; i++)
            {
                float envelope = Mathf.Min(1, Mathf.Min((float)i / fade, (float)(count - 1 - i) / fade));
                samples[i] = Mathf.Clamp(mono[best + i] * gain, -.9f, .9f) * envelope;
            }
            string path = Folder + "/pencil-writing-" + (variant + 1) + ".wav";
            using (var writer = new BinaryWriter(File.Create(path)))
            {
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); writer.Write(36 + count * 2);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); writer.Write(16);
                writer.Write((short)1); writer.Write((short)1); writer.Write(source.frequency);
                writer.Write(source.frequency * 2); writer.Write((short)2); writer.Write((short)16);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("data")); writer.Write(count * 2);
                foreach (float sample in samples) writer.Write((short)(sample * 32767));
            }
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var variationImporter = (AudioImporter)AssetImporter.GetAtPath(path);
            var variationSettings = variationImporter.defaultSampleSettings;
            variationSettings.loadType = AudioClipLoadType.DecompressOnLoad;
            variationSettings.compressionFormat = AudioCompressionFormat.PCM;
            variationImporter.defaultSampleSettings = variationSettings;
            variationImporter.forceToMono = true;
            variationImporter.SaveAndReimport();
            result[variant] = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }
        File.WriteAllText(Folder + "/PROCESSING.md", "Source: Pencil Sounds, OpenGameArt, CC0. Writing only: NachtmahrTV, freesound.org/s/571800/. Eraser recording is not used.\nOriginal OGG is retained. Three independent strongest one-second writing windows, downmixed to mono, 160 Hz high pass / 9 kHz low pass, RMS matched to 0.12 with gain cap and 20 ms fades. Generated WAVs retain the real recording; no synthesized noise is used.\n");
        return result;
    }
}
