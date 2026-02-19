using HarmonyLib;
using System;
using UnityEngine;

namespace Omni
{
    public class OmniMod : IModApi
    {
        public void InitMod(Mod _modInstance)
        {
            new Harmony("7D-Omni").PatchAll();
        }
    }

    [HarmonyPatch(typeof(EntityPlayerLocal), "MoveByInput")]
    static class Patch_EntityPlayerLocal_MoveByInput
    {
        public static bool AnyMoveNonZero(EntityPlayerLocal self)
        {
            return Mathf.Abs(self.moveDirection.x) > 0.0001f || Mathf.Abs(self.moveDirection.z) > 0.0001f;
        }


        public static bool Prefix(EntityPlayerLocal __instance)
        {
            MoveByInput(__instance);

            return false;
        }

        public static void MoveByInput(EntityPlayerLocal __instance)
        {
            bool isCrouching = __instance.IsCrouching;
            if (__instance.IsStuck || EffectManager.GetValue(PassiveEffects.DisableMovement, null, 0f, __instance, null, default(FastTags<TagGroup.Global>), true, true, true, true, true, 1, true, false) > 0f)
            {
                __instance.movementInput.Clear();
            }
            if (EffectManager.GetValue(PassiveEffects.FlipControls, null, 0f, __instance, null, default(FastTags<TagGroup.Global>), true, true, true, true, true, 1, true, false) > 0f)
            {
                __instance.movementInput.moveForward *= -1f;
                __instance.movementInput.moveStrafe *= -1f;
            }
            if (__instance.AttachedToEntity != null)
            {
                __instance.Crouching = false;
                __instance.CrouchingLocked = false;
                __instance.Climbing = false;
                __instance.MovementRunning = false;
                __instance.AimingGun = false;
                __instance.AttachedToEntity.MoveByAttachedEntity(__instance);
            }
            else
            {
                bool isMoving = false;
                __instance.moveDirection.x = __instance.movementInput.moveStrafe;
                __instance.moveDirection.z = __instance.movementInput.moveForward;
                if (__instance.moveDirection.x != 0f || __instance.moveDirection.z != 0f)
                {
                    isMoving = true;
                }
                bool flag2 = ((!__instance.IsSwimming()) ? (!__instance.bExhausted && AnyMoveNonZero(__instance)) : (__instance.swimExhaustedTicks == 0));
                bool flag3 = __instance.movementInput.running && isMoving;
                if (!__instance.IsFlyMode.Value)
                {
                    flag3 = flag3 && flag2;
                }
                __instance.MovementRunning = flag3;
                if (__instance.IsSwimming())
                {
                    if (!__instance.IsSwimmingMoving() || __instance.swimExhaustedTicks > 0)
                    {
                        __instance.CurrentMovementTag = EntityAlive.MovementTagFloating;
                    }
                    else if (!__instance.MovementRunning)
                    {
                        __instance.CurrentMovementTag = EntityAlive.MovementTagSwimming;
                    }
                    else
                    {
                        __instance.CurrentMovementTag = EntityAlive.MovementTagSwimmingRun;
                    }
                }
                else if (isMoving)
                {
                    if (!__instance.MovementRunning)
                    {
                        __instance.CurrentMovementTag = EntityAlive.MovementTagWalking;
                    }
                    else
                    {
                        __instance.CurrentMovementTag = EntityAlive.MovementTagRunning;
                    }
                }
                else
                {
                    __instance.CurrentMovementTag = EntityAlive.MovementTagIdle;
                }
                if (__instance.movementInput.downToggle)
                {
                    __instance.CrouchingLocked = !__instance.CrouchingLocked;
                }
                __instance.CrouchingLocked = __instance.CrouchingLocked && !__instance.isLadderAttached && !__instance.movementInput.down;
                __instance.Crouching = !__instance.IsFlyMode.Value && !__instance.isLadderAttached && (__instance.movementInput.down || __instance.CrouchingLocked);
                if (!__instance.AimingGun)
                {
                    if (!__instance.IsFlyMode.Value)
                    {
                        if (!__instance.JetpackWearing)
                        {
                            if (__instance.movementInput.jump && __instance.vp_FPController && !__instance.inputWasJump)
                            {
                                __instance.vp_FPController.enabled = true;
                            }
                            if (!__instance.Jumping && !__instance.wasJumping && __instance.movementInput.jump && (__instance.onGround || __instance.isLadderAttached) && __instance.AttachedToEntity == null)
                            {
                                __instance.jumpTrigger = true;
                            }
                            else if (__instance.wasLadderAttachedJump && !__instance.isLadderAttached && __instance.movementInput.jump && !__instance.inputWasJump)
                            {
                                __instance.canLadderAirAttach = true;
                            }
                        }
                        else
                        {
                            if (__instance.movementInput.jump)
                            {
                                __instance.motion.y = __instance.motion.y + 0.15f;
                                isMoving = true;
                            }
                            if (__instance.movementInput.down)
                            {
                                __instance.motion.y = __instance.motion.y - 0.15f;
                            }
                        }
                    }
                    else
                    {
                        if (__instance.movementInput.jump)
                        {
                            if (__instance.movementInput.running)
                            {
                                __instance.motion.y = 0.9f;
                            }
                            else
                            {
                                __instance.motion.y = 0.3f * __instance.GodModeSpeedModifier;
                            }
                        }
                        if (__instance.movementInput.down)
                        {
                            if (__instance.movementInput.running)
                            {
                                __instance.motion.y = -0.9f;
                            }
                            else
                            {
                                __instance.motion.y = -0.3f * __instance.GodModeSpeedModifier;
                            }
                        }
                    }
                }
                __instance.JetpackActive = __instance.JetpackWearing && isMoving;
            }
            if (!SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer)
            {
                bool isCrouching2 = __instance.IsCrouching;
                if (isCrouching2 != isCrouching)
                {
                    SingletonMonoBehaviour<ConnectionManager>.Instance.SendToServer(NetPackageManager.GetPackage<NetPackageEntityStealth>().Setup(__instance, isCrouching2), false);
                }
            }
            if (!__instance.bFirstPersonView && __instance.vp_FPCamera.Locked3rdPerson)
            {
                Vector3 right = __instance.transform.right;
                Vector3 forward = __instance.transform.forward;
                right.y = 0f;
                forward.y = 0f;
                right.Normalize();
                forward.Normalize();
                __instance.moveDirection = forward * __instance.moveDirection.z + right * __instance.moveDirection.x;
            }
            else if (!__instance.bFirstPersonView && __instance.CameraRelativeMovement)
            {
                Vector3 right2 = __instance.playerCamera.transform.right;
                Vector3 forward2 = __instance.playerCamera.transform.forward;
                right2.y = 0f;
                forward2.y = 0f;
                right2.Normalize();
                forward2.Normalize();
                __instance.moveDirection = forward2 * __instance.moveDirection.z + right2 * __instance.moveDirection.x;
            }
            if (__instance.vp_FPController != null)
            {
                if (__instance.AttachedToEntity == null)
                {
                    __instance.vp_FPController.Player.InputMoveVector.Set(new Vector2(__instance.moveDirection.x, __instance.moveDirection.z));
                }
                __instance.vp_FPController.Player.InputSmoothLook.Set(new Vector2(__instance.movementInput.rotation.y, -__instance.movementInput.rotation.x));
            }
            __instance.inputWasJump = __instance.movementInput.jump;
            __instance.inputWasDown = __instance.movementInput.down;
            __instance.movementInput.Clear();
        }
    }

    [HarmonyPatch(typeof(PlayerMoveController), "Update")]
    static class Patch_PlayerMoveController_Update
    {
        public static void Postfix(PlayerMoveController __instance)
        {
            try
            {
                var playerInput = __instance.playerInput;
                if (playerInput == null)
                    return;

                bool enabled = playerInput.VehicleActions.Enabled;
                Vector2 moveVec = enabled ? playerInput.VehicleActions.Move.Vector : playerInput.Move.Vector;
                bool isAnyMove = moveVec.magnitude >= 0.35f
                                 || playerInput.MoveForward.IsPressed
                                 || playerInput.MoveBack.IsPressed
                                 || playerInput.MoveLeft.IsPressed
                                 || playerInput.MoveRight.IsPressed;

                // quick-tap/run-release -> toggle guard (allow omni-directional movement)
                if (playerInput.Run.WasReleased && !__instance.runToggleActive && __instance.runInputTime < 0.2f)
                {
                    if (isAnyMove || __instance.sprintMode == 1)
                    {
                        __instance.runToggleActive = true;
                        if (__instance.entityPlayerLocal != null)
                            __instance.entityPlayerLocal.movementInput.running = true;
                    }
                }

                // when toggle is active, stop it if player stops moving (replace forward-only release check)
                if (__instance.runToggleActive)
                {
                    bool flag7 = __instance.wasVehicle != enabled;
                    var epl = __instance.entityPlayerLocal;
                    // preserve original stop conditions and also clear toggle when in TOGGLE mode and player stops moving
                    if (flag7 || (__instance.sprintMode == 0 && ((epl != null && epl.Stamina <= 0f) || !isAnyMove)) || (__instance.sprintMode == 1 && !isAnyMove))
                    {
                        __instance.runToggleActive = false;
                        __instance.runPressedWhileActive = false;
                        if (epl != null)
                            epl.movementInput.running = false;
                    }
                    else
                    {
                        if (epl != null)
                            epl.movementInput.running = true;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[OmniSprint] Update postfix error: " + ex);
            }
        }
    }

    [HarmonyPatch(typeof(EntityPlayerLocal), "SetMoveState")]
    static class Patch_EntityPlayerLocal_SetMoveState
    {
        public static void Postfix(EntityPlayerLocal __instance, EntityPlayerLocal.MoveState _state)
        {
            try
            {
                if (_state == EntityPlayerLocal.MoveState.Run && __instance != null && __instance.vp_FPController != null)
                {
                    // make sideways speed equal to forward speed
                    __instance.vp_FPController.MotorSidewaysSpeed = 1f;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[OmniSprint] SetMoveState postfix error: " + ex);
            }
        }
    }

    // Fix swim strafing scale when sprinting (remove 0.7 penalty while MovementRunning)
    [HarmonyPatch(typeof(EntityPlayerLocal), "SwimModeUpdateThrottle")]
    static class Patch_EntityPlayerLocal_SwimModeUpdateThrottle
    {
        static void Postfix(EntityPlayerLocal __instance)
        {
            try
            {
                if (__instance != null && __instance.MovementRunning && __instance.vp_FPController != null)
                {
                    var mt = __instance.vp_FPController.m_MotorThrottle;
                    // the original multiplies strafe by 0.7f for swim — undo that when sprinting
                    mt.x *= (1f / 0.7f);
                    __instance.vp_FPController.m_MotorThrottle = mt;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[OmniSprint] SwimModeUpdateThrottle postfix error: " + ex);
            }
        }
    }
}
