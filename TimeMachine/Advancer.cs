﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace TimeMachine
{
    public sealed partial class TimeMachine
    {
        public class Advancer
        {
            private readonly SortedDictionary<int, Action> actions;
            private readonly ManuallyPumpedSynchronizationContext context;

            public int Time { get; private set; } = 0;

            internal Advancer(SortedDictionary<int, Action> actions, ManuallyPumpedSynchronizationContext context)
            {
                this.actions = actions ?? throw new ArgumentNullException(nameof(actions));
                this.context = context ?? throw new ArgumentNullException(nameof(context));
            }

            /// <summary>
            /// Advances to the given target time.
            /// </summary>
            /// <param name="targetTime"></param>
            public void AdvanceTo(int targetTime)
            {
                if (targetTime <= this.Time)
                    throw new ArgumentOutOfRangeException(nameof(targetTime), "Can only advance time forwards");

                List<int> timesToRemove = new List<int>();

                foreach (var entry in actions.TakeWhile(e => e.Key <= targetTime))
                {
                    timesToRemove.Add(entry.Key);
                    entry.Value();
                    context.PumpAll();
                }

                foreach (int key in timesToRemove)
                {
                    actions.Remove(key);
                }

                this.Time = targetTime;
            }

            /// <summary>
            /// Advances the clock by the given number of arbitrary time units
            /// </summary>
            /// <param name="amount"></param>
            public void AdvanceBy(int amount)
            {
                if (amount <= 0)
                    throw new ArgumentOutOfRangeException(nameof(amount), "Can only advance time forwards");

                AdvanceTo(this.Time + amount);
            }

            /// <summary>
            /// Advances the clock by one time unit.
            /// </summary>
            public void Advance()
            {
                AdvanceBy(1);
            }
        }
    }
}
