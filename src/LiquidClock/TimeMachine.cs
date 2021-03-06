﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LiquidClock
{
    /// <summary>
    /// <see cref="TimeMachine"/> allows you to schedule completions and result of <see cref="Task"/>s then
    /// advance time using an <see cref="Advancer"/> to complete the scheduled <see cref="Task"/>s in order.
    /// </summary>
    public sealed class TimeMachine
    {
        private readonly SortedDictionary<int, Action> actions = new SortedDictionary<int, Action>();

        /// <summary>
        /// Creates a <see cref="Task" /> that will complete successfuly when the <see cref="TimeMachine" /> is advanced to the
        /// given <paramref name="time" />.
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public Task ScheduleSuccess(int time) =>
            this.AddAction<bool>(time, tcs => tcs.SetResult(true));

        /// <summary>
        /// Creates a <see cref="Task" /> of <typeparamref name="T" /> that will complete successfuly when the
        /// <see cref="TimeMachine" /> is advanced to the given <paramref name="time" />.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="time"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public Task<T> ScheduleSuccess<T>(int time, T value) =>
            this.AddAction<T>(time, tcs => tcs.SetResult(value));

        /// <summary>
        /// Creates a <see cref="Task" /> that will await on <paramref name="func" /> then return a completed task when the
        /// <see cref="TimeMachine" /> is advanced to the given <paramref name="time" />.
        /// </summary>
        /// <param name="time"></param>
        /// <param name="valueProducer">The function to call to get the result. It will be called when the <see cref="TimeMachine" /> is advanced to the given <paramref name="time" />. There is no error handling on this, make sure it succeeds</param>
        /// <returns></returns>
        public Task ScheduleSuccess(int time, Func<Task> valueProducer)
        {
            return this.AddAction<bool>(time, async tcs =>
            {
                await valueProducer();
                tcs.SetResult(true);
            });
        }

        /// <summary>
        /// Creates a <see cref="Task" /> of <typeparamref name="T" /> that will await for the result of
        /// <paramref name="valueProducer" /> and put it in a completed task when the <see cref="TimeMachine" /> is advanced to
        /// the given <paramref name="time" />.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="time"></param>
        /// <param name="valueProducer">The function to call to get the result. It will be called when the <see cref="TimeMachine" /> is advanced to the given <paramref name="time" />. There is no error handling on this, make sure it succeeds</param>
        /// <returns></returns>
        public Task<T> ScheduleSuccess<T>(int time, Func<Task<T>> valueProducer) =>
            this.AddAction<T>(time, async tcs => tcs.SetResult(await valueProducer()));

        /// <summary>
        /// Creates a <see cref="Task" /> that will fault when the <see cref="TimeMachine" /> is
        /// advanced to the given <paramref name="time" />.
        /// </summary>
        /// <param name="time"></param>
        /// <param name="exceptions"></param>
        /// <returns></returns>
        public Task ScheduleFault(int time, params Exception[] exceptions) =>
            this.AddAction<bool>(time, tcs => tcs.SetException(exceptions));

        /// <summary>
        /// Creates a <see cref="Task" /> of <typeparamref name="T" /> that will fault when the <see cref="TimeMachine" /> is
        /// advanced to the given <paramref name="time" />.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="time"></param>
        /// <param name="exceptions"></param>
        /// <returns></returns>
        public Task<T> ScheduleFault<T>(int time, params Exception[] exceptions) =>
            this.AddAction<T>(time, tcs => tcs.SetException(exceptions));

        /// <summary>
        /// Creates a <see cref="Task" /> that will be maked as cancelled when the
        /// <see cref="TimeMachine" /> is advanced to the given <paramref name="time" />.
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public Task ScheduleCancellation(int time) =>
            this.AddAction<bool>(time, tcs => tcs.SetCanceled());

        /// <summary>
        /// Creates a <see cref="Task" /> of <typeparamref name="T" /> that will be maked as cancelled when the
        /// <see cref="TimeMachine" /> is advanced to the given <paramref name="time" />.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="time"></param>
        /// <returns></returns>
        public Task<T> ScheduleCancellation<T>(int time) =>
            this.AddAction<T>(time, tcs => tcs.SetCanceled());

        private Task<T> AddAction<T>(int time, Action<TaskCompletionSource<T>> action)
        {
            if (time <= 0)
                throw new ArgumentOutOfRangeException(nameof(time), "Tasks can only be scheduled with a positive time");

            if (actions.ContainsKey(time))
                throw new ArgumentException("A task completing at this time has already been scheduled.", nameof(time));

            var source = new TaskCompletionSource<T>();
            actions[time] = () => action(source);

            return source.Task;
        }

        /// <summary>
        /// Execute the given action in the <see cref="TimeMachine" />. Use the given <see cref="Advancer" /> to advance time
        /// and observe the tasks being completed/faulted/cancelled.
        /// </summary>
        /// <param name="action"></param>
        public void ExecuteInContext(Action<Advancer> action)
        {
            var temporaryContext = new ManuallyPumpedSynchronizationContext();

            var originalContext = SynchronizationContext.Current;

            try
            {
                SynchronizationContext.SetSynchronizationContext(temporaryContext);
                var advancer = new Advancer(actions, temporaryContext);
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