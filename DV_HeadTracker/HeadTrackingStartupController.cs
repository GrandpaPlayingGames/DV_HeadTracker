using UnityEngine;
using UnityEngine.UI;


namespace DV_HeadTracker
{
    public static class HeadTrackingStartupController
    {
        public enum StartupState
        {
            WaitingForSignal,
            WaitingForRecenter,
            Active
        }

        private const float StartupGracePeriod = 2f;

        private static StartupState _state = StartupState.WaitingForSignal;
        private static float _sessionStartTime;

        private static GameObject _promptObject;
        private static Text _promptText;

        public static StartupState State => _state;
        public static bool IsActive => _state == StartupState.Active;

        public static bool ShowNoSignalWarning =>
            _state == StartupState.WaitingForSignal &&
            Time.unscaledTime - _sessionStartTime >= StartupGracePeriod;

        public static void ResetSession()
        {
            _state = StartupState.WaitingForSignal;
            _sessionStartTime = Time.unscaledTime;
        }

        public static void Update()
        {
            if (_state == StartupState.Active)
            {
                if (!OpenTrackReceiver.SignalAvailable)
                    _state = StartupState.WaitingForSignal;

                UpdatePrompt();
                return;
            }

            if (_state == StartupState.WaitingForSignal &&
                OpenTrackReceiver.SignalAvailable)
            {
                _state = StartupState.WaitingForRecenter;
            }

            UpdatePrompt();
        }

        public static void Activate()
        {
            if (OpenTrackReceiver.SignalAvailable)
                _state = StartupState.Active;
        }

        private static void UpdatePrompt()
        {
            if (_state == StartupState.Active)
            {
                if (_promptObject != null)
                    _promptObject.SetActive(false);

                return;
            }

            if (_state == StartupState.WaitingForSignal && !ShowNoSignalWarning)
            {
                if (_promptObject != null)
                    _promptObject.SetActive(false);

                return;
            }

            EnsurePrompt();

            _promptObject.SetActive(true);

            _promptText.text =
                _state == StartupState.WaitingForSignal
                ? "No OpenTrack signal detected"
                : $"Center your head and view, then press {Main.Settings.recenterKey}";
        }

        private static void EnsurePrompt()
        {
            if (_promptObject != null)
                return;

            _promptObject = new GameObject("DV_HeadTracker Startup Prompt");

            var canvas = _promptObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10000;

            _promptObject.AddComponent<CanvasScaler>();

            GameObject textObject = new GameObject("Text");
            textObject.transform.SetParent(_promptObject.transform, false);

            _promptText = textObject.AddComponent<Text>();
            _promptText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            _promptText.fontSize = 22;
            _promptText.alignment = TextAnchor.MiddleCenter;
            _promptText.color = Color.white;

            var rect = _promptText.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -80f);
            rect.sizeDelta = new Vector2(800f, 50f);
        }
    }
}
