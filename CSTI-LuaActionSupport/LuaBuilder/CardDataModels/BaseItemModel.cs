using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using BepInEx;
using CSTI_LuaActionSupport.LuaCodeHelper;
using NLua;
using UnityEngine;

namespace CSTI_LuaActionSupport.LuaBuilder.CardDataModels;

public static class BaseItemModel
{
    public static string HexStr(this byte[] bytes, int step, params byte[][] bytesN)
    {
        var stringBuilder = new StringBuilder();
        for (var i = 0; i + 1 < bytes.Length; i += step)
        {
            ushort st = bytes[i];
            st <<= 8;
            st |= bytes[i + 1];
            stringBuilder.Append(st.ToString("X4"));
        }

        foreach (var _bytes in bytesN)
        {
            for (var j = 0; j + 1 < _bytes.Length; j += step)
            {
                ushort st = _bytes[j];
                st <<= 8;
                st |= _bytes[j + 1];
                stringBuilder.Append(st.ToString("X4"));
            }
        }

        return stringBuilder.ToString();
    }

    public static readonly SHA256 SHA256 = SHA256.Create();

    public static string ToSha256(this string s)
    {
        var bytes1 = SHA256.ComputeHash(Encoding.UTF8.GetBytes(s));
        var bytes2 = SHA256.ComputeHash(Encoding.UTF8.GetBytes(s + s));
        var bytes3 = SHA256.ComputeHash(Encoding.UTF8.GetBytes(s + s + s));
        return bytes1.HexStr(16, bytes2, bytes3);
    }

    public static Sprite EmptySprite;

    static BaseItemModel()
    {
        var texture2D = new Texture2D(0, 0);
        EmptySprite = Sprite.Create(texture2D, new Rect(0, 0, 0, 0), Vector2.zero);
    }

    public static CardDataPack BuildBaseExp(CardDataPack forEnv, string desc, string img, LuaTable? table,
        LuaFunction? lua)
    {
        var name = forEnv.Cards["Main"].CardName + "_Exp";
        var exp_cardData = BuildBase(name, desc, 0, img).Cards["Main"];
        exp_cardData.CardType = CardTypes.Explorable;
        forEnv.Cards["Main_Exp"] = exp_cardData;
        forEnv.AddButton($"探索{forEnv.Cards["Main"].CardName}", lua, addTo: "Main_Exp");
        
        throw new NotSupportedException("Todo");
        return forEnv;
    }

    public static CardDataPack BuildSimplePath(string name, string desc, float weight, string img, string where2Go,
        LuaFunction? lua)
    {
        var pack = BuildBaseLocation(name, desc, weight, img);
        if (AccessTool[where2Go] is {UniqueIDScriptable: CardData {CardType: CardTypes.Environment}} access)
        {
            pack.AddMoveButton(access, lua);
            if (pack.Cards["Main"].CardImage == null)
            {
                pack.Cards["Main"].CardImage = ((CardData) access.UniqueIDScriptable).CardImage;
            }
        }

        return pack;
    }

    public static CardDataPack BuildMultiPath(string name, string desc, float weight, string img, LuaTable where2Go,
        LuaTable goLua)
    {
        var pack = BuildBaseLocation(name, desc, weight, img);
        var index = 0;
        foreach (var access in where2Go.ToList<string>().Where(s => !s.IsNullOrWhiteSpace())
                     .Select(s => AccessTool[s ?? string.Empty]))
        {
            if (access != null)
                pack.AddMoveButton(access, goLua[index] as LuaFunction);
        }

        return pack;
    }

    public static CardDataPack BuildBaseLocation(string name, string desc, float weight, string img)
    {
        var cardDataPack = BuildBase(name, desc, weight, img);
        cardDataPack.Cards["Main"].CardType = CardTypes.Location;
        return cardDataPack;
    }

    public static CardDataPack BuildEnv(string name, string desc, float weight, string img)
    {
        var cardDataPack = BuildBase(name, desc, 0, img);
        cardDataPack.Cards["Main"].CardType = CardTypes.Environment;
        cardDataPack.Cards["Main"].MaxWeightCapacity = weight;
        return cardDataPack;
    }

    public static CardDataPack BuildBase(string name, string desc, float weight, string img)
    {
        var cardData = ScriptableObject.CreateInstance<CardData>();
        cardData.CardName = new LocalizedString {DefaultText = name};
        cardData.name = name;
        cardData.UniqueID = name.ToSha256();
        cardData.ObjectWeight = weight;
        cardData.Ambience = new AmbienceSettings {RandomNoises = Array.Empty<AudioClip>()};
        cardData.AmmoNeeded = Array.Empty<CardOrTagRef>();
        cardData.ArmorValues = new ArmorValues();
        cardData.CardDescription = new LocalizedString {DefaultText = desc};
        SpriteDict.TryGetValue(img, out cardData.CardImage);
        cardData.CardInteractions = Array.Empty<CardOnCardAction>();
        cardData.CardTags = Array.Empty<CardTag>();
        cardData.CompletedObjectives = new List<ObjectiveSubObjective>();
        cardData.CookingRecipes = Array.Empty<CookingRecipe>();
        cardData.DamageTypes = Array.Empty<DamageType>();
        cardData.DeconstructSounds = Array.Empty<AudioClip>();
        cardData.DefaultEnvCards = Array.Empty<CardData>();
        cardData.DismantleActions = new List<DismantleCardAction>();
        cardData.DroppedOnDestroy = Array.Empty<CardsDropCollection>();
        cardData.EffectsToInventoryContent = Array.Empty<PassiveEffect>();
        cardData.EnvironmentDamages = Array.Empty<CardDataRef>();
        cardData.EnvironmentImprovements = Array.Empty<CardDataRef>();
        cardData.EquipmentTags = Array.Empty<EquipmentTag>();
        cardData.ExclusivelyAcceptedLiquids = Array.Empty<CardOrTagRef>();
        cardData.ExplorationResults = Array.Empty<ExplorationResult>();
        cardData.Progress = new DurabilityStat(false, 0);
        cardData.FuelCapacity = new DurabilityStat(false, 0);
        cardData.SpoilageTime = new DurabilityStat(false, 0);
        cardData.UsageDurability = new DurabilityStat(false, 0);
        cardData.SpecialDurability1 = new DurabilityStat(false, 0);
        cardData.SpecialDurability2 = new DurabilityStat(false, 0);
        cardData.SpecialDurability3 = new DurabilityStat(false, 0);
        cardData.SpecialDurability4 = new DurabilityStat(false, 0);
        cardData.InventorySlots = Array.Empty<CardData>();
        cardData.LocalCounterEffects = Array.Empty<LocalCounterEffect>();
        cardData.OnStatsChangeActions = Array.Empty<FromStatChangeAction>();
        cardData.PassiveEffects = Array.Empty<PassiveEffect>();
        cardData.PassiveStatEffects = Array.Empty<StatModifier>();
        cardData.RemotePassiveEffects = Array.Empty<RemotePassiveEffect>();
        cardData.SpawningBlockedBy = Array.Empty<CardData>();
        cardData.VisualEffects = Array.Empty<WeatherSpecialEffect>();
        cardData.WeaponActionStatChanges = Array.Empty<StatModifier>();
        cardData.WeaponClashStatInfluences = Array.Empty<PlayerEncounterVariable>();
        cardData.WeaponDamageStatInfluences = Array.Empty<PlayerEncounterVariable>();
        cardData.WhenCreatedSounds = Array.Empty<AudioClip>();

        if (!GameLoad.Instance.DataBase.AllData.Contains(cardData))
            GameLoad.Instance.DataBase.AllData.Add(cardData);
        MainBuilder.Name2Card[name] = cardData;
        return new CardDataPack(new Dictionary<string, CardData>
        {
            {"Main", cardData}
        });
    }
}