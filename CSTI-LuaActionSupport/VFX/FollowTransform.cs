using BepInEx;
using UnityEngine;

namespace CSTI_LuaActionSupport.VFX;

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

    private void Update()
    {
        if (Follow != null)
        {
            transform.position = Follow.position;
        }
    }
}