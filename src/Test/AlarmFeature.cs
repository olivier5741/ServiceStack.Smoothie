using ServiceStack.Data;
using ServiceStack.OrmLite;
using ServiceStack.Validation;

namespace ServiceStack.Smoothie.Test
{
    public class AlarmFeature : IPlugin
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
        }
    }
}