using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace Wayshrine
{
    public class Assets
    {
        public static int
            MinimapPinId =
                180; //start at 180 just in case other mods or the devs want to add more pins. Hopefully dodges a conflict

        public static GameObject wayshrine = null!;
        public static GameObject wayshrine_ash = null!;
        public static GameObject wayshrine_plains = null!;
        public static GameObject wayshrine_frost = null!;
        public static GameObject wayshrine_skull = null!;
        public static GameObject wayshrine_skull_2 = null!;
        public static GameObject vfx_bifrost = null!;
        public static readonly List<GameObject> wayshrinesList = new();
        private static bool AssetsLoaded;

        private static AssetBundle GetAssetBundleFromResources(string filename)
        {
            var execAssembly = Assembly.GetExecutingAssembly();
            var resourceName = execAssembly.GetManifestResourceNames()
                .Single(str => str.EndsWith(filename));

            using var stream = execAssembly.GetManifestResourceStream(resourceName);
            return AssetBundle.LoadFromStream(stream);
        }

        public static void InitAssets(GameObject wayshrineGO)
        {
            WayshrineCustomBehaviour wayshrineComponent = wayshrineGO.AddComponent<WayshrineCustomBehaviour>();
            var piece = wayshrineGO.GetComponent<Piece>();
            //piece.m_name = name;
            piece.m_description = "Call to Heimdall, for he shall take you home";
            piece.m_canBeRemoved = false;
            piece.m_primaryTarget = false;
            piece.m_randomTarget = true;
            piece.m_enabled = true;
            wayshrinesList.Add(wayshrineGO);

            wayshrineComponent.pinType = (Minimap.PinType)MinimapPinId++;
        }

        public static void LoadAssets()
        {
            AssetBundle assetBundle = GetAssetBundleFromResources("azuwayshrine");
            wayshrine = assetBundle.LoadAsset<GameObject>("Wayshrine");
            wayshrine_ash = assetBundle.LoadAsset<GameObject>("Wayshrine_Ashlands");
            wayshrine_frost = assetBundle.LoadAsset<GameObject>("Wayshrine_Frost");
            wayshrine_plains = assetBundle.LoadAsset<GameObject>("Wayshrine_Plains");
            wayshrine_skull = assetBundle.LoadAsset<GameObject>("Wayshrine_Skull");
            wayshrine_skull_2 = assetBundle.LoadAsset<GameObject>("Wayshrine_Skull_2");
            vfx_bifrost = assetBundle.LoadAsset<GameObject>("vfx_bifrost");

            InitAssets(wayshrine);
            InitAssets(wayshrine_ash);
            InitAssets(wayshrine_frost);
            InitAssets(wayshrine_plains);
            InitAssets(wayshrine_skull);
            InitAssets(wayshrine_skull_2);

            assetBundle?.Unload(false);
            AssetsLoaded = true;
        }

        [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
        public static class Wayshrine_ZNetScene_Awake_Patch
        {
            public static bool Prefix(ZNetScene __instance)
            {
                if (!AssetsLoaded) LoadAssets();

                foreach (GameObject wayshrine in wayshrinesList) __instance.m_prefabs.Add(wayshrine);
                __instance.m_prefabs.Add(vfx_bifrost);

                WayshrinePlugin.waylogger.LogDebug("Loading the prefabs");
                return true;
            }
        }
    }
}