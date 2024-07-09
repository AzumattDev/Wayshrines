using System;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using Wayshrine;
using Wayshrine.Extensions;
using Wayshrine.Patches;
using Wayshrine.Utils;

public class WayEntryDetector : MonoBehaviour
{
    public CapsuleCollider m_wayTELEPORT = null!;
    internal ZNetView _zNetView = null!;

    private void Awake()
    {
        _zNetView = GetComponentInParent<ZNetView>();
        if (_zNetView == null)
            WayshrinePlugin.waylogger.LogError("ZNetView is null");
        if (_zNetView.GetZDO() is ZDO zdo && zdo.IsValid())
        {
            WayshrinePlugin.waylogger.LogDebug("ZDO is valid WayEntryDetector");
        }
        else
        {
            WayshrinePlugin.waylogger.LogError("ZDO is not valid on the WayEntryDetector");
        }
    }

    private void OnTriggerEnter(Collider collider)
    {
        Player component = collider.GetComponent<Player>();
        if (component == null || Player.m_localPlayer != component)
            return;
        PlayerProfile playerProfile = Game.instance.GetPlayerProfile();
        if (!component.IsTeleportable())
        {
            if (WayshrinePlugin.Teleportable.Value == WayshrinePlugin.Toggle.Off)
            {
                component.Message(MessageHud.MessageType.Center, "$msg_noteleport");
                return;
            }
        }

        if (!Util.CheckTeleportCost())
        {
            component.Message(MessageHud.MessageType.Center, $"$wayshrine_cost_error : {WayshrinePlugin.ChargeItemAmount.Value} {MinimapPatches.ItemName}");
            return;
        }

        Util.AddToPlayerIfNotExists(_zNetView, component);

        if (Util.UseOriginalFunc(component, playerProfile, component, _zNetView))
            return;

        Util.ShowPointAddPins(transform, component);
    }

    private void OnTriggerExit(Collider collider)
    {
        Minimap.instance.SetMapMode(Minimap.MapMode.Small);
    }
}