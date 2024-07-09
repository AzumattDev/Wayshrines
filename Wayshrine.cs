using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using JetBrains.Annotations;
using LocalizationManager;
using ServerSync;
using UnityEngine;
using Wayshrine.Utils;

namespace Wayshrine
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    public class WayshrinePlugin : BaseUnityPlugin
    {
        internal const string ModName = "AzuWayshrine";
        internal const string ModVersion = "1.1.3";
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
        private static readonly ConfigSync configSync = new(ModGUID) { DisplayName = ModName, CurrentVersion = ModVersion, MinimumRequiredVersion = ModVersion, ModRequired = true };
        private static readonly Dictionary<string, ConfigEntry<string>> MLocalizedStrings = new();

        // RPC Method Constants
        public const string RPC_RequestWayshrines = "RequestWayZDOs";
        public const string RPC_DeleteWayshrines = "DeleteWayZDOs";

        public enum Toggle
        {
            On = 1,
            Off = 0
        }

        public void Awake()
        {
            Localizer.Load();
            Assembly assembly = Assembly.GetExecutingAssembly();
            Harmony harmony = new(ModGUID);
            Assets.LoadAssets();

            _serverConfigLocked = config("General", "Force Server Config", Toggle.On, "Force the server configuration. If on, the configuration is locked and can be changed by server admins only.");
            configSync.AddLockingConfigEntry(_serverConfigLocked);
            OriginalFunc = config("General", "Original Function", Toggle.Off, "Use the original functionality of the Wayshrines, unlink them and only take you to spawn or home");
            DisableBifrostEffect = config("General", "Disable Bifrost Effect", Toggle.Off, "Disable the bifrost effect on teleport", false);
            Teleportable = config("General", "AllowTeleport", Toggle.Off, "Enable teleport with restricted items");

            ModifierKey = config("Hotkeys", "Interaction Modifier Key", new KeyboardShortcut(KeyCode.LeftAlt), new ConfigDescription("Personal hotkey to toggle a ward on which you're permitted on/off", new AcceptableShortcuts()), false);

            /* Charge */
            ShouldCost = config("Wayshrine Cost", "Should Cost?", Toggle.Off, new ConfigDescription("Should using the wayshrines cost the player something from their inventory?", null, new ConfigurationManagerAttributes() { Order = 2 }));
            ChargeItem = config("Wayshrine Cost", "Cost Item", "WayshrineToken", new ConfigDescription("Item needed to use the wayshrine. Limit is 1 item: Goes by prefab name. List of vanilla items here: https://github.com/Valheim-Modding/Wiki/wiki/ObjectDB-Table", null, new ConfigurationManagerAttributes() { Order = 1 }));
            ChargeItemAmount = config("Wayshrine Cost", "Cost Item Amount", 5, new ConfigDescription("Amount of the Item needed to teleport using a wayshrine.", null, new ConfigurationManagerAttributes() { Order = 0 }));

            SetupWatcher();
            harmony.PatchAll(assembly);
        }

        public void OnDestroy()
        {
            Config.Save();
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

        # region Config Options

        private static ConfigEntry<Toggle> _serverConfigLocked = null!;
        public static ConfigEntry<Toggle> OriginalFunc = null!;
        public static ConfigEntry<Toggle> DisableBifrostEffect = null!;
        public static ConfigEntry<Toggle> Teleportable = null!;
        public static ConfigEntry<Toggle> ShouldCost = null!;
        public static ConfigEntry<string> ChargeItem = null!;
        public static ConfigEntry<int> ChargeItemAmount = null!;
        public static ConfigEntry<KeyboardShortcut> ModifierKey = null!;

        private ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description, bool synchronizedSetting = true)
        {
            ConfigEntry<T> configEntry = Config.Bind(group, name, value, description);

            SyncedConfigEntry<T> syncedConfigEntry = configSync.AddConfigEntry(configEntry);
            syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

            return configEntry;
        }

        private ConfigEntry<T> config<T>(string group, string name, T value, string description, bool synchronizedSetting = true)
        {
            return config(group, name, value, new ConfigDescription(description), synchronizedSetting);
        }

        private class ConfigurationManagerAttributes
        {
            [UsedImplicitly] public int? Order;
            [UsedImplicitly] public bool? Browsable;
            [UsedImplicitly] public string? Category;
            [UsedImplicitly] public Action<ConfigEntryBase>? CustomDrawer;
        }

        class AcceptableShortcuts : AcceptableValueBase
        {
            public AcceptableShortcuts() : base(typeof(KeyboardShortcut))
            {
            }

            public override object Clamp(object value) => value;
            public override bool IsValid(object value) => true;

            public override string ToDescriptionString() => "# Acceptable values: " + string.Join(", ", UnityInput.Current.SupportedKeyCodes);
        }

        #endregion
    }

    public static class KeyboardExtensions
    {
        public static bool IsKeyDown(this KeyboardShortcut shortcut)
        {
            return shortcut.MainKey != KeyCode.None && Input.GetKeyDown(shortcut.MainKey) && shortcut.Modifiers.All(Input.GetKey);
        }

        public static bool IsKeyHeld(this KeyboardShortcut shortcut)
        {
            return shortcut.MainKey != KeyCode.None && Input.GetKey(shortcut.MainKey) && shortcut.Modifiers.All(Input.GetKey);
        }
    }
}