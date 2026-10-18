using Game.Shared;
using Microsoft.Xna.Framework.Audio;

namespace Game.MonoGameClient;

// DesktopGL's SoundEffect backend ignores the constructor's loop points in
// MonoGame 3.8.4.1. Queue PCM instead: play the intro once, then the authored loop.
internal sealed class LoopingMusic : IDisposable
{
    const int FramesPerBuffer = 16384;
    readonly byte[] samples = [];
    readonly int bytesPerFrame;
    readonly DynamicSoundEffectInstance? instance;
    int cursor;
    public bool Loaded => instance is not null;
    public bool Playing => instance?.State == SoundState.Playing;
    public int QueuedBuffers => instance?.PendingBufferCount ?? 0;
    public string Status { get; }
    public float Volume
    {
        get => instance?.Volume ?? 0;
        set { if (instance is not null) instance.Volume = Math.Clamp(value, 0, 1); }
    }

    public LoopingMusic(string audioRoot, float volume)
    {
        try
        {
            using var input = new BinaryReader(File.OpenRead(Path.Combine(audioRoot, MusicTrack.RelativePath)));
            string FourCc() => new(input.ReadChars(4));
            if (FourCc() != "RIFF") throw new InvalidDataException("Music is not a RIFF WAV.");
            input.ReadUInt32();
            if (FourCc() != "WAVE") throw new InvalidDataException("Music is not a WAV.");
            ushort format = 0, channels = 0, bits = 0;
            int sampleRate = 0;
            while (input.BaseStream.Position + 8 <= input.BaseStream.Length)
            {
                string chunk = FourCc();
                uint length = input.ReadUInt32();
                long next = checked(input.BaseStream.Position + length + (length & 1));
                if (next > input.BaseStream.Length) throw new InvalidDataException("Music WAV has a truncated chunk.");
                if (chunk == "fmt " && length >= 16)
                {
                    format = input.ReadUInt16(); channels = input.ReadUInt16(); sampleRate = input.ReadInt32();
                    input.ReadUInt32(); input.ReadUInt16(); bits = input.ReadUInt16();
                }
                else if (chunk == "data") samples = input.ReadBytes(checked((int)length));
                input.BaseStream.Position = next;
            }
            if (format != 1 || channels is not (1 or 2) || bits != 16 || sampleRate != MusicTrack.SampleRate)
                throw new InvalidDataException("Music requires 44.1 kHz 16-bit PCM mono/stereo WAV.");
            bytesPerFrame = channels * sizeof(short);
            if (samples.Length / bytesPerFrame <= MusicTrack.LoopEnd)
                throw new InvalidDataException("Music does not contain the authored loop.");
            instance = new DynamicSoundEffectInstance(sampleRate, (AudioChannels)channels) { Volume = volume };
            instance.BufferNeeded += Refill;
            Refill(this, EventArgs.Empty);
            instance.Play();
            Status = "Music playing: Echoes of Valhalla";
            Console.WriteLine($"AUDIO PLAYING MonoGame: PCM={sampleRate}Hz/{channels}ch/16bit loop={MusicTrack.LoopStart}+{MusicTrack.LoopLength}; queued={QueuedBuffers}");
        }
        catch (Exception exception)
        {
            if (instance is not null) { instance.BufferNeeded -= Refill; instance.Dispose(); instance = null; }
            Status = "Music unavailable: " + exception.Message;
            Console.Error.WriteLine(Status);
        }
    }

    // Frame units, including the last frame before jumping exactly to LoopStart.
    internal static (int Frames, int Next) SegmentAt(int position)
    {
        int frames = Math.Min(FramesPerBuffer, MusicTrack.LoopEnd + 1 - position);
        return (frames, position + frames > MusicTrack.LoopEnd ? MusicTrack.LoopStart : position + frames);
    }
    void Refill(object? sender, EventArgs args)
    {
        while (instance is not null && instance.PendingBufferCount < 3)
        {
            var segment = SegmentAt(cursor);
            instance.SubmitBuffer(samples, cursor * bytesPerFrame, segment.Frames * bytesPerFrame);
            cursor = segment.Next;
        }
    }
    public void Dispose()
    {
        if (instance is null) return;
        instance.BufferNeeded -= Refill;
        instance.Stop();
        instance.Dispose();
    }
}
