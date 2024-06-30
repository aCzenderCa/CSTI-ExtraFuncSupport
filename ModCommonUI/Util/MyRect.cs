using System;
using UnityEngine;

namespace ModCommonUI.Util;

[Serializable]
public class MyRect
{
    public Vector2 pos;
    public Vector2 size;

    public static Rect operator *(Rect rect, MyRect facRect)
    {
        var pos = rect.position + facRect.pos * rect.size;
        var size = rect.size * facRect.size;
        return new Rect(pos, size);
    }

    public static MyRect CreateByFac(Vector2 pos, Vector2 size)
    {
        return new MyRect {pos = pos, size = size};
    }
}