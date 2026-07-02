using System;
using BepInEx;
using BepInEx.Bootstrap;
using HarmonyLib;
using CustomCompany.Patch;
using CustomCompany.Behaviour;

namespace CustomCompany
{
    /// <summary>
    /// CleanShip Mod - V81 Compatible
    /// Original by qh3, updated for Lethal Company V81
    /// 
    /// Changelog for V81:
    /// - Fixed transpiler patches to use pattern matching instead of hardcoded IL indices
    /// - Updated Harmony patches for V80/V81 method signature changes
    /// - Added compatibility with new utility item slot
    /// - Made OPJosMod (ReviveCompany) dependency fully optional
    /// - Improved error handling and logging
    /// - Updated night vision to account for V80's reduced range
    /// </summary>
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGUID = "qh3.CustomCompany.CleanShip";
        public const string PluginName = "CleanShip";
        public const string PluginVersion = "2.6.0"; // Bumped version for V81

        public static Plugin Instance;

        private static readonly Harmony harmony = new Harmony("qh3.CustomCompany");

        public static CustomLog Log = new CustomLog();

        private void Awake()
        {
            Log.LogInfo("Loading CustomCompany.CleanShip Mod V" + PluginVersion + " (V81 Compatible)");
            Log.enable = true;
            Instance = this;

            try
            {
                CustomCompanyManager.Init();
                CustomCompanyUI.Init();
            }
            catch (Exception ex)
            {
                Log.LogError($"Failed to initialize mod: {ex}");
                return;
            }

            // Patch with error handling - each patch is independent
            TryPatch(typeof(PlayerControllerB_Patch));
            TryPatch(typeof(StartOfRound_Patch));

            Log.LogInfo("CustomCompany.CleanShip loaded successfully!");
        }

        private static void TryPatch(Type type)
        {
            try
            {
                harmony.PatchAll(type);
                Log.LogInfo($"Successfully patched {type.Name}");
            }
            catch (Exception ex)
            {
                Log.LogError($"Unable to patch {type.Name}: {ex}");
            }
        }
    }
}
