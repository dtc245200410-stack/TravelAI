using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace TravelAI.Services
{
    public class OllamaService
    {
        private readonly HttpClient _httpClient;

        public OllamaService()
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri("http://localhost:11434"),
                Timeout = TimeSpan.FromMinutes(10)
            };
        }

        // =========================================================
        // CHAT THƯỜNG
        // =========================================================

        public async Task<string> AskAsync(string question)
        {
            if (string.IsNullOrWhiteSpace(question))
            {
                return "Vui lòng nhập câu hỏi.";
            }

            var request = new
            {
                model = "qwen2.5-coder:7b",

                messages = new[]
                {
                    new
                    {
                        role = "user",
                        content = question
                    }
                },

                stream = false
            };

            try
            {
                var response =
                    await _httpClient.PostAsJsonAsync(
                        "/api/chat",
                        request
                    );

                var responseContent =
                    await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception(
                        $"Ollama API lỗi {(int)response.StatusCode}: {responseContent}"
                    );
                }

                var result =
                    JsonSerializer.Deserialize<OllamaChatResponse>(
                        responseContent,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        }
                    );

                if (result?.Message == null)
                {
                    return "Xin lỗi, AI chưa trả về phản hồi.";
                }

                if (string.IsNullOrWhiteSpace(
                    result.Message.Content))
                {
                    return "Xin lỗi, AI chưa tạo được câu trả lời.";
                }

                return result.Message.Content;
            }
            catch (HttpRequestException)
            {
                return
                    "Không thể kết nối đến Ollama. " +
                    "Hãy kiểm tra Ollama có đang chạy hay không.";
            }
            catch (TaskCanceledException)
            {
                return
                    "Ollama xử lý quá lâu. " +
                    "Vui lòng thử lại.";
            }
        }


        // =========================================================
        // CHAT VỚI TOOLS
        // =========================================================

        public async Task<ToolCallResult?> ChatWithToolsAsync(
            string question,
            object tools)
        {
            if (string.IsNullOrWhiteSpace(question))
            {
                return null;
            }

            var messages = new List<object>
            {
                new
                {
                    role = "user",
                    content = question
                }
            };

            var request = new
            {
                model = "qwen2.5-coder:7b",

                messages = messages,

                tools = tools,

                stream = false
            };

            try
            {
                var response =
                    await _httpClient.PostAsJsonAsync(
                        "/api/chat",
                        request
                    );

                var responseContent =
                    await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception(
                        $"Ollama API lỗi {(int)response.StatusCode}: {responseContent}"
                    );
                }

                OllamaChatResponse? result;

                try
                {
                    result =
                        JsonSerializer.Deserialize<OllamaChatResponse>(
                            responseContent,
                            new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            }
                        );
                }
                catch (JsonException ex)
                {
                    throw new Exception(
                        "Không thể đọc phản hồi JSON từ Ollama: "
                        + ex.Message
                    );
                }

                if (result?.Message == null)
                {
                    return null;
                }


                // =================================================
                // TRƯỜNG HỢP 1:
                // OLLAMA TRẢ NATIVE TOOL CALLS
                // =================================================

                if (result.Message.ToolCalls != null &&
                    result.Message.ToolCalls.Count > 0)
                {
                    var toolCalls =
                        new List<ToolCall>();

                    foreach (
                        var toolCall
                        in result.Message.ToolCalls)
                    {
                        if (toolCall?.Function == null)
                        {
                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(
                            toolCall.Function.Name))
                        {
                            continue;
                        }

                        toolCalls.Add(
                            new ToolCall
                            {
                                ToolName =
                                    toolCall.Function.Name,

                                Arguments =
                                    toolCall.Function.Arguments
                            }
                        );
                    }

                    // Loại bỏ tool trùng nhau
                    toolCalls =
                        RemoveDuplicateToolCalls(
                            toolCalls
                        );

                    if (toolCalls.Count > 0)
                    {
                        return new ToolCallResult
                        {
                            ToolCalls = toolCalls,

                            // Giữ ToolName để tương thích
                            // với code cũ
                            ToolName =
                                toolCalls[0].ToolName,

                            Arguments =
                                toolCalls[0].Arguments,

                            Content = null
                        };
                    }
                }


                // =================================================
                // TRƯỜNG HỢP 2:
                // QWEN TRẢ TOOL CALL TRONG CONTENT
                // =================================================

                if (!string.IsNullOrWhiteSpace(
                    result.Message.Content))
                {
                    var content =
                        result.Message.Content.Trim();

                    var parsedToolCalls =
                        TryParseMultipleToolCalls(
                            content
                        );

                    if (parsedToolCalls.Count > 0)
                    {
                        return new ToolCallResult
                        {
                            ToolCalls =
                                parsedToolCalls,

                            // Giữ ToolName để tương thích
                            // với code cũ
                            ToolName =
                                parsedToolCalls[0].ToolName,

                            Arguments =
                                parsedToolCalls[0].Arguments,

                            Content = null
                        };
                    }

                    // Không phải tool call
                    return new ToolCallResult
                    {
                        ToolCalls =
                            new List<ToolCall>(),

                        ToolName = null,

                        Arguments = null,

                        Content = content
                    };
                }


                // =================================================
                // KHÔNG CÓ TOOL VÀ KHÔNG CÓ CONTENT
                // =================================================

                return new ToolCallResult
                {
                    ToolCalls =
                        new List<ToolCall>(),

                    ToolName = null,

                    Arguments = null,

                    Content = null
                };
            }
            catch (HttpRequestException ex)
            {
                throw new Exception(
                    "Không thể kết nối đến Ollama: "
                    + ex.Message
                );
            }
            catch (TaskCanceledException)
            {
                throw new Exception(
                    "Ollama xử lý quá lâu."
                );
            }
        }


        // =========================================================
        // ĐỌC NHIỀU TOOL CALL TỪ CONTENT
        // =========================================================

        private List<ToolCall> TryParseMultipleToolCalls(
            string content)
        {
            var toolCalls =
                new List<ToolCall>();

            if (string.IsNullOrWhiteSpace(content))
            {
                return toolCalls;
            }


            // -----------------------------------------------------
            // Cách 1:
            // Tìm các JSON object dạng:
            //
            // {"name":"GetTourCount","arguments":{}}
            // -----------------------------------------------------

            var matches =
                Regex.Matches(
                    content,
                    @"\{[\s\S]*?""name""\s*:\s*""([^""]+)""[\s\S]*?\}"
                );


            foreach (Match match in matches)
            {
                if (!match.Success)
                {
                    continue;
                }

                var toolName =
                    ExtractToolName(match.Value);

                if (string.IsNullOrWhiteSpace(toolName))
                {
                    continue;
                }

                var arguments =
                    ExtractArguments(
                        match.Value
                    );

                toolCalls.Add(
                    new ToolCall
                    {
                        ToolName = toolName,

                        Arguments = arguments
                    }
                );
            }


            // -----------------------------------------------------
            // Cách 2:
            //
            // Nếu regex ở trên không bắt được,
            // tìm trực tiếp tất cả:
            //
            // "name": "TênTool"
            // -----------------------------------------------------

            if (toolCalls.Count == 0)
            {
                var nameMatches =
                    Regex.Matches(
                        content,
                        @"""name""\s*:\s*""([^""]+)"""
                    );

                foreach (
                    Match match
                    in nameMatches)
                {
                    if (!match.Success ||
                        match.Groups.Count < 2)
                    {
                        continue;
                    }

                    var toolName =
                        match.Groups[1]
                            .Value
                            .Trim();

                    if (string.IsNullOrWhiteSpace(
                        toolName))
                    {
                        continue;
                    }

                    toolCalls.Add(
                        new ToolCall
                        {
                            ToolName =
                                toolName,

                            Arguments =
                                null
                        }
                    );
                }
            }


            // -----------------------------------------------------
            // Loại bỏ tool trùng
            // -----------------------------------------------------

            return RemoveDuplicateToolCalls(
                toolCalls
            );
        }


        // =========================================================
        // LẤY TÊN TOOL
        // =========================================================

        private string? ExtractToolName(
            string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            var match =
                Regex.Match(
                    json,
                    @"""name""\s*:\s*""([^""]+)"""
                );

            if (!match.Success ||
                match.Groups.Count < 2)
            {
                return null;
            }

            return match.Groups[1]
                .Value
                .Trim();
        }


        // =========================================================
        // LẤY ARGUMENTS
        // =========================================================

        private JsonElement? ExtractArguments(
            string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            try
            {
                using var document =
                    JsonDocument.Parse(json);

                var root =
                    document.RootElement;

                if (root.TryGetProperty(
                    "arguments",
                    out JsonElement arguments))
                {
                    return arguments.Clone();
                }
            }
            catch
            {
                // Không parse được arguments
                // thì vẫn cho phép tool chạy
            }

            return null;
        }


        // =========================================================
        // LOẠI TOOL TRÙNG
        // =========================================================

        private List<ToolCall>
            RemoveDuplicateToolCalls(
                List<ToolCall> toolCalls)
        {
            var result =
                new List<ToolCall>();

            var usedNames =
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase
                );

            foreach (var toolCall in toolCalls)
            {
                if (toolCall == null)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(
                    toolCall.ToolName))
                {
                    continue;
                }

                if (usedNames.Add(
                    toolCall.ToolName))
                {
                    result.Add(toolCall);
                }
            }

            return result;
        }


        // =========================================================
        // RESPONSE CỦA OLLAMA
        // =========================================================

        private class OllamaChatResponse
        {
            public OllamaMessage? Message { get; set; }
        }


        // =========================================================
        // MESSAGE
        // =========================================================

        private class OllamaMessage
        {
            public string Role { get; set; }
                = string.Empty;

            public string Content { get; set; }
                = string.Empty;

            public List<OllamaToolCall>? ToolCalls
            {
                get;
                set;
            }
        }


        // =========================================================
        // TOOL CALL CỦA OLLAMA
        // =========================================================

        private class OllamaToolCall
        {
            public string Type { get; set; }
                = string.Empty;

            public OllamaFunctionCall? Function
            {
                get;
                set;
            }
        }


        // =========================================================
        // FUNCTION CALL
        // =========================================================

        private class OllamaFunctionCall
        {
            public string Name { get; set; }
                = string.Empty;

            public JsonElement Arguments
            {
                get;
                set;
            }
        }


        // =========================================================
        // TOOL CALL DÙNG CHO AGENT
        // =========================================================

        public class ToolCall
        {
            public string ToolName { get; set; }
                = string.Empty;

            public JsonElement? Arguments
            {
                get;
                set;
            }
        }


        // =========================================================
        // KẾT QUẢ CHAT + NHIỀU TOOL
        // =========================================================

        public class ToolCallResult
        {
            // Danh sách TOÀN BỘ tool AI yêu cầu
            public List<ToolCall> ToolCalls
            {
                get;
                set;
            } = new List<ToolCall>();


            // Giữ lại các property cũ
            // để code hiện tại không bị lỗi
            public string? ToolName
            {
                get;
                set;
            }


            public JsonElement? Arguments
            {
                get;
                set;
            }


            public string? Content
            {
                get;
                set;
            }
        }
    }
}