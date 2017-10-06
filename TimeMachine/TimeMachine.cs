using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TimeMachine
{
    public sealed partial class TimeMachine
    {
        private readonly SortedDictionary<int, Action> actions = new SortedDictionary<int, Action>();

        public Task<T> ScheduleSuccess<T>(int time, T value) => AddAction<T>(time, tcs => tcs.SetResult(value));

        public Task<T> ScheduleFault<T>(int time, Exception exception) => AddAction<T>(time, tcs => tcs.SetException(exception));

        public Task<T> ScheduleFault<T>(int time, IEnumerable<Exception> exceptions) => AddAction<T>(time, tcs => tcs.SetException(exceptions));

        public Task<T> ScheduleCancellation<T>(int time) => AddAction<T>(time, tcs => tcs.SetCanceled());

        private Task<T> AddAction<T>(int time, Action<TaskCompletionSource<T>> action)
        {
            if (time <= 0)
                throw new ArgumentOutOfRangeException(nameof(time), "Tasks can only be scheduled with a positive time");

            if (actions.ContainsKey(time))
                throw new ArgumentException("A task completing at this time has already been scheduled.", nameof(time));

            TaskCompletionSource<T> source = new TaskCompletionSource<T>();
            actions[time] = () => action(source);

            return source.Task;
        }

        public void ExecuteInContext(Action<Advancer> action) =>
            ExecuteInContext(new ManuallyPumpedSynchronizationContext(), action);

        public void ExecuteInContext(ManuallyPumpedSynchronizationContext context, Action<Advancer> action)
        {
            SynchronizationContext originalContext = SynchronizationContext.Current;

            try
            {
                SynchronizationContext.SetSynchronizationContext(context);
                Advancer advancer = new Advancer(actions, context);
                // This is where the tests assertions etc will go...
                action(advancer);
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(originalContext);
            }
        }
    }
}
