using HarmonyLib;
using UnityEngine;

namespace Wayshrine
{
    [HarmonyPatch]
    public class PlayerPatches
    {
        //private static GameObject backpack;
        private static Inventory? _backpackInventory;

        //Second Backpack container/inv
        private static Inventory? _backpackInventory2;

        private static int _bagininv;

        private static PieceTable? _hammer;

        [HarmonyPatch(typeof(Player), nameof(Player.OnSpawned))]
        public static class Wayshrine_OnSpawned_Patch
        {
            public static void Postfix(Player __instance)
            {
                //Util.RPC_GenerateWayshrines(null);
                if (ZNet.instance.IsLocalInstance())
                {
                    WayshrinePlugin.isAdmin = true;
                    WayshrinePlugin.waylogger.LogDebug("Local Play Detected setting Admin: True");
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
    }
}