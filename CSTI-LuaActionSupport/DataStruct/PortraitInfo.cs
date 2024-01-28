using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CSTI_LuaActionSupport.DataStruct;

public class PortraitInfo : ScriptableObject
{
    public static readonly Dictionary<string, PortraitInfo> PortraitInfos = new();
    public static readonly Queue<PortraitInfo> LoadQueue = new();

    public static void LoadAllInQueue()
    {
        while (LoadQueue.Count > 0)
        {
            var portraitInfo = LoadQueue.Dequeue();
            if (!PortraitInfos.ContainsKey(portraitInfo.PortraitID))
            {
                PortraitInfos[portraitInfo.PortraitID] = portraitInfo;
                portraitInfo.LoadSprite();
            }
        }
    }

    private void OnEnable()
    {
        LoadQueue.Enqueue(this);
    }

    public void LoadSprite()
    {
        if (CachePortraitSprite == null)
        {
            SpriteDict.TryGetValue(PortraitSprite, out CachePortraitSprite);
            if (CachePortraitSprite == null)
            {
                CachePortraitSprite = Resources.FindObjectsOfTypeAll<Sprite>()
                    .FirstOrDefault(sprite => sprite.name == PortraitSprite);
            }
        }
    }

    public string PortraitID = "";
    public string PortraitSprite = "";
    [NonSerialized] public Sprite? CachePortraitSprite;
}