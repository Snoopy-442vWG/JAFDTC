# JAFDTC: Configuring AH-64D Apache Airframes

*442 vWG fork. Added 16-Nov-2026.*

JAFDTC builds AH-64D (Block II) setups as **DCS native Data Transfer Cartridge (.dtc) files**. It does not press
cockpit buttons for the Apache; you load the tape with the DCS DTC Manager or the in-sim DTC interface, the same way
as a tape saved by DCS.

JAFDTC edits these parts of the tape:

| System | What it sets | .dtc section |
|---|---|---|
| **DTC Mission 1** | Point database (W01-W50, C51-C99, T01-T50), 10 routes, 15 lines, 12 areas, 16 NFZ/PFZ zones, 10 ADF presets | `data.NAV.Mission_1` |
| **DTC Mission 2** | Same as Mission 1, for a follow-on or contingency mission | `data.NAV.Mission_2` |
| **Comm Presets** | The 10 COM presets: unit ID, call sign, primary radio, DL net, VHF/UHF/FM1/FM2/HF | `data.Presets.COMM` |
| **DCS DTC Tape** | Base tape (template) and the output .dtc file | — |

Everything else on the tape comes from the **base tape** unchanged: ASE/CMDS, RFI/RLWR threat lists, weapons,
laser codes, IFF, initial radio settings, MPS presets and IDM. To change those, set them in the DCS DTC Manager,
save that tape, and import it as the base tape on the DCS DTC Tape page.

## Quick start

1. On the configuration list, pick **AH-64D Apache** in the airframe menu and **Add** a configuration.
2. Open **DTC Mission 1**:
   * Choose the **DCS Theater** at the top. The tape stores DCS map x/y, so the theater must match your mission's map.
   * Add points by hand, or use **Import CSV** with your worksheets (format below), or **Import .dtc** to pull a
     mission out of a tape DCS saved.
3. Open **Comm Presets** and set the presets, or import the radio worksheet / a .dtc.
4. Open **DCS DTC Tape**:
   * Optional: **add a base tape** (a .dtc saved by the DCS DTC Manager with your ASE, weapons and laser settings).
     With no base tape, JAFDTC uses its built-in AH-64D default tape.
   * Tick the systems to merge (all three are ticked the first time you open the page).
   * **Set the output file**. Save it in `Saved Games\DCS\DTC` so DCS lists it. JAFDTC writes the tape right away.
   * Tick **Rebuild** to rewrite the tape automatically whenever you leave a Mission or Comm page.
5. In DCS: Mission Editor **DTC Manager** (or the in-sim DTC interface) → open the tape → load it to the group.

## DTC Mission pages

**Points.** The list shows every point. The editor on the right adds or replaces one point:

* **Database**: W/H (waypoints and hazards, 1-50), C (control measures, 51-99) or T (targets/threats, 1-50).
* **IDENT**: the TSD symbol. The list is DCS's own `Point_ID` table from the AH-64D module, filtered to the
  database. SAM targets use their number (`6` = SA-6).
* **Free Text**: up to 3 letters/digits.
* **MGRS** or **Lat, Lon**: either one; the other fills in. MGRS takes 2-10 digits (8 matches the KU).
* **Elevation**: feet MSL. The tape stores meters.
* **Load to aircraft**: sets whether each database loads from this tape. DCS also fills C51-C99 with mission units
  that are not hidden on the MFD, so routes built on W points are the safer choice.

Saving a point onto a number that is already used replaces that point. Deleting a point also removes it from routes.

**Routes.** Pick one of the 10 routes, name it (up to 5 characters), set the start time, and add W or C points.
Leg distance and time are worked out in the theater's DCS map projection, the same grid DCS measures on.
The tape stores them for the DTC Manager.

**Lines.** 2-4 vertices, one per line (MGRS or `lat, lon`). The **Type** is DCS's `type_num`. Tapes saved by DCS
use 1 and 3; check the result in the DTC Manager when using other values.

**Areas & Zones.** Areas, NFZs and PFZs need 3 or more vertices. Shapes imported from a DCS tape keep DCS's caption
positions; new or edited shapes get captions laid out the way the DTC Manager lays out a new zone.

**ADF.** 10 presets (3-letter ID, 100-2199.5 kHz) and the load switch.

If anything on a mission would stop the tape from being written, a yellow bar at the bottom of the page lists it.

## CSV worksheets

Column names are matched without regard to case, spaces or underscores. Column order does not matter. Position
comes from `Latitude`/`Longitude` when both have 5 or more decimals, otherwise from `MGRS`.

| Worksheet | Columns |
|---|---|
| Points | `TSD CP, IDENT, FREE Text, Name, MGRS, Elev_ft, Latitude, Longitude` |
| Route | `Route, #, Waypoint, TSD CP, IDENT, FREE Text, MGRS, Alt, Latitude, Longitude, Elev_ft` and optional `Speed` (kt, default 80) |
| Radios | `Preset, UnitID, CallSign, Primary, DLNet, VHF, UHF, FM1, FM2` and optional `HF` |

These are the same layouts as the 442CWG AH-64D DTC worksheets (`IQT-4_DAY_control_measures.csv`,
`IQT-4_DAY_route.csv`, `AH-64D_radio_presets.csv`, ...). **Export CSV** writes the points and routes back out with
8-digit MGRS.

## What is checked, and how

* **Coordinates.** Every DCS theater is a transverse Mercator with its own origin. The parameters were solved from
  points placed with dcs-mcp (Eagle Dynamics' terrain data) for 13 theaters: residual 0.000 m. They match
  pydcs for the 8 theaters pydcs publishes. Tests check 44 reference points to 1 cm.
* **MGRS.** Tested against the GeoTrans-based `mgrs` package, including the Norway and Svalbard zone exceptions and
  the southern hemisphere.
* **Tape format.** Three tapes saved by DCS (TTI Hannover, Orontes Gate, IQT-4 day) are imported and merged back
  into themselves. Points, routes, lines, areas, zones, ADF and comm presets come back the same to 0.1 mm, and every
  other section is unchanged.
* `JAFDTC.AH64D.Tests` has 29 tests. Run `dotnet test` in that folder.

## Flying it (checked in DCS 17-Nov-2026)

* JAFDTC recognizes the Apache cockpit ("Pilot in Pit"). This needs the updated `CommonFunctions.lua` in
  `Saved Games\DCS\Scripts\JAFDTC`.
* **Load to Jet** writes the tape (by default to `Saved Games\DCS\DTC\<configuration name>.dtc`). Then load it in DCS
  from the DTC interface and run MASTER LOAD on the DTU page.
* **Control measures that seem missing are usually just hidden.** In NAV phase the TSD hides FRIENDLY UNITS by
  default (TSD > SHOW > COORD SHOW), so C points with friendly-unit IDENTs such as FU and HO don't draw. Check
  TSD > POINT > CTRLM, turn FRIENDLY UNITS on (or use ATK phase), or use CP / AP IDENTs for route-related points.

## Limits

* No cockpit upload: "Load to Jet" stays off for AH-64D configurations. Load the tape through DCS.
* **IDM / datalink** members and networks are not edited. No DCS-saved tape seen so far has IDM data to copy the
  format from, so JAFDTC keeps the base tape's IDM section. The **DL** box on a comm preset is supported.
* Routes can use W and C points. T points cannot be added to routes.
* The JAFDTC map window is not hooked up for the Apache yet.
* DCS loads tapes, but the Apache's own handling is DCS's. Fly the first tape and check the TSD.
