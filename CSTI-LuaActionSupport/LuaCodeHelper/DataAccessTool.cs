using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using BepInEx.Logging;
using CSTI_LuaActionSupport.AllPatcher;
using NLua;
using UnityEngine;
using Logger = BepInEx.Logging.Logger;

namespace CSTI_LuaActionSupport.LuaCodeHelper
{
    public static class DataAccessTool
    {
        public static readonly Func<string, CardData> GetCardDataIns = GetCard;
        public static readonly Func<string, CardAccessBridge> GetCardAccessBridgeIns = GetGameCard;
        public static readonly Func<string, List<CardAccessBridge>> GetCardAccessBridgesIns = GetGameCards;
        public static readonly Func<string, GameStat> GetStatModelIns = GetStat;
        public static readonly Func<string, GameStatAccessBridge> GetGameStatAccessBridgeIns = GetGameStat;
        public static readonly Func<string, bool, bool, int> CountCardOnBoardIns = CountCardOnBoard;
        public static readonly Func<string, bool, int> CountCardInBaseIns = CountCardInBase;
        public static readonly Func<string, bool, int> CountCardInHandIns = CountCardInHand;
        public static readonly Func<string, bool, int> CountCardInLocationIns = CountCardInLocation;

        private static CardData GetCard(string id)
        {
            return UniqueIDScriptable.GetFromID<CardData>(id);
        }

        private static CardAccessBridge GetGameCard(string id)
        {
            var inGameCardBases = new List<InGameCardBase>();
            GameManager.Instance.CardIsOnBoard(UniqueIDScriptable.GetFromID<CardData>(id), true,
                _Results: inGameCardBases);
            return new CardAccessBridge(inGameCardBases.FirstOrDefault());
        }

        private static CardAccessBridge? GetGameCardByTag(string tag)
        {
            var inGameCardBases = new List<InGameCardBase>(GameManager.Instance.AllCards.Where(cardBase =>
                cardBase.CardModel.CardTags.Any(cardTag =>
                    cardTag != null && (cardTag.InGameName.DefaultText == tag || cardTag.name == tag))));
            return inGameCardBases.FirstOrDefault() is { } card ? new CardAccessBridge(card) : null;
        }

        private static List<CardAccessBridge> GetGameCards(string id)
        {
            var list = new List<InGameCardBase>();
            GameManager.Instance.CardIsOnBoard(UniqueIDScriptable.GetFromID<CardData>(id), true, _Results: list);
            return list.Select(cardBase => new CardAccessBridge(cardBase)).ToList();
        }

        private static List<CardAccessBridge> GetGameCardsByTag(string tag)
        {
            var inGameCardBases = new List<InGameCardBase>(GameManager.Instance.AllCards.Where(cardBase =>
                cardBase.CardModel.CardTags.Any(cardTag =>
                    cardTag != null && (cardTag.InGameName.DefaultText == tag || cardTag.name == tag))));
            return inGameCardBases.Select(cardBase => new CardAccessBridge(cardBase)).ToList();
        }

        private static int CountCardOnBoard(string id, bool _CountInInventories = true, bool _CountInBackground = false)
        {
            var list = new List<InGameCardBase>();
            GameManager.Instance.CardIsOnBoard(UniqueIDScriptable.GetFromID<CardData>(id), true,
                _CountInInventories, false, _CountInBackground, list);
            return list.Count;
        }

        private static int CountCardInBase(string id, bool _CountInInventories = true)
        {
            var list = new List<InGameCardBase>();
            GameManager.Instance.CardIsInBase(UniqueIDScriptable.GetFromID<CardData>(id), _CountInInventories, false,
                list);
            return list.Count;
        }

        private static int CountCardInHand(string id, bool _CountInInventories = true)
        {
            var list = new List<InGameCardBase>();
            GameManager.Instance.CardIsInHand(UniqueIDScriptable.GetFromID<CardData>(id), _CountInInventories, list);
            return list.Count;
        }

        private static int CountCardInLocation(string id, bool _CountInInventories = true)
        {
            var list = new List<InGameCardBase>();
            GameManager.Instance.CardIsInLocation(UniqueIDScriptable.GetFromID<CardData>(id), _CountInInventories,
                false, list);
            return list.Count;
        }

        private static GameStat GetStat(string id)
        {
            return UniqueIDScriptable.GetFromID<GameStat>(id);
        }

        private static GameStatAccessBridge GetGameStat(string id)
        {
            return new GameStatAccessBridge(GameManager.Instance.StatsDict[GetStat(id)]);
        }
    }

    public class DebugBridge
    {
        public object info
        {
            set => Debug.LogFormat("[Info] {0}", (value is LuaTable table ? TableToString(table) : value));
        }

        public object debug
        {
            set => Debug.LogFormat("[Debug] {0}",value is LuaTable table ? TableToString(table) : value);
        }

        public object warn
        {
            set => Debug.LogWarningFormat("[Warn] {0}",value is LuaTable table ? TableToString(table) : value);
        }

        public object error
        {
            set => Debug.LogErrorFormat("[Error] {0}",value is LuaTable table ? TableToString(table) : value);
        }

        public static string TableToString(LuaTable table, List<LuaTable>? cache = null)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.Append("{");
            foreach (var (key, val) in table.Items())
            {
                if (stringBuilder.Length != 1)
                {
                    stringBuilder.Append(", ");
                }

                if (key is LuaTable keyTable && cache?.Contains(keyTable) is not true)
                {
                    stringBuilder.Append(TableToString(keyTable,
                        cache == null ? new List<LuaTable> {table} : cache.Append(table).ToList()));
                }
                else
                {
                    stringBuilder.Append(key);
                }

                stringBuilder.Append(" : ");
                if (val is LuaTable valTable && cache?.Contains(valTable) is not true)
                {
                    stringBuilder.Append(TableToString(valTable,
                        cache == null ? new List<LuaTable> {table} : cache.Append(table).ToList()));
                }
                else
                {
                    stringBuilder.Append(val);
                }
            }

            stringBuilder.Append('}');
            return stringBuilder.ToString();
        }
    }

    public class CardAccessBridge
    {
        public readonly InGameCardBase? CardBase;

        public CardAccessBridge(InGameCardBase? cardBase)
        {
            CardBase = cardBase;
        }

        public static readonly Regex KVDataCheck = new(@"zender\.luaSupportData\.\{(?<key>.+?)\}:\{(?<val>.+?)\}");

        public List<CardAccessBridge>? this[long index]
        {
            get
            {
                if (CardBase == null) return null;
                if (!CardBase.IsInventoryCard) return null;
                if (index < 0 || index >= CardBase.CardsInInventory.Count) return null;
                return CardBase.CardsInInventory[Mathf.RoundToInt((float) index)].AllCards
                    .Select(cardBase => new CardAccessBridge(cardBase))
                    .ToList();
            }
        }

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

            return CardBase.CardModel.CardTags.Any(cardTag =>
                cardTag.name == tag || cardTag.InGameName.DefaultText == tag);
        }

        public string Id => CardBase != null ? CardBase.CardModel.UniqueID : "";

        public float Spoilage
        {
            get => CardBase != null ? CardBase.CurrentSpoilage : 0;
            set => CardActionPatcher.Enumerators.Add(ModifyDurability(value, DurabilitiesTypes.Spoilage));
        }

        public float Usage
        {
            get => CardBase != null ? CardBase.CurrentUsageDurability : 0;
            set => CardActionPatcher.Enumerators.Add(ModifyDurability(value, DurabilitiesTypes.Usage));
        }

        public float Fuel
        {
            get => CardBase != null ? CardBase.CurrentFuel : 0;
            set => CardActionPatcher.Enumerators.Add(ModifyDurability(value, DurabilitiesTypes.Fuel));
        }

        public float Progress
        {
            get => CardBase != null ? CardBase.CurrentProgress : 0;
            set => CardActionPatcher.Enumerators.Add(ModifyDurability(value, DurabilitiesTypes.Progress));
        }

        public float Special1
        {
            get => CardBase != null ? CardBase.CurrentSpecial1 : 0;
            set => CardActionPatcher.Enumerators.Add(ModifyDurability(value, DurabilitiesTypes.Special1));
        }

        public float Special2
        {
            get => CardBase != null ? CardBase.CurrentSpecial2 : 0;
            set => CardActionPatcher.Enumerators.Add(ModifyDurability(value, DurabilitiesTypes.Special2));
        }

        public float Special3
        {
            get => CardBase != null ? CardBase.CurrentSpecial3 : 0;
            set => CardActionPatcher.Enumerators.Add(ModifyDurability(value, DurabilitiesTypes.Special3));
        }

        public float Special4
        {
            get => CardBase != null ? CardBase.CurrentSpecial4 : 0;
            set => CardActionPatcher.Enumerators.Add(ModifyDurability(value, DurabilitiesTypes.Special4));
        }

        public float LiquidQuantity
        {
            get => CardBase != null && CardBase.IsLiquid ? CardBase.CurrentLiquidQuantity : 0;
            set => CardActionPatcher.Enumerators.Add(ModifyDurability(value, DurabilitiesTypes.Liquid));
        }

        private IEnumerator ModifyDurability(float val, DurabilitiesTypes types)
        {
            if (CardBase == null) yield break;
            switch (types)
            {
                case DurabilitiesTypes.Spoilage:
                    if (!CardBase.CardModel.SpoilageTime)
                    {
                        yield break;
                    }

                    var rawCurrentSpoilage = CardBase.CurrentSpoilage;
                    if (val >= CardBase.CardModel.SpoilageTime.Max)
                    {
                        CardBase.SpoilEmpty = false;
                        CardBase.CurrentSpoilage = CardBase.CardModel.SpoilageTime.Max;
                        if (rawCurrentSpoilage < CardBase.CardModel.SpoilageTime.Max)
                        {
                            CardBase.SpoilFull = true;
                            yield return GameManager.Instance.ActionRoutine(CardBase.CardModel.SpoilageTime.OnFull,
                                CardBase, false);
                        }

                        break;
                    }

                    if (val <= 0)
                    {
                        CardBase.SpoilFull = false;
                        CardBase.CurrentSpoilage = 0;
                        if (rawCurrentSpoilage > 0)
                        {
                            CardBase.SpoilEmpty = true;
                            yield return GameManager.Instance.ActionRoutine(CardBase.CardModel.SpoilageTime.OnZero,
                                CardBase, false);
                        }

                        break;
                    }

                    CardBase.CurrentSpoilage = val;
                    CardBase.SpoilEmpty = false;
                    CardBase.SpoilFull = false;
                    break;
                case DurabilitiesTypes.Usage:
                    if (!CardBase.CardModel.UsageDurability)
                    {
                        yield break;
                    }

                    var rawCurrentUsage = CardBase.CurrentUsageDurability;
                    if (val >= CardBase.CardModel.UsageDurability.Max)
                    {
                        CardBase.UsageEmpty = false;
                        CardBase.CurrentUsageDurability = CardBase.CardModel.UsageDurability.Max;
                        if (rawCurrentUsage < CardBase.CardModel.UsageDurability.Max)
                        {
                            CardBase.UsageFull = true;
                            yield return GameManager.Instance.ActionRoutine(
                                CardBase.CardModel.UsageDurability.OnFull,
                                CardBase, false);
                        }

                        break;
                    }

                    if (val <= 0)
                    {
                        CardBase.UsageFull = false;
                        CardBase.CurrentUsageDurability = 0;
                        if (rawCurrentUsage > 0)
                        {
                            CardBase.UsageEmpty = true;
                            yield return GameManager.Instance.ActionRoutine(
                                CardBase.CardModel.UsageDurability.OnZero,
                                CardBase, false);
                        }

                        break;
                    }

                    CardBase.CurrentUsageDurability = val;
                    CardBase.UsageEmpty = false;
                    CardBase.UsageFull = false;
                    break;
                case DurabilitiesTypes.Fuel:
                    if (!CardBase.CardModel.FuelCapacity)
                    {
                        yield break;
                    }

                    var rawCurrentFuel = CardBase.CurrentFuel;
                    if (val >= CardBase.CardModel.FuelCapacity.Max)
                    {
                        CardBase.FuelEmpty = false;
                        CardBase.CurrentFuel = CardBase.CardModel.FuelCapacity.Max;
                        if (rawCurrentFuel < CardBase.CardModel.FuelCapacity.Max)
                        {
                            CardBase.FuelFull = true;
                            yield return GameManager.Instance.ActionRoutine(CardBase.CardModel.FuelCapacity.OnFull,
                                CardBase, false);
                        }

                        break;
                    }

                    if (val <= 0)
                    {
                        CardBase.FuelFull = false;
                        CardBase.CurrentFuel = 0;
                        if (rawCurrentFuel > 0)
                        {
                            CardBase.FuelEmpty = true;
                            yield return GameManager.Instance.ActionRoutine(CardBase.CardModel.FuelCapacity.OnZero,
                                CardBase, false);
                        }

                        break;
                    }

                    CardBase.CurrentFuel = val;
                    CardBase.FuelEmpty = false;
                    CardBase.FuelFull = false;
                    break;
                case DurabilitiesTypes.Progress:
                    if (!CardBase.CardModel.Progress)
                    {
                        yield break;
                    }

                    var rawCurrentProgress = CardBase.CurrentProgress;
                    if (val >= CardBase.CardModel.Progress.Max)
                    {
                        CardBase.ProgressEmpty = false;
                        CardBase.CurrentProgress = CardBase.CardModel.Progress.Max;
                        if (rawCurrentProgress < CardBase.CardModel.Progress.Max)
                        {
                            CardBase.ProgressFull = true;
                            yield return GameManager.Instance.ActionRoutine(CardBase.CardModel.Progress.OnFull,
                                CardBase, false);
                        }

                        break;
                    }

                    if (val <= 0)
                    {
                        CardBase.ProgressFull = false;
                        CardBase.CurrentProgress = 0;
                        if (rawCurrentProgress > 0)
                        {
                            CardBase.ProgressEmpty = true;
                            yield return GameManager.Instance.ActionRoutine(CardBase.CardModel.Progress.OnZero,
                                CardBase, false);
                        }

                        break;
                    }

                    CardBase.CurrentProgress = val;
                    CardBase.ProgressEmpty = false;
                    CardBase.ProgressFull = false;
                    break;
                case DurabilitiesTypes.Liquid:
                    if (!CardBase.IsLiquid)
                    {
                        yield break;
                    }

                    CardBase.CurrentLiquidQuantity += val;
                    CardBase.CurrentLiquidQuantity = CardBase.CurrentMaxLiquidQuantity > 0f
                        ? Mathf.Clamp(CardBase.CurrentLiquidQuantity, 0f, CardBase.CurrentMaxLiquidQuantity)
                        : Mathf.Min(CardBase.CurrentLiquidQuantity, 0f);
                    CardBase.WeightHasChanged();
                    break;
                case DurabilitiesTypes.Special1:
                    if (!CardBase.CardModel.SpecialDurability1)
                    {
                        yield break;
                    }

                    var rawCurrentSpecialDurability1 = CardBase.CurrentSpecial1;
                    if (val >= CardBase.CardModel.SpecialDurability1.Max)
                    {
                        CardBase.Special1Empty = false;
                        CardBase.CurrentSpecial1 = CardBase.CardModel.SpecialDurability1.Max;
                        if (rawCurrentSpecialDurability1 < CardBase.CardModel.SpecialDurability1.Max)
                        {
                            CardBase.Special1Full = true;
                            yield return GameManager.Instance.ActionRoutine(
                                CardBase.CardModel.SpecialDurability1.OnFull,
                                CardBase, false);
                        }

                        break;
                    }

                    if (val <= 0)
                    {
                        CardBase.Special1Full = false;
                        CardBase.CurrentSpecial1 = 0;
                        if (rawCurrentSpecialDurability1 > 0)
                        {
                            CardBase.Special1Empty = true;
                            yield return GameManager.Instance.ActionRoutine(
                                CardBase.CardModel.SpecialDurability1.OnZero,
                                CardBase, false);
                        }

                        break;
                    }

                    CardBase.CurrentSpecial1 = val;
                    CardBase.Special1Empty = false;
                    CardBase.Special1Full = false;
                    break;
                case DurabilitiesTypes.Special2:
                    if (!CardBase.CardModel.SpecialDurability2)
                    {
                        yield break;
                    }

                    var rawCurrentSpecialDurability2 = CardBase.CurrentSpecial2;
                    if (val >= CardBase.CardModel.SpecialDurability2.Max)
                    {
                        CardBase.Special2Empty = false;
                        CardBase.CurrentSpecial2 = CardBase.CardModel.SpecialDurability2.Max;
                        if (rawCurrentSpecialDurability2 < CardBase.CardModel.SpecialDurability2.Max)
                        {
                            CardBase.Special2Full = true;
                            yield return GameManager.Instance.ActionRoutine(
                                CardBase.CardModel.SpecialDurability2.OnFull,
                                CardBase, false);
                        }

                        break;
                    }

                    if (val <= 0)
                    {
                        CardBase.Special2Full = false;
                        CardBase.CurrentSpecial2 = 0;
                        if (rawCurrentSpecialDurability2 > 0)
                        {
                            CardBase.Special2Empty = true;
                            yield return GameManager.Instance.ActionRoutine(
                                CardBase.CardModel.SpecialDurability2.OnZero,
                                CardBase, false);
                        }

                        break;
                    }

                    CardBase.CurrentSpecial2 = val;
                    CardBase.Special2Empty = false;
                    CardBase.Special2Full = false;
                    break;
                case DurabilitiesTypes.Special3:
                    if (!CardBase.CardModel.SpecialDurability3)
                    {
                        yield break;
                    }

                    var rawCurrentSpecialDurability3 = CardBase.CurrentSpecial3;
                    if (val >= CardBase.CardModel.SpecialDurability3.Max)
                    {
                        CardBase.Special3Empty = false;
                        CardBase.CurrentSpecial3 = CardBase.CardModel.SpecialDurability3.Max;
                        if (rawCurrentSpecialDurability3 < CardBase.CardModel.SpecialDurability3.Max)
                        {
                            CardBase.Special3Full = true;
                            yield return GameManager.Instance.ActionRoutine(
                                CardBase.CardModel.SpecialDurability3.OnFull,
                                CardBase, false);
                        }

                        break;
                    }

                    if (val <= 0)
                    {
                        CardBase.Special3Full = false;
                        CardBase.CurrentSpecial3 = 0;
                        if (rawCurrentSpecialDurability3 > 0)
                        {
                            CardBase.Special3Empty = true;
                            yield return GameManager.Instance.ActionRoutine(
                                CardBase.CardModel.SpecialDurability3.OnZero,
                                CardBase, false);
                        }

                        break;
                    }

                    CardBase.CurrentSpecial3 = val;
                    CardBase.Special3Empty = false;
                    CardBase.Special3Full = false;
                    break;
                case DurabilitiesTypes.Special4:
                    if (!CardBase.CardModel.SpecialDurability4)
                    {
                        yield break;
                    }

                    var rawCurrentSpecialDurability4 = CardBase.CurrentSpecial4;
                    if (val >= CardBase.CardModel.SpecialDurability4.Max)
                    {
                        CardBase.Special4Empty = false;
                        CardBase.CurrentSpecial4 = CardBase.CardModel.SpecialDurability4.Max;
                        if (rawCurrentSpecialDurability4 < CardBase.CardModel.SpecialDurability4.Max)
                        {
                            CardBase.Special4Full = true;
                            yield return GameManager.Instance.ActionRoutine(
                                CardBase.CardModel.SpecialDurability4.OnFull,
                                CardBase, false);
                        }

                        break;
                    }

                    if (val <= 0)
                    {
                        CardBase.Special4Full = false;
                        CardBase.CurrentSpecial4 = 0;
                        if (rawCurrentSpecialDurability4 > 0)
                        {
                            CardBase.Special4Empty = true;
                            yield return GameManager.Instance.ActionRoutine(
                                CardBase.CardModel.SpecialDurability4.OnZero,
                                CardBase, false);
                        }

                        break;
                    }

                    CardBase.CurrentSpecial4 = val;
                    CardBase.Special4Empty = false;
                    CardBase.Special4Full = false;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(types), types, null);
            }
            CardBase.UpdateVisibility();
        }

        public void AddCard(string id, int amount = 1)
        {
            var cardData = UniqueIDScriptable.GetFromID<CardData>(id);
            if (cardData == null)
            {
                return;
            }

            var i = 0;
            do
            {
                i += 1;
                CardActionPatcher.Enumerators.Add(GameManager.Instance.AddCard(cardData, CardBase, true,
                    cardData.CardType == CardTypes.Liquid ? new TransferedDurabilities {Liquid = amount} : null, true,
                    SpawningLiquid.Empty, Vector2Int.zero, false));
            } while (i < amount && cardData.CardType != CardTypes.Liquid);
        }

        public void Remove(bool doDrop)
        {
            CardActionPatcher.Enumerators.Add(GameManager.Instance.RemoveCard(CardBase, true, doDrop));
        }
    }

    public class GameStatAccessBridge
    {
        public readonly InGameStat GameStat;

        public GameStatAccessBridge(InGameStat gameStat)
        {
            GameStat = gameStat;
        }

        public float Value
        {
            get => GameStat.SimpleCurrentValue;
            set => GameManager.Instance.ChangeStatValueTo(GameStat, value);
        }

        public float Rate
        {
            get => GameStat.SimpleRatePerTick;
            set => GameManager.Instance.ChangeStatRateTo(GameStat, value);
        }
    }

    public static class CLR2Lua
    {
        private static ulong TableIndex;

        public static LuaTable ToLuaTable<TK, TV>(this Dictionary<TK, TV> dictionary, string? name = null)
        {
            if (name == null)
            {
                Debug.LogWarning($"Dict to LuaTable{TableIndex:X} with no name and not in an env");
            }

            CardActionPatcher.LuaRuntime.NewTable(
                name ?? $"____zender____CLR2Lua_DataBase_Tables_zender_TmpTable_{TableIndex:X}");
            var luaTable =
                CardActionPatcher.LuaRuntime.GetTable(
                    name ?? $"____zender____CLR2Lua_DataBase_Tables_zender_TmpTable_{TableIndex:X}");
            foreach (var (key, value) in dictionary)
            {
                luaTable[key] = value;
            }

            TableIndex += 1;
            return luaTable;
        }

        private static ulong ListTableIndex;

        public static LuaTable ToLuaTable<TItem>(this List<TItem> list, string? name = null)
        {
            if (name == null)
            {
                Debug.LogWarning($"List to LuaTable{ListTableIndex:X} with no name and not in an env");
            }

            CardActionPatcher.LuaRuntime.NewTable(
                name ?? $"____zender____CLR2Lua_DataBase_ListTables_zender_TmpTable_{ListTableIndex:X}");
            var luaTable =
                CardActionPatcher.LuaRuntime.GetTable(
                    name ?? $"____zender____CLR2Lua_DataBase_ListTables_zender_TmpTable_{ListTableIndex:X}");
            for (int i = 0; i < list.Count; i++)
            {
                luaTable[i] = list[i];
            }

            ListTableIndex += 1;
            return luaTable;
        }
    }
}