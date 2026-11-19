// ********************************************************************************************************************
//
// COMMSystem.cs : AH-64D dtc comm presets (dtc Presets.COMM)
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
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace JAFDTC.Models.AH64D
{
    /// <summary>
    /// the ten AH-64D comm presets. a preset that is not "set" keeps whatever the dtc base tape (template) has.
    /// </summary>
    public sealed class COMMSystem : SystemBase
    {
        public const string SystemTag = "JAFDTC:AH64D:COMM";

        public List<Ah64CommPreset> Presets { get; set; } = NewPresets();

        [JsonIgnore]
        public override bool IsDefault => !Presets.Any(p => p.IsSet);

        public COMMSystem() { }

        public COMMSystem(COMMSystem other)
            => Presets = JsonSerializer.Deserialize<List<Ah64CommPreset>>(JsonSerializer.Serialize(other.Presets));

        public object Clone() => new COMMSystem(this);

        public static List<Ah64CommPreset> NewPresets()
            => [.. Enumerable.Range(0, Ah64Limits.CommPresets).Select(_ => new Ah64CommPreset()) ];

        public void Normalize()
        {
            Presets ??= NewPresets();
            while (Presets.Count < Ah64Limits.CommPresets)
                Presets.Add(new Ah64CommPreset());
            if (Presets.Count > Ah64Limits.CommPresets)
                Presets.RemoveRange(Ah64Limits.CommPresets, Presets.Count - Ah64Limits.CommPresets);
        }

        public override void Reset() => Presets = NewPresets();

        public override JsonNode MergeIntoSimDTC(JsonNode dataRoot) => Ah64Dtc.MergeComms(dataRoot, Presets);
    }
}
