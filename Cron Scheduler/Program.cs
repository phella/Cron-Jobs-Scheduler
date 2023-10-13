
using Cron_Scheduler;

var scheduler = new CronScheduler();
scheduler.AddJob(1, TimeSpan.FromMilliseconds(20), TimeSpan.FromSeconds(1), ()=>{});
scheduler.AddJob(2, TimeSpan.FromMilliseconds(20), TimeSpan.FromSeconds(2), ()=>{});
await Task.Delay(TimeSpan.FromSeconds(5));
scheduler.RemoveJob(1);
await Task.Delay(TimeSpan.FromSeconds(3));
scheduler.RemoveJob(2);