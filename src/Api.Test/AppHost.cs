using System;
using System.Collections.Generic;
using EasyNetQ;
using Funq;
using ServiceStack.Api.OpenApi;
using ServiceStack.Data;
using ServiceStack.OrmLite;
using ServiceStack.Redis;
using ServiceStack.Smoothie.Test;
using ServiceStack.Smoothie.Test.Alarms;
using ServiceStack.Smoothie.Test.Interfaces;
using ServiceStack.Smoothie.Test.Smooths;
using ServiceStack.Text;
using ServiceStack.Validation;

namespace ServiceStack.Smoothie.Api.Test
{
    public class DummyService : IService
    {
        
    }
    
    public class AppHost : AppHostBase
    {
        // Initializes your AppHost Instance, with the Service Name and assembly containing the Services
        public AppHost() : base("ServiceStack.Smoothie.Api.Test", typeof(DummyService).Assembly)
        {
            JsConfig.EmitCamelCaseNames = true;
            JsConfig.DateHandler = DateHandler.ISO8601;
            JsConfig.TimeSpanHandler = TimeSpanHandler.DurationFormat;
        }

        // Configure your AppHost with the necessary configuration and dependencies your App needs
        public override void Configure(Container container)
        {
            var redisClientsManager = new RedisManagerPool("localhost");
            container.Register<IRedisClientsManager>(c => redisClientsManager);
            
            container.Register<IDbConnectionFactory>(
                new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider));

            var bus = RabbitHutch.CreateBus("host=localhost");
            container.Register(bus);

            bus.Subscribe<Alarm>("api", a =>
            {
                a.PrintDump();
            });
            
            bus.Subscribe<Smooth>("api", a =>
            {
                a.PrintDump();
            });
            
            Plugins.Add(new ValidationFeature());
            container.RegisterValidators(typeof(SmoothValidator).Assembly);
            
            Plugins.Add(new OpenApiFeature());
            Plugins.Add(new AlarmFeature());
            Plugins.Add(new SmoothFeature());
            
            AfterInitCallbacks = new List<Action<IAppHost>>
            {
                apphost =>
                {
                    using (var sess = container.TryResolve<IDbConnectionFactory>().Open())
                    {
                        var app0 = new AlarmApp
                        {
                            Id = new Guid("139ab7b8ab49417185fe8a2c7ff37042"),
                            TenantId = new Guid("bd43f135-eb3b-4006-b176-ec7c6f58f12d")
                        };

                        sess.Save(app0);
                
                        sess.CreateTable<SmoothApp>();
                        sess.CreateTable<Smooth>();
                
                        var app1 = new SmoothApp
                        {
                            Id = new Guid("d8927a9c-7512-4b1b-9ed7-c6d2bdd68e60"),
                            TenantId = new Guid("bd43f135-eb3b-4006-b176-ec7c6f58f12d"),
                            Limit = new SmoothLimitPerHour
                            {
                                Amount = 5
                            }
                        };
                
                        var app2 = new SmoothApp
                        {
                            Id = new Guid("6f11eb45-a35d-4f5d-ac17-96365acf9c9d"),
                            TenantId = new Guid("67be595b-4686-43e1-b54e-8bba209b5de7"),
                            Limit = new SmoothLimitPerHour
                            {
                                Amount = 2
                            }
                        };

                        sess.SaveAll(new []{app1, app2});
                    }
                }
            };
            
            
            
            //Register Redis Client Manager singleton in ServiceStack's built-in Func IOC
            //container.Register<IRedisClientsManager>(new BasicRedisClientManager("localhost"));

            
        }
    }
}