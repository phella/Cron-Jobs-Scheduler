using Cron_Scheduler;
using Moq;
using FluentAssertions;

namespace TestProject2
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void AddJobs_ShouldWork()
        {
            var mockScheduler =  new Mock<CronScheduler>();
            mockScheduler.CallBase = true;
            var scheduler = mockScheduler.Object;

            var mockDelegate = new Mock<Action>();
            scheduler.AddJob(id: 1, executionTimeEstimate: TimeSpan.FromHours(1), frequency: TimeSpan.FromHours(2), mockDelegate.Object).Should().BeTrue();
            // Can't enqueue two jobs with same id
            scheduler.AddJob(id: 1, executionTimeEstimate: TimeSpan.FromHours(1), frequency: TimeSpan.FromHours(2), mockDelegate.Object).Should().BeFalse();

            mockScheduler.Verify(mock => mock.ResumeExecuteJobs(), Times.Once);
            mockDelegate.Verify(mock => mock(), Times.Once);
            scheduler.Jobs.Should().HaveCount(1);
        }

        [Test]
        public void RemoveJob_ShouldWork()
        {
            var mockScheduler =  new Mock<CronScheduler>();
            mockScheduler.CallBase = true;
            var scheduler = mockScheduler.Object;

            scheduler.Jobs.Add(1);
            scheduler.Jobs.Remove(1);
            scheduler.Jobs.Should().HaveCount(0);

            // Verify that no exception is thrown when attempting to remove a non-existent job.
            Assert.DoesNotThrow(() => scheduler.Jobs.Remove(2));
        }

        [Test]
        public void RemoveAllJobs_ShouldWork()
        {
            var mockScheduler =  new Mock<CronScheduler>();
            mockScheduler.CallBase = true;
            var scheduler = mockScheduler.Object;

            scheduler.Jobs.Add(1);
            scheduler.Jobs.Add(2);
            scheduler.Jobs.Add(3);

            scheduler.RemoveAllJobs();
            scheduler.Jobs.Should().HaveCount(0);
        }

        [Test]
        public async Task Sleep_ShouldWork()
        {
            var mockScheduler =  new Mock<CronScheduler>();
            mockScheduler.CallBase = true;
            var scheduler = mockScheduler.Object;

            var start = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            await scheduler.Sleep(sleepDuration: TimeSpan.FromSeconds(1));
            (DateTimeOffset.UtcNow.ToUnixTimeSeconds() - start).Should().Be(1);
        }

        [Test]
        public async Task ResumeJobs_ShouldWork()
        {
            var mockScheduler =  new Mock<CronScheduler>();
            mockScheduler.CallBase = true;
            var scheduler = mockScheduler.Object;

            var start = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            await scheduler.Sleep(sleepDuration: TimeSpan.FromSeconds(1));
            (DateTimeOffset.UtcNow.ToUnixTimeSeconds() - start).Should().Be(1);
        }

        [Test]
        public async Task ExecuteJob_ShouldWork()
        {
            var mockScheduler =  new Mock<CronScheduler>();
            mockScheduler.CallBase = true;
            var scheduler = mockScheduler.Object;

            var mockDelegate = new Mock<Action>();
            var cronJob = new CronJob(1, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(1), mockDelegate.Object);

            var mockConsole = new Mock<TextWriter>();
            Console.SetOut(mockConsole.Object);

            await scheduler.ExecuteJob(cronJob);

            mockConsole.Verify(c => c.WriteLine("Warning: Cron Job #1 may be running too frequently"), Times.Once);
            mockDelegate.Verify(mock => mock(), Times.Once);

            // Verify that failed jobs are captured
            mockDelegate.Setup(action => action.Invoke()).Throws(new Exception());
            var cronJob2 = new CronJob(2, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(3), mockDelegate.Object);
            await scheduler.ExecuteJob(cronJob2);
            mockConsole.Verify(c => c.WriteLine(It.Is<string>(st => st.EndsWith("failed."))), Times.Once);
        }

        [Test]
        public async Task CronScheduler_ShouldWorkForShortJobs()
        {
            var mockConsole = new Mock<TextWriter>();
            Console.SetOut(mockConsole.Object);

            var scheduler = new CronScheduler();
            scheduler.AddJob(1, TimeSpan.FromMilliseconds(20), TimeSpan.FromSeconds(1), ()=>{});
            scheduler.AddJob(2, TimeSpan.FromMilliseconds(20), TimeSpan.FromSeconds(2), ()=>{});
            await Task.Delay(TimeSpan.FromSeconds(5));

            // Ensure that the number of times job 1 and job 2 were executed aligns with the expected count.
            mockConsole.Verify(c => c.WriteLine(It.Is<string>(st => st.Contains("Job #1 started."))), Times.Exactly(6));
            mockConsole.Verify(c => c.WriteLine(It.Is<string>(st => st.Contains("Job #1 finished"))), Times.Exactly(6));
            mockConsole.Verify(c => c.WriteLine(It.Is<string>(st => st.Contains("Job #2 started."))), Times.Exactly(3));
            mockConsole.Verify(c => c.WriteLine(It.Is<string>(st => st.Contains("Job #2 finished"))), Times.Exactly(3));
            mockConsole.Reset();

            scheduler.RemoveJob(1);
            scheduler.AddJob(3, TimeSpan.FromHours(1), TimeSpan.FromSeconds(4), ()=>{});
            await Task.Delay(TimeSpan.FromSeconds(3));
            scheduler.RemoveJob(2);
            scheduler.RemoveJob(3);

            // Ensure that the number of times job 2 and job 3 were executed aligns with the expected count.
            mockConsole.Verify(c => c.WriteLine(It.Is<string>(st => st.Contains("Job #2 started."))), Times.Exactly(2));
            mockConsole.Verify(c => c.WriteLine(It.Is<string>(st => st.Contains("Job #2 finished"))), Times.Exactly(2));
            mockConsole.Verify(c => c.WriteLine(It.Is<string>(st => st.Contains("Job #3 started."))), Times.Once);
            mockConsole.Verify(c => c.WriteLine(It.Is<string>(st => st.Contains("Job #3 finished"))), Times.Once);
            // Ensure that job 3 log a warning due to its long expected execution time and short frequency.
            mockConsole.Verify(c => c.WriteLine(It.Is<string>(st => st.Contains("Warning"))), Times.Once);
            // Ensure that job 1 never ran after being removed.
            mockConsole.Verify(c => c.WriteLine(It.Is<string>(st => st.Contains("Job #1 started."))), Times.Never);
        }

        [Test]
        public async Task CronScheduler_ShouldWorkForLongJobs()
        {
            var mockConsole = new Mock<TextWriter>();
            Console.SetOut(mockConsole.Object);

            var scheduler = new CronScheduler();
            scheduler.AddJob(1, TimeSpan.FromMilliseconds(20), TimeSpan.FromSeconds(1), LongProcessingTask);
            scheduler.AddJob(2, TimeSpan.FromMilliseconds(20), TimeSpan.FromSeconds(2), LongProcessingTask);
            await Task.Delay(TimeSpan.FromSeconds(5));

            // Ensure that the number of times job 1 and job 2 were starts aligns with the expected count.
            // also there are still some jobs that didn't end.
            mockConsole.Verify(c => c.WriteLine(It.Is<string>(st => st.Contains("Job #1 started."))), Times.Exactly(6));
            mockConsole.Verify(c => c.WriteLine(It.Is<string>(st => st.Contains("Job #1 finished"))), Times.AtMost(5));
            mockConsole.Verify(c => c.WriteLine(It.Is<string>(st => st.Contains("Job #2 started."))), Times.Exactly(3));
            mockConsole.Verify(c => c.WriteLine(It.Is<string>(st => st.Contains("Job #2 finished"))), Times.AtMost(3));

            // Ensure that we are logging a warning to user that jobs are running very frequently
            mockConsole.Verify(c => c.WriteLine(It.Is<string>(st => st.Contains("Warning: Cron Job #1 may be running too frequently"))), Times.AtLeast(1));
            mockConsole.Verify(c => c.WriteLine(It.Is<string>(st => st.Contains("Warning: Cron Job #2 may be running too frequently"))), Times.AtLeast(1));
        }

        private void LongProcessingTask()
        {
            var x = 0;
            for (var i = 0; i < Math.Pow(10, 8); i++)
            {
                x++;
            }
        }
    }
}