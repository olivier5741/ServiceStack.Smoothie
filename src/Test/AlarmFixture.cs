using System;
using System.IO;
using System.Threading;
using EasyNetQ;
using NUnit.Framework;
using ServiceStack.Auth;
using ServiceStack.Data;
using ServiceStack.OrmLite;
using ServiceStack.Redis;
using ServiceStack.Redis.Generic;
using ServiceStack.Testing;
using ServiceStack.Text;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ServiceStack.Smoothie.Test
{
    public class HeartBeatUnprecise
    {
        public DateTime Time { get; set; }
        public TimeSpan Interval { get; set; }
    }

    public class HeartBeat
    {
        public DateTime Time { get; set; }
        public TimeSpan Interval { get; set; }
    }

    public class HeartBeatClient : IDisposable
    {
        private readonly IBus _bus;
        private readonly IRedisClientsManager _redisClientsManager;
        private readonly System.Timers.Timer _timer;
        private TimeSpan _interval;

        public HeartBeatClient(IBus bus, IRedisClientsManager redisClientsManager, TimeSpan interval)
        {
            _interval = interval;
            _bus = bus;
            _redisClientsManager = redisClientsManager;
            _timer = new System.Timers.Timer(_interval.Milliseconds);

            _timer.Elapsed += (sender, args) =>
            {
                // might be better to get it from dependency injection ...
                bus.Publish(new HeartBeatUnprecise {Time = args.SignalTime, Interval = _interval});
            };
        }

        public void Start()
        {
            _timer.Enabled = true;
            _bus.Subscribe<HeartBeatUnprecise>("peacemaker", h =>
            {
                using (var redis = _redisClientsManager.GetClient())
                {
                    var rounded = new DateTime((long) Math.Floor((double) h.Time.Ticks / _interval.Ticks) *
                                               _interval.Ticks);

                    var value = rounded.ToString("O");
                    var isNotPresent = redis.AddItemToSortedSet("test:peacemaker", value);

                    if (isNotPresent == false)
                        return;


                    
                    
                    _bus.Publish(new HeartBeat
                    {
                        Time = rounded,
                        Interval = _interval
                    },rounded.Topic());
                }
            });

            _bus.Subscribe<HeartBeatUnprecise>("missing-beats", h =>
            {
                using (var redis = _redisClientsManager.GetClient())
                {
                    var from = h.Time.AddMinutes(2);
                    var to = h.Time.AddMinutes(1);
                    
                    var list = redis.GetRangeFromSortedSetByHighestScore("test:peacemaker",from.ToString("O"),
                        to.ToString("O"));

                    var expected = new List<string>();

                    for (var d = from.CreateCopy(); d < to.CreateCopy(); d = d.Add(_interval))
                    {
                        expected.Add(d.ToString("O"));
                    }

                    var missing = list.Except(list);
                    
                    foreach (var s in missing)
                    {
                        _bus.Publish(new HeartBeatUnprecise {Time = DateTime.Parse(s), Interval = _interval});
                    }
                }
            });

            _bus.Subscribe<HeartBeat>("peacemaker", h =>
            {
                h.PrintDump();
            }, cfg => cfg.WithTopic("#.ms.500.#").WithTopic("#.ms.0.#"));
        }

        

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }

    public static class HeartBeatExts
    {
        public static string Topic(this DateTime d)
        {
            var t = $"d.{d.Day}.wd.{d.DayOfWeek.ToString().ToLower()}.h.{d.Hour}.m.{d.Minute}.s.{d.Second}.ms.{d.Millisecond}";
            return t;
        }
    }

    [TestFixture]
    public class HeartBeatFixture
    {
        private HeartBeatClient _heartbeatSvc;
        private IBus _bus;
        private RedisManagerPool _redisClientsManager;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            JsConfig.DateHandler = DateHandler.ISO8601;
            _bus = RabbitHutch.CreateBus("host=localhost");
            _redisClientsManager = new RedisManagerPool("localhost");
            _heartbeatSvc = new HeartBeatClient(_bus, _redisClientsManager, TimeSpan.FromMilliseconds(100));
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            _heartbeatSvc?.Dispose();
            _bus?.Dispose();
            _redisClientsManager.Dispose();
        }

        [Test]
        public void HeartBeatTest()
        {
            _heartbeatSvc.Start();
            Thread.Sleep(2000);
        }
    }

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
            var container = _appHost.Container;

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
            var alarm = _svc.Post(new Alarm {Time = DateTime.Now.AddHours(-1)});
            _svc.Post(new AlarmCancel {Id = alarm.Id});

            Assert.True(_svc.Get(alarm).Cancelled);
        }

        //  [Test] // commented because will fail on app veyor
        public void Timer()
        {
            var counter = 0;
            _bus.Subscribe<Alarm>("test", a => counter++);

            var t = new Timer(o => { _svc.Play(); }, null, new TimeSpan(), TimeSpan.FromSeconds(1));
            Thread.Sleep(TimeSpan.FromSeconds(3));
            Assert.True(counter > 0);
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