using System;

namespace ServiceStack.Smoothie.Test
{
    [Route("/alarm/cancel", "POST")]
    public class AlarmCancel
    {
        public Guid Id { get; set; }
    }
}