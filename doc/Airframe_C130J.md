# JAFDTC: C-130J Hercules (442 vWG build)

The C-130J airframe loads **waypoints**, the **route** (LEGS page) and **UHF-1 / VHF-1 radio presets** into the
CNI-MU by pressing cockpit keys, the same way JAFDTC loads the A-10C, F-16C, F-15E and F/A-18C. It covers what
DCS-DTC v10 does for the C-130J, using the same key sequences.

## What gets loaded

| Page in JAFDTC | What it sets | CNI-MU path |
|---|---|---|
| **Waypoints** | Name, position, elevation (ft) | INDEX › NEXT PAGE › LSK R2, then each point: name › L1, MGRS › L2, elevation › L3, R6 |
| **Route** | Which waypoints become the route | MARK › L1 › LEGS › R6 › L1, last point › R1, NEXT PAGE, points on R1-R4 (paging every 4), L6 › R6 |
| **Radios** | UHF-1 and VHF-1: up to 20 presets each (name + frequency) and the selected frequency or preset | CLR › COMM TUNE › L1 (UHF-1) or L3 (VHF-1), back to page 1, freq › L1, presets `NAME/freq` on L2-L5 of each page, preset › L1 |

Upload order: waypoints, route, UHF-1, VHF-1.

### Waypoints

- Positions are edited in degrees/decimal minutes (like the A-10C) and loaded as **8-digit MGRS** (10 m precision).
- The CNI identifier is the waypoint **name**, letters and digits only, **six characters max**. Spaces and
  punctuation are allowed in JAFDTC and dropped on load (`LZ EAGLE` loads as `LZEAGL`); the name field shows the warning style only when the letters and digits run past six. A blank name loads
  as `WP<number>`. If two names would load the same identifier, the second becomes e.g. `OBJEC2`, so the route
  never points at the wrong waypoint.
- Elevation is feet MSL; blank loads 0, below sea level loads 0 (the CNI keypad has no minus key).
- Import works the same as the other airframes: from a `.miz` (C-130J-30 flights), CombatFlite, Tacview, the POI
  database, the map window, or DCS waypoint capture.
- TOS and "Is Target" from DCS-DTC are **not** loaded: DCS-DTC stores them but never sends them to the jet either.

### Route

| Choice | Result |
|---|---|
| No route (default) | Waypoints go into the database only; LEGS is untouched |
| All waypoints | Every waypoint, in list order |
| Range of waypoints | Waypoints *From*..*To* by number (either order) |
| Specific waypoints | Your list, in your order, e.g. `1,3,5-8` or `9-6` |

The page previews the identifiers that will go onto LEGS.

### Radios

- UHF-1: 225.000-399.975 MHz. VHF-1: 108.000-173.975 MHz. 25 kHz steps.
- Preset **description = preset name**: letters and digits, 5 characters; shorter names are padded with `0`
  (`ARCO` loads as `ARCO0`) because the field is fixed width (DCS-DTC does the same).
- **Default tuning**: a frequency (`251.000`) tunes that frequency; a preset number (`3`) selects that preset;
  blank leaves the radio alone.

## In DCS

1. Install/refresh the JAFDTC Lua (JAFDTC does this at start-up; the 442 build also ships
   `Scripts\JAFDTC\C130JFunctions.lua`). JAFDTC recognises the `C-130J-30` cockpit as **C130J**.
2. Sit in the C-130J pilot seat with the **CNI-MU powered** and on any page, scratchpad empty. A cold-and-dark
   jet ignores every key (dcs.log shows `C130J (Main): Ignoring input command … because no power`). The easy way
   is a hot start: in the Mission Editor set the C-130J's first waypoint type to **Takeoff from parking hot** (or
   **Takeoff from runway**, or start it in the air).
3. In JAFDTC select the configuration and press **Load to Jet**. Hands off the CNI until the marker clears.
4. Check INDEX › waypoint list, LEGS, and COMM TUNE.

A full load of 20 waypoints, a 20-leg route and 40 presets is roughly 1,300 key presses: expect a few minutes at the
default 150 ms per key.

## Known limits (honest list)

- **Cockpit test status:** confirmed working in DCS on 17-Nov-2026 (hot-start C-130J: waypoints, route and radios all loaded).
  them press-for-press) but the first real test is yours.
- Existing database waypoints with the same identifier: behaviour is whatever the CNI does on re-entry (same as
  DCS-DTC).
- The "page back to COMM TUNE page 1" step reads the CNI's `n/10` page counter; if DCS changes that display the
  loop stops safely after 12 presses or immediately if no counter is found.
- No cockpit hotkey for upload yet beyond DCS-DTC's argument 1166 hook; use **Load to Jet** in JAFDTC.

## Credits

Key codes and sequences ported from **DCS-DTC** by the-paid-actor and contributors
(https://github.com/the-paid-actor/dcs-dtc, GPL-3, commit ae9637d). JAFDTC is GPL-3; this port keeps that licence.

## Source map

| Piece | Path |
|---|---|
| Key-sequence planner (pure, tested) | `JAFDTC.Airlift/C130J/C130JCni.cs`, `JAFDTC.Airlift/Cni/*` |
| Tests (21, incl. press-for-press DCS-DTC equivalence) | `JAFDTC.Airlift.Tests/C130JCniTests.cs` |
| Configuration and systems | `JAFDTC/Models/C130J/**` |
| CNI-MU device map, upload agent | `JAFDTC/Models/C130J/C130JDeviceManager.cs`, `C130JUploadAgent.cs` |
| Editor pages | `JAFDTC/UI/C130J/**` |
| DCS Lua | `JAFDTC/DCS/JAFDTC/C130JFunctions.lua` (+ `CommonFunctions.lua`, `JAFDTC.lua`) |
