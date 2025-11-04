using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace CadProjector.Rendering.Threading
{
    internal class RenderTaskScheduler
    {
        private readonly ConcurrentQueue<RenderTask> taskQueue = new();
        private readonly int maxDegreeOfParallelism;
        private readonly TaskFactory taskFactory;

        public RenderTaskScheduler(int? maxDegreeOfParallelism = null)
  {
  this.maxDegreeOfParallelism = maxDegreeOfParallelism ?? Environment.ProcessorCount;
this.taskFactory = new TaskFactory(new TaskSchedulerConfig 
 { 
       MaxDegreeOfParallelism = this.maxDegreeOfParallelism 
     });
   }

     public void EnqueueTask(RenderTask task)
        {
            taskQueue.Enqueue(task);
 ProcessQueueAsync();
        }

  private async void ProcessQueueAsync()
        {
   var tasks = new List<Task>();
       
     while (taskQueue.TryDequeue(out var renderTask))
        {
    var task = taskFactory.StartNew(() => renderTask.Execute(), 
      TaskCreationOptions.LongRunning);
                tasks.Add(task);

    if (tasks.Count >= maxDegreeOfParallelism)
    {
     await Task.WhenAny(tasks);
          tasks.RemoveAll(t => t.IsCompleted);
     }
            }

       await Task.WhenAll(tasks);
        }

    private class TaskSchedulerConfig : TaskScheduler
   {
     public int MaxDegreeOfParallelism { get; init; }

      protected override IEnumerable<Task> GetScheduledTasks() => Enumerable.Empty<Task>();

            protected override void QueueTask(Task task)
            {
       ThreadPool.QueueUserWorkItem(_ => TryExecuteTask(task));
     }

    protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
    {
          return TryExecuteTask(task);
    }
      }
    }

    internal abstract class RenderTask
    {
        public abstract void Execute();
    }
}