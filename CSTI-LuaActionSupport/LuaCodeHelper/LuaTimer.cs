using System;
using System.Collections;
using System.Collections.Generic;
using BepInEx;
using CSTI_LuaActionSupport.Helper;
using HarmonyLib;
using NLua;
using UnityEngine;
using Random = UnityEngine.Random;

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
        return Runtime.StartCoroutine(waitAll());

        IEnumerator waitAll()
        {
            while (GameManager.PerformingAction) yield return null;

            var controllers = GameManager.Instance.ProcessCache();

            OnWaitCA = true;
            while (controllers.Count > 0)
            {
                var coroutineController = controllers.Dequeue();
                if (coroutineController == null) break;
                while (coroutineController.state != CoroutineState.Finished) yield return null;
            }

            OnWaitCA = false;
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.PerformingAction), MethodType.Getter)]
    public static void SetOnWaitCA(ref bool __result)
    {
        __result |= OnWaitCA;
    }

    public class SimpleTimer
    {
        public readonly float Time;
        public float CurTime;

        public SimpleTimer(float time, float curTime)
        {
            Time = time;
            CurTime = curTime;
        }

        public bool Step()
        {
            CurTime += UnityEngine.Time.deltaTime;
            if (CurTime < Time) return false;
            CurTime -= Time;
            return true;
        }
    }

    public static readonly List<LuaFunction> FrameFunctions = new();
    public static readonly List<LuaFunction> FixFrameFunctions = new();
    public static readonly Dictionary<LuaFunction, SimpleTimer> EveryTimeFunctions = new();

    [LuaFunc]
    public static CoroutineHelper.CoroutineQueue ProcessCacheEnum()
    {
        return GameManager.Instance.ProcessCache();
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
        Runtime.StartCoroutine(Coroutine());
        return;

        IEnumerator Coroutine()
        {
            while (true)
            {
                var objects = function.Call();
                if (objects.Length > 0 && objects[0].TryNum<float>() is { } d)
                {
                    if (d is > -3 and <= -2)
                        yield return Wait4CA();
                    else if (d is > -2 and <= -1)
                        yield return new WaitForFixedUpdate();
                    else if (d is > -1 and <= 0)
                        yield return null;
                    else
                        yield return new WaitForSeconds(d);
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

    [LuaFunc]
    public static bool GetCodedKey(string key)
    {
        if (!Enum.TryParse<KeyCode>(key, true, out var kc)) return false;
        return UnityInput.Current.GetKey(kc);
    }

    [LuaFunc]
    public static bool GetCodedKeyDown(string key)
    {
        if (!Enum.TryParse<KeyCode>(key, true, out var kc)) return false;
        return UnityInput.Current.GetKeyDown(kc);
    }

    [LuaFunc]
    public static bool GetCodedKeyUp(string key)
    {
        if (!Enum.TryParse<KeyCode>(key, true, out var kc)) return false;
        return UnityInput.Current.GetKeyUp(kc);
    }
}