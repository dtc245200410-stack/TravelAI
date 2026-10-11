# Tìm tất cả tour liên quan và ưu tiên theo số yêu cầu khớp

## Quy tắc đầu vào

AI nhận độc lập địa điểm, giá/ngân sách, số người, số ngày và ngày/khoảng khởi hành. Chỉ những thông tin thực sự được nhập mới được tính điểm; mỗi yêu cầu khớp được 1 điểm.

| Người dùng nhập | Kết quả |
| --- | --- |
| Hà Nội | Tất cả tour công khai liên quan Hà Nội, nhóm 1/1 |
| 5 triệu hoặc 5000000 | Tour có giá hiện tại không vượt 5 triệu/người, nhóm 1/1 |
| 2 người | Tour đủ ít nhất 2 chỗ, nhóm 1/1 |
| Hà Nội + 5 triệu | Tour liên quan Hà Nội: nhóm 2/2 trước, 1/2 sau |
| 5 triệu + 2 người | Tour khớp giá hoặc số chỗ: nhóm 2/2 trước, 1/2 sau |
| Hà Nội + 5 triệu + 2 người | Tour liên quan Hà Nội: 3/3 → 2/3 → 1/3 |
| Hà Nội + 5 triệu + 2 người + 3 ngày | Tour liên quan Hà Nội: 4/4 → 3/4 → 2/4 → 1/4 |
| Chỉ “Tư vấn tour” | Toàn bộ tour công khai, không tự thêm yêu cầu |

Nếu có thêm ngày khởi hành, tổng số yêu cầu có thể là 5. Nhóm không có tour sẽ không được hiển thị.

## Khi nào báo không tìm thấy?

- Có địa điểm: địa điểm là điều kiện bắt buộc. Nếu không có bất kỳ tour công khai liên quan Hà Nội, chat báo không tìm thấy; không thay bằng tour Đà Nẵng dù giá/chỗ phù hợp.
- Không có địa điểm: trả tất cả tour khớp ít nhất một yêu cầu đã nhập. Chỉ báo không tìm thấy khi không tour nào khớp một yêu cầu.
- Địa điểm được đối chiếu như một cụm trong tên/slug tour, tên/slug điểm đến, tỉnh/thành hoặc vùng liên kết. Không ghép các từ rời từ nhiều trường thành một địa điểm giả. Tên/slug chỉ trùng địa điểm không làm danh sách bị thu hẹp nhầm về một tour.
- Hỏi tên đầy đủ hoặc mã tour cụ thể vẫn tra cứu tour đó. Các tour Inactive không xuất hiện trong chat khách hàng.

Không còn giới hạn 3 tour. Kết quả khớp một phần được trả ngay cả khi đã có kết quả khớp đủ. Mỗi tour có link ID thật, thông tin giá/ngày/chỗ, các điều kiện khớp và chưa khớp.

## Điểm và khả năng đặt

Tour đúng địa điểm nhưng vượt giá, thiếu chỗ và sai số ngày vẫn thuộc nhóm 1/4. Tour đúng cả bốn yêu cầu nhưng đã qua ngày khởi hành vẫn thuộc nhóm 4/4, kèm cảnh báo ngày cũ. Không tự tính “ngày khởi hành từ hôm nay” thành yêu cầu nếu người dùng chưa nhập ngày.

Ngày đã qua/chưa cập nhật/hết chỗ được cảnh báo riêng. Khớp đủ yêu cầu không phải xác nhận đã đặt được. Cùng điểm, ưu tiên tour có ngày/chỗ đáp ứng, giá tăng dần, đánh giá giảm dần, ngày khởi hành rồi ID để ổn định thứ tự.

Giá dùng SalePrice nếu có, ngược lại dùng Price. Số người so sánh với AvailablePeople. Giá nhập đơn lẻ là trần ngân sách mỗi người. “Tổng ngân sách/cả nhóm” có số khách thì chia ra ngân sách mỗi người. Nếu chưa có số khách, chat ghi rõ mới đối chiếu giá một người và chưa xác nhận chi phí cả nhóm.

Hỗ trợ số tiền triệu/tr/m, nghìn/k, VND/đồng, số tiền đứng riêng hoặc có nhãn giá/ngân sách. Số người và số ngày cần nhập bằng chữ số. Parser chưa hiểu đầy đủ mọi khoảng giá hoặc mọi cách diễn đạt tiếng Việt.

## Tệp thay thế trong Visual Studio

Dành cho dự án đã cài bản AI bàn giao trước đó. Đường dẫn tính từ thư mục chứa TravelAI.csproj.

| Trong BanVa_XepHangTour.zip | Chép vào dự án | Bắt buộc? |
| --- | --- | --- |
| Services/TravelAIService.cs | Ghi đè toàn bộ Services/TravelAIService.cs | Có |
| Tests/AIIntegrationTests.cs | Ghi đè cùng đường dẫn | Nếu dùng bộ kiểm thử |
| Tests/TourRankingTests.cs | Tạo cùng đường dẫn | Nếu dùng bộ kiểm thử |
| SUA_LOI_XEP_HANG_TOUR.md | Lưu để đọc hướng dẫn | Không |

1. Dừng ứng dụng bằng Shift+F5 và sao lưu service hiện tại.
2. Ghi đè service mới. Nếu cập nhật kiểm thử, chép cả hai tệp Tests.
3. Build → Rebuild Solution, nhấn F5.
4. Hỏi lại hoặc bấm Tạo lại. Câu trả lời cũ trong lịch sử không tự thay đổi.

Chỉ service là tệp chạy thay đổi trong lần này; không cần sửa controller/model/view, đổi model hay thêm migration cho riêng bản vá. Nếu chưa có bản AI ban đầu, dùng ZIP dự án đầy đủ và làm theo README_AI_LOCAL.md.

## Tốc độ và kiểm chứng

Danh sách chat, sources và lịch sử chứa toàn bộ tour liên quan. Chỉ phần dữ liệu gửi Ollama giới hạn tối đa 6 tour đầu, kèm tổng số và số tour từng nhóm để tránh tăng prompt theo toàn bộ danh mục. Không giới hạn kết quả hiển thị theo giới hạn prompt. Danh mục rất lớn cần thời gian đọc/render tương ứng.

80/80 kiểm thử backend, 28/28 kiểm tra DOM đạt; build 0 lỗi/0 cảnh báo. Gồm đủ 15 tổ hợp bốn điều kiện, nhóm 4/3/2/1, địa điểm không có tour, giá đơn lẻ, tổng ngân sách, ngày khởi hành, danh sách đầy đủ và lịch sử. SQL danh mục được sinh bằng provider SQL Server; chat tích hợp dùng SQLite thật và Ollama giả lập. Chưa thực thi trên SQL Server LocalDB hay đo tốc độ 10–15 giây trên GPU/model của máy bạn.
