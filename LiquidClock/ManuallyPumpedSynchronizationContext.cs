using System;
using System.Collections.Concurrent;
using System.Threading;

namespace LiquidClock
{
        public sealed class ManuallyPumpedSynchronizationContext : SynchronizationContext
        {
            private readonly BlockingCollection<Tuple<SendOrPostCallback, object>> callbacks = 
                new BlockingCollection<Tuple<SendOrPostCallback, object>>();

            public override void Post(SendOrPostCallback callback, object state) =>
                callbacks.Add(Tuple.Create(callback, state));

            public override void Send(SendOrPostCallback d, object state) =>
                throw new NotSupportedException($"Synchronous operations not supported on {nameof(ManuallyPumpedSynchronizationContext)}");

            public void PumpAll()
            {
                Tuple<SendOrPostCallback, object> callback;
                while (callbacks.TryTake(out callback))
                {
                    callback.Item1(callback.Item2);
                }
            }
        }
}
