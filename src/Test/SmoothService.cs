using System;
using System.Collections.Generic;
using System.Linq;
using EasyNetQ;
using ServiceStack.OrmLite;

namespace ServiceStack.Smoothie.Test
{
    public class SmoothService : Service
    {
        private readonly IBus _bus;

        public SmoothService(IBus bus)
        {
            _bus = bus;
        }

        private List<Smooth> Next(List<Guid> appIds)
        {
            var query = Db.From<Smooth>()
                .Where(s => s.Published ==
                            false) //s.Cancelled == false && s.Published == false && appIds.Contains(s.Id))
                .Take(100);
            return Db.Select(query);
        }

        public Smooth Post(Smooth request)
        {
            var app = Db.SingleById<SmoothApp>(request.AppId);

            if (app == null)
                throw new ArgumentNullException("App does not exist");

            Db.Save(request);

            return request;
        }

        public void Play(TimeSpan lastTimeSpan, TimeSpan nextTimeSpan)
        {
            var now = DateTime.Now;
            var from = now.Subtract(lastTimeSpan);

            var counters = GetCounters(@from);
            var apps = Db.Select<SmoothApp>(a => a.Inactive == false);

            var amountToPublishByApp = AmountToPublishByApp(apps, counters, lastTimeSpan, nextTimeSpan);
            Publish(amountToPublishByApp);
        }

        private List<SmoothCounter> GetCounters(DateTime @from)
        {
            var query = Db.From<Smooth>()
                .Where(s => s.Cancelled == false)
                .Where(s => s.Smoothed == null || s.Smoothed >= @from)
                .GroupBy(s => new {s.AppId, s.Published})
                .Select(s => new {s.AppId, s.Published, Count = Sql.Count("*")});

            var counters = Db.Select<SmoothCounter>(query);
            return counters;
        }

        private void Publish(Dictionary<Guid, int> amountToPublishByApp)
        {
            var sum = amountToPublishByApp.Sum(a => a.Value);
            while (amountToPublishByApp.Count > 0)
            {
                var list = Next(amountToPublishByApp.Select(d => d.Key).ToList());
                var toPublish = PublishPreparation(amountToPublishByApp, list);

                toPublish.ForEach(s => s.Published = true);
                toPublish.ForEach(s => _bus.Publish(s));
                Db.SaveAll(toPublish);
                
                var newSum = amountToPublishByApp.Sum(a => a.Value);
                
                if (newSum == sum)
                    break;
                
                sum = newSum;
            }
        }

        private static List<Smooth> PublishPreparation(Dictionary<Guid, int> amountToPublishByApp, IReadOnlyCollection<Smooth> list)
        {
            var toPublish = new List<Smooth>();
            foreach (var key in amountToPublishByApp.Keys.ToList())
            {
                var value = amountToPublishByApp[key];
                var next = list.Where(e => e.AppId == key).Take(value).ToList();

                toPublish.AddRange(next);

                value = value - next.Count();

                if (value == 0)
                    amountToPublishByApp.RemoveKey(key);
                else
                    amountToPublishByApp[key] = value;
            }
            return toPublish;
        }

        private static Dictionary<Guid, int> AmountToPublishByApp(IEnumerable<SmoothApp> apps, IList<SmoothCounter> counters,
            TimeSpan lastTimeSpan, TimeSpan nextTimeSpan)
        {
            var amountToPublishByApp = new Dictionary<Guid, int>();

            foreach (var a in apps)
            {
                var speedCoefficient = nextTimeSpan / lastTimeSpan;
                var publishedAmount = counters.SingleOrDefault(c => c.AppId == a.Id && c.Published)?.Count ?? 0;

                // speed dictated by limit + progressive retroaction to reach the speed
                var amount = a.Limit.Amount * speedCoefficient + (a.Limit.Amount - publishedAmount) * speedCoefficient;

                if (amount <= 0)
                    continue;

                amountToPublishByApp.Add(a.Id, (int) Math.Ceiling(amount));
            }
            return amountToPublishByApp;
        }
    }
}