using System;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Wayshrine
{
    public class MinimapPatches
    {
        public static bool IsHeimdallMode;
        public static bool list_populated = false;

        private static void HeimdallMode()
        {
            IsHeimdallMode = true;
        }

        private static void LeaveHeimdallMode()
        {
            IsHeimdallMode = false;
            foreach (Minimap.PinData pinData in WayshrineCustomBehaviour.pins) Minimap.instance.RemovePin(pinData);
            WayshrineCustomBehaviour.pins.Clear();
        }

        public static async void DoBiFrostStuff()
        {
            if (!Player.m_localPlayer.IsTeleportable())
            {
                Player.m_localPlayer.Message(MessageHud.MessageType.Center, "$msg_noteleport", 0, null);
                return;
            }
                

            foreach (Minimap.PinData pinData in WayshrineCustomBehaviour.pins) pinData.m_save = true;

            Minimap minimap = Minimap.instance;
            Minimap.PinData closestPin = minimap.GetClosestPin(minimap.ScreenToWorldPoint(Input.mousePosition),
                minimap.m_removeRadius * (minimap.m_largeZoom * 2f));

            WayshrinePlugin.waylogger.LogDebug("Closest Pin grabbed " + closestPin.m_name.ToString());

            foreach (Minimap.PinData pinData in WayshrineCustomBehaviour.pins) pinData.m_save = false;

            if (closestPin == null ||
                !WayshrineCustomBehaviour.Wayshrines.TryGetValue(closestPin.m_pos, out var wayshrine)) return;

            var position = closestPin.m_pos;
            var rotation = wayshrine.rotation;
            var pos = position + rotation.normalized * Vector3.forward + Vector3.up;
            Minimap.instance.SetMapMode(Minimap.MapMode.Small);
            LeaveHeimdallMode();
            await Task.Delay(TimeSpan.FromSeconds(2));
            Player.m_localPlayer.Message(MessageHud.MessageType.Center, "Heimdall opens the Bifrost: Teleporting");
            await Task.Delay(TimeSpan.FromSeconds(2));
            //GameObject prefab1 = ZNetScene.instance.GetPrefab("fx_eikthyr_stomp");
            GameObject prefab2 = ZNetScene.instance.GetPrefab("fx_dragon_land");
            //GameObject prefab3 = ZNetScene.instance.GetPrefab("lightningAOE");
            GameObject prefab3 = ZNetScene.instance.GetPrefab("vfx_bifrost");
            GameObject prefab4 = ZNetScene.instance.GetPrefab("sfx_thunder");
            if (!Equals(prefab2, null) && !Equals(prefab3, null))
                if (WayshrinePlugin.DisableBifrostEffect is { Value: false })
                {
                    //GameObject.Instantiate<GameObject>(prefab1, Player.m_localPlayer.transform.position, Quaternion.identity);
                    Object.Instantiate(prefab2, Player.m_localPlayer.transform.position,
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

                    var effect = Object.Instantiate(prefab3, Player.m_localPlayer.transform.position,
                        Quaternion.identity);
                    effect.AddComponent<TimedDestruction>().m_timeout = 4;
                    var effectTD = effect.GetComponent<TimedDestruction>();
                    effectTD.m_triggerOnAwake = true;
                }

            await Task.Delay(TimeSpan.FromSeconds(1));
            Player p = Player.m_localPlayer;
            PlayerProfile playerProfile = Game.instance.GetPlayerProfile();

            WayshrinePlugin.waylogger.LogDebug("Calling BiFrost on: " + p.GetPlayerName());



            Player.m_localPlayer.TeleportTo(new Vector3(pos.x + 1.5f, pos.y, pos.z + 1.5f), rotation, true);
            p.m_lastGroundTouch = 0f;
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
            private static void Postfix(Minimap __instance, ref bool[] ___m_visibleIconTypes)
            {
                ___m_visibleIconTypes = new bool[200];
                for (int i = 0; i < 200; i++)
                {
                    __instance.m_visibleIconTypes[i] = true;
                }
                foreach (GameObject wayshrine in Assets.wayshrinesList)
                {
                    var pieceIcon = wayshrine.GetComponent<Piece>().m_icon;
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
            private static bool Prefix(Minimap __instance)
            {

                WayshrinePlugin.waylogger.LogDebug("Map Left Click: HeimdallMode is " + IsHeimdallMode);

                if (!IsHeimdallMode)
                    return true;
                DoBiFrostStuff();
                return false;
            }
        }

        [HarmonyPatch(typeof(Minimap), nameof(Minimap.OnMapDblClick))]
        private class OnMapDblClick_Patch
        {
            private static bool Prefix(Minimap __instance)
            {
                return !IsHeimdallMode;
            }
        }

        [HarmonyPatch(typeof(Minimap), nameof(Minimap.OnMapRightClick))]
        private class OnMapRightClick_Patch
        {
            private static bool Prefix(Minimap __instance, UIInputHandler handler)
            {
                return !IsHeimdallMode;
            }
        }

        [HarmonyPatch(typeof(Minimap), nameof(Minimap.OnMapMiddleClick))]
        private class OnMapMiddleClick_Patch
        {
            private static bool Prefix(Minimap __instance, UIInputHandler handler)
            {
                return !IsHeimdallMode;
            }
        }
    }
}