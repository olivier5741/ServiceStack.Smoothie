using System;

namespace ServiceStack.Smoothie.Test
{
    public class SmoothApp
    {
        public Guid Id { get; set; }
        
        public Guid AppId { get; set; }
        
        public SmoothLimitPerHour Limit { get; set; }
        
        public bool Inactive { get; set; }
    }
}