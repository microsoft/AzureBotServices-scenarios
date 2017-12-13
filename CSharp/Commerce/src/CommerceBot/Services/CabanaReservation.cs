using System;

namespace CommerceBot.Services
{
    [Serializable]
    public class CabanaReservation
    {
        public int CabanaBookingId { get; set; }
        public int CabanaId { get; set; }
        public int ReservationId { get; set; }
        public DateTime StartDate { get; set; }
        public int Days { get; set; }
    }

}