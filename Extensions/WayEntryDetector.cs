using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using Wayshrine;

[HarmonyPatch]
public class WayEntryDetector : MonoBehaviour
{
    public CapsuleCollider m_wayTELEPORT;

    private void OnTriggerEnter(Collider collider)
    {
        Player component = collider.GetComponent<Player>();
        if (component == null || Player.m_localPlayer != component)
            return;
        Player p = Player.m_localPlayer;
        PlayerProfile playerProfile = Game.instance.GetPlayerProfile();
        if (!Player.m_localPlayer.IsTeleportable())
        {
            if (!WayshrinePlugin.teleportable.Value)
            {
                Player.m_localPlayer.Message(MessageHud.MessageType.Center, "$msg_noteleport");
                return;
            }
        }
        if (WayshrinePlugin.OriginalFunc is { Value: true })
        {
            GameObject prefab2 = ZNetScene.instance.GetPrefab("fx_dragon_land");
            //GameObject prefab3 = ZNetScene.instance.GetPrefab("lightningAOE");
            GameObject prefab3 = ZNetScene.instance.GetPrefab("vfx_bifrost");
            GameObject prefab4 = ZNetScene.instance.GetPrefab("sfx_thunder");
            if (!Equals(prefab2, null) && !Equals(prefab3, null))
            {
                if (WayshrinePlugin.DisableBifrostEffect is { Value: false })
                {
                    //GameObject.Instantiate<GameObject>(prefab1, Player.m_localPlayer.transform.position, Quaternion.identity);
                    Vector3 position = Player.m_localPlayer.transform.position;
                    Instantiate(prefab2, position,
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

                    Instantiate(prefab3, position,
                        Quaternion.identity);
                }
            }

            p.Message(MessageHud.MessageType.Center, Util.GetLocalized("$activated_heimdall"));
            Vector3 spawn = playerProfile.HaveCustomSpawnPoint()
                ? playerProfile.GetCustomSpawnPoint()
                : playerProfile.GetHomePoint();
            p.TeleportTo(spawn, Quaternion.identity, false);
            p.m_lastGroundTouch = 0f;
        }

        MinimapPatches.IsHeimdallMode = true;
        Minimap.instance.ShowPointOnMap(transform.position);
        foreach (KeyValuePair<Vector3, WayshrineCustomBehaviour.WayshrineInfo> kv in Wayshrine.WayshrineCustomBehaviour
            .Wayshrines)
        {
            /* This code will add all the correct pins to the map for each different type of shrine. */
            WayshrineCustomBehaviour wayshrine = kv.Value.prefab.GetComponent<WayshrineCustomBehaviour>();
            //WayshrinePlugin.waylogger.LogDebug(wayshrine.name.ToLower());
            Wayshrine.WayshrineCustomBehaviour.pins.Add(Minimap.instance.AddPin(kv.Key, wayshrine.pinType,
                Util.GetLocalized(kv.Value.prefab.GetComponent<Piece>().m_name), false, false));
        }
    }

    private void OnTriggerExit(Collider collider)
    {
    }
}