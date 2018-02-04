using System;
using System.Threading.Tasks;
using Moq;
using Xunit;

namespace LiquidClock.Tests
{
    public class TimeMachineTest
    {
        [Theory]
        [InlineData(0)]
        [InlineData(-2)]
        public async Task Schedule_ThrowsIfTimeIsZeroOrLess(int invalidTime)
        {
            // Fixture setup
            var sut = new TimeMachine();
            // Exercise system & Verify outcome
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => sut.ScheduleSuccess(invalidTime, "Hello"));
            // Teardown 
        }

        [Fact]
        public void AdvanceToEnd_FinishesAllScheduledTasks()
        {
            // Fixture setup
            var sut = new TimeMachine();
            var firstTask = sut.ScheduleSuccess(1, "Foo");
            var lastTask = sut.ScheduleSuccess(99, "Bar");
            // Exercise system & Verify outcome
            sut.ExecuteInContext(advancer =>
            {
                Assert.False(firstTask.IsCompleted);
                Assert.False(lastTask.IsCompleted);
                advancer.AdvanceToEnd();
                Assert.True(firstTask.IsCompleted);
                Assert.True(lastTask.IsCompleted);
            });
            // Teardown 
        }

        [Fact]
        public async Task Schedule_ThrowsWhenSchedulingTwiceAtTheSameTime()
        {
            // Fixture setup
            var sut = new TimeMachine();
            var _ = sut.ScheduleCancellation<string>(1);
            // Exercise system & Verify outcome
            await Assert.ThrowsAsync<ArgumentException>(() => sut.ScheduleFault<string>(1, new Exception()));
            // Teardown 
        }

        [Fact]
        public void ScheduleCancellation()
        {
            // Fixture setup
            var sut = new TimeMachine();
            var result = sut.ScheduleCancellation<string>(1);
            var mock = new Mock<ITestEngine>();
            mock.Setup(s => s.ReturnsString()).Returns(result);
            var subject = mock.Object;
            // Exercise system & Verify outcome
            sut.ExecuteInContext(advancer =>
            {
                var task = subject.ReturnsString();
                Assert.False(task.IsCompleted);
                advancer.Advance();
                Assert.True(task.IsCanceled);
            });
            // Teardown 
        }

        [Fact]
        public void ScheduleFault_SingleException()
        {
            // Fixture setup
            var ex = new Exception();
            var sut = new TimeMachine();
            var result = sut.ScheduleFault<string>(1, ex);
            var mock = new Mock<ITestEngine>();
            mock.Setup(s => s.ReturnsString()).Returns(result);
            var subject = mock.Object;
            // Exercise system & Verify outcome
            sut.ExecuteInContext(advancer =>
            {
                var task = subject.ReturnsString();
                Assert.False(task.IsCompleted);
                advancer.Advance();
                Assert.True(task.IsFaulted);
                Assert.Same(ex, task.Exception.InnerException);
            });
            // Teardown 
        }

        [Fact]
        public void ScheduleFault_MultipleExceptions()
        {
            // Fixture setup
            var ex1 = new Exception();
            var ex2 = new Exception();
            var sut = new TimeMachine();
            var result = sut.ScheduleFault<string>(1, ex1, ex2 );
            var mock = new Mock<ITestEngine>();
            mock.Setup(s => s.ReturnsString()).Returns(result);
            var subject = mock.Object;
            // Exercise system & Verify outcome
            sut.ExecuteInContext(advancer =>
            {
                var task = subject.ReturnsString();
                Assert.False(task.IsCompleted);
                advancer.Advance();
                Assert.True(task.IsFaulted);
                Assert.Same(ex1, task.Exception.InnerExceptions[0]);
                Assert.Same(ex2, task.Exception.InnerExceptions[1]);
            });
            // Teardown 
        }

        [Fact]
        public void ScheduleSuccess()
        {
            // Fixture setup
            var sut = new TimeMachine();
            var result = sut.ScheduleSuccess(1, "Hello");
            var mock = new Mock<ITestEngine>();
            mock.Setup(s => s.ReturnsString()).Returns(result);
            var subject = mock.Object;
            // Exercise system & Verify outcome
            sut.ExecuteInContext(advancer =>
            {
                var task = subject.ReturnsString();
                Assert.False(task.IsCompleted);
                advancer.Advance();
                Assert.True(task.IsCompletedSuccessfully);
                Assert.Equal("Hello", task.Result);
            });
            // Teardown 
        }

        [Fact]
        public void ScheduleSuccess_NonGeneric()
        {
            // Fixture setup
            var sut = new TimeMachine();
            var result = sut.ScheduleSuccess(1);
            var mock = new Mock<ITestEngine>();
            mock.Setup(s => s.DoSomething()).Returns(result);
            var subject = mock.Object;
            // Exercise system & Verify outcome
            sut.ExecuteInContext(advancer =>
            {
                var task = subject.DoSomething();
                Assert.False(task.IsCompleted);
                advancer.Advance();
                Assert.True(task.IsCompletedSuccessfully);
            });
            // Teardown 
        }

        [Fact]
        public void ScheduleFault_NonGeneric()
        {
            // Fixture setup
            var sut = new TimeMachine();
            var ex = new Exception();
            var result = sut.ScheduleFault(1, ex);
            var mock = new Mock<ITestEngine>();
            mock.Setup(s => s.DoSomething()).Returns(result);
            var subject = mock.Object;
            // Exercise system & Verify outcome
            sut.ExecuteInContext(advancer =>
            {
                var task = subject.DoSomething();
                Assert.False(task.IsCompleted);
                advancer.Advance();
                Assert.True(task.IsCompleted);
                Assert.True(task.IsFaulted);
                Assert.Same(ex, task.Exception.InnerException);
            });
            // Teardown 
        }

        [Fact]
        public void ScheduleCancellation_NonGeneric()
        {
            // Fixture setup
            var sut = new TimeMachine();
            var result = sut.ScheduleCancellation(1);
            var mock = new Mock<ITestEngine>();
            mock.Setup(s => s.DoSomething()).Returns(result);
            var subject = mock.Object;
            // Exercise system & Verify outcome
            sut.ExecuteInContext(advancer =>
            {
                var task = subject.DoSomething();
                Assert.False(task.IsCompleted);
                advancer.Advance();
                Assert.True(task.IsCompleted);
                Assert.True(task.IsCanceled);
            });
            // Teardown 
        }

        [Fact]
        public void ScheduleSuccess_NonGeneric_WithFunc_FuncOnlyExecutedAtGivenTime()
        {
            // Fixture setup
            var endObject = new EndObject();
            var sut = new TimeMachine();
            var result = sut.ScheduleSuccess(1, () => endObject.DoSomething());
            var mock = new Mock<ITestEngine>();
            mock.Setup(s => s.DoSomething()).Returns(result);
            var subject = mock.Object;
            // Exercise system & Verify outcome
            sut.ExecuteInContext(advancer =>
            {
                var task = subject.DoSomething();
                Assert.False(task.IsCompleted);
                Assert.False(endObject.WasCalled);
                advancer.Advance();
                Assert.True(task.IsCompletedSuccessfully);
                Assert.True(endObject.WasCalled);
            });
            // Teardown 
        }

        [Fact]
        public void ScheduleSuccess_WithFunc_FuncOnlyExecutedAtGivenTime()
        {
            // Fixture setup
            var endObject = new EndObject();
            var sut = new TimeMachine();
            var result = sut.ScheduleSuccess(1, () => endObject.GetString());
            var mock = new Mock<ITestEngine>();
            mock.Setup(s => s.ReturnsString()).Returns(result);
            var subject = mock.Object;
            // Exercise system & Verify outcome
            sut.ExecuteInContext(advancer =>
            {
                var task = subject.ReturnsString();
                Assert.False(task.IsCompleted);
                Assert.False(endObject.WasCalled);
                advancer.Advance();
                Assert.True(task.IsCompletedSuccessfully);
                Assert.True(endObject.WasCalled);
                Assert.Equal("Who you're gonna call?", task.Result);
            });
            // Teardown 
        }
    }

    public interface ITestEngine
    {
        Task<string> ReturnsString();
        Task DoSomething();
    }

    public class EndObject
    {
        public bool WasCalled { get; private set; }

        public Task<string> GetString()
        {
            WasCalled = true;
            return Task.FromResult("Who you're gonna call?");
        }

        public Task DoSomething()
        {
            WasCalled = true;
            return Task.CompletedTask;
        }
    }
}