using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace Wayshrine;

public class AdminSyncing
{
    private static bool isServer;

    [HarmonyPriority(Priority.VeryHigh)]
    internal static void AdminStatusSync(ZNet __instance)
    {
        isServer = __instance.IsServer();
        ZRoutedRpc.instance.Register<ZPackage>(WayshrinePlugin.ModGUID + " WayAdminStatusSync",
            RPC_AdminPieceAddRemove);

        IEnumerator WatchAdminListChanges()
        {
            List<string> CurrentList = new(ZNet.instance.m_adminList.GetList());
            for (;;)
            {
                yield return new WaitForSeconds(30);
                if (!ZNet.instance.m_adminList.GetList().SequenceEqual(CurrentList))
                {
                    CurrentList = new List<string>(ZNet.instance.m_adminList.GetList());
                    List<ZNetPeer> adminPeer = ZNet.instance.GetPeers().Where(p =>
                        ZNet.instance.ListContainsId(ZNet.instance.m_adminList,p.m_rpc.GetSocket().GetHostName())).ToList();
                    List<ZNetPeer> nonAdminPeer = ZNet.instance.GetPeers().Except(adminPeer).ToList();
                    SendAdmin(nonAdminPeer, false);
                    SendAdmin(adminPeer, true);

                    void SendAdmin(List<ZNetPeer> peers, bool isAdmin)
                    {
                        ZPackage package = new();
                        package.Write(isAdmin);
                        ZNet.instance.StartCoroutine(sendZPackage(peers, package));
                    }
                }
            }
            // ReSharper disable once IteratorNeverReturns
        }

        if (isServer)
        {
            ZNet.instance.StartCoroutine(WatchAdminListChanges());
        }
    }

    private static IEnumerator sendZPackage(List<ZNetPeer> peers, ZPackage package)
    {
        if (!ZNet.instance)
        {
            yield break;
        }

        const int compressMinSize = 10000;

        if (package.GetArray() is { LongLength: > compressMinSize } rawData)
        {
            ZPackage compressedPackage = new();
            compressedPackage.Write(4);
            MemoryStream output = new();
            using (DeflateStream deflateStream = new(output, System.IO.Compression.CompressionLevel.Optimal))
            {
                deflateStream.Write(rawData, 0, rawData.Length);
            }

            compressedPackage.Write(output.ToArray());
            package = compressedPackage;
        }

        List<IEnumerator<bool>> writers =
            peers.Where(peer => peer.IsReady()).Select(p => TellPeerAdminStatus(p, package)).ToList();
        writers.RemoveAll(writer => !writer.MoveNext());
        while (writers.Count > 0)
        {
            yield return null;
            writers.RemoveAll(writer => !writer.MoveNext());
        }
    }

    private static IEnumerator<bool> TellPeerAdminStatus(ZNetPeer peer, ZPackage package)
    {
        if (ZRoutedRpc.instance is not { } rpc)
        {
            yield break;
        }

        SendPackage(package);

        void SendPackage(ZPackage pkg)
        {
            string method = WayshrinePlugin.ModGUID + " WayAdminStatusSync";
            if (isServer)
            {
                peer.m_rpc.Invoke(method, pkg);
            }
            else
            {
                rpc.InvokeRoutedRPC(peer.m_server ? 0 : peer.m_uid, method, pkg);
            }
        }
    }

    internal static void RPC_AdminPieceAddRemove(long sender, ZPackage package)
    {
        ZNetPeer? currentPeer = ZNet.instance.GetPeer(sender);
        bool admin = false;
        try
        {
            admin = package.ReadBool();
        }
        catch
        {
            // ignore
        }

        if (isServer)
        {
            ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, WayshrinePlugin.ModGUID + " WayAdminStatusSync", new ZPackage());
            if (ZNet.instance.m_adminList.Contains(currentPeer.m_rpc.GetSocket().GetHostName()))
            {
                ZPackage pkg = new();
                pkg.Write(true);
                currentPeer.m_rpc.Invoke(WayshrinePlugin.ModGUID + " WayAdminStatusSync", pkg);
            }
        }
        else
        {
            // Remove everything they shouldn't be able to build by disabling and removing.
            foreach (GameObject? piece in Assets.wayshrinesList)
            {
                Piece piecePrefab = piece.GetComponent<Piece>();
                string pieceName = piecePrefab.m_name;
                string localizedName = Localization.instance.Localize(pieceName).Trim();
                if (!ObjectDB.instance || ObjectDB.instance.GetItemPrefab("Wood") == null) continue;
                foreach (Piece instantiatedPiece in UnityEngine.Object.FindObjectsOfType<Piece>())
                {
                    if (admin)
                    {
                        if (instantiatedPiece.m_name == pieceName)
                        {
                            instantiatedPiece.m_enabled = true;
                        }
                    }
                    else
                    {
                        if (instantiatedPiece.m_name == pieceName)
                        {
                            instantiatedPiece.m_enabled = false;
                        }
                    }
                }

                List<GameObject>? hammerPieces = ObjectDB.instance.GetItemPrefab("Hammer").GetComponent<ItemDrop>()
                    .m_itemData.m_shared.m_buildPieces
                    .m_pieces;
                if (admin)
                {
                    if (!hammerPieces.Contains(ZNetScene.instance.GetPrefab(piecePrefab.name)))
                        hammerPieces.Add(ZNetScene.instance.GetPrefab(piecePrefab.name));
                }
                else
                {
                    if (hammerPieces.Contains(ZNetScene.instance.GetPrefab(piecePrefab.name)))
                        hammerPieces.Remove(ZNetScene.instance.GetPrefab(piecePrefab.name));
                }
            }
        }
    }
}

[HarmonyPatch(typeof(ZNet), nameof(ZNet.OnNewConnection))]
class RegisterClientRPCPatch
{
    private static void Postfix(ZNet __instance, ZNetPeer peer)
    {
        if (!__instance.IsServer())
        {
            peer.m_rpc.Register<ZPackage>(WayshrinePlugin.ModGUID + " WayAdminStatusSync",
                RPC_InitialAdminSync);
        }
        else
        {
            ZPackage packge = new();
            packge.Write(__instance.m_adminList.Contains(peer.m_rpc.GetSocket().GetHostName()));

            peer.m_rpc.Invoke(WayshrinePlugin.ModGUID + " WayAdminStatusSync", packge);
        }
    }

    private static void RPC_InitialAdminSync(ZRpc rpc, ZPackage package) =>
        AdminSyncing.RPC_AdminPieceAddRemove(0, package);
}

[HarmonyPatch(typeof(ZNet),nameof(ZNet.Awake))]
static class ZNetAwakePatch
{
    static void Postfix(ZNet __instance)
    {
        AdminSyncing.AdminStatusSync(__instance);
    }
}