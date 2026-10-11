# Sửa lỗi “Tài khoản không có quyền truy cập” khi chat

Áp dụng cho bản AI TravelAI đã bàn giao, khi cả trang `/AI` và `/AdminAI` mở được nhưng gửi câu hỏi bị từ chối HTTP 403.

## Nguyên nhân được tái hiện

Migration gốc `20260907093942_AddUserStatus.cs` thêm `Users.Status` với `defaultValue: ""`. Tài khoản đã tồn tại từ trước có thể mang trạng thái rỗng. Luồng đăng nhập gốc chỉ chặn `Locked`, trong khi bản AI trước chỉ chấp nhận `Active`. Sự không thống nhất này khiến tài khoản cũ đăng nhập được nhưng gửi chat bị từ chối. Bộ kiểm thử đã tái hiện lỗi trước khi sửa, ở cả hai loại chat. Database trên máy Windows của bạn chưa được truy cập trực tiếp.

## Chép tệp nào vào đâu

Dừng ứng dụng trong Visual Studio. Giải nén `BanVa_Chat403.zip`. Từ thư mục trong ZIP, chép các tệp vào thư mục dự án có chứa `TravelAI.csproj`, ghi đè đúng tệp tương ứng:

| Tệp trong ZIP | Tệp cần ghi đè trong dự án |
| --- | --- |
| `Controllers/AIChatControllerBase.cs` | `Controllers/AIChatControllerBase.cs` |
| `wwwroot/js/ai-chat.js` | `wwwroot/js/ai-chat.js` |
| `Tests/AIIntegrationTests.cs` | `Tests/AIIntegrationTests.cs` — cập nhật bộ kiểm thử nếu dùng |

Ví dụ nếu dự án là `D:\DuAn\TravelAI-master`, tệp controller phải ở `D:\DuAn\TravelAI-master\Controllers\AIChatControllerBase.cs`, tệp JavaScript phải ở `D:\DuAn\TravelAI-master\wwwroot\js\ai-chat.js`.

## Chạy lại

1. Mở solution, chọn Build → Rebuild Solution. Nếu chạy tests, bộ kiểm thử cần .NET 10 như dự án.
2. Nhấn F5; mở ứng dụng Ollama đang dùng với `qwen3.5:4b`.
3. Đăng xuất rồi đăng nhập lại để làm mới cookie tài khoản.
4. Mở trang chat và nhấn Ctrl+F5 trong trình duyệt để tải JavaScript mới.
5. Với khách hàng: thử “Tìm tour Đà Nẵng 3 ngày dưới 5 triệu mỗi người”. Với Admin: thử “Báo cáo tổng quan hệ thống tháng này”.

Bản vá không thêm migration hoặc đổi cấu hình model, nên không cần cập nhật database riêng cho lỗi này. Nếu bạn chưa áp dụng migration `AddAIConversationScope` của bản AI trước, vẫn thực hiện `Update-Database` theo hướng dẫn cài bản AI ban đầu.

Nếu còn bị từ chối, giao diện mới sẽ hiển thị nguyên nhân cụ thể: tài khoản khóa, tài khoản chưa hoạt động, quyền quản trị đã thay đổi, hoặc phiên đăng nhập không khớp tài khoản. Với tài khoản khóa/chưa hoạt động, quản trị viên kiểm tra trạng thái tại `/AdminUser`. Chat quản lí yêu cầu Role `Admin`. Không đổi Role của tất cả tài khoản hoặc mở khóa hàng loạt.

## Phạm vi bản vá

- Chấp nhận trạng thái rỗng của tài khoản cũ và `Active` có khoảng trắng/khác hoa thường.
- Vẫn chặn `Locked`, trạng thái khác chưa hoạt động, tài khoản không tồn tại và cookie Admin đã mất quyền trong database.
- Không tự ghi lại Status hoặc Role của tài khoản.
- Hiển thị thông báo server trong form chat; giữ câu hỏi khi gửi lỗi; giữ câu trả lời cũ khi tạo lại bị từ chối.

Kiểm chứng: 35/35 kiểm thử backend và 26/26 kiểm tra DOM đạt; build 0 lỗi/0 cảnh báo. Dùng MVC thật qua TestServer, SQLite và Ollama giả lập. Chưa xác nhận trực tiếp trên Windows/SQL Server LocalDB hoặc đo thời gian suy luận GPU của bạn.
