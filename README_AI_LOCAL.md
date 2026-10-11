# TravelAI — AI local và giao diện chat mới

Bản sửa từ TravelAI-master.zip, theo lựa chọn: máy i5-12450H/RTX 4050; giao diện xanh TravelAI, bố cục chat giống ChatGPT; AI quản lí chỉ báo cáo, gợi ý và mở trang xử lý.

## 1. Cài và chạy trên Visual Studio

1. Cài .NET 10 SDK và dùng Visual Studio hỗ trợ .NET 10; giữ SQL Server LocalDB như dự án gốc.
2. Giải nén bản ZIP, mở `TravelAI.slnx`. Nếu chép vào dự án cũ, chép đủ các tệp được liệt kê trong tài liệu mã thay đổi, gồm cả Services, Data, Program, CSS/JS, cấu hình và migrations.
3. Cài/cập nhật Ollama từ https://ollama.com/download và mở ứng dụng Ollama. Trong PowerShell chạy:

```powershell
ollama --version
ollama pull qwen3.5:4b
```

Hoặc chạy script ở thư mục dự án:

```powershell
powershell -ExecutionPolicy Bypass -File .\Scripts\Setup-LocalAI.ps1
```

4. Kiểm tra `ConnectionStrings:DefaultConnection` trong `appsettings.json`, mặc định giữ nguyên:

```text
Server=(localdb)\MSSQLLocalDB;Database=TravelAIDB;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true
```

5. Trong Package Manager Console của Visual Studio, chọn project TravelAI rồi chạy:

```powershell
Update-Database
```

Lệnh áp dụng cả migrations gốc và migration mới `AddAIConversationScope`. Không chỉ chép controller/view rồi bỏ qua bước này: database cũ chưa có cột Scope.

Nếu dùng CLI:

```powershell
dotnet tool install --global dotnet-ef --version 10.0.6
dotnet ef database update --project TravelAI.csproj
```

Nếu đã cài dotnet-ef, dùng `dotnet tool update --global dotnet-ef --version 10.0.6` thay lệnh install. `Scripts/Update-AI.sql` là lựa chọn cho database đã có đủ hai migrations gốc; không dùng thay toàn bộ migrations cho database mới.

6. Restore/Build, chạy bằng HTTPS trong Visual Studio. Đăng nhập trước khi mở `/AI`; tài khoản Admin mở `/AdminAI`.

Dự án gốc có seeder tài khoản demo khi chưa có dữ liệu: `admin@travelai.com` / `Admin@123456` và `user@travelai.com` / `123456`. Nếu dữ liệu/tài khoản cũ đã tồn tại, dùng thông tin đăng nhập hiện có. Seeder chỉ chạy sau khi database đã được tạo/cập nhật. Bản sửa AI không yêu cầu OPENAI_API_KEY.

## 2. Chức năng đã sửa

### Khách hàng

- Model mặc định `qwen3.5:4b`, chạy qua Ollama loopback local.
- Tra cứu tour thật theo tên/mã, điểm đến có/không dấu, ngân sách, số ngày, số khách và ngày khởi hành dạng dd/MM/yyyy; hỗ trợ “tháng này”, “tháng sau”, “7 ngày tới”.
- Đọc toàn bộ tour Active; chỉ tính các yêu cầu thực sự được nhập và xếp hạng theo số yêu cầu khớp giảm dần. Khi có địa điểm, chỉ trả tour liên quan đến địa điểm đó. Trả tất cả kết quả liên quan, không giới hạn 3 tour. Tour quá ngày/hết chỗ vẫn hiển thị để tham khảo kèm cảnh báo, không khẳng định có thể đặt.
- Hiểu “tổng ngân sách” hoặc “cả nhóm” khi có số khách. Ngân sách không nói rõ mặc định hiểu theo mỗi người và được ghi rõ trong kết quả.
- Hỏi nối tiếp “tour đó”, “tour thứ hai”, giá/chỗ/link giữ ngữ cảnh gần đây.
- Các link tour do server tạo từ ID thật, không phụ thuộc link do model sinh.
- Tra cứu khách sạn/phòng từ dữ liệu hiện tại, hiển thị rõ chưa xác minh tồn theo ngày nhận/trả phòng.
- Không tìm thấy nơi/tour phù hợp thì báo rõ; không tự tạo sản phẩm.

### Quản lí

- Tổng quan bao phủ Users, Destinations, Tours, TourImages, Hotels, Rooms, Bookings, HotelBookings, Payments, Combos, ComboItems, Promotions, Reviews, Favorites, AIConversations và AIChatMessages.
- Báo cáo ghi thời điểm và kỳ thống kê; hỗ trợ hôm nay/tháng này/tháng trước/năm nay/N ngày gần đây/từ dd/MM/yyyy đến dd/MM/yyyy.
- Danh mục và tồn hiện tại không bị trình bày như dữ liệu lịch sử theo kỳ.
- Doanh thu thống nhất dashboard: tổng TotalPrice của đơn không hủy và PaymentStatus=Paid; đơn Confirmed/Unpaid không được cộng vào chỉ tiêu này.
- Giá trị giao dịch Paid từ Payments là chỉ tiêu riêng theo PaymentDate; không cộng trùng vào giá trị booking. Schema gốc chưa có ngày xác nhận thực thu/hoàn tiền, báo cáo nêu rõ giới hạn này.
- Gợi ý xử lý đơn chờ, đơn xác nhận chưa trả, đánh giá thấp, dữ liệu tour cần rà soát và khuyến mãi sắp hết hạn; link mở controller quản trị hiện có.
- AI không có công cụ sửa giá/xóa dữ liệu/xác nhận thanh toán. Các thao tác quản trị thực hiện ở màn hình hiện có.
- Tên, mô tả và bình luận là dữ liệu tham khảo, không được coi là chỉ thị. Mật khẩu và hash không được đưa vào dữ liệu AI.

### Giao diện

Sidebar lịch sử; chat mới; tìm kiếm, đổi tên, xóa hội thoại; nội dung streaming; dừng trả lời; sao chép; tạo lại câu trả lời; Markdown, bảng, link nội bộ; trạng thái kết nối và thời gian xử lý. Bố cục responsive xanh TravelAI, dùng CSS/JS local, không cần CDN cho hai trang chat. Nội dung được dựng bằng DOM, không chèn HTML của model vào innerHTML.

## 3. Tốc độ và cấu hình

```json
"AI": {
  "BaseUrl": "http://localhost:11434",
  "Model": "qwen3.5:4b",
  "ResponseTimeoutSeconds": 15,
  "GenerationTimeoutSeconds": 11,
  "ContextLength": 4096,
  "MaxResponseTokens": 320,
  "HistoryMessages": 8,
  "KeepAlive": "30m",
  "WarmupOnStartup": true
}
```

- Server gửi kết quả dữ liệu trước, rồi stream phần diễn giải Ollama.
- Tối đa một lượt sinh model trong một câu hỏi. Không chạy vòng gọi tool nhiều lần.
- Nạp sẵn chạy nền, giữ model 30 phút; `think=false`, giới hạn context/lịch sử/output.
- ResponseTimeoutSeconds=15 là ngân sách xử lý mục tiêu; tối đa 2 giây dành cho lưu lịch sử. Phần diễn giải riêng tối đa 11 giây và còn bị giới hạn bởi ngân sách tổng.
- Nếu model bận, lỗi, mất kết nối hoặc quá thời gian, tour/link hoặc số liệu lấy từ database vẫn được trả, kèm thông báo rõ. Kết quả này không được nhận là câu trả lời model đã hoàn tất.
- Một GPU phục vụ một lượt sinh tại một thời điểm; yêu cầu khác vẫn nhận tra cứu dữ liệu. Một tài khoản không gửi/sửa/xóa hội thoại đồng thời khi lượt hiện tại còn chạy.
- Đây không phải cam kết mọi câu trả lời Ollama dài sẽ hoàn tất trong 10–15 giây. Database chậm, nạp model, RAM/VRAM thực tế, nhiệt độ laptop và tải đồng thời đều ảnh hưởng. Máy i5-12450H/RTX 4050 chưa được benchmark trực tiếp trong lần sửa này.

Chạy kiểm tra model trên máy bạn:

```powershell
powershell -ExecutionPolicy Bypass -File .\Scripts\Benchmark-Ollama.ps1 -Model qwen3.5:4b -Samples 5
ollama ps
```

Script phân biệt lượt đầu và lượt warm; xuất `Scripts/ollama-benchmark.csv` cùng P95. Đây là phép đo model với prompt mẫu, không phải benchmark toàn website. Trên website, thử các câu mẫu và xem thời gian hoàn tất trong form chat; phân biệt kết quả Ollama với kết quả dữ liệu khi fallback.

Nếu chưa đạt tốc độ: kiểm tra ollama ps xem model dùng GPU hay CPU, đóng ứng dụng chiếm VRAM, cắm nguồn laptop, rồi thử `ollama pull qwen3.5:2b` và đổi AI:Model. Chưa chọn 9B mặc định vì chưa biết RAM/VRAM thực tế. Các tag model và API được đối chiếu với nguồn chính thức:

- https://ollama.com/library/qwen3.5:4b
- https://ollama.com/library/qwen3.5:9b
- https://docs.ollama.com/api/chat
- https://docs.ollama.com/faq

## 4. Kiểm thử lại

```powershell
dotnet build TravelAI.csproj
dotnet test Tests/TravelAI.Tests.csproj
```

Hoặc chạy `Scripts/Check-Project.ps1`. Test project xuất hiện trong solution, dùng SQLite riêng và Ollama giả lập; không thay đổi database SQL Server của bạn. Xem `BAO_CAO_KIEM_THU.md` để phân biệt phần đã kiểm chứng và phần chưa đo thực tế.

## 5. Những điểm cần hiểu khi dùng

- Không thêm khả năng duyệt web, đọc tài liệu tải lên hay tạo file như ChatGPT; phạm vi là dữ liệu và chức năng TravelAI.
- Số liệu gợi ý kinh doanh dựa vào database. Không có log/hạ tầng trong schema để kết luận cần nâng cấp CPU/RAM hay xác định lỗi máy chủ.
- Danh sách chi tiết trong báo cáo được giới hạn (thường 3 hoặc 5 mục); các tổng số được tính trên toàn bộ tập dữ liệu tương ứng. Không tải toàn bộ database vào prompt.
- Không dùng schema mới cho AIChatMessage; Scope mới nằm trên AIConversation. Hội thoại cũ của tài khoản Admin chuyển vào Scope=Admin; hội thoại khác vào Travel. Nếu Admin từng dùng chat du lịch trước bản sửa, hội thoại cũ đó cũng được phân loại Admin vì schema cũ không ghi loại chat.
- Do chat dừng được lưu ở server sau khi kết nối ngắt, nếu bấm thao tác ngay lập tức có thể cần chờ lưu xong rồi thử lại. Form sẽ thông báo khi tài khoản còn một lượt đang chạy.

## 6. Bản vá lỗi chat HTTP 403 trên database cũ

Migration gốc `20260907093942_AddUserStatus` đặt `Status` của tài khoản đã tồn tại thành chuỗi rỗng. AccountController cho tài khoản này đăng nhập, nhưng bản AI trước yêu cầu đúng `Active`, dẫn tới thông báo “Tài khoản không có quyền truy cập” khi gửi ở cả `/AI` và `/AdminAI`.

Bản vá chấp nhận trạng thái rỗng của tài khoản cũ và `Active` sau khi bỏ khoảng trắng/so sánh không phân biệt hoa thường. `Locked` và các trạng thái chưa hoạt động vẫn bị chặn; quyền Admin được kiểm tra cả ở cookie và database. Không tự sửa Status, đổi Role hoặc mở khóa tài khoản.

Giao diện hiển thị đúng nguyên nhân từ server, giữ câu hỏi khi gửi thất bại và giữ câu trả lời cũ nếu tạo lại bị từ chối. Nút tạo lại chỉ xuất hiện khi có nội dung để tạo lại.

Nếu đã chép bản AI mới trước đó, chỉ cần thay `Controllers/AIChatControllerBase.cs` và `wwwroot/js/ai-chat.js` từ `BanVa_Chat403.zip`; file `Tests/AIIntegrationTests.cs` kèm theo để cập nhật bộ kiểm thử. Dừng ứng dụng, thay tệp, Rebuild Solution, chạy lại, đăng xuất/đăng nhập rồi Ctrl+F5 trên trang chat. Bản vá này không thêm migration hoặc đổi cấu hình model. Xem `SUA_LOI_CHAT403.md` để biết từng bước.

## 7. Có tour trên website nhưng AI không tìm thấy

Trang `/Tour` liệt kê tour `Active`. Các bản AI trước lọc đồng thời ngày khởi hành, chỗ, giá và số ngày, nên có thể cho kết quả trống dù danh mục có tour. Bản hiện tại chuyển sang xếp hạng theo số yêu cầu thực sự được nhập. Giá dùng SalePrice nếu có; số người kiểm tra AvailablePeople. Địa điểm được đối chiếu điểm đến, tỉnh/thành/vùng và tên/slug tour.

Bản hiện tại hiển thị tất cả tour liên quan, kể cả khi đã có tour khớp đủ; chia nhóm số yêu cầu khớp từ cao xuống thấp. Mỗi tour có link thật và điều kiện khớp/chưa khớp. Ngày đã qua/chưa cập nhật/hết chỗ là cảnh báo khả năng đặt riêng. Điều kiện không được hỏi không bị tính vào điểm. Không trả tour ở nơi khác khi người dùng nêu địa điểm, hoặc đưa tour Inactive vào kết quả.

Nếu đã cài bản AI và bản vá 403 trước đó, thay `Services/TravelAIService.cs` bằng tệp trong `BanVa_XepHangTour.zip`. Hai tệp Tests kèm theo để cập nhật bộ kiểm thử. `Scripts/Diagnose-TourSearch.sql` trong dự án đầy đủ là truy vấn đọc để đối chiếu dữ liệu; các cột loại theo bộ lọc cũ không phải điểm xếp hạng mới. Rebuild và hỏi lại hoặc bấm Tạo lại; câu trả lời cũ trong lịch sử không tự thay đổi. Xem `SUA_LOI_XEP_HANG_TOUR.md`.

## 8. Sửa “Chưa hoàn tất tra cứu dữ liệu” trên SQL Server

Bản tìm tour trước có lỗi ở truy vấn thống kê khi người dùng không nêu số ngày hoặc khoảng khởi hành: EF Core SQL Server sinh `COUNT(NULL)` cho điều kiện đếm luôn sai. Ngoài ra, giá trị dự phòng `decimal.MaxValue` khi không có ngân sách vượt phạm vi tham số `decimal(18,2)`. Đây là lỗi trong bản vá trước; kiểm thử SQLite không phát hiện khác biệt này.

Bản xếp hạng hiện tại đọc danh mục Active bằng truy vấn EF Core đơn giản, rồi chấm điểm/thống kê trên dữ liệu đã đọc. Không còn các phép đếm điều kiện tùy chọn trong SQL hoặc tham số giá dự phòng cực đại. Đã kiểm tra SQL sinh ra từ chính truy vấn danh mục bằng provider SQL Server và kiểm thử các tổ hợp thiếu điều kiện. Chưa thực thi trên LocalDB của máy bạn.

Tệp service trong các gói bản vá mới và ZIP dự án đầy đủ đã đồng bộ. Không cần migration mới hay cài lại Ollama cho riêng thay đổi này. Xem `SUA_LOI_SQLSERVER_AI.md` nếu cần lấy exception trong Visual Studio.

## 9. Các trường hợp nhập và thứ tự ưu tiên

| Đầu vào | Danh sách và nhóm ưu tiên |
| --- | --- |
| Hà Nội | Tất cả tour công khai liên quan Hà Nội; 1/1 yêu cầu |
| 5 triệu hoặc giá 5000000 | Tour có giá hiện tại không vượt 5 triệu/người; 1/1 |
| 2 người | Tour đủ ít nhất 2 chỗ; 1/1 |
| Hà Nội + 5 triệu | Tour liên quan Hà Nội; 2/2 trước, 1/2 sau |
| 5 triệu + 2 người | Tour khớp giá hoặc số chỗ; 2/2 trước, 1/2 sau; không trả tour khớp 0 yêu cầu |
| Hà Nội + 5 triệu + 2 người | Tour liên quan Hà Nội; 3/3 → 2/3 → 1/3 |
| Hà Nội + 5 triệu + 2 người + 3 ngày | Tour liên quan Hà Nội; 4/4 → 3/4 → 2/4 → 1/4 |

Nếu thêm ngày khởi hành, đó là một yêu cầu nữa; có thể có nhóm 5/5. Khi có địa điểm, việc liên quan địa điểm là điều kiện bắt buộc: không có tour Hà Nội thì không đưa tour ở nơi khác chỉ vì khớp giá/chỗ. Khi không có địa điểm, cần khớp ít nhất một yêu cầu đã nhập. Khi chỉ hỏi chung “Tư vấn tour”, hiển thị toàn bộ tour công khai. Chỉ báo không tìm thấy khi danh sách liên quan rỗng.

Cùng điểm khớp: ưu tiên ngày/chỗ có thể đáp ứng, rồi giá tăng dần, đánh giá giảm dần, ngày khởi hành và ID để giữ thứ tự ổn định. Khớp tất cả yêu cầu không xác nhận đặt được; cần đọc cảnh báo ngày/chỗ.

Ngân sách có đơn vị triệu/tr/m, nghìn/k/VND/đồng; số tiền đứng riêng hoặc có nhãn giá/ngân sách được hiểu là VND. Giá nhập đơn lẻ được hiểu là trần ngân sách mỗi người, không phải yêu cầu giá bằng đúng số tiền. “Tổng ngân sách/cả nhóm” được chia theo số người đã nhập. Nếu chưa có số người, chỉ đối chiếu giá một người và nói rõ chưa xác nhận tổng chi phí nhóm. Cách diễn đạt khoảng giá, số người bằng chữ hoặc ngôn ngữ khác chưa có parser đầy đủ.

Danh sách chat, sources và lịch sử chứa toàn bộ kết quả. Chỉ phần dữ liệu gửi Ollama lấy tối đa 6 tour đầu, kèm tổng số và số tour trong từng nhóm. Không giới hạn danh sách chat theo giới hạn prompt. Danh mục rất lớn vẫn cần thời gian đọc/render tương ứng; mục tiêu 10–15 giây cần đo trên máy thật.
