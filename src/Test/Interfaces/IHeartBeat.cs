using System;

namespace ServiceStack.Smoothie.Test.Interfaces
{
    public interface IHeartBeat
    {
        DateTime Time { get; set; }
        TimeSpan Interval { get; set; }
    }
}