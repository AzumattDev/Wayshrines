using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using ServerSync;
using UnityEngine;

namespace Wayshrine
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    public class WayshrinePlugin : BaseUnityPlugin
    {
        internal const string ModName = "AzuWayshrine";
        internal const string ModVersion = "1.0.6";
        internal const string ModGUID = "azumatt.Wayshrine";
        public static bool isAdmin = false;
        public static bool hammerAdded = false;
        public static Sprite way_icon = null!;
        public static Sprite way_icon_ash = null!;
        public static Sprite way_icon_frost = null!;
        public static Sprite way_icon_plains = null!;
        public static Sprite way_icon_skull = null!;
        public static Sprite way_icon_skull_2 = null!;
        public static readonly ManualLogSource waylogger = BepInEx.Logging.Logger.CreateLogSource(ModName);

        private static readonly ConfigSync configSync = new(ModGUID)
        { DisplayName = ModName, CurrentVersion = ModVersion, MinimumRequiredVersion = ModVersion };

        private static ConfigFile localizationFile = null!;
        private static readonly Dictionary<string, ConfigEntry<string>> m_localizedStrings = new();

        private static ConfigEntry<bool>? ServerConfigLocked;
        public static ConfigEntry<bool>? OriginalFunc;
        public static ConfigEntry<bool>? DisableBifrostEffect;
        public static ConfigEntry<bool>? teleportable;

        private ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description,
            bool synchronizedSetting = true)
        {
            ConfigEntry<T> configEntry = Config.Bind(group, name, value, description);

            SyncedConfigEntry<T> syncedConfigEntry = configSync.AddConfigEntry(configEntry);
            syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

            return configEntry;
        }

        private ConfigEntry<T> config<T>(string group, string name, T value, string description,
            bool synchronizedSetting = true)
        {
            return config(group, name, value, new ConfigDescription(description), synchronizedSetting);
        }

        public void Awake()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            Harmony harmony = new(ModGUID);
            localizationFile =
                new ConfigFile(
                    Path.Combine(Path.GetDirectoryName(Config.ConfigFilePath)!, ModGUID + ".Localization.cfg"), false);
            Assets.LoadAssets();

            ServerConfigLocked = config("General", "Force Server Config", false, "Force Server Config");
            configSync.AddLockingConfigEntry(ServerConfigLocked);
            config("General", "NexusID", 1298, "Nexus mod ID for updates");
            OriginalFunc = config("General", "Original Function", false,
                "Use the original functionality of the Wayshrines, unlink them and only take you to spawn or home");
            DisableBifrostEffect = config("General", "Disable Bifrost Effect", false,
                "Disable the bifrost effect on teleport");
            teleportable = config("General", "AllowTeleport", false, "Enable teleport with restricted items");


            harmony.PatchAll(assembly);
            MethodInfo methodInfo = AccessTools.Method(typeof(ZNet), "RPC_CharacterID",
                new[] { typeof(ZRpc), typeof(ZDOID) });
            harmony.Patch(methodInfo, null,
                new HarmonyMethod(AccessTools.Method(typeof(AdminGET), "RPC_Char",
                    new[] { typeof(ZNet), typeof(ZRpc) })));
            Localize();
        }

        public void OnDestroy()
        {
            localizationFile.Save();
            Config.Save();
        }

        private static void Localize()
        {
            try
            {
                LocalizeWord("piece_azuwayshrine", "Wayshrine");
                LocalizeWord("piece_azuwayshrine_ashlands", "Ashlands Wayshrine");
                LocalizeWord("piece_azuwayshrine_frost", "Frost Wayshrine");
                LocalizeWord("piece_azuwayshrine_plains", "Plains Wayshrine");
                LocalizeWord("piece_azuwayshrine_skull", "Skull Wayshrine");
                LocalizeWord("piece_azuwayshrine_skull_2", "Skull Wayshrine");
                LocalizeWord("wayshrine_activate", "Activate");
                LocalizeWord("activated_heimdall", "Heimdall opens the Bifrost!");
                LocalizeWord("wayshrine_description", "Call to Heimdall, for he shall take you home");
            }
            catch (Exception ex)
            {
                waylogger.LogError($"{ex}");
            }
        }

        private static void LocalizeWord(string key, string val)
        {
            if (!m_localizedStrings.ContainsKey(key))
            {
                Localization? loc = Localization.instance;
                string? langSection = loc.GetSelectedLanguage();
                ConfigEntry<string>? configEntry = localizationFile.Bind(langSection, key, val);
                Localization.instance.AddWord(key, configEntry.Value);
                m_localizedStrings.Add(key, configEntry);
            }
        }
    }
}