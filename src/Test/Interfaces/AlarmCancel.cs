using System;

namespace ServiceStack.Smoothie.Test.Interfaces
{
    [Route("/alarm/cancel", "POST")]
    public class AlarmCancel
    {
        public Guid Id { get; set; }
    }
}