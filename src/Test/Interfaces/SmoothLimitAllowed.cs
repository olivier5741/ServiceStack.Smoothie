using System;

namespace ServiceStack.Smoothie.Test.Interfaces
{
    public class SmoothLimitAllowed
    {
        public Guid AppId { get; set; }
        public int Amount { get; set; }
        public int Current { get; set; }
        public TimeSpan Per { get; set; }
    }
}