using System;
using EasyNetQ;
using Funq;
using ServiceStack.Api.OpenApi;
using ServiceStack.Data;
using ServiceStack.OrmLite;
using ServiceStack.Smoothie.Test;
using ServiceStack.Text;

namespace ServiceStack.Smoothie.Api.Test
{
    public class AppHost : AppHostBase
    {
        // Initializes your AppHost Instance, with the Service Name and assembly containing the Services
        public AppHost() : base("ServiceStack.Smoothie.Api.Test", typeof(AlarmService).GetAssembly())
        {
            JsConfig.EmitCamelCaseNames = true;
            JsConfig.DateHandler = DateHandler.ISO8601;
            JsConfig.TimeSpanHandler = TimeSpanHandler.DurationFormat;
        }

        // Configure your AppHost with the necessary configuration and dependencies your App needs
        public override void Configure(Container container)
        {
            container.Register<IDbConnectionFactory>(
                new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider));

            var bus = RabbitHutch.CreateBus("host=localhost");
            container.Register(bus);
            
            Plugins.Add(new OpenApiFeature());
            //Register Redis Client Manager singleton in ServiceStack's built-in Func IOC
            //container.Register<IRedisClientsManager>(new BasicRedisClientManager("localhost"));

            using (var sess = container.Resolve<IDbConnectionFactory>().Open())
            {
                sess.CreateTable<SmoothApp>();
                sess.CreateTable<Smooth>();
                
                var app = new SmoothApp
                {
                    Id = new Guid("d8927a9c-7512-4b1b-9ed7-c6d2bdd68e60"),
                    Limit = new SmoothLimitPerHour
                    {
                        Amount = 5
                    }
                };

                sess.Save(app);
            }
        }
    }
}