using System;

namespace Enterprisebot.GraphApiDtos
{
    public class FindMeetingTimesRequest
    {
        public Attendee[] attendees { get; set; }
        public Timeconstraint timeConstraint { get; set; }
        public Locationconstraint locationConstraint { get; set; }
        public string meetingDuration { get; set; }
        public int maxCandidates { get; set; }
    }

    public class Timeconstraint
    {
        public Timeslot[] timeslots { get; set; }
    }

    public class Timeslot
    {
        public ZonedDateTime start { get; set; }
        public ZonedDateTime end { get; set; }
    }

    public class ZonedDateTime
    {
        public DateTime dateTime { get; set; }
        public string timeZone { get; set; }
    }

    public class Locationconstraint
    {
        public string isRequired { get; set; }
        public string suggestLocation { get; set; }
        public Location[] locations { get; set; }
    }

    public class Location
    {
        public string displayName { get; set; }
        public string locationEmailAddress { get; set; }
    }

    public class Attendee
    {
        public Emailaddress emailAddress { get; set; }
        public string type { get; set; }
    }

    public class Emailaddress
    {
        public string address { get; set; }
        public string name { get; set; }
    }

    public class FindMeetingTimesResponse
    {
        public string odatacontext { get; set; }
        public string emptySuggestionsReason { get; set; }
        public Meetingtimesuggestion[] meetingTimeSuggestions { get; set; }
    }

    public class Meetingtimesuggestion
    {
        public double confidence { get; set; }
        public string organizerAvailability { get; set; }
        public Meetingtimeslot meetingTimeSlot { get; set; }
        public object[] attendeeAvailability { get; set; }
        public Location[] locations { get; set; }
    }

    public class Meetingtimeslot
    {
        public ZonedDateTime start { get; set; }
        public ZonedDateTime end { get; set; }
    }
}