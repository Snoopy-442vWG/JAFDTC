// ********************************************************************************************************************
//
// CH47FCdu.cs : CH-47F CDU key sequence planner (ACP waypoints, FPLN route, radio presets)
//
// Copyright(C) 2026 442 vWG
//
// The key sequences are ported from DCS-DTC (https://github.com/the-paid-actor/dcs-dtc, GPL-3,
// New/Uploader/Aircrafts/CH47F/Systems/Waypoints.cs and Radios.cs, commit ae9637d), with credit to its authors.
//
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU General
// Public License as published by the Free Software Foundation, either version 3 of the License, or (at your
// option) any later version.
//
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the
// implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General Public License
// for more details.
//
// You should have received a copy of the GNU General Public License along with this program.  If not, see
// <https://www.gnu.org/licenses/>.
//
// ********************************************************************************************************************

using JAFDTC.Airlift.C130J;
using JAFDTC.Airlift.Cni;
using System.Globalization;

namespace JAFDTC.Airlift.CH47F
{
    /// <summary>
    /// the two radios the CH-47F upload programs from the CDU MSN page.
    /// </summary>
    public enum CH47FRadio
    {
        UHF1 = 0,           // MSN LSK L2 presets, LSK R2 frequency, 225.000 - 399.975 MHz
        VHF1 = 1            // MSN LSK L3 presets, AN/ARC-186 knobs for frequency, 30.000 - 151.975 MHz
    }

    /// <summary>
    /// plans the CH-47F CDU key presses that load ACP waypoints, build the flight plan (FPLN) and program radio
    /// presets. every method is pure: it returns a step list and touches nothing.
    /// </summary>
    public static class CH47FCdu
    {
        public const int MaxWaypointNumber = 200;
        public const int MaxPresets = 20;
        public const int PresetsPerPage = 5;            // MSN preset rows per page (LSK 1-5)
        public const int MaxPresetNameLength = 8;
        public const int FplnRowsOnFirstPage = 4;       // FPLN entries at L2-L5, then DOWN and L5
        public const int IdxPostWaitMs = 75;            // dcs-dtc: IDX post delay 0.5 x 150 ms
        public const int SelectPresetRepeats = 2;       // dcs-dtc: "DCS bug, it doesn't set on the first attempt"

        public const double UhfMin = 225.000, UhfMax = 399.975;
        public const double VhfMin = 30.000, VhfMax = 151.975;

        public const string FnSetV3 = "SetV3";          // lua: turn the arc-186 knobs to a frequency

        // ---- coordinates

        /// <summary>
        /// one axis as hemisphere + degrees + minutes to thousandths with the decimal point, e.g. "N4926.214" or
        /// "E00736.018" (the CDU entry form; dcs-dtc builds the same text from its DDM strings).
        /// </summary>
        public static string Ddm(double value, bool isLat)
        {
            char hemi = isLat ? ((value < 0) ? 'S' : 'N') : ((value < 0) ? 'W' : 'E');
            long thousandths = (long)Math.Round(Math.Abs(value) * 60000.0, MidpointRounding.AwayFromZero);
            long deg = thousandths / 60000;
            long min = thousandths % 60000;
            string degText = deg.ToString(isLat ? "00" : "000", CultureInfo.InvariantCulture);
            return $"{hemi}{degText}{min / 1000:00}.{min % 1000:000}";
        }

        /// <summary>
        /// the full CDU position entry: latitude then longitude, e.g. "N4926.214E00736.018".
        /// </summary>
        public static string Position(double lat, double lon) => Ddm(lat, true) + Ddm(lon, false);

        public static bool IsFrequencyValid(CH47FRadio radio, string freq)
        {
            string f3 = C130JCni.FormatFrequency(freq);
            if (f3 == null)
                return false;
            double f = double.Parse(f3, CultureInfo.InvariantCulture);
            bool inBand = (radio == CH47FRadio.UHF1) ? (f >= UhfMin && f <= UhfMax) : (f >= VhfMin && f <= VhfMax);
            long khz = (long)Math.Round(f * 1000.0);
            return inBand && (khz % 25 == 0);
        }

        /// <summary>
        /// the ACP waypoint name: letters and digits as typed (dcs-dtc types the whole name).
        /// </summary>
        public static string WaypointName(string name) => C130JCni.AlphaNum(name);

        public static string PresetName(string name)
        {
            string s = C130JCni.AlphaNum(name);
            return (s.Length > MaxPresetNameLength) ? s[..MaxPresetNameLength] : s;
        }

        // ---- waypoints

        /// <summary>
        /// load each waypoint into the ACP list: CLR > IDX > L6 > L6, then per point: number > L1, position > L1,
        /// elevation > L3, and when named: "/" name > L2.
        /// </summary>
        public static List<CniStep> PlanWaypoints(IReadOnlyList<CniWaypoint> wypts)
        {
            List<CniStep> steps = [ ];
            if ((wypts == null) || (wypts.Count == 0))
                return steps;

            steps.Add(CniStep.Note("CH47F waypoints"));
            steps.Add(CniStep.Press(CniKeys.CLR));
            steps.Add(CniStep.Press(CniKeys.IDX));
            steps.Add(CniStep.Wait(IdxPostWaitMs));
            steps.Add(CniStep.Press(CniKeys.LskL(6)));
            steps.Add(CniStep.Press(CniKeys.LskL(6)));

            foreach (CniWaypoint wypt in wypts)
            {
                steps.AddRange(C130JCni.Type(wypt.Number.ToString(CultureInfo.InvariantCulture)));
                steps.Add(CniStep.Press(CniKeys.LskL(1)));

                steps.AddRange(C130JCni.Type(Position(wypt.Lat, wypt.Lon)));
                steps.Add(CniStep.Press(CniKeys.LskL(1)));

                steps.AddRange(C130JCni.Type(Math.Max(0, wypt.ElevationFt).ToString(CultureInfo.InvariantCulture)));
                steps.Add(CniStep.Press(CniKeys.LskL(3)));

                string name = WaypointName(wypt.Name);
                if (name.Length > 0)
                {
                    steps.Add(CniStep.Press(CniKeys.SLASH));
                    steps.AddRange(C130JCni.Type(name));
                    steps.Add(CniStep.Press(CniKeys.LskL(2)));
                }
            }
            return steps;
        }

        // ---- route

        /// <summary>
        /// build the flight plan by position: CLR > FPLN > L6 > R1 > R1 > CLR, then the first four points at
        /// L2-L5, each later point after DOWN at L5.
        /// </summary>
        public static List<CniStep> PlanRoute(IReadOnlyList<CniWaypoint> wypts, CniRouteSpec spec)
        {
            List<CniStep> steps = [ ];
            List<CniWaypoint> route = C130JCni.RouteWaypoints(wypts, spec);
            if (route.Count == 0)
                return steps;

            steps.Add(CniStep.Note("CH47F route"));
            steps.Add(CniStep.Press(CniKeys.CLR));
            steps.Add(CniStep.Press(CniKeys.FPLN));
            steps.Add(CniStep.Press(CniKeys.LskL(6)));
            steps.Add(CniStep.Press(CniKeys.LskR(1)));
            steps.Add(CniStep.Press(CniKeys.LskR(1)));
            steps.Add(CniStep.Press(CniKeys.CLR));

            int submitted = 0;
            foreach (CniWaypoint wypt in route)
            {
                int slot = 5;
                if (submitted < FplnRowsOnFirstPage)
                    slot = 2 + submitted;
                else
                    steps.Add(CniStep.Press(CniKeys.DOWN));
                steps.AddRange(C130JCni.Type(Position(wypt.Lat, wypt.Lon)));
                steps.Add(CniStep.Press(CniKeys.LskL(slot)));
                submitted++;
            }
            return steps;
        }

        // ---- radios

        /// <summary>
        /// program one radio from the MSN page: UHF-1 frequency at R2; VHF-1 mode knob (manual/preset) and, for a
        /// frequency, the arc-186 knobs; presets on the radio's preset pages (name > Ln, frequency > Rn, five per
        /// page); a selected preset is pressed twice (dcs-dtc works around a dcs bug that way).
        /// </summary>
        public static List<CniStep> PlanRadio(CH47FRadio radio, CniRadioSpec spec)
        {
            List<CniStep> steps = [ ];
            if ((spec == null) || spec.IsEmpty)
                return steps;

            bool isUhf = (radio == CH47FRadio.UHF1);
            string selFreq = spec.IsFrequencySelection ? C130JCni.FormatFrequency(spec.Selection) : null;
            int selPreset = spec.IsPresetSelection ? int.Parse(spec.Selection.Trim(), CultureInfo.InvariantCulture) : 0;
            if (selPreset > MaxPresets)
                selPreset = 0;

            steps.Add(CniStep.Note($"CH47F radio {radio}"));
            steps.Add(CniStep.Press(CniKeys.CLR));
            steps.Add(CniStep.Press(CniKeys.MSN));

            if (isUhf && (selFreq != null))
            {
                steps.AddRange(C130JCni.Type(selFreq));
                steps.Add(CniStep.Press(CniKeys.LskR(2)));
            }

            steps.Add(CniStep.Press(CniKeys.CLR));

            if (!isUhf && (selFreq != null))
                steps.Add(CniStep.Press(CniKeys.ARC186_MODE_MANUAL));
            if (!isUhf && (selPreset > 0))
                steps.Add(CniStep.Press(CniKeys.ARC186_MODE_PRESET));
            if (!isUhf && (selFreq != null))
                steps.Add(CniStep.Exec(FnSetV3, selFreq));

            List<CniPreset> presets = [.. (spec.Presets ?? [ ]).Where(p => (p.Number >= 1) && (p.Number <= MaxPresets) &&
                                                                         (C130JCni.FormatFrequency(p.Frequency) != null))
                                                             .OrderBy(p => p.Number) ];

            int inPage = 1;
            // dcs-dtc opens the preset pages only when there are presets; we also open them for a preset
            // selection so the selection press never lands on the MSN page.
            if ((presets.Count > 0) || (selPreset > 0))
            {
                steps.Add(CniStep.Press(isUhf ? CniKeys.LskL(2) : CniKeys.LskL(3)));
                steps.Add(CniStep.Press(CniKeys.LskL(1)));
            }

            foreach (CniPreset preset in presets)
            {
                int pageNo = (preset.Number + PresetsPerPage - 1) / PresetsPerPage;
                int btn = preset.Number - ((pageNo - 1) * PresetsPerPage);
                while (inPage < pageNo)
                {
                    steps.Add(CniStep.Press(CniKeys.DOWN));
                    inPage++;
                }
                string name = PresetName(preset.Name);
                if (name.Length > 0)
                {
                    steps.AddRange(C130JCni.Type(name));
                    steps.Add(CniStep.Press(CniKeys.LskL(btn)));
                }
                steps.AddRange(C130JCni.Type(C130JCni.FormatFrequency(preset.Frequency)));
                steps.Add(CniStep.Press(CniKeys.LskR(btn)));
            }

            if (selPreset > 0)
            {
                int pageNo = (selPreset + PresetsPerPage - 1) / PresetsPerPage;
                int btn = selPreset - ((pageNo - 1) * PresetsPerPage);
                for (int i = 0; i < SelectPresetRepeats; i++)
                {
                    while (inPage < pageNo)
                    {
                        steps.Add(CniStep.Press(CniKeys.DOWN));
                        inPage++;
                    }
                    while (inPage > pageNo)
                    {
                        steps.Add(CniStep.Press(CniKeys.UP));
                        inPage--;
                    }
                    steps.Add(CniStep.Press(CniKeys.LskL(btn)));
                    inPage = 1;
                }
            }
            return steps;
        }
    }
}
