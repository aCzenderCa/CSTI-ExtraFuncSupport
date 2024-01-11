using System.Collections.Generic;

namespace ChatTreeLoader.LocalText
{
    public static class LocalBase
    {
        public static readonly Dictionary<string, Dictionary<string, string>> LocalData = new()
        {
            {
                "简体中文", new Dictionary<string, string>()
            },
            {
                "English", new Dictionary<string, string>
                {
                    {"按下键盘左右方向键换页(AD不行)", "Press the left and right arrow keys on the keyboard to change pages"},
                    {"购买","Purchase"},
                    {"确认购买","Confirm purchase"},
                    {"购买十份","Buy ten copies"},
                    {"再看看别的","And look at the others"},
                    {"我该走了","Time for me to go"},
                    {"你离开了","You've left"},
                    {"你购买了{0}, 花费{1}元钱","You purchased {0}, spent ${1}"},
                    {"要买这个，需要{0}元钱。","To buy this, it costs {0} dollars."},
                    {"一个{0}算{1}元钱,","One {0} counts as {1} dollars,"},
                    {"以下状态每一点算一元钱\n","Each point of the following states counts as one dollar\n"},
                    {"\n按下键盘上方向键再次显示","\nPress the arrow keys on the keyboard to display again"},
                }
            }
        };

        public static string Local(this string id)
        {
            if (LocalData[LocalizationManager.Instance.Languages[LocalizationManager.CurrentLanguage].LanguageName] is
                    { } data && data.TryGetValue(id, out var local))
            {
                return local;
            }

            return id;
        }
    }
}