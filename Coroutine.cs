using Godot;
using System;
using System.Collections;
using System.Collections.Generic;

namespace SkipTheBadEngine
{
    public class CoroutineHandler : Node
    {
        private static readonly End end = new End();

        private Dictionary<string, RoutinePack> coroutines;
        private List<RoutinePack> executing;

        public CoroutineHandler(Node parent)
        {
            coroutines = new Dictionary<string, RoutinePack>();
            executing = new List<RoutinePack>();
            parent.AddChild(this);
        }

        private void Register(Func<IEnumerator> method)
        {
            if (!coroutines.ContainsKey(method.Method.Name))
                coroutines.Add(method.Method.Name, new RoutinePack(method, executing));
        }

        public bool StartIfNotExecuting(Func<IEnumerator> method)
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

        public void Stop(Func<IEnumerator> method)
        {
            if (!coroutines.ContainsKey(method.Method.Name))
                return;

            if (coroutines[method.Method.Name].IsExecuting)
                coroutines[method.Method.Name].End();
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

        public void Destroy()
        {
            this.GetParent().RemoveChild(this);
            coroutines.Clear();
            coroutines = null;
            foreach (var item in executing) item.Destroy();
            executing.Clear();
            executing = null;
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

    internal class RoutinePack
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

        public void Destroy()
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

        public float Seconds { get; private set; }
        public Continue() { Seconds = 0; }
        public Continue(float seconds) => Seconds = seconds;

        public bool CanContinue(float delta)
        {
            timeout += delta;
            return timeout >= Seconds;
        }
    }

    public class End { }
}
