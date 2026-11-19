# JAFDTC 1.5.1 + AH-64D (442 vWG fork) - Change Log

Build date 16-Nov-26. Adds the AH-64D Apache (BLK.II) as a JAFDTC airframe that writes DCS native DTC (.dtc) tapes.

## New

### AH-64D Apache
- **AH-64D Apache** in the airframe menu (listed last so saved airframe selections still match).
- **DTC Mission 1 / DTC Mission 2** pages: point database (W01-W50, C51-C99, T01-T50) with the DCS IDENT table,
  MGRS or lat/lon entry, 10 routes with leg distance and time, lines, areas, NFZ/PFZ zones, ADF presets, per-database
  load switches, theater selection.
- **Comm Presets** page: the 10 COM presets (unit ID, call sign, primary radio, DL net, VHF/UHF/FM1/FM2/HF).
  Presets left unset keep the base tape's values.
- **DCS DTC Tape** page (shared JAFDTC page): base tape, output file, rebuild-on-change. Ships with a clean AH-64D
  default tape (`DCS\DTC\AH-64D_BLK_II.dtc`).
- **Import .dtc** (missions and comm presets from any DCS-saved AH-64D tape), **Import / Export CSV** in the
  442CWG worksheet layouts.
- See `doc/Airframe_AH64D.md`.

### New projects
- `JAFDTC.AH64D` (net10.0 library, no WinUI): DCS theater projections for 13 theaters, MGRS, AH-64D IDENT table,
  .dtc import / merge / validation, worksheet CSV.
- `JAFDTC.AH64D.Tests`: 29 MSTest tests against DCS-saved tapes, dcs-mcp reference points and the 442CWG worksheets.

## Fixes (all airframes)
- DCS DTC Tape page: "Set output" reported "Successfully generated" even when the merge failed. It now shows why the
  tape was not written.
