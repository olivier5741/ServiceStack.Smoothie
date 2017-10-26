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

        public SmoothNextResponse Get(SmoothNextRequest request)
        {
            var query = Db.From<Smooth>()
                .Where(s => s.Published == false &&
                            s.Cancelled == false) // TODO appIds.Contains(s.Id))
                .Take(request.Take)
                .Skip(request.Skip);

            if (request.TenantId != null)
                query.Where(s => s.AppId == request.TenantId);

            if (request.AppId != null)
                query.Where(s => s.AppId == request.AppId);

            return new SmoothNextResponse {Data = Db.Select(query)};
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

            var counters = Get(new SmoothStatusRequest {From = from}).Data;
            var apps = Db.Select<SmoothApp>(a => a.Inactive == false);

            var amountToPublishByApp = AmountToPublishByApp(apps, counters, lastTimeSpan, nextTimeSpan);
            Post(amountToPublishByApp);
        }

        public SmoothStatusResponse Get(SmoothStatusRequest request)
        {
            var query = Db.From<Smooth>()
                .Where(s => s.Cancelled == false)
                .Where(s => s.Smoothed == null || s.Smoothed >= request.From)
                .GroupBy(s => new {s.AppId, s.Published})
                .Select(s => new {s.AppId, s.Published, Count = Sql.Count("*")});

            if (request.TenantId != null)
                query.Where(s => s.TenantId == request.TenantId);

            if (request.AppId != null)
                query.Where(s => s.AppId == request.AppId);

            return new SmoothStatusResponse
            {
                Data = Db.Select<SmoothCounter>(query)
            };
        }

        public SmoothReleaseRequest Post(SmoothReleaseRequest request)
        {
            var sum = request.Data.Sum(a => a.Amount);
            while (request.Data.Count > 0)
            {
                var list = Get(new SmoothNextRequest()).Data; // amountToPublishByApp.Select(d => d.Key).ToList());
                var toPublish = PublishMultipleByAppFilter(request, list);

                toPublish.ForEach(s => s.Published = true);
                toPublish.ForEach(s => _bus.Publish(s));
                Db.SaveAll(toPublish);

                var newSum = request.Data.Sum(a => a.Amount);

                if (newSum == sum)
                    break;

                sum = newSum;
            }

            return request;
        }

        private static List<Smooth> PublishMultipleByAppFilter(SmoothReleaseRequest release,
            IReadOnlyCollection<Smooth> list)
        {
            var toPublish = new List<Smooth>();

            foreach (var r in release.Data)
            {
                var value = r.Amount;
                var next = list.Where(e => e.AppId == r.AppId).Take(value).ToList();

                toPublish.AddRange(next);

                r.Amount = value - next.Count();
            }

            release.Data.RemoveAll(r => r.Amount == 0);

            return toPublish;
        }

        private static SmoothReleaseRequest AmountToPublishByApp(IEnumerable<SmoothApp> apps,
            IList<SmoothCounter> counters,
            TimeSpan lastTimeSpan, TimeSpan nextTimeSpan)
        {
            var amountToPublishByApp = (from a in apps
                let speedCoefficient = nextTimeSpan / lastTimeSpan
                let publishedAmount = counters.SingleOrDefault(c => c.AppId == a.Id && c.Published)?.Count ?? 0
                let amount = a.Limit.Amount * speedCoefficient + (a.Limit.Amount - publishedAmount) * speedCoefficient
                where !(amount <= 0)
                select new SmoothRelease {AppId = a.Id, Amount = (int) Math.Ceiling(amount)}).ToList();

            return new SmoothReleaseRequest
            {
                Data = amountToPublishByApp
            };
        }
    }
}