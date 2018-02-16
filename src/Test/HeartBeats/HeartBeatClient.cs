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
        private System.Timers.Timer _timer;

        public TimeSpan Interval { get; set; } = TimeSpan.FromMilliseconds(100);


        public HeartBeatClient(IBus bus, IRedisClientsManager redisClientsManager)
        {
            _bus = bus;
            _redisClientsManager = redisClientsManager;
            
            SetUpTimerForUnpreciseBeat();
        }

        // publish an unprecise heartbeat every x time
        private void SetUpTimerForUnpreciseBeat()
        {
            _timer = new System.Timers.Timer(Interval.Milliseconds);

            _timer.Elapsed += (sender, args) =>
            {
                _bus.Publish(new HeartBeatUnprecise {Time = args.SignalTime, Interval = Interval},
                    args.SignalTime.Topic());
            };
        }

        public void Start()
        {
            _timer.Enabled = true;

            _bus.Subscribe<HeartBeatUnprecise>("peacemaker", PublishBeat);

            // should be based on unprecise perhaps, yes better but did not work lately
            _bus.Subscribe<HeartBeat>("missing-beats", HandleMissingBeats,
                cfg => cfg.WithTopic("#.ms.500.#").WithTopic("#.ms.0.#"));
        }

        // publish exactly one heartbeat based on the unprecise once (use Redis for duplicate check)
        private void PublishBeat(HeartBeatUnprecise h)
        {
            var rounded = RoundTime(h);
            var isNotPresent = AddBeatToRedis(rounded);

            if (isNotPresent == false)
                return;

            _bus.Publish(new HeartBeat
            {
                Time = rounded,
                Interval = Interval
            }, rounded.Topic());
        }

        private bool AddBeatToRedis(DateTime rounded)
        {
            using (var redis = _redisClientsManager.GetClient())
                return redis.AddItemToSortedSet("test:peacemaker", rounded.ToString("O"));
        }

        private DateTime RoundTime(HeartBeatUnprecise h)
        {
            return new DateTime((long) Math.Floor((double) h.Time.Ticks / Interval.Ticks) * Interval.Ticks);
        }

        // publish unprecise beats based on missing beats
        private void HandleMissingBeats(HeartBeat h)
        {
            var from = h.Time.AddMinutes(-2);
            var to = h.Time.AddMinutes(-1);

            var missingBeats = ExpectedBeats(@from, to).Except(PublishedBeats(@from, to));

            foreach (var s in missingBeats)
                _bus.Publish(new HeartBeatUnprecise {Time = DateTime.Parse(s), Interval = Interval});
        }

        // check missing beats based on beats stored in Redis
        private IEnumerable<string> PublishedBeats(DateTime @from, DateTime to)
        {
            using (var redis = _redisClientsManager.GetClient())
                return redis.GetRangeFromSortedSetByHighestScore("test:peacemaker", @from.ToString("O"),
                    to.ToString("O"));
        }

        private IEnumerable<string> ExpectedBeats(DateTime @from, DateTime to)
        {
            var expectedBeats = new List<string>();

            for (var d = @from.CreateCopy(); d < to.CreateCopy(); d = d.Add(Interval))
                expectedBeats.Add(d.ToString("O"));

            return expectedBeats;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}