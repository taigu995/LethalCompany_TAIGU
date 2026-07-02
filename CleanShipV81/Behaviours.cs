using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CustomCompany.Behaviour;
using GameNetcodeStuff;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;

namespace CustomCompany.Behaviour
{
    /// <summary>
    /// Configuration classes for behaviours.
    /// </summary>
    [Serializable]
    public class BehaviourConfig
    {
        public CleanShipConfig CleanShipConfig = new CleanShipConfig();
        public CollectScrapConfig CollectScrapConfig = new CollectScrapConfig();
    }

    [Serializable]
    public class CollectScrapConfig
    {
        public List<string> targetList = new List<string>();
        public bool bTarget;
        public List<string> ignoreList = new List<string>();
        public bool bIgnore;
    }

    [Serializable]
    public class CleanShipConfig : ICloneable
    {
        public StrVector3 bigItemPos = new StrVector3(-3.3f, 0.5f, -15.8f);
        public StrVector3 smallItemPos = new StrVector3(-3.3f, 0.5f, -12.7f);
        public StrVector3 shovelPos = new StrVector3(5f, 0.5f, -16f);
        public StrVector3 lightPos = new StrVector3(2.9f, 0.5f, -15.6f);
        public StrVector3 stunPos = new StrVector3(-4.3f, 0.5f, -14f);
        public StrVector3 sprPos = new StrVector3(2.9f, 0.5f, -12.7f);
        public StrVector3 keyPos = new StrVector3(9.5f, 0.5f, -16f);
        public StrVector3 bagPos = new StrVector3(5f, 0.5f, -11.7f);
        public StrVector3 radarPos = new StrVector3(-0.8f, 0.5f, -12.7f);
        public StrVector3 otherPos = new StrVector3(6.8f, 0.5f, -11.9f);
        public StrVector3 gunPos = new StrVector3(10f, 0.5f, -11.9f);
        public StrVector3 ammoPos = new StrVector3(9f, 0.5f, -11.9f);
        public List<CleanShipCustomItem> customItems = new List<CleanShipCustomItem>();

        public object Clone()
        {
            throw new NotImplementedException();
        }

        public bool TryGet(string name, out StrVector3 v3)
        {
            CleanShipCustomItem cleanShipCustomItem = customItems.FirstOrDefault(
                (CleanShipCustomItem itemInfo) => !string.IsNullOrEmpty(itemInfo.name) && itemInfo.name.Equals(name));
            v3 = cleanShipCustomItem?.pos ?? null;
            return cleanShipCustomItem != null;
        }
    }

    [Serializable]
    public class CleanShipCustomItem
    {
        public StrVector3 pos;
        public string name = string.Empty;

        public CleanShipCustomItem()
        {
            pos = new StrVector3();
        }
    }

    [Serializable]
    public class StrVector3
    {
        [SerializeField]
        private float x;
        [SerializeField]
        private float y;
        [SerializeField]
        private float z;

        public string X
        {
            get { return x.ToString("F2"); }
            set { x = (float.TryParse(value, out var result) ? result : 0f); }
        }

        public string Y
        {
            get { return y.ToString("F2"); }
            set { y = (float.TryParse(value, out var result) ? result : 0f); }
        }

        public string Z
        {
            get { return z.ToString("F2"); }
            set { z = (float.TryParse(value, out var result) ? result : 0f); }
        }

        public StrVector3(string _x, string _y, string _z)
        {
            x = (float.TryParse(_x, out var result) ? result : 0f);
            y = (float.TryParse(_y, out var result2) ? result2 : 0f);
            z = (float.TryParse(_z, out var result3) ? result3 : 0f);
        }

        public StrVector3(float _x, float _y, float _z)
        {
            x = _x;
            y = _y;
            z = _z;
        }

        public StrVector3()
        {
        }

        public Vector3 ToVector3()
        {
            return new Vector3(x, y, z);
        }
    }

    /// <summary>
    /// CleanShip behaviour - sorts/organizes items in the ship.
    /// V81: No major changes needed, but added null checks for safety.
    /// </summary>
    public static class CleanShipBehaviour
    {
        public static CleanShipConfig CleanShipConfig;
        private static Coroutine tidyItem_crt;

        internal static CustomLog Log => Plugin.Log;

        private static IEnumerator SortCoroutine(Action callBack)
        {
            Setting.bCleaning = true;
            GameObject ship = GameObject.Find("/Environment/HangarShip");
            PlayerControllerB player = RoundManager.Instance.playersManager.localPlayerController;
            GrabbableObject[] componentsInChildren = ship.transform.GetComponentsInChildren<GrabbableObject>();

            foreach (GrabbableObject item in componentsInChildren)
            {
                if (!player.isInHangarShipRoom)
                {
                    break;
                }
                if (item.isHeld || item.heldByPlayerOnServer || (Setting.bOnlyCustom && !IsCustom(item)))
                {
                    continue;
                }

                Vector3 cfgPos = GetTidyTargetPos(item);
                Vector3 targetPos = item.GetItemFloorPosition(cfgPos);
                Vector3 offsetTargetPos = GetVector3_OffsetXZ(targetPos, Setting.cleanOffset);

                if (Vector3.Distance(item.transform.position, targetPos) <= Setting.cleanOffset)
                {
                    continue;
                }

                Log.LogInfo(string.Format("等待丢弃操作结束...{0} 当前准备抓取的物品 {1}",
                    Traverse.Create((object)player).Field<bool>("throwingObject").Value,
                    item.itemProperties.itemName));

                yield return new WaitUntil(() => !Traverse.Create((object)player).Field<bool>("throwingObject").Value);

                Log.LogInfo("开始抓取飞船物品:" + item.itemProperties.itemName);
                NetworkObjectReference obj = new NetworkObjectReference(((NetworkBehaviour)item).NetworkObject);
                Traverse.Create((object)player).Field<GrabbableObject>("currentlyGrabbingObject").Value = item;
                item.InteractItem();
                player.twoHanded = item.itemProperties.twoHanded;
                PlayerControllerB obj2 = player;
                obj2.carryWeight += Mathf.Clamp(item.itemProperties.weight - 1f, 0f, 10f);
                item.parentObject = player.localItemHolder;
                Utility.CallMethod(player, "GrabObjectServerRpc", BindingFlags.Instance | BindingFlags.NonPublic, obj);

                Log.LogInfo($"等待抓取操作结束... 当前准备丢弃的物品 {player.currentlyHeldObjectServer}");
                yield return new WaitUntil(() => player.currentlyHeldObjectServer == item);

                Log.LogInfo("开始丢弃飞船物品:" + item.itemProperties.itemName);
                player.isHoldingObject = true;
                player.DiscardHeldObject(true, null, offsetTargetPos, false);
            }

            Setting.bCleaning = false;
            callBack?.Invoke();
            tidyItem_crt = null;
        }

        private static Vector3 GetTidyTargetPos(GrabbableObject obj)
        {
            if (Setting.bRandomTidy)
            {
                return new Vector3(UnityEngine.Random.Range(-3f, 9f), 0.5f, UnityEngine.Random.Range(-15.8f, -12.7f));
            }
            if (CleanShipConfig.TryGet(obj.itemProperties.itemName, out var v))
            {
                return v.ToVector3();
            }
            if (obj is Shovel)
            {
                return CleanShipConfig.shovelPos.ToVector3();
            }
            if (obj is FlashlightItem)
            {
                return CleanShipConfig.lightPos.ToVector3();
            }
            if (obj is StunGrenadeItem)
            {
                return CleanShipConfig.stunPos.ToVector3();
            }
            if (obj is SprayPaintItem)
            {
                return CleanShipConfig.sprPos.ToVector3();
            }
            if (obj is KeyItem)
            {
                return CleanShipConfig.keyPos.ToVector3();
            }
            if (obj is JetpackItem)
            {
                return CleanShipConfig.bagPos.ToVector3();
            }
            if (obj is RadarBoosterItem)
            {
                return CleanShipConfig.radarPos.ToVector3();
            }
            if (obj is ShotgunItem)
            {
                return CleanShipConfig.gunPos.ToVector3();
            }
            if (obj is GunAmmo)
            {
                return CleanShipConfig.ammoPos.ToVector3();
            }
            if (obj.itemProperties.twoHanded && obj.itemProperties.isScrap)
            {
                return CleanShipConfig.bigItemPos.ToVector3();
            }
            if (!obj.itemProperties.twoHanded && obj.itemProperties.isScrap)
            {
                return CleanShipConfig.smallItemPos.ToVector3();
            }
            return CleanShipConfig.otherPos.ToVector3();
        }

        private static bool IsCustom(GrabbableObject obj)
        {
            return CleanShipConfig.TryGet(obj.itemProperties.itemName, out var _);
        }

        private static Vector3 GetVector3_OffsetXZ(Vector3 v3, float offset)
        {
            Vector2 val = UnityEngine.Random.insideUnitCircle * offset;
            return new Vector3(v3.x + val.x, v3.y, v3.z + val.y);
        }

        public static void StartSort(Action callBack)
        {
            if (StartOfRound.Instance.localPlayerController != null &&
                !StartOfRound.Instance.localPlayerController.isPlayerDead)
            {
                StopSort();
                tidyItem_crt = ((MonoBehaviour)CustomCompanyManager.Instance).StartCoroutine(SortCoroutine(callBack));
            }
        }

        public static void StopSort()
        {
            if (tidyItem_crt != null)
            {
                ((MonoBehaviour)CustomCompanyManager.Instance).StopCoroutine(tidyItem_crt);
                tidyItem_crt = null;
            }
            if (Setting.bCleaning)
            {
                Setting.bCleaning = false;
            }
        }

        public static bool TryGetShipItemNames(out List<string> names)
        {
            names = new List<string>();
            if (StartOfRound.Instance == null || StartOfRound.Instance.localPlayerController == null)
            {
                return false;
            }
            GameObject val = GameObject.Find("/Environment/HangarShip");
            PlayerControllerB localPlayerController = RoundManager.Instance.playersManager.localPlayerController;
            GrabbableObject[] componentsInChildren = val.transform.GetComponentsInChildren<GrabbableObject>();
            foreach (GrabbableObject val2 in componentsInChildren)
            {
                string itemName = val2.itemProperties.itemName;
                names.Add(itemName);
            }
            names = names.Distinct().ToList();
            return true;
        }

        public static void LoadConfig()
        {
            CleanShipConfig = Utility.DeepCopy(CustomCompanyConfig.BehaviourConfig.CleanShipConfig);
        }

        public static void SaveConfig()
        {
            BehaviourConfig behaviourConfig = CustomCompanyConfig.BehaviourConfig;
            behaviourConfig.CleanShipConfig = Utility.DeepCopy(CleanShipConfig);
            CustomCompanyConfig.SaveBehaviourConfig();
        }
    }

    /// <summary>
    /// CollectScrap behaviour - automatically collects scrap from dungeon to ship.
    /// </summary>
    internal static class CollectScrapBehaviour
    {
        public static CollectScrapConfig Config = new CollectScrapConfig();
        private static Coroutine collectCoroutine;

        internal static CustomLog Log => Plugin.Log;

        public static void StartCollect(Action callBack)
        {
            if (StartOfRound.Instance.localPlayerController != null &&
                !StartOfRound.Instance.localPlayerController.isPlayerDead)
            {
                StopCollect();
                collectCoroutine = ((MonoBehaviour)CustomCompanyManager.Instance).StartCoroutine(CollectCoroutine(callBack));
            }
        }

        public static void StopCollect()
        {
            if (collectCoroutine != null)
            {
                ((MonoBehaviour)CustomCompanyManager.Instance).StopCoroutine(collectCoroutine);
                collectCoroutine = null;
            }
            Setting.bCollecting = false;
        }

        public static bool TryGetAllScrapNames(out List<string> names)
        {
            names = new List<string>();
            if (StartOfRound.Instance == null || StartOfRound.Instance.localPlayerController == null ||
                !GameNetworkManager.Instance.gameHasStarted)
            {
                return false;
            }
            GrabbableObject[] array = UnityEngine.Object.FindObjectsOfType<GrabbableObject>();
            foreach (GrabbableObject val in array)
            {
                if (val != null && val.itemProperties.isScrap && !val.isInShipRoom && val.grabbable)
                {
                    string itemName = val.itemProperties.itemName;
                    names.Add(itemName);
                }
            }
            names = names.Distinct().ToList();
            return true;
        }

        public static void LoadConfig()
        {
            Config = Utility.DeepCopy(CustomCompanyConfig.BehaviourConfig.CollectScrapConfig);
        }

        public static void SaveConfig()
        {
            CustomCompanyConfig.BehaviourConfig.CollectScrapConfig = Utility.DeepCopy(Config);
            CustomCompanyConfig.SaveBehaviourConfig();
        }

        private static IEnumerator CollectCoroutine(Action callBack)
        {
            Setting.bCollecting = true;
            PlayerControllerB player = StartOfRound.Instance.localPlayerController;
            GrabbableObject[] allObj = UnityEngine.Object.FindObjectsOfType<GrabbableObject>();

            foreach (GrabbableObject item in allObj)
            {
                if (CkeckTarget(item))
                {
                    Log.LogInfo(string.Format("等待丢弃操作结束...{0} 当前准备抓取的物品 {1}",
                        Traverse.Create((object)player).Field<bool>("throwingObject").Value,
                        item.itemProperties.itemName));

                    yield return new WaitUntil(() => !Traverse.Create((object)player).Field<bool>("throwingObject").Value);

                    Log.LogInfo("开始抓取飞船物品:" + item.itemProperties.itemName);
                    NetworkObjectReference obj = new NetworkObjectReference(((NetworkBehaviour)item).NetworkObject);
                    Traverse.Create((object)player).Field<GrabbableObject>("currentlyGrabbingObject").Value = item;
                    item.InteractItem();
                    player.twoHanded = item.itemProperties.twoHanded;
                    PlayerControllerB obj2 = player;
                    obj2.carryWeight += Mathf.Clamp(item.itemProperties.weight - 1f, 0f, 10f);
                    item.parentObject = player.localItemHolder;
                    Utility.CallMethod(player, "GrabObjectServerRpc", BindingFlags.Instance | BindingFlags.NonPublic, obj);

                    Log.LogInfo($"等待抓取操作结束... 当前准备丢弃的物品 {player.currentlyHeldObjectServer}");
                    yield return new WaitUntil(() => player.currentlyHeldObjectServer == item);

                    Log.LogInfo("开始丢弃手持物品:" + item.itemProperties.itemName);
                    player.isHoldingObject = true;
                    player.DiscardHeldObject(false, null, default(Vector3), true);
                }
            }

            collectCoroutine = null;
            Setting.bCollecting = false;
            callBack?.Invoke();
            yield return null;
        }

        private static bool CkeckTarget(GrabbableObject item)
        {
            if (item == null) return false;
            if (!item.itemProperties.isScrap) return false;
            if (item.isInShipRoom) return false;
            if (item.isHeld) return false;
            if (!item.grabbable) return false;
            if (item.heldByPlayerOnServer) return false;
            if (Config.bIgnore && Config.ignoreList.Contains(item.itemProperties.itemName)) return false;
            if (Config.bTarget && !Config.targetList.Contains(item.itemProperties.itemName)) return false;
            return true;
        }
    }

    /// <summary>
    /// Damage check behaviour - shows who is hitting whom.
    /// </summary>
    internal static class DamageCkeckBehaviour
    {
        public static void Check(PlayerControllerB __instance, int playerWhoHit)
        {
            if (Setting.bDmgCheck)
            {
                string playerUsername = __instance.playerUsername;
                string text = "神秘人";
                if (StartOfRound.Instance.allPlayerScripts[playerWhoHit] != null)
                {
                    text = StartOfRound.Instance.allPlayerScripts[playerWhoHit].playerUsername;
                }
                string text2 = text + "  正在殴打 " + playerUsername + "!";
                Plugin.Log.LogInfo("发送消息:" + text2);
                SendMessageBehaviour.Send(text2);
            }
        }
    }

    /// <summary>
    /// Force disconnect behaviour.
    /// </summary>
    public static class ForceDisconnectBehaviour
    {
        public static void ForceDisconnec()
        {
            GameNetworkManager instance = GameNetworkManager.Instance;
            if (instance)
            {
                instance.isDisconnecting = true;
                if (instance.isHostingGame)
                {
                    instance.disallowConnection = true;
                }
                Utility.CallMethod(instance, "StartDisconnect", BindingFlags.Instance | BindingFlags.NonPublic);
                instance.SaveGame();
                if (Unity.Netcode.NetworkManager.Singleton == null)
                {
                    Debug.Log("Server is not active; quitting to main menu");
                    Utility.CallMethod(instance, "ResetGameValuesToDefault", BindingFlags.Instance | BindingFlags.NonPublic);
                    UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
                }
                else
                {
                    ((MonoBehaviour)instance).StartCoroutine((IEnumerator)Utility.CallMethod(instance, "DisconnectProcess", BindingFlags.Instance | BindingFlags.NonPublic));
                }
            }
        }
    }

    /// <summary>
    /// Kill enemy behaviour - kill enemies by aiming and pressing Z.
    /// </summary>
    internal static class KillEnemyBehaviour
    {
        private static Ray interactRay;
        private static RaycastHit hit;

        public static void Update(PlayerControllerB player)
        {
            if (!Setting.bKillEnemy) return;

            interactRay = new Ray(player.gameplayCamera.transform.position, player.gameplayCamera.transform.forward);
            if (Physics.Raycast(interactRay, ref hit, Setting.killEnemyDis, LayerMask.GetMask(new string[1] { "Enemies" })))
            {
                EnemyAI componentInParent = hit.transform.GetComponentInParent<EnemyAI>();
                if (componentInParent != null && ((ButtonControl)Keyboard.current.zKey).wasPressedThisFrame)
                {
                    Plugin.Log.LogInfo("击杀敌人：" + componentInParent.name);
                    componentInParent.KillEnemyServerRpc(Setting.bDestroyEnemy);
                }
            }
        }
    }

    /// <summary>
    /// Night vision behaviour.
    /// V81 Fix: V80 reduced night vision range. We compensate by setting a higher value.
    /// </summary>
    internal static class NightVisionBehaviour
    {
        private static bool bOn;
        private static Color tempColor;
        private static float tempRange;
        private static float tempIntensity;

        public static void Update(PlayerControllerB player)
        {
            if (Setting.bNightVision)
            {
                On(player);
            }
            else
            {
                Off(player);
            }
        }

        public static void On(PlayerControllerB player)
        {
            if (!bOn)
            {
                bOn = true;
                player.nightVision.color = new Color(1f, 1f, 1f, 1f);
                // V81 Fix: V80 reduced night vision range, so we set a higher value to compensate
                player.nightVision.range = 5000f;
                player.nightVision.intensity = 1000f;
            }
        }

        public static void Off(PlayerControllerB player)
        {
            if (bOn)
            {
                bOn = false;
                player.nightVision.color = tempColor;
                player.nightVision.range = tempRange;
                player.nightVision.intensity = tempIntensity;
            }
        }
    }

    /// <summary>
    /// Send message behaviour.
    /// </summary>
    internal static class SendMessageBehaviour
    {
        public static void Send(string message, int playerId = -1, string color = "red")
        {
            string text = "<color=" + color + ">" + message + "</color>";
            HUDManager.Instance.AddTextToChatOnServer(text, playerId);
        }
    }

    /// <summary>
    /// Teleport player behaviour.
    /// </summary>
    public static class TeleportPlayerBehaviour
    {
        private static ShipTeleporter shipTeleporter;
        private static Camera cmr;
        private static Coroutine coroutine;

        internal static CustomLog Log => Plugin.Log;

        public static void TeleportPlayer()
        {
            if (coroutine != null)
            {
                ((MonoBehaviour)CustomCompanyManager.Instance).StopCoroutine(coroutine);
                coroutine = null;
            }
            coroutine = ((MonoBehaviour)CustomCompanyManager.Instance).StartCoroutine(TeleportPlayerSync());
        }

        private static IEnumerator TeleportPlayerSync()
        {
            ShipTeleporter teleporter = GetShipTeleporter();
            if (teleporter && Setting.curTarget)
            {
                int num = SearchForPlayerInRadar();
                StartOfRound.Instance.mapScreen.SwitchRadarTargetAndSync(num);
                yield return new WaitForSeconds(0.15f);
                yield return new WaitUntil(() => StartOfRound.Instance.mapScreen.targetTransformIndex == num);
                teleporter.PressTeleportButtonOnLocalClient();
            }
            else
            {
                Log.LogInfo("传送器为空！");
                yield return null;
            }
        }

        private static ShipTeleporter GetShipTeleporter()
        {
            if (shipTeleporter != null)
            {
                return shipTeleporter;
            }
            ShipTeleporter[] array = UnityEngine.Object.FindObjectsOfType<ShipTeleporter>();
            ShipTeleporter val = null;
            foreach (ShipTeleporter val2 in array)
            {
                if (!val2.isInverseTeleporter)
                {
                    val = val2;
                    break;
                }
            }
            shipTeleporter = val;
            return shipTeleporter;
        }

        private static int SearchForPlayerInRadar()
        {
            int result = -1;
            for (int i = 0; i < StartOfRound.Instance.mapScreen.radarTargets.Count(); i++)
            {
                if (!(StartOfRound.Instance.mapScreen.radarTargets[i].transform.gameObject.GetComponent<PlayerControllerB>() != Setting.curTarget))
                {
                    result = i;
                    break;
                }
            }
            return result;
        }
    }

    /// <summary>
    /// Noclip behaviour.
    /// </summary>
    public static class NoclipBehaviour
    {
        public static void Update()
        {
            if (!Setting.bNoclip) return;

            PlayerControllerB localPlayerController = GameNetworkManager.Instance.localPlayerController;
            if (!localPlayerController) return;

            Collider component = localPlayerController.GetComponent<CharacterController>();
            if (!component) return;

            Transform transform = localPlayerController.transform;
            component.enabled = !transform;

            if (!component.enabled)
            {
                bool isPressed = ((ButtonControl)Keyboard.current.wKey).isPressed;
                bool isPressed2 = ((ButtonControl)Keyboard.current.aKey).isPressed;
                bool isPressed3 = ((ButtonControl)Keyboard.current.sKey).isPressed;
                bool isPressed4 = ((ButtonControl)Keyboard.current.dKey).isPressed;
                bool isPressed5 = ((ButtonControl)Keyboard.current.spaceKey).isPressed;
                bool isPressed6 = ((ButtonControl)Keyboard.current.leftCtrlKey).isPressed;

                Vector3 val = Vector3.zero;
                if (isPressed) val += transform.forward;
                if (isPressed3) val -= transform.forward;
                if (isPressed2) val -= transform.right;
                if (isPressed4) val += transform.right;
                if (isPressed5) val.y += transform.up.y;
                if (isPressed6) val.y -= transform.up.y;

                Transform transform2 = localPlayerController.transform;
                transform2.position += val * (Setting.noclipSpeed * Time.deltaTime);
            }
        }
    }
}
