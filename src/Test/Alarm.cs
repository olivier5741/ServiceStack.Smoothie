using System;

namespace ServiceStack.Smoothie.Test
{
    [Route("/alarm", "POST,GET")]
    public class Alarm : IPost, IReturn<Alarm>
    {
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public DateTime Time { get; set; }
        public Guid Id { get; set; }
        
        public Guid AppId { get; set; }
        public Guid TenantId { get; set; }
        
        public bool Cancelled { get; set; }
        public bool Published { get; set; }
    }
}