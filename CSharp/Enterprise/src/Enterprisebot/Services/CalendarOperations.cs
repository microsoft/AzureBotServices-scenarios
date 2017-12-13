using Enterprisebot.GraphApiDtos;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Xml;

namespace Enterprisebot.Services
{
    public class CalendarOperations : ICalendarOperations
    {
        private const string graphUrl = "https://graph.microsoft.com/v1.0/me";

        public async Task<Dictionary<string, MeetingSlot>> FindMeetingTimes(MeetingRequestInput meetingRequest, string AccessToken)
        {
            string queryParameter = "/findMeetingTimes";
            using (var client = new HttpClient())
            {
                using (var request = new HttpRequestMessage(HttpMethod.Post, graphUrl + queryParameter))
                {
                    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);

                    FindMeetingTimesRequest fmtr = BuildFMTR(meetingRequest);

                    var postBody = await Task.Run(() => JsonConvert.SerializeObject(fmtr));
                    var httpContent = new StringContent(postBody, Encoding.UTF8, "application/json");
                    request.Content = httpContent;

                    using (var response = await client.SendAsync(request))
                    {
                        string bodyText = await response.Content.ReadAsStringAsync();

                        if (response.IsSuccessStatusCode)
                        {
                            Trace.TraceInformation("Find Meeting Request complete");
                            var findMeetingResponse = JsonConvert.DeserializeObject<FindMeetingTimesResponse>(bodyText);

                            Dictionary<string, MeetingSlot> possibleTimes = new Dictionary<string, MeetingSlot>();
                            
                            foreach (var item in findMeetingResponse.meetingTimeSuggestions)
                            {
                                DateTime start = item.meetingTimeSlot.start.dateTime;
                                possibleTimes.Add(start.ToLongTimeString(), new MeetingSlot() { Start = start });
                            }
                            return possibleTimes;
                        }
                        else
                        {
                            Trace.TraceError(bodyText);
                            return null;
                        }
                    }
                }
            }
        }

        public async Task<string> MakeAppointment(MeetingRequestInput meetingRequest, MeetingSlot slot, string AccessToken)
        {
            string retval = "Make Appointment Results";   
            string queryParameter = "/events";

            using (var client = new HttpClient())
            {
                using (var req = new HttpRequestMessage(HttpMethod.Post, graphUrl + queryParameter))
                {
                    req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);

                    CalendarEvent ce = CreateCalendarEvent(meetingRequest, slot);

                    var postBody = await Task.Run(() => JsonConvert.SerializeObject(ce));

                    var httpContent = new StringContent(postBody, Encoding.UTF8, "application/json");
                    req.Content = httpContent;

                    using (var resp = await client.SendAsync(req))
                    {
                        var bodyText = await resp.Content.ReadAsStringAsync();
                        if (resp.IsSuccessStatusCode)
                        {
                            Trace.TraceInformation("Create Event Request Complete.");
                            retval = "Meeting Created!";
                        }
                        else
                        {
                            Trace.TraceError("Create Event Request Failed to Complete.");
                            retval = "Meeting Not Created";
                        }
                    }
                }
            }
            return retval;
        }

        
        private FindMeetingTimesRequest BuildFMTR(MeetingRequestInput mri)
        {
            DateTime preferredDate = mri.RequestedDateTime.Value;
            int meetingDuration = mri.MeetingDuration.Value;


            FindMeetingTimesRequest fmtr = new FindMeetingTimesRequest();

            // Attendees
            // In this example, we're only looking at the organzier's 
            // calendar. 
            var me = new Attendee();
            var ema = new Emailaddress()
            {
                address = mri.OrganizerEmail,
                name = mri.OrganizerName
            };
            me.emailAddress = ema;
            me.type = "Required";

            // Location Constraint
            var lc = new Locationconstraint();

            var l = new Location()
            {
                displayName = "Skype"
            };
            lc.locations = new Location[] { l };
            lc.isRequired = bool.FalseString;
            lc.suggestLocation = bool.TrueString;

            // Time Constraint 
            var tc = new Timeconstraint();
            var ztStart = new ZonedDateTime();
            var ztEnd = new ZonedDateTime();

            // TODO: This assumes we're offsetting from Pacific Time
            // https://developer.microsoft.com/en-us/graph/docs/api-reference/v1.0/resources/dateTimeTimeZone
            var current = new DateTime(preferredDate.Year, preferredDate.Month, preferredDate.Day, 17, 0, 0, DateTimeKind.Utc);
            string timeZoneText = "Pacific Standard Time";
            
            ztStart.dateTime = current;
            ztStart.timeZone = timeZoneText;
            ztEnd.dateTime = current.AddDays(1).AddHours(1);
            ztEnd.timeZone = timeZoneText;

            var ts = new Timeslot()
            {
                start = ztStart,
                end = ztEnd
            };
            tc.timeslots = new Timeslot[] { ts };

            fmtr.attendees = new Attendee[] { me };
            fmtr.locationConstraint = lc;
            // https://developer.microsoft.com/en-us/graph/docs/api-reference/v1.0/api/user_findmeetingtimes
            // The length of the meeting, denoted in ISO8601 format.
            fmtr.meetingDuration = XmlConvert.ToString(TimeSpan.FromSeconds(meetingDuration));
            fmtr.timeConstraint = tc;
            fmtr.maxCandidates = 3;

            return fmtr;
        }

        private CalendarEvent CreateCalendarEvent(MeetingRequestInput mri, MeetingSlot slot)
        {
            CalendarEvent ce = new CalendarEvent();
            var ema = new Emailaddress()
            {
                address = mri.OrganizerEmail,
                name = mri.OrganizerName
            };
            var o = new Organizer()
            {
                EmailAddress = ema
            };
            ce.Organizer = o;
            ce.IsOrganizer = true;

            ce.Location = new SimpleLocation() { DisplayName = "Skype" };
            // TODO: Here's where to integrate with CRM
            // Probably need three extra items:
            // ICRMOperations
            // CRMOperations
            // CRM DTO of some sort
            string bodyText = "How can I help you?";
            ce.Body = new Body() { Content = bodyText, ContentType = "text" };

            // TODO: This assumes we're offsetting from Pacific Time
            // https://developer.microsoft.com/en-us/graph/docs/api-reference/v1.0/resources/dateTimeTimeZone
            string timeZoneText = "Pacific Standard Time";
            ce.Start = new ZonedDateTime() { dateTime = slot.Start, timeZone = timeZoneText };
            ce.End = new ZonedDateTime() { dateTime = slot.Start.AddSeconds(mri.MeetingDuration.Value), timeZone = timeZoneText };
            ce.Subject = mri.MeetingSubject;

            var aema = new Emailaddress()
            {
                address = mri.AttendeeEmail,
                name = mri.AttendeeName
            };
            var a = new InvitedAttendee()
            {
                EmailAddress = aema,
                Type = "Required"
            };
            ce.Attendees = new InvitedAttendee[] { a, };
            ce.ResponseRequested = true;

            // https://developer.microsoft.com/en-us/graph/docs/api-reference/v1.0/api/user_findmeetingtimes
            ce.Type = "0"; // Single Instance
            ce.Importance = "1"; // Normal
            ce.ShowAs = "2"; // Busy

            ce.IsReminderOn = true;
            ce.ReminderMinutesBeforeStart = 15;

            ce.HasAttachments = false;

            return ce;
        }
    }
}