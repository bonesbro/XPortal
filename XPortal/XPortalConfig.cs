﻿using BepInEx.Configuration;
using System;
using XPortal.RPC;

namespace XPortal
{
    internal sealed class XPortalConfig
    {
        ////////////////////////////
        //// Singleton instance ////
        private static readonly Lazy<XPortalConfig> lazy = new Lazy<XPortalConfig>(() => new XPortalConfig());
        public static XPortalConfig Instance { get { return lazy.Value; } }
        ////////////////////////////

        public event Action OnLocalConfigChanged;
        public event Action OnServerConfigChanged;

        private ConfigFile configFile;

        /// <summary>
        /// Container class for all of XPortal's config settings
        /// </summary>
        public class ConfigSettings
        {
            public bool PingMapDisabled;
            public bool DisplayPortalColour;
            public bool DoublePortalCosts;
        }

        /// <summary>
        /// Track local config settings
        /// </summary>
        public ConfigSettings Local { get; set; }

        /// <summary>
        /// Track Server config settings
        /// </summary>
        public ConfigSettings Server { get; set; }

        private XPortalConfig()
        {
            Local = new ConfigSettings();
            Server = new ConfigSettings();
        }

        /// <summary>
        /// Load the config file, and track the settings inside it
        /// </summary>
        /// <param name="configFile">The config file being loaded</param>
        public void LoadLocalConfig(ConfigFile configFile)
        {
            this.configFile = configFile;
            ReloadLocalConfig();

            this.configFile.ConfigReloaded += LocalConfigChanged;
            this.configFile.SettingChanged += LocalConfigChanged;

            if (Environment.IsServer)
            {
                Server = Local;
            }
        }

        /// <summary>
        /// Reload the settings inside the config file
        /// </summary>
        private void ReloadLocalConfig()
        {
            // Add Nexus ID to config for Nexus Update Check (https://www.nexusmods.com/valheim/mods/102)
            configFile.Bind("General", "NexusID", Mod.Info.NexusId, "Nexus mod ID for updates (do not change)");

            // Add PingMapDisabled option which disables the Ping Map button
            var cfgPingMapDisabled = configFile.Bind("General", "PingMapDisabled", false, "Disable the Ping Map button completely. For players who wish to play without a map. This setting is enforced (but not overwritten) by the server.");
            Local.PingMapDisabled = cfgPingMapDisabled.Value;

            var cfgDisplayPortalColour = configFile.Bind("General", "DisplayPortalColour", false, "Show a \">>\" tag in the list of portals that has the same colour as the light that the portal emits (integration with \"Advanced Portals\" by RandyKnapp).");
            Local.DisplayPortalColour = cfgDisplayPortalColour.Value;

            var cfgDoublePortalCosts = configFile.Bind("General", "DoublePortalCosts", false, "By using XPortal, you effectively only need half the amount of portals. To compensate for that, we can double the costs of portals. This setting is enforced (but not overwritten) by the server.");
            Local.DoublePortalCosts = cfgDoublePortalCosts.Value;
        }

        /// <summary>
        /// The config file was reloaded or a setting was changed. 
        /// If we are the server, sync the config to clients.
        /// </summary>
        private void LocalConfigChanged(object sender, EventArgs e)
        {
            ReloadLocalConfig();

            if (Environment.IsServer)
            {
                Log.Debug("The config was changed, propagating to clients..");
                SendToClient.Config(PackLocalConfig());
            }

            OnLocalConfigChanged?.Invoke();
        }

        /// <summary>
        /// Wrap the config settings into a package
        /// </summary>
        /// <returns>A ZPackage containing all config settings</returns>
        public ZPackage PackLocalConfig()
        {
            var pkg = new ZPackage();
            pkg.Write(Local.PingMapDisabled);
            pkg.Write(Local.DoublePortalCosts);
            return pkg;
        }

        /// <summary>
        /// Set our config settings based on the package we received from the server
        /// </summary>
        /// <param name="pkg">A ZPackage containing all config settings</param>
        public void ReceiveServerConfig(ZPackage pkg)
        {
            Server.PingMapDisabled = pkg.ReadBool();
            Server.DoublePortalCosts = pkg.ReadBool();
            Log.Debug($"PingMapDisabled {{ Local: {Local.PingMapDisabled}, Server: {Server.PingMapDisabled} }}");
            Log.Debug($"DoublePortalCosts {{ Local: {Local.DoublePortalCosts}, Server: {Server.DoublePortalCosts} }}");

            OnServerConfigChanged?.Invoke();
        }
    }
}
