using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace Wayshrine
{
    public static class Assets
    {
        private static int
            MinimapPinId =
                180; //start at 180 just in case other mods or the devs want to add more pins. Hopefully dodges a conflict

        public static GameObject wayshrine = null!;
        private static GameObject wayshrine_ash = null!;
        private static GameObject wayshrine_plains = null!;
        private static GameObject wayshrine_frost = null!;
        private static GameObject wayshrine_skull = null!;
        private static GameObject wayshrine_skull_2 = null!;
        private static GameObject vfx_bifrost = null!;
        public static readonly List<GameObject> wayshrinesList = new();
        private static bool AssetsLoaded;

        private static AssetBundle GetAssetBundleFromResources(string filename)
        {
            Assembly execAssembly = Assembly.GetExecutingAssembly();
            string resourceName = execAssembly.GetManifestResourceNames()
                .Single(str => str.EndsWith(filename));

            using Stream? stream = execAssembly.GetManifestResourceStream(resourceName);
            return AssetBundle.LoadFromStream(stream);
        }

        private static void InitAssets(GameObject wayshrineGO)
        {
            WayshrineCustomBehaviour wayshrineComponent = wayshrineGO.AddComponent<WayshrineCustomBehaviour>();
            //var location = wayshrineGO.AddComponent<Location>();

            Piece? piece = wayshrineGO.GetComponent<Piece>();
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

            assetBundle.Unload(false);
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

        /*[HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.SetupLocations))]
        static class ZoneSystem_SetupLocations_Patch
        {
            static void Prefix(ZoneSystem __instance)
            {
                foreach (GameObject wayshrine in wayshrinesList)
                {
                    Heightmap.Biome biome;
                    if (wayshrine.name.Contains("Frost"))
                    {
                        biome = Heightmap.Biome.DeepNorth;
                    }
                    else if (wayshrine.name.Contains("Ashland"))
                    {
                        biome = Heightmap.Biome.AshLands;
                    }
                    else if (wayshrine.name.Contains("Plains"))
                    {
                        biome = Heightmap.Biome.Plains;
                    }
                    else if (wayshrine.name == "Wayshrine")
                    {
                        biome = Heightmap.Biome.BlackForest;
                    }
                    else if (wayshrine.name.Contains("Skull_2"))
                    {
                        biome = Heightmap.Biome.Mistlands;
                    }
                    else
                    {
                        biome = Heightmap.Biome.Swamp;
                    }

                    Location wayshrineLocation = wayshrine.GetComponent<Location>();
                    wayshrineLocation.m_clearArea = true;
                    wayshrineLocation.m_exteriorRadius = 16;
                    wayshrineLocation.m_noBuild = true;

                    foreach (GameObject gameObject in Resources.FindObjectsOfTypeAll<GameObject>())
                    {
                        if (gameObject.name == "_Locations" &&
                            gameObject.transform.Find("Misc") is Transform locationMisc)
                        {
                            GameObject altarCopy = Object.Instantiate(wayshrine, locationMisc, true);
                            altarCopy.name = wayshrine.name;
                            __instance.m_locations.Add(new ZoneSystem.ZoneLocation
                            {
                                m_randomRotation = true,
                                m_minAltitude = 10,
                                m_maxAltitude = 1000,
                                m_maxDistance = 1500,
                                m_quantity = 5,
                                m_biome = Heightmap.Biome.Meadows,
                                m_prefabName = wayshrine.name,
                                m_enable = true,
                                m_minDistanceFromSimilar = 800,
                                m_prioritized = true,
                                m_forestTresholdMax = 5
                            });
                        }
                    }
                }
            }
        }*/
    }
}