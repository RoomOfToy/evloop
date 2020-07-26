using System;
using System.Threading.Tasks;

namespace evloop
{
    class Program
    {
        static int cnt = 0;
        static bool stop()
        {
            while (cnt < 10)
            {
                cnt++;
                return false;
            }
                
            return true;
        }
        static void Main(string[] args)
        {
            Evloop.Run(async () =>
            {
                await Task.Delay(100);
                Console.WriteLine("test");
            });
            Evloop.Run(async () =>
            {
                await Task.Delay(200);
                Console.WriteLine("test again");
            });
            Evloop.Run(async () =>
            {
                await Task.Delay(200);
                Console.WriteLine("test again");
            }, () => { return true; });
            Evloop.Run(async () =>
            {
                await Task.Delay(200);
                Console.WriteLine($"test {cnt}");
            }, stop);
            /* deadlock
            Evloop.Run(async () =>
            {
                await Task.Delay(300);
                Console.WriteLine("test again again");
            }, () => {
                async Task<bool> wait() {
                    await Task.Delay(300);
                    return true;
                }
                return wait().Result;
            });
            */
        }
    }
}
