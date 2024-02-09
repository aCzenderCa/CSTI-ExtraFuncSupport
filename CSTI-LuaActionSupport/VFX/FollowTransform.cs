using System;
using BepInEx;
using CartoonFX;
using UnityEngine;

namespace CSTI_LuaActionSupport.VFX;

public class CfxrAnimLoop : MonoBehaviour
{
    public CFXR_Effect? effect;
    private ParticleSystem? _rootParticleSystem;
    public float? loopTime = null;
    public float curPlayTime;

    private void Update()
    {
        if (effect != null)
        {
            curPlayTime += Time.deltaTime;
            if (_rootParticleSystem == null)
            {
                _rootParticleSystem = GetComponent<ParticleSystem>();
            }

            if (curPlayTime >= loopTime)
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
    private void Update()
    {
        if (Camera.main == null) return;
        var currentMousePosition = UnityInput.Current.mousePosition;
        var mouseWorldPoint = Camera.main.ScreenToWorldPoint(currentMousePosition);
        mouseWorldPoint.z = 0;
        transform.position = mouseWorldPoint;
    }
}

public class FollowTransform : MonoBehaviour
{
    public Transform? Follow;
    public bool followInit;

    public void BeginFollow()
    {
        transform.position = Follow!.position;
        followInit = true;
    }

    private void Update()
    {
        if (Follow != null && Follow.gameObject.activeInHierarchy)
        {
            transform.position = Follow.position;
        }
        else if (followInit)
        {
            Destroy(gameObject);
        }
    }
}