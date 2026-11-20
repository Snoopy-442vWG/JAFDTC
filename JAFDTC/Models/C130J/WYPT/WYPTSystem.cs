// ********************************************************************************************************************
//
// WYPTSystem.cs -- c-130j waypoint system
//
// Copyright(C) 2023-2024 ilominar/raven, JAFDTC contributors
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

using JAFDTC.Airlift.C130J;
using JAFDTC.Airlift.Cni;
using JAFDTC.Models.Base;
using JAFDTC.Models.Core;
using JAFDTC.Models.Units;
using JAFDTC.Utilities;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.Json.Serialization;

namespace JAFDTC.Models.C130J.WYPT
{
    /// <summary>
    /// the c-130j waypoint list loaded into the CNI-MU waypoint database.
    /// </summary>
    public partial class WYPTSystem : NavpointSystemBase<WaypointInfo>
    {
        public const string SystemTag = "JAFDTC:C130J:WYPT";
        public const string WYPTListTag = $"{SystemTag}:LIST";

        public static readonly NavpointSystemInfo SystemInfo = new(SystemTag,
                                                                   WYPTListTag,
                                                                   AirframeTypes.C130J,
                                                                   "Waypoint",
                                                                   LLFormat.DDM_P3ZF,
                                                                   C130JCni.MaxIdentLength,
                                                                   C130JCni.MaxWaypoints,
                                                                   [ "Main" ])
        {
            NavptNameCountsAlphaNumOnly = true
        };

        [JsonIgnore]
        public override NavpointSystemInfo SysInfo => SystemInfo;

        public WYPTSystem() => (Points) = ([ ]);

        public WYPTSystem(WYPTSystem other) => (Points) = (new ObservableCollection<WaypointInfo>(other.Points));

        public virtual object Clone() => new WYPTSystem(this);

        /// <summary>
        /// the valid waypoints as planner input (lat/lon in decimal degrees, elevation in feet, blank = 0).
        /// </summary>
        public List<CniWaypoint> ToCniWaypoints()
        {
            List<CniWaypoint> list = [ ];
            foreach (WaypointInfo wypt in Points)
            {
                if (!wypt.IsValid ||
                    !double.TryParse(wypt.Lat, NumberStyles.Float, CultureInfo.InvariantCulture, out double lat) ||
                    !double.TryParse(wypt.Lon, NumberStyles.Float, CultureInfo.InvariantCulture, out double lon))
                {
                    continue;
                }
                int elev = int.TryParse(wypt.Alt, NumberStyles.Integer, CultureInfo.InvariantCulture, out int e) ? e : 0;
                list.Add(new CniWaypoint(wypt.Number, wypt.Name, lat, lon, elev));
            }
            return list;
        }

        public override void AddNavpointsFromPositionList(IReadOnlyList<UnitPositionItem> posnList)
        {
            foreach (UnitPositionItem posn in posnList)
            {
                WaypointInfo wypt = new()
                {
                    Name = C130JCni.AlphaNum(posn.Name ?? ""),
                    Lat = $"{posn.Latitude:F8}",
                    Lon = $"{posn.Longitude:F8}",
                    Alt = $"{(int)posn.Altitude}"
                };
                if (wypt.Name.Length > C130JCni.MaxIdentLength)
                    wypt.Name = wypt.Name[..C130JCni.MaxIdentLength];
                Add(wypt);
            }
        }

        public override WaypointInfo Add(WaypointInfo wypt = null, int atIndex = -1)
        {
            wypt ??= new();
            atIndex = (atIndex >= Points.Count) ? -1 : atIndex;
            if (Points.Count == 0)
                wypt.Number = 1;
            else if (atIndex == -1)
                wypt.Number = Points[^1].Number + 1;
            else
                wypt.Number = Points[atIndex].Number;
            wypt.Name = (string.IsNullOrEmpty(wypt.Name)) ? $"WP{wypt.Number}" : wypt.Name;
            return base.Add(wypt, atIndex);
        }
    }
}
