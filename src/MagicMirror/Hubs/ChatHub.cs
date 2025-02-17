using Microsoft.AspNetCore.SignalR;
using System;
using System.IO;
using System.Threading.Tasks;
using MagicMirror.Services;

namespace MagicMirror.Hubs;
public class ChatHub : Hub
{
    private readonly WhisperService _whisperService;
    private readonly LLMService _llmService;
    private readonly TTSService _ttsService;
    private readonly IConfiguration configuration;
    private readonly SimulationBroadcaster _simulationBroadcaster;

    public ChatHub(WhisperService whisperService, LLMService llmService, TTSService ttsService, IConfiguration configuration, SimulationBroadcaster simulationBroadcaster)
    {
        _whisperService = whisperService;
        _llmService = llmService;
        _ttsService = ttsService;
        this.configuration = configuration;
        _simulationBroadcaster = simulationBroadcaster;
    }

    public override async Task OnConnectedAsync()
    {
        Console.WriteLine("Client connected");
        await base.OnConnectedAsync();

        // Start simulation broadcast on first connection
        if (configuration["Audio:SimulationMode"] == "true")
        {
            _simulationBroadcaster.Start();
        }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        Console.WriteLine("Client disconnected");
        await base.OnDisconnectedAsync(exception);
    }

    public async Task SendMessage(string message)
    {
        Console.WriteLine($"Received message: {message}");
        await Clients.AllExcept(Context.ConnectionId).SendAsync("ReceiveMessage", message);
    }

    public async Task SendAudioData(string audioBase64)
    {
        Console.WriteLine("Received audio data from client.");
        byte[] audioBytes = Convert.FromBase64String(audioBase64);
        string path = Path.Combine(Directory.GetCurrentDirectory(), configuration["Audio:Path"] ?? "ChatAudio");
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
        string filename = $"user_{DateTime.Now:yyyyMMdd_HHmmss}.wav"; 
        string speechFilePath = Path.Combine(path, filename); 
        await File.WriteAllBytesAsync(speechFilePath, audioBytes);
        Console.WriteLine($"Audio saved to {speechFilePath}");

        await ProcessAudio(speechFilePath);
    }

    private async Task ProcessAudio(string audioFilename)
    {
        // Check for simulation mode via configuration flag
        if (configuration["Audio:SimulationMode"] == "true")
        {
            var simulatedFile = Path.Combine(Directory.GetCurrentDirectory(), configuration["Audio:SimulatedFile"] ?? "simulated_response.mp3");
            if (!File.Exists(simulatedFile))
            {
                File.Create(simulatedFile).Close();
            }
            byte[] simulatedAudio = await File.ReadAllBytesAsync(simulatedFile);
            string simulatedBase64 = Convert.ToBase64String(simulatedAudio);
            for (int i = 1; i <= 3; i++)
            {
                // Broadcast simulated audio to all clients every 10 seconds
                await Clients.All.SendAsync("ReceiveAudio", simulatedBase64);
                await Task.Delay(10000);
            }
            return;
        }
        
        await Clients.Caller.SendAsync("ReceiveMessage", "transcribing");
        Console.WriteLine("transcribing audio...");
        string text = await _whisperService.ConvertToText(audioFilename);
        Console.WriteLine($"transcription: {text}");

        await Clients.Caller.SendAsync("ReceiveMessage", "processing");
        string aiResponse;
        string ttsAudioFile;
        try
        {
            aiResponse = await _llmService.GetAIResponse(text);
            ttsAudioFile = await _ttsService.GetTextToSpeech(aiResponse);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error: {e.Message}");
            aiResponse = "The AI service is currently unavailable."; // Fallback text response
            ttsAudioFile = "chatserviceunavailable.mp3"; // Placeholder audio file if TTS fails - you'd need to include this file
            // Consider logging the error and handling TTS failure gracefully.
            if (!File.Exists(ttsAudioFile))
            {
                // Create a dummy mp3 file or use a default sound if 'chatserviceunavailable.mp3' is missing.
                // For now, let's create an empty file as a placeholder.
                File.Create(ttsAudioFile).Close();
            }
        }

        Console.WriteLine($"AI Response: {aiResponse}");

        byte[] audioData = await File.ReadAllBytesAsync(ttsAudioFile);
        string audioBase64 = Convert.ToBase64String(audioData);

        await Clients.Caller.SendAsync("ReceiveAudio", audioBase64);
    }
}