# Sửa lỗi “Chưa hoàn tất tra cứu dữ liệu”

Thông báo trong ảnh xuất hiện khi bước đọc database thất bại, trước khi có kết quả tour. Chưa có exception/log trên máy bạn để xác nhận nguyên nhân duy nhất.

## Lỗi đã sửa

Bản vá cũ sinh COUNT(NULL) trên SQL Server khi đếm một điều kiện tùy chọn luôn sai. Khi không có ngân sách, decimal.MaxValue còn được dùng làm giá dự phòng vượt phạm vi tham số decimal(18,2). Kiểm thử SQLite trước đó không phát hiện khác biệt này.

Bản hiện tại đọc danh mục tour Active bằng truy vấn EF Core đơn giản, rồi thống kê và xếp hạng bằng C#. Không còn truy vấn SQL đếm điều kiện tùy chọn hay giá dự phòng cực đại. Đồng thời đã đổi tìm tour sang tất cả kết quả liên quan, xếp theo số yêu cầu khớp; xem SUA_LOI_XEP_HANG_TOUR.md.

## Thay đúng tệp trong Visual Studio

Dành cho dự án đã cài bản AI TravelAI bàn giao trước đó. Đường dẫn tính từ thư mục chứa TravelAI.csproj.

| Tệp trong ZIP | Thay vào dự án | Yêu cầu |
| --- | --- | --- |
| Services/TravelAIService.cs | Services/TravelAIService.cs | Bắt buộc, ghi đè toàn bộ |
| Tests/AIIntegrationTests.cs | Cùng đường dẫn | Tùy chọn, cập nhật kiểm thử |
| Tests/TourRankingTests.cs | Tạo cùng đường dẫn | Tùy chọn, thêm ca xếp hạng |

1. Dừng ứng dụng bằng Shift+F5 và sao lưu service cũ.
2. Giải nén BanVa_SQLServerAI.zip hoặc BanVa_XepHangTour.zip, ghi đè service.
3. Build → Rebuild Solution, nhấn F5.
4. Gửi lại câu hỏi hoặc bấm Tạo lại. Nội dung đã lưu không tự đổi.

Không cần Update-Database hoặc tải lại Ollama cho riêng thay đổi này. Nếu chưa cài bản AI ban đầu, dùng dự án đầy đủ và làm theo README_AI_LOCAL.md.

## Kiểm chứng và lấy log nếu còn lỗi

80/80 kiểm thử backend đạt. Đã kiểm tra SQL sinh ra từ chính truy vấn danh mục bằng provider SQL Server, không có COUNT(NULL) hoặc decimal.MaxValue; 15 tổ hợp nhập thiếu thông tin và nhiều ca chat được kiểm tra riêng. Chưa thực thi trên SQL Server LocalDB hoặc đo tốc độ GPU/model trên máy bạn.

Nếu vẫn lỗi, mở View → Output trong Visual Studio, chọn Debug hoặc ASP.NET Core Web Server, gửi lại câu hỏi rồi sao chép tên exception, thông báo và vị trí lỗi sau dòng “Không hoàn tất lượt chat Travel”. Log giúp phân biệt lỗi truy vấn, kết nối database hoặc schema chưa cập nhật. Bỏ thông tin nhạy cảm nếu có trong log.
