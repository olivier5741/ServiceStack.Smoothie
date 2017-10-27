using ServiceStack.Data;
using ServiceStack.OrmLite;
using ServiceStack.Validation;

namespace ServiceStack.Smoothie.Test
{
    public class SmoothFeature : IPlugin
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
    }
}