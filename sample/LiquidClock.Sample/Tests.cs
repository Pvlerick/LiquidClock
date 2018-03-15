using Moq;
using System.Threading.Tasks;
using Xunit;

namespace LiquidClock.Sample
{
    public class Tests
    {
        [Fact]
        public void SingleTask()
        {
            var timeMachine = new TimeMachine();

            //Creates a Task<string> that will complete and have "Foo" as a result at time 1
            Task<string> task = timeMachine.ScheduleSuccess(1, "Foo");

            timeMachine.ExecuteInContext(advancer =>
            {
                //Task is not completed
                Assert.False(task.IsCompleted);
                //Advance the time by one unit
                advancer.Advance();
                //Taks is now completed (sucessfuly)
                Assert.True(task.IsCompleted);
                //And its result is "Foo"
                Assert.Equal("Foo", task.Result);
            });
        }

        [Fact]
        public void MultipleTasks()
        {
            var timeMachine = new TimeMachine();
            var mock = new Mock<IService>();
            //First read will return a task that will complete at time 2 and return "Foo"
            mock.Setup(s => s.Read()).Returns(timeMachine.ScheduleSuccess(2, "Foo"));
            //Second read will return a task that will complete at time 1 and return "Bar"
            mock.Setup(s => s.Read()).Returns(timeMachine.ScheduleSuccess(1, "Bar"));
            var service = mock.Object;
            timeMachine.ExecuteInContext(advancer =>
            {
                var task1 = service.Read();
                Assert.False(task1.IsCompleted);
                var task2 = service.Read();
                Assert.False(task2.IsCompleted);
                advancer.AdvanceTo(1);
                Assert.False(task1.IsCompleted);
                Assert.True(task2.IsCompleted);
                Assert.Equal("Bar", task2.Result);
                advancer.AdvanceTo(2);
                Assert.True(task1.IsCompleted);
                Assert.Equal("Foo", task1.Result);
            });
        }
    }
}
