using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using PieceManager;
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
            MaterialReplacer.RegisterGameObjectForShaderSwap(wayshrineGO, MaterialReplacer.ShaderType.PieceShader);
            WayshrineCustomBehaviour wayshrineComponent = wayshrineGO.AddComponent<WayshrineCustomBehaviour>();
            var location = wayshrineGO.AddComponent<Location>();

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

        [HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.SetupLocations))]
        static class ZoneSystemSetupLocationsPatch
        {
            static readonly Dictionary<string, Heightmap.Biome> biomeMapping = new()
            {
                { "Frost", Heightmap.Biome.DeepNorth },
                { "Ashland", Heightmap.Biome.AshLands },
                { "Plains", Heightmap.Biome.Plains },
                { "Wayshrine", Heightmap.Biome.BlackForest },
                { "Skull_2", Heightmap.Biome.Mistlands },
            };
    
            static void Prefix(ZoneSystem __instance)
            {
                foreach (GameObject wayshrine in wayshrinesList)
                {
                    var biome = Heightmap.Biome.Swamp;
                    foreach (var entry in biomeMapping)
                    {
                        if (wayshrine.name.Contains(entry.Key))
                        {
                            biome = entry.Value;
                            break;
                        }
                    }

                    Location wayshrineLocation = wayshrine.GetComponent<Location>();
                    wayshrineLocation.m_clearArea = true;
                    wayshrineLocation.m_exteriorRadius = 16;
                    wayshrineLocation.m_noBuild = true;

                    foreach (GameObject gameObject in Resources.FindObjectsOfTypeAll<GameObject>())
                    {
                        if (gameObject.name == "_Locations" && gameObject.transform.Find("Misc") is Transform locationMisc)
                        {
                            WayshrinePlugin.waylogger.LogDebug("Adding the shit");
                            GameObject altarCopy = Object.Instantiate(wayshrine, locationMisc, true);
                            altarCopy.name = wayshrine.name;
                            __instance.m_locations.Add(CreateZoneLocation(biome, wayshrine.name));
                        }
                    }
                }
            }

            static ZoneSystem.ZoneLocation CreateZoneLocation(Heightmap.Biome biome, string prefabName)
            {
                return new ZoneSystem.ZoneLocation
                {
                    m_randomRotation = true,
                    m_minAltitude = 10,
                    m_maxAltitude = 1000,
                    m_maxDistance = 1500,
                    m_quantity = 5,
                    m_biome = biome,
                    m_prefabName = prefabName,
                    m_enable = true,
                    m_minDistanceFromSimilar = 1000,
                    m_prioritized = true,
                    m_forestTresholdMax = 5,
                    m_forestTresholdMin = 0
                };
            }
        }

    }
}