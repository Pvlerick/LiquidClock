LiquidClock
===========
A small utility to test asynchronous code in .NET

Build Status: [![Build status](https://ci.appveyor.com/api/projects/status/mpdke63xfxqnisth/branch/master?svg=true)](https://ci.appveyor.com/project/Pvlerick/liquidclock)

[NuGet Package](https://www.nuget.org/packages/LiquidClock)

# Release Notes
See the RELEASE_NOTES.md file.

# Credits
This utility is based on Jon Skeet's TimeMachine code that was demonstrated in his [Pulralsight's course "Asynchronous C# 5.0"](https://app.pluralsight.com/library/courses/skeet-async/table-of-contents) which can be found here: https://github.com/jskeet/DemoCode/tree/master/AsyncIntro/Code/Testing.NUnit
It is licensed under the Apache 2.0 license, just like the original code.

# Introduction
LiquidClock is designed to be used in unit tests when you want to control the order in which ```Task```s complete.
Ordering the completing of ```Task```s allows you to cover all possible execution paths and create scenarios that would be difficult to test otherwise.

# Simple Usage

The ```TimeMachine``` class provide a way to produce uncompleted ```Task```s that can later be completed (or faulted or canceled) in a deterministic manner.

```csharp
var timeMachine = new TimeMachine();

//Creates a Task<string> that will complete and have "Foo" as a result at time 1
Task<string> task = timeMachine.ScheduleSuccess(1, "Foo");

//Once you have planned tasks, you can call ExecuteInContext in order to start
// advancing time and complete tasks one by one
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
```
# Advanced

## Overloads

There are four overloads of this methods:
```csharp
//Creates a Task that will sucessfuly complete at time 1
Task task1 = timeMachine.ScheduleSuccess(1);
//Creates a Task<string> that will sucessfuly complete and have "Foo" as a result at time 2
Task<string> task2 = timeMachine.ScheduleSuccess(2, "Foo");
//Creates a Task that will print "Baz" at time 3 then be marked as complete
Task task3 = timeMachine.ScheduleSuccess(3, () => Task.Run(() => Console.WriteLine("Baz")));
//Creates a Task that will execute the given function at time 4 and return "Baz"
Task<string> task4 = timeMachine.ScheduleSuccess(4, () => Task.FromResult("Baz"));
```

The two last overload allow you to execute some arbitrary code at the given time, just before the ```Task``` is marked as completed.

## Faults and Cancellations

Just like successes, you can schedule faults and cancellations:

```csharp
//Creates a task that will fault at time 1 and contain the given exception
Task task1 = timeMachine.ScheduleFault(1, new Exception());
//Creates a task<string> that will fault at time 2 and contain the given exception
Task<string> task2 = timeMachine.ScheduleFault<string>(2, new Exception());
//Creates a task that will be canceled at time 3
Task task3 = timeMachine.ScheduleCancellation(3);
//Creates a task<string> that will be canceled at time 4
Task<string> task4 = timeMachine.ScheduleCancellation<string>(4);
```

Note that ```ScheduleFault``` and ```ScheduleCancellation``` have the same overloads as ```ScheduleSuccess```.

## Advancer

The ```Advancer``` class is what is given to you when you call the ```ExecuteInContext``` method on ```TimeMachine```. It only has methods to advance time.

- ```Advance()``` will advance time forward from one unit
- ```AdvanceBy(int amount)``` will advance the time by the given amount (like calling ```Advance``` _amount_ number of times in a loop)
- ```AdvanceTo(int targetTime)``` will advance the time until the target time
- ```AdvanceToEnd()``` will advance the time to the last scheduled event

Note that you cannot make time go backward, so if you try to move time backward you will get an exception.

Most of the features are demonstrated one way or another in the unit tests.

# TODO
- Add tests on the Advancer class
- Check if the ManuallyPumpedSynchronizationContext can/should be replaced: https://stackoverflow.com/a/42907493/920
