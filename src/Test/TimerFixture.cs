using System;
using System.Collections.Generic;
using ServiceStack.Data;
using ServiceStack.OrmLite;
using ServiceStack.Testing;
using System.Threading;
using EasyNetQ;
using ServiceStack.Text;
using Xunit;

namespace ServiceStack.Smoothie.Test
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class AlarmService : Service
    {
        private readonly IBus _bus;

        public AlarmService(IBus bus)
        {
            _bus = bus;
        }
        
        public Alarm Post(Alarm request)
        {
            if(request.Id == Guid.Empty)
                request.Id = Guid.NewGuid();

            Db.Save(request);
            
            return request;
        }

        public Alarm Get(Alarm request)
        {
            return Db.SingleById<Alarm>(request.Id);
        }

        public void Post(AlarmCancel request)
        {
            var alarm = Db.SingleById<Alarm>(request.Id);

            if (alarm.Published)
                throw new ArgumentException("Alarm already published");
            
            alarm.Inactive = true;
            
            Db.Save(alarm);
        }

        public void Play()
        {
            // best to use redis afterwards
            var alarms = Db.Select<Alarm>(); //a => a.Time <= DateTime.Now && a.Inactive == false && a.Published == false);
            alarms.ForEach(a =>
            {
                a.Published = true;
                Db.Save(a);
                _bus.Publish(a);
            });
        }
    }

    public class Alarm
    {
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public DateTime Time { get; set; }
        public Guid Id { get; set; }
        public bool Inactive { get; set; }
        public bool Published { get; set; }
    }

    // ReSharper disable once ClassNeverInstantiated.Global
    public class AlarmCancel
    {
        public Guid Id { get; set; }
    }

    public class AlarmExpired
    {
        public Guid Id { get; set; }
    }
    
    public class AlarmFixture : IDisposable
    {
        private readonly AlarmService _svc;
        private readonly ServiceStackHost _appHost;
        private readonly IBus _bus;

        public AlarmFixture()
        {
            _appHost = new BasicAppHost().Init();
            var container =  _appHost.Container;

            container.Register<IDbConnectionFactory>(
                new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider));

            _bus = RabbitHutch.CreateBus("host=localhost");
            container.Register(_bus);

            container.RegisterAutoWired<AlarmService>();

            using (var db = container.Resolve<IDbConnectionFactory>().Open())
            {
                db.DropAndCreateTable<Alarm>();
                db.Save(new Alarm {Id = Guid.NewGuid(), Time = DateTime.Now.AddMinutes(-1)});
            }

            _svc = container.Resolve<AlarmService>();
        }
        
        [Fact]
        public void CreateAndCancel()
        {
            var alarm = _svc.Post(new Alarm{Time = DateTime.Now.AddHours(-1)});
            _svc.Post(new AlarmCancel {Id = alarm.Id});
            
            Assert.True(_svc.Get(alarm).Inactive);
        }

       // [Fact] // commented because will fail on app veyor
        public void Timer()
        {
            var counter = 0;
            _bus.Subscribe<Alarm>("test", a => counter++);
            
            var t = new Timer(o => { _svc.Play(); },null,new TimeSpan(),TimeSpan.FromSeconds(1));
            Thread.Sleep(TimeSpan.FromSeconds(3));
            Assert.True(counter > 0);
        }

        public void Dispose()
        {
            _svc?.Dispose();
            _bus?.Dispose();
            _appHost?.Dispose();
        }
    }
}