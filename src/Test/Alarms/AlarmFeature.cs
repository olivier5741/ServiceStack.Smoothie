using System;
using EasyNetQ;
using ServiceStack.Data;
using ServiceStack.OrmLite;
using ServiceStack.Redis;
using ServiceStack.Smoothie.Test.HeartBeats;
using ServiceStack.Smoothie.Test.Interfaces;
using ServiceStack.Validation;

namespace ServiceStack.Smoothie.Test.Alarms
{
    public class AlarmFeature : IPlugin, IPostInitPlugin
    {
        public void Register(IAppHost appHost)
        {
            appHost.RegisterService<AlarmService>();
            appHost.GetContainer().RegisterValidators(typeof(AlarmValidator).Assembly);

            using (var db = appHost.TryResolve<IDbConnectionFactory>().Open())
            {
                db.CreateTableIfNotExists<AlarmApp>();
                db.CreateTableIfNotExists<Alarm>();
            }

            appHost.GetContainer().RegisterAutoWired<HeartBeatClient>();
        }

        public void AfterPluginsLoaded(IAppHost appHost)
        {
            appHost.TryResolve<IBus>().Subscribe<HeartBeat>("alarm",
                h =>
                {
                    using (var service = appHost.TryResolve<AlarmService>())
                        service.Play();
                }, c => c.WithTopic("#.ms.0.#"));
            
            
            appHost.TryResolve<HeartBeatClient>().Start();
        }
    }
}