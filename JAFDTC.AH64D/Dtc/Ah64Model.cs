// ********************************************************************************************************************
//
// Ah64Model.cs : plain data model for the AH-64D dcs dtc (navigation missions, comm presets)
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
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JAFDTC.AH64D.Dtc
{
    /// <summary>
    /// limits of the AH-64D dtc (eagle dynamics, "DCS: AH-64D Improvements", 7-Aug-2026): two missions, each with
    /// a 149 point database (50 W/H, 49 C, 50 T), 10 routes, 15 lines, 12 areas, 16 zones, 10 ADF presets; 10
    /// comm presets.
    /// </summary>
    public static class Ah64Limits
    {
        public const int Routes = 10;
        public const int Lines = 15;
        public const int Areas = 12;
        public const int Zones = 16;
        public const int AdfPresets = 10;
        public const int CommPresets = 10;
        public const int RouteNameLength = 5;
        public const int FreeTextLength = 3;
        public const int LineVertices = 4;                   // the TSD LINE page shows up to four vertices per line

        public const double MetersPerFoot = 0.3048;
        public const double MetersPerSecondPerKnot = 1852.0 / 3600.0;

        public static readonly string[] DefaultRouteNames
            = ["ALPHA", "BRAVO", "DELTA", "ECHO ", "HOTEL", "INDIA", "LIMA ", "OSCAR", "ROMEO", "TANGO"];
    }

    /// <summary>
    /// a latitude / longitude in decimal degrees (WGS-84).
    /// </summary>
    public sealed class GeoPoint
    {
        public double Lat { get; set; }
        public double Lon { get; set; }

        public GeoPoint() { }
        public GeoPoint(double lat, double lon) => (Lat, Lon) = (lat, lon);

        [JsonIgnore]
        public string Mgrs8 => Geo.Mgrs.FromLatLon(Lat, Lon, 8);
    }

    /// <summary>
    /// one point in a WPTHZ, CTRLM or TGT database.
    /// </summary>
    public sealed class Ah64Point
    {
        public PointCategory Category { get; set; }

        public int Number { get; set; }                     // W/T: 1-50, C: 51-99

        public string Ident { get; set; } = "";             // IDENT code, see PointIdents (e.g. "WP", "AI", "6")

        public string Free { get; set; } = "";              // up to 3 characters of free text

        public string Note { get; set; } = "";              // dtc "note" (shown in the dcs dtc manager)

        public double Lat { get; set; }

        public double Lon { get; set; }

        public double AltFt { get; set; }                   // feet MSL (the dtc stores meters)

        [JsonIgnore]
        public string Label => PointIdents.Label(Category, Number);

        [JsonIgnore]
        public string Mgrs8 => Mgrs.FromLatLon(Lat, Lon, 8);

        [JsonIgnore]
        public PointIdent IdentInfo => PointIdents.Find(Category, Ident);
    }

    /// <summary>
    /// one point on a route. routes reference points in the WPTHZ (1-50) or CTRLM (51-99) databases by number;
    /// those ranges do not overlap, which is why the dtc can store just "num".
    /// </summary>
    public sealed class Ah64RoutePoint
    {
        public int Number { get; set; }

        public double SpeedKts { get; set; } = 80.0;

        public double AltFt { get; set; }                   // dtc route "alt" (meters in the file); 0 = not set

        public bool Fix { get; set; }

        [JsonIgnore]
        public PointCategory Category => (Number >= 51) ? PointCategory.CTRLM : PointCategory.WPTHZ;

        [JsonIgnore]
        public string Label => PointIdents.Label(Category, Number);
    }

    public sealed class Ah64Route
    {
        public string Name { get; set; } = "";

        public bool IsEnabled { get; set; }

        public double StartTimeSec { get; set; }            // "eta" of the first point: seconds after midnight

        public List<Ah64RoutePoint> Points { get; set; } = [];
    }

    public sealed class Ah64Line
    {
        public int TypeNum { get; set; } = 1;

        public string Text { get; set; } = "";

        public string Note { get; set; } = "";

        public List<GeoPoint> Vertices { get; set; } = [];
    }

    public enum ShapeKind
    {
        Area = 0,
        NoFireZone = 1,                                      // dtc "NFZ"
        PriorityFireZone = 2                                 // dtc "PFZ"
    }

    /// <summary>
    /// an area, NFZ or PFZ polygon. the dcs dtc manager stores four caption anchor points with each polygon; when
    /// a shape is imported from a .dtc its captions are kept (CaptionsJson) and reused unless the vertices change.
    /// </summary>
    public sealed class Ah64Shape
    {
        public ShapeKind Kind { get; set; } = ShapeKind.Area;

        public string Note { get; set; } = "";

        public List<GeoPoint> Vertices { get; set; } = [];

        public string CaptionsJson { get; set; } = "";      // "caption_pos" as imported, "" to regenerate

        public string CaptionsVertexKey { get; set; } = ""; // vertex signature CaptionsJson belongs to

        public string VertexKey()
            => string.Join(";", Vertices.Select(v => $"{v.Lat:F8},{v.Lon:F8}"));
    }

    public sealed class Ah64AdfPreset
    {
        public string Id { get; set; } = "";                // NDB identifier, up to 3 characters

        public double FreqKHz { get; set; } = 100.0;

        [JsonIgnore]
        public bool IsDefault => string.IsNullOrEmpty(Id) && (FreqKHz == 100.0);
    }

    /// <summary>
    /// one of the two AH-64D dtc "missions" (Mission_1 / Mission_2).
    /// </summary>
    public sealed class Ah64Mission
    {
        public string Theater { get; set; } = "";

        public List<Ah64Point> Points { get; set; } = [];

        public bool LoadWptHz { get; set; } = true;          // Points.WPTHZ.isEnabled

        public bool LoadCtrlm { get; set; } = true;          // Points.CTRLM.isEnabled

        public bool LoadTgt { get; set; } = true;            // Points.TGT.isEnabled

        public List<Ah64Route> Routes { get; set; } = NewRoutes();

        public List<Ah64Line> Lines { get; set; } = [];

        public List<Ah64Shape> Shapes { get; set; } = [];

        public bool AdfEnabled { get; set; }

        public List<Ah64AdfPreset> Adf { get; set; } = NewAdf();

        public static List<Ah64Route> NewRoutes()
            => [.. Ah64Limits.DefaultRouteNames.Select(n => new Ah64Route() { Name = n }) ];

        public static List<Ah64AdfPreset> NewAdf()
            => [.. Enumerable.Range(0, Ah64Limits.AdfPresets).Select(_ => new Ah64AdfPreset()) ];

        [JsonIgnore]
        public bool IsDefault => (Points.Count == 0) &&
                                 Routes.All(r => r.Points.Count == 0) &&
                                 (Lines.Count == 0) &&
                                 (Shapes.Count == 0) &&
                                 !AdfEnabled && Adf.All(a => a.IsDefault);

        public IEnumerable<Ah64Point> PointsIn(PointCategory cat)
            => Points.Where(p => p.Category == cat).OrderBy(p => p.Number);

        public Ah64Point FindPoint(PointCategory cat, int number)
            => Points.FirstOrDefault(p => (p.Category == cat) && (p.Number == number));

        /// <summary>
        /// next unused point number in a category, -1 if the database is full.
        /// </summary>
        public int NextFreeNumber(PointCategory cat)
        {
            HashSet<int> used = [.. Points.Where(p => p.Category == cat).Select(p => p.Number) ];
            for (int n = PointIdents.MinNumber(cat); n <= PointIdents.MaxNumber(cat); n++)
                if (!used.Contains(n))
                    return n;
            return -1;
        }

        public void Reset()
        {
            Points.Clear();
            LoadWptHz = LoadCtrlm = LoadTgt = true;
            Routes = NewRoutes();
            Lines.Clear();
            Shapes.Clear();
            AdfEnabled = false;
            Adf = NewAdf();
        }

        public Ah64Mission Clone() => Ah64Json.Clone(this);
    }

    /// <summary>
    /// the primary radio a comm preset selects. values are the dtc "PrimaryFrequency" codes.
    /// </summary>
    public enum PrimaryRadio
    {
        None = 0,
        VHF = 1,
        UHF = 2,
        FM1 = 3,
        FM2 = 4
    }

    public sealed class Ah64RadioSetting
    {
        public double Frequency { get; set; }               // MHz

        public int Modulation { get; set; }                 // dtc: 0 AM, 1 FM, 2 HF (as written by dcs)

        public int Encryption { get; set; }                 // dtc: 0 plain
    }

    /// <summary>
    /// one AH-64D comm preset (Presets.COMM[n] in the dtc).
    /// </summary>
    public sealed class Ah64CommPreset
    {
        public bool IsSet { get; set; }                      // false: leave the template's preset alone

        public string UnitId { get; set; } = "";             // up to 8 characters

        public string CallSign { get; set; } = "";           // up to 5 characters

        public PrimaryRadio Primary { get; set; } = PrimaryRadio.None;

        public bool DlNet { get; set; }

        public Ah64RadioSetting VHF { get; set; } = new() { Frequency = 127.5, Modulation = 0 };

        public Ah64RadioSetting UHF { get; set; } = new() { Frequency = 305.0, Modulation = 0 };

        public Ah64RadioSetting FM1 { get; set; } = new() { Frequency = 30.0, Modulation = 1 };

        public Ah64RadioSetting FM2 { get; set; } = new() { Frequency = 30.0, Modulation = 1 };

        public Ah64RadioSetting HFTx { get; set; } = new() { Frequency = 2.0, Modulation = 2 };

        public Ah64RadioSetting HFRx { get; set; } = new() { Frequency = 2.0, Modulation = 2 };

        public int HfAle { get; set; } = 1;

        public int HfPre { get; set; } = 1;

        public bool DoNotUploadTabData { get; set; }

        public Ah64CommPreset Clone() => Ah64Json.Clone(this);
    }

    /// <summary>
    /// frequency ranges for validation (MHz). VHF ARC-186, UHF ARC-164, FM ARC-201D, HF ARC-220.
    /// </summary>
    public static class Ah64RadioRanges
    {
        public static (double Min, double Max) VHF => (30.0, 151.975);
        public static (double Min, double Max) UHF => (225.0, 399.975);
        public static (double Min, double Max) FM => (30.0, 87.975);
        public static (double Min, double Max) HF => (2.0, 29.9999);
        public static (double Min, double Max) ADFKHz => (100.0, 2199.5);

        public static bool InRange(double v, (double Min, double Max) r) => (v >= r.Min) && (v <= r.Max);
    }

    internal static class Ah64Json
    {
        private static readonly JsonSerializerOptions _opts = new() { WriteIndented = false };

        public static T Clone<T>(T obj) => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(obj, _opts), _opts);
    }
}
