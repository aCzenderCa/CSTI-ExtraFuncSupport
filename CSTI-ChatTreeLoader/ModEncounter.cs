using System;
using System.Collections.Generic;
using ChatTreeLoader.ScriptObjects;
using UnityEngine;

namespace ChatTreeLoader
{
    [Serializable]
    public class ModEncounter : ModEncounterTypedBase<ModEncounter>
    {
        public static readonly Dictionary<string, ModEncounter> ModEncounters = new();
        public string ThisId;
        public ModEncounterNode[] ModEncounterNodes = { };
        public AudioClip DefaultPlayerAudio;
        public AudioClip DefaultEnemyAudio;

        public override Dictionary<string, ModEncounter> GetValidEncounterTable()
        {
            return ModEncounters;
        }

        public override void Init()
        {
            if (HadInit)
            {
                return;
            }

            if (!string.IsNullOrEmpty(ThisId) && !ModEncounters.ContainsKey(ThisId))
            {
                HadInit = true;
                ModEncounters[ThisId] = this;
            }
        }

        public override void OnEnable()
        {
            
            if (AllModEncounters.TryGetValue(typeof(ModEncounter), out var modEncounterBases))
            {
                modEncounterBases.Add(this);
            }
            else
            {
                AllModEncounters[typeof(ModEncounter)] = new List<ModEncounterBase> {this};
            }
        }
    }

    [Serializable]
    public class ModEncounterNode : ScriptableObject
    {
        public GeneralCondition Condition;
        public GeneralCondition ShowCondition;
        public bool EndNode;
        public bool BackNode;
        public LocalizedString PlayerText;
        public float PlayerTextDuration;
        public AudioClip PlayerAudio;
        public LocalizedString Title;
        public LocalizedString EnemyText;
        public float EnemyTextDuration;
        public AudioClip EnemyAudio;
        public ModEncounterNode[] ChildrenEncounterNodes;
        public CardAction NodeEffect;
        public bool HasNodeEffect;
        public bool DontShowEnd;
    }
}