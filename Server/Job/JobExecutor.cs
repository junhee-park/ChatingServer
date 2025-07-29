using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Job
{
    public class JobExecutor
    {
        JobTimer _jobTimer = new JobTimer();
        ConcurrentQueue<IJob> _jobQueue = new ConcurrentQueue<IJob>();

        public void Enqueue(IJob job)
        {
            if (job == null)
                throw new ArgumentNullException(nameof(job));
            _jobQueue.Enqueue(job);
        }

        public void Enqueue(Action job)
        {
            Enqueue(new Job(job));
        }
        public void Enqueue<T1>(Action<T1> job, T1 t1)
        {
            Enqueue(new Job<T1>(job, t1));
        }

        public void Enqueue<T1, T2>(Action<T1, T2> job, T1 t1, T2 t2)
        {
            Enqueue(new Job<T1, T2>(job, t1, t2));
        }

        public void Enqueue<T1, T2, T3>(Action<T1, T2, T3> job, T1 t1, T2 t2, T3 t3)
        {
            Enqueue(new Job<T1, T2, T3>(job, t1, t2, t3));
        }


        public void Execute()
        {
            while (_jobQueue.TryDequeue(out IJob job))
            {
                job.Execute();
            }
        }

        public void CancelAll()
        {
            foreach (var job in _jobQueue)
            {
                job.IsCancelled = true;
            }
            _jobQueue.Clear();
        }

        public void CancelJob(Job job)
        {
            if (job == null)
                throw new ArgumentNullException(nameof(job));

            if (_jobQueue.Contains(job))
            {
                job.IsCancelled = true;
            }
        }


    }
}
