using System;
using System.Collections;
using System.Collections.Generic;
using CSTI_LuaActionSupport.Attr;
using CSTI_LuaActionSupport.LuaCodeHelper;
using LitJson;
using UnityEngine;
using Random = UnityEngine.Random;

// ReSharper disable InconsistentNaming

namespace CSTI_LuaActionSupport.DataStruct;

public class CommonCardGen : ScriptableObject, IModLoaderJsonObj
{
    [Serializable]
    public class CardGenPosProvider : IModLoaderJsonObj
    {
        [Note("卡牌所需要在的槽位类型")] [DefaultFieldVal(SlotsTypes.Base)]
        public SlotsTypes SlotsType;

        [Note("是否需要查找满足条件的卡牌并生成在其上，如查找碗并生成水")] public bool NeedFindCard;
        [Note("卡牌查找器")] public CommonCardFinder CardFinder = null!;

        public void CreateByJson(string json)
        {
            JsonUtility.FromJsonOverwrite(json, this);
            var jsonData = JsonMapper.ToObject(json);
            if (jsonData.ContainsKey(nameof(CardFinder)))
            {
                CardFinder = JsonUtility.FromJson<CommonCardFinder>(jsonData[nameof(CardFinder)].ToJson());
            }
        }
    }

    public static float TempChance;

    [Note("是否阻止蓝图被取消")] public bool bpDropDisableCancel;
    [Note("是否需要使用预初始化耐久")] public bool NeedInitDurabilities;

    [Note("初始化的耐久，内部不含note")] [ClsExport(typeof(TransferedDurabilities))]
    public TransferedDurabilities? InitDurabilities;

    [Note("基础掉落率，1.0=100%，通过通用id：Sp|DropChance计算动态值")]
    public float BaseChance;

    [Note("卡牌生成位置，可以实现生成到容器内")] public CardGenPosProvider? PosProvider;
    [Note("要生成的卡牌")] public CardData? CardData;
    [Note("卡牌上伴随生成的流体卡")] public CardData? WithLiquidCard;
    [Note("卡牌生成数量，x为最小值，y为最大值，在范围内随机")] public Vector2Int Count;

    [Note("计算动态掉落率用，也可以修改卡牌（搜索到的卡或exp卡）耐久和状态值")]
    public SimpleVarModEntry[] ChanceMod = [];

    [Note("设置为-10086不强制初始化蓝图建造阶段")] [DefaultFieldVal(-10086)]
    public int BpStage;

    [Note("在卡牌被生成时执行在卡牌上的action")] public CardActionPack? ActOnCardGen;

    public void Act()
    {
        if (CardData == null) return;
        TempChance = BaseChance;
        InGameCardBase? dropOn = null;
        if (PosProvider?.NeedFindCard is true)
        {
            dropOn = PosProvider.CardFinder.FindFirst();
        }

        if (dropOn != null)
        {
            foreach (var modEntry in ChanceMod)
            {
                modEntry.Act(GameManager.Instance, dropOn, null);
            }
        }
        else
        {
            foreach (var modEntry in ChanceMod)
            {
                modEntry.Act(GameManager.Instance, GameManager.Instance.CurrentExplorableCard, null);
            }
        }

        if (Random.value > TempChance)
        {
            return;
        }

        var genCount = Random.Range(Count.x, Count.y + 1);
        var forceDur = NeedInitDurabilities ? InitDurabilities : null;
        var forceLiq = SpawningLiquid.Empty;
        forceLiq.StayEmpty = WithLiquidCard;
        forceLiq.LiquidCard = WithLiquidCard;
        var forceBpData = new BlueprintSaveData(null, null) {CurrentStage = BpStage};
        List<CollectionDropsSaveData>? initData = null;
        var initDataNode = new DataNodeTableAccessBridge([]);
        if (CardData.CardType == CardTypes.Blueprint && bpDropDisableCancel)
        {
            initDataNode[nameof(bpDropDisableCancel)] = true;
        }

        if (ActOnCardGen != null)
        {
            initDataNode[nameof(ActOnCardGen)] = ActOnCardGen.uid;
        }

        if (initDataNode.Count > 0)
        {
            initData = [initDataNode.IntoSave()];
        }

        var enumerators = new List<IEnumerator>();
        for (var i = 0; i < genCount; i++)
        {
            GameManager.Instance.MoniAddCard(CardData, dropOn, forceDur!,
                    new SlotInfo(PosProvider?.SlotsType ?? SlotsTypes.Base, -2), forceBpData, true,
                    forceLiq, new Vector2Int(GameManager.Instance.CurrentTickInfo.z, -1), initData)
                .Add2Li(enumerators);
        }

        if (enumerators.Count > 0)
        {
            enumerators.Add2AllEnumerators(PriorityEnumerators.Normal);
        }
    }

    public void CreateByJson(string json)
    {
        JsonUtility.FromJsonOverwrite(json, this);
        var jsonData = JsonMapper.ToObject(json);
        if (jsonData.ContainsKey(nameof(PosProvider)))
        {
            PosProvider = new CardGenPosProvider();
            PosProvider.CreateByJson(jsonData[nameof(PosProvider)].ToJson());
        }

        if (jsonData.ContainsKey(nameof(InitDurabilities)))
        {
            InitDurabilities =
                JsonUtility.FromJson<TransferedDurabilities>(jsonData[nameof(InitDurabilities)].ToJson());
        }
    }
}