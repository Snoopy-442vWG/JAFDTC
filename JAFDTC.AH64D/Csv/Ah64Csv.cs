// ********************************************************************************************************************
//
// Ah64Csv.cs : import / export AH-64D dtc worksheets as csv
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

using JAFDTC.AH64D.Dtc;
using JAFDTC.AH64D.Geo;
using System.Globalization;
using System.Text;

namespace JAFDTC.AH64D.Csv
{
    public sealed class CsvImportResult
    {
        public int PointsAdded { get; set; }
        public int PointsUpdated { get; set; }
        public int RoutesSet { get; set; }
        public int PresetsSet { get; set; }
        public List<string> Warnings { get; } = [];

        public string Summary()
        {
            List<string> parts = [];
            if (PointsAdded > 0) parts.Add($"{PointsAdded} point(s) added");
            if (PointsUpdated > 0) parts.Add($"{PointsUpdated} point(s) updated");
            if (RoutesSet > 0) parts.Add($"{RoutesSet} route(s) set");
            if (PresetsSet > 0) parts.Add($"{PresetsSet} preset(s) set");
            return (parts.Count == 0) ? "Nothing imported" : string.Join(", ", parts);
        }
    }

    /// <summary>
    /// csv worksheets for AH-64D dtc data. the column layout matches the 442 vWG AH-64D dtc worksheets:
    ///
    ///   points:  TSD CP, IDENT, FREE Text, Name, MGRS, Elev_ft, Latitude, Longitude
    ///   routes:  Route, #, Waypoint, TSD CP, IDENT, FREE Text, MGRS, Alt, Latitude, Longitude, Elev_ft [, Speed]
    ///   radios:  Preset, UnitID, CallSign, Primary, DLNet, VHF, UHF, FM1, FM2 [, HF] [, Notes]
    ///
    /// column names are matched without regard to case, spaces or underscores, and column order does not matter.
    /// position comes from Latitude/Longitude when both are given with at least 5 decimals, otherwise from MGRS.
    /// </summary>
    public static class Ah64Csv
    {
        public const double DefaultRouteSpeedKts = 80.0;

        // ------------------------------------------------------------------------------------------------------------
        //
        // import
        //
        // ------------------------------------------------------------------------------------------------------------

        public enum CsvKind { Unknown, Points, Route, Radios }

        public static CsvKind Detect(string csvText)
        {
            List<List<string>> rows = Parse(csvText);
            if (rows.Count == 0)
                return CsvKind.Unknown;
            HashSet<string> cols = [.. rows[0].Select(Key) ];
            if (cols.Contains("preset") && (cols.Contains("uhf") || cols.Contains("vhf") || cols.Contains("fm1")))
                return CsvKind.Radios;
            if (cols.Contains("route") && cols.Contains("tsdcp"))
                return CsvKind.Route;
            if (cols.Contains("tsdcp"))
                return CsvKind.Points;
            return CsvKind.Unknown;
        }

        /// <summary>
        /// import a points or route worksheet into a mission. points with the same TSD label are replaced.
        /// </summary>
        public static CsvImportResult ImportNav(Ah64Mission m, string csvText)
            => Detect(csvText) switch
            {
                CsvKind.Points => ImportPoints(m, csvText),
                CsvKind.Route => ImportRoutes(m, csvText),
                _ => throw new InvalidDataException("The CSV is not a points or route worksheet (it needs a " +
                                                    "\"TSD CP\" column).")
            };

        public static CsvImportResult ImportPoints(Ah64Mission m, string csvText)
        {
            CsvImportResult result = new();
            Table t = new(Parse(csvText));
            for (int r = 0; r < t.Count; r++)
            {
                Ah64Point p = PointFromRow(t, r, result, out bool hasPosition);
                if ((p != null) && !hasPosition)
                    result.Warnings.Add($"{p.Label}: no position (MGRS or Latitude/Longitude), skipped.");
                else if (p != null)
                    Upsert(m, p, result);
            }
            return result;
        }

        public static CsvImportResult ImportRoutes(Ah64Mission m, string csvText)
        {
            CsvImportResult result = new();
            Table t = new(Parse(csvText));

            // rows in file order, grouped by route name (keeping first-seen order of routes), sorted by "#".
            List<string> routeOrder = [];
            Dictionary<string, List<(int Seq, int Row)>> routes = new(StringComparer.OrdinalIgnoreCase);
            for (int r = 0; r < t.Count; r++)
            {
                string name = t.Get(r, "route").Trim();
                if (string.IsNullOrEmpty(name))
                    continue;
                if (!routes.ContainsKey(name))
                {
                    routes[name] = [];
                    routeOrder.Add(name);
                }
                int seq = int.TryParse(t.Get(r, "#"), out int s) ? s : routes[name].Count;
                routes[name].Add((seq, r));
            }

            foreach (string name in routeOrder)
            {
                Ah64Route route = FindOrAllocateRoute(m, name, result);
                if (route == null)
                    continue;
                route.Points.Clear();
                foreach ((int _, int r) in routes[name].OrderBy(x => x.Seq))
                {
                    Ah64Point p = PointFromRow(t, r, result, out bool hasPosition);
                    if (p == null)
                        continue;
                    if (p.Category == PointCategory.TGT)
                    {
                        result.Warnings.Add($"Route {name}: {p.Label} skipped, routes can only use W and C points.");
                        continue;
                    }
                    // a point repeated in a route (e.g. return to base) only updates the database once.
                    Ah64Point existing = m.FindPoint(p.Category, p.Number);
                    if (hasPosition && !((existing != null) && route.Points.Any(rp => rp.Number == p.Number)))
                        Upsert(m, p, result);
                    else if (existing == null)
                    {
                        result.Warnings.Add($"Route {name}: {p.Label} has no position and is not in the database.");
                        continue;
                    }
                    double speed = ParseDouble(t.Get(r, "speed"), DefaultRouteSpeedKts);
                    double altFt = ParseDouble(t.Get(r, "alt"), 0.0);
                    route.Points.Add(new Ah64RoutePoint() { Number = p.Number, SpeedKts = speed, AltFt = altFt });
                }
                route.IsEnabled = route.Points.Count > 0;
                result.RoutesSet++;
            }
            return result;
        }

        public static CsvImportResult ImportRadios(IList<Ah64CommPreset> presets, string csvText)
        {
            CsvImportResult result = new();
            Table t = new(Parse(csvText));
            for (int r = 0; r < t.Count; r++)
            {
                if (!int.TryParse(t.Get(r, "preset"), out int n) || (n < 1) || (n > Ah64Limits.CommPresets))
                {
                    if (!string.IsNullOrWhiteSpace(t.Get(r, "preset")))
                        result.Warnings.Add($"Row {r + 2}: preset \"{t.Get(r, "preset")}\" is not 1-10, skipped.");
                    continue;
                }
                while (presets.Count < Ah64Limits.CommPresets)
                    presets.Add(new Ah64CommPreset());

                Ah64CommPreset p = new()
                {
                    IsSet = true,
                    UnitId = Clip(t.Get(r, "unitid"), 8, $"Preset {n} unit ID", result),
                    CallSign = Clip(t.Get(r, "callsign"), 5, $"Preset {n} call sign", result),
                    DlNet = IsYes(t.Get(r, "dlnet")),
                    Primary = ParsePrimary(t.Get(r, "primary"))
                };
                SetFreq(p.VHF, t.Get(r, "vhf"), Ah64RadioRanges.VHF, $"Preset {n} VHF", result);
                SetFreq(p.UHF, t.Get(r, "uhf"), Ah64RadioRanges.UHF, $"Preset {n} UHF", result);
                SetFreq(p.FM1, t.Get(r, "fm1"), Ah64RadioRanges.FM, $"Preset {n} FM1", result);
                SetFreq(p.FM2, t.Get(r, "fm2"), Ah64RadioRanges.FM, $"Preset {n} FM2", result);
                if (!string.IsNullOrWhiteSpace(t.Get(r, "hf")))
                {
                    SetFreq(p.HFTx, t.Get(r, "hf"), Ah64RadioRanges.HF, $"Preset {n} HF", result);
                    p.HFRx.Frequency = p.HFTx.Frequency;
                }
                presets[n - 1] = p;
                result.PresetsSet++;
            }
            return result;
        }

        private static Ah64Route FindOrAllocateRoute(Ah64Mission m, string name, CsvImportResult result)
        {
            string key = name.Trim().ToUpperInvariant();
            if (key.Length > Ah64Limits.RouteNameLength)
            {
                result.Warnings.Add($"Route \"{name}\" shortened to \"{key[..Ah64Limits.RouteNameLength]}\" " +
                                    $"(the TSD allows {Ah64Limits.RouteNameLength} characters).");
                key = key[..Ah64Limits.RouteNameLength];
            }
            Ah64Route route = m.Routes.FirstOrDefault(r => r.Name.Trim().Equals(key, StringComparison.OrdinalIgnoreCase));
            if (route != null)
                return route;

            // take the first route slot that is still empty.
            route = m.Routes.FirstOrDefault(r => r.Points.Count == 0);
            if (route == null)
            {
                result.Warnings.Add($"Route \"{name}\" not imported: all {Ah64Limits.Routes} routes are in use.");
                return null;
            }
            route.Name = key;
            return route;
        }

        private static Ah64Point PointFromRow(Table t, int r, CsvImportResult result, out bool hasPosition)
        {
            hasPosition = false;
            string label = t.Get(r, "tsdcp").Trim();
            if (string.IsNullOrEmpty(label))
                return null;
            if (!PointIdents.TryParseLabel(label, out PointCategory cat, out int number))
            {
                result.Warnings.Add($"Row {r + 2}: \"{label}\" is not a TSD point (W01-W50, C51-C99, T01-T50), skipped.");
                return null;
            }
            string rowTag = PointIdents.Label(cat, number);

            string identCode = t.Get(r, "ident").Trim();
            PointIdent ident = PointIdents.Find(cat, identCode);
            if (ident == null)
            {
                ident = PointIdents.Default(cat);
                if (!string.IsNullOrEmpty(identCode))
                    result.Warnings.Add($"{rowTag}: IDENT \"{identCode}\" is not valid for a " +
                                        $"{PointIdents.CategoryName(cat)}, using {ident.Code}.");
            }

            Ah64Point p = new()
            {
                Category = cat,
                Number = number,
                Ident = ident.Code,
                Free = Clip(t.Get(r, "freetext").ToUpperInvariant(), Ah64Limits.FreeTextLength, $"{rowTag} free text",
                            result),
                Note = FirstNonEmpty(t.Get(r, "name"), t.Get(r, "waypoint"), t.Get(r, "note")),
                AltFt = ParseDouble(FirstNonEmpty(t.Get(r, "elevft"), t.Get(r, "elevation"), t.Get(r, "altft")), 0.0)
            };

            string latS = t.Get(r, "latitude");
            string lonS = t.Get(r, "longitude");
            string mgrs = t.Get(r, "mgrs");
            bool haveLL = double.TryParse(latS, NumberStyles.Float, CultureInfo.InvariantCulture, out double lat) &&
                          double.TryParse(lonS, NumberStyles.Float, CultureInfo.InvariantCulture, out double lon);
            bool llPrecise = haveLL && (Decimals(latS) >= 5) && (Decimals(lonS) >= 5);
            if (haveLL && (llPrecise || string.IsNullOrWhiteSpace(mgrs)))
            {
                p.Lat = double.Parse(latS, CultureInfo.InvariantCulture);
                p.Lon = double.Parse(lonS, CultureInfo.InvariantCulture);
                hasPosition = true;
            }
            else if (!string.IsNullOrWhiteSpace(mgrs))
            {
                if (Mgrs.TryParse(mgrs, out double mLat, out double mLon))
                {
                    (p.Lat, p.Lon) = (mLat, mLon);
                    hasPosition = true;
                }
                else
                {
                    result.Warnings.Add($"{rowTag}: MGRS \"{mgrs}\" could not be read.");
                }
            }
            if (!hasPosition && haveLL)
            {
                p.Lat = double.Parse(latS, CultureInfo.InvariantCulture);
                p.Lon = double.Parse(lonS, CultureInfo.InvariantCulture);
                hasPosition = true;
            }
            return p;
        }

        private static void Upsert(Ah64Mission m, Ah64Point p, CsvImportResult result)
        {
            Ah64Point existing = m.FindPoint(p.Category, p.Number);
            if (existing != null)
            {
                m.Points[m.Points.IndexOf(existing)] = p;
                result.PointsUpdated++;
            }
            else
            {
                m.Points.Add(p);
                result.PointsAdded++;
            }
        }

        private static PrimaryRadio ParsePrimary(string s)
            => (s ?? "").Trim().ToUpperInvariant() switch
            {
                "VHF" or "1" => PrimaryRadio.VHF,
                "UHF" or "2" => PrimaryRadio.UHF,
                "FM1" or "FM" or "3" => PrimaryRadio.FM1,
                "FM2" or "4" => PrimaryRadio.FM2,
                _ => PrimaryRadio.None
            };

        private static void SetFreq(Ah64RadioSetting r, string s, (double Min, double Max) range, string tag,
                                    CsvImportResult result)
        {
            if (string.IsNullOrWhiteSpace(s))
                return;
            if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out double f) &&
                Ah64RadioRanges.InRange(f, range))
                r.Frequency = f;
            else
                result.Warnings.Add($"{tag} \"{s}\" is not a valid frequency ({range.Min}-{range.Max} MHz), kept " +
                                    $"{r.Frequency}.");
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // export
        //
        // ------------------------------------------------------------------------------------------------------------

        public static string ExportPoints(Ah64Mission m)
        {
            StringBuilder sb = new();
            sb.AppendLine("TSD CP,IDENT,FREE Text,Name,MGRS,Elev_ft,Latitude,Longitude");
            foreach (PointCategory cat in new[] { PointCategory.WPTHZ, PointCategory.CTRLM, PointCategory.TGT })
                foreach (Ah64Point p in m.PointsIn(cat))
                    sb.AppendLine(Join(p.Label, p.Ident, p.Free, p.Note, p.Mgrs8, F(p.AltFt, 0), F(p.Lat, 6),
                                       F(p.Lon, 6)));
            return sb.ToString();
        }

        public static string ExportRoutes(Ah64Mission m)
        {
            StringBuilder sb = new();
            sb.AppendLine("Route,#,Waypoint,TSD CP,IDENT,FREE Text,MGRS,Alt,Latitude,Longitude,Elev_ft,Speed");
            foreach (Ah64Route r in m.Routes.Where(r => r.Points.Count > 0))
            {
                int seq = 0;
                foreach (Ah64RoutePoint rp in r.Points)
                {
                    Ah64Point p = m.FindPoint(rp.Category, rp.Number);
                    if (p == null)
                        continue;
                    sb.AppendLine(Join(r.Name.Trim(), seq.ToString(CultureInfo.InvariantCulture), p.Note, p.Label,
                                       p.Ident, p.Free, p.Mgrs8, F(rp.AltFt, 0), F(p.Lat, 6), F(p.Lon, 6),
                                       F(p.AltFt, 0), F(rp.SpeedKts, 0)));
                    seq++;
                }
            }
            return sb.ToString();
        }

        public static string ExportRadios(IList<Ah64CommPreset> presets)
        {
            StringBuilder sb = new();
            sb.AppendLine("Preset,UnitID,CallSign,Primary,DLNet,VHF,UHF,FM1,FM2,HF");
            for (int i = 0; i < presets.Count; i++)
            {
                Ah64CommPreset p = presets[i];
                if (!p.IsSet)
                    continue;
                sb.AppendLine(Join((i + 1).ToString(CultureInfo.InvariantCulture), p.UnitId, p.CallSign,
                                   (p.Primary == PrimaryRadio.None) ? "" : p.Primary.ToString(),
                                   p.DlNet ? "Yes" : "No", F(p.VHF.Frequency, 3), F(p.UHF.Frequency, 3),
                                   F(p.FM1.Frequency, 3), F(p.FM2.Frequency, 3), F(p.HFTx.Frequency, 4)));
            }
            return sb.ToString();
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // csv plumbing
        //
        // ------------------------------------------------------------------------------------------------------------

        private sealed class Table
        {
            private readonly Dictionary<string, int> _cols = [];
            private readonly List<List<string>> _rows;

            public Table(List<List<string>> rows)
            {
                if (rows.Count == 0)
                    throw new InvalidDataException("The CSV file is empty.");
                for (int i = 0; i < rows[0].Count; i++)
                    _cols.TryAdd(Key(rows[0][i]), i);
                _rows = [.. rows.Skip(1).Where(r => r.Any(c => !string.IsNullOrWhiteSpace(c))) ];
            }

            public int Count => _rows.Count;

            public string Get(int row, string key)
                => (_cols.TryGetValue(key, out int c) && (c < _rows[row].Count)) ? _rows[row][c].Trim() : "";
        }

        private static string Key(string header)
            => new string([.. (header ?? "").Trim().ToLowerInvariant().Where(ch => ch != ' ' && ch != '_' && ch != '-') ])
               .Replace("﻿", "");

        /// <summary>
        /// rfc 4180 csv parser (quoted fields, doubled quotes, embedded commas / newlines).
        /// </summary>
        public static List<List<string>> Parse(string text)
        {
            List<List<string>> rows = [];
            List<string> row = [];
            StringBuilder field = new();
            bool inQuotes = false;
            text ??= "";
            if (text.Length > 0 && text[0] == '﻿')
                text = text[1..];
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if ((i + 1 < text.Length) && (text[i + 1] == '"'))
                        {
                            field.Append('"');
                            i++;
                        }
                        else
                            inQuotes = false;
                    }
                    else
                        field.Append(c);
                }
                else if (c == '"')
                    inQuotes = true;
                else if (c == ',')
                {
                    row.Add(field.ToString());
                    field.Clear();
                }
                else if ((c == '\r') || (c == '\n'))
                {
                    if ((c == '\r') && (i + 1 < text.Length) && (text[i + 1] == '\n'))
                        i++;
                    row.Add(field.ToString());
                    field.Clear();
                    rows.Add(row);
                    row = [];
                }
                else
                    field.Append(c);
            }
            if ((field.Length > 0) || (row.Count > 0))
            {
                row.Add(field.ToString());
                rows.Add(row);
            }
            return rows;
        }

        private static string Join(params string[] fields)
            => string.Join(",", fields.Select(f =>
            {
                f ??= "";
                return (f.IndexOfAny([',', '"', '\n', '\r']) >= 0) ? "\"" + f.Replace("\"", "\"\"") + "\"" : f;
            }));

        private static string F(double v, int places) => v.ToString("F" + places, CultureInfo.InvariantCulture);

        private static int Decimals(string s)
        {
            int dot = (s ?? "").IndexOf('.');
            return (dot < 0) ? 0 : s.Length - dot - 1;
        }

        private static double ParseDouble(string s, double dflt)
            => double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out double v) ? v : dflt;

        private static bool IsYes(string s)
            => (s ?? "").Trim().ToUpperInvariant() is "YES" or "Y" or "TRUE" or "1" or "X";

        private static string FirstNonEmpty(params string[] values)
            => values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v))?.Trim() ?? "";

        private static string Clip(string s, int max, string tag, CsvImportResult result)
        {
            s = (s ?? "").Trim();
            if (s.Length > max)
            {
                result.Warnings.Add($"{tag} \"{s}\" shortened to {max} characters.");
                s = s[..max];
            }
            return s;
        }
    }
}
