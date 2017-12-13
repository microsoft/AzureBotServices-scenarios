using System.Collections.Generic;
using System.Threading.Tasks;

namespace CommerceBot.Services
{
    public interface IHotelReservationOperations
    {
        Task<IList<string>> GetExistingReservations(int userId);
    }
}
