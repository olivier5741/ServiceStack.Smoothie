using System;

namespace ServiceStack.Smoothie.Test.Interfaces
{
    public class SmoothApp
    {
        public Guid Id { get; set; }
        
        public Guid TenantId { get; set; }
        
        public int LimitAmount { get; set; }
        public int LimitByMilliseconds { get; set; }

        public bool Inactive { get; set; }
    }
}