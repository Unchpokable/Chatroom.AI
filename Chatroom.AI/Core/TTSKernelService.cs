using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Net.Http.Json;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Net.WebSockets;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;

using Chatroom.AI.Utils;

namespace Chatroom.AI.Core;

internal sealed record TtsModelDescription
(
    [property: JsonPropertyName("model_name")]
    string ModelName,

    [property: JsonPropertyName("model_lang")]
    string ModelLang,

    [property: JsonPropertyName("model_samplerate")]
    string ModelSampleRate
);

internal sealed record TtsModelsResponse(TtsModelDescription[] Models);

internal class TtsKernelService
{
    private Process? _ttsProcess;
    private IntPtr _shutdownEvent = IntPtr.Zero;
    private ClientWebSocket? _ttsWebsocketClient;

    public bool Running => _ttsProcess is not null && !_ttsProcess.HasExited;
    public bool Connected => _ttsWebsocketClient is not null && _ttsWebsocketClient.State == WebSocketState.Open;

    public List<string> TtsModels { get; private set; } = new();

    ~TtsKernelService()
    {
        if (!_ttsProcess?.HasExited ?? false)
        {
            _ttsProcess.Kill();
            _ttsProcess.Dispose();
        }

        if (_shutdownEvent != IntPtr.Zero)
        {
            CloseHandle(_shutdownEvent);
        }
    }

    public async Task Startup()
    {
        var exePath = LocateExecutable();
        Assert.NotNullOrEmpty(exePath);

        var modelsPath = LocateTtsModels();

        var (eventHandle, eventName) = CreateTtsShutdownEvent();

        _shutdownEvent = eventHandle;

        _ttsProcess = new Process();
        _ttsProcess.StartInfo.FileName = exePath;
        _ttsProcess.StartInfo.Arguments = $"{eventName} \"{modelsPath}\"";
        _ttsProcess.StartInfo.UseShellExecute = false;
        _ttsProcess.StartInfo.CreateNoWindow = true;
        _ttsProcess.StartInfo.RedirectStandardInput = true;
        _ttsProcess.StartInfo.RedirectStandardOutput = true;

        var started = _ttsProcess.Start();

        if (started)
        {
            await Task.Delay(500); // I guess that 500 milliseconds is enough to TTSServer to startup
            _ttsWebsocketClient = new ClientWebSocket();
            // todo: add port configuration
            await _ttsWebsocketClient.ConnectAsync(new Uri("ws://127.0.0.1:45678"), CancellationToken.None);

            while (_ttsWebsocketClient.State == WebSocketState.Connecting)
            {
                await Task.Delay(10);
            }

            Assert.State(_ttsWebsocketClient.State == WebSocketState.Open);
        }
    }

    public async Task<TtsModelsResponse?> RequestModels()
    {
        Assert.NotNull(_ttsWebsocketClient);

        var request = new
        {
            type = "ask_config",
            payload = new { },
        };

        var text = JsonSerializer.Serialize(request);

        await _ttsWebsocketClient.SendAsync(Encoding.UTF8.GetBytes(text), WebSocketMessageType.Text, true, CancellationToken.None);

        var responseBuffer = new ArraySegment<byte>(new byte[2048]);
        var response = new StringBuilder();

        var result = await _ttsWebsocketClient.ReceiveAsync(responseBuffer, CancellationToken.None);
        while (!result.EndOfMessage)
        {
            result = await _ttsWebsocketClient.ReceiveAsync(responseBuffer, CancellationToken.None);
            response.Append(Encoding.UTF8.GetString(responseBuffer));
        }

        return JsonSerializer.Deserialize<TtsModelsResponse>(response.ToString());
    }

    public async Task<NamedPipeServerStream> RequestAudioStream(string content, string modelName, int sampleRate, bool shouldStream, int sentencesChunkSize = 2)
    {
        Assert.NotNull(_ttsWebsocketClient);

        var pipeName = $"TTSKernel_AudioPipe_{Random.Shared.Next()}";
        var stream = CreateTtsPipeServer(pipeName);

        Assert.State(stream.IsConnected);

        var request = JsonContent.Create(new
        {
            type = "ask_say",
            payload = new
            {
                pipe_name = $"\\\\.\\pipe\\{pipeName}",
                content,
                model_name = modelName,
                samplerate = sampleRate,
                should_stream = shouldStream,
                chunk_size = sentencesChunkSize
            }
        });

        var requestJson = JsonSerializer.Serialize(request);

        await _ttsWebsocketClient.SendAsync(Encoding.UTF8.GetBytes(requestJson), WebSocketMessageType.Text, true, CancellationToken.None);


        return stream;
    }

    public async Task ShutdownAsync(int timeout = 5000)
    {
        await Task.Run(() =>
        {
            if (_ttsProcess is null)
            {
                return;
            }

            SetEvent(_shutdownEvent);

            if (!_ttsProcess.WaitForExit(timeout))
            {
                _ttsProcess.Kill();
            }

            ResetEvent(_shutdownEvent);
            CloseHandle(_shutdownEvent);
        });
    }

    private NamedPipeServerStream CreateTtsPipeServer(string pipeName)
    {
        var stream = new NamedPipeServerStream(pipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte);
        stream.WaitForConnection();
        return stream;
    }

    private string LocateExecutable()
    {
        var selfDir = AppContext.BaseDirectory;

        var executablePlugins = Directory.GetFiles(selfDir, "TTSKernel.exe", SearchOption.TopDirectoryOnly);

        if (executablePlugins.Any())
        {
            return executablePlugins.First();
        }

        return string.Empty;
    }

    private string LocateTtsModels()
    {
        var selfDir = AppContext.BaseDirectory;
        var modelsDir = Path.Combine(selfDir, "TTSModels");
        if (Directory.Exists(modelsDir))
        {
            return modelsDir;
        }

        return string.Empty;
    }

    private (IntPtr, string) CreateTtsShutdownEvent()
    {
        var eventName = $"Local\\TTSKernelShutdownEvent_Chatroom.ai-{Random.Shared.Next()}";
        return (CreateEvent(IntPtr.Zero, true, false, eventName), eventName);
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern IntPtr CreateEvent(IntPtr lpEventAttributes, bool bManualReset, bool bInitialState, string lpName);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool SetEvent(IntPtr hEvent);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool ResetEvent(IntPtr hEvent);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool CloseHandle(IntPtr hObject);
}
