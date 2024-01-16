using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using CSTI_LuaActionSupport.AllPatcher;
using gfoidl.Base64;
using UnityEngine;

namespace CSTI_LuaActionSupport.Helper;

public static class AnotherAddCard
{
    public static IEnumerator SuperChangeEnvironment(this GameManager instance, CardData newEnvCardData, string newEnv,
        bool modRetStack)
    {
        if (modRetStack)
        {
            var curReturnStack = LuaSystem.CurReturnStack();
            curReturnStack.Push(newEnvCardData.UniqueID, LuaSystem.GetCurEnvId() ?? "");
        }

        instance.LeavingEnvironment = true;
        instance.EnvironmentTransition = true;
        instance.GameSounds.StopAllOtherAmbiences();
        if (instance.CurrentEnvironment == null)
        {
            foreach (var card in instance.AllCards)
            {
                card.UpdateEnvironment(instance.NextEnvironment, instance.CurrentEnvironment, instance.NextTravelIndex);
            }

            yield break;
        }

        var num = instance.GameGraphics.SetLoading(true);
        yield return new WaitForSeconds(num);
        instance.GameGraphics.ClearFilterTags();
        var envKey = LuaSystem.GetCurEnvId() ?? "";
        CreateCurEnv(instance, envKey);

        instance.CalculateEnvironmentWeight(true);
        instance.EnvironmentsData[envKey].CurrentWeight = instance.CurrentEnvWeight;
        instance.EnvironmentsData[envKey].CurrentMaxWeight = instance.MaxEnvWeight;
        var waitFor = new List<CoroutineController>();
        InGameCardBase[] array2 = instance.AllCards.ToArray();
        List<InGameRefCardSaveData> updatedBGCards = new();
        List<InGameCardBase> cardsRemainingInBG = new();
        SaveCurCards(instance, array2, cardsRemainingInBG, updatedBGCards, envKey);

        SaveCurPins(instance, envKey);
        SaveRemoveAllCards(instance, array2, waitFor);

        foreach (var c in waitFor)
        {
            while (c.state != CoroutineState.Finished)
            {
                yield return null;
            }
        }

        waitFor.Clear();

        instance.TravelCardCopies.Clear();
        instance.AllCards.Clear();
        foreach (var card in cardsRemainingInBG.Where(card => card && !card.Destroyed))
        {
            instance.AllCards.Add(card);
            if ((card.CardModel.HasPassiveEffects ||
                 card.HasExternalPassiveEffects) &&
                !instance.CardsWithPassiveEffects.Contains(card))
            {
                instance.CardsWithPassiveEffects.Add(card);
            }

            if (card.CardModel.HasRemoteEffects)
            {
                instance.AddRemotePassiveEffects(card);
            }
        }

        LuaSystem.SetCurEnvId(newEnv);
        var superGetEnvSaveData = instance.SuperGetEnvSaveData(newEnvCardData, newEnv);
        var prevEnv = instance.CurrentEnvironment;
        if (instance.EnvironmentsData.ContainsKey(newEnv))
        {
            if (instance.EnvironmentsData[newEnv].AllPinnedCards != null)
            {
                foreach (var pinData in instance.EnvironmentsData[newEnv].AllPinnedCards.Where(pinData =>
                             UniqueIDScriptable.GetFromID<CardData>(pinData.CardID)))
                {
                    instance.PinnedCards.Add(
                        new InGamePinData(pinData));
                }
            }

            yield return instance.StartCoroutine(instance.LoadCardSet(instance.EnvironmentsData[newEnv].AllRegularCards,
                instance.EnvironmentsData[newEnv].AllInventoryCards,
                instance.EnvironmentsData[newEnv].NestedInventoryCards,
                updatedBGCards, true));
            foreach (var card in instance.AllCards)
            {
                card.UpdateEnvironment(instance.NextEnvironment, instance.CurrentEnvironment, instance.NextTravelIndex);
            }

            instance.CurrentEnvironmentCard = null;
            instance.LeavingEnvironment = false;
            instance.GameGraphics.RefreshSlots(false);
            yield return null;
            instance.StartCoroutineEx(instance.UpdatePassiveEffects(), out var controller);
            while (controller.state != CoroutineState.Finished)
            {
                yield return null;
            }

            instance.IsCatchingUp = true;
            instance.CatchingUpEnvData = instance.EnvironmentsData[newEnv];
            for (var i = 0;
                 i < instance.CurrentTickInfo.z - instance.EnvironmentsData[newEnv].LastUpdatedTick;
                 i++)
            {
                instance.StartCoroutineEx(instance.ApplyRates(1, instance.EnvironmentsData[newEnv].LastUpdatedTick + i),
                    out controller);
                while (controller.state != CoroutineState.Finished)
                {
                    yield return null;
                }
            }

            instance.IsCatchingUp = false;
            instance.CatchingUpEnvData = null;
            foreach (var card in instance.AllCards)
            {
                card.UpdateEnvironment(instance.NextEnvironment, prevEnv, instance.NextTravelIndex);
            }
        }

        instance.GameGraphics.LoadBookmarks(superGetEnvSaveData);
        instance.GameGraphics.SetLoading(false);
        instance.GameGraphics.ExplorationDeckPopup.SelectTab(0);
        instance.EnvironmentTransition = false;
    }

    private static void SaveRemoveAllCards(GameManager instance, InGameCardBase[] array2,
        List<CoroutineController> waitFor)
    {
        foreach (var card in array2)
        {
            if (card.CurrentContainer != null || card == instance.CurrentEnvironmentCard ||
                card == instance.CurrentHandCard || card == instance.CurrentWeatherCard ||
                card == instance.CurrentEventCard || card.IndependentFromEnv) continue;
            instance.StartCoroutineEx(instance.RemoveCard(card, true, false, GameManager.RemoveOption.RemoveAll),
                out var controller);
            waitFor.Add(controller);
        }
    }

    private static void SaveCurPins(GameManager instance, string envKey)
    {
        foreach (var pinData in instance.PinnedCards)
        {
            instance.EnvironmentsData[envKey].AllPinnedCards ??= new List<PinSaveData>();

            if (instance.EnvironmentsData[envKey].HasPinData(pinData.PinnedCard)) continue;
            instance.EnvironmentsData[envKey].AllPinnedCards.Add(new PinSaveData());
            instance.EnvironmentsData[envKey]
                .AllPinnedCards[instance.EnvironmentsData[envKey].AllPinnedCards.Count - 1]
                .SavePin(pinData);
        }

        instance.PinnedCards.Clear();
    }

    public static EnvironmentSaveData? SuperGetEnvSaveData(this GameManager instance, CardData _Env, string envId)
    {
        if (!GameManager.Instance) return null;
        if (!GameManager.Instance.CardsLoaded) return null;
        if (instance.EnvironmentsData == null) return null;
        if (instance.EnvironmentsData.TryGetValue(envId, out var envSaveData)) return envSaveData;
        instance.EnvironmentsData[envId] = new EnvironmentSaveData(_Env, instance.CurrentTickInfo.z,
            UniqueIDScriptable.AddNamesToComplexID(envId))
        {
            CurrentMaxWeight = _Env.GetWeightCapacity(0f)
        };
        instance.EnvironmentsData[envId].FillCounters(instance.AllCounters);
        if (_Env.DefaultEnvCards == null) return instance.EnvironmentsData[envId];
        foreach (var cardData in _Env.DefaultEnvCards)
        {
            if (cardData != null) instance.CreateCardAsSaveData(cardData, _Env, envId, null, null, true);
        }

        return instance.EnvironmentsData[envId];
    }

    public static IEnumerator RemoveEnvCard(this GameManager instance, CardData newEnvCardData, InGameCardBase _Card,
        string newEnv, bool modRetStack)
    {
        if (!_Card || _Card.Destroyed || _Card.CardModel.CardType != CardTypes.Environment) yield break;

        if (_Card.CardModel.OnStatsChangeActions != null && _Card.CardModel.OnStatsChangeActions.Length != 0)
        {
            foreach (var statChangeActions in _Card.CardModel.OnStatsChangeActions)
            {
                for (var k = 0; k < statChangeActions.StatChangeTrigger.Length; k++)
                {
                    if (statChangeActions.StatChangeTrigger[k].Stat == null)
                    {
                        Debug.LogError("Empty stat trigger condition on " + _Card.name, _Card);
                    }
                    else if (instance.StatsDict.ContainsKey(statChangeActions.StatChangeTrigger[k]
                                 .Stat))
                    {
                        instance.StatsDict[statChangeActions.StatChangeTrigger[k].Stat]
                            .RemoveListener(_Card);
                    }
                }
            }
        }

        instance.AllCards.Remove(_Card);
        if (instance.LatestCreatedCards.Contains(_Card)) instance.LatestCreatedCards.Remove(_Card);

        if (instance.CardsWithPassiveEffects.Contains(_Card)) instance.CardsWithPassiveEffects.Remove(_Card);

        if (_Card.CardModel.HasRemoteEffects) instance.RemoveRemotePassiveEffects(_Card);

        if (instance.AllVisibleCards.Contains(_Card)) instance.AllVisibleCards.Remove(_Card);

        if (instance.CardsWithCounters.Contains(_Card)) instance.CardsWithCounters.Remove(_Card);

        yield return instance.StartCoroutine(instance.SuperChangeEnvironment(newEnvCardData, newEnv, modRetStack));
        instance.PrevEnvironment = _Card.CardModel;
        instance.CurrentEnvironmentCard = null;
        instance.CurrentTravelIndex = instance.NextTravelIndex;

        GameManager.OnCardDestroyed?.Invoke(_Card);

        instance.StartCoroutineEx(_Card.DestroyCard(true), out var controller);
        while (controller.state != CoroutineState.Finished)
            yield return null;

        instance.GameGraphics.RefreshSlots(false);
    }

    public static IEnumerator AddEnvCard(this GameManager instance, CardData _Data,
        TransferedDurabilities? _TransferedDurabilites, string newEnv, bool modRetStack)
    {
        if (!_Data || _Data.CardType != CardTypes.Environment) yield break;

        instance.NextEnvironment = _Data;
        yield return instance.StartCoroutine(instance.RemoveEnvCard(_Data, instance.CurrentEnvironmentCard, newEnv,
            modRetStack));
        instance.CurrentEnvironmentCard = CardPooling.NextCard(instance.GameGraphics.CardsMovingParent,
            Vector3.zero, _Data, false);

        var spawned = instance.CurrentEnvironmentCard;
        spawned.name = instance.SpawnedCards + "_Env";
        instance.SpawnedCards++;
        spawned.name = spawned.name + "_" + _Data.name;

        spawned.SetCustomName("");
        spawned.SetModel(_Data);
        spawned.SetPinned(false, null);
        spawned.IgnoreBaseRow = false;

        instance.LatestCreatedCards.Add(spawned);
        while (instance.LatestCreatedCards.Count > 20) instance.LatestCreatedCards.RemoveAt(0);
        instance.AllCards.Add(spawned);
        if (_Data.HasPassiveEffects) instance.CardsWithPassiveEffects.Add(spawned);
        if (_Data.ActiveCounters != null && _Data.ActiveCounters.Length != 0)
            instance.CardsWithCounters.Add(spawned);

        instance.CheckForPassiveEffects = true;
        var finalSlotInfo = new SlotInfo(SlotsTypes.Environment, -2);
        instance.GameGraphics.GetSlotForCard(_Data, null, finalSlotInfo).AssignCard(spawned);
        spawned.PulseAfterReachingSlot = false;
        Canvas.ForceUpdateCanvases();
        spawned.transform.position = spawned.CurrentSlot.GetParent.position;
        spawned.transform.SetParent(spawned.CurrentSlot.GetParent);

        spawned.Environment = instance.CurrentEnvironment;
        spawned.PrevEnvironment = null;
        spawned.PrevEnvTravelIndex = 0;
        yield return instance.StartCoroutine(spawned.Init(_TransferedDurabilites, null, null,
            null, null, new Vector2Int(GameManager.Instance.CurrentTickInfo.z, -1)));
        if (_Data.HasRemoteEffects) instance.AddRemotePassiveEffects(spawned);
        if (spawned.CardVisuals) spawned.CardVisuals.RefreshDurabilities();

        instance.GameGraphics.RefreshSlots(false);

        if (_Data.OnStatsChangeActions != null && _Data.OnStatsChangeActions.Length != 0)
        {
            var waitFor = new List<CoroutineController>();
            for (var j = 0; j < _Data.OnStatsChangeActions.Length; j++)
            {
                for (var k = 0; k < _Data.OnStatsChangeActions[j].StatChangeTrigger.Length; k++)
                {
                    if (_Data.OnStatsChangeActions[j].StatChangeTrigger[k].Stat == null)
                    {
                        Debug.LogError("Empty stat trigger condition on " + _Data.name, spawned);
                    }
                    else if (instance.StatsDict.ContainsKey(_Data.OnStatsChangeActions[j].StatChangeTrigger[k].Stat))
                    {
                        instance.StatsDict[_Data.OnStatsChangeActions[j].StatChangeTrigger[k].Stat]
                            .RegisterListener(spawned);
                    }
                }

                try
                {
                    if (spawned.StatTriggeredActions[j].ReadyToPlay)
                    {
                        instance.StartCoroutineEx(
                            instance.ActionRoutine(spawned.CardModel.OnStatsChangeActions[j], spawned, false),
                            out var coroutineController);
                        waitFor.Add(coroutineController);
                    }
                }
                catch
                {
                    Debug.LogError(string.Concat("Mismatch in stat trigger action count between ", spawned.name, " (",
                        spawned.StatTriggeredActions.Length, " actions) and ", _Data.name, " (",
                        _Data.OnStatsChangeActions.Length, " actions)"));
                }

                if (spawned.Destroyed)
                {
                    break;
                }
            }

            int num;
            for (var i = 0; i < waitFor.Count; i = num + 1)
            {
                if (waitFor[i].state != CoroutineState.Finished)
                {
                    i = -1;
                    yield return null;
                }

                num = i;
            }
        }

        Action<InGameCardBase> onCardSpawned = GameManager.OnCardSpawned;
        onCardSpawned?.Invoke(spawned);

        instance.CalculateEnvironmentWeight(true);
    }

    public static IEnumerator CommonChangeEnvironment(this GameManager instance)
    {
        var curEnvId = LuaSystem.GetCurEnvId();
        Debug.Log($"CommonChangeEnvironment from {curEnvId}");
        var curReturnStack = LuaSystem.CurReturnStack();
        curReturnStack.Push(instance.CurrentEnvironment.UniqueID, curEnvId ?? "");
        instance.LeavingEnvironment = true;
        instance.EnvironmentTransition = true;
        instance.GameSounds.StopAllOtherAmbiences();
        List<InGameCardBase> inGameCardBases;
        if (instance.CurrentEnvironment == null)
        {
            inGameCardBases = instance.AllCards.ToList();
            foreach (var card in inGameCardBases.OfType<InGameCardBase>())
            {
                card.UpdateEnvironment(instance.NextEnvironment, instance.CurrentEnvironment,
                    instance.NextTravelIndex);
            }

            yield break;
        }

        var num = instance.GameGraphics.SetLoading(true);
        yield return new WaitForSeconds(num);
        instance.GameGraphics.ClearFilterTags();
        var envKey = curEnvId ?? "";
        CreateCurEnv(instance, envKey);

        instance.CalculateEnvironmentWeight(true);
        instance.EnvironmentsData[envKey].CurrentWeight = instance.CurrentEnvWeight;
        instance.EnvironmentsData[envKey].CurrentMaxWeight = instance.MaxEnvWeight;
        var waitFor = new List<CoroutineController>();
        InGameCardBase[] array2 = instance.AllCards.ToArray();
        List<InGameRefCardSaveData> updatedBGCards = new();
        List<InGameCardBase> cardsRemainingInBG = new();
        SaveCurCards(instance, array2, cardsRemainingInBG, updatedBGCards, envKey);

        SaveCurPins(instance, envKey);
        SaveRemoveAllCards(instance, array2, waitFor);

        foreach (var c in waitFor)
        {
            while (c.state != CoroutineState.Finished)
            {
                yield return null;
            }
        }

        waitFor.Clear();

        instance.TravelCardCopies.Clear();
        instance.AllCards.Clear();
        foreach (var card in cardsRemainingInBG.Where(card => card && !card.Destroyed))
        {
            instance.AllCards.Add(card);
            if ((card.CardModel.HasPassiveEffects ||
                 card.HasExternalPassiveEffects) &&
                !instance.CardsWithPassiveEffects.Contains(card))
            {
                instance.CardsWithPassiveEffects.Add(card);
            }

            if (card.CardModel.HasRemoteEffects)
            {
                instance.AddRemotePassiveEffects(card);
            }
        }

        var newEnvKey = "";
        if (instance.CurrentEnvironment.InstancedEnvironment && instance.NextEnvironment.InstancedEnvironment)
        {
            newEnvKey = LuaSystem.GetCurEnvId() ?? "";
            var sha256 = SHA256.Create();
            var memoryStream = new MemoryStream();
            var binaryWriter = new BinaryWriter(memoryStream);
            binaryWriter.Write(newEnvKey);
            binaryWriter.Write(newEnvKey + "_From");
            binaryWriter.Write(newEnvKey + "_Super");
            var encode = Base64.Default.Encode(sha256.ComputeHash(memoryStream.ToArray()));
            newEnvKey = encode + "_" + instance.NextEnvironment.UniqueID;
            if (instance.NextTravelIndex != 0)
            {
                newEnvKey += "=" + instance.NextTravelIndex;
            }

            LuaSystem.SetCurEnvId(newEnvKey);
        }
        else
        {
            newEnvKey = instance.NextEnvironment.EnvironmentDictionaryKey(instance.CurrentEnvironment,
                instance.NextTravelIndex);
            LuaSystem.SetCurEnvId(newEnvKey);
        }

        var superGetEnvSaveData = instance.SuperGetEnvSaveData(instance.NextEnvironment, newEnvKey)!;

        var prevEnv = instance.CurrentEnvironment;
        if (superGetEnvSaveData.AllPinnedCards != null)
        {
            foreach (var pinData in superGetEnvSaveData.AllPinnedCards.Where(pinData =>
                         UniqueIDScriptable.GetFromID<CardData>(pinData.CardID)))
            {
                instance.PinnedCards.Add(
                    new InGamePinData(pinData));
            }
        }

        yield return instance.StartCoroutine(instance.LoadCardSet(
            superGetEnvSaveData.AllRegularCards,
            superGetEnvSaveData.AllInventoryCards,
            superGetEnvSaveData.NestedInventoryCards, updatedBGCards, true));
        inGameCardBases = instance.AllCards.ToList();
        foreach (var card in inGameCardBases.OfType<InGameCardBase>())
        {
            card.UpdateEnvironment(instance.NextEnvironment, instance.CurrentEnvironment,
                instance.NextTravelIndex);
        }

        instance.CurrentEnvironmentCard = null;
        instance.LeavingEnvironment = false;
        instance.GameGraphics.RefreshSlots(false);
        yield return null;
        instance.StartCoroutineEx(instance.UpdatePassiveEffects(), out var controller);
        while (controller.state != CoroutineState.Finished)
        {
            yield return null;
        }

        instance.IsCatchingUp = true;
        instance.CatchingUpEnvData = superGetEnvSaveData;
        for (var i = 0;
             i < instance.CurrentTickInfo.z - superGetEnvSaveData.LastUpdatedTick;
             i++)
        {
            instance.StartCoroutineEx(instance.ApplyRates(1, superGetEnvSaveData.LastUpdatedTick + i),
                out controller);
            while (controller.state != CoroutineState.Finished)
            {
                yield return null;
            }
        }

        instance.IsCatchingUp = false;
        instance.CatchingUpEnvData = null;
        inGameCardBases = instance.AllCards.ToList();
        foreach (var card in inGameCardBases.OfType<InGameCardBase>())
        {
            card.UpdateEnvironment(instance.NextEnvironment, prevEnv, instance.NextTravelIndex);
        }

        instance.GameGraphics.LoadBookmarks(superGetEnvSaveData);
        instance.GameGraphics.SetLoading(false);
        instance.GameGraphics.ExplorationDeckPopup.SelectTab(0);
        instance.EnvironmentTransition = false;
    }

    private static void SaveCurCards(GameManager instance, InGameCardBase[] array2,
        List<InGameCardBase> cardsRemainingInBG,
        List<InGameRefCardSaveData> updatedBGCards, string envKey)
    {
        foreach (var card in array2)
        {
            if (card.CurrentContainer != null || card == instance.CurrentEnvironmentCard ||
                card == instance.CurrentHandCard || card == instance.CurrentWeatherCard ||
                card == instance.CurrentEventCard)
            {
                cardsRemainingInBG.Add(card);
            }
            else if (card.IndependentFromEnv)
            {
                if (card.Environment == instance.NextEnvironment)
                {
                    updatedBGCards.Add(card.MakeRefData());
                }
                else
                {
                    cardsRemainingInBG.Add(card);
                    card.UpdateEnvironment(instance.NextEnvironment, instance.CurrentEnvironment,
                        instance.NextTravelIndex);
                }
            }
            else if (card.IsInventoryCard || card.IsLiquidContainer)
            {
                instance.EnvironmentsData[envKey].AllInventoryCards
                    .Add(card.SaveInventory(instance.EnvironmentsData[envKey].NestedInventoryCards));
            }
            else
            {
                instance.EnvironmentsData[envKey].AllRegularCards.Add(card.Save());
            }
        }
    }

    private static void CreateCurEnv(GameManager instance, string envKey)
    {
        if (!instance.EnvironmentsData.ContainsKey(envKey))
        {
            instance.EnvironmentsData.Add(envKey,
                new EnvironmentSaveData(instance.CurrentEnvironment, instance.CurrentTickInfo.z,
                    UniqueIDScriptable.AddNamesToComplexID(envKey)));
            instance.EnvironmentsData[envKey].FillCounters(instance.AllCounters);
        }
        else
        {
            string[] bookmarkedCardsIDs = instance.EnvironmentsData[envKey].BookmarkedCardsIDs;
            string[] bookmarkedLiquidsIDs = instance.EnvironmentsData[envKey].BookmarkedLiquidsIDs;
            string[] array = instance.EnvironmentsData[envKey].CheckedImprovements.ToArray();
            var list = new List<InGameTickCounter>();
            list.AddRange(instance.EnvironmentsData[envKey].LocalCounterValues);
            instance.EnvironmentsData[envKey] = new EnvironmentSaveData(instance.CurrentEnvironment,
                instance.CurrentTickInfo.z, UniqueIDScriptable.AddNamesToComplexID(envKey))
            {
                BookmarkedCardsIDs = bookmarkedCardsIDs,
                BookmarkedLiquidsIDs = bookmarkedLiquidsIDs
            };
            instance.EnvironmentsData[envKey].CheckedImprovements.AddRange(array);
            instance.EnvironmentsData[envKey].FillCounters(list);
        }
    }
}