using HarmonyLib;
using UnityEngine;

namespace ChatTreeLoader.Patchers
{
    public static class SlotChangePatcher
    {
        // [HarmonyPostfix,
        //  HarmonyPatch(typeof(DynamicViewLayoutGroup), nameof(DynamicViewLayoutGroup.GetElementPosition))]
        public static void MoreLineGetElementPosition(DynamicViewLayoutGroup __instance, int _Index,
            ref Vector3 __result)
        {
            if (__instance != GraphicsManager.Instance.BaseSlotsLine)
            {
                return;
            }

            if (__instance.ExtraSpaces is not { Count: > 0 } spaces) return;
            var num = 0f;
            foreach (var dynamicViewExtraSpace in spaces)
            {
                if (dynamicViewExtraSpace.AtIndex <= _Index)
                {
                    num += dynamicViewExtraSpace.Space;
                }
            }

            __result = __instance.LayoutOriginPos +
                       __instance.LayoutDirection * (__instance.Spacing * (_Index / 2) + num) +
                       new Vector2(0, -80) * (_Index % 2) + new Vector2(0, 30);
        }

        private static bool OnCalSize;

        // [HarmonyPrefix, HarmonyPostfix,
        //  HarmonyPatch(typeof(DynamicViewLayoutGroup), nameof(DynamicViewLayoutGroup.CalculateSizeAndProperties))]
        public static void CalculateSizeAndProperties(DynamicViewLayoutGroup __instance)
        {
            __instance.Size = __instance.Spacing * 0.5f * __instance.AllElements.Count -
                              __instance.Spacing * 0.5f * __instance.InactiveElements;
            if (__instance.ExtraSpaces != null)
            {
                foreach (var dynamicViewExtraSpace in __instance.ExtraSpaces)
                {
                    __instance.Size += dynamicViewExtraSpace.Space;
                }
            }

            if (__instance.AddedSize && __instance.AddedSize.gameObject.activeSelf)
            {
                if (__instance.LayoutOrientation == RectTransform.Axis.Horizontal)
                {
                    __instance.Size += __instance.AddedSize.rect.width;
                }
                else
                {
                    __instance.Size += __instance.AddedSize.rect.height;
                }
            }

            if (__instance.LayoutOrientation == RectTransform.Axis.Horizontal)
            {
                __instance.Size += __instance.Padding.left + __instance.Padding.right;
            }
            else
            {
                __instance.Size += __instance.Padding.top + __instance.Padding.right;
            }

            __instance.Size = Mathf.Max(__instance.Size, __instance.MinSize);
        }

        // [HarmonyPrefix,
        //  HarmonyPatch(typeof(RectTransform), nameof(RectTransform.SetSizeWithCurrentAnchors))]
        public static void SetSizeWithCurrentAnchors(RectTransform __instance, ref float size)
        {
            if (!OnCalSize)
            {
                return;
            }

            if (__instance != GraphicsManager.Instance.BaseSlotsLine.RectTr)
            {
                return;
            }

            Debug.Log($"set __ {size}");
            size -= 0.5f *
                    (GraphicsManager.Instance.BaseSlotsLine.Spacing *
                     GraphicsManager.Instance.BaseSlotsLine.AllElements.Count -
                     GraphicsManager.Instance.BaseSlotsLine.Spacing *
                     GraphicsManager.Instance.BaseSlotsLine.InactiveElements);
        }
    }
}