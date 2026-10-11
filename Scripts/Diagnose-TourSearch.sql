-- Read-only diagnostic for: tour Da Nang, 3 days, up to 5 million VND/person.
-- Run against the database in appsettings.json DefaultConnection.
DECLARE @Destination nvarchar(200) = N'Đà Nẵng';
DECLARE @Days int = 3;
DECLARE @Budget decimal(18, 2) = 5000000;
DECLARE @People int = 1;
DECLARE @Today date = CONVERT(date, SYSDATETIME());

SELECT t.Id, t.Name, d.Name AS Destination, t.Status, t.Duration,
       t.Price, t.SalePrice, COALESCE(t.SalePrice, t.Price) AS EffectivePrice,
       t.DepartureDate, t.AvailablePeople, @Today AS CheckedOn,
       CASE WHEN t.Status = N'Active' THEN 1 ELSE 0 END AS IsPublic,
       CASE WHEN COALESCE(t.SalePrice, t.Price) <= @Budget THEN 1 ELSE 0 END AS FitsBudget,
       CASE WHEN t.Duration = @Days THEN 1 ELSE 0 END AS FitsDuration,
       CASE WHEN t.DepartureDate >= @Today THEN 1 ELSE 0 END AS HasUpcomingDeparture,
       CASE WHEN t.AvailablePeople >= @People THEN 1 ELSE 0 END AS HasEnoughSeats,
       CONCAT(
           CASE WHEN t.Status <> N'Active' THEN N'Không ở trạng thái Active; ' ELSE N'' END,
           CASE WHEN t.DepartureDate < @Today THEN N'Đã qua/chưa cập nhật ngày khởi hành; ' ELSE N'' END,
           CASE WHEN t.AvailablePeople < @People THEN N'Không đủ chỗ; ' ELSE N'' END,
           CASE WHEN COALESCE(t.SalePrice, t.Price) > @Budget THEN N'Vượt ngân sách; ' ELSE N'' END,
           CASE WHEN t.Duration <> @Days THEN N'Số ngày không khớp; ' ELSE N'' END
       ) AS Reasons
FROM Tours AS t
LEFT JOIN Destinations AS d ON d.Id = t.DestinationId
WHERE d.Name LIKE N'%' + @Destination + N'%'
   OR t.Name LIKE N'%' + @Destination + N'%'
ORDER BY t.Id;
