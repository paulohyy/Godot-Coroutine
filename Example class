    public class Example : Node
    {
        Coroutine coroutine;

        public Example()
        {
            coroutine = new Coroutine(this);
            coroutine.Register("example", ExampleCoroutine);
        }

        public IEnumerator ExampleCoroutine()
        {
            bool stopCondition = true;

            for (int i = 0; i < 10; i++)
            {
                // code to do something once every 1 second
                yield return Coroutine.Continue(1);

                if (stopCondition)
                    yield return Coroutine.End;
            }
        }

        public override void _Process(float delta)
        {
            var someCondition = true;
            if (someCondition)
                coroutine.CallIfNotExecuting("example");
        }
    }
