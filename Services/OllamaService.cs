using System.Net.Http.Json;

namespace TravelAI.Services
{
    public class OllamaService
    {
        private readonly HttpClient _httpClient;

        public OllamaService()
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri("http://localhost:11434")
            };
        }

        public async Task<string> AskAsync(string question)
        {
            if (string.IsNullOrWhiteSpace(question))
            {
                return "Vui lòng nhập câu hỏi.";
            }

            var request = new
            {
                model = "qwen2.5-coder:7b",
                prompt = question,
                stream = false
            };

            var response = await _httpClient.PostAsJsonAsync(
                "/api/generate",
                request
            );

            response.EnsureSuccessStatusCode();

            var result =
                await response.Content.ReadFromJsonAsync<OllamaResponse>();

            if (result == null || string.IsNullOrWhiteSpace(result.Response))
            {
                return "Xin lỗi, AI chưa tạo được câu trả lời.";
            }

            return result.Response;
        }

        private class OllamaResponse
        {
            public string Response { get; set; } = string.Empty;
        }
    }
}