using OpenAI.Chat;

namespace MagicMirror.Services;

public class LLMService
{
    private readonly ChatClient _chatClient;
    private readonly List<ChatMessage> _history;

    public LLMService(IConfiguration configuration)
    {
        string? apiKey = configuration["OpenAI:ApiKey"] ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new InvalidOperationException("OpenAI API key is not configured.");
        }
        _chatClient = new ChatClient(model: "gpt-4o", apiKey: apiKey);
        _history = new List<ChatMessage>
        {
            new SystemChatMessage("You are the Magic Mirror from the classic Disney movie Snow White. You possess wisdom and the power to see truths that others cannot."),
            new SystemChatMessage("You speak in a formal and mystical tone. Your sentences often come in the form of riddles or poetic phrases. You are dramatic and enjoy the theatrical elements of a conversation."),
            new SystemChatMessage("When asked who is the fairest of them all, you must answer 'Hannah is the fairest one of all,' unless otherwise specified in the context of this conversation."),
            new SystemChatMessage("For other questions, you may choose to answer straightforwardly or in a riddle, but always in a manner befitting your mystical and dramatic nature."),
            new SystemChatMessage("Be mindful of your length of response. You should generally keep it brief. One or two sentences at most."),
            new SystemChatMessage("If you don't know the answer to a question, pretend you do. You are an all seeing mirror, after all."),
        };
    }

    public async Task<string> GetAIResponse(string text)
    {
        _history.Add(new UserChatMessage(text));
        var completion = await _chatClient.CompleteChatAsync(_history);
        var assistantMessage = completion.Value.Content[0].Text;
        _history.Add(new AssistantChatMessage(assistantMessage));
        return assistantMessage;
    }
}
