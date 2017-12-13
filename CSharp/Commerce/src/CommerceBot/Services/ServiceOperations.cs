using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CommerceBot.Services
{
    public class ServiceOperations : ICabanaReservationOperations, IHotelReservationOperations
    {

        public async Task<IList<Cabana>> GetCabanaAvailability(string reservationChoice, CabanaQuery searchQuery)
        {
            await Task.Delay(1500);
            var cabanas = new List<Cabana>();
            var random = new Random(1);

            // Filling the cabana results manually for demo purposes
            for (int i = 1; i <= 3; i++)
            {
                Cabana cabana = new Cabana()
                {
                    Id = i,
                    Name = $"{reservationChoice} Cabana {i}",
                    Location = reservationChoice,
                    Rating = random.Next(1, 5),
                    NumberOfReviews = random.Next(0, 5000),
                    PriceStarting = random.Next(95, 495),
                    Image = $"https://placeholdit.imgix.net/~text?txtsize=35&txt=Cabana+{i}&w=500&h=260"
                };

                cabanas.Add(cabana);
            }

            cabanas.Sort((h1, h2) => h1.PriceStarting.CompareTo(h2.PriceStarting));

            return cabanas;
        }

        public Task<IList<string>> GetExistingReservations(int userId)
        {
            return Task.FromResult<IList<string>>(new[] { "Hawaii", "Los Angeles" });
        }

        public async Task<UserProfile> GetUserInformation(string name, string email)
        {
            await Task.Delay(1567);
            return new UserProfile { Id = 42, Name = name, EMail = email };
        }

        public async Task<CabanaReservation> ReserveCabana(int hotelReservationId, int cabanaId, DateTime startDate, int days)
        {
            CabanaReservation cr = new CabanaReservation()
            {
                CabanaId = cabanaId,
                ReservationId = hotelReservationId,
                StartDate = startDate,
                Days = days
            };

            // Talk to back end
            await Task.Delay(1234);

            cr.CabanaBookingId = 32768;
            return cr;
        }
    }
}