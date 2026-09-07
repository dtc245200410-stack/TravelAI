using Microsoft.EntityFrameworkCore;
using TravelAI.Models;
using Microsoft.AspNetCore.Identity;

namespace TravelAI.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(TravelDbContext context)
        {
            var passwordHasher = new PasswordHasher<User>();

            // =====================================================
            // ADMIN MẪU
            // =====================================================

            var admin = await context.Users
                .FirstOrDefaultAsync(u => u.Email == "admin@travelai.com");

            if (admin == null)
            {
                admin = new User
                {
                    FullName = "TravelAI Administrator",
                    Email = "admin@travelai.com",
                    Phone = "0900000000",
                    Role = "Admin",
                    CreatedAt = DateTime.Now
                };

                admin.PasswordHash = passwordHasher.HashPassword(
                    admin,
                    "Admin@123456"
                );

                await context.Users.AddAsync(admin);
                await context.SaveChangesAsync();
            }
            else if (admin.Role != "Admin")
            {
                admin.Role = "Admin";

                await context.SaveChangesAsync();
            }
            // =====================================================
            // 0. USER MẪU
            // =====================================================

            var user = await context.Users
                .FirstOrDefaultAsync(u => u.Email == "user@travelai.com");

            if (user == null)
            {
                user = new User
                {
                    FullName = "Nguyễn Văn A",
                    Email = "user@travelai.com",
                    Phone = "0900000000",
                    Role = "Customer",
                    CreatedAt = DateTime.Now
                };

                user.PasswordHash = passwordHasher.HashPassword(
                    user,
                    "123456"
                );

                await context.Users.AddAsync(user);
                await context.SaveChangesAsync();
            }
            else
            {
                // Tài khoản cũ có thể đang lưu mật khẩu dạng plaintext.
                // Nếu chưa phải hash của PasswordHasher thì tạo hash mới.
                if (string.IsNullOrWhiteSpace(user.PasswordHash) ||
                    !user.PasswordHash.StartsWith("AQAAAA"))
                {
                    user.PasswordHash = passwordHasher.HashPassword(
                        user,
                        "123456"
                    );

                    user.Role = "Customer";

                    await context.SaveChangesAsync();
                }
            }

            // Nếu đã có dữ liệu thì không thêm lại
            if (await context.Destinations.AnyAsync())
            {
                return;
            }

            // =====================================================
            // 1. DESTINATIONS
            // =====================================================

            var destinations = new List<Destination>
            {
                new Destination
                {
                    Name = "Đà Nẵng",
                    Slug = "da-nang",
                    Description = "Thành phố biển nổi tiếng với bãi biển đẹp, Bà Nà Hills và cầu Vàng.",
                    ImageUrl = "https://images.unsplash.com/photo-1559592413-7cec4d0cae2b?auto=format&fit=crop&w=900&q=80",
                    Province = "Đà Nẵng",
                    Region = "Miền Trung"
                },

                new Destination
                {
                    Name = "Đà Lạt",
                    Slug = "da-lat",
                    Description = "Thành phố ngàn hoa với khí hậu mát mẻ và nhiều cảnh đẹp.",
                    ImageUrl = "https://images.unsplash.com/photo-1528127269322-539801943592?auto=format&fit=crop&w=900&q=80",
                    Province = "Lâm Đồng",
                    Region = "Tây Nguyên"
                },

                new Destination
                {
                    Name = "Phú Quốc",
                    Slug = "phu-quoc",
                    Description = "Đảo ngọc với biển xanh, bãi cát đẹp và nhiều khu nghỉ dưỡng.",
                    ImageUrl = "https://images.unsplash.com/photo-1537996194471-e657df975ab4?auto=format&fit=crop&w=900&q=80",
                    Province = "Kiên Giang",
                    Region = "Miền Nam"
                },

                new Destination
                {
                    Name = "Nha Trang",
                    Slug = "nha-trang",
                    Description = "Thành phố biển nổi tiếng với các hoạt động nghỉ dưỡng và vui chơi.",
                    ImageUrl = "https://images.unsplash.com/photo-1552465011-b4e21bf6e79a?auto=format&fit=crop&w=900&q=80",
                    Province = "Khánh Hòa",
                    Region = "Miền Trung"
                },

                new Destination
                {
                    Name = "Sapa",
                    Slug = "sapa",
                    Description = "Điểm đến vùng cao nổi bật với núi non, ruộng bậc thang và Fansipan.",
                    ImageUrl = "https://images.unsplash.com/photo-1528127269322-539801943592?auto=format&fit=crop&w=900&q=80",
                    Province = "Lào Cai",
                    Region = "Miền Bắc"
                },

                new Destination
                {
                    Name = "Hạ Long",
                    Slug = "ha-long",
                    Description = "Vịnh biển nổi tiếng với hàng nghìn đảo đá và cảnh quan đặc sắc.",
                    ImageUrl = "https://images.unsplash.com/photo-1528127269322-539801943592?auto=format&fit=crop&w=900&q=80",
                    Province = "Quảng Ninh",
                    Region = "Miền Bắc"
                },

                new Destination
                {
                    Name = "Hội An",
                    Slug = "hoi-an",
                    Description = "Phố cổ nổi tiếng với kiến trúc cổ kính, đèn lồng và ẩm thực.",
                    ImageUrl = "https://images.unsplash.com/photo-1528127269322-539801943592?auto=format&fit=crop&w=900&q=80",
                    Province = "Quảng Nam",
                    Region = "Miền Trung"
                },

                new Destination
                {
                    Name = "Hà Nội",
                    Slug = "ha-noi",
                    Description = "Thủ đô với lịch sử lâu đời, văn hóa đặc sắc và nhiều điểm tham quan.",
                    ImageUrl = "https://images.unsplash.com/photo-1509030450996-dd1a26dda07a?auto=format&fit=crop&w=900&q=80",
                    Province = "Hà Nội",
                    Region = "Miền Bắc"
                }
            };

            await context.Destinations.AddRangeAsync(destinations);
            await context.SaveChangesAsync();


            // =====================================================
            // 2. TOURS
            // =====================================================

            var daNang = destinations.First(x => x.Name == "Đà Nẵng");
            var daLat = destinations.First(x => x.Name == "Đà Lạt");
            var phuQuoc = destinations.First(x => x.Name == "Phú Quốc");
            var nhaTrang = destinations.First(x => x.Name == "Nha Trang");
            var sapa = destinations.First(x => x.Name == "Sapa");
            var haLong = destinations.First(x => x.Name == "Hạ Long");
            var hoiAn = destinations.First(x => x.Name == "Hội An");
            var haNoi = destinations.First(x => x.Name == "Hà Nội");

            var tours = new List<Tour>
            {
                new Tour
                {
                    Name = "Đà Nẵng - Hội An - Bà Nà Hills",
                    Slug = "da-nang-hoi-an-ba-na-hills",
                    Description = "Khám phá những điểm đến nổi tiếng nhất miền Trung.",
                    Price = 4990000,
                    SalePrice = 3990000,
                    Duration = 3,
                    DepartureLocation = "Hà Nội",
                    DepartureDate = DateTime.Now.AddDays(15),
                    MaxPeople = 30,
                    AvailablePeople = 18,
                    ImageUrl = daNang.ImageUrl,
                    Rating = 4.9,
                    ReviewCount = 126,
                    DestinationId = daNang.Id,
                    Status = "Active"
                },

                new Tour
                {
                    Name = "Đà Lạt - Thành phố ngàn hoa",
                    Slug = "da-lat-thanh-pho-ngan-hoa",
                    Description = "Hành trình khám phá thiên nhiên và văn hóa Đà Lạt.",
                    Price = 3990000,
                    SalePrice = 3290000,
                    Duration = 3,
                    DepartureLocation = "TP. Hồ Chí Minh",
                    DepartureDate = DateTime.Now.AddDays(20),
                    MaxPeople = 30,
                    AvailablePeople = 22,
                    ImageUrl = daLat.ImageUrl,
                    Rating = 4.8,
                    ReviewCount = 98,
                    DestinationId = daLat.Id,
                    Status = "Active"
                },

                new Tour
                {
                    Name = "Phú Quốc - Thiên đường biển đảo",
                    Slug = "phu-quoc-thien-duong-bien-dao",
                    Description = "Nghỉ dưỡng và khám phá vẻ đẹp của đảo ngọc Phú Quốc.",
                    Price = 5590000,
                    SalePrice = 4590000,
                    Duration = 4,
                    DepartureLocation = "TP. Hồ Chí Minh",
                    DepartureDate = DateTime.Now.AddDays(25),
                    MaxPeople = 25,
                    AvailablePeople = 15,
                    ImageUrl = phuQuoc.ImageUrl,
                    Rating = 4.9,
                    ReviewCount = 154,
                    DestinationId = phuQuoc.Id,
                    Status = "Active"
                },

                new Tour
                {
                    Name = "Nha Trang - Biển xanh và đảo đẹp",
                    Slug = "nha-trang-bien-xanh",
                    Description = "Tận hưởng biển xanh và các hoạt động vui chơi tại Nha Trang.",
                    Price = 4490000,
                    SalePrice = 3790000,
                    Duration = 3,
                    DepartureLocation = "Hà Nội",
                    DepartureDate = DateTime.Now.AddDays(18),
                    MaxPeople = 30,
                    AvailablePeople = 20,
                    ImageUrl = nhaTrang.ImageUrl,
                    Rating = 4.7,
                    ReviewCount = 87,
                    DestinationId = nhaTrang.Id,
                    Status = "Active"
                },

                new Tour
                {
                    Name = "Sapa - Fansipan - Bản làng Tây Bắc",
                    Slug = "sapa-fansipan",
                    Description = "Khám phá núi rừng Tây Bắc và chinh phục Fansipan.",
                    Price = 4290000,
                    SalePrice = 3490000,
                    Duration = 3,
                    DepartureLocation = "Hà Nội",
                    DepartureDate = DateTime.Now.AddDays(12),
                    MaxPeople = 25,
                    AvailablePeople = 16,
                    ImageUrl = sapa.ImageUrl,
                    Rating = 4.9,
                    ReviewCount = 112,
                    DestinationId = sapa.Id,
                    Status = "Active"
                },

                new Tour
                {
                    Name = "Hạ Long - Du thuyền 5 sao",
                    Slug = "ha-long-du-thuyen",
                    Description = "Trải nghiệm nghỉ dưỡng cao cấp trên vịnh Hạ Long.",
                    Price = 6990000,
                    SalePrice = 5990000,
                    Duration = 2,
                    DepartureLocation = "Hà Nội",
                    DepartureDate = DateTime.Now.AddDays(10),
                    MaxPeople = 20,
                    AvailablePeople = 8,
                    ImageUrl = haLong.ImageUrl,
                    Rating = 4.8,
                    ReviewCount = 76,
                    DestinationId = haLong.Id,
                    Status = "Active"
                },

                new Tour
                {
                    Name = "Hội An - Phố cổ và ẩm thực",
                    Slug = "hoi-an-pho-co",
                    Description = "Khám phá phố cổ Hội An và những món ăn đặc trưng.",
                    Price = 3590000,
                    SalePrice = 2990000,
                    Duration = 3,
                    DepartureLocation = "Đà Nẵng",
                    DepartureDate = DateTime.Now.AddDays(16),
                    MaxPeople = 25,
                    AvailablePeople = 17,
                    ImageUrl = hoiAn.ImageUrl,
                    Rating = 4.8,
                    ReviewCount = 65,
                    DestinationId = hoiAn.Id,
                    Status = "Active"
                },

                new Tour
                {
                    Name = "Hà Nội - Ninh Bình - Hạ Long",
                    Slug = "ha-noi-ninh-binh-ha-long",
                    Description = "Hành trình khám phá miền Bắc với nhiều điểm đến nổi tiếng.",
                    Price = 5290000,
                    SalePrice = 4490000,
                    Duration = 4,
                    DepartureLocation = "Hà Nội",
                    DepartureDate = DateTime.Now.AddDays(22),
                    MaxPeople = 30,
                    AvailablePeople = 19,
                    ImageUrl = haNoi.ImageUrl,
                    Rating = 4.7,
                    ReviewCount = 91,
                    DestinationId = haNoi.Id,
                    Status = "Active"
                },

                new Tour
                {
                    Name = "Đà Nẵng - Huế - Hội An",
                    Slug = "da-nang-hue-hoi-an",
                    Description = "Khám phá văn hóa và lịch sử miền Trung.",
                    Price = 4690000,
                    SalePrice = 3890000,
                    Duration = 4,
                    DepartureLocation = "Hà Nội",
                    DepartureDate = DateTime.Now.AddDays(28),
                    MaxPeople = 30,
                    AvailablePeople = 21,
                    ImageUrl = daNang.ImageUrl,
                    Rating = 4.8,
                    ReviewCount = 103,
                    DestinationId = daNang.Id,
                    Status = "Active"
                },

                new Tour
                {
                    Name = "Phú Quốc - Nghỉ dưỡng cao cấp",
                    Slug = "phu-quoc-nghi-duong",
                    Description = "Kỳ nghỉ thư giãn tại các khu nghỉ dưỡng ven biển.",
                    Price = 7990000,
                    SalePrice = 6790000,
                    Duration = 4,
                    DepartureLocation = "Hà Nội",
                    DepartureDate = DateTime.Now.AddDays(30),
                    MaxPeople = 20,
                    AvailablePeople = 12,
                    ImageUrl = phuQuoc.ImageUrl,
                    Rating = 5.0,
                    ReviewCount = 58,
                    DestinationId = phuQuoc.Id,
                    Status = "Active"
                }
            };

            await context.Tours.AddRangeAsync(tours);
            await context.SaveChangesAsync();


            // =====================================================
            // 3. HOTELS
            // =====================================================

            var hotels = new List<Hotel>
            {
                new Hotel
                {
                    Name = "Ocean View Hotel",
                    Slug = "ocean-view-hotel",
                    Description = "Khách sạn hiện đại gần biển Đà Nẵng.",
                    Address = "123 Võ Nguyên Giáp",
                    City = "Đà Nẵng",
                    Province = "Đà Nẵng",
                    ImageUrl = "https://images.unsplash.com/photo-1566073771259-6a8506099945?auto=format&fit=crop&w=900&q=80",
                    Rating = 4.8,
                    ReviewCount = 156,
                    DestinationId = daNang.Id,
                    Status = "Active"
                },

                new Hotel
                {
                    Name = "Pine Hill Resort",
                    Slug = "pine-hill-resort",
                    Description = "Khu nghỉ dưỡng giữa thiên nhiên Đà Lạt.",
                    Address = "45 Trần Hưng Đạo",
                    City = "Đà Lạt",
                    Province = "Lâm Đồng",
                    ImageUrl = "https://images.unsplash.com/photo-1542314831-068cd1dbfeeb?auto=format&fit=crop&w=900&q=80",
                    Rating = 4.7,
                    ReviewCount = 112,
                    DestinationId = daLat.Id,
                    Status = "Active"
                },

                new Hotel
                {
                    Name = "Sunset Bay Resort",
                    Slug = "sunset-bay-resort",
                    Description = "Resort ven biển với không gian nghỉ dưỡng cao cấp.",
                    Address = "88 Trần Hưng Đạo",
                    City = "Phú Quốc",
                    Province = "Kiên Giang",
                    ImageUrl = "https://images.unsplash.com/photo-1582719478250-c89cae4dc85b?auto=format&fit=crop&w=900&q=80",
                    Rating = 4.9,
                    ReviewCount = 203,
                    DestinationId = phuQuoc.Id,
                    Status = "Active"
                },

                new Hotel
                {
                    Name = "Blue Ocean Hotel",
                    Slug = "blue-ocean-hotel",
                    Description = "Khách sạn gần biển Nha Trang.",
                    Address = "20 Trần Phú",
                    City = "Nha Trang",
                    Province = "Khánh Hòa",
                    ImageUrl = "https://images.unsplash.com/photo-1564501049412-61c2a3083791?auto=format&fit=crop&w=900&q=80",
                    Rating = 4.6,
                    ReviewCount = 88,
                    DestinationId = nhaTrang.Id,
                    Status = "Active"
                },

                new Hotel
                {
                    Name = "Mountain View Sapa",
                    Slug = "mountain-view-sapa",
                    Description = "Khách sạn có tầm nhìn núi tuyệt đẹp tại Sapa.",
                    Address = "12 Mường Hoa",
                    City = "Sapa",
                    Province = "Lào Cai",
                    ImageUrl = "https://images.unsplash.com/photo-1544986581-efac024faf62?auto=format&fit=crop&w=900&q=80",
                    Rating = 4.8,
                    ReviewCount = 74,
                    DestinationId = sapa.Id,
                    Status = "Active"
                },

                new Hotel
                {
                    Name = "Royal Ha Long Hotel",
                    Slug = "royal-ha-long",
                    Description = "Khách sạn cao cấp với tầm nhìn vịnh Hạ Long.",
                    Address = "68 Bãi Cháy",
                    City = "Hạ Long",
                    Province = "Quảng Ninh",
                    ImageUrl = "https://images.unsplash.com/photo-1566665797739-1674de7a421a?auto=format&fit=crop&w=900&q=80",
                    Rating = 4.7,
                    ReviewCount = 91,
                    DestinationId = haLong.Id,
                    Status = "Active"
                }
            };

            await context.Hotels.AddRangeAsync(hotels);
            await context.SaveChangesAsync();


            // =====================================================
            // 4. ROOMS
            // =====================================================

            var rooms = new List<Room>();

            foreach (var hotel in hotels)
            {
                rooms.Add(new Room
                {
                    RoomName = "Phòng Standard",
                    RoomType = "Standard",
                    MaxGuests = 2,
                    PricePerNight = 850000,
                    Quantity = 10,
                    AvailableQuantity = 10,
                    Description = "Phòng tiêu chuẩn tiện nghi.",
                    ImageUrl = hotel.ImageUrl,
                    Status = "Available",
                    HotelId = hotel.Id
                });

                rooms.Add(new Room
                {
                    RoomName = "Phòng Deluxe",
                    RoomType = "Deluxe",
                    MaxGuests = 3,
                    PricePerNight = 1250000,
                    Quantity = 8,
                    AvailableQuantity = 8,
                    Description = "Phòng Deluxe rộng rãi, tiện nghi.",
                    ImageUrl = hotel.ImageUrl,
                    Status = "Available",
                    HotelId = hotel.Id
                });

                rooms.Add(new Room
                {
                    RoomName = "Phòng Suite",
                    RoomType = "Suite",
                    MaxGuests = 4,
                    PricePerNight = 1850000,
                    Quantity = 5,
                    AvailableQuantity = 5,
                    Description = "Phòng Suite cao cấp dành cho gia đình.",
                    ImageUrl = hotel.ImageUrl,
                    Status = "Available",
                    HotelId = hotel.Id
                });
            }

            await context.Rooms.AddRangeAsync(rooms);
            await context.SaveChangesAsync();
        }
    }
}