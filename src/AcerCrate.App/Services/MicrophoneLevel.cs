using System.Runtime.InteropServices;
using Windows.Media;
using Windows.Media.Audio;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Media.Render;
using WinRT;

namespace AcerCrate.App.Services;

/// <summary>Reads input samples for the level meter. Only the calculated RMS level leaves the audio frame.</summary>
internal sealed class MicrophoneLevel(AudioGraph graph, AudioDeviceInputNode input, AudioFrameOutputNode output) : IDisposable
{
    public static async Task<MicrophoneLevel?> OpenAsync()
    {
        var result = await AudioGraph.CreateAsync(new AudioGraphSettings(AudioRenderCategory.Other));
        if (result.Status != AudioGraphCreationStatus.Success) return null;

        var graph = result.Graph;
        AudioDeviceInputNode? input = null;
        AudioFrameOutputNode? output = null;
        var opened = false;
        try
        {
            var capture = await graph.CreateDeviceInputNodeAsync(MediaCategory.Other);
            if (capture.Status != AudioDeviceNodeCreationStatus.Success) return null;
            input = capture.DeviceInputNode;
            output = graph.CreateFrameOutputNode(new AudioEncodingProperties
            {
                SampleRate = graph.EncodingProperties.SampleRate,
                ChannelCount = graph.EncodingProperties.ChannelCount,
                BitsPerSample = 32,
                Subtype = MediaEncodingSubtypes.Float,
            });
            input.AddOutgoingConnection(output);
            opened = true;
            return new MicrophoneLevel(graph, input, output);
        }
        finally
        {
            if (!opened)
            {
                input?.Dispose();
                output?.Dispose();
                graph.Dispose();
            }
        }
    }

    public void Start() => graph.Start();

    public double? ReadDbfs()
    {
        using var frame = output.GetFrame();
        return ReadDbfs(frame);
    }

    internal static unsafe double? ReadDbfs(AudioFrame frame)
    {
        using var buffer = frame.LockBuffer(AudioBufferAccessMode.Read);
        if (buffer.Length < sizeof(float)) return null;
        using var reference = buffer.CreateReference();
        reference.As<IMemoryBufferByteAccess>().GetBuffer(out var data, out var capacity);
        var length = (int)(Math.Min(buffer.Length, capacity) / sizeof(float));
        if (data is null || length == 0) return null;

        var samples = new ReadOnlySpan<float>(data, length);
        double sumSquares = 0;
        foreach (var sample in samples) sumSquares += (double)sample * sample;
        return double.IsFinite(sumSquares)
            ? 10 * Math.Log10(Math.Clamp(sumSquares / length, 1e-12, 1))
            : null;
    }

    public void Dispose()
    {
        using var graphToClose = graph;
        using var outputToClose = output;
        using var inputToClose = input;
        graph.Stop();
    }

    [ComImport]
    [Guid("5B0D3235-4DBA-4D44-865E-8F1D0E4FD04D")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private unsafe interface IMemoryBufferByteAccess
    {
        void GetBuffer(out byte* buffer, out uint capacity);
    }
}
