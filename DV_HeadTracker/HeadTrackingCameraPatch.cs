using DV.Utils;
using HarmonyLib;
using System;
using UnityEngine;
using DV.Interaction.Inputs;

namespace DV_HeadTracker
{
    [HarmonyPatch(
        typeof(CustomMouseLook),
        nameof(CustomMouseLook.LookRotation),
        new Type[]
        {
            typeof(Transform),
            typeof(Transform),
            typeof(Vector2)
        })]
    public static class HeadTrackingCameraPatch
    {
        private static void Prefix(ref Vector2 mouseDelta)
        {
            if (!PlayerCameraSwitcher.IsInFirstPerson)
                return;

            if (Main.Settings != null &&
                Main.HeadTrackingEnabled &&
                HeadTrackingStartupController.IsActive &&
                HeadTrackingController.IsHeadViewActive &&
                InputManager.NewPlayer.GetButton(InputManager.Actions.Zoom))
            {
                mouseDelta = Vector2.zero;
                return;
            }

            if (mouseDelta.sqrMagnitude <= 0.0001f)
                return;

            var screenspace = SingletonBehaviour<ScreenspaceMouse>.Instance;

            if (screenspace != null && !screenspace.on)
                HeadTrackingController.NotifyMouseLookActivity();
        }

        private static void Postfix(
            CustomMouseLook __instance,
            Transform character,
            Transform camera,
            Vector2 mouseDelta)
        {
            if (!PlayerCameraSwitcher.IsInFirstPerson)
                return;

            if (camera == null)
                return;

            if (Main.Settings == null || !Main.HeadTrackingEnabled)
                return;

            if (HeadTrackingController.MousePriorityActive)
                return;

            if (HeadTrackingController.ConsumeRecenterRequest())
            {
                Quaternion bodyForward =
                    Quaternion.Euler(0f, character.eulerAngles.y, 0f);

                __instance.ForceRotationNoTilt(
                    character,
                    camera,
                    bodyForward);
            }

            float headYaw =
                HeadTrackingController.EffectiveYaw *
                Main.Settings.yawSensitivity;

            float headPitch =
                HeadTrackingController.EffectivePitch *
                Main.Settings.pitchSensitivity;

            camera.localRotation =
                Quaternion.Euler(-headPitch, headYaw, 0f) *
                camera.localRotation;
        }
    }
}

namespace DV_HeadTracker
{
    [HarmonyPatch(typeof(CameraZoom), "LateUpdate")]
    public static class CameraZoomPatch
    {
        private static bool Prefix(
            CameraZoom __instance,
            ref bool ___isZoomPressed,
            ref bool ___mouseZoomed,
            ref float ___currentZoomVelocity,
            float ___normalFOV,
            float ___zoomedFOV,
            float ___zoomInTime,
            float ___zoomOutTime,
            bool ___disableZoomForced)
        {
            if (Main.Settings == null || !Main.HeadTrackingEnabled)
                return true;

            if (!HeadTrackingStartupController.IsActive)
                return true;

            if (!HeadTrackingController.IsHeadViewActive)
                return true;

            Camera cam = __instance.cam;

            if (cam == null)
                return true;

            bool zoomHeld =
                InputManager.NewPlayer.GetButton(InputManager.Actions.Zoom);

            bool shouldZoom =
                zoomHeld && !___disableZoomForced;

            ___mouseZoomed = false;
            ___isZoomPressed = shouldZoom;

            float targetFOV =
                shouldZoom ? ___zoomedFOV : ___normalFOV;

            float smoothTime =
                shouldZoom ? ___zoomInTime : ___zoomOutTime;

            if (Time.deltaTime > 0f)
            {
                cam.fieldOfView = Mathf.SmoothDamp(
                    cam.fieldOfView,
                    targetFOV,
                    ref ___currentZoomVelocity,
                    smoothTime);
            }

            if (Mathf.Abs(cam.fieldOfView - targetFOV) < 0.01f)
            {
                cam.fieldOfView = targetFOV;
                ___currentZoomVelocity = 0f;
            }

            // Skip CameraZoom.LateUpdate completely while Head View is active.
            return false;
        }
    }
}

namespace DV_HeadTracker
{
    public static class NativeCrosshairBridge
    {
        private static bool _showRequested;

        public static void RequestShow()
        {
            _showRequested = true;
        }

        public static bool ConsumeShowRequest()
        {
            if (!_showRequested)
                return false;

            _showRequested = false;
            return true;
        }
    }

    [HarmonyPatch(typeof(InteractionInfoDynamicToggle), "TriggerFadeByRotation")]
    internal static class NativeCrosshairPatch
    {
        private static void Postfix(object __0, ref bool __result)
        {
            if (Convert.ToInt32(__0) != 1)
                return;

            if (!NativeCrosshairBridge.ConsumeShowRequest())
                return;

            __result = true;
        }
    }
}
