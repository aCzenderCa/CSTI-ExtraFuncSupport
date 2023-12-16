using System.Collections;
using System.Collections.Generic;
using BepInEx;
using CSTI_LuaActionSupport.Helper;
using HarmonyLib;
using NLua;
using UnityEngine;

namespace CSTI_LuaActionSupport.LuaCodeHelper;

public static class LuaTimer
{
    [LuaFunc]
    public static float Rand()
    {
        return Random.value;
    }
    
    private static bool OnWaitCA;

    public static Coroutine? Wait4CA()
    {
        if (!GameManager.Instance) return null;
        return LuaSupportRuntime.Runtime.StartCoroutine(waitAll());

        IEnumerator waitAll()
        {
            while (OnWaitCA)
            {
                yield return null;
            }

            var controllers = GameManager.Instance.ProcessCache();

            OnWaitCA = true;
            while (controllers.Count > 0)
            {
                var coroutineController = controllers.Dequeue();
                while (coroutineController.state != CoroutineState.Finished)
                {
                    yield return null;
                }
            }

            OnWaitCA = false;
        }
    }

    [HarmonyPostfix, HarmonyPatch(typeof(GameManager), nameof(GameManager.PerformingAction), MethodType.Getter)]
    public static void SetOnWaitCA(ref bool __result)
    {
        __result |= OnWaitCA;
    }

    public class SimpleTimer(float time, float curTime)
    {
        public readonly float Time = time;
        public float CurTime = curTime;
    }

    public static readonly List<LuaFunction> FrameFunctions = [];
    public static readonly List<LuaFunction> FixFrameFunctions = [];
    public static readonly Dictionary<LuaFunction, SimpleTimer> EveryTimeFunctions = [];

    [LuaFunc]
    public static void ProcessCacheEnum()
    {
        GameManager.Instance.ProcessCache();
    }

    [LuaFunc]
    public static void Frame(LuaFunction function)
    {
        FrameFunctions.Add(function);
    }

    [LuaFunc]
    public static void FixFrame(LuaFunction function)
    {
        FixFrameFunctions.Add(function);
    }

    [LuaFunc]
    public static void EveryTime(LuaFunction function, float time = 0.1f)
    {
        EveryTimeFunctions.Add(function, new SimpleTimer(time, 0));
    }

    [LuaFunc]
    public static float FrameTime()
    {
        return Time.deltaTime;
    }

    [LuaFunc]
    public static float FixFrameTime()
    {
        return Time.fixedDeltaTime;
    }

    [LuaFunc]
    public static void StartCoroutine(LuaFunction function)
    {
        LuaSupportRuntime.Runtime.StartCoroutine(Coroutine());
        return;

        IEnumerator Coroutine()
        {
            while (true)
            {
                var objects = function.Call();
                if (objects.Length > 0 && objects[0].TryNum<float>() is { } d)
                {
                    if (d is > -3 and <= -2)
                    {
                        yield return Wait4CA();
                    }
                    else if (d is > -2 and <= -1)
                    {
                        yield return new WaitForFixedUpdate();
                    }
                    else if (d is > -1 and <= 0)
                    {
                        yield return null;
                    }
                    else
                    {
                        yield return new WaitForSeconds(d);
                    }
                }
                else
                {
                    yield break;
                }
            }
        }
    }
}

public static class LuaInput
{
    [LuaFunc]
    public static float GetScroll()
    {
        return UnityInput.Current.mouseScrollDelta.y;
    }

    [LuaFunc]
    public static bool GetKey(string key)
    {
        return UnityInput.Current.GetKey(key);
    }

    [LuaFunc]
    public static bool GetKeyDown(string key)
    {
        return UnityInput.Current.GetKeyDown(key);
    }

    [LuaFunc]
    public static bool GetKeyUp(string key)
    {
        return UnityInput.Current.GetKeyUp(key);
    }
}