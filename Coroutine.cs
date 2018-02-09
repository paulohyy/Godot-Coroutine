using Godot;
using System;
using System.Collections;
using System.Collections.Generic;

namespace SkipTheBadEngine
{
    public interface RoutineFlux { }
    public class Continue : RoutineFlux { }
    public class End : RoutineFlux { }

    public class Coroutine : Node // needs refactoring and implementation of wait for seconds
    {
        public static readonly End End = new End();
        public static readonly Continue Continue = new Continue();

        Dictionary<string, BoolMethod> coroutines = new Dictionary<string, BoolMethod>();
        List<BoolMethod> executing = new List<BoolMethod>();

        public Coroutine(Node parent)
        {
            parent.AddChild(this);
        }

        public void Register(string key, Func<IEnumerator> method)
        {
            coroutines.Add(key, new BoolMethod(false, method));
        }

        public bool CallIfNotExecuting(string key)
        {
            if (!coroutines[key].isExecuting)
                coroutines[key].Start(executing);
            return false;
        }

        public override void _Process(float delta)
        {
            foreach (BoolMethod bm in executing)
            {
                if (!bm.isExecuting)
                    bm.Start(executing);
                else if (bm.enumerator.MoveNext())
                {
                    if (bm.enumerator.Current is End)
                        bm.End();
                }
            }

            executing.RemoveAll(e => e.enumerator == null);
        }

        int count = 0;
    }

    internal class BoolMethod
    {
        public IEnumerator enumerator;

        public bool isExecuting;
        public Func<IEnumerator> method;

        public BoolMethod(bool b, Func<IEnumerator> m)
        {
            isExecuting = b;
            method = m;
            enumerator = null;
        }

        public void Start(List<BoolMethod> executing)
        {
            enumerator = method.Invoke();
            isExecuting = true;
            executing.Add(this);
        }

        public void End()
        {
            enumerator = null;
            isExecuting = false;
        }
    }
}
