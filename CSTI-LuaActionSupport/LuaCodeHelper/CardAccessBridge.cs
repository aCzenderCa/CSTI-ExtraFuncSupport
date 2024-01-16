using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CSTI_LuaActionSupport.AllPatcher;
using CSTI_LuaActionSupport.Helper;
using CSTI_LuaActionSupport.UIStruct;
using gfoidl.Base64;
using NLua;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CSTI_LuaActionSupport.LuaCodeHelper;

public class CardAccessBridge : LuaAnim.ITransProvider
{
    public readonly InGameCardBase? CardBase;

    public static readonly Regex KVDataCheck = new(@"zender\.luaSupportData\.\{(?<key>.+?)\}:\{(?<val>.+?)\}");

    public SimpleUniqueAccess? CardModel => CardBase != null ? new SimpleUniqueAccess(CardBase.CardModel) : null;

    public bool IsEquipped => CardBase != null && CardBase.CurrentSlot.SlotType == SlotsTypes.Equipment;
    public bool IsInHand => CardBase != null && CardBase.CurrentSlot.SlotType == SlotsTypes.Hand;
    public bool IsInBase => CardBase != null && CardBase.CurrentSlot.SlotType == SlotsTypes.Base;
    public bool IsInLocation => CardBase != null && CardBase.CurrentSlot.SlotType == SlotsTypes.Location;
    public bool IsInBackground => CardBase != null && CardBase.InBackground;
    public bool IsInstanceEnv => CardBase != null && CardBase.CardModel && CardBase.CardModel.InstancedEnvironment;
    public int TravelIndex => CardBase != null ? CardBase.TravelCardIndex : -10086;

    public CardAccessBridge? CurrentContainer => CardBase != null && CardBase.CurrentContainer != null
        ? new CardAccessBridge(CardBase.CurrentContainer)
        : null;

    public bool MoveToSlot(string slotType)
    {
        if (CardBase == null || GraphicsManager.Instance == null) return false;
        if (SlotType == slotType) return true;
        switch (slotType)
        {
            case nameof(SlotsTypes.Equipment):
                GraphicsManager.Instance.MoveCardToSlot(CardBase, new SlotInfo(SlotsTypes.Equipment, -2), true, false);
                break;
            case nameof(SlotsTypes.Base):
                GraphicsManager.Instance.MoveCardToSlot(CardBase, new SlotInfo(SlotsTypes.Base, -2), true, false);
                break;
            case nameof(SlotsTypes.Hand):
                GraphicsManager.Instance.MoveCardToSlot(CardBase, new SlotInfo(SlotsTypes.Hand, -2), true, false);
                break;
            case nameof(SlotsTypes.Location):
                GraphicsManager.Instance.MoveCardToSlot(CardBase, new SlotInfo(SlotsTypes.Location, -2), true, false);
                break;
            default:
                return false;
        }

        return true;
    }

    public bool MoveTo(CardAccessBridge cardAccessBridge)
    {
        if (CardBase == null || CardBase.CardModel == null) return false;
        if (cardAccessBridge.CardBase == null || cardAccessBridge.CardBase.CardModel == null) return false;
        if (cardAccessBridge.CardBase.IsInventoryCard)
        {
            if (CardBase == cardAccessBridge.CardBase) return false;
            var indexForInventory = cardAccessBridge.CardBase.GetIndexForInventory(0, CardBase.CardModel,
                CardBase.ContainedLiquidModel,
                CardBase.CurrentWeight);
            if (indexForInventory == -1) return false;
            if (CardBase.CurrentSlot) CardBase.CurrentSlot.RemoveSpecificCard(CardBase, true);
            if (CardBase.CurrentContainer) CardBase.CurrentContainer.RemoveCardFromInventory(CardBase);
            CardBase.CurrentContainer = cardAccessBridge.CardBase;
            CardBase.SetSlot(null, true);
            CardBase.CurrentSlotInfo = new SlotInfo(SlotsTypes.Inventory, indexForInventory);

            cardAccessBridge.CardBase.AddCardToInventory(CardBase, indexForInventory);
            if (CardBase.CardVisuals) CardBase.BlocksRaycasts = !CardBase.CardVisuals.DontBlockRaycasts;
            else CardBase.BlocksRaycasts = true;

            SoundManager.Instance.PerformCardAppearanceSound(CardBase.CardModel.WhenCreatedSounds);
            return true;
        }

        if (cardAccessBridge.CardBase.IsLiquidContainer && CardBase.IsLiquid)
        {
            if (!cardAccessBridge.CardBase.CanReceiveLiquid(CardBase)) return false;
            if (CardBase.CurrentContainer) CardBase.CurrentContainer.SetContainedLiquid(null, false, false);
            if (cardAccessBridge.CardBase.LiquidEmpty)
            {
                cardAccessBridge.CardBase.SetContainedLiquid(CardBase, false, false);
                CardBase.CurrentContainer = cardAccessBridge.CardBase;
                CardBase.SetSlot(null, true);
                CardBase.SetParent(cardAccessBridge.CardBase.CurrentParentObject, true);
                CardBase.CurrentSlotInfo = cardAccessBridge.CardBase.CurrentSlotInfo;
            }
            else
            {
                cardAccessBridge.CardBase.ContainedLiquid.CurrentLiquidQuantity += CardBase.CurrentLiquidQuantity;
                Remove(false);
            }

            return true;
        }

        if (cardAccessBridge.CardBase.CurrentSlot.CanReceiveCard(CardBase, false))
        {
            cardAccessBridge.CardBase.CurrentSlot.AssignCard(CardBase, true);
            return true;
        }

        if (GraphicsManager.Instance)
        {
            GraphicsManager.Instance.MoveCardToSlot(CardBase,
                new SlotInfo(cardAccessBridge.CardBase.CurrentSlotInfo.SlotType, -2), true, false);
            return true;
        }

        return false;
    }

    public void AddAnim(LuaTable? animList, LuaTable? animTimeList)
    {
        if (CardBase == null) return;
        RemoveAnim();
        var animCard = CardBase.gameObject.AddComponent<AnimCard>();
        animCard.Init(animList.ToList<string>(), animTimeList.ToList(o => o.TryNum<float>() ?? 0));
    }

    public bool RemoveAnim()
    {
        if (CardBase == null || (CardBase.GetComponent<AnimCard>() is var animCard && animCard == null)) return false;
        Object.DestroyImmediate(animCard);
        return true;
    }

    public void UpdateVisuals()
    {
        if (CardBase == null) return;
        if (CardBase.CardVisuals == null) return;
        CardBase.CardVisuals.Setup(CardBase);
    }

    public bool CheckInventory(bool useAll, params string[] uid)
    {
        return useAll ? uid.All(s => HasInInventory(s)) : uid.Any(s => HasInInventory(s));
    }

    public bool CheckTagInventory(bool useAll, params string[] tags)
    {
        return useAll ? tags.All(s => HasTagInInventory(s)) : tags.Any(s => HasTagInInventory(s));
    }

    public bool CheckRegexTagInventory(bool useAll, params string[] regexTags)
    {
        return useAll ? regexTags.All(s => HasRegexTagInInventory(s)) : regexTags.Any(s => HasRegexTagInInventory(s));
    }

    public bool HasInInventory(string uid, long needCount = 0)
    {
        if (CardBase == null) return false;
        if (!CardBase.IsInventoryCard) return false;
        if (UniqueIDScriptable.GetFromID<CardData>(uid) is not { } card) return false;
        if (needCount <= 0)
        {
            return CardBase.CardsInInventory.Any(slot => slot.CardModel.UniqueID == uid);
        }

        return CardBase.InventoryCount(card) >= needCount;
    }

    public bool HasTagInInventory(string tag, long needCount = 0)
    {
        if (CardBase == null) return false;
        if (!CardBase.IsInventoryCard) return false;
        if (needCount <= 0)
        {
            return CardBase.CardsInInventory.Any(slot => HasTag(slot.CardModel, tag));
        }

        var sum = CardBase.CardsInInventory.Where(slot => HasTag(slot.CardModel, tag)).Sum(slot => slot.CardAmt);
        return sum >= needCount;
    }

    public bool HasRegexTagInInventory(string regexTag, long needCount = 0)
    {
        if (CardBase == null) return false;
        if (!CardBase.IsInventoryCard) return false;
        if (needCount <= 0)
        {
            return CardBase.CardsInInventory.Any(slot => HasRegexTag(slot.CardModel, regexTag));
        }

        var sum = CardBase.CardsInInventory.Where(slot => HasRegexTag(slot.CardModel, regexTag))
            .Sum(slot => slot.CardAmt);
        return sum >= needCount;
    }

    public CardAccessBridge? LiquidInventory()
    {
        if (CardBase == null) return null;
        if (CardBase.LiquidEmpty) return null;
        return new CardAccessBridge(CardBase.ContainedLiquid);
    }

    public CardAccessBridge[]? this[long index]
    {
        get
        {
            if (CardBase == null) return null;
            if (!CardBase.IsInventoryCard) return null;
            if (index < 0 || index >= CardBase.CardsInInventory.Count) return null;
            return CardBase.CardsInInventory[Mathf.RoundToInt(index)].AllCards
                .Select(cardBase => new CardAccessBridge(cardBase))
                .ToArray();
        }
    }

    public int InventorySlotCount => CardBase && CardBase!.IsInventoryCard ? CardBase.CardsInInventory.Count : 0;

    public object? this[string key]
    {
        get
        {
            if (CardBase == null) return null;
            if (CardBase.DroppedCollections.TryGetValue(key, out var item))
            {
                return item;
            }

            var dictionary = CardBase.DroppedCollections.Keys.Select<string, KeyValuePair<string, string>?>(
                    s =>
                    {
                        var match = KVDataCheck.Match(s);
                        if (match.Success)
                        {
                            return new KeyValuePair<string, string>(match.Groups["key"].ToString(),
                                match.Groups["val"].ToString());
                        }

                        return null;
                    }).Where(pair => pair != null)
                .ToDictionary(pair => pair!.Value.Key, pair => pair!.Value.Value);
            if (!dictionary.TryGetValue(key, out var item1)) return null;
            if (double.TryParse(item1, out var result))
            {
                return result;
            }

            return item1;
        }

        set
        {
            if (CardBase == null) return;
            if (value is double i)
            {
                CardBase.DroppedCollections[key] = new Vector2Int(Mathf.RoundToInt((float) i), 0);
            }
            else
            {
                var strVal = value is LuaTable table
                    ? DebugBridge.TableToString(table)
                    : value;
                CardBase.DroppedCollections[
                    $"zender.luaSupportData.{{{key}}}:{{{strVal}}}"] = Vector2Int.one;
            }
        }
    }

    public string SlotType => CardBase != null ? CardBase.CurrentSlot.SlotType.ToString() : "nil";

    public string CardType => CardBase != null ? CardBase.CardModel.CardType.ToString() : "nil";

    public float Weight => CardBase != null ? CardBase.CurrentWeight : 0;

    public bool HasTag(string tag)
    {
        if (CardBase == null)
        {
            return false;
        }

        return HasTag(CardBase.CardModel, tag);
    }

    public static bool HasTag(CardData cardData, string tag)
    {
        if (cardData == null)
        {
            return false;
        }

        return cardData.CardTags.Any(cardTag =>
            cardTag.name == tag || cardTag.InGameName.DefaultText == tag);
    }

    public bool HasRegexTag(string regexTag)
    {
        if (CardBase == null)
        {
            return false;
        }

        return HasRegexTag(CardBase.CardModel, regexTag);
    }

    public static bool HasRegexTag(CardData cardData, string regexTag)
    {
        if (cardData == null)
        {
            return false;
        }

        var tag = new Regex(regexTag);

        return cardData.CardTags.Any(cardTag =>
            tag.IsMatch(cardTag.name) || tag.IsMatch(cardTag.InGameName.DefaultText));
    }

    public int TravelCardIndex => CardBase != null ? CardBase.TravelCardIndex : -1;

    public string Id => CardBase != null ? CardBase.CardModel.UniqueID : "";

    private DataNode? _dataNode;
    private static readonly Regex DataNodeReg = new(@"^LNbt\|\>(?<nbt>.+?)\<\|$");

    public CardAccessBridge(InGameCardBase? CardBase)
    {
        this.CardBase = CardBase;
    }

    // language=Lua
    [TestCode("""
              receive:InitData()
              local d = receive.Data
              if d["i"] == nil then
                d["i"] = 10
              else
                d["i"] = d["i"] + 1
              end
              receive:SaveData()
              """)]
    public DataNodeTableAccessBridge? Data
    {
        get
        {
            if (CardBase == null) return null;
            if (_dataNode is {NodeType: DataNode.DataNodeType.Table}) goto end;
            foreach (var key in CardBase.DroppedCollections.Keys)
            {
                if (DataNodeReg.Match(key) is not { } match) continue;
                using var memoryStream =
                    new MemoryStream(Base64.Default.Decode(match.Groups["nbt"].Value.AsSpan()));
                var binaryReader = new BinaryReader(memoryStream);
                _dataNode = DataNode.Load(binaryReader);
                break;
            }

            end: ;
            return _dataNode is {NodeType: DataNode.DataNodeType.Table}
                ? new DataNodeTableAccessBridge(_dataNode.Value.table)
                : null;
        }
    }

    public void InitData(DataNodeTableAccessBridge? initData = null)
    {
        if (Data == null)
        {
            _dataNode = initData?.Table == null
                ? new DataNode(new Dictionary<string, DataNode>())
                : new DataNode(initData.Table);
        }
    }

    public void SaveData()
    {
        if (CardBase == null) return;
        if (_dataNode is not {NodeType: DataNode.DataNodeType.Table} dataNode) return;
        var memoryStream = new MemoryStream();
        var binaryWriter = new BinaryWriter(memoryStream);
        dataNode.Save(binaryWriter);
        var array = memoryStream.ToArray();
        memoryStream.Close();
        if (CardBase.DroppedCollections.FirstOrDefault(pair => DataNodeReg.IsMatch(pair.Key)) is {Key: not null} p)
        {
            CardBase.DroppedCollections.Remove(p.Key);
        }

        CardBase.DroppedCollections[$"LNbt|>{Base64.Default.Encode(array)}<|"] = Vector2Int.zero;
    }

    public float Spoilage
    {
        get => CardBase != null ? CardBase.CurrentSpoilage : 0;
        set => ModifyDurability(value, DurabilitiesTypes.Spoilage).Add2AllEnumerators(PriorityEnumerators.High);
    }

    public float Usage
    {
        get => CardBase != null ? CardBase.CurrentUsageDurability : 0;
        set => ModifyDurability(value, DurabilitiesTypes.Usage).Add2AllEnumerators(PriorityEnumerators.High);
    }

    public float Fuel
    {
        get => CardBase != null ? CardBase.CurrentFuel : 0;
        set => ModifyDurability(value, DurabilitiesTypes.Fuel).Add2AllEnumerators(PriorityEnumerators.High);
    }

    public float Progress
    {
        get => CardBase != null ? CardBase.CurrentProgress : 0;
        set => ModifyDurability(value, DurabilitiesTypes.Progress).Add2AllEnumerators(PriorityEnumerators.High);
    }

    public float Special1
    {
        get => CardBase != null ? CardBase.CurrentSpecial1 : 0;
        set => ModifyDurability(value, DurabilitiesTypes.Special1).Add2AllEnumerators(PriorityEnumerators.High);
    }

    public float Special2
    {
        get => CardBase != null ? CardBase.CurrentSpecial2 : 0;
        set => ModifyDurability(value, DurabilitiesTypes.Special2).Add2AllEnumerators(PriorityEnumerators.High);
    }

    public float Special3
    {
        get => CardBase != null ? CardBase.CurrentSpecial3 : 0;
        set => ModifyDurability(value, DurabilitiesTypes.Special3).Add2AllEnumerators(PriorityEnumerators.High);
    }

    public float Special4
    {
        get => CardBase != null ? CardBase.CurrentSpecial4 : 0;
        set => ModifyDurability(value, DurabilitiesTypes.Special4).Add2AllEnumerators(PriorityEnumerators.High);
    }

    public float LiquidQuantity
    {
        get => CardBase != null && CardBase.IsLiquid ? CardBase.CurrentLiquidQuantity : 0;
        set => ModifyDurability(value, DurabilitiesTypes.Liquid).Add2AllEnumerators(PriorityEnumerators.High);
    }

    private IEnumerator ModifyDurability(float val, DurabilitiesTypes types)
    {
        if (CardBase == null) yield break;
        var cardBaseCardModel = CardBase.CardModel;
        Coroutine? coroutine;
        switch (types)
        {
            case DurabilitiesTypes.Spoilage:
                if (inner_modify(cardBaseCardModel.SpoilageTime, CardBase.CurrentSpoilage, CardBase,
                        ref CardBase.CurrentSpoilage, ref CardBase.SpoilEmpty, ref CardBase.SpoilFull,
                        out coroutine))
                    yield return coroutine;
                break;
            case DurabilitiesTypes.Usage:
                if (inner_modify(cardBaseCardModel.UsageDurability, CardBase.CurrentUsageDurability, CardBase,
                        ref CardBase.CurrentUsageDurability, ref CardBase.UsageEmpty, ref CardBase.UsageFull,
                        out coroutine))
                    yield return coroutine;
                break;
            case DurabilitiesTypes.Fuel:
                if (inner_modify(cardBaseCardModel.FuelCapacity, CardBase.CurrentFuel, CardBase,
                        ref CardBase.CurrentFuel, ref CardBase.FuelEmpty, ref CardBase.FuelFull,
                        out coroutine))
                    yield return coroutine;
                break;
            case DurabilitiesTypes.Progress:
                if (inner_modify(cardBaseCardModel.Progress, CardBase.CurrentProgress, CardBase,
                        ref CardBase.CurrentProgress, ref CardBase.ProgressEmpty, ref CardBase.ProgressFull,
                        out coroutine))
                    yield return coroutine;
                break;
            case DurabilitiesTypes.Liquid:
                if (!CardBase.IsLiquid)
                {
                    yield break;
                }

                var rawLiquidEmpty = CardBase.LiquidEmpty;
                CardBase.CurrentLiquidQuantity += val;
                CardBase.CurrentLiquidQuantity = CardBase.CurrentMaxLiquidQuantity > 0f
                    ? Mathf.Clamp(CardBase.CurrentLiquidQuantity, 0f, CardBase.CurrentMaxLiquidQuantity)
                    : Mathf.Min(CardBase.CurrentLiquidQuantity, 0f);
                CardBase.WeightHasChanged();
                if (!rawLiquidEmpty && CardBase.LiquidEmpty)
                {
                    yield return GameManager.PerformActionAsEnumerator(CardData.OnEvaporatedAction, CardBase, false);
                }

                break;
            case DurabilitiesTypes.Special1:
                if (inner_modify(cardBaseCardModel.SpecialDurability1, CardBase.CurrentSpecial1, CardBase,
                        ref CardBase.CurrentSpecial1, ref CardBase.Special1Empty, ref CardBase.Special1Full,
                        out coroutine))
                    yield return coroutine;
                break;
            case DurabilitiesTypes.Special2:
                if (inner_modify(cardBaseCardModel.SpecialDurability2, CardBase.CurrentSpecial2, CardBase,
                        ref CardBase.CurrentSpecial2, ref CardBase.Special2Empty, ref CardBase.Special2Full,
                        out coroutine))
                    yield return coroutine;
                break;
            case DurabilitiesTypes.Special3:
                if (inner_modify(cardBaseCardModel.SpecialDurability3, CardBase.CurrentSpecial3, CardBase,
                        ref CardBase.CurrentSpecial3, ref CardBase.Special3Empty, ref CardBase.Special3Full,
                        out coroutine))
                    yield return coroutine;
                break;
            case DurabilitiesTypes.Special4:
                if (inner_modify(cardBaseCardModel.SpecialDurability4, CardBase.CurrentSpecial4, CardBase,
                        ref CardBase.CurrentSpecial4, ref CardBase.Special4Empty, ref CardBase.Special4Full,
                        out coroutine))
                    yield return coroutine;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(types), types, null);
        }

        if (CardBase && CardBase.CardVisuals)
        {
            CardBase.CardVisuals.RefreshDurabilities();
        }
        else if (CardBase && CardBase.CurrentContainer && CardBase.CurrentContainer.CardVisuals)
        {
            CardBase.CurrentContainer.CardVisuals.RefreshDurabilities();
        }

        yield break;

        bool inner_modify(DurabilityStat durabilityStat, float rawCurrentSpoilage, InGameCardBase card,
            ref float durabilityStat_ref, ref bool durabilityStat_ref_empty, ref bool durabilityStat_ref_full,
            out Coroutine? _enumerator)
        {
            _enumerator = null;
            if (!durabilityStat)
            {
                return false;
            }

            if (val >= durabilityStat.Max)
            {
                durabilityStat_ref_empty = false;
                durabilityStat_ref = durabilityStat.Max;
                if (rawCurrentSpoilage < durabilityStat.Max)
                {
                    durabilityStat_ref_full = true;
                    _enumerator = Runtime.StartCoroutine(card.PerformDurabilitiesActions(true));
                    return true;
                }

                return false;
            }

            if (val <= 0)
            {
                durabilityStat_ref_full = false;
                durabilityStat_ref = 0;
                if (rawCurrentSpoilage > 0)
                {
                    durabilityStat_ref_empty = true;
                    _enumerator = Runtime.StartCoroutine(card.PerformDurabilitiesActions(true));
                    return true;
                }

                return false;
            }

            durabilityStat_ref = val;
            durabilityStat_ref_empty = false;
            durabilityStat_ref_full = false;
            return false;
        }
    }

    public void AddCard(string id, int count = 1, LuaTable? ext = null)
    {
        if (count <= 0) return;
        var cardData = UniqueIDScriptable.GetFromID<CardData>(id);
        if (cardData == null)
        {
            return;
        }

        var tDur = new TransferedDurabilities
        {
            Liquid = cardData.CardType == CardTypes.Liquid ? count : 0,
            Usage = cardData.UsageDurability.Copy(),
            Fuel = cardData.FuelCapacity.Copy(),
            Spoilage = cardData.SpoilageTime.Copy(),
            ConsumableCharges = cardData.Progress.Copy(),
            Special1 = cardData.SpecialDurability1.Copy(),
            Special2 = cardData.SpecialDurability2.Copy(),
            Special3 = cardData.SpecialDurability3.Copy(),
            Special4 = cardData.SpecialDurability4.Copy(),
        };
        var sLiq = new SpawningLiquid
        {
            LiquidCard = cardData.DefaultLiquidContained.LiquidCard,
            StayEmpty = !cardData.DefaultLiquidContained.LiquidCard
        };
        DataNodeTableAccessBridge? initData = null;
        var forceSlotInfo = new SlotInfo(SlotsTypes.Base, -10086);
        var forceBpData = new BlueprintSaveData(null, null) {CurrentStage = -10086};
        if (ext != null)
        {
            tDur.Usage.FloatValue.TryModBy(ext[nameof(TransferedDurabilities.Usage)]);
            tDur.Fuel.FloatValue.TryModBy(ext[nameof(TransferedDurabilities.Fuel)]);
            tDur.Spoilage.FloatValue.TryModBy(ext[nameof(TransferedDurabilities.Spoilage)]);
            tDur.ConsumableCharges.FloatValue.TryModBy(ext[nameof(TransferedDurabilities.ConsumableCharges)]);
            tDur.Liquid.TryModBy(ext[nameof(TransferedDurabilities.Liquid)]);
            tDur.Special1.FloatValue.TryModBy(ext[nameof(TransferedDurabilities.Special1)]);
            tDur.Special2.FloatValue.TryModBy(ext[nameof(TransferedDurabilities.Special2)]);
            tDur.Special3.FloatValue.TryModBy(ext[nameof(TransferedDurabilities.Special3)]);
            tDur.Special4.FloatValue.TryModBy(ext[nameof(TransferedDurabilities.Special4)]);

            var card =
                (ext[nameof(SpawningLiquid.LiquidCard)] as SimpleUniqueAccess)?.UniqueIDScriptable as CardData;
            sLiq.LiquidCard = card;
            sLiq.StayEmpty = !card;

            count.TryModBy(ext[nameof(count)]);
            if (ext[nameof(forceSlotInfo.SlotType)] is string slotType &&
                Enum.TryParse(slotType, out forceSlotInfo.SlotType))
            {
                forceSlotInfo.SlotIndex = -2;
            }

            forceBpData.CurrentStage.TryModBy(ext["CurrentBpStage"]);

            if (ext[nameof(initData)] is DataNodeTableAccessBridge dataNodeTable)
                initData = dataNodeTable;
        }

        var i = 0;
        var enumerators = new List<IEnumerator>();
        do
        {
            i += 1;

            GameManager.Instance.MoniAddCard(cardData, CardBase, tDur, forceSlotInfo, forceBpData, true,
                sLiq, new Vector2Int(GameManager.Instance.CurrentTickInfo.z, -1), SimpleUniqueAccess.SetInitData,
                initData).Add2Li(enumerators);
        } while (i < count && cardData.CardType != CardTypes.Liquid);

        enumerators.Add2AllEnumerators(PriorityEnumerators.Normal);
    }

    public void Remove(bool doDrop, bool dontInstant = false)
    {
        if (dontInstant)
        {
            Runtime.StartCoroutine(Runtime.Trans2EnumSU(
                card =>
                {
                    if (GameManager.PerformingAction)
                    {
                        return true;
                    }

                    if (GameManager.DraggedCardStack.Contains(card))
                    {
                        return true;
                    }

                    Runtime.StartCoroutine(GameManager.Instance.RemoveCard(card, false, doDrop));
                    return false;
                }, CardBase));
            return;
        }

        GameManager.Instance.RemoveCard(CardBase, true, doDrop).Add2AllEnumerators(PriorityEnumerators.Low);
    }

    public void AddExpProgress(float amt)
    {
        if (CardBase == null || CardBase.CardModel.CardType != CardTypes.Explorable) return;
        CardBase.ExplorationData.CurrentExploration = Mathf.Clamp01(amt + CardBase.ExplorationData.CurrentExploration);
        if (GraphicsManager.Instance.ExplorationDeckPopup.ExplorationCard != CardBase)
        {
            for (var index = 0; index < CardBase.ExplorationData.ExplorationResults.Count; index++)
            {
                var result = CardBase.ExplorationData.ExplorationResults[index];
                var shouldUnlockExplorationResults = GraphicsManager.Instance.ExplorationDeckPopup.ExploringBar
                    .ShouldUnlockExplorationResults(index);
                if (shouldUnlockExplorationResults &&
                    CardBase.CardModel.ExplorationResults.FirstOrDefault(explorationResult =>
                        explorationResult.ActionName == result.ActionName) is { } er)
                {
                    GameManager.Instance.ActionRoutine(er.Action, CardBase, false)
                        .Add2AllEnumerators(PriorityEnumerators.Normal);
                }
            }
        }
        else
        {
            GraphicsManager.Instance.ExplorationDeckPopup.ExploringBar.Animate(GraphicsManager.Instance
                .ExplorationDeckPopup.AddActionToPerform).Add2AllEnumerators(PriorityEnumerators.High);
        }
    }

    public Transform? Transform => CardBase != null ? CardBase.transform : null;
}