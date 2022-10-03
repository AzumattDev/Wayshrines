using System;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Wayshrine
{
    public static class MinimapPatches
    {
        public static bool IsHeimdallMode;
        internal static string? _itemName;

        private static void LeaveHeimdallMode()
        {
            IsHeimdallMode = false;
            foreach (Minimap.PinData pinData in WayshrineCustomBehaviour.pins) Minimap.instance.RemovePin(pinData);
            WayshrineCustomBehaviour.pins.Clear();
        }

        private static async void DoBiFrostStuff()
        {
            if (!Player.m_localPlayer.IsTeleportable())
            {
                if (!WayshrinePlugin.Teleportable.Value)
                {
                    Player.m_localPlayer.Message(MessageHud.MessageType.Center, "$msg_noteleport");
                    return;
                }
            }

            if (!CheckTeleportCost())
            {
                Player.m_localPlayer.Message(MessageHud.MessageType.Center,
                    $"$wayshrine_cost_error : {WayshrinePlugin.ChargeItemAmount.Value} {_itemName}");
                return;
            }

            foreach (Minimap.PinData pinData in WayshrineCustomBehaviour.pins) pinData.m_save = true;

            Minimap minimap = Minimap.instance;
            Minimap.PinData closestPin = minimap.GetClosestPin(minimap.ScreenToWorldPoint(Input.mousePosition),
                minimap.m_removeRadius * (minimap.m_largeZoom * 2f));

            WayshrinePlugin.waylogger.LogDebug("Closest Pin grabbed " + closestPin.m_name);

            foreach (Minimap.PinData pinData in WayshrineCustomBehaviour.pins) pinData.m_save = false;

            if (closestPin == null || !WayshrineCustomBehaviour.Wayshrines.TryGetValue(closestPin.m_pos,
                    out WayshrineCustomBehaviour.WayshrineInfo wayshrine)) return;

            Vector3 position = closestPin.m_pos;
            Quaternion rotation = wayshrine.Rotation;
            Vector3 pos = position + rotation.normalized * Vector3.forward + Vector3.up;
            Minimap.instance.SetMapMode(Minimap.MapMode.Small);
            LeaveHeimdallMode();
            await Task.Delay(TimeSpan.FromSeconds(2));
            Player.m_localPlayer.Message(MessageHud.MessageType.Center, "Heimdall opens the Bifrost: Teleporting");
            await Task.Delay(TimeSpan.FromSeconds(2));
            GameObject prefab2 = ZNetScene.instance.GetPrefab("fx_dragon_land");
            GameObject prefab3 = ZNetScene.instance.GetPrefab("vfx_bifrost");
            GameObject prefab4 = ZNetScene.instance.GetPrefab("sfx_thunder");
            if (!Equals(prefab2, null) && !Equals(prefab3, null))
                if (WayshrinePlugin.DisableBifrostEffect is { Value: false })
                {
                    Vector3 position1 = Player.m_localPlayer.transform.position;
                    Object.Instantiate(prefab2, position1,
                        Quaternion.identity);

                    // Tell it to respect the default game mixer to not blow your fucking ear drums out.
                    try
                    {
                        prefab3.GetComponentInChildren<AudioSource>().outputAudioMixerGroup =
                            AudioMan.instance.m_ambientMixer;
                    }
                    catch
                    {
                        Debug.LogError(
                            "AzuWayshrine: AudioMan.instance.m_ambientMixer could not be assigned on outputAudioMixerGroup of vfx_bifrost");
                    }

                    GameObject? effect = Object.Instantiate(prefab3, position1,
                        Quaternion.identity);
                    effect.AddComponent<TimedDestruction>().m_timeout = 4;
                    TimedDestruction? effectTD = effect.GetComponent<TimedDestruction>();
                    effectTD.m_triggerOnAwake = true;
                }

            await Task.Delay(TimeSpan.FromSeconds(1));
            Player p = Player.m_localPlayer;

            WayshrinePlugin.waylogger.LogDebug("Calling BiFrost on: " + p.GetPlayerName());

            Player.m_localPlayer.TeleportTo(new Vector3(pos.x + 1.5f, pos.y, pos.z + 1.5f), rotation, true);
            p.m_lastGroundTouch = 0f;
        }

        internal static bool CheckTeleportCost()
        {
            if (WayshrinePlugin.ShouldCost.Value)
            {
                ItemDrop? item = ObjectDB.instance.GetItemPrefab(WayshrinePlugin.ChargeItem.Value)
                    .GetComponent<ItemDrop>();
                if (item)
                {
                    _itemName = Localization.instance.Localize(item.m_itemData.m_shared.m_name);
                    if (Player.m_localPlayer.GetInventory().CountItems(item.m_itemData.m_shared.m_name) >=
                        WayshrinePlugin.ChargeItemAmount.Value)
                    {
                        Player.m_localPlayer.GetInventory().RemoveItem(item.m_itemData.m_shared.m_name,
                            WayshrinePlugin.ChargeItemAmount.Value);
                        Player.m_localPlayer.ShowRemovedMessage(item.m_itemData,
                            WayshrinePlugin.ChargeItemAmount.Value);
                        return true;
                    }

                    return false;
                }

                return false;
            }

            return true;
        }

        [HarmonyPatch(typeof(Minimap), nameof(Minimap.SetMapMode))]
        public class Minimap_SetMapMode_Patch
        {
            private static void Postfix(Minimap.MapMode mode)
            {
                if (mode != Minimap.MapMode.Large) LeaveHeimdallMode();
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

                DoBiFrostStuff();
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
}