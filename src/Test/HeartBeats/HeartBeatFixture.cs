using System;
using System.Threading;
using EasyNetQ;
using NUnit.Framework;
using ServiceStack.Redis;
using ServiceStack.Text;
using ServiceStack.Smoothie.Test.Interfaces;

namespace ServiceStack.Smoothie.Test.HeartBeats
{
    [TestFixture]
    public class HeartBeatFixture
    {
        private HeartBeatClient _heartbeatSvc;
        private IBus _bus;
        private RedisManagerPool _redisClientsManager;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            JsConfig.DateHandler = DateHandler.ISO8601;
            _bus = RabbitHutch.CreateBus("host=localhost");
            _redisClientsManager = new RedisManagerPool("localhost");
            _heartbeatSvc = new HeartBeatClient(_bus, _redisClientsManager);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            _heartbeatSvc?.Dispose();
            _bus?.Dispose();
            _redisClientsManager.Dispose();
        }

        [Test]
        public void Morethan15HeartbeatsIn2Seconds()
        {
            var counter = 0;
            _bus.Subscribe<HeartBeat>("test", a => counter++);
            
            _heartbeatSvc.Start();
            Thread.Sleep(TimeSpan.FromSeconds(2));
            Assert.GreaterOrEqual(counter,15);
        }
    }
}