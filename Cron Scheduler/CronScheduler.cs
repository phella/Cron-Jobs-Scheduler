using System.Runtime.CompilerServices;

namespace Cron_Scheduler;
public class CronScheduler
{
    public CronScheduler()
    {
        Task.Run(() => this.ScheduleJobs());
    }

    private CancellationTokenSource cancellationTokenSource = new();

    internal HashSet<int> Jobs = new();

    private PriorityQueue<CronJob, long> JobsQueue = new();

    private async Task ScheduleJobs()
    {
        var sleepDuration = TimeSpan.FromDays(1);
        while (true)
        {
            await this.Sleep(sleepDuration);
            while (JobsQueue.TryPeek(out CronJob job, out long executionTime))
            {
                if (executionTime <= DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                {
                    var nextExecutionTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + (long)job.Frequency.TotalSeconds;
                    if (Jobs.Contains(job.Id))
                    {
                        JobsQueue.Enqueue(job, nextExecutionTime);
                        Task.Run(() => this.ExecuteJob(job));
                    }

                    JobsQueue.Dequeue();
                }
                else
                {
                    break;
                }
            }

            if (JobsQueue.TryPeek(out CronJob nextJob, out long nextJobExecutionTime))
            {
                sleepDuration = TimeSpan.FromSeconds(nextJobExecutionTime - DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            }
            else
            {
                sleepDuration = TimeSpan.FromDays(1);
            }
        }
    }

    internal virtual async Task ExecuteJob(CronJob cronJob)
    {
        if (cronJob.ExecutionTimeEstimate > cronJob.Frequency)
        {
            Logger.LogWarning($"Cron Job #{cronJob.Id} may be running too frequently");
        }

        var startTime = DateTimeOffset.UtcNow;
        Console.WriteLine($"{startTime}: Cron Job #{cronJob.Id} started.");
        try
        {
            cronJob.Job();
        }
        catch (Exception exception)
        {
            Logger.LogError($"{DateTimeOffset.UtcNow}: Cron Job #{cronJob.Id} execution failed.");
        }

        var finishTime = DateTimeOffset.UtcNow;
        cronJob.ExecutionTimeEstimate = TimeSpan.FromSeconds(finishTime.ToUnixTimeSeconds() - startTime.ToUnixTimeSeconds());
        Console.WriteLine($"{finishTime}: Cron Job #{cronJob.Id} finished successfully and it took {cronJob.ExecutionTimeEstimate} seconds");
    }

    internal bool AddJob(int id, TimeSpan executionTimeEstimate, TimeSpan frequency, Action job)
    {
        if (Jobs.Contains(id))
        {
            return false;
        }

        var cronJob = new CronJob(id, executionTimeEstimate, frequency, job);
        Jobs.Add(id);
        JobsQueue.Enqueue(cronJob, DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        Console.WriteLine($"{DateTimeOffset.UtcNow}: Cron Job #{id} is Added.");
        this.ResumeExecuteJobs();
        return true;
    }

    internal virtual bool RemoveJob(int id)
    {
        Console.WriteLine($"{DateTimeOffset.UtcNow}: Cron Job #{id} is removed.");
        return Jobs.Remove(id);
    }

    internal virtual void RemoveAllJobs()
    {
        Console.WriteLine($"{DateTimeOffset.UtcNow}: All cron jobs are removed.");
        Jobs.Clear();
    }

    internal virtual void ResumeExecuteJobs()
    {
        this.cancellationTokenSource.Cancel();
    }

    internal async Task Sleep(TimeSpan sleepDuration)
    {
        try
        {
            await Task.Delay(sleepDuration, cancellationTokenSource.Token);
        }
        catch (Exception ex)
        {
            // Do nothing
            // This means a new job is added so the sleep process is canceled.
        }
    }
}