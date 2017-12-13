namespace CommerceBot.Services
{
    public class ServiceLocator
    {
        public static ICabanaReservationOperations GetCabanaReservationOperations()
        {
            return new ServiceOperations();
        }

        public static IHotelReservationOperations GetHotelReservationOperations()
        {
            return new ServiceOperations();
        }

    }
}