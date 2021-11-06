using System;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace Wayshrine
{
    [HarmonyPatch(typeof(ZNet), nameof(ZNet.RPC_PeerInfo))]
    public static class PatchZNetRPC_PeerInfo
    {
        [UsedImplicitly]
        private static void Postfix(ZNet __instance, ZRpc rpc)
        {
            if (__instance.IsServer())
            {
                Util.sendWayshrines(__instance.GetPeer(rpc).m_uid);
            }
        }
    }

    [HarmonyPatch(typeof(ZNet), nameof(ZNet.Awake))]
    public static class PatchWayshrineRPCRegistration
    {
        private static void Postfix()
        {
            ZRoutedRpc.instance.Register<ZPackage>("RequestWayZDOs", Util.readWayshrines);

            ZRoutedRpc.instance.Register<ZPackage>("DeleteWayZDOs", Util.DeleteWayZDOs);
        }
    }

    [HarmonyPatch(typeof(ZNet), nameof(ZNet.OnNewConnection))]
    public static class PatchZNetOnNewConnection
    {
        private static void Postfix(ZNet __instance, ZNetPeer peer)
        {
            if (!__instance.IsServer()) return;
            if (__instance.m_adminList.Contains(peer.m_rpc.GetSocket().GetHostName()))
            {
                peer.m_rpc.Invoke("WayshrineAdminGetEvent");

            }
        }
    }

    [HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.Start))]
    public static class GameStartPatch
    {
        private static void Prefix()
        {
            ZRoutedRpc.instance.Register("WayshrineAdminGetEvent", new Action<long, ZPackage>(AdminGET.RPC_isAdminAccess));
        }
    }
    
    public class AdminGET
    {
        private static bool _isAdmin = false;
        private static PieceTable? _hammer;
        public static void RPC_isAdminAccess(long sender, ZPackage pAzu)
        {
            if (pAzu.Size() <= 0) return;
            bool getAdm = pAzu.ReadBool();
            WayshrinePlugin.isAdmin = getAdm;
            WayshrinePlugin.waylogger.LogMessage($"ADMIN DETECTED: {Player.m_localPlayer.GetPlayerName()}");
            if (!WayshrinePlugin.isAdmin || WayshrinePlugin.hammerAdded) return;
            foreach (var o in Resources.FindObjectsOfTypeAll(typeof(PieceTable)))
            {
                var table = (PieceTable)o;
                string name = table.gameObject.name;
                if (!name.Contains("_HammerPieceTable")) continue;
                _hammer = table;
                break;
            }
            if (_hammer is null || _hammer.m_pieces.Contains(Assets.wayshrine)) return;
            foreach (GameObject wayshrine in Assets.wayshrinesList)
            {
                _hammer.m_pieces.Add(wayshrine);
            }
            WayshrinePlugin.hammerAdded = true;
        }

        private static void RPC_Char(ZNet __instance, ZRpc rpc)
        {
            if (!__instance.IsDedicated() && !__instance.IsServer())
            {
                return;
            }
            ZNetPeer peer = __instance.GetPeer(rpc);
            string peerSteamId = peer.m_rpc.GetSocket().GetHostName();
            if (ZNet.instance.m_adminList != null && ZNet.instance.m_adminList.Contains(peerSteamId)) _isAdmin = true;
            ZPackage newPAzu = new();
            WayshrinePlugin.waylogger.LogMessage($"ADMIN DETECTED: {peerSteamId} a.k.a. {peer.m_playerName}");
            newPAzu.Write(_isAdmin);
            ZRoutedRpc.instance.InvokeRoutedRPC(peer.m_uid, "WayshrineAdminGetEvent", newPAzu);
        }
    }
}
