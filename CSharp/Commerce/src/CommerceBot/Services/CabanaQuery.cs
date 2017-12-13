using Microsoft.Bot.Builder.FormFlow;
using System;

namespace CommerceBot.Services
{
    [Serializable]
    public class CabanaQuery
    {
        [Prompt("What day to you want to {&} your booking?")]
        public DateTime Start { get; set; }

        [Numeric(1, 14)]
        [Prompt("How many {&} do you want the cabana?")]
        public int Days { get; set; }
    }
}