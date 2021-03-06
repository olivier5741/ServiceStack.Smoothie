using System;

namespace ServiceStack.Smoothie.Test.Interfaces
{

    [Route("/smooth/next", "POST")]
    public class SmoothNextRequest : IGet, IReturn<SmoothNextResponse>
    {
        public Guid? AppId { get; set; }
        public Guid? TenantId { get; set; }
        public int Take { get; set; } = 100;
        public int Skip { get; set; }
    }
}
