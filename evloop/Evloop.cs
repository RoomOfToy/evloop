using System;
using System.Threading;
using System.Threading.Tasks;

namespace evloop
{
    public static class Evloop
    {
        public static void Run(Func<Task> task)
        {
            run(task, () => { return true; }) ;
        }

        // run until stopFunc returns true
        public static void Run(Func<Task> task, Func<bool> stopFunc)
        {
            run(task, stopFunc);
        }

        private static void run(Func<Task> task, Func<bool> stopFunc)
        {
            var previousCtx = SynchronizationContext.Current;
            var ctx = new Context();
            SynchronizationContext.SetSynchronizationContext(ctx);
            async Task asyncTask()
            {
                try
                {
                    await task();
                }
                catch (Exception e)
                {
                    Console.WriteLine($"{DateTime.Now} - main task failed in evloop: ", e);
                }
                finally
                {
                    if (stopFunc())
                        ctx.Cancel();
                    else
                    {
                        // repeated execution
                        await asyncTask();
                    }
                }
            }
            // post task to current ctx
            var t = asyncTask();
            Console.WriteLine($"{DateTime.Now} - evloop starts on thread {Thread.CurrentThread.ManagedThreadId}");
            ctx.Run();
            Console.WriteLine($"{DateTime.Now} - evloop stops on thread {Thread.CurrentThread.ManagedThreadId}");
            t.Wait();
            // restore context
            SynchronizationContext.SetSynchronizationContext(previousCtx);
        }
    }
}
