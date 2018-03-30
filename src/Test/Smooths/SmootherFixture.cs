using System;
using EasyNetQ;
using NUnit.Framework;
using ServiceStack.Data;
using ServiceStack.OrmLite;
using ServiceStack.Redis;
using ServiceStack.Smoothie.Test.Interfaces;
using ServiceStack.Testing;
using ServiceStack.Text;

namespace ServiceStack.Smoothie.Test.Smooths
{
    [TestFixture]
    public class UtilityFixture
    {
        [Test]
        public void GuidGeneration()
        {
            Guid.NewGuid().PrintDump();
        }
    }

    // TODO : find best way to fluidify at max so 300 by hour becomes 60*60*10 / 300 -> one very 12 * 100 ms
    
    // 700 by hour is 7 by 36 * 100 ms so 7 every 3.6 seconds
    
    [TestFixture]
    public class SmootherFixture
    {
        private readonly SmoothApp _app;
        private readonly ServiceStackHost _appHost;
        private readonly IBus _bus;
        private SmoothService _svc;

        public SmootherFixture()
        {
            _app = new SmoothApp
            {
                Id = new Guid("d8927a9c-7512-4b1b-9ed7-c6d2bdd68e60"),
                
                LimitAmount = 7,
                LimitByMilliseconds = (int) TimeSpan.FromMilliseconds(1200).TotalMilliseconds
            };

            JsConfig.DateHandler = DateHandler.ISO8601;
            JsConfig.TimeSpanHandler = TimeSpanHandler.DurationFormat;

            _appHost = new BasicAppHost().Init();
            var container = _appHost.Container;

            container.Register<IRedisClientsManager>(c =>
                new RedisManagerPool("localhost"));

            container.Register<IDbConnectionFactory>(
                new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider));

            _bus = RabbitHutch.CreateBus("host=localhost");
            container.Register(_bus);

            container.RegisterAutoWired<SmoothService>();

            using (var db = container.Resolve<IDbConnectionFactory>().Open())
            {
                db.CreateTable<Smooth>();
                db.CreateTable<SmoothApp>();
                db.Save(_app);
            }

            _svc = container.Resolve<SmoothService>();
        }

        [Test]
        public void GuidGeneration()
        {
            Guid.NewGuid().PrintDump();
        }

        [Test]
        public void Test()
        {
            _svc.Post(new Smooth {Id = Guid.NewGuid(), AppId = _app.Id});

            _svc.Play(new HeartBeat{Time = DateTime.Now.Date.AddSeconds(1).AddMilliseconds(200), Interval = TimeSpan.FromMilliseconds(100)});

            Assert.True(true);
        }
    }
}