using OpenAI.Audio;

namespace MagicMirror.Services;
public class WhisperService
{
    private readonly AudioClient _audioClient;

    public WhisperService(IConfiguration configuration)
    {
        string? apiKey = configuration["OpenAI:ApiKey"] ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new InvalidOperationException("OpenAI API key is not configured.");
        }
        _audioClient = new AudioClient("whisper-1", apiKey);
    }

    public async Task<string> ConvertToText(string audioFilePath)
    {
        var transcription = await _audioClient.TranscribeAudioAsync(audioFilePath);
        return transcription.Value.Text;
    }
}