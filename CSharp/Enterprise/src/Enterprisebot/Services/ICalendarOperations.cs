using System.Collections.Generic;
using System.Threading.Tasks;

namespace Enterprisebot.Services
{
    public interface ICalendarOperations
    {
        Task<Dictionary<string, MeetingSlot>> FindMeetingTimes(MeetingRequestInput meetingRequest, string AccessToken);
        Task<string> MakeAppointment(MeetingRequestInput meetingRequest, MeetingSlot slot, string AccessToken);
    }
}
