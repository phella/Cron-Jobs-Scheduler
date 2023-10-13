
using Cron_Scheduler;

void printTimeEverySecond() {
    Console.WriteLine(DateTime.Now);
}

var scheduler = new CronScheduler();
scheduler.AddJob(id: 1, executionTimeEstimate: TimeSpan.FromMilliseconds(20), frequency: TimeSpan.FromSeconds(1), job: printTimeEverySecond);
scheduler.AddJob(2, TimeSpan.FromMilliseconds(20), TimeSpan.FromSeconds(2), ()=>{});
await Task.Delay(TimeSpan.FromSeconds(5));
scheduler.RemoveJob(1);
await Task.Delay(TimeSpan.FromSeconds(3));
scheduler.RemoveJob(2);