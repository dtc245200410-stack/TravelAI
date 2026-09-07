using Microsoft.EntityFrameworkCore;
using TravelAI.Models;

namespace TravelAI.Data
{
    public class TravelDbContext : DbContext
    {
        public TravelDbContext(DbContextOptions<TravelDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Destination> Destinations { get; set; }
        public DbSet<Tour> Tours { get; set; }
        public DbSet<TourImage> TourImages { get; set; }
        public DbSet<Hotel> Hotels { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<HotelBooking> HotelBookings { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Favorite> Favorites { get; set; }
        public DbSet<Combo> Combos { get; set; }
        public DbSet<ComboItem> ComboItems { get; set; }
        public DbSet<Promotion> Promotions { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<AIConversation> AIConversations { get; set; }
        public DbSet<AIChatMessage> AIChatMessages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // HotelBooking -> Hotel
            // Không tự động xóa booking khi xóa khách sạn.
            modelBuilder.Entity<HotelBooking>()
                .HasOne(hb => hb.Hotel)
                .WithMany(h => h.HotelBookings)
                .HasForeignKey(hb => hb.HotelId)
                .OnDelete(DeleteBehavior.Restrict);

            // HotelBooking -> Room
            // Không tự động xóa booking khi xóa phòng.
            modelBuilder.Entity<HotelBooking>()
                .HasOne(hb => hb.Room)
                .WithMany()
                .HasForeignKey(hb => hb.RoomId)
                .OnDelete(DeleteBehavior.Restrict);

            // HotelBooking -> User
            // Không tự động xóa lịch sử booking khi xóa tài khoản.
            modelBuilder.Entity<HotelBooking>()
                .HasOne(hb => hb.User)
                .WithMany(u => u.HotelBookings)
                .HasForeignKey(hb => hb.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}