namespace Cron_Scheduler;

public class CronJob
{
    public CronJob(int id, TimeSpan executionTimeEstimate, TimeSpan frequency, Action job)
    {
        this.Id = id;
        this.ExecutionTimeEstimate = executionTimeEstimate;
        this.Frequency = frequency;
        this.Job = job;
    }

    public int Id { get; }
    public TimeSpan ExecutionTimeEstimate { set; get; }
    public TimeSpan Frequency { get; }
    public Action Job { get; }
}