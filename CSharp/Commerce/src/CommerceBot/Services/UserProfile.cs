using System;

namespace CommerceBot.Services
{
    [Serializable]
    public class UserProfile
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string EMail { get; set; }
        public string ActiveReservation { get; set; }
    }
}