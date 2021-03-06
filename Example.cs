    public class Example : Node
    {
        CoroutineHandler handler;

        public Example()
        {
            handler = new CoroutineHandler(this);
            // calling in the same thread
            handler.Start(ExampleCoroutine);
            // calling in another thread
            handler.StartThread(ExampleCoroutine);
            // calling in a task
            handler.StartThread(ExampleCoroutine);
        }

        public IEnumerator ExampleCoroutine()
        {
            bool stopCondition = true;

            for (int i = 0; i < 10; i++)
            {
                // code to do something once every 1 second
                yield return CoroutineHandler.Wait(1.5f);

                if (stopCondition)
                    yield return CoroutineHandler.End();
            }
        }

        public override void _Process(float delta)
        {
            var someCondition = true;
            if (someCondition)
                handler.StartIfNotExecuting(ExampleCoroutine);
        }
    }
