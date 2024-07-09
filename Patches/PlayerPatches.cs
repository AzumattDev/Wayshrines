using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using Wayshrine.Utils;

namespace Wayshrine.Patches;

[HarmonyPatch(typeof(Player), nameof(Player.OnSpawned))]
static class WayshrineOnSpawnedPatch
{
    private static PieceTable? _hammer;

    public static void Postfix(Player __instance)
    {
        List<string> adminList = ZNet.instance.GetAdminList();
        string networkUserId = UserInfo.GetLocalUser().NetworkUserId;
        bool admin = networkUserId != null && adminList != null && adminList.Contains(networkUserId);
        //Util.RPC_GenerateWayshrines(null);
        if (ZNet.instance.IsLocalInstance() || admin)
        {
            WayshrinePlugin.isAdmin = true;
            WayshrinePlugin.waylogger.LogDebug(!admin ? $"Local Play Detected setting Admin: True" : $"Setting Admin: True");
        }

        if (!WayshrinePlugin.isAdmin || WayshrinePlugin.hammerAdded) return;
        GameObject? hammer = ZNetScene.instance.GetPrefab("Hammer");
        PieceTable? hammerTable = hammer.GetComponent<ItemDrop>().m_itemData.m_shared.m_buildPieces;
        _hammer = hammerTable;

        if (_hammer is not null && _hammer.m_pieces.Contains(Assets.wayshrine)) return;
        foreach (GameObject wayshrine in Assets.wayshrinesList)
        {
            _hammer?.m_pieces.Add(wayshrine);
            Piece? piece = wayshrine.GetComponent<Piece>();
            piece.m_canBeRemoved = true;
        }

        WayshrinePlugin.hammerAdded = true;
    }
}