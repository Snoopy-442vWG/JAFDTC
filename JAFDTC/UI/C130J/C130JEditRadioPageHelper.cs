// ********************************************************************************************************************
//
// C130JEditRadioPageHelper.cs : IEditRadioPageHelper for the c-130j radios (UHF-1, VHF-1)
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
using JAFDTC.Models;
using JAFDTC.Models.Base;
using JAFDTC.Models.C130J;
using JAFDTC.Models.C130J.Radio;
using JAFDTC.UI.App;
using JAFDTC.UI.Base;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using static JAFDTC.Models.C130J.Radio.RadioSystem;

namespace JAFDTC.UI.C130J
{
    /// <summary>
    /// helper for the generic EditRadioPage. the preset description is the 5-character preset name; "default
    /// tuning" is the selection loaded after the presets (a frequency or a preset number).
    /// </summary>
    internal class C130JEditRadioPageHelper : RadioPageHelperBase, IEditRadioPageHelper
    {
        public static ConfigEditorPageInfo PageInfo
            => new(RadioSystem.SystemTag, "Radios", "COMM", Glyphs.RADIO, typeof(EditRadioPage),
                   typeof(C130JEditRadioPageHelper));

        public string SystemTag => RadioSystem.SystemTag;

        public List<string> RadioNames =>
        [
            "UHF-1 – 225.000 to 399.975 MHz",
            "VHF-1 – 108.000 to 173.975 MHz",
        ];

        private static C130JConfiguration Herc(IConfiguration config) => (C130JConfiguration)config;

        public void CopyConfigToEdit(int radio, IConfiguration config,
                                     ObservableCollection<RadioPresetItem> editPresets, RadioMiscItem editMisc)
        {
            editMisc.DefaultTuning = Herc(config).Radio.DefaultSetting[radio];
            editPresets.Clear();
            foreach (RadioPresetInfoBase cfgPreset in Herc(config).Radio.Presets[radio])
                editPresets.Add(new RadioPresetItem(this, 0, radio)
                {
                    Preset = cfgPreset.Preset.ToString(),
                    Frequency = new(cfgPreset.Frequency),
                    Description = new(cfgPreset.Description ?? ""),
                    Modulation = new(cfgPreset.Modulation ?? "")
                });
        }

        public void CopyEditToConfig(int radio, ObservableCollection<RadioPresetItem> editPresets,
                                     RadioMiscItem editMisc, IConfiguration config)
        {
            Herc(config).Radio.DefaultSetting[radio] = editMisc.DefaultTuning ?? "";
            ObservableCollection<RadioPreset> cfgPresetList = Herc(config).Radio.Presets[radio];
            cfgPresetList.Clear();
            foreach (RadioPresetItem item in editPresets)
                cfgPresetList.Add(new RadioPreset()
                {
                    Preset = int.Parse(item.Preset),
                    Frequency = new(item.Frequency),
                    Description = new(item.Description ?? ""),
                    Modulation = new(item.Modulation ?? "")
                });
        }

        public void RadioSysReset(IConfiguration config) => Herc(config).Radio.Reset();

        public bool RadioModuleIsDefault(IConfiguration config, int radio)
            => (Herc(config).Radio.Presets[radio].Count == 0) &&
               string.IsNullOrEmpty(Herc(config).Radio.DefaultSetting[radio]);

        public bool RadioSysIsDefault(IConfiguration config) => Herc(config).Radio.IsDefault;

        public override int RadioMaxPresets(int radio) => C130JCni.MaxPresets;

        public string RadioDefaultFrequency(int radio)
            => radio switch
            {
                (int)Radios.UHF1 => "251.000",
                (int)Radios.VHF1 => "121.500",
                _ => throw new NotImplementedException($"Unexpected radio {radio}")
            };

        public int RadioPresetCount(int radio, IConfiguration config) => Herc(config).Radio.Presets[radio].Count;

        public override bool ValidateFrequency(int radio, string freq, bool isNoEValid = true)
        {
            if (string.IsNullOrEmpty(freq))
                return isNoEValid;
            return RadioSystem.IsFreqValidForRadio((Radios)radio, freq);
        }
    }
}
