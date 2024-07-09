using HarmonyLib;
using Wayshrine.Utils;

namespace Wayshrine.Patches
{
    [HarmonyPatch(typeof(Game), nameof(Game._RequestRespawn))]
    public class SendWayshrinesGameRespawnPatch
    {
        private static void Postfix(Game __instance)
        {
            /* To keep the pins updated and the interaction with the wayshrine instant,
             we will RPC the location of all wayshrines to the client on connection AKA HERE...*/
            if (ZNet.instance.IsServer())
                Util.SendWayshrines(0);
        }
    }
}