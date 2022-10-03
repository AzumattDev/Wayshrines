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
        internal const string ModGUID = "Azumatt.Wayshrine";
        public static bool isAdmin = false;
        public static bool hammerAdded = false;
        public static Sprite way_icon = null!;
        public static Sprite way_icon_ash = null!;
        public static Sprite way_icon_frost = null!;
        public static Sprite way_icon_plains = null!;
        public static Sprite way_icon_skull = null!;
        public static Sprite way_icon_skull_2 = null!;

        private static string ConfigFileName = ModGUID + ".cfg";
        private static string ConfigFileFullPath = Paths.ConfigPath + Path.DirectorySeparatorChar + ConfigFileName;
        public static readonly ManualLogSource waylogger = BepInEx.Logging.Logger.CreateLogSource(ModName);

        private static readonly ConfigSync configSync = new(ModGUID)
            { DisplayName = ModName, CurrentVersion = ModVersion, MinimumRequiredVersion = ModVersion };

        private static ConfigFile localizationFile = null!;
        private static readonly Dictionary<string, ConfigEntry<string>> m_localizedStrings = new();

        private static ConfigEntry<bool>? _serverConfigLocked;
        public static ConfigEntry<bool>? OriginalFunc;
        public static ConfigEntry<bool>? DisableBifrostEffect;
        public static ConfigEntry<bool>? Teleportable;
        public static ConfigEntry<bool>? ShouldCost;
        public static ConfigEntry<string>? ChargeItem;
        public static ConfigEntry<int> ChargeItemAmount= null!;

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

            _serverConfigLocked = config("General", "Force Server Config", true, "Force Server Config");
            configSync.AddLockingConfigEntry(_serverConfigLocked);
            OriginalFunc = config("General", "Original Function", false,
                "Use the original functionality of the Wayshrines, unlink them and only take you to spawn or home");
            DisableBifrostEffect = config("General", "Disable Bifrost Effect", false,
                "Disable the bifrost effect on teleport");
            Teleportable = config("General", "AllowTeleport", false, "Enable teleport with restricted items");

            /* Charge */
            ShouldCost = config("Wayshrine Cost", "Should Cost?", false,
                "Should using the wayshrines cost the player something from their inventory?");
            ChargeItem = config("Wayshrine Cost", "Cost Item", "Coins",
                "Item needed to use the wayshrine. Limit is 1 item: Goes by prefab name. List of vanilla items here: https://github.com/Valheim-Modding/Wiki/wiki/ObjectDB-Table");
            ChargeItemAmount = config("Wayshrine Cost", "Cost Item Amount", 5,
                "Amount of the Item needed to teleport using a wayshrine.");

            SetupWatcher();
            harmony.PatchAll(assembly);
            MethodInfo methodInfo = AccessTools.Method(typeof(ZNet), nameof(ZNet.RPC_CharacterID),
                new[] { typeof(ZRpc), typeof(ZDOID) });
            harmony.Patch(methodInfo, null,
                new HarmonyMethod(AccessTools.Method(typeof(AdminGET), nameof(AdminGET.RPC_Char),
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
                LocalizeWord("wayshrine_cost_error", "Required Items needed");
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

        private void SetupWatcher()
        {
            FileSystemWatcher watcher = new(Paths.ConfigPath, ConfigFileName);
            watcher.Changed += ReadConfigValues;
            watcher.Created += ReadConfigValues;
            watcher.Renamed += ReadConfigValues;
            watcher.IncludeSubdirectories = true;
            watcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
            watcher.EnableRaisingEvents = true;
        }

        private void ReadConfigValues(object sender, FileSystemEventArgs e)
        {
            if (!File.Exists(ConfigFileFullPath)) return;
            try
            {
                waylogger.LogDebug("ReadConfigValues called");
                Config.Reload();
            }
            catch
            {
                waylogger.LogError($"There was an issue loading your {ConfigFileName}");
                waylogger.LogError("Please check your config entries for spelling and format!");
            }
        }
    }

    public static class ReflectionExtensions
    {
        public static T GetFieldValue<T>(this object obj, string name)
        {
            // Set the flags so that private and public fields from instances will be found
            var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
                               BindingFlags.FlattenHierarchy;
            var field = obj.GetType().GetField(name, bindingFlags);
            return (T)field?.GetValue(obj);
        }

        public static void SetFieldValue<T>(this object obj, string name, object value)
        {
            var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
                               BindingFlags.FlattenHierarchy;
            var field = obj.GetType().GetField(name, bindingFlags);
            field?.SetValue(obj, value);
        }

        public static object CallMethod(this object obj, string methodName, params object[] args)
        {
            var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
                               BindingFlags.FlattenHierarchy;
            var mi = obj.GetType().GetMethod(methodName, bindingFlags);
            if (mi != null)
            {
                return mi.Invoke(obj, args);
            }

            return null;
        }
    }
}