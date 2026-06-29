using System;

namespace MudKeyboard.Internal;

/// <summary>
/// Builds the default key-click sound the keyboards play when <c>Sound</c> is enabled, as a short
/// synthesised WAV exposed as a <c>data:</c> URI. Generating it in pure C# (a windowed sine burst)
/// keeps the feature JavaScript-free and ships no binary asset: the URI is fed straight to an
/// <c>&lt;audio&gt;</c> element rendered by Blazor. The result is cached, so the bytes are built once
/// per process. No reflection or dynamic code — AOT/trim safe.
/// </summary>
internal static class ClickSound
{
    private const int SampleRate = 44100;
    private const double FrequencyHz = 880.0; // a crisp, short "tick"
    private const int DurationMs = 35;
    private const int FadeOutMs = 18;          // taper the tail so the click does not pop
    private const double Amplitude = 0.25;     // headroom so the click is never harsh

    private static string? _dataUri;

    /// <summary>
    /// The default click as a self-contained <c>data:audio/wav;base64,…</c> URI. Built on first
    /// access and cached for the lifetime of the process.
    /// </summary>
    public static string DataUri => _dataUri ??= "data:audio/wav;base64," + Convert.ToBase64String(BuildWav());

    // 16-bit mono PCM WAV: a 44-byte canonical header followed by the samples.
    private static byte[] BuildWav()
    {
        var sampleCount = SampleRate * DurationMs / 1000;
        var fadeSamples = Math.Max(1, SampleRate * FadeOutMs / 1000);
        var dataBytes = sampleCount * 2;
        var wav = new byte[44 + dataBytes];

        WriteAscii(wav, 0, "RIFF");
        WriteInt32(wav, 4, 36 + dataBytes);   // file size minus the first 8 bytes
        WriteAscii(wav, 8, "WAVE");
        WriteAscii(wav, 12, "fmt ");
        WriteInt32(wav, 16, 16);              // PCM fmt chunk size
        WriteInt16(wav, 20, 1);               // audio format: 1 = PCM
        WriteInt16(wav, 22, 1);               // channels: mono
        WriteInt32(wav, 24, SampleRate);
        WriteInt32(wav, 28, SampleRate * 2);  // byte rate = sampleRate * channels * bytesPerSample
        WriteInt16(wav, 32, 2);               // block align = channels * bytesPerSample
        WriteInt16(wav, 34, 16);              // bits per sample
        WriteAscii(wav, 36, "data");
        WriteInt32(wav, 40, dataBytes);

        var offset = 44;
        for (var i = 0; i < sampleCount; i++)
        {
            var t = (double)i / SampleRate;
            // Linear fade over the final FadeOutMs so the burst ends smoothly.
            var fromEnd = sampleCount - i;
            var envelope = fromEnd < fadeSamples ? (double)fromEnd / fadeSamples : 1.0;
            var sample = Math.Sin(2 * Math.PI * FrequencyHz * t) * Amplitude * envelope;
            var value = (short)(sample * short.MaxValue);
            wav[offset++] = (byte)(value & 0xFF);
            wav[offset++] = (byte)((value >> 8) & 0xFF);
        }

        return wav;
    }

    private static void WriteAscii(byte[] buffer, int offset, string text)
    {
        for (var i = 0; i < text.Length; i++)
        {
            buffer[offset + i] = (byte)text[i];
        }
    }

    // Little-endian writers (WAV is little-endian) — explicit so the bytes are correct on any platform.
    private static void WriteInt32(byte[] buffer, int offset, int value)
    {
        buffer[offset] = (byte)(value & 0xFF);
        buffer[offset + 1] = (byte)((value >> 8) & 0xFF);
        buffer[offset + 2] = (byte)((value >> 16) & 0xFF);
        buffer[offset + 3] = (byte)((value >> 24) & 0xFF);
    }

    private static void WriteInt16(byte[] buffer, int offset, short value)
    {
        buffer[offset] = (byte)(value & 0xFF);
        buffer[offset + 1] = (byte)((value >> 8) & 0xFF);
    }
}
