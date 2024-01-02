namespace SynologyMediaHelper;
public static class Exetnsions
{
    public static T MinOrDefault<T>(this IEnumerable<T> source)
    {
        return source is not null && source.Any() ? source.Min() : default;
    }
    public static IEnumerable<T> SelectMany<T>(this IEnumerable<IEnumerable<T>> source)
        => source.SelectMany(a => a.Select(b => b));

    public static void ForEachUsingTasks<TInput, TArg>(
        this IEnumerable<TInput> source,
        int parallelTasksCount,
        Action<TInput, long, TArg> action,
        Func<TArg> getPerTaskArgument,
        Action<long, long> progressReportAction)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(getPerTaskArgument);

        const int DelayInMilliseconds = 500;
        var items = new List<TInput>();
        var currentEnumerationIndex = 0;

        var enumerator = Task.Run(() =>
        {
            foreach (var item in source)
                items.Add(item);
        });

        var args = new TArg[parallelTasksCount];
        var tasks = new Task[parallelTasksCount];

        for (int tIndex = 0; tIndex < parallelTasksCount; tIndex++)
        {
            var taskIndex = tIndex;

            args[taskIndex] = getPerTaskArgument();
            tasks[taskIndex] = Task.Run(() =>
            {
                while (!enumerator.IsCompleted || taskIndex < items.Count)
                {
                    Task.Delay(DelayInMilliseconds).Wait();
                    while (taskIndex < items.Count)
                    {
                        action(items[taskIndex], taskIndex, args[taskIndex % parallelTasksCount]);
                        taskIndex += parallelTasksCount;
                        Interlocked.Increment(ref currentEnumerationIndex);
                    }
                }
            });
        }

        Task.Run(() =>
        {
            while (tasks.Any(i => !i.IsCompleted))
            {
                Task.Delay(DelayInMilliseconds).Wait();
                progressReportAction(currentEnumerationIndex, items.Count);
            }
        });

        Task.WaitAll(tasks);
    }
}
