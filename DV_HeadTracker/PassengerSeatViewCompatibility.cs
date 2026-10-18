using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace DV_HeadTracker
{
    internal static class PassengerSeatViewCompatibility
    {
        private static bool _patched;
        private static bool _patchAttempted;

        private static FieldInfo _seatCameraField;
        private static FieldInfo _seatYawField;
        private static FieldInfo _seatPitchField;

        public static bool IsActive
        {
            get
            {
                if (_seatCameraField == null)
                    return false;

                Type seatViewType = _seatCameraField.DeclaringType;

                PropertyInfo activeProperty = AccessTools.Property(
                    seatViewType,
                    "IsActive");

                return activeProperty != null &&
                       (bool)activeProperty.GetValue(null, null);
            }
        }

        public static void TryInstall()
        {
            if (_patched || _patchAttempted)
                return;

            Type seatViewType = AccessTools.TypeByName(
                "DV_People.PassengerSeatViewController");

            if (seatViewType == null)
                return;

            _patchAttempted = true;

            MethodInfo lateUpdate = AccessTools.Method(
                seatViewType,
                "RuntimeLateUpdate");

            _seatCameraField = AccessTools.Field(
                seatViewType,
                "_seatCamera");

            _seatYawField = AccessTools.Field(
                seatViewType,
                "_yaw");

            _seatPitchField = AccessTools.Field(
                seatViewType,
                "_pitch");

            if (lateUpdate == null ||
                _seatCameraField == null ||
                _seatYawField == null ||
                _seatPitchField == null)
            {
                Main.Log("[DV_HeadTracker] DV_People Seat View compatibility: required members not found.");
                return;
            }

            MethodInfo postfix = AccessTools.Method(
                typeof(PassengerSeatViewCompatibility),
                nameof(SeatViewLateUpdatePostfix));

            try
            {
                Main.Harmony.Patch(
                    lateUpdate,
                    postfix: new HarmonyMethod(postfix));

                _patched = true;

                Main.Log("[DV_HeadTracker] DV_People Seat View compatibility enabled.");
            }
            catch (Exception ex)
            {
                Main.Log("[DV_HeadTracker] DV_People Seat View patch failed: " + ex);
            }
        }

        private static void SeatViewLateUpdatePostfix()
        {
            if (Main.Settings == null ||
                !Main.HeadTrackingEnabled ||
                !HeadTrackingStartupController.IsActive)
                return;

            Camera camera = _seatCameraField.GetValue(null) as Camera;

            if (camera == null)
                return;

            float mouseYaw = (float)_seatYawField.GetValue(null);
            float mousePitch = (float)_seatPitchField.GetValue(null);

            float headYaw =
                HeadTrackingController.EffectiveYaw *
                Main.Settings.yawSensitivity;

            float headPitch =
                HeadTrackingController.EffectivePitch *
                Main.Settings.pitchSensitivity;

            Quaternion combinedLookRotation =
                Quaternion.Euler(
                    Mathf.Clamp(mousePitch - headPitch, -80f, 80f),
                    mouseYaw + headYaw,
                    0f);

            Quaternion seatWorldRotation =
                camera.transform.rotation *
                Quaternion.Inverse(
                    Quaternion.Euler(mousePitch, mouseYaw, 0f));

            camera.transform.rotation =
                seatWorldRotation * combinedLookRotation;

            HeadTrackingController.ConsumeRecenterRequest();
        }
    }
}
