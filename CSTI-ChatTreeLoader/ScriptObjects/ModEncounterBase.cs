using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChatTreeLoader.ScriptObjects
{
    public abstract class ModEncounterBase : ScriptableObject
    {
        public static readonly Dictionary<Type, List<ModEncounterBase>> AllModEncounters = new();
        public abstract void Init();

        public abstract void OnEnable();
        protected bool HadInit;
    }

    public abstract class ModEncounterTypedBase<T> : ModEncounterBase
        where T : ModEncounterTypedBase<T>
    {
        public abstract Dictionary<string, T> GetValidEncounterTable();
    }
}