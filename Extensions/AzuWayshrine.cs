using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using Wayshrine.Patches;
using Wayshrine.Utils;

namespace Wayshrine.Extensions;

public class WayshrineCustomBehaviour : MonoBehaviour, Hoverable, Interactable
{
    public static readonly Dictionary<Vector3, WayshrineInfo> Wayshrines = new();
    public static readonly List<Minimap.PinData> pins = new();

    internal ZNetView _zNetView = null!;
    internal ZDOID _zdoId = ZDOID.None;

    private Piece piece = null!;
    public Minimap.PinType pinType;
    private Piece wayshrinePiece = null!;

    public string GetHoverText()
    {
        return WayshrinePlugin.OriginalFunc.Value == WayshrinePlugin.Toggle.On ? Util.GetLocalized(piece.m_name + $"\n[<color=yellow><b>$KEY_Use</b></color>] $wayshrine_activate\n[<color=yellow><b>{WayshrinePlugin.ModifierKey.Value} + $KEY_Use</b></color>] $wayshrine_teleportHome") : Util.GetLocalized(piece.m_name + $"\n[<color=yellow><b>$KEY_Use</b></color>] $wayshrine_activate");
    }

    public string GetHoverName()
    {
        return Util.GetLocalized(piece.m_name);
    }

    public bool Interact(Humanoid character, bool hold, bool alt)
    {
        if (hold || Player.m_localPlayer == null || Player.m_localPlayer != (Player)character)
            return true;

        Player? p = character as Player;
        PlayerProfile playerProfile = Game.instance.GetPlayerProfile();
        if (!character.IsTeleportable())
        {
            if (WayshrinePlugin.Teleportable.Value == WayshrinePlugin.Toggle.Off)
            {
                character.Message(MessageHud.MessageType.Center, "$msg_noteleport");
                return true;
            }
        }

        if (!Util.CheckTeleportCost())
        {
            Player.m_localPlayer.Message(MessageHud.MessageType.Center, $"$wayshrine_cost_error : {WayshrinePlugin.ChargeItemAmount.Value} {MinimapPatches.ItemName}");
            return true;
        }

        Util.AddToPlayerIfNotExists(_zNetView, p);
        if (Util.UseOriginalFunc(p, playerProfile, character, _zNetView))
            return true;

        Util.ShowPointAddPins(transform, p);

        return false;
    }

    public bool UseItem(Humanoid user, ItemDrop.ItemData item)
    {
        return false;
    }

    private void Awake()
    {
        /* in the awake method of our component, we will send the location of the new wayshrine. check for m_ownerRevision being 0 and increase it afterwards */
        piece = GetComponent<Piece>();
        _zNetView = GetComponent<ZNetView>();

        if (_zNetView.GetZDO() is ZDO zdo && zdo.IsValid())
        {
            if (zdo.OwnerRevision == 0)
            {
                zdo.IncreaseOwnerRevision();

                Util.SendWayshrines(ZRoutedRpc.Everybody, new List<ZDO> { zdo });
            }
            _zdoId = zdo.m_uid;
        }
        else
        {
            DestroyImmediate(this);
        }
    }

    public void OnDestroy()
    {
        if (Wayshrines.ContainsKey(transform.position))
        {
            ZPackage package = new();
            package.Write(1);
            package.Write(transform.position);
            package.Write(transform.rotation);
            package.Write(0);
            package.Write(new ZDOID());
            ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, WayshrinePlugin.RPC_RequestWayshrines, package);
        }
    }

    public void Start()
    {
        if (WayshrinePlugin.isAdmin)
        {
            wayshrinePiece = GetComponent<Piece>();
            wayshrinePiece.m_canBeRemoved = true;
        }

        /* It appears Util.sendWayshrines(0) is needed here, otherwise the map pins disappear from the map after a distant teleport */
        Util.SendWayshrines(0);
    }

    public struct WayshrineInfo
    {
        public GameObject Prefab;
        public Quaternion Rotation;
        public Vector3 Position;
        public ZDOID ZdoId;
    }
}