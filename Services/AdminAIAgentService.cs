using System.Text.Json;
using System.Text.RegularExpressions;
using TravelAI.Models;

namespace TravelAI.Services
{
    public class AdminAIAgentService
    {
        private readonly OllamaService _ollamaService;
        private readonly AdminAITools _adminAITools;

        public AdminAIAgentService(
            OllamaService ollamaService,
            AdminAITools adminAITools)
        {
            _ollamaService = ollamaService;
            _adminAITools = adminAITools;
        }

        public async Task<string> AskAsync(
            string question,
            List<AIChatMessage>? history = null)
        {
            if (string.IsNullOrWhiteSpace(question))
            {
                return "Vui lòng nhập câu hỏi.";
            }

            var conversationHistory = "";

            if (history != null && history.Count > 0)
            {
                conversationHistory = string.Join(
                    "\n",
                    history
                        .OrderBy(x => x.CreatedAt)
                        .TakeLast(10)
                        .Select(x =>
                            $"{x.Role}: {x.Content}"
                        )
                );
            }

            if (IsSystemOverviewQuestion(question))
            {
                return await GetSystemOverviewAsync(question);
            }

            var tools = CreateTools();

            var agentQuestion =
                BuildAgentQuestion(
                    question,
                    conversationHistory
                );

            var result =
                await _ollamaService.ChatWithToolsAsync(
                    agentQuestion,
                    tools
                );

            if (result == null)
            {
                return "Agent không nhận được phản hồi từ AI.";
            }


            // =====================================================
            // XỬ LÝ NHIỀU TOOL
            // =====================================================

            if (result.ToolCalls != null &&
                result.ToolCalls.Count > 0)
            {
                var toolResults =
                    new List<string>();

                foreach (var toolCall in result.ToolCalls)
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

                    if (!IsKnownTool(
                        toolCall.ToolName))
                    {
                        continue;
                    }

                    var toolResult =
                        await ExecuteToolAsync(
                            toolCall.ToolName,
                            toolCall.Arguments
                        );

                    if (!string.IsNullOrWhiteSpace(
                        toolResult))
                    {
                        toolResults.Add(
                            toolResult
                        );
                    }
                }

                if (toolResults.Count > 0)
                {
                    var combinedResults =
                        string.Join(
                            "\n",
                            toolResults
                        );

                    return await CreateFinalAnswerAsync(
                        question,
                        conversationHistory,
                        combinedResults
                    );
                }
            }


            // =====================================================
            // FALLBACK:
            // QWEN CÓ THỂ TRẢ JSON TOOL TRONG CONTENT
            // =====================================================

            if (!string.IsNullOrWhiteSpace(
                result.Content))
            {
                var content =
                    result.Content.Trim();

                var toolNames =
                    ExtractToolNames(content);

                if (toolNames.Count > 0)
                {
                    var toolResults =
                        new List<string>();

                    foreach (var toolName in toolNames)
                    {
                        if (!IsKnownTool(toolName))
                        {
                            continue;
                        }

                        var toolResult =
                            await ExecuteToolAsync(
                                toolName,
                                null
                            );

                        if (!string.IsNullOrWhiteSpace(
                            toolResult))
                        {
                            toolResults.Add(
                                toolResult
                            );
                        }
                    }

                    if (toolResults.Count > 0)
                    {
                        var combinedResults =
                            string.Join(
                                "\n",
                                toolResults
                            );

                        return await CreateFinalAnswerAsync(
                            question,
                            conversationHistory,
                            combinedResults
                        );
                    }
                }

                return CleanAIResponse(content);
            }

            return "AI chưa tạo được câu trả lời.";
        }


        // =========================================================
        // BUILD CÂU HỎI CHO AGENT
        // =========================================================

        private string BuildAgentQuestion(
            string question,
            string conversationHistory)
        {
            if (!string.IsNullOrWhiteSpace(
                conversationHistory))
            {
                return $"""
                    Bạn là AI hỗ trợ quản trị hệ thống TravelAI.

                    Bạn đang hỗ trợ quản trị viên tra cứu
                    dữ liệu của hệ thống.

                    ========================================
                    LỊCH SỬ HỘI THOẠI
                    ========================================

                    {conversationHistory}

                    ========================================
                    CÂU HỎI HIỆN TẠI
                    ========================================

                    {question}

                    ========================================
                    QUY TẮC
                    ========================================

                    1. Hiểu câu hỏi hiện tại dựa trên
                       cả lịch sử hội thoại.

                    2. Nếu câu hỏi cần nhiều loại dữ liệu,
                       hãy gọi TẤT CẢ các tool cần thiết.

                    3. Có thể gọi nhiều tool trong cùng
                       một lần xử lý.

                    4. Không giới hạn số lượng tool.

                    5. Nếu cần 1 tool thì gọi 1 tool.

                    6. Nếu cần 5 tool thì gọi 5 tool.

                    7. Nếu cần nhiều tool hơn nữa thì
                       tiếp tục gọi tất cả tool cần thiết.

                    8. Không tự bịa số liệu.

                    9. Không nhầm:
                       - User với Booking
                       - Tour với Booking
                       - Hotel với Room
                       - Revenue với Booking.

                    10. Trả lời bằng tiếng Việt.
                    """;
            }

            return $"""
                Bạn là AI hỗ trợ quản trị hệ thống TravelAI.

                Quản trị viên hỏi:

                {question}

                ========================================
                QUY TẮC
                ========================================

                1. Nếu câu hỏi cần dữ liệu hệ thống,
                   hãy sử dụng tool phù hợp.

                2. Nếu câu hỏi cần nhiều loại dữ liệu,
                   hãy gọi TẤT CẢ các tool cần thiết.

                3. Có thể gọi nhiều tool trong cùng
                   một lần xử lý.

                4. Không giới hạn số lượng tool.

                5. Nếu cần 1 tool thì gọi 1 tool.

                6. Nếu cần nhiều tool thì gọi tất cả
                   tool cần thiết.

                7. Không tự bịa số liệu.

                8. Không nhầm:
                   - User với Booking
                   - Tour với Booking
                   - Hotel với Room
                   - Revenue với Booking.

                9. Trả lời bằng tiếng Việt.
                """;
        }


        // =========================================================
        // LẤY TÊN NHIỀU TOOL TỪ CONTENT
        // =========================================================

        private List<string> ExtractToolNames(
            string content)
        {
            var toolNames =
                new List<string>();

            if (string.IsNullOrWhiteSpace(content))
            {
                return toolNames;
            }

            var matches =
                Regex.Matches(
                    content,
                    @"""name""\s*:\s*""([^""]+)"""
                );

            foreach (Match match in matches)
            {
                if (match.Groups.Count <= 1)
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

                if (!IsKnownTool(toolName))
                {
                    continue;
                }

                if (!toolNames.Contains(
                    toolName,
                    StringComparer.OrdinalIgnoreCase))
                {
                    toolNames.Add(toolName);
                }
            }

            return toolNames;
        }


        // =========================================================
        // KIỂM TRA TOOL HỢP LỆ
        // =========================================================

        private bool IsKnownTool(
            string toolName)
        {
            return toolName switch
            {
                "GetUserCount" => true,
                "GetTourCount" => true,
                "GetHotelCount" => true,
                "GetRoomCount" => true,
                "GetBookingStatistics" => true,
                "GetRevenue" => true,

                _ => false
            };
        }


        // =========================================================
        // TẠO CÂU TRẢ LỜI CUỐI
        // =========================================================

        private async Task<string> CreateFinalAnswerAsync(
            string question,
            string conversationHistory,
            string toolResult)
        {
            var finalQuestion = $"""
                Bạn là AI hỗ trợ quản trị hệ thống TravelAI.

                ========================================
                LỊCH SỬ HỘI THOẠI
                ========================================

                {conversationHistory}

                ========================================
                CÂU HỎI HIỆN TẠI
                ========================================

                {question}

                ========================================
                DỮ LIỆU THỰC TẾ TỪ DATABASE
                ========================================

                {toolResult}

                ========================================
                QUY TẮC BẮT BUỘC
                ========================================

                1. Chỉ sử dụng số liệu có trong
                   dữ liệu database.

                2. Không được tự bịa số liệu.

                3. Không sửa số liệu.

                4. Không suy đoán số liệu.

                5. Nếu lịch sử có số liệu cũ và
                   database có số liệu mới hơn,
                   sử dụng số liệu database.

                6. Không hiển thị JSON.

                7. Không hiển thị tên tool.

                8. Không nói về Agent.

                9. Không nói về prompt.

                10. Không nói về quá trình xử lý
                    nội bộ.

                11. Không đưa ra nhận xét kinh doanh
                    nếu database không cung cấp dữ liệu.

                12. Không suy đoán.

                13. Trả lời bằng tiếng Việt.

                14. Trả lời trực tiếp câu hỏi.

                15. Trả lời ngắn gọn, dễ hiểu.

                ========================================
                PHÂN BIỆT DỮ LIỆU
                ========================================

                UserCount = số người dùng.

                TourCount = số tour.

                HotelCount = số khách sạn.

                RoomCount = số phòng.

                BookingStatistics = thống kê booking.

                Revenue = doanh thu.

                Không được gọi booking là tour.

                Ví dụ:

                "6 booking"

                Không được viết:

                "6 tour".

                ========================================
                YÊU CẦU
                ========================================

                Hãy trả lời trực tiếp câu hỏi của
                quản trị viên dựa trên dữ liệu
                thực tế ở trên.
                """;

            var answer =
                await _ollamaService.AskAsync(
                    finalQuestion
                );

            return CleanAIResponse(answer);
        }


        // =========================================================
        // LÀM SẠCH CÂU TRẢ LỜI AI
        // =========================================================

        private string CleanAIResponse(
            string response)
        {
            if (string.IsNullOrWhiteSpace(
                response))
            {
                return "AI chưa tạo được câu trả lời.";
            }

            var result =
                response.Trim();

            var jsonMatches =
                Regex.Matches(
                    result,
                    @"\{[^{}]*""name""\s*:\s*""[^""]+""[^{}]*\}"
                );

            foreach (Match match in jsonMatches)
            {
                result =
                    result.Replace(
                        match.Value,
                        ""
                    );
            }

            result =
                result.Trim();

            if (string.IsNullOrWhiteSpace(
                result))
            {
                return "AI chưa tạo được câu trả lời.";
            }

            return result;
        }


        // =========================================================
        // NHẬN DIỆN CÂU HỎI TỔNG QUAN
        // =========================================================

        private bool IsSystemOverviewQuestion(
            string question)
        {
            var text =
                question
                    .Trim()
                    .ToLowerInvariant();

            return
                text.Contains("tổng quan") ||
                text.Contains("tình hình hệ thống") ||
                text.Contains("toàn bộ hệ thống") ||
                text.Contains("tổng thể hệ thống") ||
                text.Contains("tổng hợp hệ thống");
        }


        // =========================================================
        // TỔNG QUAN HỆ THỐNG
        // =========================================================

        private async Task<string>
            GetSystemOverviewAsync(
                string question)
        {
            var userCount =
                await _adminAITools
                    .GetUserCountAsync();

            var tourCount =
                await _adminAITools
                    .GetTourCountAsync();

            var hotelCount =
                await _adminAITools
                    .GetHotelCountAsync();

            var roomCount =
                await _adminAITools
                    .GetRoomCountAsync();

            var bookingStatistics =
                await _adminAITools
                    .GetBookingStatisticsAsync();

            var revenue =
                await _adminAITools
                    .GetRevenueAsync();

            var systemData = new
            {
                Users = userCount,

                Tours = tourCount,

                Hotels = hotelCount,

                Rooms = roomCount,

                Bookings =
                    bookingStatistics,

                RevenueVND =
                    revenue.ToString(
                        "#,##0",
                        System.Globalization
                            .CultureInfo
                            .InvariantCulture)
            };

            var jsonData =
                JsonSerializer.Serialize(
                    systemData,
                    new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });

            var prompt = $"""
                Bạn là AI hỗ trợ quản trị hệ thống TravelAI.

                Quản trị viên hỏi:

                {question}

                ========================================
                DỮ LIỆU THỰC TẾ TỪ DATABASE
                ========================================

                {jsonData}

                ========================================
                QUY TẮC BẮT BUỘC
                ========================================

                1. Chỉ sử dụng đúng số liệu trong
                   database.

                2. Không tự thêm số liệu.

                3. Không sửa số liệu.

                4. Không suy đoán.

                5. Phân biệt chính xác:

                   Users = người dùng

                   Tours = tour

                   Hotels = khách sạn

                   Rooms = phòng

                   Bookings = booking

                   Revenue = doanh thu

                6. Không được gọi booking là tour.

                7. Không được nói tour đang hoạt động
                   nếu database không cung cấp trạng thái.

                8. Không đưa ra đánh giá kinh doanh.

                9. Không đưa ra khuyến nghị kinh doanh.

                10. Không hiển thị JSON.

                11. Không nói về tool.

                12. Không nói về Agent.

                13. Không nói về prompt.

                14. Không nói về quá trình xử lý
                    nội bộ.

                15. Trả lời bằng tiếng Việt.

                16. RevenueVND đã là số tiền VNĐ
                    được hệ thống định dạng.

                Không được nhân.

                Không được chia.

                Không được đổi đơn vị.

                Không được thêm số 0.

                ========================================
                CẤU TRÚC
                ========================================

                Người dùng:
                ...

                Tour:
                ...

                Khách sạn:
                ...

                Phòng:
                ...

                Booking:
                - Tổng số:
                - Chờ xác nhận:
                - Đã xác nhận:
                - Đã hủy:

                Doanh thu:
                ...

                ========================================
                YÊU CẦU
                ========================================

                Hãy trả lời trực tiếp quản trị viên.

                Chỉ sử dụng dữ liệu đã cung cấp.
                """;

            var answer =
                await _ollamaService.AskAsync(
                    prompt
                );

            return CleanAIResponse(answer);
        }


        // =========================================================
        // KHAI BÁO TOÀN BỘ TOOLS
        // =========================================================

        private object[] CreateTools()
        {
            return new object[]
            {
                new
                {
                    type = "function",

                    function = new
                    {
                        name =
                            "GetUserCount",

                        description =
                            "Lấy tổng số tài khoản người dùng trong hệ thống TravelAI.",

                        parameters = new
                        {
                            type = "object",

                            properties =
                                new { },

                            required =
                                Array.Empty<string>()
                        }
                    }
                },

                new
                {
                    type = "function",

                    function = new
                    {
                        name =
                            "GetTourCount",

                        description =
                            "Lấy tổng số tour trong hệ thống TravelAI.",

                        parameters = new
                        {
                            type = "object",

                            properties =
                                new { },

                            required =
                                Array.Empty<string>()
                        }
                    }
                },

                new
                {
                    type = "function",

                    function = new
                    {
                        name =
                            "GetHotelCount",

                        description =
                            "Lấy tổng số khách sạn trong hệ thống TravelAI.",

                        parameters = new
                        {
                            type = "object",

                            properties =
                                new { },

                            required =
                                Array.Empty<string>()
                        }
                    }
                },

                new
                {
                    type = "function",

                    function = new
                    {
                        name =
                            "GetRoomCount",

                        description =
                            "Lấy tổng số phòng khách sạn trong hệ thống TravelAI.",

                        parameters = new
                        {
                            type = "object",

                            properties =
                                new { },

                            required =
                                Array.Empty<string>()
                        }
                    }
                },

                new
                {
                    type = "function",

                    function = new
                    {
                        name =
                            "GetBookingStatistics",

                        description =
                            "Lấy thống kê booking gồm tổng số, chờ xác nhận, đã xác nhận và đã hủy.",

                        parameters = new
                        {
                            type = "object",

                            properties =
                                new { },

                            required =
                                Array.Empty<string>()
                        }
                    }
                },

                new
                {
                    type = "function",

                    function = new
                    {
                        name =
                            "GetRevenue",

                        description =
                            "Lấy tổng doanh thu từ các booking đã xác nhận.",

                        parameters = new
                        {
                            type = "object",

                            properties =
                                new { },

                            required =
                                Array.Empty<string>()
                        }
                    }
                }
            };
        }


        // =========================================================
        // THỰC THI TOOL
        // =========================================================

        private async Task<string?> ExecuteToolAsync(
            string toolName,
            JsonElement? arguments)
        {
            if (string.IsNullOrWhiteSpace(
                toolName))
            {
                return null;
            }

            toolName =
                toolName.Trim();

            switch (toolName)
            {
                case "GetUserCount":
                    {
                        var userCount =
                            await _adminAITools
                                .GetUserCountAsync();

                        return JsonSerializer.Serialize(
                            new
                            {
                                Tool =
                                    "GetUserCount",

                                UserCount =
                                    userCount
                            }
                        );
                    }


                case "GetTourCount":
                    {
                        var tourCount =
                            await _adminAITools
                                .GetTourCountAsync();

                        return JsonSerializer.Serialize(
                            new
                            {
                                Tool =
                                    "GetTourCount",

                                TourCount =
                                    tourCount
                            }
                        );
                    }


                case "GetHotelCount":
                    {
                        var hotelCount =
                            await _adminAITools
                                .GetHotelCountAsync();

                        return JsonSerializer.Serialize(
                            new
                            {
                                Tool =
                                    "GetHotelCount",

                                HotelCount =
                                    hotelCount
                            }
                        );
                    }


                case "GetRoomCount":
                    {
                        var roomCount =
                            await _adminAITools
                                .GetRoomCountAsync();

                        return JsonSerializer.Serialize(
                            new
                            {
                                Tool =
                                    "GetRoomCount",

                                RoomCount =
                                    roomCount
                            }
                        );
                    }


                case "GetBookingStatistics":
                    {
                        var bookingStatistics =
                            await _adminAITools
                                .GetBookingStatisticsAsync();

                        return JsonSerializer.Serialize(
                            new
                            {
                                Tool =
                                    "GetBookingStatistics",

                                Data =
                                    bookingStatistics
                            }
                        );
                    }


                case "GetRevenue":
                    {
                        var revenue =
                            await _adminAITools
                                .GetRevenueAsync();

                        return JsonSerializer.Serialize(
                            new
                            {
                                Tool =
                                    "GetRevenue",

                                Revenue =
                                    revenue
                            }
                        );
                    }


                default:
                    {
                        return null;
                    }
            }
        }
    }
}