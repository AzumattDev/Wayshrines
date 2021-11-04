using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace Wayshrine
{
    [HarmonyPatch]
    public class WayshrineCustomBehaviour : MonoBehaviour, Hoverable, Interactable
    {
        public static readonly Dictionary<Vector3, WayshrineInfo> Wayshrines = new();
        public static readonly List<Minimap.PinData> pins = new();

        [TextArea] private ZNetView _zNetView = null!;

        private Piece piece = null!;
        public Minimap.PinType pinType;
        private Piece wayshrinePiece = null!;

        public string GetHoverText()
        {
            return Util.GetLocalized(piece.m_name + "\n[<color=yellow><b>$KEY_Use</b></color>] $wayshrine_activate");
        }

        public string GetHoverName()
        {
            return Util.GetLocalized(piece.m_name);
        }

        public bool Interact(Humanoid character, bool hold, bool alt)
        {
            if (hold)
                return false;

            Player p = Player.m_localPlayer;
            PlayerProfile playerProfile = Game.instance.GetPlayerProfile();
            if (WayshrinePlugin.OriginalFunc is { Value: true })
            {
                GameObject prefab2 = ZNetScene.instance.GetPrefab("fx_dragon_land");
                //GameObject prefab3 = ZNetScene.instance.GetPrefab("lightningAOE");
                GameObject prefab3 = ZNetScene.instance.GetPrefab("vfx_bifrost");
                GameObject prefab4 = ZNetScene.instance.GetPrefab("sfx_thunder");
                if (!Equals(prefab2, null) && !Equals(prefab3, null))
                    if (WayshrinePlugin.DisableBifrostEffect is { Value: false })
                    {
                        //GameObject.Instantiate<GameObject>(prefab1, Player.m_localPlayer.transform.position, Quaternion.identity);
                        Instantiate(prefab2, Player.m_localPlayer.transform.position,
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

                        Instantiate(prefab3, Player.m_localPlayer.transform.position,
                            Quaternion.identity);
                    }

                character.Message(MessageHud.MessageType.Center, Util.GetLocalized("$activated_heimdall"));
                var spawn = playerProfile.HaveCustomSpawnPoint()
                    ? playerProfile.GetCustomSpawnPoint()
                    : playerProfile.GetHomePoint();
                p.TeleportTo(spawn, Quaternion.identity, false);
                p.m_lastGroundTouch = 0f;
                return true;
            }

            MinimapPatches.IsHeimdallMode = true;
            Minimap.instance.ShowPointOnMap(transform.position);
            foreach (var kv in Wayshrines)
            {
                /* This code will add all the correct pins to the map for each different type of shrine. */
                WayshrineCustomBehaviour wayshrine = kv.Value.prefab.GetComponent<WayshrineCustomBehaviour>();
                //WayshrinePlugin.waylogger.LogDebug(wayshrine.name.ToLower());
                pins.Add(Minimap.instance.AddPin(kv.Key, wayshrine.pinType, Util.GetLocalized(kv.Value.prefab.GetComponent<Piece>().m_name), false, false, 0));
            }

            return false;
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item)
        {
            return false;
        }

        private void Awake()
        {
            /* in the awake method of our component, we will send the location of the new wayshrine. check for m_ownerRevision being 0 and increase it afterwards */
            piece = GetComponent<Piece>();
            _zNetView = GetComponent<ZNetView>();

            if (_zNetView.GetZDO() is ZDO { m_ownerRevision: 0 } zdo)
            {
                zdo.IncreseOwnerRevision();

                ZPackage package = new();
                package.Write(1);
                package.Write(transform.position);
                package.Write(zdo.GetPrefab());
                ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, "RequestWayZDOs", package);
            }
        }

        public void OnDestroy()
        {
            Minimap.PinData pinData = new();
            pinData.m_type = this.pinType;
            pinData.m_name = this.name;
            pinData.m_pos = transform.position;
            pinData.m_icon = Minimap.instance.GetSprite(this.pinType);
            pinData.m_save = false;
            pinData.m_checked = false;
            pinData.m_ownerID = 0L;
            Minimap.instance.RemovePin(pinData);
            pins.Remove(pinData);
            Util.sendWayshrines(0L);
            if (_zNetView.GetZDO() is ZDO { m_ownerRevision: 0 } zdo)
            {
                ZPackage package = new();
                package.Write(1);
                package.Write(transform.position);
                package.Write(zdo.GetPrefab());
                ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, "RequestWayZDOs", package);
            }
        }

        public void Start()
        {
            if (WayshrinePlugin.isAdmin)
            {
                wayshrinePiece = GetComponent<Piece>();
                wayshrinePiece.m_canBeRemoved = true;
            }

            Util.sendWayshrines(0);
        }

        public struct WayshrineInfo
        {
            public GameObject prefab;
            public Quaternion rotation;
        }
    }
}