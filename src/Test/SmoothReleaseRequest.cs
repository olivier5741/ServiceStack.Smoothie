using System.Collections.Generic;

namespace ServiceStack.Smoothie.Test
{
    [Route("/smooth/release", "POST")]
    public class SmoothReleaseRequest : IPost, IReturn<SmoothReleaseRequest>
    {
        public List<SmoothRelease> Data { get; set; }
    }
}