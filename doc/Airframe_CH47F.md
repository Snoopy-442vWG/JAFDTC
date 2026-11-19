# JAFDTC: CH-47F Chinook (442 vWG build)

The CH-47F airframe loads **ACP waypoints**, the **flight plan** (FPLN) and **UHF-1 / VHF-1 (AN/ARC-186) radio
presets** by pressing CDU keys and, for the VHF frequency, turning the ARC-186 knobs. It covers what DCS-DTC v10 does
for the CH-47F, using the same key sequences. Same workflow as the C-130J: build the config, sit in a **powered**
jet, press **Load to Jet**.

## What gets loaded

| Page in JAFDTC | What it sets | CDU path |
|---|---|---|
| **Waypoints** | Number, position, elevation (ft), name | CLR › IDX › L6 › L6, then each point: number › L1, position › L1, elevation › L3, `/name` › L2 |
| **Route** | Which waypoints go into the flight plan | CLR › FPLN › L6 › R1 › R1 › CLR, then positions at L2-L5, later points DOWN › L5 |
| **Radios** | UHF-1 and VHF-1: up to 20 presets each (name + frequency) and the selected frequency or preset | CLR › MSN; UHF-1 freq › R2; VHF-1 mode knob and frequency knobs; presets on the radio's preset pages (L2 UHF / L3 VHF › L1), five per page: name › Ln, freq › Rn; selected preset pressed twice |

Upload order: waypoints, flight plan, UHF-1, VHF-1.

### Waypoints

- The JAFDTC waypoint **number is the ACP waypoint number** (1-200).
- Positions are entered as degrees and decimal minutes to 0.001' (about 2 m), e.g. `N4926.214E00736.018`.
- Names load as letters and digits (spaces and punctuation are dropped). A blank name leaves the ACP name alone.
- Elevation is feet MSL; blank or negative loads 0.
- Import from `.miz` (CH-47F flights), CombatFlite, Tacview, POIs, the map, or DCS capture, as for other airframes.

### Route

Same choices as the C-130J (none / all / range / list such as `1,3,5-8`). The CH-47F flight plan is entered **by
position**, so the route is independent of waypoint names; the page previews the points in order.

### Radios

- **UHF-1:** 225.000-399.975 MHz. A selected frequency is typed at MSN › R2.
- **VHF-1 (AN/ARC-186):** 30.000-151.975 MHz. A selected frequency sets the mode knob to manual and turns the four
  frequency knobs in Lua, reading the current frequency from the CDU (`V3_freq`). A selected preset sets the mode knob
  to preset.
- Preset names: letters and digits, up to 8 characters.
- **Default tuning**: frequency or preset number; blank leaves the radio alone.
- 25 kHz steps on both radios.

## Differences from DCS-DTC (deliberate)

| Situation | DCS-DTC | ESP-JAFDTC |
|---|---|---|
| ARC-186 knob that has to "wrap" (e.g. MHz digit 9 → 0) | Assumes every knob spans 3-15 and takes the "shorter" way round, which lands on the wrong digit | Turns the direct difference, correct whether or not the knobs wrap |
| CDU frequency not readable | Lua error, upload stops | Logs it and skips the VHF frequency, rest of the load continues |
| Selected preset with no presets programmed | Presses the preset key on the MSN page | Opens the radio's preset page first |
| Waypoint name of only spaces/punctuation | Enters an empty name | Leaves the name alone |

Everything else is press-for-press identical; 6 of the 13 CH-47F unit tests compare the two directly.

## In DCS

1. Use a hot CH-47F (Mission Editor: first waypoint **Takeoff from parking hot**, **Takeoff from runway**, or air
   start). A cold jet ignores the keys.
2. Pilot seat, CDU on any page, scratchpad empty, then **Load to Jet**.
3. Cockpit hotkey: DCS-DTC's trigger is the MFD **DATA** button (argument 423). JAFDTC uses the same, so pressing
   MFD DATA in the cockpit starts a load of the selected configuration.

## Known limits

- **Cockpit test status:** confirmed working in DCS on 17-Nov-2026 (hot-start CH-47F: ACP waypoints, flight plan, UHF presets and ARC-186 frequency).
- The ARC-186 knob direction and detent size (0.1 per step, 0.25 for the 25 kHz knob) are DCS-DTC's values.
- Re-loading over existing ACP waypoints behaves however the CDU handles re-entry (same as DCS-DTC).

## Credits

Key codes, sequences and the ARC-186 knob logic are ported from **DCS-DTC** by the-paid-actor and contributors
(https://github.com/the-paid-actor/dcs-dtc, GPL-3, commit ae9637d).

## Source map

| Piece | Path |
|---|---|
| Key-sequence planner | `JAFDTC.Airlift/CH47F/CH47FCdu.cs` |
| Tests | `JAFDTC.Airlift.Tests/CH47FCduTests.cs` |
| Configuration, device map, upload agent | `JAFDTC/Models/CH47F/**` |
| Editor pages | `JAFDTC/UI/CH47F/**` |
| DCS Lua | `JAFDTC/DCS/JAFDTC/CH47FFunctions.lua` |
