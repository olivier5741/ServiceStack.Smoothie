using System;

namespace ServiceStack.Smoothie.Test
{
    public class Alarm
    {
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public DateTime Time { get; set; }
        public Guid Id { get; set; }
        public bool Inactive { get; set; }
        public bool Published { get; set; }
    }
}