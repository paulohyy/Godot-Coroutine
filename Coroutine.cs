using Godot;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SkipTheBadEngine
{
    public class CoroutineHandler : Node
    {
        private static readonly End end = new End();

        private Dictionary<string, RoutinePack> coroutines;
        private List<RoutinePack> executing;
        private Dictionary<string,Task> tasks;
        private Dictionary<string, System.Threading.Thread> threads;

        public CoroutineHandler(Node parent)
        {
            coroutines = new Dictionary<string, RoutinePack>();
            executing = new List<RoutinePack>();
            tasks = new Dictionary<string, Task>();
            threads = new Dictionary<string, System.Threading.Thread>();
            parent.AddChild(this);
        }

        private void Register(Func<IEnumerator> method)
        {
            if (!coroutines.ContainsKey(method.Method.Name))
                coroutines.Add(method.Method.Name, new RoutinePack(method, executing));
        }

        public bool Start(Func<IEnumerator> method)
        {
            if (!coroutines.ContainsKey(method.Method.Name))
                Register(method);

            if (!coroutines[method.Method.Name].IsExecuting)
            {
                coroutines[method.Method.Name].IsExecuting = true;
                coroutines[method.Method.Name].Start();
                return true;
            }
            return false;
        }

        public void StartTask(Func<IEnumerator> method)
        {
            if (tasks.ContainsKey(method.Method.Name))
                return;

            var task = new Task(async () =>
            {
                foreach (var item in TaskIterator(method))
                {
                    if (item is Continue cont)
                        await Task.Delay((int)(cont.Seconds * 1000));
                    else if (item is End)
                        break;
                }
                CallBack(method.Method.Name);
            });
            tasks.Add(method.Method.Name, task);
            task.Start();
        }

        public void StartThread(Func<IEnumerator> method)
        {
            if (threads.ContainsKey(method.Method.Name))
                return;

            var thread = new System.Threading.Thread(() =>
            {
                foreach (var item in TaskIterator(method))
                {
                    if (item is Continue cont)
                        new ManualResetEvent(false).WaitOne(((int)(cont.Seconds * 1000)));
                    else if (item is End)
                        break;
                }
                CallBack(method.Method.Name);
            });
            threads.Add(method.Method.Name, thread);
            thread.Start();
        }

        private void CallBack(string method, bool isTask = true)
        {
            if (isTask)
                tasks.Remove(Name);
            else
                threads.Remove(Name);
        }

        private IEnumerable<object> TaskIterator(Func<IEnumerator> method)
        {
            var enumerator = method();
            while (enumerator.MoveNext())
                yield return enumerator.Current;
        }

        public void Stop(Func<IEnumerator> method)
        {
            if (coroutines.ContainsKey(method.Method.Name) && coroutines[method.Method.Name].IsExecuting)
                coroutines[method.Method.Name].End();
            else if (threads.ContainsKey(method.Method.Name))
            {
                threads[method.Method.Name].Abort();
                threads.Remove(method.Method.Name);
            }
            else if (tasks.ContainsKey(method.Method.Name))
            {
                tasks[method.Method.Name].Dispose();
                tasks.Remove(method.Method.Name);
            }
        }

        public override void _Process(float delta)
        {
            for (int i = 0; i < executing.Count; i++)
            {
                var current = executing[i];
                if (current.Enumerator.Current is Continue cont)
                {
                    if (cont.CanContinue(delta))
                        if (!current.Enumerator.MoveNext())
                            current.End();
                }
                else if (current.Enumerator.Current is End)
                    current.End();
            }
        }

        protected override void Dispose(bool disposing)
        {
            executing.Clear();
            base.Dispose(disposing);
            foreach (var coroutine in coroutines.Values)
                coroutine.Dispose();

            coroutines.Clear();

            foreach (var thread in threads.Values)
                thread.Abort();

            foreach (var task in tasks.Values)
                task.Dispose();

            this.GetParent().RemoveChild(this);

            executing = null;
            coroutines = null;
        }

        /// <summary>
        /// Continues in the next frame
        /// </summary>
        public static Continue Continue() => new Continue();
        /// <summary>
        /// Waits a specified amount of seconds
        /// </summary>
        public static Continue Wait(float seconds) => new Continue(seconds);
        /// <summary>
        /// Ends the coroutine
        /// </summary>
        public static End End() => end;
    }

    internal class RoutinePack : IDisposable
    {
        private Func<IEnumerator> method;
        private List<RoutinePack> executing;

        public IEnumerator Enumerator { get; private set; }
        public bool IsExecuting { get; set; } = new bool();

        public RoutinePack(Func<IEnumerator> m, List<RoutinePack> executing)
        {
            this.executing = executing;
            Enumerator = null;
            method = m;
        }

        public void Start()
        {
            Enumerator = method.Invoke();
            Enumerator.MoveNext();
            executing.Add(this);
        }

        public void End()
        {
            executing.Remove(this);
            Enumerator = null;
            IsExecuting = false;
        }

        public void Dispose()
        {
            End();
            executing = null;
            method = null;
            Enumerator = null;
        }
    }

    public class Continue
    {
        private float timeout = 0;

        public float Seconds { get; private set; } = 0.0625f;
        public Continue() { }
        public Continue(float seconds) => Seconds = seconds;

        public bool CanContinue(float delta)
        {
            timeout += delta;
            return timeout >= Seconds;
        }
    }

    public class End { }
}
