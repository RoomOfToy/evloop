using System;
using System.Collections.Generic;
using System.Threading;

namespace evloop
{
    internal sealed class Context : SynchronizationContext
    {
        // this may be used in multiple threads, so use ConcurrentQueue
        private readonly Queue<Action> tasks = new Queue<Action>();
        private readonly Thread mainThread;
        private readonly ManualResetEvent stopEvt = new ManualResetEvent(false);
        private readonly AutoResetEvent newTaskEvt = new AutoResetEvent(false);

        // bind ctx to current evloop thread
        public Context()
        {
            mainThread = Thread.CurrentThread;
        }

        // async, non-blocking
        public override void Post(SendOrPostCallback work, object state)
        {
            tasks.Enqueue(() => { work(state); });
            // new task incoming
            newTaskEvt.Set();
        }

        // sync, blocking
        public override void Send(SendOrPostCallback work, object state)
        {
            var workResultEvt = new ManualResetEventSlim();
            // send work to evloop
            Post(s => {
                try
                {
                    work(s);
                }
                // whatever happened, remove blocking
                finally
                {
                    workResultEvt.Set();
                }
            }, state);
            // if current thread is mainThread (evloop thread), then execute
            if (Thread.CurrentThread == mainThread)
            {
                run(workResultEvt);
            }
            // block here
            workResultEvt.Wait();
        }

        public void Run()
        {
            // wait for new task coming
            while (WaitHandle.WaitAny(new WaitHandle[] { stopEvt, newTaskEvt }) == 1)
            {
                run(null);
            }
        }

        private void run(ManualResetEventSlim doneEvt)
        {
            if (Thread.CurrentThread != mainThread)
                throw new InvalidOperationException("evloop should run on the same thread with this context created on");
            // not task done && not stopped
            while ((doneEvt == null || !doneEvt.IsSet) && !stopEvt.WaitOne(0))
            {
                // run task
                if (tasks.Count > 0 && tasks.TryDequeue(out Action task))
                {
                    task?.Invoke();
                }
            }
        }

        // stop running loop
        public void Cancel()
        {
            stopEvt.Set();
        }
    }
}
