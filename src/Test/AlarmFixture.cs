using System;
using System.Threading;
using EasyNetQ;
using NUnit.Framework;
using ServiceStack.Data;
using ServiceStack.OrmLite;
using ServiceStack.Redis;
using ServiceStack.Testing;
using ServiceStack.Text;

namespace ServiceStack.Smoothie.Test
{
    [TestFixture]
    public class AlarmFixture : IDisposable
    {
        private readonly AlarmService _svc;
        private readonly ServiceStackHost _appHost;
        private readonly IBus _bus;

        public AlarmFixture()
        {
            JsConfig.DateHandler = DateHandler.ISO8601;
            JsConfig.TimeSpanHandler = TimeSpanHandler.DurationFormat;
            
            _appHost = new BasicAppHost().Init();
            var container =  _appHost.Container;

            container.Register<IRedisClientsManager>(c => 
                new RedisManagerPool("localhost"));
            
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
        
        [Test]
        public void CreateAndCancel()
        {
            var alarm = _svc.Post(new Alarm{Time = DateTime.Now.AddHours(-1)});
            _svc.Post(new AlarmCancel {Id = alarm.Id});
            
            Assert.True(_svc.Get(alarm).Inactive);
        }

        [Test] // commented because will fail on app veyor
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