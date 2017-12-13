using System;

namespace Enterprisebot.GraphApiDtos
{
    public class CalendarEvent
    {
        public int ReminderMinutesBeforeStart { get; set; }
        public bool IsReminderOn { get; set; }
        public bool HasAttachments { get; set; }
        public string Subject { get; set; }
        public string Importance { get; set; }
        public bool IsOrganizer { get; set; }
        public bool ResponseRequested { get; set; }
        public string ShowAs { get; set; }
        public string Type { get; set; }
        public Body Body { get; set; }
        public ZonedDateTime Start { get; set; }
        public ZonedDateTime End { get; set; }
        public SimpleLocation Location { get; set; }
        public InvitedAttendee[] Attendees { get; set; }
        public Organizer Organizer { get; set; }
    }

    public class Responsestatus
    {
        public string Response { get; set; }
        public DateTime Time { get; set; }
    }

    public class Body
    {
        public string ContentType { get; set; }
        public string Content { get; set; }
    }

    public class SimpleLocation
    {
        public string DisplayName { get; set; }
    }

    public class Organizer
    {
        public Emailaddress EmailAddress { get; set; }
    }

    public class InvitedAttendee
    {
        public string Type { get; set; }
        public Status Status { get; set; }
        public Emailaddress EmailAddress { get; set; }
    }

    public class Status
    {
        public string Response { get; set; }
        public DateTime Time { get; set; }
    }
}