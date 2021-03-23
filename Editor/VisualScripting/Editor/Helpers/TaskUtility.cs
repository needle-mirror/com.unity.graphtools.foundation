using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Packages.VisualScripting.Editor.Helpers
{
    public static class TaskUtility
    {
        public static ConcurrentBag<TOutput> RunTasks<TInput, TOutput>(
            List<TInput> items,
            Action<TInput, ConcurrentBag<TOutput>> action)
        {
            var cb = new ConcurrentBag<TOutput>();
            var count = Environment.ProcessorCount;
            var tasks = new Task[count];
            int itemsPerTask = (int)Math.Ceiling(items.Count / (float)count);

            for (int i = 0; i < count; i++)
            {
                int i1 = i;
                tasks[i] = Task.Run(() =>
                {
                    for (int j = 0; j < itemsPerTask; j++)
                    {
                        int index = j + itemsPerTask * i1;
                        if (index >= items.Count)
                            break;

                        action.Invoke(items[index], cb);
                    }
                });
            }

            Task.WaitAll(tasks);
            return cb;
        }
    }
}
