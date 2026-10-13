# DV_HeadTracker

**Head tracking for Derail Valley — without VR.**

DV_HeadTracker is a free, open-source Unity Mod Manager mod by **GrandpaPlayingGames**. It brings head-controlled looking to Derail Valley on a conventional monitor, using tracking data supplied by **opentrack**. Head movement and the game's normal mouse look can work together, so you can look around naturally without giving up mouse control.

> **Looking to play rather than browse the code?**  
> Download the compiled mod from **[DV_HeadTracker on Nexus Mods](https://www.nexusmods.com/derailvalley/mods/1750)**. This GitHub repository is **source code only**; it is not an installable mod package.

## Features

- Head-controlled yaw and pitch in first-person view.
- Hybrid head tracking and conventional mouse look.
- Recenter control and a configurable in-game tracking toggle.
- Configurable deadzones and mouse/head-tracking interaction.
- Compatibility with Derail Valley's right-mouse-button zoom behaviour.
- Optional centre cursor that hides after a configurable period of inactivity.
- Integration with supported views in other GrandpaPlayingGames mods, including **DV_People**, **DV_Drive**, **DV_Boats** and **DV_Replay**.

## Requirements

To use the mod in-game, you need:

- **Derail Valley** (non-VR mode).
- **Unity Mod Manager** for loading DV_HeadTracker.
- **DV_HeadTracker**, downloaded from Nexus Mods.
- **[opentrack](https://github.com/opentrack/opentrack/releases)**, installed, configured and running with a compatible tracking input.

**Both DV_HeadTracker and opentrack are required.** Installing either one alone will not enable head tracking in Derail Valley. opentrack can use a webcam or other supported tracking hardware/software; specific input setup depends on the chosen tracker.

## Installation and setup

1. Download the compiled DV_HeadTracker mod from [Nexus Mods](https://www.nexusmods.com/derailvalley/mods/1750).
2. Install the mod using Unity Mod Manager.
3. Install [opentrack](https://github.com/opentrack/opentrack/releases).
4. Configure a suitable opentrack input and the output required by DV_HeadTracker, then start tracking.
5. Launch Derail Valley, enter first-person view and follow the mod's recenter prompt.

For the full walkthrough, see the **Feature Showcase & Installation Tutorial** on the mod's [Nexus Mods page (Videos tab)](https://www.nexusmods.com/derailvalley/mods/1750?tab=videos).

## Repository contents

This repository publishes the mod's **C# source files** for reference and review. It intentionally does **not** include Visual Studio solution/project files, compiled DLLs, game assemblies, third-party dependencies or release ZIP packages.

For a ready-to-install build, use the [Nexus Mods download](https://www.nexusmods.com/derailvalley/mods/1750).

## Licence

DV_HeadTracker source code is released under the **MIT Licence**. See [LICENSE](LICENSE) for the full terms.

Copyright © 2026 GrandpaPlayingGames.

## Third-party software

[opentrack](https://github.com/opentrack/opentrack) is a separate open-source application, developed and licensed independently of DV_HeadTracker. It is not developed, maintained or distributed by GrandpaPlayingGames. Consult the opentrack project for its downloads and licensing terms.

## Links

- [DV_HeadTracker on Nexus Mods — downloads, instructions and videos](https://www.nexusmods.com/derailvalley/mods/1750)
- [DV_HeadTracker source repository](https://github.com/GrandpaPlayingGames/DV_HeadTracker)
- [opentrack releases](https://github.com/opentrack/opentrack/releases)
