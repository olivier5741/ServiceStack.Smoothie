using System;

namespace ServiceStack.Smoothie.Test
{
    public class SmoothStatusRequest : IGet, IReturn<SmoothStatusResponse>
    {
        public DateTime From { get; set; }
        public Guid? TenantId { get; set; }
        public Guid? AppId { get; set; }
    }
}