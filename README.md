# ServiceStack.Smoothie

![appveyor status](https://ci.appveyor.com/api/projects/status/github/olivier5741/servicestack.smoothie?branch=master&svg=true)

Event-based timer and message smoother

## HeartBeat

A resilient (high available and partitionned) pacemaker. 
In code, search for `HeartBeatFixture` and `HeartBeatClient`.

### Unprecise heartbeat

For a given interval (let say every 100ms), all service instances publish a (unprecise) heartbeat event on the bus (rabbitmq). You can subscribe to a specific tempo through rabbit topic subscription. 

This heart is called `HeartBeatUnprecise` since it is based on the programming language time, lots of duplicates (all instance publishing) and there might be some missing when something goes wrong.

### Heartbeat

To go beyond `HeartBeatUnprecise` limitations, you can subscribe to `HeartBeat`. Works the same way but the service instances synchronise to not publish duplicates (don't forget that rabbitmq *at most once* topology) and overcome missing heartbeats. The system achieve this by internally subscribing to `HeartBeat` and synchronise to redis.


## Alarm

You can set alarm. You will be notified when it's expired. Or you can cancel it before it expires.

On every heartbeat (from the previous service), the system queries the alarms that are expired. Publish the `AlarmExpired` event (instances synchronisation to prevent duplicates). 

## Smooth