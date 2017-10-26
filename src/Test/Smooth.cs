using System;
using System.Runtime.Serialization;

namespace ServiceStack.Smoothie.Test
{
    [Route("/smooth", "POST")]
    public class Smooth
    {
        [IgnoreDataMember]
        public int OrderId { get; set; } // FIFO, autoincrement

        public Guid Id { get; set; }
        
        public Guid TenantId { get; set; }
        public Guid AppId { get; set; }

        public DateTime? Smoothed { get; set; }
        
        public bool Published { get; set; }
        
        public bool Cancelled { get; set; }
    }
}