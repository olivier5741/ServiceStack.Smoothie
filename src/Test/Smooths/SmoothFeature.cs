using System;
using EasyNetQ;
using ServiceStack.Data;
using ServiceStack.OrmLite;
using ServiceStack.Smoothie.Test.Interfaces;
using ServiceStack.Validation;

namespace ServiceStack.Smoothie.Test.Smooths
{
    public class SmoothFeature : IPlugin, IPostInitPlugin
    {
        public void Register(IAppHost appHost)
        {
            appHost.RegisterService<SmoothService>();
            appHost.GetContainer().RegisterValidators(typeof(SmoothValidator).Assembly);

            using (var db = appHost.TryResolve<IDbConnectionFactory>().Open())
            {
                db.CreateTableIfNotExists<SmoothApp>();
                db.CreateTableIfNotExists<Smooth>();
            }
        }

        public void AfterPluginsLoaded(IAppHost appHost)
        {
            appHost.TryResolve<IBus>().Subscribe<HeartBeat>("smooth",
                h =>
                {
                    appHost.TryResolve<SmoothService>().Play(h);
                },
                // every 10 seconds
                c => c.WithTopic("#.s.0.ms.0.#").WithTopic("#.s.10.ms.0.#").WithTopic("#.s.20.ms.0.#")
                    .WithTopic("#.s.30.ms.0.#").WithTopic("#.s.40.ms.0.#").WithTopic("#.s.50.ms.0.#"));
        }
    }
}