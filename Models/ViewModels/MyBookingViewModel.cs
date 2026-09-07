using TravelAI.Models;

namespace TravelAI.Models.ViewModels
{
    public class MyBookingViewModel
    {
        public List<Booking> TourBookings { get; set; }
            = new List<Booking>();

        public List<HotelBooking> HotelBookings { get; set; }
            = new List<HotelBooking>();
    }
}