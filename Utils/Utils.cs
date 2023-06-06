using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace Wayshrine
{
    public static class Util
    {
        public static string GetLocalized(string param)
        {
            return Localization.instance.Localize(param);
        }

        public static void SendWayshrines(long target)
        {
            List<ZDO> wayshrineZDOs = new();
            foreach (GameObject gameObject in Assets.wayshrinesList)
            {
                GetAllZDOsWithPrefab(gameObject.name, wayshrineZDOs);
            }

            SendWayshrines(target, wayshrineZDOs);
        }
        
        public static void GetAllZDOsWithPrefab(string prefab, List<ZDO> zdos)
        {
            int stableHashCode = prefab.GetStableHashCode();
            foreach (ZDO zdo in ZDOMan.instance.m_objectsByID.Values)
            {
                if (zdo.GetPrefab() == stableHashCode)
                    zdos.Add(zdo);
            }
        }

        public static void SendWayshrines(long target, List<ZDO> wayshrineZDOs)
        {
            ZPackage package = new();
            package.Write(wayshrineZDOs.Count);
            foreach (ZDO zdo in wayshrineZDOs)
            {
                package.Write(zdo.m_position);
                package.Write(zdo.GetRotation());
                package.Write(zdo.GetPrefab());
            }

            ZRoutedRpc.instance.InvokeRoutedRPC(target, "RequestWayZDOs", package);
        }

        public static void ReadWayshrines(long target, ZPackage package)
        {
            for (int i = package.ReadInt(); i > 0; --i)
            {
                Vector3 position = package.ReadVector3();
                Quaternion rotation = package.ReadQuaternion();
                int prefabId = package.ReadInt();
                if (prefabId == 0)
                {
                    WayshrineCustomBehaviour.Wayshrines.Remove(position);
                }
                else
                {
                    WayshrineCustomBehaviour.Wayshrines[position] = new WayshrineCustomBehaviour.WayshrineInfo
                        { Prefab = ZNetScene.instance.GetPrefab(prefabId), Rotation = rotation };
                }
            }
        }

        // ReSharper disable once InconsistentNaming
        public static void DeleteWayZDOs(long sender, ZPackage pkg)
        {
            if (ZNet.m_isServer)
            {
                WayshrinePlugin.waylogger.LogMessage("Deleting wayshrines");
            }

            List<ZDO> wayshrineZDOs = new();
            foreach (GameObject gameObject in Assets.wayshrinesList)
            {
                GetAllZDOsWithPrefab(gameObject.name, wayshrineZDOs);
            }

            if (wayshrineZDOs.Count == 0)
            {
                return;
            }

            foreach (ZDO zdo in wayshrineZDOs) ZDOMan.instance.m_destroySendList.Add(zdo.m_uid);
            foreach (Minimap.PinData pinData in WayshrineCustomBehaviour.pins) Minimap.instance.RemovePin(pinData);

            WayshrineCustomBehaviour.pins.Clear();
            SendWayshrines(0);
        }
    }
}