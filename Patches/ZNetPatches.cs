using HarmonyLib;
using JetBrains.Annotations;
using Wayshrine.Utils;

namespace Wayshrine.Patches
{
    [HarmonyPatch(typeof(ZNet), nameof(ZNet.RPC_PeerInfo))]
    public static class PatchZNetRPC_PeerInfo
    {
        [UsedImplicitly]
        private static void Postfix(ZNet __instance, ZRpc rpc)
        {
            if (__instance.IsServer())
            {
                Util.SendWayshrines(__instance.GetPeer(rpc).m_uid);
            }
        }
    }

    [HarmonyPatch(typeof(ZNet), nameof(ZNet.Awake))]
    public static class PatchWayshrineRPCRegistration
    {
        private static void Postfix()
        {
            ZRoutedRpc.instance.Register<ZPackage>(WayshrinePlugin.RPC_RequestWayshrines, Util.ReadWayshrines);

            ZRoutedRpc.instance.Register<ZPackage>(WayshrinePlugin.RPC_DeleteWayshrines, Util.DeleteWayZDOs);
        }
    }
}
