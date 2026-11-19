// ********************************************************************************************************************
//
// RTESystem.cs -- c-130j route (LEGS page) selection
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

using JAFDTC.Airlift.Cni;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json.Serialization;

namespace JAFDTC.Models.C130J.RTE
{
    /// <summary>
    /// which waypoints the upload strings into the route on the LEGS page. Mode is the integer value of
    /// CniRouteMode as a string: 0 = no route (default), 1 = all waypoints, 2 = range From..To, 3 = list.
    /// </summary>
    public class RTESystem : SystemBase
    {
        public const string SystemTag = "JAFDTC:C130J:RTE";

        private string _mode;                               // integer [0, 3]
        public string Mode
        {
            get => _mode;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 0, 3)) ? null : "Invalid format";
                SetProperty(ref _mode, value, error);
            }
        }

        private string _rangeFrom;                          // waypoint number
        public string RangeFrom
        {
            get => _rangeFrom;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 1, 200)) ? null : "Invalid format";
                if (error == null)
                    value = FixupIntegerField(value);
                SetProperty(ref _rangeFrom, value, error);
            }
        }

        private string _rangeTo;                            // waypoint number
        public string RangeTo
        {
            get => _rangeTo;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 1, 200)) ? null : "Invalid format";
                if (error == null)
                    value = FixupIntegerField(value);
                SetProperty(ref _rangeTo, value, error);
            }
        }

        private string _waypointList;                       // "1,3,5-8"
        public string WaypointList
        {
            get => _waypointList;
            set
            {
                string error = (string.IsNullOrEmpty(value) || CniRouteSpec.IsValidList(value)) ? null : "Invalid format";
                SetProperty(ref _waypointList, value, error);
            }
        }

        [JsonIgnore]
        public CniRouteMode ModeValue
            => (int.TryParse(Mode, out int m) && (m >= 0) && (m <= 3)) ? (CniRouteMode)m : CniRouteMode.None;

        [JsonIgnore]
        public override bool IsDefault => (ModeValue == CniRouteMode.None) &&
                                          string.IsNullOrEmpty(RangeFrom) && string.IsNullOrEmpty(RangeTo) &&
                                          string.IsNullOrEmpty(WaypointList);

        public RTESystem() => Reset();

        public RTESystem(RTESystem other)
        {
            Mode = other.Mode;
            RangeFrom = other.RangeFrom;
            RangeTo = other.RangeTo;
            WaypointList = other.WaypointList;
        }

        public virtual object Clone() => new RTESystem(this);

        public override void Reset()
        {
            Mode = "0";
            RangeFrom = "";
            RangeTo = "";
            WaypointList = "";
        }

        /// <summary>
        /// the route selection as planner input. a range with one end blank uses that end for both; a range with
        /// both ends blank, or a list that is blank, plans no route.
        /// </summary>
        public CniRouteSpec ToSpec()
        {
            switch (ModeValue)
            {
                case CniRouteMode.All:
                    return new CniRouteSpec(CniRouteMode.All);
                case CniRouteMode.Range:
                    bool hasFrom = int.TryParse(RangeFrom, NumberStyles.Integer, CultureInfo.InvariantCulture, out int from);
                    bool hasTo = int.TryParse(RangeTo, NumberStyles.Integer, CultureInfo.InvariantCulture, out int to);
                    if (!hasFrom && !hasTo)
                        return CniRouteSpec.NoRoute;
                    return new CniRouteSpec(CniRouteMode.Range, hasFrom ? from : to, hasTo ? to : from);
                case CniRouteMode.List:
                    List<int> list = CniRouteSpec.ParseList(WaypointList);
                    return (list.Count > 0) ? new CniRouteSpec(CniRouteMode.List, List: list) : CniRouteSpec.NoRoute;
                default:
                    return CniRouteSpec.NoRoute;
            }
        }
    }
}
