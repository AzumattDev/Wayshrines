using HarmonyLib;

namespace Wayshrine
{
    [HarmonyPatch(typeof(Game), nameof(Game._RequestRespawn))]
    public class SendWayshrines_GameRespawnPatch
    {
        private static void Postfix(Game __instance)
        {
            if (__instance.m_firstSpawn && ZNet.instance.IsServer())
                /* to keep the pins updated and the interaction with the wayshrine instant, we will RPC the location of all wayshrines to the client on connection
                    AKA HERE...
                    */
                Util.SendWayshrines(0);
        }
    }
}