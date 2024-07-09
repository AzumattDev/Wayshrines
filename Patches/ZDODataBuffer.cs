using System.Collections.Generic;
using HarmonyLib;

namespace Wayshrine.Patches;

/* Pretty much all of this code is to help offset issues with BetterNetworking */

public static class WayshrineZdoDataBuffer
{
    private static readonly List<ZPackage> PackageBuffer = new();

    [HarmonyPatch(typeof(ZNet), nameof(ZNet.OnNewConnection))]
    private class StartBufferingOnNewConnection
    {
        private static void Postfix(ZNet __instance, ZNetPeer peer)
        {
            if (!__instance.IsServer())
            {
                peer.m_rpc.Register<ZPackage>("ZDOData", (_, package) => PackageBuffer.Add(package));
            }
        }
    }

    [HarmonyPatch(typeof(ZNet), nameof(ZNet.Shutdown))]
    private class WayshrineClearPackageBufferOnShutdown
    {
        private static void Postfix() => PackageBuffer.Clear();
    }

    [HarmonyPatch(typeof(ZDOMan), nameof(ZDOMan.AddPeer))]
    private class WayshrineEvaluateBufferedPackages
    {
        private static void Postfix(ZDOMan __instance, ZNetPeer netPeer)
        {
            foreach (ZPackage package in PackageBuffer)
            {
                __instance.RPC_ZDOData(netPeer.m_rpc, package);
            }

            PackageBuffer.Clear();
        }
    }
}