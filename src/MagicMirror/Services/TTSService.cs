using OpenAI.Audio;

namespace MagicMirror.Services;

public class TTSService
{
    private readonly AudioClient _audioClient;
    private readonly IConfiguration configuration;

    public TTSService(IConfiguration configuration)
    {
        string? apiKey = configuration["OpenAI:ApiKey"] ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new InvalidOperationException("OpenAI API key is not configured.");
        }
        _audioClient = new AudioClient("tts-1", apiKey);
        this.configuration = configuration;
    }

    public async Task<string> GetTextToSpeech(string text)
    {
        string filename = $"response_{DateTime.Now:yyyyMMdd_HHmmss}.mp3";
        string path = Path.Combine(Directory.GetCurrentDirectory(), configuration["Audio:Path"] ?? "ChatAudio");
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        string speechFilePath = Path.Combine(path, filename); // Save to the project root for simplicity

        BinaryData speech = await _audioClient.GenerateSpeechAsync(text, GeneratedSpeechVoice.Onyx);

        using FileStream stream = File.OpenWrite(speechFilePath);
        speech.ToStream().CopyTo(stream);
        return speechFilePath;
    }
}