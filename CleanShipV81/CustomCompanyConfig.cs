using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using CustomCompany.Behaviour;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace CustomCompany
{
    /// <summary>
    /// Configuration management for CleanShip mod.
    /// V81: Updated key binding to use Equals key as default.
    /// </summary>
    internal static class CustomCompanyConfig
    {
        public static KeyControl menuKey = Keyboard.current.equalsKey;

        public static BehaviourConfig BehaviourConfig = new BehaviourConfig();

        private static string keyDisplayNames;

        public static void LoadConfig()
        {
            try
            {
                keyDisplayNames = string.Join(", ",
                    ((IEnumerable<KeyControl>)(object)Keyboard.current.allKeys)
                        .Select((KeyControl x) => ((InputControl)x).displayName));
                menuKey = BindKey("按键配置", "菜单键", Keyboard.current.equalsKey,
                    "参考按键对应字符串: " + keyDisplayNames);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"Failed to load key config: {ex.Message}");
                menuKey = Keyboard.current.equalsKey;
            }
        }

        public static void LoadBehaviourConfig()
        {
            string path = Paths.ConfigPath + "/CleanShipConfig.json";
            if (System.IO.File.Exists(path))
            {
                try
                {
                    string text = System.IO.File.ReadAllText(path);
                    BehaviourConfig = LitJson.JsonMapper.ToObject<BehaviourConfig>(text);
                    Plugin.Log.LogInfo("Behaviour config loaded successfully");
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogError($"Failed to load behaviour config: {ex.Message}");
                    BehaviourConfig = new BehaviourConfig();
                }
            }
        }

        public static void SaveBehaviourConfig()
        {
            try
            {
                string text = LitJson.JsonMapper.ToJson((object)BehaviourConfig);
                Plugin.Log.LogInfo(text);
                string path = Paths.ConfigPath + "/CleanShipConfig.json";
                System.IO.File.WriteAllText(path, text);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"Failed to save behaviour config: {ex.Message}");
            }
        }

        private static KeyControl BindKey(string section, string key, KeyControl defaultValue, string configDescription = null)
        {
            try
            {
                ConfigEntry<string> val = ((BaseUnityPlugin)Plugin.Instance).Config.Bind<string>(
                    section, key, ((InputControl)defaultValue).displayName, configDescription);
                KeyControl val2 = Keyboard.current.FindKeyOnCurrentKeyboardLayout(val.Value);
                if (val2 != null)
                {
                    return val2;
                }
                Plugin.Log.LogWarning("按键设置失败！" + key + ":" + val.Value);
                return defaultValue;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning("按键设置失败!" + ex.Message);
                return defaultValue;
            }
        }
    }
}
