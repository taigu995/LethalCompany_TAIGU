using System;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using BepInEx.Logging;
using CustomCompany.Behaviour;
using UnityEngine;

namespace CustomCompany
{
    /// <summary>
    /// Singleton manager for CleanShip mod.
    /// </summary>
    public class CustomCompanyManager : MonoBehaviour
    {
        private static CustomCompanyManager instance;

        public static CustomCompanyManager Instance
        {
            get
            {
                if (instance == null)
                {
                    Init();
                }
                return instance;
            }
        }

        public static void Init()
        {
            if (instance != null) return;

            GameObject val = new GameObject("CustomCompanyManager");
            DontDestroyOnLoad(val);
            val.hideFlags = (HideFlags)61;
            instance = val.AddComponent<CustomCompanyManager>();
        }

        private void Start()
        {
            CustomCompanyConfig.LoadConfig();
            CustomCompanyConfig.LoadBehaviourConfig();
            CleanShipBehaviour.LoadConfig();
            CollectScrapBehaviour.LoadConfig();
        }

        private void Update()
        {
        }
    }

    /// <summary>
    /// Global settings for CleanShip mod.
    /// </summary>
    internal static class Setting
    {
        public static bool bMenu;
        public static bool bCleaning;
        public static bool bCollecting;
        public static bool bRandomTidy;
        public static bool bCleanCustom;
        public static float cleanOffset = 0.2f;
        public static bool bOnlyCustom;
        public static bool bNoclip;
        public static float noclipSpeed;
        public static GameNetcodeStuff.PlayerControllerB curTarget;
        public static bool bNightVision;
        public static float playerVolumesRate = 1f;
        public static bool bDmgCheck;
        public static bool hasReviveCompany;
        public static bool bKillEnemy;
        public static bool bDestroyEnemy;
        public static float killEnemyDis = 10f;
    }

    /// <summary>
    /// Utility methods for CleanShip mod.
    /// </summary>
    public static class Utility
    {
        internal static CustomLog Log = Plugin.Log;

        public static BindingFlags protectedFlags = BindingFlags.Instance | BindingFlags.NonPublic;

        public static Vector3 GetLocalPlayerPosition()
        {
            if (GameNetcodeStuff.StartOfRound.Instance.localPlayerController == null ||
                GameNetcodeStuff.StartOfRound.Instance.localPlayerController.isPlayerDead)
            {
                return Vector3.zero;
            }
            return GameNetcodeStuff.StartOfRound.Instance.localPlayerController.transform.position;
        }

        public static object CallMethod(object instance, string methodName, BindingFlags bindingFlags, params object[] parameters)
        {
            Type type = instance.GetType();
            MethodInfo method = type.GetMethod(methodName, bindingFlags);
            if (method != null)
            {
                return method.Invoke(instance, parameters);
            }
            else
            {
                Log.LogError($"执行方法失败！{instance}.{methodName}");
                return null;
            }
        }

        public static Vector3 SetPositionY(this Vector3 position, float y)
        {
            return new Vector3(position.x, y, position.z);
        }

        public static bool IsModLoaded(string guid)
        {
            return BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(guid);
        }

        public static T DeepCopy<T>(T obj)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                BinaryFormatter binaryFormatter = new BinaryFormatter();
                binaryFormatter.Serialize(memoryStream, obj);
                memoryStream.Seek(0L, SeekOrigin.Begin);
                return (T)binaryFormatter.Deserialize(memoryStream);
            }
        }
    }

    /// <summary>
    /// Custom logging wrapper.
    /// </summary>
    public class CustomLog
    {
        public bool enable = true;

        internal static ManualLogSource Log = Logger.CreateLogSource("TAIGU.CustomCompany.CleanShip");

        public void LogInfo(object message)
        {
            if (enable) Log.LogInfo(message);
        }

        public void LogWarning(object message)
        {
            if (enable) Log.LogWarning(message);
        }

        public void LogError(object message)
        {
            if (enable) Log.LogError(message);
        }

        public void LogDebug(object message)
        {
            if (enable) Log.LogDebug(message);
        }

        public void LogMessage(object message)
        {
            if (enable) Log.LogMessage(message);
        }

        public void LogFatal(object message)
        {
            if (enable) Log.LogFatal(message);
        }
    }
}
