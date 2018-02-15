using System;
using EasyNetQ;
using ServiceStack.OrmLite;
using ServiceStack.Smoothie.Test.Interfaces;

namespace ServiceStack.Smoothie.Test.Alarms
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class AlarmService : Service
    {
        private readonly IBus _bus;

        public AlarmService(IBus bus)
        {
            _bus = bus;
        }
        
        // Schedule an alarm. When time is right, it will be published.
        public Alarm Post(Alarm request)
        {
            var app = Db.SingleById<AlarmApp>(request.AppId);

            if (app == null)
                throw new ArgumentNullException("App does not exist");

            request.TenantId = app.TenantId;
            request.AppId = app.Id;
            
            request.Id = Guid.NewGuid();
            request.Cancelled = false;
            request.Published = false;

            Db.Save(request);
            
            return request;
        }

        public Alarm Get(Alarm request)
        {
            return Db.SingleById<Alarm>(request.Id);
        }

        // Cancel the alarm (if not yet published)
        public void Post(AlarmCancel request)
        {
            var alarm = Db.SingleById<Alarm>(request.Id);

            if (alarm.Published)
                throw new ArgumentException("Alarm already published");
            
            alarm.Cancelled = true;
            
            Db.Save(alarm);
        }

        // Get all expired alarms (also not cancelled and not published) and publish them
        public void Play()
        {
            // best to use redis afterwards
            var alarms = Db.Select<Alarm>(a => a.Time <= DateTime.Now && a.Cancelled == false && a.Published == false);
            alarms.ForEach(a =>
            {
                a.Published = true;
                // at most once
                Db.Save(a);
                Redis.Set("test:alarm:"+a.Id, a, TimeSpan.FromMinutes(1)); // Don't remember why I need this
                _bus.Publish(a);
            });
        }
    }

    // ReSharper disable once ClassNeverInstantiated.Global
}