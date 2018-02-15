using System;

namespace ServiceStack.Smoothie.Test.Interfaces
{
    [Route("/smooth/status", "GET")]
    public class SmoothStatusRequest : IGet, IReturn<SmoothStatusResponse>
    {
        public DateTime From { get; set; }
        public Guid? TenantId { get; set; }
        public Guid? AppId { get; set; }
    }
}