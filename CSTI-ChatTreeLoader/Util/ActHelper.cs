using System.Collections;
using System.Collections.Generic;
using ChatTreeLoader.Patchers;

namespace ChatTreeLoader.Util
{
    public static class ActHelper
    {
        public static void WaitAll(this GameManager __instance, Queue<CoroutineController> queue)
        {
            __instance.StartCoroutine(_inner_WaitAll(queue));
        }

        private static IEnumerator _inner_WaitAll(Queue<CoroutineController> queue)
        {
            NormalPatcher.ShouldWaitExtra = true;
            while (queue.Count > 0)
            {
                var coroutineController = queue.Dequeue();
                while (coroutineController.state == CoroutineState.Running)
                {
                    yield return null;
                }
            }

            NormalPatcher.ShouldWaitExtra = false;
        }
    }
}