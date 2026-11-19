// ********************************************************************************************************************
//
// AH64DConfigurationEditor.cs : ui configuration editor for the AH-64D Apache
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

using JAFDTC.Models;
using JAFDTC.Models.AH64D;
using JAFDTC.UI.App;
using System.Collections.ObjectModel;

namespace JAFDTC.UI.AH64D
{
    internal class Glyphs
    {
        public const string NAV1 = "\xF146";                    // number "1" (circle) - mission 1
        public const string NAV2 = "\xF147";                    // number "2" (circle) - mission 2
        public const string COMM = UI.Glyphs.Radio;
        public const string DTC = UI.Glyphs.DTC;
    }

    /// <summary>
    /// configuration editor for the AH-64D. the systems are dtc mission 1, dtc mission 2, the comm presets, and the
    /// dcs dtc tape (base template + output file).
    /// </summary>
    public class AH64DConfigurationEditor : ConfigurationEditorBase
    {
        public AH64DConfigurationEditor(IConfiguration config, ConfigurationPage configPage = null)
            => (Config, ConfigPage) = (config, configPage);

        public override ObservableCollection<ConfigEditorPageInfo> ConfigEditorPageInfo
            => [
                AH64DMission1Helper.PageInfo,
                AH64DMission2Helper.PageInfo,
                AH64DEditCommPage.PageInfo,
                AH64DEditCoreSimDTCPageHelper.PageInfo
            ];
    }

    /// <summary>
    /// selects which dtc mission an AH64DEditMissionPage edits (the page is shared by both missions).
    /// </summary>
    public interface IAH64DMissionHelper
    {
        public int MissionIndex { get; }
        public string SystemTag { get; }
        public string SystemName { get; }
        public NavMissionSystemBase System(IConfiguration config);
    }

    internal sealed class AH64DMission1Helper : IAH64DMissionHelper
    {
        public static ConfigEditorPageInfo PageInfo
            => new(NAV1System.SystemTag, "DTC Mission 1", "MSN 1", Glyphs.NAV1, typeof(AH64DEditMissionPage),
                   typeof(AH64DMission1Helper));

        public int MissionIndex => 1;
        public string SystemTag => NAV1System.SystemTag;
        public string SystemName => "DTC Mission 1";
        public NavMissionSystemBase System(IConfiguration config) => ((AH64DConfiguration)config).NAV1;
    }

    internal sealed class AH64DMission2Helper : IAH64DMissionHelper
    {
        public static ConfigEditorPageInfo PageInfo
            => new(NAV2System.SystemTag, "DTC Mission 2", "MSN 2", Glyphs.NAV2, typeof(AH64DEditMissionPage),
                   typeof(AH64DMission2Helper));

        public int MissionIndex => 2;
        public string SystemTag => NAV2System.SystemTag;
        public string SystemName => "DTC Mission 2";
        public NavMissionSystemBase System(IConfiguration config) => ((AH64DConfiguration)config).NAV2;
    }
}
