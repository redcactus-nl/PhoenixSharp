using System;
using System.Threading.Tasks;

namespace Phoenix
{
    public interface IDelayedExecution
    {
        void Cancel();
    }

    /**
     * IDelayedExecutor
     * This class is equivalent to javascript setTimeout/clearTimeout functions.
     */
    public interface IDelayedExecutor
    {
        IDelayedExecution Execute(Action action, TimeSpan delay);
    }

    /** 
     * Scheduler
     * This class is equivalent to the Timer class in the Phoenix JS library.
     */
    public sealed class Scheduler
    {
        private readonly Action _callback;
        private readonly IDelayedExecutor _delayedExecutor;
        private readonly Func<int, TimeSpan> _timerCalc;
        private IDelayedExecution _execution;
        private int _tries;

        public Scheduler(Action callback, Func<int, TimeSpan> timerCalc, IDelayedExecutor delayedExecutor)
        {
            _callback = callback;
            _timerCalc = timerCalc;
            _delayedExecutor = delayedExecutor;
        }

        public void Reset()
        {
            _tries = 0;
            _execution?.Cancel();
            _execution = null;
        }

        public void ScheduleTimeout()
        {
            _execution?.Cancel();
            _execution = _delayedExecutor.Execute(() =>
            {
                _tries += 1;
                _callback();
            }, _timerCalc(_tries + 1));
        }
    }

    // Provide a default delayed executor that uses Tasks API.

    public sealed class TaskExecution : IDelayedExecution
    {
        internal bool Cancelled;

        public void Cancel()
        {
            Cancelled = true;
        }
    }


    public sealed class TaskDelayedExecutor : IDelayedExecutor
    {
        public IDelayedExecution Execute(Action action, TimeSpan delay)
        {
            var execution = new TaskExecution();
            Task.Delay(delay).GetAwaiter().OnCompleted(() =>
            {
                if (!execution.Cancelled)
                {
                    try
                    {
                        action();
                    }
                    catch (Exception)
                    {
                        // Workaround do not throw exception which causes a application crash.
                        /*
                         * Stacktrace:
   at System.Collections.Generic.Dictionary`2.FindValue(TKey key)
   at System.Collections.Generic.Dictionary`2.TryGetValue(TKey key, TValue& value)
   at Phoenix.Channel.Trigger(Message message)
   at Phoenix.Push.Trigger(ReplyStatus status)
   at Phoenix.Push.<StartTimeout>b__17_1()
   at Phoenix.TaskDelayedExecutor.<>c__DisplayClass0_0.<Execute>b__0()
   at System.Threading.Tasks.AwaitTaskContinuation.<>c.<.cctor>b__17_0(Object state)
   at System.Threading.ExecutionContext.RunInternal(ExecutionContext executionContext, ContextCallback callback, Object state)
--- End of stack trace from previous location ---
   at System.Threading.Tasks.AwaitTaskContinuation.RunCallback(ContextCallback callback, Object state, Task& currentTask)
--- End of stack trace from previous location ---
   at System.Threading.Tasks.Task.<>c.<ThrowAsync>b__128_1(Object state)
   at System.Threading.QueueUserWorkItemCallbackDefaultContext.Execute()
   at System.Threading.ThreadPoolWorkQueue.Dispatch()
   at System.Threading.PortableThreadPool.WorkerThread.WorkerThreadStart()
   at System.Threading.Thread.StartCallback()
                         */
                    }
                }
            });

            return execution;
        }
    }
}