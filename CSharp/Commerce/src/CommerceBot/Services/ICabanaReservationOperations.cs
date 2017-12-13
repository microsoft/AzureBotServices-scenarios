using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CommerceBot.Services
{
    public interface ICabanaReservationOperations
    {
        // Should return basic user profile info e.g., Loyalty Level, etc.
        // Should also return any current hotel reservations
        // Should also return any cabana reservations associated with the hotel reservations
        Task<UserProfile> GetUserInformation(string name, string email);
        Task<IList<Cabana>> GetCabanaAvailability(string reservationChoice, CabanaQuery searchQuery);
        Task<CabanaReservation> ReserveCabana(int hotelReservationId, int CabanaId, DateTime startDate, int days);
        // lot more we can do
        // edit, delete, etc.
    }
}
