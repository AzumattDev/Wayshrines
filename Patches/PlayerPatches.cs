using HarmonyLib;
using UnityEngine;

namespace Wayshrine
{
    [HarmonyPatch]
    public class PlayerPatches
    {
        //private static GameObject backpack;
        private static Inventory _backpackInventory;

        //Second Backpack container/inv
        private static Inventory _backpackInventory2;

        private static int _bagininv;

        private static PieceTable? _hammer;

        [HarmonyPatch(typeof(Player), nameof(Player.OnSpawned))]
        public static class Wayshrine_OnSpawned_Patch
        {
            public static void Postfix(Player __instance)
            {
                if (ZNet.instance.IsLocalInstance())
                {
                    WayshrinePlugin.isAdmin = true;
#if DEBUG
                    WayshrinePlugin.waylogger.LogInfo("Local Play Detected setting Admin: True");
#endif
                }

                if (!WayshrinePlugin.isAdmin || WayshrinePlugin.hammerAdded) return;
                foreach (var o in Resources.FindObjectsOfTypeAll(typeof(PieceTable)))
                {
                    PieceTable? table = (PieceTable)o;
                    string name = table.gameObject.name;
                    if (!name.Contains("_HammerPieceTable")) continue;
                    _hammer = table;
                    break;
                }

                if (_hammer is not null && _hammer.m_pieces.Contains(Assets.wayshrine)) return;
                foreach (GameObject wayshrine in Assets.wayshrinesList)
                {
                    _hammer?.m_pieces.Add(wayshrine);
                    var piece = wayshrine.GetComponent<Piece>();
                    piece.m_canBeRemoved = true;
                }

                WayshrinePlugin.hammerAdded = true;
            }
        }
    }
}