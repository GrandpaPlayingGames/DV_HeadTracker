using System;
using UnityEngine;
using UnityModManagerNet;

namespace DV_HeadTracker
{
    [Serializable]
    public class Settings : UnityModManager.ModSettings, IDrawable
    {
        // =========================================================
        // GENERAL
        // =========================================================
#if DEBUG
        public bool debugLogging = false;
#endif
        public float yawSensitivity = 4.5f;
        public float pitchSensitivity = 6.0f;

        public float yawDeadzone = 5.0f;
        public float pitchDeadzone = 3.0f;

        public float mouselookDeadzone = 7f;
        public float mouselookDelay = 0.75f;

        public bool fadeHeadTrackingCursor = false;
        public float headCursorFadeDelay = 3.0f;

        // =========================================================
        // SAVE
        // =========================================================

        public override void Save(UnityModManager.ModEntry modEntry)
        {
            Save(this, modEntry);
        }

        public void OnChange()
        {
            // Intentionally empty
        }

        public KeyBind lockViewKey = new KeyBind(KeyCode.Mouse2);
        private bool waitingLockViewKey = false;

        public KeyBind returnToMouseViewKey = new KeyBind(KeyCode.Mouse2, shift: true);
        private bool waitingReturnToMouseViewKey = false;

        public KeyBind alignBodyToHeadKey = new KeyBind(KeyCode.Mouse2, alt: true);
        private bool waitingAlignBodyToHeadKey = false;

        public KeyBind recenterKey = new KeyBind(KeyCode.Mouse2, alt: true, shift: true);
        private bool waitingRecenterKey = false;

        public KeyBind toggleHeadTrackingKey = new KeyBind(KeyCode.H, shift: true);
        private bool waitingToggleHeadTrackingKey = false;

        // =========================================================
        // DRAW UI
        // =========================================================

        public void Draw(UnityModManager.ModEntry modEntry)
        {
            var titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.gray }
            };

            GUILayout.Space(6);

            BeginGroup("General", titleStyle);

#if DEBUG
            debugLogging = GUILayout.Toggle(debugLogging, "Debug Logging");
#endif
            GUILayout.Space(8);

            fadeHeadTrackingCursor = GUILayout.Toggle(
                fadeHeadTrackingCursor,
                "Fade Head Tracking Cursor");

            GUILayout.Space(8);

            GUI.enabled = fadeHeadTrackingCursor;

            GUILayout.BeginHorizontal();

            GUILayout.Label(
                $"Cursor Fade Delay: {headCursorFadeDelay:F1}s",
                GUILayout.Width(180));

            headCursorFadeDelay = GUILayout.HorizontalSlider(
                headCursorFadeDelay,
                1.0f,
                5.0f,
                GUILayout.Width(200));

            GUILayout.EndHorizontal();

            GUI.enabled = true;

            GUILayout.Space(8);

            GUILayout.Label($"Yaw Sensitivity: {yawSensitivity:F2}");
            yawSensitivity = GUILayout.HorizontalSlider(
                yawSensitivity,
                0.5f,
                8.0f);

            GUILayout.Label($"Pitch Sensitivity: {pitchSensitivity:F2}");
            pitchSensitivity = GUILayout.HorizontalSlider(
                pitchSensitivity,
                0.5f,
                8.0f);

            GUILayout.Space(8);

            GUILayout.Label($"Yaw Deadzone: {yawDeadzone:F1}°");
            yawDeadzone = GUILayout.HorizontalSlider(
                yawDeadzone,
                0.0f,
                15.0f);

            GUILayout.Label($"Pitch Deadzone: {pitchDeadzone:F1}°");
            pitchDeadzone = GUILayout.HorizontalSlider(
                pitchDeadzone,
                0.0f,
                15.0f);

            GUILayout.Space(10);

            GUILayout.Label($"Mouselook Delay: {mouselookDelay:F1}s");
            mouselookDelay = GUILayout.HorizontalSlider(
                mouselookDelay,
                0.0f,
                5.0f);

            mouselookDelay = Mathf.Round(mouselookDelay * 10f) / 10f;

            GUILayout.Space(10);

            DrawKeybind(
                "Head Tracking On / Off",
                ref toggleHeadTrackingKey,
                ref waitingToggleHeadTrackingKey,
                () => Save(modEntry)
            );

            DrawKeybind(
                "Freeze / Return Head View",
                ref lockViewKey,
                ref waitingLockViewKey,
                () => Save(modEntry)
            );

            DrawKeybind(
                "Return to Mouse View",
                ref returnToMouseViewKey,
                ref waitingReturnToMouseViewKey,
                () => Save(modEntry)
            );

            DrawKeybind(
                "Align Body to Head View",
                ref alignBodyToHeadKey,
                ref waitingAlignBodyToHeadKey,
                () => Save(modEntry)
            );

            DrawKeybind(
                "Recenter Head Tracking",
                ref recenterKey,
                ref waitingRecenterKey,
                () => Save(modEntry)
            );

            EndGroup();
        }

        // =========================================================
        // UI HELPERS
        // =========================================================

        private void BeginGroup(string title, GUIStyle style)
        {
            GUI.backgroundColor = new Color(0.15f, 0.15f, 0.15f);
            GUILayout.BeginVertical("box");
            GUI.backgroundColor = Color.white;

            GUILayout.Label(title, style);
            GUILayout.Space(6);
        }

        private void EndGroup()
        {
            GUILayout.EndVertical();
            GUILayout.Space(10);
        }   

        private void DrawKeybind(string label, ref KeyBind keybind, ref bool waiting, System.Action onChanged = null)
        {
            GUILayout.BeginHorizontal();

            GUILayout.Label(label, GUILayout.Width(150));

            // Modifiers
            keybind.Ctrl = GUILayout.Toggle(keybind.Ctrl, "Ctrl", GUILayout.Width(50));
            keybind.Alt = GUILayout.Toggle(keybind.Alt, "Alt", GUILayout.Width(50));
            keybind.Shift = GUILayout.Toggle(keybind.Shift, "Shift", GUILayout.Width(60));

            if (waiting)
            {
                GUILayout.Label("Press key...", GUILayout.Width(100));

                Event e = Event.current;

                if (e != null)
                {
                    if (e.isKey && e.type == EventType.KeyDown)
                    {
                        if (e.keyCode != KeyCode.LeftControl &&
                            e.keyCode != KeyCode.RightControl &&
                            e.keyCode != KeyCode.LeftShift &&
                            e.keyCode != KeyCode.RightShift &&
                            e.keyCode != KeyCode.LeftAlt &&
                            e.keyCode != KeyCode.RightAlt)
                        {
                            keybind.Key = e.keyCode;

                            waiting = false;

                            e.Use();

                            onChanged?.Invoke();
                        }
                    }
                    else if (e.type == EventType.MouseDown)
                    {
                        keybind.Key = (KeyCode)((int)KeyCode.Mouse0 + e.button);

                        waiting = false;

                        e.Use();

                        onChanged?.Invoke();
                    }
                }
            }
            else
            {
                string keyText;

                if (keybind.Key == KeyCode.None)
                    keyText = "Unassigned";
                else if (keybind.Key == KeyCode.Mouse2)
                    keyText = "MMB";
                else
                    keyText = keybind.Key.ToString();

                if (GUILayout.Button(keyText, GUILayout.Width(120)))
                {
                    waiting = true;
                }
            }

            GUILayout.EndHorizontal();
        }
    }
}

[System.Serializable]
public class KeyBind
{
    public KeyCode Key;
    public bool Ctrl;
    public bool Alt;
    public bool Shift;

    public KeyBind()
    {
        Key = KeyCode.None;
    }

    public KeyBind(KeyCode key, bool ctrl = false, bool alt = false, bool shift = false)
    {
        Key = key;
        Ctrl = ctrl;
        Alt = alt;
        Shift = shift;
    }

    public bool IsPressed()
    {
        if (Key == KeyCode.None)
            return false;

        if (!Input.GetKeyDown(Key))
            return false;

        return ModifiersMatch();
    }


    public bool IsHeld()
    {
        if (Key == KeyCode.None)
            return false;

        if (!Input.GetKey(Key))
            return false;

        return ModifiersMatch();
    }

    private bool ModifiersMatch()
    {
        bool ctrlHeld = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        bool altHeld = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
        bool shiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        return ctrlHeld == Ctrl &&
               altHeld == Alt &&
               shiftHeld == Shift;
    }

    public override string ToString()
    {
        if (Key == KeyCode.None)
            return "Unassigned";

        string mod = "";

        if (Ctrl) mod += "Ctrl+";
        if (Alt) mod += "Alt+";
        if (Shift) mod += "Shift+";

        string keyName =
            Key == KeyCode.Mouse2
                ? "MMB"
                : Key.ToString();

        return mod + keyName;
    }
}