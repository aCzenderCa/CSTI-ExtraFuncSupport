using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ChatTreeLoader.Patchers
{
    public static class TestCardAdd
    {
        public static void AddTestCard()
        {
            var instanceDataBase = GameLoad.Instance.DataBase;

            var encounter = ScriptableObject.CreateInstance<Encounter>();
            encounter.UniqueID = "test______encounter__onlyTest__dontUseIt__{.}encounter1";
            encounter.name = "aaae1";
            instanceDataBase.AllData.Add(encounter);

            encounter.EncounterTitle = new LocalizedString {DefaultText = "测试用"};
            encounter.EncounterStartingLog = new EncounterLogMessage("仅用于测试");

            var cardData =
                Object.Instantiate(instanceDataBase.AllData.First(scriptable =>
                    scriptable.UniqueID == "a7384e5147b23a642809451cc4ef24fb")) as CardData;
            cardData!.UniqueID = "test______encounter__onlyTest__dontUseIt__{.}card1";
            cardData.name = "aaa1";
            cardData.CardTags = new CardTag[] { };
            cardData.CardInteractions = new CardOnCardAction[] { };
            cardData.OnStatsChangeActions = new FromStatChangeAction[] { };
            instanceDataBase.AllData.Add(cardData);
            var durabilityStat = new DurabilityStat(false, 0);
            cardData.Progress = durabilityStat;
            cardData.FuelCapacity = durabilityStat;
            cardData.SpoilageTime = durabilityStat;
            cardData.UsageDurability = durabilityStat;
            cardData.SpecialDurability1 = durabilityStat;
            cardData.SpecialDurability2 = durabilityStat;
            cardData.SpecialDurability3 = durabilityStat;
            cardData.SpecialDurability4 = durabilityStat;
            cardData.CardName = new LocalizedString {DefaultText = "测试卡"};
            cardData.CardDescription = new LocalizedString {DefaultText = "仅测试用"};
            cardData.DismantleActions = new List<DismantleCardAction>
            {
                new()
                {
                    ActionName = new LocalizedString
                    {
                        DefaultText = "测试",
                        LocalizationKey = ""
                    },
                    ProducedCards = new[]
                    {
                        new CardsDropCollection
                        {
                            DroppedEncounter = encounter
                        }
                    }
                }
            };


            var modEncounter = ScriptableObject.CreateInstance<ModEncounter>();
            modEncounter.name = "mode1";
            modEncounter.ThisId = encounter.UniqueID;
            modEncounter.ModEncounterNodes = new[]
            {
                // ReSharper disable once Unity.IncorrectScriptableObjectInstantiation
                new ModEncounterNode
                {
                    EndNode = true,
                    Title = new LocalizedString {DefaultText = "test1"},
                    PlayerText = new LocalizedString {LocalizationKey = "player1"},
                    EnemyText = new LocalizedString {DefaultText = "enemy1"},
                    HasNodeEffect = true,
                    NodeEffect = new CardAction
                    {
                        ProducedCards = new[]
                        {
                            new CardsDropCollection
                            {
                                DroppedCards = new[]
                                {
                                    new CardDrop
                                    {
                                        DroppedCard = cardData,
                                        Quantity = Vector2Int.one
                                    }
                                }
                            }
                        },
                        UseMiniTicks = MiniTicksBehavior.CostsAMiniTick
                    }
                },
                // ReSharper disable once Unity.IncorrectScriptableObjectInstantiation
                new ModEncounterNode
                {
                    EndNode = true,
                    Title = new LocalizedString {DefaultText = "test2"},
                    PlayerText = new LocalizedString {LocalizationKey = "player2"},
                    EnemyText = new LocalizedString {DefaultText = "enemy1"},
                    HasNodeEffect = true,
                    NodeEffect = new CardAction {DaytimeCost = 1}
                }
            };
        }
    }
}