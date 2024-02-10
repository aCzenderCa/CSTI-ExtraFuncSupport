using System;
using BepInEx;
using CartoonFX;
using CSTI_LuaActionSupport.AllPatcher;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CSTI_LuaActionSupport.VFX;

public class CfxrAnimLoop : MonoBehaviour
{
    public CFXR_Effect? effect;
    public MonoBehaviour? loopObj;
    private ParticleSystem? _rootParticleSystem;
    public float? LoopTime = null;
    public float curPlayTime;

    private void Update()
    {
        if (loopObj != null && !loopObj.gameObject.activeInHierarchy)
        {
            effect = null;
        }

        if (effect != null)
        {
            curPlayTime += Time.deltaTime;
            if (_rootParticleSystem == null)
            {
                _rootParticleSystem = GetComponent<ParticleSystem>();
            }

            if (curPlayTime >= LoopTime)
            {
                curPlayTime = 0;
                effect.ResetState();
                _rootParticleSystem.Play();
            }

            if (!_rootParticleSystem.IsAlive(true))
            {
                effect.ResetState();
                _rootParticleSystem.Play();
            }
        }
    }
}

public class FollowMouse : MonoBehaviour
{
    public Vector2 withOffset;

    private void Update()
    {
        if (Camera.main == null) return;
        var currentMousePosition = UnityInput.Current.mousePosition;
        var mouseWorldPoint = (Vector2)Camera.main.ScreenToWorldPoint(currentMousePosition);
        if (withOffset == Vector2.zero)
        {
            transform.position = mouseWorldPoint;
        }
        else if (LuaAnim.SpecialScreenPos(withOffset) is { Transform: { } tr })
        {
            transform.position = mouseWorldPoint + (Vector2)tr.position;
        }
        else
        {
            transform.position = mouseWorldPoint;
        }
    }
}

public class FollowTransform : MonoBehaviour
{
    public Transform? Follow;
    public bool followInit;
    public Vector2 withOffset;

    public void BeginFollow()
    {
        transform.position = Follow!.position;
        followInit = true;
    }

    private void Update()
    {
        if (Follow != null && Follow.gameObject.activeInHierarchy)
        {
            if (withOffset == Vector2.zero)
            {
                transform.position = Follow.position;
            }
            else if (LuaAnim.SpecialScreenPos(withOffset) is { Transform: { } tr })
            {
                transform.position = Follow.position + (Vector3)(Vector2)tr.position;
            }
            else
            {
                transform.position = Follow.position;
            }
        }
        else if (followInit)
        {
            Destroy(gameObject);
        }
    }
}