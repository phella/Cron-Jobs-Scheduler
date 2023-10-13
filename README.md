# Cron-Jobs-Scheduler

## Solution Description
I introduced a `CronScheduler` class that can be used in-process by instantiating an instance from this class, you can add a cron job to the scheduler by defining these fields: 
- id: job id
- executionTimeEstimate: An estimate of how long the job will take to execute.
-  frequency: The frequency at which the job should run.
-  job: the actual code.
  
You can also remove jobs from the scheduler using the job id.

## Technical Description
CronScheduler is a job scheduling library implemented in C#. I chose C# for this project due to my experience with the language and its Task interface, which greatly simplifies job handling.

1. **Non-blocking Scheduling:** CronScheduler runs on a separate thread, ensuring the main scheduling algorithm remains non-blocking. This design minimizes delays in scheduling jobs. The scheduler intelligently sleeps when there's no work to do, reducing CPU consumption. This is achieved using Task.Delay to sleep the scheduler thread until the next job time, with the aid of a cancellation token to awaken the scheduler if a new job is added.

2. **Immediate Execution:** New jobs are executed immediately upon being added to the scheduler.

3. **Priority Queue:** CronScheduler employs a priority queue to manage cron jobs and their next execution times. When dequeuing a job, it's immediately enqueued with an updated next execution time. This strategy ensures that updated jobs are enqueued before dequeuing, facilitating the implementation of recovery algorithms in the future.

4. **Thread Isolation:** Each cron job runs in its own thread to prevent blocking the main scheduling algorithm.

5. **Job ID Tracking:** The scheduler maintains a set of job IDs. This serves a dual purpose: to verify that the job hasn't been removed before execution and to ensure job uniqueness.

6. **Runtime Monitoring:** The scheduler keeps track of the job's expected runtime. If a job runs too frequently relative to its execution time, the user is warned. 

## Tradeoffs
The solution has some complexity to support intelligent sleep and maximize performance. If we were willing to trade off CPU consumption, a simpler solution could have been implemented.

## Usage Examples
Here is an example of adding two jobs to the scheduler, the first job prints time every second and the second does nothing.
```csharp
var scheduler = new CronScheduler();

scheduler.AddJob(id: 1,
                 executionTimeEstimate: TimeSpan.FromMilliseconds(20),
                 frequency: TimeSpan.FromSeconds(1),
                 job: printTimeEverySecond);

scheduler.AddJob(id: 2,
                 executionTimeEstimate: TimeSpan.FromMilliseconds(40),
                 frequency: TimeSpan.FromSeconds(2),
                 job: ()=>{});

void printTimeEverySecond() {
    Console.WriteLine(DateTime.Now);
}
```

you can later remove these jobs from the scheduler as follows:
```csharp
scheduler.RemoveJob(id: 1);
scheduler.RemoveJob(id: 2);
```

or you can remove all jobs from the scheduler using `scheduler.RemoveAllJobs()`.

## Future Improvements
