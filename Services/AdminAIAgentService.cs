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

            // =====================================================
            // CÂU HỎI TỔNG QUAN HỆ THỐNG
            // =====================================================

            if (IsSystemOverviewQuestion(question))
            {
                return await GetSystemOverviewAsync(question);
            }

            // =====================================================
            // TẠO TOOLS
            // =====================================================

            var tools = CreateTools();

            // =====================================================
            // TẠO CÂU HỎI CHO AGENT
            // =====================================================

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
            // XỬ LÝ TOOL CALLS
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
            // FALLBACK
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
            return $"""
                Bạn là AI Assistant chuyên hỗ trợ quản trị
                hệ thống TravelAI.

                ==================================================
                VAI TRÒ
                ==================================================

                Bạn hỗ trợ quản trị viên:

                - Tra cứu số liệu hệ thống.
                - Phân tích thông tin từ database.
                - Giải thích số liệu dễ hiểu.
                - Tổng hợp thông tin từ nhiều nguồn dữ liệu.
                - Đưa ra gợi ý quản trị dựa trên dữ liệu thực tế.

                Bạn không tự quyết định thay quản trị viên.

                Bạn chỉ cung cấp thông tin, phân tích và
                gợi ý có căn cứ.

                ==================================================
                NGUỒN DỮ LIỆU
                ==================================================

                Dữ liệu chính xác của hệ thống phải được lấy
                thông qua các tool được cung cấp.

                Không được tự tạo số liệu.

                Không được đoán số liệu.

                Không được sử dụng kiến thức bên ngoài để
                thay thế dữ liệu của database.

                ==================================================
                LỊCH SỬ HỘI THOẠI
                ==================================================

                {(
                    string.IsNullOrWhiteSpace(conversationHistory)
                        ? "Không có lịch sử hội thoại."
                        : conversationHistory
                )}

                ==================================================
                CÂU HỎI HIỆN TẠI
                ==================================================

                {question}

                ==================================================
                NGUYÊN TẮC CHỌN TOOL
                ==================================================

                1. Trước tiên xác định chính xác quản trị viên
                   đang muốn biết thông tin gì.

                2. Nếu câu hỏi cần dữ liệu hệ thống,
                   bắt buộc sử dụng tool phù hợp.

                3. Nếu câu hỏi cần nhiều loại dữ liệu,
                   hãy gọi tất cả các tool cần thiết.

                4. Không gọi tool không liên quan.

                5. Không bỏ qua tool nếu dữ liệu cần thiết
                   chưa có.

                6. Có thể sử dụng nhiều tool trong cùng
                   một yêu cầu.

                7. Không tự suy ra số liệu nếu chưa có
                   dữ liệu cần thiết từ database.

                ==================================================
                PHÂN BIỆT DỮ LIỆU
                ==================================================

                UserCount:
                Tổng số người dùng/tài khoản trong hệ thống.

                TourCount:
                Tổng số tour trong hệ thống.

                HotelCount:
                Tổng số khách sạn trong hệ thống.

                RoomCount:
                Tổng số phòng trong hệ thống.

                BookingStatistics:
                Thống kê booking theo dữ liệu mà tool cung cấp,
                gồm tổng số, chờ xác nhận, đã xác nhận và đã hủy.

                Revenue:
                Tổng doanh thu được tool cung cấp.

                Tuyệt đối không nhầm:

                - Người dùng với booking.
                - Tour với booking.
                - Khách sạn với phòng.
                - Doanh thu với booking.

                ==================================================
                CÁCH PHÂN TÍCH
                ==================================================

                Sau khi có dữ liệu từ tool:

                Bước 1:
                Xác định dữ liệu trả lời trực tiếp câu hỏi.

                Bước 2:
                Nếu có nhiều số liệu liên quan,
                hãy tổng hợp chúng.

                Bước 3:
                Nếu dữ liệu cho phép nhận thấy một xu hướng,
                sự chênh lệch hoặc mối quan hệ đơn giản,
                có thể mô tả trực tiếp.

                Bước 4:
                Chỉ đưa ra nhận xét có thể suy ra rõ ràng
                từ dữ liệu được cung cấp.

                Bước 5:
                Nếu dữ liệu không đủ để kết luận,
                phải nói rõ dữ liệu hiện tại chưa đủ.

                ==================================================
                CÁCH ĐƯA RA GỢI Ý
                ==================================================

                Khi phù hợp, sau phần trả lời chính có thể
                đưa ra một số gợi ý cho quản trị viên.

                Gợi ý phải:

                - Dựa trên dữ liệu thực tế đã lấy được.
                - Mang tính hỗ trợ.
                - Không quyết định thay quản trị viên.
                - Không tự tạo thêm số liệu.
                - Không khẳng định một vấn đề kinh doanh
                  nếu database không cung cấp đủ thông tin.

                Ví dụ:

                Nếu quản trị viên hỏi về booking,
                có thể gợi ý kiểm tra thêm doanh thu hoặc
                thống kê booking nếu các dữ liệu đó có thể
                lấy từ hệ thống.

                Nếu quản trị viên hỏi về số lượng tour,
                có thể gợi ý xem thêm booking hoặc doanh thu
                nếu phù hợp với câu hỏi.

                Không được biến một gợi ý thành kết luận chắc chắn.

                ==================================================
                PHONG CÁCH TRẢ LỜI
                ==================================================

                Trả lời bằng tiếng Việt.

                Phong cách:

                - Chuyên nghiệp.
                - Tự nhiên.
                - Ngắn gọn.
                - Dễ đọc.
                - Có cấu trúc.
                - Ưu tiên bullet points.

                Nếu câu hỏi đơn giản:
                trả lời trực tiếp và ngắn gọn.

                Nếu câu hỏi yêu cầu phân tích:
                sử dụng cấu trúc:

                KẾT QUẢ
                - ...

                PHÂN TÍCH
                - ...

                GỢI Ý
                - ...

                Không hiển thị:

                - JSON.
                - Tên tool.
                - Tool call.
                - Prompt.
                - Quá trình suy luận nội bộ.
                - Thông tin kỹ thuật không cần thiết
                  cho quản trị viên.

                ==================================================
                YÊU CẦU
                ==================================================

                Hãy xử lý câu hỏi của quản trị viên dựa trên
                lịch sử hội thoại và dữ liệu thực tế của hệ thống.

                Nếu cần dữ liệu, hãy sử dụng tool phù hợp.
                Không tự bịa số liệu.
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
                Bạn là AI Assistant chuyên hỗ trợ quản trị
                hệ thống TravelAI.

                ==================================================
                VAI TRÒ
                ==================================================

                Bạn giúp quản trị viên:

                - Hiểu số liệu hệ thống.
                - Phân tích dữ liệu được cung cấp.
                - Tóm tắt thông tin rõ ràng.
                - Đưa ra gợi ý phù hợp dựa trên dữ liệu.

                Bạn không được tự tạo dữ liệu.
                Bạn không được quyết định thay quản trị viên.

                ==================================================
                LỊCH SỬ HỘI THOẠI
                ==================================================

                {(
                    string.IsNullOrWhiteSpace(conversationHistory)
                        ? "Không có lịch sử hội thoại."
                        : conversationHistory
                )}

                ==================================================
                CÂU HỎI CỦA QUẢN TRỊ VIÊN
                ==================================================

                {question}

                ==================================================
                DỮ LIỆU THỰC TẾ TỪ DATABASE
                ==================================================

                {toolResult}

                ==================================================
                QUY TẮC DỮ LIỆU
                ==================================================

                1. Chỉ sử dụng dữ liệu được cung cấp
                   ở phần "DỮ LIỆU THỰC TẾ TỪ DATABASE".

                2. Không tự tạo số liệu.

                3. Không sửa số liệu.

                4. Không suy đoán dữ liệu còn thiếu.

                5. Nếu dữ liệu không đủ để trả lời,
                   phải nói rõ rằng dữ liệu hiện tại chưa đủ.

                6. Nếu lịch sử hội thoại có thông tin cũ
                   nhưng database có thông tin mới hơn,
                   ưu tiên dữ liệu database.

                ==================================================
                PHÂN BIỆT DỮ LIỆU
                ==================================================

                UserCount = số người dùng.

                TourCount = số tour.

                HotelCount = số khách sạn.

                RoomCount = số phòng.

                BookingStatistics = thống kê booking.

                Revenue = doanh thu.

                Không được nhầm:

                - Booking với Tour.
                - Hotel với Room.
                - User với Booking.
                - Revenue với Booking.

                ==================================================
                CÁCH PHÂN TÍCH
                ==================================================

                Hãy xử lý theo 3 mức:

                MỨC 1 - KẾT QUẢ
                Trả lời chính xác câu hỏi của quản trị viên.

                MỨC 2 - PHÂN TÍCH
                Nếu dữ liệu cho phép, đưa ra nhận xét đơn giản
                và có căn cứ.

                MỨC 3 - GỢI Ý
                Đưa ra một số hướng quản trị viên có thể
                kiểm tra tiếp theo.

                Gợi ý không phải là kết luận.

                Không được nói hệ thống đang "tốt",
                "xấu", "có vấn đề", "hiệu quả" hoặc
                "không hiệu quả" nếu dữ liệu hiện tại
                không đủ để chứng minh.

                Thay vào đó có thể sử dụng:

                "Quản trị viên có thể kiểm tra thêm..."

                hoặc:

                "Nếu muốn đánh giá vấn đề này,
                có thể xem thêm..."

                ==================================================
                CÁCH TRÌNH BÀY
                ==================================================

                Nếu câu hỏi đơn giản:

                **Kết quả**
                - ...

                Nếu câu hỏi cần phân tích:

                **Kết quả**
                - ...

                **Phân tích**
                - ...

                **Gợi ý**
                - ...

                Nếu có nhiều số liệu:

                **Tổng quan**
                - Người dùng: ...
                - Tour: ...
                - Khách sạn: ...
                - Phòng: ...
                - Booking: ...
                - Doanh thu: ...

                **Phân tích**
                - ...

                **Gợi ý tiếp theo**
                - ...

                ==================================================
                PHONG CÁCH
                ==================================================

                Trả lời bằng tiếng Việt.

                Phong cách:

                - Chuyên nghiệp.
                - Tự nhiên.
                - Dễ đọc.
                - Không quá dài.
                - Không lặp lại câu hỏi.

                Không hiển thị:

                - JSON.
                - Tên tool.
                - Tool call.
                - Prompt.
                - Agent.
                - Quá trình suy luận nội bộ.

                Nếu câu hỏi chỉ yêu cầu một con số,
                không cần viết phân tích dài.

                Nếu câu hỏi yêu cầu tổng quan hoặc phân tích,
                sử dụng cấu trúc:

                Kết quả → Phân tích → Gợi ý.

                ==================================================
                YÊU CẦU CUỐI
                ==================================================

                Hãy trả lời trực tiếp câu hỏi của quản trị viên
                dựa hoàn toàn trên dữ liệu thực tế được cung cấp.
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
                Bạn là AI Assistant chuyên hỗ trợ quản trị
                hệ thống TravelAI.

                ==================================================
                NHIỆM VỤ
                ==================================================

                Hãy cung cấp cho quản trị viên một bản tổng quan
                ngắn gọn, chuyên nghiệp về dữ liệu hiện tại
                của hệ thống.

                ==================================================
                CÂU HỎI
                ==================================================

                {question}

                ==================================================
                DỮ LIỆU THỰC TẾ
                ==================================================

                {jsonData}

                ==================================================
                NGUYÊN TẮC DỮ LIỆU
                ==================================================

                1. Chỉ sử dụng số liệu được cung cấp.

                2. Không tự thêm số liệu.

                3. Không sửa số liệu.

                4. Không suy đoán số liệu.

                5. Không được nhầm:

                   Users = người dùng

                   Tours = tour

                   Hotels = khách sạn

                   Rooms = phòng

                   Bookings = booking

                   Revenue = doanh thu

                6. Không được gọi booking là tour.

                7. RevenueVND đã là giá trị doanh thu
                   bằng VNĐ.

                   Không nhân.

                   Không chia.

                   Không đổi đơn vị.

                   Không thêm số 0.

                8. Không kết luận rằng hệ thống tốt hoặc xấu
                   nếu dữ liệu không đủ để chứng minh.

                9. Không đưa ra đánh giá kinh doanh chắc chắn
                   nếu dữ liệu không cung cấp đủ cơ sở.

                ==================================================
                CÁCH PHÂN TÍCH
                ==================================================

                Bước 1:
                Tóm tắt các số liệu quan trọng.

                Bước 2:
                Nêu một vài nhận xét trực tiếp có thể
                suy ra từ các số liệu.

                Bước 3:
                Đưa ra các gợi ý kiểm tra tiếp theo
                nếu phù hợp.

                Gợi ý phải mang tính hỗ trợ quản trị viên,
                không phải quyết định thay quản trị viên.

                ==================================================
                ĐỊNH DẠNG
                ==================================================

                **Tổng quan hệ thống**

                - Người dùng: ...
                - Tour: ...
                - Khách sạn: ...
                - Phòng: ...
                - Booking: ...
                - Doanh thu: ...

                **Phân tích**

                - ...
                - ...

                **Gợi ý**

                - ...
                - ...

                ==================================================
                PHONG CÁCH
                ==================================================

                - Tiếng Việt.
                - Chuyên nghiệp.
                - Ngắn gọn.
                - Dễ đọc.
                - Không hiển thị JSON.
                - Không nhắc đến tool.
                - Không nhắc đến Agent.
                - Không nhắc đến prompt.
                - Không mô tả quá trình xử lý nội bộ.

                Nếu dữ liệu không đủ để đưa ra nhận xét,
                hãy nói rõ điều đó thay vì suy đoán.

                Hãy trả lời trực tiếp quản trị viên.
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
                            "Lấy tổng số tài khoản người dùng trong hệ thống TravelAI. Sử dụng khi quản trị viên hỏi về số lượng người dùng hoặc tài khoản.",

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
                            "Lấy tổng số tour trong hệ thống TravelAI. Sử dụng khi quản trị viên hỏi về số lượng tour.",

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
                            "Lấy tổng số khách sạn trong hệ thống TravelAI. Sử dụng khi quản trị viên hỏi về số lượng khách sạn.",

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
                            "Lấy tổng số phòng khách sạn trong hệ thống TravelAI. Sử dụng khi quản trị viên hỏi về số lượng phòng.",

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
                            "Lấy thống kê booking gồm tổng số booking, số booking chờ xác nhận, đã xác nhận và đã hủy. Sử dụng khi quản trị viên hỏi về tình hình booking.",

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
                            "Lấy tổng doanh thu từ các booking đã xác nhận theo logic hiện tại của hệ thống TravelAI. Sử dụng khi quản trị viên hỏi về doanh thu.",

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