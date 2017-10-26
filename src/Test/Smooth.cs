using System;

namespace ServiceStack.Smoothie.Test
{
    public class Smooth
    {
        public int OrderId { get; set; } // FIFO, autoincrement
        public Guid AppId { get; set; }
        public Guid Id { get; set; }
        public DateTime? Smoothed { get; set; }
        public bool Published { get; set; }
        public bool Cancelled { get; set; }
    }
}