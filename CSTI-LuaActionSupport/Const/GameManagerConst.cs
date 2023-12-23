using System;
using System.Collections.Generic;
using UnityEngine;

namespace CSTI_LuaActionSupport.Const;

public static class GameManagerConst
{
    public static readonly Type[] FullAddCard =
    [
        typeof(CardData), typeof(SlotInfo), typeof(CardData), typeof(InGameCardBase), typeof(bool),
        typeof(TransferedDurabilities), typeof(List<CollectionDropsSaveData>), typeof(List<StatTriggeredActionStatus>),
        typeof(ExplorationSaveData), typeof(BlueprintSaveData),
        typeof(Vector3), typeof(bool), typeof(SpawningLiquid), typeof(bool), typeof(bool), typeof(Vector2Int),
        typeof(CardData), typeof(int), typeof(int), typeof(bool), typeof(string)
    ];
}