// ********************************************************************************************************************
//
// CniModels.cs : plain inputs for the CNI-MU planners (waypoints, route selection, radios)
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

using System.Globalization;
using System.Text.RegularExpressions;

namespace JAFDTC.Airlift.Cni
{
    /// <summary>
    /// a waypoint to load: number (the JAFDTC list number), name, wgs-84 position (decimal degrees) and elevation
    /// in feet msl.
    /// </summary>
    public sealed record CniWaypoint(int Number, string Name, double Lat, double Lon, int ElevationFt);

    /// <summary>
    /// how the route (LEGS page) is built from the waypoint list.
    /// </summary>
    public enum CniRouteMode
    {
        None = 0,           // load waypoints only, do not build a route
        All = 1,            // every waypoint, in list order
        Range = 2,          // waypoints From..To (by number)
        List = 3            // waypoints in the order given, e.g. "1,3,5-8"
    }

    public sealed partial record CniRouteSpec(CniRouteMode Mode, int From = 0, int To = 0, IReadOnlyList<int> List = null)
    {
        public static readonly CniRouteSpec NoRoute = new(CniRouteMode.None);

        [GeneratedRegex(@"^\s*\d+(\s*-\s*\d+)?(\s*,\s*\d+(\s*-\s*\d+)?)*\s*$")]
        private static partial Regex ListRegex();

        /// <summary>
        /// true if the text is a valid waypoint list: numbers and ascending or descending ranges separated by
        /// commas, e.g. "1,3,5-8" or "6-2".
        /// </summary>
        public static bool IsValidList(string text) => !string.IsNullOrWhiteSpace(text) && ListRegex().IsMatch(text);

        /// <summary>
        /// parse a waypoint list ("1,3,5-8") into waypoint numbers in order. returns an empty list if the text is
        /// not valid.
        /// </summary>
        public static List<int> ParseList(string text)
        {
            List<int> nums = [ ];
            if (!IsValidList(text))
                return nums;
            foreach (string part in text.Split(','))
            {
                string[] ends = part.Split('-');
                int a = int.Parse(ends[0].Trim(), CultureInfo.InvariantCulture);
                int b = (ends.Length > 1) ? int.Parse(ends[1].Trim(), CultureInfo.InvariantCulture) : a;
                int step = (b >= a) ? 1 : -1;
                for (int n = a; n != b + step; n += step)
                    nums.Add(n);
            }
            return nums;
        }
    }

    /// <summary>
    /// a radio preset: number (1-based), optional short name, frequency in MHz as text ("251.000").
    /// </summary>
    public sealed record CniPreset(int Number, string Name, string Frequency);

    /// <summary>
    /// what to load into one radio: presets, plus an optional selection that is either a frequency ("251.000") or
    /// a preset number ("3"). an empty selection leaves the radio tuned where it is.
    /// </summary>
    public sealed record CniRadioSpec(IReadOnlyList<CniPreset> Presets, string Selection = null)
    {
        public bool IsEmpty => ((Presets == null) || (Presets.Count == 0)) && string.IsNullOrWhiteSpace(Selection);

        /// <summary>
        /// true when the selection is a preset number rather than a frequency.
        /// </summary>
        public bool IsPresetSelection => !string.IsNullOrWhiteSpace(Selection) && !Selection.Contains('.') &&
                                         int.TryParse(Selection.Trim(), out int n) && (n >= 1) && (n < 100);

        public bool IsFrequencySelection => !string.IsNullOrWhiteSpace(Selection) && !IsPresetSelection;
    }
}
