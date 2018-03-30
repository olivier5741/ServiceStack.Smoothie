using System;

namespace ServiceStack.Smoothie.Test.Interfaces
{
    public class SmoothLimit
    {
        public int Amount { get; set; }
        public TimeSpan By { get; set; } // multiple of 10 seconds
    }
}