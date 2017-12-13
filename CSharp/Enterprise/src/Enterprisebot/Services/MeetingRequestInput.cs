using System;

namespace Enterprisebot.Services
{
    [Serializable]
    public class MeetingRequestInput
    {
        public string OrganizerName { get; set; }
        public string OrganizerEmail { get; set; }
        public string AttendeeEmail { get; set; }
        public string AttendeeName { get; set; }
        public string MeetingSubject { get; set; }
        public DateTime? RequestedDateTime { get; set; }
        public int? MeetingDuration { get; set; }
    }
}