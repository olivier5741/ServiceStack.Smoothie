using System;
using System.Collections.Generic;
using Xunit;

namespace ServiceStack.Smoothie.Test
{
    public class Smooth
    {
        public Guid AppId { get; set; }
        public Guid Id { get; set; }
    }

    public class SmoothApp
    {
        public Guid Id { get; set; }
        public List<SmoothLimit> Limits { get; set; }
    }

    public class SmoothLimit
    {
       public int Amount { get; set; }
        public TimeSpan Per { get; set; }
    }
    
    public class SmootherFixture
    {
        private SmoothApp _app;

        public SmootherFixture()
        {
            _app = new SmoothApp
            {
                Id = new Guid("d8927a9c-7512-4b1b-9ed7-c6d2bdd68e60"),
                Limits = new List<SmoothLimit>
                {
                    new SmoothLimit
                    {
                        Amount = 5,
                        Per = TimeSpan.FromMinutes(1)
                    }
                }
            };
        }
        
        [Fact]
        public void Test()
        {
            var t = Guid.NewGuid();
            Assert.True(true);
        }
    }
}