using System.Collections.Generic;

namespace ServiceStack.Smoothie.Test
{
    public class SmoothReleaseRequest : IPost, IReturn<SmoothReleaseRequest>
    {
        public List<SmoothRelease> Data { get; set; }
    }
}