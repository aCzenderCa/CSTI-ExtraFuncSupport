using System.Collections.Generic;
using ChatTreeLoader.Attr;
using UnityEngine;

namespace ChatTreeLoader.ScriptObjects
{
    public class ExtraStatTable : ScriptableObject
    {
        public static readonly List<ExtraStatTable> AllTables = new();
        [Note("场景绑定状态"),NoteEn("env binding gameStat")]
        public List<GameStat> EnvBindStats = new();
        [Note("卡牌绑定状态"),NoteEn("card binding gameStat")]
        public List<GameStat> CardBindStats = new();
        [Note("需要绑定的卡牌"),NoteEn("Cards that need to be bound")]
        public List<CardData> CardBindCards = new();

        private void OnEnable()
        {
            AllTables.Add(this);
        }
    }
}