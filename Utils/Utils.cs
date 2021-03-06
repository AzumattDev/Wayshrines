using System.Collections.Generic;
using UnityEngine;

namespace Wayshrine
{
    public static class Util
    {
        public static string GetLocalized(string param)
        {
            return Localization.instance.Localize(param);
        }

        public static void sendWayshrines(long target)
        {
            List<ZDO> wayshrineZDOs = new();
            foreach (GameObject gameObject in Assets.wayshrinesList)
            {
                ZDOMan.instance.GetAllZDOsWithPrefab(gameObject.name, wayshrineZDOs);
            }

            sendWayshrines(target, wayshrineZDOs);
        }

        public static void sendWayshrines(long target, List<ZDO> wayshrineZDOs)
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

        public static void readWayshrines(long target, ZPackage package)
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
                        { prefab = ZNetScene.instance.GetPrefab(prefabId), rotation = rotation };
                }
            }
        }
        
        public static void DeleteWayZDOs(long sender, ZPackage pkg)
        {
            List<ZDO> wayshrineZDOs = new();
            foreach (GameObject gameObject in Assets.wayshrinesList)
            {
                ZDOMan.instance.GetAllZDOsWithPrefab(gameObject.name, wayshrineZDOs);
            }
            if (wayshrineZDOs.Count == 0)
            {
                return;
            }
            foreach (ZDO zdo in wayshrineZDOs) ZDOMan.instance.m_destroySendList.Add(zdo.m_uid);
            foreach (Minimap.PinData pinData in WayshrineCustomBehaviour.pins) Minimap.instance.RemovePin(pinData);

            WayshrineCustomBehaviour.pins.Clear();
            sendWayshrines(0);
        }
    }
}