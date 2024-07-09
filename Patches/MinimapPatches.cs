using System;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;
using Wayshrine.Extensions;
using Wayshrine.Utils;
using Object = UnityEngine.Object;

namespace Wayshrine.Patches;

public static class MinimapPatches
{
    public static bool IsHeimdallMode;
    internal static string? ItemName;
    internal static string? ItemSharedName;
    internal static ItemDrop.ItemData? itemData;

    [HarmonyPatch(typeof(Minimap), nameof(Minimap.SetMapMode))]
    public class Minimap_SetMapMode_Patch
    {
        private static void Postfix(Minimap.MapMode mode)
        {
            if (mode != Minimap.MapMode.Large) Util.LeaveHeimdallMode();
        }
    }

    [HarmonyPatch(typeof(Minimap), nameof(Minimap.Start))]
    public class MMStart_Patch
    {
        private static void Postfix(Minimap __instance, out bool[] ___m_visibleIconTypes)
        {
            ___m_visibleIconTypes = new bool[200];
            for (int i = 0; i < 200; ++i)
            {
                __instance.m_visibleIconTypes[i] = true;
            }

            foreach (GameObject wayshrine in Assets.wayshrinesList)
            {
                Sprite? pieceIcon = wayshrine.GetComponent<Piece>().m_icon;
                __instance.m_icons.Add(new Minimap.SpriteData
                {
                    m_name = wayshrine.GetComponent<WayshrineCustomBehaviour>().pinType,
                    m_icon = pieceIcon
                });
            }
        }
    }

    [HarmonyPatch(typeof(Minimap), nameof(Minimap.OnMapLeftClick))]
    private class OnMapLeftClick_Patch
    {
        private static bool Prefix()
        {
            WayshrinePlugin.waylogger.LogDebug("Map Left Click: HeimdallMode is " + IsHeimdallMode);

            if (!IsHeimdallMode)
            {
                return true;
            }

            Util.DoBiFrostStuff();
            return false;
        }
    }

    [HarmonyPatch(typeof(Minimap), nameof(Minimap.OnMapDblClick))]
    private class OnMapDblClick_Patch
    {
        private static bool Prefix()
        {
            return !IsHeimdallMode;
        }
    }

    [HarmonyPatch(typeof(Minimap), nameof(Minimap.OnMapRightClick))]
    private class OnMapRightClick_Patch
    {
        private static bool Prefix()
        {
            return !IsHeimdallMode;
        }
    }

    [HarmonyPatch(typeof(Minimap), nameof(Minimap.OnMapMiddleClick))]
    private class OnMapMiddleClick_Patch
    {
        private static bool Prefix()
        {
            return !IsHeimdallMode;
        }
    }
}