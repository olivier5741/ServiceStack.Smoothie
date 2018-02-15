using System.Collections.Generic;

namespace ServiceStack.Smoothie.Test.Interfaces
{
    [Route("/smooth/release", "POST")]
    public class SmoothReleaseRequest : IPost, IReturn<SmoothReleaseRequest>
    {
        public List<SmoothRelease> Data { get; set; }
    }
}