using System;
using System.Threading;
using EasyNetQ;
using NUnit.Framework;
using ServiceStack.Data;
using ServiceStack.OrmLite;
using ServiceStack.Redis;
using ServiceStack.Smoothie.Test.HeartBeats;
using ServiceStack.Smoothie.Test.Interfaces;
using ServiceStack.Testing;
using ServiceStack.Text;

namespace ServiceStack.Smoothie.Test.Alarms
{
    [TestFixture]
    public class AlarmFixture : IDisposable
    {
        private readonly AlarmService _svc;
        private readonly ServiceStackHost _appHost;
        private readonly IBus _bus;
        private AlarmApp _alarmApp;

        public AlarmFixture()
        {
            JsConfig.DateHandler = DateHandler.ISO8601;
            JsConfig.TimeSpanHandler = TimeSpanHandler.DurationFormat;

            _appHost = new BasicAppHost().Init();
            var container = _appHost.Container;
            
            var redisClientsManager = new RedisManagerPool("localhost");
            container.Register<IRedisClientsManager>(c => redisClientsManager);

            container.Register<IDbConnectionFactory>(
                new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider));

            _bus = RabbitHutch.CreateBus("host=localhost");
            container.Register(_bus);
            
            container.RegisterAutoWired<HeartBeatClient>();
            container.RegisterAutoWired<AlarmService>();

            using (var db = container.Resolve<IDbConnectionFactory>().Open())
            {
                db.DropAndCreateTable<Alarm>();
                db.DropAndCreateTable<AlarmApp>();
                
                _alarmApp = new AlarmApp {Id = Guid.NewGuid(), TenantId = Guid.NewGuid()};
                db.Save(_alarmApp);
                db.Save(new Alarm {Id = Guid.NewGuid(), AppId = _alarmApp.Id, Time = DateTime.Now.AddMinutes(-1)});
            }

            _svc = container.Resolve<AlarmService>();

            _bus.Subscribe<HeartBeat>("alarm", a =>
            {
                container.Resolve<AlarmService>().Play();
                
            }, cfg => cfg.WithTopic("#.ms.500.#").WithTopic("#.ms.0.#"));
        }
        
        [Test]
        public void SubscribeToAlarmAndPostAlarmInThePast()
        {
            var counter = 0;
            _bus.Subscribe<Alarm>("test", a => counter++);
            
            _svc.Post(new Alarm {Time = DateTime.Now.AddHours(-1), AppId = _alarmApp.Id});

            var t = new Timer(o => { _svc.Play(); }, null, new TimeSpan(), TimeSpan.FromSeconds(1));
            Thread.Sleep(TimeSpan.FromSeconds(3));
            Assert.True(counter > 0);
        }

        [Test]
        public void CreateAlarmAndCancelThrowsAlreadyPublished()
        {
            var alarm = _svc.Post(new Alarm {Time = DateTime.Now.AddHours(-1), AppId = _alarmApp.Id});

            var heartBeat = _appHost.Resolve<HeartBeatClient>();
            
            heartBeat.Start();
            Thread.Sleep(2000);
            heartBeat.Dispose();

            Assert.Throws<ArgumentException>(() => _svc.Post(new AlarmCancel {Id = alarm.Id}));
        }

        public void RedisTest()
        {
            var redis = new RedisClient();

            // have a heartbeat with intelligent routing, redis filter to try to publish once, and retroaction to 
            // publish missing ...

            // have a simple set (PopItemsFromSet) foreach but do intelligent keys (is it possible) to remove 
            // specific campaigns

            var setName = "smoothie.alarm.appid.100";

            redis.AddItemToList(setName, "my value");

            var items = redis.PopItemsFromSet(setName, 100);
        }

        public void Dispose()
        {
            _svc?.Dispose();
            _bus?.Dispose();
            _appHost?.Dispose();
        }
    }
}