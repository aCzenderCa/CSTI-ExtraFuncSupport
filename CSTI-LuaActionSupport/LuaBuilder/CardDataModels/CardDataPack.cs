using System.Collections.Generic;
using System.Linq;
using CSTI_LuaActionSupport.AllPatcher;
using CSTI_LuaActionSupport.LuaCodeHelper;
using NLua;
using UnityEngine;

namespace CSTI_LuaActionSupport.LuaBuilder.CardDataModels;

public class CardDataPack
{
    public readonly Dictionary<string, CardData> Cards;

    public CardDataPack(Dictionary<string, CardData> cards)
    {
        Cards = cards;
    }

    public CardDataPack AddPathTo(SimpleUniqueAccess access, float needExp = 0, string addTo = "Main")
    {
        if (!Cards.ContainsKey(addTo) ||
            access.UniqueIDScriptable is not CardData {CardType: CardTypes.Environment} card) return this;
        var buildSimplePath = BaseItemModel.BuildSimplePath($"前往{card.CardName.CnStr()}",
            $"前往{card.CardName.CnStr()}的路", 0,
            card.CardImage != null ? card.CardImage.name : "", card.UniqueID, null);
        Cards[$"PathTo{card.CardName.CnStr()}"] = buildSimplePath.Cards["Main"];
        if (needExp == 0)
        {
            AddEnvCards(new SimpleUniqueAccess(buildSimplePath.Cards["Main"]), addTo);
        }
        else
        {
            AddExpCards(new List<CardDrop> {new(buildSimplePath.Cards["Main"])}, needExp, null);
        }

        return this;
    }

    public CardDataPack AddEnvCards(SimpleUniqueAccess access, string addTo = "Main")
    {
        if (!Cards.ContainsKey(addTo) || access.UniqueIDScriptable is not CardData card) return this;
        Cards[addTo].DefaultEnvCards = Cards[addTo].DefaultEnvCards.Append(card).ToArray();
        return this;
    }
    
    public CardDataPack AddExpCards(List<CardDrop> drops, float target, LuaFunction? lua, string addTo = "Main")
    {
        if (!Cards.ContainsKey(addTo) || !Cards.ContainsKey(addTo + "_Exp")) return this;
        var expR_id = addTo + "_Exp_" + Cards[addTo].name +
                      Cards[addTo + "_Exp"].ExplorationResults.Length;
        var f_expR_id = $"F_{expR_id.ToSha256()}_{Cards[addTo + "_Exp"].ExplorationResults.Length}_EXP";
        if (lua != null)
            GetModTable(MainBuilder.CurModId)[f_expR_id] = lua;
        Cards[addTo + "_Exp"].ExplorationResults = Cards[addTo + "_Exp"].ExplorationResults
            .Append(new ExplorationResult
            {
                TriggerValue = target, Action = new CardAction
                {
                    ActionName = new LocalizedString
                    {
                        DefaultText = expR_id,
                        LocalizationKey = lua == null ? "" : "LuaCardAction_" + expR_id,
                        ParentObjectID = lua == null
                            ? Cards[addTo + "_Exp"].UniqueID
                            : $"GetModTable('{MainBuilder.CurModId}').{f_expR_id}()"
                    },
                    ProducedCards = new CardsDropCollection[]
                    {
                        new()
                        {
                            CollectionName = "MainCollection" + expR_id,
                            DroppedCards = drops.ToArray()
                        }
                    }
                }
            }).ToArray();
        return this;
    }

    public CardDataPack SetWeight(float weight, string setTo = "Main")
    {
        if (!Cards.ContainsKey(setTo)) return this;
        Cards[setTo].ObjectWeight = weight;
        return this;
    }

    public CardDataPack SetSlots(int count, LuaTable? content, string setTo = "Main")
    {
        if (!Cards.ContainsKey(setTo)) return this;
        Cards[setTo].InventorySlots = new CardData[count];
        if (content != null)
        {
            for (var i = 0; i < count; i++)
            {
                if (content[i + 1] is SimpleUniqueAccess {UniqueIDScriptable: CardData card})
                {
                    Cards[setTo].InventorySlots[i] = card;
                }
            }
        }

        return this;
    }

    public CardDataPack AddMoveButton(SimpleUniqueAccess access, LuaFunction? lua, object? desc = null,
        string addTo = "Main")
    {
        if (!Cards.ContainsKey(addTo)) return this;
        if (access.UniqueIDScriptable is not CardData {CardType: CardTypes.Environment} cardData) return this;
        var name = $"移动到{cardData.CardName.CnStr()}";
        var id_name = MainBuilder.CurModId + "_" + name;
        desc ??= name;

        AddButton(name, lua, desc, addTo);
        var list = Cards[addTo].DismantleActions;
        var action = list[list.Count - 1];
        action.ProducedCards = new[]
        {
            new CardsDropCollection
            {
                CollectionName = $"{id_name}__0",
                DroppedCards = new[] {new CardDrop(cardData, Vector2Int.one)}
            }
        };
        return this;
    }

    public CardDataPack AddButton(string name, LuaFunction? lua, object? desc = null, string addTo = "Main")
    {
        if (!Cards.ContainsKey(addTo)) return this;
        var id_name = MainBuilder.CurModId + "_" + name;
        var funcId =
            $"F_{name.ToSha256().Substring(0, 8)}__Lua__{Cards[addTo].DismantleActions.Count}__{id_name.ToSha256()}";
        if (lua != null)
            GetModTable(MainBuilder.CurModId)[funcId] = lua;
        Cards[addTo].DismantleActions.Add(new DismantleCardAction
        {
            ActionName = new LocalizedString
            {
                DefaultText = name, LocalizationKey = "LuaCardAction_" + name,
                ParentObjectID = lua != null ? $"GetModTable('{MainBuilder.CurModId}').{funcId}()" : ""
            },
            ActionDescription = new LocalizedString
            {
                DefaultText = desc is not LuaFunction luaDesc
                    ? desc?.ToString() ?? ""
                    : luaDesc.LuaFun2Desc(id_name)
            }
        });
        return this;
    }

    public CardDataPack AddInter(string name, LuaFunction lua, LuaTable acceptCards, LuaTable acceptTags,
        bool doubleWay = false, object? desc = null, string addTo = "Main")
    {
        if (!Cards.ContainsKey(addTo)) return this;
        var id_name = MainBuilder.CurModId + "_" + name;
        var funcId =
            $"F_{name.ToSha256().Substring(0, 8)}__Lua__{Cards[addTo].CardInteractions.Length}__{id_name.ToSha256()}";
        GetModTable(MainBuilder.CurModId)[funcId] = lua;
        var cardOnCardActions = Cards[addTo].CardInteractions.ToList();
        var cardInteractionTrigger = new CardInteractionTrigger
        {
            TriggerCards = acceptCards
                .ToList<UniqueIDScriptable?>(o => (o as SimpleUniqueAccess)?.UniqueIDScriptable)
                .OfType<CardData>().ToArray(),
            TriggerTags = acceptTags.ToList<string>().Select(s =>
                    Resources.FindObjectsOfTypeAll<CardTag>().FirstOrDefault(tag =>
                        tag.name == s || tag.InGameName == s || tag.InGameName.DefaultText == s)).Where(tag => tag)
                .ToArray()
        };
        cardOnCardActions.Add(new CardOnCardAction(new LocalizedString
            {
                DefaultText = name, LocalizationKey = "LuaCardOnCardAction_" + name,
                ParentObjectID = $"GetModTable('{MainBuilder.CurModId}').{funcId}()"
            },
            new LocalizedString
            {
                DefaultText = desc is not LuaFunction luaDesc
                    ? desc?.ToString() ?? ""
                    : luaDesc.LuaFun2Desc(id_name)
            }, 0)
        {
            CompatibleCards = cardInteractionTrigger,
            WorksBothWays = doubleWay
        });
        Cards[addTo].CardInteractions = cardOnCardActions.ToArray();
        return this;
    }

    public CardDataPack SetDesc(LuaFunction lua, string setTo = "Main")
    {
        if (!Cards.ContainsKey(setTo)) return this;
        Cards[setTo].CardDescription = new LocalizedString
        {
            DefaultText = lua.LuaFun2Desc(Cards[setTo].name)
        };
        return this;
    }
}