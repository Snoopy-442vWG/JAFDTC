// ********************************************************************************************************************
//
// RadioSystem.cs -- ch-47f radio presets (UHF-1, VHF-1)
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

using JAFDTC.Airlift.CH47F;
using JAFDTC.Airlift.Cni;
using JAFDTC.Models.Base;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json.Serialization;

namespace JAFDTC.Models.CH47F.Radio
{
    public class RadioPreset : RadioPresetInfoBase
    {
    }

    /// <summary>
    /// presets and selected tuning for the two radios the CH-47F upload programs from the CDU MSN page: UHF-1
    /// and VHF-1 (AN/ARC-186). the preset Description is the preset name (up to eight characters). DefaultSetting holds the selection: a frequency ("251.000")
    /// or a preset number ("3"); blank leaves the radio as it is.
    /// </summary>
    public class RadioSystem : RadioSystemBase<RadioPreset>, ISystem
    {
        public const string SystemTag = "JAFDTC:CH47F:RADIO";

        public enum Radios
        {
            UHF1 = 0,
            VHF1 = 1,
            NUM_RADIOS = 2
        };

        public string[] DefaultSetting { get; set; }

        [JsonIgnore]
        public override bool IsDefault
        {
            get
            {
                foreach (ObservableCollection<RadioPreset> presets in Presets)
                    if (presets.Count > 0)
                        return false;
                foreach (string setting in DefaultSetting)
                    if (!string.IsNullOrEmpty(setting))
                        return false;
                return true;
            }
        }

        public RadioSystem()
        {
            Presets = [ new(), new() ];
            Reset();
        }

        public RadioSystem(RadioSystem other)
        {
            Presets = [ ];
            foreach (ObservableCollection<RadioPreset> radio in other.Presets)
            {
                ObservableCollection<RadioPreset> newPresets = [ ];
                foreach (RadioPreset preset in radio)
                    newPresets.Add(new()
                    {
                        Preset = preset.Preset,
                        Frequency = preset.Frequency,
                        Description = preset.Description,
                        Modulation = preset.Modulation,
                    });
                Presets.Add(newPresets);
            }
            DefaultSetting = [ new(other.DefaultSetting[0] ?? ""), new(other.DefaultSetting[1] ?? "") ];
        }

        public virtual object Clone() => new RadioSystem(this);

        public override void Reset()
        {
            foreach (ObservableCollection<RadioPreset> radio in Presets)
                radio.Clear();
            DefaultSetting = [ "", "" ];
        }

        /// <summary>
        /// make sure there are two preset lists and two settings after loading older or hand-edited json.
        /// </summary>
        public override void Sanitize()
        {
            Presets ??= [ ];
            while (Presets.Count < (int)Radios.NUM_RADIOS)
                Presets.Add([ ]);
            if ((DefaultSetting == null) || (DefaultSetting.Length != (int)Radios.NUM_RADIOS))
                DefaultSetting = [ "", "" ];
        }

        public static CH47FRadio CduRadio(Radios radio) => (radio == Radios.UHF1) ? CH47FRadio.UHF1 : CH47FRadio.VHF1;

        public static bool IsFreqValidForRadio(Radios radio, string freq)
            => CH47FCdu.IsFrequencyValid(CduRadio(radio), freq);

        /// <summary>
        /// the radio as planner input.
        /// </summary>
        public CniRadioSpec ToSpec(Radios radio)
        {
            List<CniPreset> presets = [.. Presets[(int)radio].Select(p => new CniPreset(p.Preset, p.Description, p.Frequency)) ];
            return new CniRadioSpec(presets, DefaultSetting[(int)radio]);
        }
    }
}
