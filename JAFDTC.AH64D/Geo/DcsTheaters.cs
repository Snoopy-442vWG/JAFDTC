// ********************************************************************************************************************
//
// DcsTheaters.cs : dcs theater (terrain) map projections, x/y <-> lat/lon
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

namespace JAFDTC.AH64D.Geo
{
    /// <summary>
    /// a position in dcs map coordinates. dcs "x" is north (meters), dcs "y" is east (meters). this is the
    /// coordinate system used by .miz files and by the "x"/"y" fields of .dtc files.
    /// </summary>
    public readonly record struct DcsXY(double X, double Y);

    /// <summary>
    /// dcs theater projections. every dcs theater is a WGS-84 transverse mercator (scale 0.9996) with a theater
    /// specific central meridian and false easting / northing; dcs x = northing, dcs y = easting.
    ///
    /// the parameters below were solved (16-Nov-2026) from points placed with dcs-mcp, which reads eagle
    /// dynamics' own terrain data: every theater fits with a residual of 0.0000 m, and the 8 theaters that pydcs
    /// also publishes (Caucasus, Nevada, PersianGulf, Normandy, TheChannel, Syria, MarianaIslands, Falklands)
    /// match pydcs exactly. GermanyCW, Kola, Afghanistan, Iraq and Sinai are not in the released pydcs.
    /// </summary>
    public static class DcsTheaters
    {
        public sealed record TheaterProjection(string Name, double CentralMeridian, double FalseEasting,
                                               double FalseNorthing, double ScaleFactor = 0.9996)
        {
            private readonly TransverseMercator _tm
                = new(CentralMeridian, ScaleFactor, FalseEasting, FalseNorthing);

            public DcsXY ToXY(double latDeg, double lonDeg)
            {
                (double e, double n) = _tm.Forward(latDeg, lonDeg);
                return new DcsXY(n, e);
            }

            public (double Lat, double Lon) ToLatLon(DcsXY xy) => _tm.Inverse(xy.Y, xy.X);

            public (double Lat, double Lon) ToLatLon(double x, double y) => _tm.Inverse(y, x);
        }

        private static readonly Dictionary<string, TheaterProjection> _theaters
            = new(StringComparer.OrdinalIgnoreCase)
            {
                ["Afghanistan"]        = new("Afghanistan",     63.0,  -300150.0,  -3759657.0),
                ["Caucasus"]           = new("Caucasus",        33.0,   -99517.0,  -4998115.0),
                ["Falklands"]          = new("Falklands",      -57.0,   147640.0,   5815417.0),
                ["GermanyCW"]          = new("GermanyCW",       21.0,    35427.62, -6061633.128),
                ["Iraq"]               = new("Iraq",            45.0,    72290.0,  -3680057.0),
                ["Kola"]               = new("Kola",            21.0,   -62702.0,  -7543625.0),
                ["MarianaIslands"]     = new("MarianaIslands", 147.0,   238418.0,  -1491840.0),
                ["MarianaIslandsWWII"] = new("MarianaIslandsWWII", 147.0, 238418.0, -1491840.0),
                ["Nevada"]             = new("Nevada",        -117.0,  -193996.81, -4410028.064),
                ["Normandy"]           = new("Normandy",        -3.0,  -195526.0,  -5484813.0),
                ["PersianGulf"]        = new("PersianGulf",     57.0,    75756.0,  -2894933.0),
                ["SinaiMap"]           = new("SinaiMap",        33.0,   169222.0,  -3325313.0),
                ["Syria"]              = new("Syria",           39.0,   282801.0,  -3879866.0),
                ["TheChannel"]         = new("TheChannel",       3.0,    99376.0,  -5636889.0),
            };

        // alternate names used by dcs / tools for the same theater.
        private static readonly Dictionary<string, string> _aliases = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Sinai"] = "SinaiMap",
            ["Germany"] = "GermanyCW",
            ["ColdWarGermany"] = "GermanyCW",
            ["Marianas"] = "MarianaIslands",
            ["Falklands_Islands"] = "Falklands",
            ["SouthAtlantic"] = "Falklands",
        };

        /// <summary>
        /// theater names (as dcs writes them in the .dtc "terrain" field) in display order.
        /// </summary>
        public static IReadOnlyList<string> Names { get; }
            = [.. _theaters.Keys.Where(k => k != "MarianaIslandsWWII").OrderBy(k => k) ];

        public static bool IsKnown(string theater) => Find(theater) != null;

        /// <summary>
        /// returns the projection for a theater (accepting aliases), null if unknown.
        /// </summary>
        public static TheaterProjection Find(string theater)
        {
            if (string.IsNullOrWhiteSpace(theater))
                return null;
            if (_theaters.TryGetValue(theater.Trim(), out TheaterProjection proj))
                return proj;
            if (_aliases.TryGetValue(theater.Trim(), out string alias))
                return _theaters[alias];
            return null;
        }

        /// <summary>
        /// returns the projection for a theater, throws if unknown.
        /// </summary>
        public static TheaterProjection Get(string theater)
            => Find(theater) ?? throw new ArgumentException($"Unknown DCS theater \"{theater}\"");

        /// <summary>
        /// returns the canonical dcs name for a theater (resolving aliases), or the input if unknown.
        /// </summary>
        public static string Canonical(string theater) => Find(theater)?.Name ?? theater;
    }
}
