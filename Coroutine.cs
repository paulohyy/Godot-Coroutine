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
                coroutines.Add(key, new RoutinePack(method));
        }

        public bool CallIfNotExecuting(string key)
        {
            if (!coroutines[key].IsExecuting)
                coroutines[key].Start(executing);
            return false;
        }

        public override void _Process(float delta)
        {
            for (int i = 0; i < executing.Count; i++)
            {
                var current = executing[i];
                if (!current.IsExecuting)
                    current.Start(executing);
                else if (current.Enumerator.Current is Continue cast)
                {
                    if (cast.CanContinue(delta))
                        if (!current.Enumerator.MoveNext())
                            current.End(executing);
                }
                else if (current.Enumerator.Current is End)
                    current.End(executing);
            }
        }

        public static Continue Continue() => new Continue();
        public static Continue Continue(float seconds) => new Continue(seconds);
    }

    internal struct RoutinePack
    {
        public IEnumerator Enumerator { get; private set; }
        public bool IsExecuting { get { return Enumerator != null; } }
        private Func<IEnumerator> method;

        public RoutinePack(Func<IEnumerator> m)
        {
            Enumerator = null;
            method = m;
        }

        public void Start(List<RoutinePack> executing)
        {
            Enumerator = method.Invoke();
            Enumerator.MoveNext();
            executing.Add(this);
        }

        public void End(List<RoutinePack> executing)
        {
            executing.Remove(this);
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
