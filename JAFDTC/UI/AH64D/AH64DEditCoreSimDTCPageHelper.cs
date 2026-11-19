// ********************************************************************************************************************
//
// AH64DEditCoreSimDTCPageHelper.cs : AH-64D helper for the common dcs dtc tape editor page
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
using JAFDTC.Models.Base;
using JAFDTC.UI.App;
using JAFDTC.UI.Base;
using System.Collections.Generic;

namespace JAFDTC.UI.AH64D
{
    internal class AH64DEditCoreSimDTCPageHelper : IEditCoreSimDTCPageHelper
    {
        public static ConfigEditorPageInfo PageInfo
            => new(CoreSimDTCSystem.SystemTag, "DCS DTC Tape", "DCS DTC", Glyphs.DTC, typeof(EditCoreSimDTCPage),
                   typeof(AH64DEditCoreSimDTCPageHelper));

        public SystemBase GetSystemConfig(IConfiguration config) => ((AH64DConfiguration)config).DTE;

        public List<ConfigEditorPageInfo> MergableSystems =>
        [
            AH64DMission1Helper.PageInfo,
            AH64DMission2Helper.PageInfo,
            AH64DEditCommPage.PageInfo
        ];

        public void ValidateDTCSystem(IConfiguration config)
        {
            CoreSimDTCSystem dte = ((AH64DConfiguration)config).DTE;
            dte.ValidateForAirframe(config.Airframe);

            // a tape that has never been set up merges everything jafdtc edits for the apache; the user can untick
            // systems on the page.
            if (dte.IsDefault)
                foreach (string tag in config.MergeableSysTags)
                    dte.MergedSystemTags.Add(tag);
        }

        public void CopyConfigToEdit(IConfiguration config, CoreSimDTCSystem editDTC)
        {
            CoreSimDTCSystem dte = ((AH64DConfiguration)config).DTE;
            editDTC.Template = new(dte.Template);
            editDTC.OutputPath = new(dte.OutputPath);
            editDTC.MergedSystemTags = [ ];
            foreach (string tag in dte.MergedSystemTags)
                editDTC.MergedSystemTags.Add(tag);
            editDTC.EnableRebuild = new(dte.EnableRebuild);
            editDTC.EnableLoad = new(dte.EnableLoad);
        }

        public void CopyEditToConfig(CoreSimDTCSystem editDTC, IConfiguration config)
        {
            CoreSimDTCSystem dte = ((AH64DConfiguration)config).DTE;
            dte.Template = new(editDTC.Template);
            dte.OutputPath = new(editDTC.OutputPath);
            dte.MergedSystemTags = [ ];
            foreach (string tag in editDTC.MergedSystemTags)
                dte.MergedSystemTags.Add(tag);
            dte.EnableRebuild = new(editDTC.EnableRebuild);
            dte.EnableLoad = new(editDTC.EnableLoad);
        }
    }
}
