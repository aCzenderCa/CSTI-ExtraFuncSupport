using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChatTreeLoader
{
    public class ModEncounter : ScriptableObject
    {
        public static readonly List<ModEncounter> AllModEncounters = new();
        public static readonly Dictionary<string, ModEncounter> ModEncounters = new();
        public string ThisId;
        public ModEncounterNode[] ModEncounterNodes = {};
        public AudioClip DefaultPlayerAudio;
        public AudioClip DefaultEnemyAudio;

        public void Init()
        {
            if (!string.IsNullOrEmpty(ThisId)&&!ModEncounters.ContainsKey(ThisId))
            {
                ModEncounters[ThisId] = this;
            }
        }

        private void OnEnable()
        {
            AllModEncounters.Add(this);
        }
    }

    [Serializable]
    public class ModEncounterNode
    {
        public GeneralCondition Condition;
        public bool EndNode;
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
    }
}