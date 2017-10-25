using System;
using System.Collections.Generic;
using System.Linq;
using EasyNetQ;
using ServiceStack.Data;
using ServiceStack.OrmLite;
using ServiceStack.Redis;
using ServiceStack.Testing;
using ServiceStack.Text;
using Xunit;

namespace ServiceStack.Smoothie.Test
{
    public class Smooth
    {
        public int OrderId { get; set; } // FIFO, autoincrement
        public Guid AppId { get; set; }
        public Guid Id { get; set; }
        public DateTime? Smoothed { get; set; }
        public bool Published { get; set; }
        public bool Cancelled { get; set; }
    }

    public class Smoothed
    {
        public DateTime Time { get; set; }
        public Guid AppId { get; set; }
        public Guid Id { get; set; }
    }

    public class SmoothApp
    {
        public Guid Id { get; set; }
        public SmoothLimitPerHour Limit { get; set; }
        public bool Inactive { get; set; }
    }

    public class SmoothLimitPerHour
    {
        public int Amount { get; set; }
    }

    public class SmoothCounter
    {
        public Guid AppId { get; set; }
        public bool Published { get; set; }
        public int Count { get; set; }
    }

    public class SmoothLimitAllowed
    {
        public Guid AppId { get; set; }
        public int Amount { get; set; }
        public int Current { get; set; }
        public TimeSpan Per { get; set; }
    }

    public class SmoothService : Service
    {
        private readonly IBus _bus;

        public SmoothService(IBus bus)
        {
            _bus = bus;
        }

        private List<Smooth> Next(List<Guid> appIds)
        {
            var query = Db.From<Smooth>().Where(s => s.Published == false)  //s.Cancelled == false && s.Published == false && appIds.Contains(s.Id))
                .Take(100);
            return Db.Select(query);
        }

        public Smooth Post(Smooth request)
        {
            var app = Db.SingleById<SmoothApp>(request.AppId);
            
            if(app == null)
                throw new ArgumentNullException("App does not exist");
            
            Db.Save(request);

            return request;
        }
        
        public void Play(TimeSpan lastTimeSpan, TimeSpan nextTimeSpan)
        {
            var now = DateTime.Now;
            var from = now.Subtract(lastTimeSpan);
            
            var query = Db.From<Smooth>()
                .Where(s => s.Cancelled == false)
                .Where(s => s.Smoothed == null || s.Smoothed >= from)
                .GroupBy(s => new { s.AppId, s.Published })
                .Select(s => new { s.AppId, s.Published, Count = Sql.Count("*")});

            var counters = Db.Select<SmoothCounter>(query);

            var apps = Db.Select<SmoothApp>(a => a.Inactive == false);

            var amountToPublishByApp = new Dictionary<Guid,int>();
            
            foreach (var a in apps)
            {
                var speedCoefficient = nextTimeSpan / lastTimeSpan;
                var publishedAmount = counters.SingleOrDefault(c => c.AppId == a.Id && c.Published)?.Count ?? 0;
                
                // speed dictated by limit + progressive retroaction to reach the speed
                var amount = a.Limit.Amount*speedCoefficient + (a.Limit.Amount - publishedAmount)*speedCoefficient;

                if (amount <= 0)
                    continue;
                
                amountToPublishByApp.Add(a.Id, (int)Math.Ceiling(amount));
            }
            
            
            // TODO dangerous zone, a while is not a good idea
            while (amountToPublishByApp.Count > 0)
            {
                var appIds = amountToPublishByApp.Select(d => d.Key).ToList();
                var toPublish = new List<Smooth>();
                var list = Next(appIds);

                var appToRemove = new List<Guid>();
                
                foreach (var key in amountToPublishByApp.Keys.ToList())
                {
                    var value = amountToPublishByApp[key];
                    var next = list.Where(e => e.AppId == key).Take(value).ToList();
                    
                    toPublish.AddRange(next);
                    
                    // TODO not sure I can update an iterated dictionnary
                   value = value - next.Count();

                    if (value == 0)
                        amountToPublishByApp.RemoveKey(key);
                    else
                        amountToPublishByApp[key] = value;
                }
                
                toPublish.ForEach(s => s.Published = true);
                toPublish.ForEach(s => _bus.Publish(s));

                Db.SaveAll(toPublish);
            }
        }
    }
    
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
                Limit = new SmoothLimitPerHour
                {
                    Amount = 5
                }
            };
            
            JsConfig.DateHandler = DateHandler.ISO8601;
            JsConfig.TimeSpanHandler = TimeSpanHandler.DurationFormat;
            
            _appHost = new BasicAppHost().Init();
            var container =  _appHost.Container;

            //container.Register<IRedisClientsManager>(c => 
              //  new RedisManagerPool("localhost"));
            
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
        
      //  [Fact]
        public void Test()
        {
            _svc.Post(new Smooth {Id = Guid.NewGuid(), AppId = _app.Id});
            
            _svc.Play(TimeSpan.FromMinutes(30),TimeSpan.FromSeconds(30));
            
            Assert.True(true);
        }
    }
}