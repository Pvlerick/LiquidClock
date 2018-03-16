using System;
using System.Collections.Generic;
using System.Linq;

namespace LiquidClock
{
    /// <summary>
    /// This class allows advancing the time thus completing tasks that were scheduled in the <see cref="TimeMachine"/>
    /// </summary>
    public sealed class Advancer
    {
        private readonly SortedDictionary<int, Action> actions;
        private readonly ManuallyPumpedSynchronizationContext context;

        internal Advancer(SortedDictionary<int, Action> actions, ManuallyPumpedSynchronizationContext context)
        {
            this.actions = actions ?? throw new ArgumentNullException(nameof(actions));
            this.context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public int Time { get; private set; }

        /// <summary>
        /// Advances to the given target time.
        /// </summary>
        /// <param name="targetTime"></param>
        public void AdvanceTo(int targetTime)
        {
            if (targetTime <= Time)
                throw new ArgumentOutOfRangeException(nameof(targetTime), "Can only advance time forwards");

            var timesToRemove = new List<int>();

            foreach (var entry in actions.TakeWhile(e => e.Key <= targetTime))
            {
                timesToRemove.Add(entry.Key);
                entry.Value();
                context.PumpAll();
            }

            foreach (var key in timesToRemove)
                actions.Remove(key);

            Time = targetTime;
        }

        /// <summary>
        /// Advances the clock by the given number of arbitrary time units
        /// </summary>
        /// <param name="amount"></param>
        public void AdvanceBy(int amount)
        {
            if (amount <= 0)
                throw new ArgumentOutOfRangeException(nameof(amount), "Can only advance time forwards");

            AdvanceTo(Time + amount);
        }

        /// <summary>
        /// Advances the clock by one time unit.
        /// </summary>
        public void Advance() => AdvanceBy(1);

        /// <summary>
        /// Advances the clock past the last scheduled task so that they are all completed.
        /// </summary>
        public void AdvanceToEnd() => AdvanceTo(actions.Last().Key);
    }
}