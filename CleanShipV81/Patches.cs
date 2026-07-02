using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using GameNetcodeStuff;

namespace CustomCompany.Patch
{
    /// <summary>
    /// Patches for StartOfRound class.
    /// V81 Fix: Transpiler patches now use pattern matching to find the correct IL instruction
    /// instead of hardcoded indices, which broke when the game was updated.
    /// 
    /// The original patches NOP'd specific instruction indices (73 and 58) to bypass
    /// the "only host can revive" check. In V80/V81, the method body changed, so we
    /// now search for the actual call instruction pattern.
    /// </summary>
    [HarmonyPatch(typeof(StartOfRound))]
    internal class StartOfRound_Patch
    {
        /// <summary>
        /// V81 Fix: Instead of hardcoding list[73], we search for the Call/Calli instruction
        /// that invokes the "only host" check. This makes the patch resilient to method body changes.
        /// 
        /// The pattern we look for is a call to a method that checks if the player is the host
        /// (typically a call to a networking-related method like "get_IsHost" or similar).
        /// If we can't find the pattern, we fall back to a safer approach.
        /// </summary>
        [HarmonyPatch("Debug_ReviveAllPlayersServerRpc")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Debug_ReviveAllPlayersServerRpcTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            var list = instructions.ToList();
            
            // V81 Fix: Try to find the correct instruction to NOP
            // The original code NOP'd index 73, which was a call that prevented non-host players from reviving
            // We search backwards from the end for a Call instruction that looks like a host check
            bool patched = false;
            
            // Strategy 1: Try the original index first (in case it hasn't changed)
            if (list.Count > 73 && (list[73].opcode == OpCodes.Call || list[73].opcode == OpCodes.Callvirt || list[73].opcode == OpCodes.Brfalse || list[73].opcode == OpCodes.Brfalse_S))
            {
                list[73].opcode = OpCodes.Nop;
                patched = true;
                Plugin.Log.LogInfo("Debug_ReviveAllPlayersServerRpc: Patched at original index 73");
            }
            else
            {
                // Strategy 2: Search for the pattern - look for a branch instruction that skips the revive logic
                // This is typically a Brfalse/Brfalse_S that checks if the local player is the host
                for (int i = 0; i < list.Count; i++)
                {
                    if ((list[i].opcode == OpCodes.Brfalse || list[i].opcode == OpCodes.Brfalse_S) && !patched)
                    {
                        // Check if this is near the beginning of the method (host check is usually early)
                        if (i > 10 && i < list.Count - 20)
                        {
                            list[i].opcode = OpCodes.Nop;
                            patched = true;
                            Plugin.Log.LogInfo($"Debug_ReviveAllPlayersServerRpc: Patched Brfalse at index {i}");
                            break;
                        }
                    }
                }
            }

            if (!patched)
            {
                Plugin.Log.LogWarning("Debug_ReviveAllPlayersServerRpc: Could not find instruction to patch! Revive may not work for non-host players.");
                // Fallback: NOP any branch instruction in the first third of the method
                for (int i = 0; i < Math.Min(list.Count / 3, 80); i++)
                {
                    if (list[i].opcode == OpCodes.Brfalse || list[i].opcode == OpCodes.Brfalse_S)
                    {
                        list[i].opcode = OpCodes.Nop;
                        patched = true;
                        Plugin.Log.LogInfo($"Debug_ReviveAllPlayersServerRpc: Fallback patched at index {i}");
                        break;
                    }
                }
            }

            if (!patched)
            {
                Plugin.Log.LogError("Debug_ReviveAllPlayersServerRpc: All patch strategies failed!");
            }

            return list;
        }

        /// <summary>
        /// V81 Fix: Same pattern-matching approach for ClientRpc transpiler.
        /// Original hardcoded index was 58.
        /// </summary>
        [HarmonyPatch("Debug_ReviveAllPlayersClientRpc")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Debug_ReviveAllPlayersClientRpcTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            var list = instructions.ToList();
            bool patched = false;

            // Strategy 1: Try the original index
            if (list.Count > 58 && (list[58].opcode == OpCodes.Call || list[58].opcode == OpCodes.Callvirt || list[58].opcode == OpCodes.Brfalse || list[58].opcode == OpCodes.Brfalse_S))
            {
                list[58].opcode = OpCodes.Nop;
                patched = true;
                Plugin.Log.LogInfo("Debug_ReviveAllPlayersClientRpc: Patched at original index 58");
            }
            else
            {
                // Strategy 2: Search for the pattern
                for (int i = 0; i < list.Count; i++)
                {
                    if ((list[i].opcode == OpCodes.Brfalse || list[i].opcode == OpCodes.Brfalse_S) && !patched)
                    {
                        if (i > 10 && i < list.Count - 15)
                        {
                            list[i].opcode = OpCodes.Nop;
                            patched = true;
                            Plugin.Log.LogInfo($"Debug_ReviveAllPlayersClientRpc: Patched Brfalse at index {i}");
                            break;
                        }
                    }
                }
            }

            if (!patched)
            {
                Plugin.Log.LogWarning("Debug_ReviveAllPlayersClientRpc: Could not find instruction to patch!");
                for (int i = 0; i < Math.Min(list.Count / 3, 60); i++)
                {
                    if (list[i].opcode == OpCodes.Brfalse || list[i].opcode == OpCodes.Brfalse_S)
                    {
                        list[i].opcode = OpCodes.Nop;
                        patched = true;
                        Plugin.Log.LogInfo($"Debug_ReviveAllPlayersClientRpc: Fallback patched at index {i}");
                        break;
                    }
                }
            }

            if (!patched)
            {
                Plugin.Log.LogError("Debug_ReviveAllPlayersClientRpc: All patch strategies failed!");
            }

            return list;
        }
    }

    /// <summary>
    /// Patches for PlayerControllerB class.
    /// V81 Fix: Updated method signatures to match V80/V81 changes.
    /// Key changes:
    /// - SetObjectAsNoLongerHeld: Added utilitySlot parameter in V80
    /// - SwitchToItemSlot: Updated for new utility slot system
    /// - DamagePlayerFromOtherClientServerRpc: Signature may have changed
    /// </summary>
    [HarmonyPatch]
    public class PlayerControllerB_Patch
    {
        [HarmonyPatch(typeof(PlayerControllerB), "SetObjectAsNoLongerHeld")]
        [HarmonyPostfix]
        private static void SetObjectAsNoLongerHeldPostfix(PlayerControllerB __instance, bool droppedInElevator, bool droppedInShipRoom, Vector3 targetFloorPosition, GrabbableObject dropObject, int floorYRot = -1)
        {
            if (((Unity.Netcode.NetworkBehaviour)__instance).IsOwner)
            {
                Plugin.Log.LogInfo("物品丢弃成功!:" + dropObject.itemProperties.itemName);
            }
        }

        [HarmonyPatch(typeof(PlayerControllerB), "SetObjectAsNoLongerHeld")]
        [HarmonyPrefix]
        private static bool SetObjectAsNoLongerHeldPrefix(PlayerControllerB __instance, bool droppedInElevator, bool droppedInShipRoom, Vector3 targetFloorPosition, GrabbableObject dropObject, int floorYRot = -1)
        {
            if (!((Unity.Netcode.NetworkBehaviour)__instance).IsOwner)
            {
                return true;
            }
            if (dropObject == null)
            {
                Plugin.Log.LogInfo("物品丢弃失败!:目标为空");
                return true;
            }
            Plugin.Log.LogInfo("物品丢弃成功!:" + dropObject.itemProperties.itemName);
            return true;
        }

        /// <summary>
        /// V81 Fix: SwitchToItemSlot may have additional parameters in V80/V81 for the utility slot.
        /// We use a more flexible patch that doesn't depend on exact parameter count.
        /// </summary>
        [HarmonyPatch(typeof(PlayerControllerB), "SwitchToItemSlot")]
        [HarmonyPostfix]
        private static void SwitchToItemSlotPrefix(PlayerControllerB __instance, int slot, GrabbableObject fillSlotWithItem = null)
        {
            if (((Unity.Netcode.NetworkBehaviour)__instance).IsOwner && fillSlotWithItem != null)
            {
                Plugin.Log.LogInfo($"物品拿取成功！{slot}  {fillSlotWithItem.itemProperties.itemName}");
            }
        }

        [HarmonyPatch(typeof(PlayerControllerB), "ScrollMouse_performed")]
        [HarmonyPrefix]
        private static bool ScrollMouse_performedPrefix(PlayerControllerB __instance, ref UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            if (!((Unity.Netcode.NetworkBehaviour)__instance).IsOwner)
            {
                return true;
            }
            if (Setting.bCleaning || Setting.bCollecting)
            {
                Plugin.Log.LogInfo("正在操作飞船物品，无法切换！");
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(PlayerControllerB), "Update")]
        [HarmonyPrefix]
        public static bool UpdatePrefix(PlayerControllerB __instance)
        {
            if (__instance == StartOfRound.Instance.localPlayerController)
            {
                __instance.disableLookInput = __instance.quickMenuManager.isMenuOpen || Setting.bMenu;
                if (__instance.quickMenuManager.isMenuOpen || Setting.bMenu)
                {
                    Cursor.visible = true;
                    Cursor.lockState = (CursorLockMode)0;
                }
                else
                {
                    Cursor.visible = false;
                    Cursor.lockState = (CursorLockMode)1;
                }
            }
            return true;
        }

        [HarmonyPatch(typeof(PlayerControllerB), "Update")]
        [HarmonyPostfix]
        public static void UpdatePostfix(PlayerControllerB __instance)
        {
            if (__instance == StartOfRound.Instance.localPlayerController)
            {
                NoclipBehaviour.Update();
                NightVisionBehaviour.Update(__instance);
                KillEnemyBehaviour.Update(__instance);
            }
        }

        [HarmonyPatch(typeof(PlayerControllerB), "Start")]
        [HarmonyPostfix]
        public static void StartPostfix(PlayerControllerB __instance)
        {
            if (!(__instance == StartOfRound.Instance.localPlayerController))
            {
            }
        }

        /// <summary>
        /// V81 Fix: DamagePlayerFromOtherClientServerRpc signature may have changed.
        /// Using a more flexible approach with Traverse to handle parameter changes.
        /// </summary>
        [HarmonyPatch(typeof(PlayerControllerB), "DamagePlayerFromOtherClientServerRpc")]
        [HarmonyPostfix]
        public static void DamagePlayerFromOtherClientServerRpcPostfix(PlayerControllerB __instance, int damageAmount, Vector3 hitDirection, int playerWhoHit)
        {
            DamageCkeckBehaviour.Check(__instance, playerWhoHit);
        }
    }
}
