﻿using System;
using System.Collections.Generic;
using BepInEx;
using CartoonFX;
using CSTI_LuaActionSupport.Helper;
using CSTI_LuaActionSupport.LuaCodeHelper;
using CSTI_LuaActionSupport.VFX;
using DG.Tweening;
using NLua;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CSTI_LuaActionSupport.AllPatcher;

public static class LuaAnim
{
    // ReSharper disable once IdentifierTypo
    public static readonly AssetBundle? CFXR;

    static LuaAnim()
    {
        try
        {
            CFXR = AssetBundle.LoadFromStream(EmbeddedResources.CFXRBundle);
        }
        catch (Exception e)
        {
            Debug.LogWarning(e);
        }
    }

    public static readonly Dictionary<string, Action<Object, LuaTable>> AnimFunc = new();

    public interface ITransProvider
    {
        public Transform? Transform { get; }
    }

    public class TransProvider : ITransProvider
    {
        public TransProvider(Transform? transform)
        {
            Transform = transform;
        }

        public Transform? Transform { get; }
    }

    public static Transform? _CurMouse;
    public static Transform? _SpecialPos;

    [LuaFunc]
    public static ITransProvider? SpecialScreenPos(Vector2 pos)
    {
        if (Camera.main == null) return null;
        if (_SpecialPos == null)
        {
            var gameObject = new GameObject("SpecialPos_TransProvider");
            _SpecialPos = gameObject.transform;
        }

        _SpecialPos.position =
            (Vector2)Camera.main.ViewportToWorldPoint(new Vector3(pos.x, pos.y, Camera.main.nearClipPlane));
        return new TransProvider(_SpecialPos);
    }

    [LuaFunc]
    public static ITransProvider? CurMouse()
    {
        if (Camera.main == null) return null;
        if (_CurMouse == null)
        {
            var gameObject = new GameObject("Cursor_TransProvider");
            _CurMouse = gameObject.transform;
        }

        var currentMousePosition = UnityInput.Current.mousePosition;
        var mouseWorldPoint = Camera.main.ScreenToWorldPoint(currentMousePosition);
        mouseWorldPoint.z = 0;
        _CurMouse.position = mouseWorldPoint;

        return new TransProvider(_CurMouse);
    }

    [TestCode("""
              LuaAnim.GenCFXR(receive, "CFXR3 Fire Explosion B", true, nil, {
                  animatedLights = {
                      {
                          animateIntensity = true,
                          intensityEnd = 0.5,
                          intensityDuration = 1,
              
                          fadeIn = true,
                          fadeInDuration = 0.2,
                          fadeOut = true,
                          fadeOutDuration = 0.2,
                      }
                  }
              })
              """)]
    [LuaFunc]
    // ReSharper disable once IdentifierTypo
    public static ITransProvider GenCFXR(ITransProvider transProvider, string fxName, bool moveWithProvider = false,
        float? time = null, LuaTable? ext = null)
    {
        if (CFXR == null) return transProvider;
        var transform = transProvider.Transform;
        if (transform == null) return transProvider;
        if (CFXR.LoadAsset<GameObject>(fxName) is not { } fxPrefab) return transProvider;
        var fx = Object.Instantiate(fxPrefab, transform.position, Quaternion.identity);
        if (moveWithProvider)
        {
            var followTransform = fx.AddComponent<FollowTransform>();
            followTransform.Follow = transform;
            followTransform.BeginFollow();
        }

        var cfxrEffect = fx.AddComponent<CFXR_Effect>();
        cfxrEffect.fadeOutReference = fx.GetComponent<ParticleSystem>();
        if (time != null)
        {
            cfxrEffect.clearBehavior = CFXR_Effect.ClearBehavior.None;
            fx.transform.DOBlendableMoveBy(Vector3.zero, time.Value).onComplete += () => { Object.Destroy(fx); };
        }

        if (ext != null)
        {
            var FollowMouse = false;
            FollowMouse.TryModBy(ext[nameof(FollowMouse)]);
            if (FollowMouse && !moveWithProvider)
            {
                fx.AddComponent<FollowMouse>();
            }

            var loop = false;
            if (ext[nameof(CFXR_Effect.animatedLights)] is LuaTable _animatedLights)
            {
                var animatedLights = new List<CFXR_Effect.AnimatedLight>();
                for (var i = 1;; i++)
                {
                    if (_animatedLights[i] is not LuaTable _animatedLight) break;
                    var animatedLight = new CFXR_Effect.AnimatedLight
                    {
                        light = fx.GetComponentInChildren<Light>()
                    };
                    animatedLight.loop.TryModBy(_animatedLight[nameof(animatedLight.loop)]);
                    loop |= animatedLight.loop;

                    animatedLight.animateIntensity.TryModBy(_animatedLight[nameof(animatedLight.animateIntensity)]);
                    animatedLight.intensityStart.TryModBy(_animatedLight[nameof(animatedLight.intensityStart)]);
                    animatedLight.intensityEnd.TryModBy(_animatedLight[nameof(animatedLight.intensityEnd)]);
                    animatedLight.intensityDuration.TryModBy(_animatedLight[nameof(animatedLight.intensityDuration)]);
                    animatedLight.perlinIntensity.TryModBy(_animatedLight[nameof(animatedLight.perlinIntensity)]);
                    animatedLight.perlinIntensitySpeed.TryModBy(
                        _animatedLight[nameof(animatedLight.perlinIntensitySpeed)]);

                    animatedLight.fadeIn.TryModBy(_animatedLight[nameof(animatedLight.fadeIn)]);
                    animatedLight.fadeInDuration.TryModBy(_animatedLight[nameof(animatedLight.fadeInDuration)]);

                    animatedLight.fadeOut.TryModBy(_animatedLight[nameof(animatedLight.fadeOut)]);
                    animatedLight.fadeOutDuration.TryModBy(_animatedLight[nameof(animatedLight.fadeOutDuration)]);

                    animatedLight.animateRange.TryModBy(_animatedLight[nameof(animatedLight.animateRange)]);
                    animatedLight.rangeStart.TryModBy(_animatedLight[nameof(animatedLight.rangeStart)]);
                    animatedLight.rangeEnd.TryModBy(_animatedLight[nameof(animatedLight.rangeEnd)]);
                    animatedLight.rangeDuration.TryModBy(_animatedLight[nameof(animatedLight.rangeDuration)]);
                    animatedLight.perlinRange.TryModBy(_animatedLight[nameof(animatedLight.perlinRange)]);
                    animatedLight.perlinRangeSpeed.TryModBy(_animatedLight[nameof(animatedLight.perlinRangeSpeed)]);

                    animatedLight.animateColor.TryModBy(_animatedLight[nameof(animatedLight.animateColor)]);
                    animatedLight.colorGradient = new Gradient();
                    animatedLight.colorGradient.TryModBy(_animatedLight[nameof(animatedLight.colorGradient)]);
                    animatedLight.colorDuration.TryModBy(_animatedLight[nameof(animatedLight.colorDuration)]);
                    animatedLight.perlinColor.TryModBy(_animatedLight[nameof(animatedLight.perlinColor)]);
                    animatedLight.perlinColorSpeed.TryModBy(_animatedLight[nameof(animatedLight.perlinColorSpeed)]);

                    animatedLights.Add(animatedLight);
                }

                cfxrEffect.animatedLights = animatedLights.ToArray();
            }

            if (loop)
            {
                if (moveWithProvider)
                {
                    cfxrEffect.clearBehavior = CFXR_Effect.ClearBehavior.None;
                }

                var cfxrAnimLoop = fx.AddComponent<CfxrAnimLoop>();
                cfxrAnimLoop.effect = cfxrEffect;
                cfxrAnimLoop.LoopTime = ext["LoopTime"].TryNum<float>();
            }
        }
        else
        {
            cfxrEffect.animatedLights = new[]
            {
                new CFXR_Effect.AnimatedLight
                {
                    fadeIn = true,
                    fadeInDuration = 0.2f,
                    fadeOut = true,
                    fadeOutDuration = 0.2f,
                }
            };
        }

        return new TransProvider(fx.transform);
    }

    [LuaFunc]
    public static void DoCommonPlay(ITransProvider transProvider, string funcId, LuaTable args)
    {
        var transform = transProvider.Transform;
        if (transform == null) return;
        if (AnimFunc.TryGetValue(funcId, out var func))
        {
            func(transform, args);
        }
    }
}