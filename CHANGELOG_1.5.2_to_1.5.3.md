# JAFDTC 1.5.3 (442 vWG fork) - Change Log since 1.5.2

Airframes: AH-64D, C-130J, CH-47F (new). Build date 18-Nov-26. Built on 1.5.2 (c4a389e); A-10C and other airframes unchanged.
Fork: github.com/Snoopy-442vWG/JAFDTC

## New

### AH-64D Apache - DCS native DTC (.dtc)
- New airframe **AH-64D Apache**. JAFDTC writes the DCS Data Transfer Cartridge file the Apache loads from the DTC interface
  (Comms menu `\` > F8 Ground Crew > DTC); no cockpit key presses.
- **Mission 1 and Mission 2:** points (waypoints/hazards, control measures, targets/threats), routes (up to 10), lines,
  areas and zones (NFZ/PFZ), ADF presets.
- **Comm presets:** the 10 COMM presets (UHF, VHF, FM1, FM2, HF and initial mode).
- **Import:** existing `.dtc` files and the CSV worksheet; **export** CSV. Copy one mission into the other.
- Positions are converted with the DCS theater projections (all current maps) and MGRS; IDENT codes follow the Apache DTU table.
- **Load to Jet** writes `Saved Games\DCS\DTC\<configuration name>.dtc` (or the output path set on the DTC page) and messages
  the cockpit. The Apache cockpit is recognised for the pilot-in-pit check.

### C-130J Hercules - cockpit upload (CNI-MU)
- New airframe **C-130J Hercules**, loaded with key presses like the A-10C.
- **Waypoints:** name, position, elevation. Loaded into the CNI waypoint database as 8-digit MGRS. Names may contain spaces and
  punctuation; the CNI identifier is the letters and digits, 6 characters, made unique if two names collide (OBJECT, OBJEC2).
- **Route:** new page. No route / all waypoints / range From-To / list ("1,3,5-8"); builds the LEGS page. Previews the legs.
- **Radios:** UHF-1 (225.000-399.975) and VHF-1 (108.000-173.975), 20 presets each (name 5 characters + frequency), plus the
  selected frequency or preset.
- Waypoint import from .miz (C-130J-30 flights), CombatFlite, Tacview, POIs, the map, and DCS capture.

### CH-47F Chinook - cockpit upload (CDU)
- New airframe **CH-47F Chinook**, loaded with key presses.
- **Waypoints:** the JAFDTC number is the ACP waypoint number; position as degrees/decimal minutes, elevation, name.
- **Route:** same choices as the C-130J; builds the CDU flight plan (FPLN) by position.
- **Radios:** UHF-1 (225.000-399.975) and VHF-1 / AN/ARC-186 (30.000-151.975), 20 presets each (name 8 characters +
  frequency); the selected VHF frequency is set by turning the ARC-186 knobs.
- Upload hotkey: the MFD **DATA** button (same as DCS-DTC).

### Miscellaneous
- **Portable build:** `dotnet publish -p:WindowsPackageType=None` builds a version that runs without installing (the store manifest is
  dropped only for that build; the packaged build is unchanged).
- **JAFDTC Installer** (Visual Studio) project updated to 1.5.3 (new product and package codes, same upgrade code and install folder).
- Airframe guides: `doc/Airframe_AH64D.md`, `doc/Airframe_C130J.md`, `doc/Airframe_CH47F.md`.
- Lua: `C130JFunctions.lua`, `CH47FFunctions.lua`; `CommonFunctions.lua` recognises AH-64D_BLK_II, C-130J-30 and CH-47Fbl1.
- Waypoint editor: an airframe can count only letters and digits toward the name-length warning (used by the C-130J only).

## Tests
- 63 unit tests: AH-64D DTC conversion (29); C-130J and CH-47F key sequences (34), checked press for press against DCS-DTC.
- All three new airframes confirmed in DCS on 17-Nov-26 (hot-start cockpits), and re-tested together with the 1.5.2 A-10C
  features (Battle Book, named profiles, GBU-39, Mode 3/A) on the merged 1.5.3 build on 18/19-Nov-26.

## Known limits
- **AH-64D:** no IDM (datalink) editing; no map window for Apache points; line type is copied as-is from imported files.
- **AH-64D, DCS quirk:** a tape loaded in-game (`\` > Ground Crew > DTC) does not send waypoints, control measures or
  targets until each of those tabs' **Do not upload tab data** box is ticked and unticked once in the DTC window. DCS's own
  saved tapes behave the same; see `doc/Airframe_AH64D.md`.
- **C-130J / CH-47F:** need a powered cockpit (hot start); a cold jet ignores the keys. TOS and "Is Target" are not loaded
  (DCS-DTC does not load them either). Re-entering an existing waypoint follows whatever the CNI/CDU does on re-entry.
- **CH-47F:** pressing MFD DATA in the cockpit starts an upload of the selected configuration.

## Credits
C-130J and CH-47F key codes and sequences are ported from DCS-DTC (the-paid-actor and contributors, GPL-3, commit ae9637d),
credited in the file headers. New code is GPL-3 like JAFDTC.