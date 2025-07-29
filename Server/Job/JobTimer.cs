using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Job
{
    struct JobTimerElem
    {
        public Job Job { get; set; }
        public DateTime ExecuteTime { get; set; }
        public JobTimerElem(Job job, DateTime executeTime)
        {
            Job = job ?? throw new ArgumentNullException(nameof(job));
            ExecuteTime = executeTime;
        }
    }
    internal class JobTimer
    {
        private PriorityQueue<JobTimerElem, DateTime> _jobQueue = new PriorityQueue<JobTimerElem, DateTime>();
        private readonly object _lock = new object();

        public void Enqueue(Job job, DateTime executeTime)
        {
            if (job == null)
                throw new ArgumentNullException(nameof(job));
            lock (_lock)
            {
                _jobQueue.Enqueue(new JobTimerElem(job, executeTime), executeTime);
            }
        }

        public void ExecuteDueJobs()
        {
            DateTime now = DateTime.UtcNow;
            lock (_lock)
            {
                while (_jobQueue.Count > 0)
                {
                    JobTimerElem jobTimerElem = _jobQueue.Peek();
                    if (jobTimerElem.ExecuteTime > now)
                        break; // 아직 실행할 시간이 아니면 중단
                    jobTimerElem = _jobQueue.Dequeue();
                    try
                    {
                        jobTimerElem.Job.Execute();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Job execution failed: {ex.Message}");
                    }
                }
            }
            
        }

        public void CancelAll()
        {
            lock (_lock)
            {
                while (_jobQueue.Count > 0)
                {
                    JobTimerElem job = _jobQueue.Dequeue();
                    job.Job.IsCancelled = true;
                }
            }
        }

    }
}
