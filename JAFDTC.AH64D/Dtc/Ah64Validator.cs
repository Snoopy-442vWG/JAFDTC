// ********************************************************************************************************************
//
// Ah64Validator.cs : validation of AH-64D dtc mission / comm data before it is written
//
// Copyright(C) 2026 442 vWG
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
using System.Text.RegularExpressions;

namespace JAFDTC.AH64D.Dtc
{
    public static partial class Ah64Validator
    {
        [GeneratedRegex(@"^[A-Z0-9 ]*$")]
        private static partial Regex FreeTextRegex();

        public static List<string> ValidateMission(Ah64Mission m)
        {
            List<string> errs = [];

            if (!DcsTheaters.IsKnown(m.Theater))
                errs.Add($"Theater \"{m.Theater}\" is not a known DCS theater (choose one on the mission page).");

            // points
            foreach (PointCategory cat in Enum.GetValues<PointCategory>())
            {
                List<Ah64Point> pts = [.. m.Points.Where(p => p.Category == cat) ];
                if (pts.Count > PointIdents.MaxCount(cat))
                    errs.Add($"{PointIdents.CategoryName(cat)}: {pts.Count} points, the database holds " +
                             $"{PointIdents.MaxCount(cat)}.");
                foreach (IGrouping<int, Ah64Point> dup in pts.GroupBy(p => p.Number).Where(g => g.Count() > 1))
                    errs.Add($"{PointIdents.Label(cat, dup.Key)} is used by {dup.Count()} points.");
            }
            foreach (Ah64Point p in m.Points)
            {
                string label = PointIdents.Label(p.Category, p.Number);
                if ((p.Number < PointIdents.MinNumber(p.Category)) || (p.Number > PointIdents.MaxNumber(p.Category)))
                    errs.Add($"{label}: number must be {PointIdents.MinNumber(p.Category)}-" +
                             $"{PointIdents.MaxNumber(p.Category)} for a {PointIdents.CategoryName(p.Category)}.");
                if (PointIdents.Find(p.Category, p.Ident) == null)
                    errs.Add($"{label}: IDENT \"{p.Ident}\" is not valid for a {PointIdents.CategoryName(p.Category)}.");
                string free = (p.Free ?? "").ToUpperInvariant();
                if ((free.Length > Ah64Limits.FreeTextLength) || !FreeTextRegex().IsMatch(free))
                    errs.Add($"{label}: free text \"{p.Free}\" must be up to 3 letters / digits.");
                if (!IsLatLon(p.Lat, p.Lon))
                    errs.Add($"{label}: position is not valid.");
                if ((p.AltFt < -1500.0) || (p.AltFt > 30000.0))
                    errs.Add($"{label}: altitude {p.AltFt:F0} ft is out of range.");
            }

            // routes
            if (m.Routes.Count > Ah64Limits.Routes)
                errs.Add($"{m.Routes.Count} routes, the dtc holds {Ah64Limits.Routes}.");
            for (int i = 0; i < m.Routes.Count; i++)
            {
                Ah64Route r = m.Routes[i];
                string rName = string.IsNullOrWhiteSpace(r.Name) ? $"Route {i + 1}" : $"Route {r.Name.Trim()}";
                if ((r.Name ?? "").Trim().Length > Ah64Limits.RouteNameLength)
                    errs.Add($"{rName}: name is longer than {Ah64Limits.RouteNameLength} characters.");
                if ((r.StartTimeSec < 0.0) || (r.StartTimeSec >= 86400.0 * 31))
                    errs.Add($"{rName}: start time is out of range.");
                foreach (Ah64RoutePoint rp in r.Points)
                {
                    if (m.FindPoint(rp.Category, rp.Number) == null)
                        errs.Add($"{rName}: {rp.Label} is not in the point database (routes use W01-W50 and C51-C99).");
                    if ((rp.SpeedKts < 0.0) || (rp.SpeedKts > 200.0))
                        errs.Add($"{rName}: {rp.Label} speed {rp.SpeedKts:F0} kt is out of range (0-200).");
                }
            }

            // lines, shapes
            if (m.Lines.Count > Ah64Limits.Lines)
                errs.Add($"{m.Lines.Count} lines, the dtc holds {Ah64Limits.Lines}.");
            for (int i = 0; i < m.Lines.Count; i++)
            {
                if (m.Lines[i].Vertices.Count < 2)
                    errs.Add($"Line {i + 1}: needs at least 2 vertices.");
                if (m.Lines[i].Vertices.Count > Ah64Limits.LineVertices)
                    errs.Add($"Line {i + 1}: has {m.Lines[i].Vertices.Count} vertices, the TSD shows " +
                             $"{Ah64Limits.LineVertices}.");
                if (m.Lines[i].Vertices.Any(v => !IsLatLon(v.Lat, v.Lon)))
                    errs.Add($"Line {i + 1}: has an invalid vertex.");
            }
            int nAreas = m.Shapes.Count(s => s.Kind == ShapeKind.Area);
            int nZones = m.Shapes.Count - nAreas;
            if (nAreas > Ah64Limits.Areas)
                errs.Add($"{nAreas} areas, the dtc holds {Ah64Limits.Areas}.");
            if (nZones > Ah64Limits.Zones)
                errs.Add($"{nZones} zones (NFZ + PFZ), the dtc holds {Ah64Limits.Zones}.");
            for (int i = 0; i < m.Shapes.Count; i++)
            {
                if (m.Shapes[i].Vertices.Count < 3)
                    errs.Add($"{m.Shapes[i].Kind} \"{m.Shapes[i].Note}\": needs at least 3 vertices.");
                if (m.Shapes[i].Vertices.Any(v => !IsLatLon(v.Lat, v.Lon)))
                    errs.Add($"{m.Shapes[i].Kind} \"{m.Shapes[i].Note}\": has an invalid vertex.");
            }

            // adf
            for (int i = 0; i < m.Adf.Count; i++)
            {
                Ah64AdfPreset a = m.Adf[i];
                if (!Ah64RadioRanges.InRange(a.FreqKHz, Ah64RadioRanges.ADFKHz))
                    errs.Add($"ADF preset {i + 1}: {a.FreqKHz} kHz is outside 100-2199.5 kHz.");
                if ((a.Id ?? "").Length > 3)
                    errs.Add($"ADF preset {i + 1}: identifier \"{a.Id}\" is longer than 3 characters.");
            }

            return errs;
        }

        public static List<string> ValidateComms(IList<Ah64CommPreset> presets)
        {
            List<string> errs = [];
            for (int i = 0; i < presets.Count; i++)
            {
                Ah64CommPreset p = presets[i];
                if (!p.IsSet)
                    continue;
                string tag = $"Preset {i + 1}";
                if ((p.UnitId ?? "").Length > 16)
                    errs.Add($"{tag}: unit ID \"{p.UnitId}\" is too long.");
                if ((p.CallSign ?? "").Length > 16)
                    errs.Add($"{tag}: call sign \"{p.CallSign}\" is too long.");
                if (!Ah64RadioRanges.InRange(p.VHF.Frequency, Ah64RadioRanges.VHF))
                    errs.Add($"{tag}: VHF {p.VHF.Frequency} MHz is out of range.");
                if (!Ah64RadioRanges.InRange(p.UHF.Frequency, Ah64RadioRanges.UHF))
                    errs.Add($"{tag}: UHF {p.UHF.Frequency} MHz is out of range.");
                if (!Ah64RadioRanges.InRange(p.FM1.Frequency, Ah64RadioRanges.FM))
                    errs.Add($"{tag}: FM1 {p.FM1.Frequency} MHz is out of range.");
                if (!Ah64RadioRanges.InRange(p.FM2.Frequency, Ah64RadioRanges.FM))
                    errs.Add($"{tag}: FM2 {p.FM2.Frequency} MHz is out of range.");
                if (!Ah64RadioRanges.InRange(p.HFTx.Frequency, Ah64RadioRanges.HF) ||
                    !Ah64RadioRanges.InRange(p.HFRx.Frequency, Ah64RadioRanges.HF))
                    errs.Add($"{tag}: HF frequency is out of range.");
            }
            return errs;
        }

        /// <summary>
        /// non-fatal advice for comm presets. the dcs dtc manager itself writes longer names ("PRESET 10",
        /// "PRE 10"), so these are not errors, but the KU / COM page show 8 (unit ID) and 5 (call sign) characters.
        /// </summary>
        public static List<string> CommWarnings(IList<Ah64CommPreset> presets)
        {
            List<string> warns = [];
            for (int i = 0; i < presets.Count; i++)
            {
                Ah64CommPreset p = presets[i];
                if (!p.IsSet)
                    continue;
                if ((p.UnitId ?? "").Length > 8)
                    warns.Add($"Preset {i + 1}: unit ID \"{p.UnitId}\" is longer than the 8 characters the COM page shows.");
                if ((p.CallSign ?? "").Length > 5)
                    warns.Add($"Preset {i + 1}: call sign \"{p.CallSign}\" is longer than the 5 characters the COM page shows.");
            }
            return warns;
        }

        private static bool IsLatLon(double lat, double lon)
            => !double.IsNaN(lat) && !double.IsNaN(lon) && (lat >= -80.0) && (lat <= 84.0) &&
               (lon >= -180.0) && (lon <= 180.0) && !((lat == 0.0) && (lon == 0.0));
    }
}
