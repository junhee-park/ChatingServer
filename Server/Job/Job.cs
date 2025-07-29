using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Job
{
    public abstract class IJob
    {
        public bool IsCancelled { get; set; } = false; // 작업 취소 여부
        public abstract void Execute();

    }
    public class Job : IJob
    {
        Action _action;
        public Job(Action action)
        {
            _action = action ?? throw new ArgumentNullException(nameof(action));
        }
        public override void Execute()
        {
            if (IsCancelled)
                return;

            _action.Invoke();
        }
    }

    public class Job<T1> : IJob
    {
        Action<T1> _action;
        T1 _t1;
        public Job(Action<T1> action, T1 t1)
        {
            _action = action ?? throw new ArgumentNullException(nameof(action));
            _t1 = t1;
        }
        public override void Execute()
        {
            if (IsCancelled)
                return;

            _action.Invoke(_t1);
        }
    }

    public class Job<T1, T2> : IJob
    {
        Action<T1, T2> _action;
        T1 _t1;
        T2 _t2;
        public Job(Action<T1, T2> action, T1 t1, T2 t2)
        {
            _action = action ?? throw new ArgumentNullException(nameof(action));
            _t1 = t1;
            _t2 = t2;
        }

        public override void Execute()
        {
            if (IsCancelled)
                return;

            _action.Invoke(_t1, _t2);
        }
    }

    public class Job<T1, T2, T3> : IJob
    {
        Action<T1, T2, T3> _action;
        T1 _t1;
        T2 _t2;
        T3 _t3;
        public Job(Action<T1, T2, T3> action, T1 t1, T2 t2, T3 t3)
        {
            _action = action ?? throw new ArgumentNullException(nameof(action));
            _t1 = t1;
            _t2 = t2;
            _t3 = t3;
        }
        public override void Execute()
        {
            if (IsCancelled)
                return;
            _action.Invoke(_t1, _t2, _t3);
        }
    }
}
