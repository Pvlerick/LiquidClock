using System;
using Xunit;

namespace LiquidClock.Tests
{
    public class TimeMachineTest
    {
        [Fact]
        public void ScheduleSuccess_TaskIsNotCompletedBeforeGivenTime()
        {
            // Fixture setup
            var sut = new TimeMachine();
            // Exercise system
            var scheduledSuccess = sut.ScheduleSuccess(1, "Bar");
            // Verify outcome
            sut.ExecuteInContext(advancer =>
            {
                Assert.False(scheduledSuccess.IsCompleted);
                advancer.Advance();
                Assert.True(scheduledSuccess.IsCompleted);
                Assert.False(scheduledSuccess.IsFaulted);
                Assert.False(scheduledSuccess.IsCanceled);
                Assert.Equal("Bar", scheduledSuccess.Result);
            });
            // Teardown 
        }

        [Fact]
        public void ScheduleFault_TaskIsNotFaultedBeforeGivenTime()
        {
            // Fixture setup
            var ex = new Exception();
            var sut = new TimeMachine();
            // Exercise system
            var scheduledFault = sut.ScheduleFault<string>(1, ex);
            // Verify outcome
            sut.ExecuteInContext(advancer =>
            {
                Assert.False(scheduledFault.IsCompleted);
                advancer.Advance();
                Assert.True(scheduledFault.IsCompleted);
                Assert.True(scheduledFault.IsFaulted);
                Assert.Equal(scheduledFault.Exception.InnerExceptions[0], ex);
            });
            // Teardown 
        }

        [Fact]
        public void ScheduleFault_MultipleExceptions_TaskIsNotFaultedBeforeGivenTime()
        {
            // Fixture setup
            var ex1 = new Exception();
            var ex2 = new Exception();
            var sut = new TimeMachine();
            // Exercise system
            var scheduledFault = sut.ScheduleFault<string>(1, new[] { ex1, ex2 });
            // Verify outcome
            sut.ExecuteInContext(advancer =>
            {
                Assert.False(scheduledFault.IsCompleted);
                advancer.Advance();
                Assert.True(scheduledFault.IsCompleted);
                Assert.True(scheduledFault.IsFaulted);
                Assert.Equal(scheduledFault.Exception.InnerExceptions[0], ex1);
                Assert.Equal(scheduledFault.Exception.InnerExceptions[1], ex2);
            });
            // Teardown 
        }

        [Fact]
        public void ScheduleCancellation_TaskIsNotCancelledBeforeGivenTime()
        {
            // Fixture setup
            var sut = new TimeMachine();
            // Exercise system
            var scheduledSuccess = sut.ScheduleCancellation<string>(1);
            // Verify outcome
            sut.ExecuteInContext(advancer =>
            {
                Assert.False(scheduledSuccess.IsCompleted);
                advancer.Advance();
                Assert.True(scheduledSuccess.IsCompleted);
                Assert.True(scheduledSuccess.IsCanceled);
            });
            // Teardown 
        }
    }
}
