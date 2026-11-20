// ********************************************************************************************************************
//
// CH47FEditRoutePage.xaml.cs : ui c# for the ch-47f route (FPLN) page
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

using JAFDTC.Airlift.C130J;
using JAFDTC.Airlift.Cni;
using JAFDTC.Models;
using JAFDTC.Models.CH47F;
using JAFDTC.Models.CH47F.RTE;
using JAFDTC.UI.App;
using JAFDTC.UI.Base;
using System.Collections.Generic;
using System.Linq;

namespace JAFDTC.UI.CH47F
{
    /// <summary>
    /// route page: choose which waypoints the upload strings into the route, and preview the result.
    /// </summary>
    public sealed partial class CH47FEditRoutePage : SystemEditorPageBase
    {
        private const string SYSTEM_NAME = "Route";

        protected override SystemBase SystemConfig => ((CH47FConfiguration)Config).RTE;
        protected override string SystemTag => RTESystem.SystemTag;
        protected override string SystemName => SYSTEM_NAME;

        public static ConfigEditorPageInfo PageInfo
            => new(RTESystem.SystemTag, SYSTEM_NAME, "RTE", Glyphs.RTE, typeof(CH47FEditRoutePage));

        public CH47FEditRoutePage()
        {
            InitializeComponent();
            InitializeBase(new RTESystem(), uiTextRangeFrom, uiCtlLinkResetBtns);
        }

        protected override void UpdateUICustom(bool isEditable)
        {
            RTESystem edit = (RTESystem)EditState;
            CniRouteMode mode = edit.ModeValue;

            uiTextRangeFrom.IsEnabled = isEditable && (mode == CniRouteMode.Range);
            uiTextRangeTo.IsEnabled = isEditable && (mode == CniRouteMode.Range);
            uiTextList.IsEnabled = isEditable && (mode == CniRouteMode.List);

            uiTextSummary.Text = Summary(edit);
        }

        private string Summary(RTESystem edit)
        {
            if (edit.ModeValue == CniRouteMode.None)
                return "Waypoints are loaded into the ACP list only; the flight plan is not changed.";

            List<CniWaypoint> wypts = ((CH47FConfiguration)Config).WYPT.ToCniWaypoints();
            if (wypts.Count == 0)
                return "There are no valid waypoints yet, so no route will be built.";

            List<CniWaypoint> route = C130JCni.RouteWaypoints(wypts, edit.ToSpec());
            if (route.Count == 0)
                return "No waypoints match this selection, so no route will be built.";

            string legs = string.Join(" > ", route.Select(w => string.IsNullOrWhiteSpace(w.Name) ? $"#{w.Number}" : $"#{w.Number} {w.Name}"));
            return $"Flight plan ({route.Count} points, entered by position): {legs}";
        }
    }
}
