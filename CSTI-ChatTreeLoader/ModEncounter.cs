using System;
using System.Collections.Generic;
using ChatTreeLoader.Attr;
using ChatTreeLoader.ScriptObjects;
using UnityEngine;

namespace ChatTreeLoader
{
    [Serializable]
    public class ModEncounter : ModEncounterTypedBase<ModEncounter>
    {
        public static readonly Dictionary<string, ModEncounter> ModEncounters = new();
        [Note("要修改的Encounter的id")] public string ThisId;
        [Note("初始的选项列表")] public ModEncounterNode[] ModEncounterNodes = { };
        [Note("玩家说话时默认播放的音频")] public AudioClip DefaultPlayerAudio;
        [Note("敌人（对方）说话时默认播放的音频")] public AudioClip DefaultEnemyAudio;

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
        [Note("选项的条件，不满足不能按,检测的card是当前环境卡和天气卡，其中一张满足条件就能按")]
        public GeneralCondition Condition;

        [Note("选项是否显示的条件")] public GeneralCondition ShowCondition;
        [Note("是否是结束节点（是的话按下对应按钮后会关闭界面）")] public bool EndNode;

        [Note("是否是返回节点,按下后会返回上一个界面,没有循环返回的问题")]
        public bool BackNode;

        [Note("玩家说的话")] public LocalizedString PlayerText;
        [Note("玩家说话前的延迟(单位秒)")] public float PlayerTextDuration;
        [Note("玩家说话时播放的音频")] public AudioClip PlayerAudio;
        [Note("对话选项的名称")] public LocalizedString Title;
        [Note("敌人(对方)说的话")] public LocalizedString EnemyText;
        [Note("敌人(对方)说话前的延迟(单位秒)")] public float EnemyTextDuration;
        [Note("敌人(对方)说话时播放的音频")] public AudioClip EnemyAudio;
        [Note("选择了该选项后的选项列表")] public ModEncounterNode[] ChildrenEncounterNodes;
        [Note("该选项的效果")] public CardAction NodeEffect;
        [Note("该选项是否拥有效果")] public bool HasNodeEffect;

        [Note("为true时强制禁用结束弹窗,默认自动检测是否生成遭遇或事件卡")]
        public bool DontShowEnd;
    }
}