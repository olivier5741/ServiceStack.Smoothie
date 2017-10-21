using System;
using ServiceStack.Data;
using ServiceStack.OrmLite;
using ServiceStack.Testing;
using Xunit;

namespace ServiceStack.Smoothie.Test
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class TimerService : Service
    {
        public Timer Post(Timer request)
        {
            if(request.Id == Guid.Empty)
                request.Id = Guid.NewGuid();

            Db.Save(request);
            
            return request;
        }

        public Timer Get(Timer request)
        {
            return Db.SingleById<Timer>(request.Id);
        }

        public void Post(TimerCancel request)
        {
            var timer = Db.SingleById<Timer>(request.Id);
            timer.Inactive = true;
            Db.Save(timer);
        }
    }

    public class Timer
    {
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public DateTime Time { get; set; }
        public Guid Id { get; set; }
        public bool Inactive { get; set; }
    }

    // ReSharper disable once ClassNeverInstantiated.Global
    public class TimerCancel
    {
        public Guid Id { get; set; }
    }

    public class TimerExpired
    {
        public Guid Id { get; set; }
    }
    
    public class TimerFixture : IDisposable
    {
        private readonly TimerService _svc;

        public TimerFixture()
        {
            var appHost = new BasicAppHost().Init();
            var container =  appHost.Container;

            container.Register<IDbConnectionFactory>(
                new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider));

            container.RegisterAutoWired<TimerService>();

            using (var db = container.Resolve<IDbConnectionFactory>().Open())
            {
                db.DropAndCreateTable<Timer>();
            }

            _svc = container.Resolve<TimerService>();
        }
        
        [Fact]
        public void CreateAndCancel()
        {
            var timer = _svc.Post(new Timer{Time = DateTime.Now.AddHours(1)});
            _svc.Post(new TimerCancel {Id = timer.Id});
            
            Assert.True(_svc.Get(timer).Inactive);
        }

        public void Dispose()
        {
            _svc?.Dispose();
        }
    }
}