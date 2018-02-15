using System;

namespace ServiceStack.Smoothie.Test.Interfaces
{
    public class HeartBeat
    {
        public DateTime Time { get; set; }
        public TimeSpan Interval { get; set; }
    }
}