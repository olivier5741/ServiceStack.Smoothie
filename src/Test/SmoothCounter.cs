using System;

namespace ServiceStack.Smoothie.Test
{
    public class SmoothCounter
    {
        public Guid AppId { get; set; }
        public bool Published { get; set; }
        public int Count { get; set; }
    }
}