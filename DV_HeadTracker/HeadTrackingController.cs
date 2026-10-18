using DV.Interaction.Inputs;
using DV.UI;
using DV.Utils;
using System;
using UnityEngine;

namespace DV_HeadTracker
{
    public static class HeadTrackingController
    {
        private static double _centreYaw;
        private static double _centrePitch;
        private static bool _recenterRequested;
        private static readonly object _cursorRequestOwner = new object();
        private static bool _cursorRequested;
        private static bool _viewLocked;
        private static float _lockedYaw;
        private static float _lockedPitch;
        private static float _mousePriorityUntil;
        private static bool _movingOnFoot;
        private static bool _waitingForHeadCentre;
        private static double _bodyYawOffset;
        private static bool _wasHeadOutsideDeadzone;

        public static bool IsViewLocked => _viewLocked;
        public static bool IsHeadViewActive =>
             EffectiveYaw != 0f || EffectivePitch != 0f;

        public static bool MousePriorityActive =>
            !_movingOnFoot && Time.unscaledTime < _mousePriorityUntil;
     
        public static void NotifyMouseLookActivity()
        {
            if (!Main.HeadTrackingEnabled)
                return;

            if (!_movingOnFoot)
            {
                float delay =
                    Main.Settings != null
                        ? Main.Settings.mouselookDelay
                        : 1f;

                _mousePriorityUntil = Time.unscaledTime + delay;
            }
        }

        public static float CentredRawYaw
        {
            get
            {
                return (float)(OpenTrackReceiver.Yaw - _centreYaw);
            }
        }

        public static float EffectiveYaw
        {
            get
            {
                if (!Main.HeadTrackingEnabled || !HeadTrackingStartupController.IsActive)
                    return 0f;

                if (_waitingForHeadCentre)
                    return 0f;

                if (_viewLocked)
                    return _lockedYaw;

                float yaw = (float)(OpenTrackReceiver.Yaw - _centreYaw - _bodyYawOffset);

                return ApplyDeadzone(
                    yaw,
                    Main.Settings != null ? Main.Settings.yawDeadzone : 0f);
            }
        }

        public static float EffectivePitch
        {
            get
            {
                if (!HeadTrackingStartupController.IsActive)
                    return 0f;

                if (_waitingForHeadCentre)
                    return 0f;

                if (_viewLocked)
                    return _lockedPitch;

                float pitch = (float)(OpenTrackReceiver.Pitch - _centrePitch);

                return ApplyDeadzone(
                    pitch,
                    Main.Settings != null ? Main.Settings.pitchDeadzone : 0f);
            }
        }

        private static float ApplyDeadzone(float value, float deadzone)
        {
            float abs = UnityEngine.Mathf.Abs(value);

            if (abs <= deadzone)
                return 0f;

            return UnityEngine.Mathf.Sign(value) * (abs - deadzone);
        }

        public static void Recenter()
        {
            if (!Main.HeadTrackingEnabled)
                return;

            _centreYaw = OpenTrackReceiver.Yaw;
            _centrePitch = OpenTrackReceiver.Pitch;

            _recenterRequested = true;

            HeadTrackingStartupController.Activate();

            Main.Log($"Head tracking recentered | Yaw={_centreYaw:F2} | Pitch={_centrePitch:F2}");
        }

        
        public static bool ConsumeRecenterRequest()
        {
            if (!_recenterRequested)
                return false;

            _recenterRequested = false;
            return true;
        }

        public static void UpdateCursorMode(CustomFirstPersonController controller)
        {
            if (!PlayerCameraSwitcher.IsInFirstPerson)
            {
                ReleaseCursorRequest();
                return;
            }
            if (controller == null ||
                Main.Settings == null ||
                !Main.HeadTrackingEnabled)
            {
                ReleaseCursorRequest();
                return;
            }

            
            if (_waitingForHeadCentre)
            {
                float yaw = Mathf.Abs(CentredRawYaw);
                float pitch = Mathf.Abs((float)(OpenTrackReceiver.Pitch - _centrePitch));

                if (yaw <= Main.Settings.yawDeadzone)
                {
                    _waitingForHeadCentre = false;
                    Main.Log("Head tracking re-armed.");
                }
            }


            bool headOutsideDeadzone =
                EffectiveYaw != 0f ||
                EffectivePitch != 0f;

            if (_wasHeadOutsideDeadzone && !headOutsideDeadzone)
            {
                NativeCrosshairBridge.RequestShow();
            }

            _wasHeadOutsideDeadzone = headOutsideDeadzone;

            _movingOnFoot = controller.m_Input.sqrMagnitude > float.Epsilon;

            if (_movingOnFoot && _viewLocked)
                ReturnToMouseView();

            bool shouldRequestMenuCursor =
                HeadTrackingUIController.ManualMouseMode ||
                (headOutsideDeadzone &&
                 !_movingOnFoot &&
                 !MousePriorityActive);

            if (shouldRequestMenuCursor)
            {
                if (!_cursorRequested)
                {
                    SingletonBehaviour<ScreenspaceMouse>.Instance.RequestOverride(
                        _cursorRequestOwner,
                        true);

                    _cursorRequested = true;

                    Main.Log("Head tracking requested screenspace mouse.");
                }
            }
            else
            {
                ReleaseCursorRequest();
            }
        }

        private static void ReleaseCursorRequest()
        {
            if (!_cursorRequested)
                return;

            SingletonBehaviour<ScreenspaceMouse>.Instance.RemoveRequest(
                _cursorRequestOwner);

            _cursorRequested = false;

            Main.Log("Head tracking released menu cursor.");
        }

        public static void AlignBodyToHead(CustomFirstPersonController controller)
        {
            if (controller == null ||
                Main.Settings == null ||
                !Main.HeadTrackingEnabled ||
                !HeadTrackingStartupController.IsActive)
                return;

            float yaw =
                EffectiveYaw *
                Main.Settings.yawSensitivity;

            float pitch =
                EffectivePitch *
                Main.Settings.pitchSensitivity;

            if (Mathf.Approximately(yaw, 0f) &&
                Mathf.Approximately(pitch, 0f))
                return;

            _viewLocked = false;
            _bodyYawOffset = 0.0;
            _waitingForHeadCentre = true;

            ReleaseCursorRequest();

            controller.RotateViewBy(
                new Vector2(yaw, pitch));

            Main.Log($"Body aligned to head | Yaw={yaw:F2} | Pitch={pitch:F2}");
        }

        public static void ReturnToMouseView()
        {
            _viewLocked = false;
            _lockedYaw = 0f;
            _lockedPitch = 0f;
            _waitingForHeadCentre = true;
            _bodyYawOffset = 0.0;

            ReleaseCursorRequest();

            Main.Log("Returned to mouse view; waiting for head centre.");
        }

        public static void ToggleViewLock()
        {
            if (!_viewLocked)
            {
                _lockedYaw = EffectiveYaw;
                _lockedPitch = EffectivePitch;
                _viewLocked = true;

                Main.Log($"Head view frozen | Yaw={_lockedYaw:F2} | Pitch={_lockedPitch:F2}");
            }
            else
            {
                ReturnToMouseView();
            }
        }

        public static void DisableTracking()
        {
            ReleaseCursorRequest();

            _viewLocked = false;
            _lockedYaw = 0f;
            _lockedPitch = 0f;

            _mousePriorityUntil = 0f;
            _movingOnFoot = false;

            _waitingForHeadCentre = false;
            _bodyYawOffset = 0.0;

            _wasHeadOutsideDeadzone = false;
            _recenterRequested = false;

            NativeCrosshairBridge.RequestShow();

            Main.Log("Head tracking runtime state cleared.");
        }
        public static void ResetSession()
        {
            ReleaseCursorRequest();

            _viewLocked = false;
            _lockedYaw = 0f;
            _lockedPitch = 0f;

            _mousePriorityUntil = 0f;
            _movingOnFoot = false;

            _waitingForHeadCentre = false;
            _bodyYawOffset = 0.0;
            _wasHeadOutsideDeadzone = false;

            _recenterRequested = false;
        }
    }
}



namespace DV_HeadTracker
{
    public static class HeadTrackingUIController
    {
        private static GameObject _cursorButtons;
        private static GameObject _weatherButton;
        private static Texture2D _headCursor;

        private const float CursorFadeDuration = 0.15f;

        private static float _lastMouseActivityTime;
        private static float _cursorAlpha = -1f;
        private static Vector3 _lastMousePosition;
        private static bool _mousePositionInitialised;
        private static double _lastHeadYaw;
        private static double _lastHeadPitch;
        private static bool _headPositionInitialised;

        private const double HeadMovementThreshold = 0.1;
        private static bool _headCursorActive;
        private static bool _manualMouseMode;
        public static bool ManualMouseMode => _manualMouseMode;

        private static float _nextHUDFindTime;
        private const float HUDFindInterval = 1f;

        public static void CancelAltTap()
        {
            _altTapCandidate = false;
        }

        private static bool _altTapCandidate;

        public static void Update()
        {
            if (!PlayerCameraSwitcher.IsInFirstPerson)
            {
                RestoreCursor();
                return;
            }
            if (Main.Settings == null || !Main.HeadTrackingEnabled)
            {
                _manualMouseMode = false;
                _altTapCandidate = false;

                RestoreCursor();
                return;
            }

            bool altDown =
                Input.GetKeyDown(KeyCode.LeftAlt) ||
                Input.GetKeyDown(KeyCode.RightAlt);

            bool altHeld =
                Input.GetKey(KeyCode.LeftAlt) ||
                Input.GetKey(KeyCode.RightAlt);

            bool altUp =
                Input.GetKeyUp(KeyCode.LeftAlt) ||
                Input.GetKeyUp(KeyCode.RightAlt);

            if (altDown)
            {
                _altTapCandidate = true;
            }

            if (_altTapCandidate && altHeld)
            {
                if (Input.GetKeyDown(KeyCode.Mouse2) ||
                    Input.GetKeyDown(KeyCode.H))
                {
                    _altTapCandidate = false;
                }
            }

            if (altUp)
            {
                if (_altTapCandidate)
                    _manualMouseMode = !_manualMouseMode;

                _altTapCandidate = false;
            }

            FindHUDObjects();

            if (_manualMouseMode)
            {
                if (_cursorButtons != null)
                    _cursorButtons.SetActive(true);

                if (_weatherButton != null)
                    _weatherButton.SetActive(true);

                RestoreCursor();
                return;
            }

            if (HeadTrackingController.IsHeadViewActive)
            {
                if (_cursorButtons != null)
                    _cursorButtons.SetActive(false);

                if (_weatherButton != null)
                    _weatherButton.SetActive(false);

                SetHeadCursor();
                UpdateHeadCursorFade();
            }
            else
            {
                RestoreCursor();
            }
        }

        public static void OnHeadTrackingDisabled()
        {
            _manualMouseMode = false;
            _altTapCandidate = false;

            if (_cursorButtons != null)
                _cursorButtons.SetActive(true);

            if (_weatherButton != null)
                _weatherButton.SetActive(true);

            RestoreCursor();
        }

        private static void FindHUDObjects()
        {
            if (_cursorButtons != null && _weatherButton != null)
                return;

            if (Time.unscaledTime < _nextHUDFindTime)
                return;

            _nextHUDFindTime = Time.unscaledTime + HUDFindInterval;

            if (_cursorButtons == null)
            {
                _cursorButtons = GameObject.Find(
                    "[MAIN]/[GameUI]/[NewCanvasController]/Auxiliary Canvas, EventSystem, Input Module/TopOfLocoHUDGroup/CursorButtonsGroup");

                Main.Log($"CursorButtons found={_cursorButtons != null}");
            }

            if (_weatherButton == null)
            {
                _weatherButton = GameObject.Find(
                    "[MAIN]/[GameUI]/common/HUDManager/ControlsGroup/PhotoModeWeatherSettings");

                Main.Log($"WeatherButton found={_weatherButton != null}");
            }
        }

        private static void SetHeadCursor()
        {
            if (_headCursorActive)
                return;

            if (_headCursor == null)
                _headCursor = CreateHeadCursor();

            Cursor.SetCursor(
                _headCursor,
                new Vector2(8f, 8f),
                CursorMode.ForceSoftware);

            _headCursorActive = true;
        }

        private static void RestoreCursor()
        {
            _mousePositionInitialised = false;
            _headPositionInitialised = false;

            if (!_headCursorActive)
                return;

            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);

            _headCursorActive = false;
            _cursorAlpha = -1f;
        }

        private static void UpdateHeadCursorFade()
        {
            if (!_headCursorActive || _headCursor == null)
                return;

            Vector3 mousePosition = Input.mousePosition;

            if (!_mousePositionInitialised)
            {
                _lastMousePosition = mousePosition;
                _lastMouseActivityTime = Time.unscaledTime;
                _mousePositionInitialised = true;
            }

            bool mouseMoved = mousePosition != _lastMousePosition;

            _lastMousePosition = mousePosition;

            double headYaw = OpenTrackReceiver.Yaw;
            double headPitch = OpenTrackReceiver.Pitch;

            bool headMoved = false;

            if (!_headPositionInitialised)
            {
                _lastHeadYaw = headYaw;
                _lastHeadPitch = headPitch;
                _headPositionInitialised = true;
            }
            else
            {
                headMoved =
                    Math.Abs(headYaw - _lastHeadYaw) > HeadMovementThreshold ||
                    Math.Abs(headPitch - _lastHeadPitch) > HeadMovementThreshold;

                _lastHeadYaw = headYaw;
                _lastHeadPitch = headPitch;
            }

            if (mouseMoved || headMoved)
                _lastMouseActivityTime = Time.unscaledTime;

            float alpha = 1f;

            if (Main.Settings.fadeHeadTrackingCursor)
            {
                float inactiveTime = Time.unscaledTime - _lastMouseActivityTime;

                float fadeDelay = Mathf.Clamp(
                    Main.Settings.headCursorFadeDelay,
                    1.0f,
                    5.0f);

                if (inactiveTime > fadeDelay)
                {
                    alpha = Mathf.Clamp01(
                        1f - (inactiveTime - fadeDelay) / CursorFadeDuration);
                }
            }

            if (Mathf.Approximately(alpha, _cursorAlpha))
                return;

            _cursorAlpha = alpha;

            Color[] pixels = _headCursor.GetPixels();

            for (int i = 0; i < pixels.Length; i++)
            {
                if (pixels[i].r > 0.5f)
                    pixels[i].a = alpha;
            }

            _headCursor.SetPixels(pixels);
            _headCursor.Apply();

            Cursor.SetCursor(
                _headCursor,
                new Vector2(8f, 8f),
                CursorMode.ForceSoftware);
        }

        private static Texture2D CreateHeadCursor()
        {
            const int size = 17;
            const int centre = 8;

            var texture = new Texture2D(
                size,
                size,
                TextureFormat.RGBA32,
                false);

            var clear = new Color(0f, 0f, 0f, 0f);

            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    texture.SetPixel(x, y, clear);

            for (int i = 3; i <= 13; i++)
            {
                texture.SetPixel(centre, i, Color.white);
                texture.SetPixel(i, centre, Color.white);
            }

            texture.Apply();
            return texture;
        }


    }
}