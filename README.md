# Godot-Coroutine

Simple -unstable- implementation of Unity-like coroutines

First things first... I see game development as a hobby. This is something I do to be lazy and experiment. I leave good coding practices for my day job. So don't expect tests or bug fixes.

It works, but you should really try to understand Threads and Tasks before trying to use them, or else they will work against you.
Quick guide:
Don't use the StartThread or the StartTask method if you're not going to wait for more than a couple of seconds.
Use the Start method if you're going to run it like a parallel Process method, as I do most of the time.

Have fun and don't use this if your coding skills are better than mine, this is really simple to implement, so I sugest you try it yourself.

Last but not least... Have fun with this. I'm only sharing it so those people who are afraid of coming to Godot from Unity can have a gentle push.
