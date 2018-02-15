using System;

namespace ServiceStack.Smoothie.Test.Interfaces
{
    public class SmoothApp
    {
        public Guid Id { get; set; }
        
        public Guid TenantId { get; set; }

        public SmoothLimitPerHour Limit { get; set; }

        public bool Inactive { get; set; }
    }
}