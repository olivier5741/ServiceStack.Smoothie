using System;
using System.Collections.Generic;
using System.Linq;
using EasyNetQ;
using ServiceStack.Redis;
using ServiceStack.Smoothie.Test.Interfaces;
using ServiceStack.Text;

namespace ServiceStack.Smoothie.Test.HeartBeats
{
    public class HeartBeatClient : IDisposable
    {
        private readonly IBus _bus;
        private readonly IRedisClientsManager _redisClientsManager;
        private readonly System.Timers.Timer _timer;
        private TimeSpan _interval;

        public HeartBeatClient(IBus bus, IRedisClientsManager redisClientsManager, TimeSpan interval)
        {
            _interval = interval;
            _bus = bus;
            _redisClientsManager = redisClientsManager;
            _timer = new System.Timers.Timer(_interval.Milliseconds);

            // publish an unprecise heartbeat every x time
            _timer.Elapsed += (sender, args) =>
            {
                // might be better to get it from dependency injection ...
                bus.Publish(new HeartBeatUnprecise {Time = args.SignalTime, Interval = _interval});
            };
        }

        public void Start()
        {
            _timer.Enabled = true;
            
            // publish exactly one heartbeat based on the unprecise once (use Redis for duplicate check)
            _bus.Subscribe<HeartBeatUnprecise>("peacemaker", h =>
            {
                using (var redis = _redisClientsManager.GetClient())
                {
                    var rounded = new DateTime((long) Math.Floor((double) h.Time.Ticks / _interval.Ticks) *
                                               _interval.Ticks);

                    var value = rounded.ToString("O");
                    var isNotPresent = redis.AddItemToSortedSet("test:peacemaker", value);

                    if (isNotPresent == false)
                        return;
                    
                    _bus.Publish(new HeartBeat
                    {
                        Time = rounded,
                        Interval = _interval
                    },rounded.Topic());
                }
            });

            // check missing heartbeats based on heartbeats stored in Redis
            // publish unprecise heartbeats based on those missing
            _bus.Subscribe<HeartBeatUnprecise>("missing-beats", h =>
            {
                using (var redis = _redisClientsManager.GetClient())
                {
                    var from = h.Time.AddMinutes(2); // why is from higher than to ??
                    var to = h.Time.AddMinutes(1);
                    
                    var list = redis.GetRangeFromSortedSetByHighestScore("test:peacemaker",from.ToString("O"),
                        to.ToString("O"));

                    var expected = new List<string>();

                    for (var d = from.CreateCopy(); d < to.CreateCopy(); d = d.Add(_interval))
                    {
                        expected.Add(d.ToString("O"));
                    }

                    var missing = list.Except(list);
                    
                    foreach (var s in missing)
                    {
                        _bus.Publish(new HeartBeatUnprecise {Time = DateTime.Parse(s), Interval = _interval});
                    }
                }
            });

            // test : to remove later
            _bus.Subscribe<HeartBeat>("peacemaker", h =>
            {
                h.PrintDump();
            }, cfg => cfg.WithTopic("#.ms.500.#").WithTopic("#.ms.0.#"));
        }

        

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}