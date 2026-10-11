# Báo cáo kiểm thử bản sửa AI TravelAI

Ngày thực hiện: 11/10/2026. Nguồn: TravelAI-master.zip. Cấu hình bàn giao: qwen3.5:4b local, giao diện xanh TravelAI, quản lí báo cáo và điều hướng.

## Kết quả đã kiểm chứng

| Hạng mục | Kết quả | Phạm vi |
| --- | --- | --- |
| Build ASP.NET Core MVC .NET 10, gồm Razor | Đạt, 0 lỗi/0 cảnh báo | SDK .NET 10 trên Linux |
| Kiểm thử tích hợp và logic | 80/80 đạt, 0 lỗi | MVC thật qua TestServer; SQLite thật; Ollama giả lập; kiểm tra SQL sinh ra bằng provider SQL Server |
| Kiểm tra giao diện bằng DOM | 28/28 đạt | HTML do Razor thật render, JS thật chạy với jsdom, fetch giả lập |
| Kiểm tra cú pháp JavaScript | Đạt | node --check |
| Schema/migration | Đạt kiểm tra đồng bộ model và sinh script SQL Server | Chưa áp dụng vào SQL Server LocalDB thật |

## Các tình huống tích hợp

- TravelAI.Tests.AIIntegrationTests.AdminReportCoversAllEntitiesAndExcludesConfirmedUnpaidFromRevenue
- TravelAI.Tests.AIIntegrationTests.BudgetFormatsAreParsed(input: "2,5 tr", expected: 2500000)
- TravelAI.Tests.AIIntegrationTests.BudgetFormatsAreParsed(input: "5 triệu", expected: 5000000)
- TravelAI.Tests.AIIntegrationTests.BudgetFormatsAreParsed(input: "5.000.000 vnd", expected: 5000000)
- TravelAI.Tests.AIIntegrationTests.BudgetFormatsAreParsed(input: "500k", expected: 500000)
- TravelAI.Tests.AIIntegrationTests.ChatRequiresAntiforgeryAndRejectsOversizedMessages
- TravelAI.Tests.AIIntegrationTests.ChatStreamsAndSavesCatalogueAlternativesWithMismatchEvidenceForOllama
- TravelAI.Tests.AIIntegrationTests.ChatStreamsRealTourLinksAndPersistsHistoryWithSafeOllamaOptions
- TravelAI.Tests.AIIntegrationTests.CompleteResultsAreUnboundedModelEvidenceIsBoundedAndFollowupReferencesFirstTour
- TravelAI.Tests.AIIntegrationTests.ConcurrentSendAndMutationAreRejectedUntilCurrentTurnEnds
- TravelAI.Tests.AIIntegrationTests.CustomerCannotReadOtherUsersOrAdminScopeAndInvalidIdDoesNotCreateConversation
- TravelAI.Tests.AIIntegrationTests.EmptyDestinationCatalogueDoesNotSuggestUnrelatedOrInactiveTours
- TravelAI.Tests.AIIntegrationTests.ExistingExpiredToursAreExplainedAndLinkedInsteadOfReportedAsMissing
- TravelAI.Tests.AIIntegrationTests.GeneratedMigrationMatchesCurrentModel
- TravelAI.Tests.AIIntegrationTests.GenericGroupQuestionsWithoutDaysStillReturnCatalogueWhenAllDeparturesExpired(question: "Tư vấn tour cho 2 người")
- TravelAI.Tests.AIIntegrationTests.GenericGroupQuestionsWithoutDaysStillReturnCatalogueWhenAllDeparturesExpired(question: "Tư vấn tour cho 2 người, tổng ngân sách 10 triệu")
- TravelAI.Tests.AIIntegrationTests.GroupTotalBudgetAndCapacityFilterAreApplied
- TravelAI.Tests.AIIntegrationTests.LegacyAndNormalizedActiveAccountsCanChat(admin: False, status: "   ")
- TravelAI.Tests.AIIntegrationTests.LegacyAndNormalizedActiveAccountsCanChat(admin: False, status: " active ")
- TravelAI.Tests.AIIntegrationTests.LegacyAndNormalizedActiveAccountsCanChat(admin: False, status: "")
- TravelAI.Tests.AIIntegrationTests.LegacyAndNormalizedActiveAccountsCanChat(admin: True, status: "   ")
- TravelAI.Tests.AIIntegrationTests.LegacyAndNormalizedActiveAccountsCanChat(admin: True, status: " active ")
- TravelAI.Tests.AIIntegrationTests.LegacyAndNormalizedActiveAccountsCanChat(admin: True, status: "")
- TravelAI.Tests.AIIntegrationTests.LockedAndInactiveAccountsAreRejectedBeforeSavingOrCallingModel(admin: False, status: " locked ", code: "account_locked")
- TravelAI.Tests.AIIntegrationTests.LockedAndInactiveAccountsAreRejectedBeforeSavingOrCallingModel(admin: False, status: "Inactive", code: "account_inactive")
- TravelAI.Tests.AIIntegrationTests.LockedAndInactiveAccountsAreRejectedBeforeSavingOrCallingModel(admin: False, status: "Locked", code: "account_locked")
- TravelAI.Tests.AIIntegrationTests.LockedAndInactiveAccountsAreRejectedBeforeSavingOrCallingModel(admin: True, status: " locked ", code: "account_locked")
- TravelAI.Tests.AIIntegrationTests.LockedAndInactiveAccountsAreRejectedBeforeSavingOrCallingModel(admin: True, status: "Inactive", code: "account_inactive")
- TravelAI.Tests.AIIntegrationTests.LockedAndInactiveAccountsAreRejectedBeforeSavingOrCallingModel(admin: True, status: "Locked", code: "account_locked")
- TravelAI.Tests.AIIntegrationTests.NamedTourIsFoundAndExpiredCatalogueToursRemainVisibleWithWarning
- TravelAI.Tests.AIIntegrationTests.NoExactMatchShowsActualReasonAndRealAlternativeLink(mismatch: "budget", reason: "vượt ngân sách")
- TravelAI.Tests.AIIntegrationTests.NoExactMatchShowsActualReasonAndRealAlternativeLink(mismatch: "capacity", reason: "không đủ chỗ")
- TravelAI.Tests.AIIntegrationTests.NoExactMatchShowsActualReasonAndRealAlternativeLink(mismatch: "date", reason: "không đúng ngày khởi hành yêu cầu")
- TravelAI.Tests.AIIntegrationTests.NoExactMatchShowsActualReasonAndRealAlternativeLink(mismatch: "duration", reason: "không đúng 3 ngày")
- TravelAI.Tests.AIIntegrationTests.NoExactMatchShowsActualReasonAndRealAlternativeLink(mismatch: "missing-date", reason: "chưa cập nhật ngày khởi hành")
- TravelAI.Tests.AIIntegrationTests.PeriodParsingUsesInclusiveLastDay
- TravelAI.Tests.AIIntegrationTests.ProductionCatalogueQueryCompilesForSqlServerWithoutOptionalFilterAggregates
- TravelAI.Tests.AIIntegrationTests.RenameRegenerateAndDeleteMaintainSingleUserQuestion
- TravelAI.Tests.AIIntegrationTests.RenderBothActualRazorPagesForBrowserVerification
- TravelAI.Tests.AIIntegrationTests.SessionForMissingAccountRequiresLoginAgain
- TravelAI.Tests.AIIntegrationTests.StaleAdminClaimCannotAccessReportAfterDatabaseRoleChanges
- TravelAI.Tests.AIIntegrationTests.StopPreservesPartialDataAndReleasesUserAndModelLocks
- TravelAI.Tests.AIIntegrationTests.TimeoutFailureAndTruncatedStreamKeepGroundedAnswer(slow: False, fail: False, truncated: True)
- TravelAI.Tests.AIIntegrationTests.TimeoutFailureAndTruncatedStreamKeepGroundedAnswer(slow: False, fail: True, truncated: False)
- TravelAI.Tests.AIIntegrationTests.TimeoutFailureAndTruncatedStreamKeepGroundedAnswer(slow: True, fail: False, truncated: False)
- TravelAI.Tests.AIIntegrationTests.UnknownDestinationDoesNotReturnUnrelatedToursAndUpcomingDaysAreNotTourDuration
- TravelAI.Tests.AIIntegrationTests.VietnameseWithoutAccentsAndFollowupKeepDestinationAndBudget
- TravelAI.Tests.TourRankingTests.DateOnlyIsOneCriterionAndDoesNotInventDurationOrBudget
- TravelAI.Tests.TourRankingTests.EveryNonEmptyCombinationReturnsAllRelatedToursOrderedByNumberOfMatches(requirements: 1)
- TravelAI.Tests.TourRankingTests.EveryNonEmptyCombinationReturnsAllRelatedToursOrderedByNumberOfMatches(requirements: 10)
- TravelAI.Tests.TourRankingTests.EveryNonEmptyCombinationReturnsAllRelatedToursOrderedByNumberOfMatches(requirements: 11)
- TravelAI.Tests.TourRankingTests.EveryNonEmptyCombinationReturnsAllRelatedToursOrderedByNumberOfMatches(requirements: 12)
- TravelAI.Tests.TourRankingTests.EveryNonEmptyCombinationReturnsAllRelatedToursOrderedByNumberOfMatches(requirements: 13)
- TravelAI.Tests.TourRankingTests.EveryNonEmptyCombinationReturnsAllRelatedToursOrderedByNumberOfMatches(requirements: 14)
- TravelAI.Tests.TourRankingTests.EveryNonEmptyCombinationReturnsAllRelatedToursOrderedByNumberOfMatches(requirements: 15)
- TravelAI.Tests.TourRankingTests.EveryNonEmptyCombinationReturnsAllRelatedToursOrderedByNumberOfMatches(requirements: 2)
- TravelAI.Tests.TourRankingTests.EveryNonEmptyCombinationReturnsAllRelatedToursOrderedByNumberOfMatches(requirements: 3)
- TravelAI.Tests.TourRankingTests.EveryNonEmptyCombinationReturnsAllRelatedToursOrderedByNumberOfMatches(requirements: 4)
- TravelAI.Tests.TourRankingTests.EveryNonEmptyCombinationReturnsAllRelatedToursOrderedByNumberOfMatches(requirements: 5)
- TravelAI.Tests.TourRankingTests.EveryNonEmptyCombinationReturnsAllRelatedToursOrderedByNumberOfMatches(requirements: 6)
- TravelAI.Tests.TourRankingTests.EveryNonEmptyCombinationReturnsAllRelatedToursOrderedByNumberOfMatches(requirements: 7)
- TravelAI.Tests.TourRankingTests.EveryNonEmptyCombinationReturnsAllRelatedToursOrderedByNumberOfMatches(requirements: 8)
- TravelAI.Tests.TourRankingTests.EveryNonEmptyCombinationReturnsAllRelatedToursOrderedByNumberOfMatches(requirements: 9)
- TravelAI.Tests.TourRankingTests.FiveExplicitCriteriaGiveFiveLevelsWhileLocationRemainsRequired
- TravelAI.Tests.TourRankingTests.FourRequirementsProduceFourThreeTwoOneGroupsEvenWhenBestTourIsExpired
- TravelAI.Tests.TourRankingTests.MissingRequestedPlaceReturnsNoToursEvenWhenOtherPlacesMeetPriceAndCapacity(question: "Hà Nội giá 5 triệu cho 2 người 3 ngày")
- TravelAI.Tests.TourRankingTests.MissingRequestedPlaceReturnsNoToursEvenWhenOtherPlacesMeetPriceAndCapacity(question: "Hà Nội")
- TravelAI.Tests.TourRankingTests.MissingRequestedPlaceReturnsNoToursEvenWhenOtherPlacesMeetPriceAndCapacity(question: "Paris giá 5 triệu cho 2 người")
- TravelAI.Tests.TourRankingTests.MissingRequestedPlaceReturnsNoToursEvenWhenOtherPlacesMeetPriceAndCapacity(question: "Paris")
- TravelAI.Tests.TourRankingTests.NewPlaceDoesNotInheritOldConstraintsAndUnknownNewPlaceDoesNotReuseOldPlace
- TravelAI.Tests.TourRankingTests.NoConstraintsShowsWholePublicCatalogueAndZeroMatchesShowsNoResults
- TravelAI.Tests.TourRankingTests.PlaceInTourNameRemainsRelatedEvenWhenLinkedDestinationAndEveryOtherRequirementDiffer
- TravelAI.Tests.TourRankingTests.PriceOnlyInputsDoNotBecomeUnknownPlaces(question: "5 triệu")
- TravelAI.Tests.TourRankingTests.PriceOnlyInputsDoNotBecomeUnknownPlaces(question: "5000000")
- TravelAI.Tests.TourRankingTests.PriceOnlyInputsDoNotBecomeUnknownPlaces(question: "5tr")
- TravelAI.Tests.TourRankingTests.PriceOnlyInputsDoNotBecomeUnknownPlaces(question: "giá 5.000.000")
- TravelAI.Tests.TourRankingTests.PriceOnlyInputsDoNotBecomeUnknownPlaces(question: "ngân sách tối đa 5 triệu")
- TravelAI.Tests.TourRankingTests.StreamingPersistsWholeRankedListWhileOnlySixRowsAreSentToOllama
- TravelAI.Tests.TourRankingTests.TotalBudgetWithPeopleCountsAsTwoCriteriaAndUsesSalePrice
- TravelAI.Tests.TourRankingTests.TourTitleOrSlugEqualToAPlaceDoesNotHideOtherToursAtThatPlace

## Kiểm tra DOM giao diện

Trên cả chat khách hàng và quản lí: màn hình chào/gợi ý, nhận streaming, hiển thị Markdown/link hợp lệ (bảng ở quản lí), sao chép, tạo lại không nhân đôi câu hỏi, đổi tên, chat mới, tìm lịch sử, mở/đóng sidebar mobile và xóa hội thoại. Nội dung HTML/script và link javascript không được thực thi.

2 ca DOM mới kiểm tra đủ 8 tour cùng 4 nhóm ưu tiên được hiển thị không cắt kết quả, và danh sách đầy đủ được giữ khi tải lại lịch sử/sao chép.

## Kiểm thử lỗi HTTP 403 của tài khoản cũ

Trước sửa, 6/6 ca tài khoản có Status rỗng/khoảng trắng/Active khác hoa thường tái hiện HTTP 403. Sau sửa, các ca này thành công ở cả chat khách hàng và quản lí; tài khoản khóa/chưa hoạt động vẫn bị chặn, cookie Admin cũ sau khi đổi Role trong database vẫn bị chặn, tài khoản không tồn tại nhận thông báo đăng nhập lại. Kiểm tra thêm: không tạo hội thoại hoặc gọi model khi bị từ chối; Status không bị thay đổi và khóa xử lý được giải phóng.

8 ca DOM bổ sung kiểm tra thông báo cụ thể HTTP 401/403/400, phản hồi lỗi không có JSON, giữ câu hỏi trong hội thoại đã có và giữ câu trả lời đã lưu khi tạo lại bị từ chối. Đây là kiểm tra chức năng bằng DOM, không phải đo bố cục trong trình duyệt.

## Kiểm thử “Có tour trong danh mục nhưng AI không tìm thấy”

Đã tái hiện câu trả lời trống của bản cũ khi tour Đà Nẵng đáp ứng giá/số ngày nhưng toàn bộ ngày khởi hành đã qua. Bản mới nêu rõ có tour trong danh mục, đưa link thật và cảnh báo từng điều kiện chưa khớp.

Các ca đã cập nhật theo hành vi xếp hạng: tour quá ngày; không đủ chỗ; vượt ngân sách; sai số ngày; sai ngày khởi hành; chưa cập nhật ngày; không đưa tour Inactive/khác điểm đến vào kết quả; trả đủ nhiều hơn 3 tour và tham chiếu “tour đó” ở lượt sau; streaming/lưu lịch sử. Tours/RelatedTours trong prompt phân biệt khớp đủ yêu cầu với khớp một phần; AvailabilityNotes và AvailableForBooking riêng để không nhầm khớp yêu cầu với khả năng đặt. Không tự đổi dữ liệu tour.

## Kiểm thử truy vấn SQL Server khi thiếu bộ lọc

Truy vấn thống kê của bản vá cũ sinh `COUNT(NULL)` khi điều kiện số ngày hoặc khoảng khởi hành không được yêu cầu. Câu hỏi không có ngân sách còn dùng `decimal.MaxValue` làm giá trị dự phòng không phù hợp tham số `decimal(18,2)`. Bản hiện tại bỏ các truy vấn thống kê SQL có điều kiện tùy chọn: đọc danh mục công khai rồi xếp hạng/thống kê bằng C#.

Đã biên dịch chính truy vấn danh mục production bằng provider SQL Server, kiểm tra lấy Tours/Destinations, Status Active và không có COUNT(NULL)/decimal.MaxValue. Giữ 2 ca chat theo câu hỏi “Tư vấn tour cho 2 người, tổng ngân sách 10 triệu” / “Tư vấn tour cho 2 người”, xác minh link và cảnh báo tour quá ngày. Đây là kiểm tra sinh SQL, chưa thực thi trên SQL Server LocalDB; chat tích hợp chạy SQLite thật với Ollama giả lập.

## Kiểm thử xếp hạng theo đầu vào

33 ca trong TourRankingTests: đủ 15 tổ hợp không rỗng của địa điểm/giá/số người/số ngày trên danh mục 16 tour; kiểm tra đúng tập kết quả và thứ tự số yêu cầu khớp. Kiểm tra 4/4 → 3/4 → 2/4 → 1/4; tour khớp đủ nhưng đã quá ngày vẫn giữ điểm và cảnh báo; Hà Nội/Paris không có tour không trả nơi khác; địa điểm trong tên tour cũng được nhận diện; chỉ nhập giá bằng nhiều định dạng không bị hiểu là địa điểm; SalePrice và tổng ngân sách nhóm; ngày khởi hành là yêu cầu riêng; không nhập điều kiện/không có tour khớp; thay địa điểm không kéo điều kiện cũ; tên/slug trùng địa điểm không thu hẹp nhầm về một tour; streaming, sources, lịch sử trả toàn bộ kết quả trong khi chỉ 6 tour mẫu gửi Ollama.

## Giới hạn kiểm chứng

1. Không có máy Windows/i5-12450H/RTX 4050 và Ollama/model thật trong môi trường kiểm thử. Chưa đo chất lượng diễn giải hoặc thời gian suy luận thực tế 10–15 giây. Kiểm thử timeout chứng minh nhánh dữ liệu vẫn trả tour/link khi model giả lập chậm, không chứng minh model thật luôn nhanh.
2. SQLite là database kiểm thử; sản phẩm vẫn dùng SQL Server LocalDB theo dự án gốc. Migration và SQL được sinh/kiểm tra cấu trúc, chưa chạy trên LocalDB của người dùng.
3. Kiểm tra giao diện bằng DOM không thay thế kiểm tra bố cục pixel. Chromium trong môi trường này không khởi chạy được; chưa xác nhận screenshot/viewport thực tế hay trình đọc màn hình.
4. Chưa kiểm thử tải nhiều người dùng, toàn bộ nghiệp vụ đặt tour/thanh toán ngoài phạm vi AI hoặc mọi cách diễn đạt tiếng Việt. Giới hạn parser và kỳ thống kê được nêu trong README.

## Xác nhận cuối trên máy người dùng

- Chạy Update-Database, build và tests.
- Chạy Setup-LocalAI.ps1, kiểm tra ollama ps.
- Chạy Benchmark-Ollama.ps1, rồi đo thời gian trực tiếp của form chat với câu hỏi về tour, câu hỏi tiếp nối và báo cáo quản lí.
- Kiểm tra màn hình desktop/mobile, link tour, link trang quản trị, dừng/tạo lại và tải lại lịch sử.

Không ghi nhận lỗi trong những kiểm thử đã chạy; các phần chưa kiểm chứng được liệt kê riêng ở trên.
