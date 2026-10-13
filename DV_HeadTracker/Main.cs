using System;
using UnityEngine;
using UnityModManagerNet;
using HarmonyLib;

namespace DV_HeadTracker
{
    public static class Main
    {
        public static UnityModManager.ModEntry Mod;
        public static Settings Settings;
        public static Harmony Harmony;

        private static bool _loaded;
        private static bool _gameLoaded;
        private static bool _isUnloading;
        public static bool IsUnloading => _isUnloading;

        private static CustomFirstPersonController _firstPersonController;

        private static bool _headTrackingEnabled = true;
        public static bool HeadTrackingEnabled => _headTrackingEnabled;

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            Mod = modEntry;

            Settings = UnityModManager.ModSettings.Load<Settings>(modEntry);

            modEntry.OnGUI = OnGUI;
            modEntry.OnSaveGUI = OnSaveGUI;
            modEntry.OnUpdate = Update;

            Harmony = new Harmony(modEntry.Info.Id);
            Harmony.PatchAll();

            OpenTrackReceiver.Start();

            _loaded = true;

            Log("[DV_HeadTracker] Mod loaded successfully.", true);
            Debug.Log($"[CONFIRM] Version = {Application.unityVersion}");
            return true;
        }


        private static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            Settings?.Draw(modEntry);
        }

        private static void OnSaveGUI(UnityModManager.ModEntry modEntry)
        {
            Settings?.Save(modEntry);
        }

        private static void Update(UnityModManager.ModEntry modEntry, float dt)
        {
            if (!_loaded)
                return;

            if (Settings == null)
                return;

            CheckGameLoadedOnce();

            if (!_gameLoaded)
                return;

            if (_firstPersonController == null)
            {
                _firstPersonController =
                    UnityEngine.Object.FindObjectOfType<CustomFirstPersonController>();
            }

            if (Settings.toggleHeadTrackingKey.IsPressed())
            {
                HeadTrackingUIController.CancelAltTap();
                ToggleHeadTracking();
            }

            HeadTrackingController.UpdateCursorMode(
                _firstPersonController);

            HeadTrackingUIController.Update();

            if (!PlayerCameraSwitcher.IsInFirstPerson)
            {
                if (_headTrackingEnabled &&
                    PassengerSeatViewCompatibility.IsActive &&
                    Settings.recenterKey.IsPressed())
                {
                    HeadTrackingController.Recenter();
                }

                return;
            }

            if (!_headTrackingEnabled)
                return;

            HeadTrackingStartupController.Update();

            bool moving =
                _firstPersonController != null &&
                _firstPersonController.m_Input.sqrMagnitude > float.Epsilon;

            if (Settings.returnToMouseViewKey.IsPressed())
            {
                HeadTrackingController.ReturnToMouseView();
            }
            else if (Settings.alignBodyToHeadKey.IsPressed())
            {
                HeadTrackingController.AlignBodyToHead(
                    _firstPersonController);
            }
            else if (Settings.lockViewKey.IsPressed())
            {
                if (!moving)
                    HeadTrackingController.ToggleViewLock();
            }
            else if (Settings.recenterKey.IsPressed())
            {
                HeadTrackingController.Recenter();
            }
        }


        private static void ToggleHeadTracking()
        {
            _headTrackingEnabled = !_headTrackingEnabled;

            Log(
                $"[DV_HeadTracker] Head tracking {(_headTrackingEnabled ? "ON" : "OFF")}",
                true);
        }      

        private static CharacterControllerProvider _cachedProvider;

        private static bool _wasGameLoaded;
        private static bool _gameLoadedFired;

        private static void CheckGameLoadedOnce()
        {
            if (_cachedProvider == null)
            {
                _cachedProvider = UnityEngine.Object.FindObjectOfType<CharacterControllerProvider>();
                if (_cachedProvider == null)
                    return;
            }

            bool isLoaded = _cachedProvider.IsGameLoaded;

            if (_wasGameLoaded && !isLoaded)
            {
                Log("[DV_HeadTracker] Game unloading...");

                _isUnloading = true;
                _gameLoadedFired = false;

                if (!FastTravelController.IsFastTravelling)
                {
                    Log("[DV_HeadTracker] TRUE EXIT → ResetRuntime()", true);
                    ResetRuntime();
                }
                else
                {
                    Log("[DV_HeadTracker] Fast travel → skipping ResetRuntime()", true);
                }
            }

            // LOAD
            if (!_wasGameLoaded && isLoaded)
            {
                Log("[DV_HeadTracker] Game loaded");

                _isUnloading = false;
                _gameLoadedFired = false;
            }

            if (isLoaded && !_gameLoadedFired)
            {
                _gameLoadedFired = true;

                if (!_gameLoaded)
                {
                    _gameLoaded = true;
                    OpenTrackReceiver.Start();

                    PassengerSeatViewCompatibility.TryInstall();
                }


                OnGameLoaded();
            }

            _wasGameLoaded = isLoaded;
        }

        private static void OnGameLoaded()
        {
            Log("[DV_HeadTracker] OnGameLoaded fired", true);
        }


        public static void ResetRuntime()
        {
            Log("[DV_HeadTracker] ResetRuntime called.", true);

            OpenTrackReceiver.Stop();

            HeadTrackingController.ResetSession();

            _headTrackingEnabled = true;

            _firstPersonController = null;

            _gameLoaded = false;

  
        }

        public static void Log(string message, bool force = false)
        {
#if DEBUG
    if (!force && (Settings == null || !Settings.debugLogging))
        return;
#else
            if (!force)
                return;
#endif

            Mod?.Logger?.Log(message);
        }
    }   
}
