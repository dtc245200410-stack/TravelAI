# Có tour trên website nhưng AI không tìm thấy

Các bản AI cũ lọc đồng thời giá, số ngày, số chỗ và ngày khởi hành, nên có thể cho kết quả trống dù danh mục có tour. Bản hiện tại chuyển sang trả toàn bộ tour liên quan và xếp hạng theo yêu cầu người dùng thực sự nhập.

## Cách tìm hiện tại

- Chỉ tìm tour đang công khai (Status Active).
- Nếu có địa điểm, tên/slug tour, điểm đến và tỉnh/thành/vùng được đối chiếu để giữ các tour liên quan. Không thay bằng nơi khác chỉ vì khớp giá/chỗ.
- Nếu không có địa điểm, tour cần khớp ít nhất một điều kiện đã nhập. Hỏi chung “Tư vấn tour” thì trả toàn bộ tour công khai.
- Địa điểm, giá, số người, số ngày và ngày khởi hành là các yêu cầu riêng; không tính yêu cầu chưa nhập.
- Với 4 yêu cầu: nhóm 4/4 trước, rồi 3/4, 2/4 và 1/4. Trả cả tour khớp một phần khi đã có tour khớp đủ.
- Không còn giới hạn 3 tour. Tour đã qua ngày/chưa cập nhật ngày/hết chỗ vẫn có thể liên quan và xuất hiện cùng cảnh báo, không được khẳng định có thể đặt.

Xem các ví dụ cụ thể trong SUA_LOI_XEP_HANG_TOUR.md.

## Thay tệp

Dành cho dự án đã cài bản AI bàn giao trước đó. Trong BanVa_TimTour.zip hoặc BanVa_XepHangTour.zip, lấy Services/TravelAIService.cs ghi đè cùng đường dẫn tính từ thư mục chứa TravelAI.csproj.

1. Dừng ứng dụng bằng Shift+F5 và sao lưu service cũ.
2. Ghi đè toàn bộ Services/TravelAIService.cs.
3. Build → Rebuild Solution, nhấn F5.
4. Gửi câu hỏi mới hoặc bấm Tạo lại. Câu trả lời cũ đã lưu không tự đổi.

Tests/AIIntegrationTests.cs và Tests/TourRankingTests.cs kèm theo để cập nhật kiểm thử; không bắt buộc để chạy ứng dụng. Không có migration mới, không cần tải lại model, không tự sửa ngày/chỗ/giá.

## Đối chiếu dữ liệu

Scripts/Diagnose-TourSearch.sql trong dự án đầy đủ là truy vấn chỉ đọc. Các cột Reasons mô tả điều kiện theo bộ lọc cũ để kiểm tra dữ liệu, không phải điểm xếp hạng mới. Chọn đúng database trong ConnectionStrings:DefaultConnection trước khi đối chiếu. Với tour quá ngày, quản trị viên kiểm tra lịch bán thật tại /AdminTour trước khi cập nhật; không dời ngày hàng loạt chỉ để xuất hiện trong AI.

## Kiểm chứng

80/80 kiểm thử backend và 28/28 kiểm tra DOM đạt; build 0 lỗi/0 cảnh báo. Truy vấn danh mục được biên dịch bằng provider SQL Server; chat tích hợp dùng SQLite thật và Ollama giả lập. Chưa thực thi trên LocalDB hay đo model/GPU của máy bạn.
