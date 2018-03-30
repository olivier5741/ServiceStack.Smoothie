using System;
using System.Collections.Generic;
using System.Linq;
using EasyNetQ;
using ServiceStack.OrmLite;
using ServiceStack.Smoothie.Test.Interfaces;
using ServiceStack.Text;

namespace ServiceStack.Smoothie.Test
{
    /*
     * smooth config : one every timespan
     * 10 par seconde
     * 60*10 par minute
     * 60*60*10 par heure
     */
    
    public class SmoothService : Service
    {
        private readonly IBus _bus;

        public SmoothService(IBus bus)
        {
            _bus = bus;
        }

        // Don't remember what this is used gor
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

        // TODO authorization
        // TODO add scenario, campaign and flowe
        // Create a ressource to fluidify
        public Smooth Post(Smooth request)
        {
            var app = Db.SingleById<SmoothApp>(request.AppId);

            if (app == null)
                throw new ArgumentNullException("App does not exist");

            request.TenantId = app.TenantId;
            request.AppId = app.Id;
            
            request.Id = Guid.NewGuid();
            request.Smoothed = null;
            request.Cancelled = false;
            request.Published = false;

            Db.Save(request);

            return request;
        }

        
        // Get expired ressources and release them
        public void Play(HeartBeat heartBeat)
        {
            var apps = Db.Select<SmoothApp>(a => a.Inactive == false);
           
            apps = apps.Where(a =>
            {
                var unixMS = heartBeat.Time.ToUnixTimeMs();
                var mod = heartBeat.Time.ToUnixTimeMs() % a.LimitByMilliseconds;
                return mod == 0;
            }).ToList(); // SQLite does not support modulo
            
            Post(new SmoothReleaseRequest{Data = apps.Select(a => new SmoothRelease
            {
                AppId = a.Id,
                Amount = a.LimitAmount
            }).ToList()});
        }

        // get amount of ressources not yet released or lately released (greater than request.From) 
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

        // trigger ressources
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
    }
}