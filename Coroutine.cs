using Godot;
using System;
using System.Collections;
using System.Collections.Generic;

namespace SkipTheBadEngine
{
    public class Coroutine : Node
    {
        public static readonly End End = new End();

        private Dictionary<string, RoutinePack> coroutines;
        private List<RoutinePack> executing;

        public Coroutine(Node parent)
        {
            coroutines = new Dictionary<string, RoutinePack>();
            executing = new List<RoutinePack>();
            parent.AddChild(this);
        }

        public void Register(string key, Func<IEnumerator> method)
        {
            if (!coroutines.ContainsKey(key))
                coroutines.Add(key, new RoutinePack(method, executing));
        }

      public bool CallIfNotExecuting(string key)
        {
            if (!coroutines[key].IsExecuting)
            {
                coroutines[key].Start();
                return true;
            }
            return false;
        }

        public override void _Process(float delta)
        {
            for (int i = 0; i < executing.Count; i++)
            {
                var current = executing[i];
                if (current.Enumerator.Current is Continue cast)
                {
                    if (cast.CanContinue(delta))
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

        public static Continue Continue() => new Continue();
        public static Continue Continue(float seconds) => new Continue(seconds);
    }

    internal struct RoutinePack
    {
        private Func<IEnumerator> method;
        private List<RoutinePack> executing;

        public IEnumerator Enumerator { get; private set; }
        public bool IsExecuting { get { return Enumerator != null; } }

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
