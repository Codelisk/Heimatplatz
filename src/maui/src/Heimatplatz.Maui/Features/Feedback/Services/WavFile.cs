using System.Buffers.Binary;
using System.Text;

namespace Heimatplatz.Maui.Features.Feedback.Services;

/// <summary>
/// Minimaler WAV-Container fuer die Sprachaufnahme: Shiny IAudioSource liefert
/// rohes PCM (16 kHz, 16 bit, mono) - der 44-Byte-RIFF-Header macht daraus eine
/// ueberall abspielbare .wav-Datei (App, Intern-Browser, Server).
/// </summary>
internal static class WavFile
{
    public const int SampleRate = 16000;
    public const short BitsPerSample = 16;
    public const short Channels = 1;

    /// <summary>Bytes pro Sekunde Audio (Dauer = DataLength / BytesPerSecond)</summary>
    public const int BytesPerSecond = SampleRate * Channels * BitsPerSample / 8;

    public const int HeaderLength = 44;

    /// <summary>Schreibt den Header; dataLength darf 0 sein und spaeter per Fixup korrigiert werden.</summary>
    public static void WriteHeader(Stream stream, int dataLength)
    {
        Span<byte> header = stackalloc byte[HeaderLength];

        Encoding.ASCII.GetBytes("RIFF", header[..4]);
        BinaryPrimitives.WriteInt32LittleEndian(header[4..8], 36 + dataLength);
        Encoding.ASCII.GetBytes("WAVE", header[8..12]);

        Encoding.ASCII.GetBytes("fmt ", header[12..16]);
        BinaryPrimitives.WriteInt32LittleEndian(header[16..20], 16);            // fmt-Chunk-Groesse
        BinaryPrimitives.WriteInt16LittleEndian(header[20..22], 1);             // PCM
        BinaryPrimitives.WriteInt16LittleEndian(header[22..24], Channels);
        BinaryPrimitives.WriteInt32LittleEndian(header[24..28], SampleRate);
        BinaryPrimitives.WriteInt32LittleEndian(header[28..32], BytesPerSecond);
        BinaryPrimitives.WriteInt16LittleEndian(header[32..34], (short)(Channels * BitsPerSample / 8)); // BlockAlign
        BinaryPrimitives.WriteInt16LittleEndian(header[34..36], BitsPerSample);

        Encoding.ASCII.GetBytes("data", header[36..40]);
        BinaryPrimitives.WriteInt32LittleEndian(header[40..44], dataLength);

        stream.Write(header);
    }

    /// <summary>Traegt die tatsaechliche Datenlaenge nachtraeglich in den Header ein.</summary>
    public static void FixupHeader(Stream stream, long dataLength)
    {
        Span<byte> size = stackalloc byte[4];

        stream.Seek(4, SeekOrigin.Begin);
        BinaryPrimitives.WriteInt32LittleEndian(size, (int)(36 + dataLength));
        stream.Write(size);

        stream.Seek(40, SeekOrigin.Begin);
        BinaryPrimitives.WriteInt32LittleEndian(size, (int)dataLength);
        stream.Write(size);
    }
}
