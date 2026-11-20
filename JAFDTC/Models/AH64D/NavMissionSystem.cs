// ********************************************************************************************************************
//
// NavMissionSystem.cs : AH-64D dtc navigation mission systems (dtc Mission_1 / Mission_2)
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
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace JAFDTC.Models.AH64D
{
    /// <summary>
    /// one AH-64D dtc navigation "mission": the 149 point database, routes, lines, areas / zones and ADF presets.
    /// the data itself lives in an Ah64Mission (JAFDTC.AH64D) so it can be shared with the dtc import / merge code.
    /// </summary>
    public abstract class NavMissionSystemBase : SystemBase
    {
        [JsonIgnore]
        public abstract int MissionIndex { get; }

        public Ah64Mission Mission { get; set; } = new();

        [JsonIgnore]
        public override bool IsDefault => Mission.IsDefault;

        public override void Reset()
        {
            string theater = Mission.Theater;
            Mission.Reset();
            Mission.Theater = theater;
        }

        public override JsonNode MergeIntoSimDTC(JsonNode dataRoot)
            => Ah64Dtc.MergeMission(dataRoot, MissionIndex, Mission);

        protected static Ah64Mission CloneMission(Ah64Mission m)
            => JsonSerializer.Deserialize<Ah64Mission>(JsonSerializer.Serialize(m));
    }

    public sealed class NAV1System : NavMissionSystemBase
    {
        public const string SystemTag = "JAFDTC:AH64D:NAV1";

        [JsonIgnore]
        public override int MissionIndex => 1;

        public NAV1System() { }

        public NAV1System(NAV1System other) => Mission = CloneMission(other.Mission);

        public object Clone() => new NAV1System(this);
    }

    public sealed class NAV2System : NavMissionSystemBase
    {
        public const string SystemTag = "JAFDTC:AH64D:NAV2";

        [JsonIgnore]
        public override int MissionIndex => 2;

        public NAV2System() { }

        public NAV2System(NAV2System other) => Mission = CloneMission(other.Mission);

        public object Clone() => new NAV2System(this);
    }
}
