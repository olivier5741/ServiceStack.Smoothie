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
        private HeartBeatClient _heartBeatClient;

        public void Register(IAppHost appHost)
        {
            appHost.RegisterService<AlarmService>();
            appHost.GetContainer().RegisterValidators(typeof(AlarmValidator).Assembly);

            using (var db = appHost.TryResolve<IDbConnectionFactory>().Open())
            {
                db.CreateTableIfNotExists<AlarmApp>();
                db.CreateTableIfNotExists<Alarm>();
            }

            _heartBeatClient = new HeartBeatClient(appHost.TryResolve<IBus>(),
                appHost.TryResolve<IRedisClientsManager>(), TimeSpan.FromMilliseconds(100));

            appHost.GetContainer().Register(c => _heartBeatClient);
        }

        public void AfterPluginsLoaded(IAppHost appHost)
        {
            appHost.TryResolve<HeartBeatClient>().Start();
            
            appHost.TryResolve<IBus>().Subscribe<HeartBeat>("alarm",
                h => { appHost.TryResolve<AlarmService>().Play(); }, c => c.WithTopic("#.ms.0.#"));
        }
    }
}