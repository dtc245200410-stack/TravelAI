using OpenAI.Chat;

namespace TravelAI.Services
{
    public class OpenAIService
    {
        private readonly ChatClient _chatClient;

        public OpenAIService()
        {
            string? apiKey =
                Environment.GetEnvironmentVariable("OPENAI_API_KEY");

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException(
                    "Chưa tìm thấy OPENAI_API_KEY trong biến môi trường."
                );
            }

            _chatClient = new ChatClient(
                model: "gpt-4o-mini",
                apiKey: apiKey
            );
        }

        public async Task<string> AskAsync(string question)
        {
            if (string.IsNullOrWhiteSpace(question))
            {
                return "Vui lòng nhập câu hỏi.";
            }

            ChatCompletion completion =
                await _chatClient.CompleteChatAsync(
                    new[]
                    {
                        new UserChatMessage(question)
                    });

            if (completion.Content == null ||
                completion.Content.Count == 0)
            {
                return "Xin lỗi, AI chưa tạo được câu trả lời.";
            }

            return completion.Content[0].Text;
        }
    }
}