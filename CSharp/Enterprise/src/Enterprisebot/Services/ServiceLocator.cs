namespace Enterprisebot.Services
{
    public class ServiceLocator
    {
        public static ICalendarOperations GetCalendarOperations()
        {
            return new CalendarOperations();
        }
    }
}