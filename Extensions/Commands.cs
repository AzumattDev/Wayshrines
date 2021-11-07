using HarmonyLib;

namespace Wayshrine
{
    [HarmonyPatch(typeof(Terminal), nameof(Terminal.InitTerminal))]
    public static class WayInitTerminal_Patch
    {
        private static void Postfix()
        {
#if DEBUG
            WayshrinePlugin.waylogger.LogDebug("Patching Console Commands");
#endif

            /*if (!ValidServer || !Admin)
                return;*/


            /* Delete Wayshrines and Wayshrine Pins */
            Terminal.ConsoleCommand deleteWayshrinesCommand = new("delway", "Delete Wayshrines and Wayshrine Pins",
                args =>
                {
                    ZRoutedRpc.instance.InvokeRoutedRPC("DeleteWayZDOs", new ZPackage());
                    args.Context?.AddString(
                            "<color=yellow>Delete Wayshrines called. Trying to delete all ZDOs</color>");
                    ZDOMan.instance.SendDestroyed();
                    Util.sendWayshrines(0);
                }, true);
        }
    }
}