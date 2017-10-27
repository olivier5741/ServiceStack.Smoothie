using System;
using System.Collections.Generic;
using ServiceStack.OrmLite;
using EasyNetQ;

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

        public void Post(AlarmCancel request)
        {
            var alarm = Db.SingleById<Alarm>(request.Id);

            if (alarm.Published)
                throw new ArgumentException("Alarm already published");
            
            alarm.Cancelled = true;
            
            Db.Save(alarm);
        }

        public void Play()
        {
            // best to use redis afterwards
            var alarms = Db.Select<Alarm>(a => a.Time <= DateTime.Now && a.Cancelled == false && a.Published == false);
            alarms.ForEach(a =>
            {
                a.Published = true;
                Db.Save(a);
                Redis.Set("test:alarm:"+a.Id, a, TimeSpan.FromMinutes(1));
                _bus.Publish(a);
            });
        }
    }

    // ReSharper disable once ClassNeverInstantiated.Global
}