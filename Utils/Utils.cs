using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Wayshrine.Extensions;
using Wayshrine.Patches;
using Object = UnityEngine.Object;

namespace Wayshrine.Utils;

public static class Util
{
    public static string GetLocalized(string param)
    {
        return Localization.instance.Localize(param);
    }

    public static void SendWayshrines(long target)
    {
        List<ZDO> wayshrineZDOs = new();
        foreach (GameObject gameObject in Assets.wayshrinesList)
        {
            GetAllZDOsWithPrefab(gameObject.name, wayshrineZDOs);
        }

        wayshrineZDOs.RemoveAll(ZDOMan.InvalidZDO);
        SendWayshrines(target, wayshrineZDOs);
    }

    public static void GetAllZDOsWithPrefab(string prefab, List<ZDO> zdos)
    {
        int stableHashCode = prefab.GetStableHashCode();
        foreach (ZDO zdo in ZDOMan.instance.m_objectsByID.Values)
        {
            if (zdo.GetPrefab() == stableHashCode)
                zdos.Add(zdo);
        }
    }

    public static void SendWayshrines(long target, List<ZDO> wayshrineZDOs)
    {
        ZPackage package = new();
        package.Write(wayshrineZDOs.Count);
        foreach (ZDO zdo in wayshrineZDOs)
        {
            package.Write(zdo.m_position);
            package.Write(zdo.GetRotation());
            package.Write(zdo.GetPrefab());
            package.Write(zdo.m_uid);
            ZDOMan.instance.ForceSendZDO(target, zdo.m_uid);
        }

        ZRoutedRpc.instance.InvokeRoutedRPC(target, WayshrinePlugin.RPC_RequestWayshrines, package);
    }

    public static void ReadWayshrines(long target, ZPackage package)
    {
        for (int i = package.ReadInt(); i > 0; --i)
        {
            Vector3 position = package.ReadVector3();
            Quaternion rotation = package.ReadQuaternion();
            int prefabId = package.ReadInt();
            ZDOID zoid = package.ReadZDOID();
            if (prefabId == 0)
            {
                WayshrineCustomBehaviour.Wayshrines.Remove(position);
            }
            else
            {
                WayshrineCustomBehaviour.Wayshrines[position] = new WayshrineCustomBehaviour.WayshrineInfo { Prefab = ZNetScene.instance.GetPrefab(prefabId), Rotation = rotation, Position = position, ZdoId = zoid };
            }
        }
    }

    // ReSharper disable once InconsistentNaming
    public static void DeleteWayZDOs(long sender, ZPackage pkg)
    {
        if (ZNet.m_isServer)
        {
            WayshrinePlugin.waylogger.LogMessage("Deleting wayshrines");
        }

        List<ZDO> wayshrineZDOs = new();
        foreach (GameObject gameObject in Assets.wayshrinesList)
        {
            GetAllZDOsWithPrefab(gameObject.name, wayshrineZDOs);
        }

        if (wayshrineZDOs.Count == 0)
        {
            return;
        }

        foreach (ZDO zdo in wayshrineZDOs) ZDOMan.instance.m_destroySendList.Add(zdo.m_uid);
        foreach (Minimap.PinData pinData in WayshrineCustomBehaviour.pins) Minimap.instance.RemovePin(pinData);

        WayshrineCustomBehaviour.pins.Clear();
        SendWayshrines(0);
    }

    public static void RemoveItem()
    {
        if (WayshrinePlugin.ShouldCost.Value == WayshrinePlugin.Toggle.Off) return;
        Player.m_localPlayer.GetInventory().RemoveItem(MinimapPatches.ItemSharedName, WayshrinePlugin.ChargeItemAmount.Value);
        Player.m_localPlayer.ShowRemovedMessage(MinimapPatches.itemData, WayshrinePlugin.ChargeItemAmount.Value);
    }

    internal static void LeaveHeimdallMode()
    {
        MinimapPatches.IsHeimdallMode = false;
        foreach (Minimap.PinData pinData in WayshrineCustomBehaviour.pins) Minimap.instance.RemovePin(pinData);
        WayshrineCustomBehaviour.pins.Clear();
        //Minimap.instance.SetMapMode(Minimap.MapMode.Small);
    }

    internal static async void DoBiFrostStuff()
    {
        if (!Player.m_localPlayer.IsTeleportable())
        {
            if (WayshrinePlugin.Teleportable.Value == WayshrinePlugin.Toggle.Off)
            {
                Player.m_localPlayer.Message(MessageHud.MessageType.Center, "$msg_noteleport");
                return;
            }
        }

        if (!CheckTeleportCost())
        {
            Player.m_localPlayer.Message(MessageHud.MessageType.Center, $"$wayshrine_cost_error : {WayshrinePlugin.ChargeItemAmount.Value} {MinimapPatches.ItemName}");
            return;
        }

        foreach (Minimap.PinData pinData in WayshrineCustomBehaviour.pins) pinData.m_save = true;

        Minimap minimap = Minimap.instance;
        Minimap.PinData closestPin = minimap.GetClosestPin(minimap.ScreenToWorldPoint(Input.mousePosition), minimap.m_removeRadius * (minimap.m_largeZoom * 2f));


        foreach (Minimap.PinData pinData in WayshrineCustomBehaviour.pins) pinData.m_save = false;
        if (closestPin == null) return;
        WayshrinePlugin.waylogger.LogDebug("Closest Pin grabbed " + closestPin.m_name);
        if (!WayshrineCustomBehaviour.Wayshrines.TryGetValue(closestPin.m_pos, out WayshrineCustomBehaviour.WayshrineInfo wayshrine)) return;

        Vector3 position = closestPin.m_pos;
        Quaternion rotation = wayshrine.Rotation;
        Vector3 pos = position + rotation.normalized * Vector3.forward + Vector3.up;
        Minimap.instance.SetMapMode(Minimap.MapMode.Small);
        LeaveHeimdallMode();
        await Task.Delay(TimeSpan.FromSeconds(2));
        MessageHud.instance.ShowBiomeFoundMsg("Heimdall opens the Bifrost: Teleporting", true);
        await Task.Delay(TimeSpan.FromSeconds(2));
        GameObject prefab2 = ZNetScene.instance.GetPrefab("fx_dragon_land");
        GameObject prefab3 = ZNetScene.instance.GetPrefab("vfx_bifrost");
        GameObject prefab4 = ZNetScene.instance.GetPrefab("sfx_thunder");
        if (!Equals(prefab2, null) && !Equals(prefab3, null))
            if (WayshrinePlugin.DisableBifrostEffect is { Value: WayshrinePlugin.Toggle.Off })
            {
                Vector3 position1 = Player.m_localPlayer.transform.position;
                Object.Instantiate(prefab2, position1, Quaternion.identity);

                // Tell it to respect the default game mixer to not blow your fucking ear drums out.
                try
                {
                    prefab3.GetComponentInChildren<AudioSource>().outputAudioMixerGroup = AudioMan.instance.m_ambientMixer;
                }
                catch
                {
                    WayshrinePlugin.waylogger.LogError("AudioMan.instance.m_ambientMixer could not be assigned on outputAudioMixerGroup of vfx_bifrost");
                }

                GameObject? effect = Object.Instantiate(prefab3, position1, Quaternion.identity);
                effect.AddComponent<TimedDestruction>().m_timeout = 4;
                TimedDestruction? effectTD = effect.GetComponent<TimedDestruction>();
                effectTD.m_triggerOnAwake = true;
            }

        await Task.Delay(TimeSpan.FromSeconds(1));
        Player p = Player.m_localPlayer;

        WayshrinePlugin.waylogger.LogDebug("Calling BiFrost on: " + p.GetPlayerName());
        Util.RemoveItem();
        Player.m_localPlayer.TeleportTo(new Vector3(pos.x + 1.5f, pos.y, pos.z + 1.5f), rotation, true);
        p.m_lastGroundTouch = 0f;
    }

    internal static bool CheckTeleportCost()
    {
        if (WayshrinePlugin.ShouldCost.Value == WayshrinePlugin.Toggle.Off) return true;
        ItemDrop? item = ZNetScene.instance.GetPrefab(WayshrinePlugin.ChargeItem.Value).GetComponent<ItemDrop>();
        if (!item) return false;
        MinimapPatches.ItemName = Localization.instance.Localize(item.m_itemData.m_shared.m_name);
        MinimapPatches.ItemSharedName = item.m_itemData.m_shared.m_name;
        MinimapPatches.itemData = item.m_itemData;
        return Player.m_localPlayer.GetInventory().CountItems(item.m_itemData.m_shared.m_name) >= WayshrinePlugin.ChargeItemAmount.Value;
    }

    internal static bool UseOriginalFunc(Player? p, PlayerProfile playerProfile, Character character, ZNetView _zNetView)
    {
        if (WayshrinePlugin.OriginalFunc is not { Value: WayshrinePlugin.Toggle.On } || !WayshrinePlugin.ModifierKey.Value.IsKeyHeld()) return false;
        GameObject prefab2 = ZNetScene.instance.GetPrefab("fx_dragon_land");
        GameObject prefab3 = ZNetScene.instance.GetPrefab("vfx_bifrost");
        if (!Equals(prefab2, null) && !Equals(prefab3, null))
        {
            if (WayshrinePlugin.DisableBifrostEffect is { Value: WayshrinePlugin.Toggle.Off })
            {
                Vector3 position = Player.m_localPlayer.transform.position;
                Object.Instantiate(prefab2, position, Quaternion.identity);
                //Instantiate(prefab4, position, Quaternion.identity);

                // Tell it to respect the default game mixer to not blow your fucking ear drums out.
                try
                {
                    prefab3.GetComponentInChildren<AudioSource>().outputAudioMixerGroup = AudioMan.instance.m_ambientMixer;
                }
                catch
                {
                    WayshrinePlugin.waylogger.LogError("AudioMan.instance.m_ambientMixer could not be assigned on outputAudioMixerGroup of vfx_bifrost");
                }

                Object.Instantiate(prefab3, position, Quaternion.identity);
            }
        }

        RemoveItem();
        character.Message(MessageHud.MessageType.Center, GetLocalized("$activated_heimdall"));
        Vector3 spawn = playerProfile.HaveCustomSpawnPoint() ? playerProfile.GetCustomSpawnPoint() : playerProfile.GetHomePoint();
        p.TeleportTo(spawn, Quaternion.identity, false);
        p.m_lastGroundTouch = 0f;

        if (_zNetView.GetZDO() is ZDO zdo && zdo.IsValid())
        {
            if (!p.m_customData.ContainsKey($"Wayshrine_{zdo.m_uid}"))
            {
                p.m_customData.Add($"Wayshrine_{zdo.m_uid}", $"{zdo.m_uid}");
            }
        }

        return true;
    }

    internal static void ShowPointAddPins(Transform t, Player? p)
    {
        MinimapPatches.IsHeimdallMode = true;
        Minimap.instance.ShowPointOnMap(t.position);
        foreach (KeyValuePair<Vector3, WayshrineCustomBehaviour.WayshrineInfo> kv in WayshrineCustomBehaviour.Wayshrines)
        {
            /* This code will add all the correct pins to the map for each different type of shrine. */
            WayshrineCustomBehaviour wayshrine = kv.Value.Prefab.GetComponent<WayshrineCustomBehaviour>();
            if (p.m_customData.ContainsKey($"Wayshrine_{wayshrine._zdoId}"))
            {
                WayshrineCustomBehaviour.pins.Add(Minimap.instance.AddPin(kv.Key, wayshrine.pinType, Util.GetLocalized(kv.Value.Prefab.GetComponent<Piece>().m_name), false, false));
            }
        }
    }

    internal static void AddToPlayerIfNotExists(ZNetView _zNetView, Player? p)
    {
        if (_zNetView.GetZDO() is ZDO zdo2 && zdo2.IsValid())
        {
            if (!p.m_customData.ContainsKey($"Wayshrine_{zdo2.m_uid}"))
            {
                p.m_customData.Add($"Wayshrine_{zdo2.m_uid}", $"{zdo2.m_uid}");
            }
        }
    }
}