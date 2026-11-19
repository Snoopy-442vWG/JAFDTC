// ********************************************************************************************************************
//
// C130JCni.cs : C-130J CNI-MU key sequence planner (waypoints, route, radio presets)
//
// Copyright(C) 2026 442 vWG
//
// The key sequences are ported from DCS-DTC (https://github.com/the-paid-actor/dcs-dtc, GPL-3,
// New/Uploader/Aircrafts/C130/Systems/Waypoints.cs and Radios.cs, commit ae9637d), with credit to its authors.
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

using JAFDTC.AH64D.Geo;
using JAFDTC.Airlift.Cni;
using System.Globalization;
using System.Text;

namespace JAFDTC.Airlift.C130J
{
    /// <summary>
    /// the two radios the C-130J upload programs, and the COMM TUNE line select key that opens each.
    /// </summary>
    public enum C130JRadio
    {
        UHF1 = 0,           // COMM TUNE LSK L1, 225.000 - 399.975 MHz
        VHF1 = 1            // COMM TUNE LSK L3, 108.000 - 173.975 MHz
    }

    /// <summary>
    /// plans the C-130J CNI-MU key presses that load waypoints into the database, build the route on the LEGS page
    /// and program radio presets. every method is pure: it returns a step list and touches nothing.
    /// </summary>
    public static class C130JCni
    {
        // ---- limits

        public const int MaxIdentLength = 6;            // waypoint identifier on the CNI
        public const int MaxPresetNameLength = 5;       // preset name field
        public const int MaxPresets = 20;               // presets per radio
        public const int PresetsPerPage = 4;            // COMM TUNE preset rows per page (LSK L2-L5)
        public const int RouteRowsPerPage = 4;          // LEGS rows per page (LSK R1-R4)
        public const int MaxWaypoints = 200;
        public const int RadioPageWaitMs = 200;

        public const double UhfMin = 225.000, UhfMax = 399.975;
        public const double VhfMin = 108.000, VhfMax = 173.975;

        // ---- text helpers

        /// <summary>
        /// key presses to type the text into the scratchpad. characters without a key (spaces, dashes, ...) are
        /// skipped, letters are upper-cased.
        /// </summary>
        public static IEnumerable<CniStep> Type(string text)
        {
            foreach (char c in (text ?? "").ToUpperInvariant())
                if (CniKeys.IsTypeable(c))
                    yield return CniStep.Press(c.ToString());
        }

        /// <summary>
        /// upper-cased letters and digits only.
        /// </summary>
        public static string AlphaNum(string text)
        {
            StringBuilder sb = new();
            foreach (char c in (text ?? "").ToUpperInvariant())
                if ((c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9'))
                    sb.Append(c);
            return sb.ToString();
        }

        /// <summary>
        /// the CNI identifier for a waypoint: its name as letters and digits, at most six characters, or "WP" plus
        /// the number when the name has nothing usable.
        /// </summary>
        public static string Ident(string name, int number)
        {
            string id = AlphaNum(name);
            if (id.Length == 0)
                id = $"WP{number}";
            return (id.Length > MaxIdentLength) ? id[..MaxIdentLength] : id;
        }

        /// <summary>
        /// identifiers for the whole list, made unique: a repeat keeps as much of its name as fits and ends in a
        /// counter ("OBJECT", "OBJEC2", "OBJEC3"). the route refers to waypoints by identifier, so two waypoints
        /// must never share one.
        /// </summary>
        public static Dictionary<int, string> Idents(IReadOnlyList<CniWaypoint> wypts)
        {
            Dictionary<int, string> map = [ ];
            HashSet<string> used = [ ];
            foreach (CniWaypoint wypt in wypts)
            {
                string id = Ident(wypt.Name, wypt.Number);
                for (int n = 2; used.Contains(id); n++)
                {
                    string suffix = n.ToString(CultureInfo.InvariantCulture);
                    string stem = Ident(wypt.Name, wypt.Number);
                    stem = stem[..Math.Min(stem.Length, MaxIdentLength - suffix.Length)];
                    id = stem + suffix;
                }
                used.Add(id);
                map[wypt.Number] = id;
            }
            return map;
        }

        /// <summary>
        /// 8-digit mgrs for the position with the spaces removed, e.g. "32UMV12345678".
        /// </summary>
        public static string Mgrs8(double lat, double lon) => Mgrs.FromLatLon(lat, lon, 8).Replace(" ", "");

        /// <summary>
        /// frequency text as the CNI expects it: three decimals, invariant culture. returns null if not a number.
        /// </summary>
        public static string FormatFrequency(string freq)
        {
            if (!double.TryParse((freq ?? "").Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double f))
                return null;
            return f.ToString("F3", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// the preset name field: letters and digits, five characters, padded with "0" when shorter (DCS-DTC
        /// behavior; the field is fixed width).
        /// </summary>
        public static string PresetName(string name)
        {
            string s = AlphaNum(name);
            if (s.Length == 0)
                return "";
            s = (s.Length > MaxPresetNameLength) ? s[..MaxPresetNameLength] : s;
            return s.PadRight(MaxPresetNameLength, '0');
        }

        public static bool IsFrequencyValid(C130JRadio radio, string freq)
        {
            string f3 = FormatFrequency(freq);
            if (f3 == null)
                return false;
            double f = double.Parse(f3, CultureInfo.InvariantCulture);
            bool inBand = (radio == C130JRadio.UHF1) ? (f >= UhfMin && f <= UhfMax) : (f >= VhfMin && f <= VhfMax);
            long khz = (long)Math.Round(f * 1000.0);
            return inBand && (khz % 25 == 0);
        }

        // ---- waypoints

        /// <summary>
        /// load each waypoint into the CNI database: INDEX > NEXT PAGE > (R2) waypoint page, then for each point
        /// type ident > L1, mgrs > L2, elevation > L3, R6.
        /// </summary>
        public static List<CniStep> PlanWaypoints(IReadOnlyList<CniWaypoint> wypts)
        {
            List<CniStep> steps = [ ];
            if ((wypts == null) || (wypts.Count == 0))
                return steps;

            Dictionary<int, string> idents = Idents(wypts);

            steps.Add(CniStep.Note("C130J waypoints"));
            steps.Add(CniStep.Press(CniKeys.CLR));
            steps.Add(CniStep.Press(CniKeys.INDEX));
            steps.Add(CniStep.Press(CniKeys.NEXT));
            steps.Add(CniStep.Press(CniKeys.LskR(2)));

            foreach (CniWaypoint wypt in wypts)
            {
                steps.AddRange(Type(idents[wypt.Number]));
                steps.Add(CniStep.Press(CniKeys.LskL(1)));

                steps.AddRange(Type(Mgrs8(wypt.Lat, wypt.Lon)));
                steps.Add(CniStep.Press(CniKeys.LskL(2)));

                steps.AddRange(Type(Math.Max(0, wypt.ElevationFt).ToString(CultureInfo.InvariantCulture)));
                steps.Add(CniStep.Press(CniKeys.LskL(3)));

                steps.Add(CniStep.Press(CniKeys.LskR(6)));
            }
            return steps;
        }

        // ---- route

        /// <summary>
        /// the waypoints that make up the route, in route order.
        /// </summary>
        public static List<CniWaypoint> RouteWaypoints(IReadOnlyList<CniWaypoint> wypts, CniRouteSpec spec)
        {
            if ((wypts == null) || (spec == null) || (spec.Mode == CniRouteMode.None))
                return [ ];

            List<CniWaypoint> ordered = [.. wypts.OrderBy(w => w.Number) ];
            switch (spec.Mode)
            {
                case CniRouteMode.Range:
                    int from = Math.Min(spec.From, spec.To);
                    int to = Math.Max(spec.From, spec.To);
                    return [.. ordered.Where(w => (w.Number >= from) && (w.Number <= to)) ];
                case CniRouteMode.List:
                    List<CniWaypoint> list = [ ];
                    foreach (int num in spec.List ?? [ ])
                    {
                        CniWaypoint wypt = ordered.FirstOrDefault(w => w.Number == num);
                        if (wypt != null)
                            list.Add(wypt);
                    }
                    return list;
                default:
                    return ordered;
            }
        }

        /// <summary>
        /// build the route on the LEGS page: MARK > L1 > LEGS > R6 > L1, enter the last waypoint at R1, NEXT PAGE,
        /// then enter each route waypoint at R1-R4, paging every four, and finish with L6 > R6.
        /// </summary>
        public static List<CniStep> PlanRoute(IReadOnlyList<CniWaypoint> wypts, CniRouteSpec spec)
        {
            List<CniStep> steps = [ ];
            List<CniWaypoint> route = RouteWaypoints(wypts, spec);
            if (route.Count == 0)
                return steps;

            Dictionary<int, string> idents = Idents(wypts);

            steps.Add(CniStep.Note("C130J route"));
            steps.Add(CniStep.Press(CniKeys.MARK));
            steps.Add(CniStep.Press(CniKeys.LskL(1)));
            steps.Add(CniStep.Press(CniKeys.LEGS));
            steps.Add(CniStep.Press(CniKeys.LskR(6)));
            steps.Add(CniStep.Press(CniKeys.LskL(1)));
            steps.AddRange(Type(idents[route[^1].Number]));
            steps.Add(CniStep.Press(CniKeys.LskR(1)));
            steps.Add(CniStep.Press(CniKeys.NEXT));

            int slot = 1;
            foreach (CniWaypoint wypt in route)
            {
                steps.AddRange(Type(idents[wypt.Number]));
                steps.Add(CniStep.Press(CniKeys.LskR(slot)));
                if (++slot > RouteRowsPerPage)
                {
                    steps.Add(CniStep.Press(CniKeys.NEXT));
                    slot = 1;
                }
            }

            steps.Add(CniStep.Press(CniKeys.LskL(6)));
            steps.Add(CniStep.Press(CniKeys.LskR(6)));
            return steps;
        }

        // ---- radios

        /// <summary>
        /// program one radio from COMM TUNE: CLR > COMM TUNE > L1 (UHF-1) or L3 (VHF-1), back to page 1, then the
        /// selected frequency (L1), each preset as [name] / freq on L2-L5 of its page, and the selected preset (L1).
        /// </summary>
        public static List<CniStep> PlanRadio(C130JRadio radio, CniRadioSpec spec)
        {
            List<CniStep> steps = [ ];
            if ((spec == null) || spec.IsEmpty)
                return steps;

            steps.Add(CniStep.Note($"C130J radio {radio}"));
            steps.Add(CniStep.Press(CniKeys.CLR));
            steps.Add(CniStep.Press(CniKeys.COMM_TUNE));
            steps.Add(CniStep.Press((radio == C130JRadio.UHF1) ? CniKeys.LskL(1) : CniKeys.LskL(3)));
            steps.Add(CniStep.Wait(RadioPageWaitMs));
            steps.Add(CniStep.FirstPage());

            if (spec.IsFrequencySelection)
            {
                string f = FormatFrequency(spec.Selection);
                if (f != null)
                {
                    steps.AddRange(Type(f));
                    steps.Add(CniStep.Press(CniKeys.LskL(1)));
                }
            }

            List<CniPreset> presets = [.. (spec.Presets ?? [ ]).Where(p => (p.Number >= 1) && (p.Number <= MaxPresets) &&
                                                                         (FormatFrequency(p.Frequency) != null))
                                                             .OrderBy(p => p.Number) ];
            if (presets.Count > 0)
            {
                int inPage = 1;
                foreach (CniPreset preset in presets)
                {
                    int pageNo = (preset.Number + PresetsPerPage - 1) / PresetsPerPage;
                    while (inPage < pageNo)
                    {
                        steps.Add(CniStep.Press(CniKeys.NEXT));
                        inPage++;
                    }
                    steps.AddRange(Type(PresetName(preset.Name)));
                    steps.Add(CniStep.Press(CniKeys.SLASH));
                    steps.AddRange(Type(FormatFrequency(preset.Frequency)));
                    int row = preset.Number - ((pageNo - 1) * PresetsPerPage);
                    steps.Add(CniStep.Press(CniKeys.LskL(row + 1)));
                }

                if (spec.IsPresetSelection)
                {
                    steps.AddRange(Type(spec.Selection.Trim()));
                    steps.Add(CniStep.Press(CniKeys.LskL(1)));
                }
            }
            return steps;
        }
    }
}
