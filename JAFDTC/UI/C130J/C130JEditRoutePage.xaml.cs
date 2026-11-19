// ********************************************************************************************************************
//
// C130JEditRoutePage.xaml.cs : ui c# for the c-130j route (LEGS) page
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
using JAFDTC.Models.C130J;
using JAFDTC.Models.C130J.RTE;
using JAFDTC.UI.App;
using JAFDTC.UI.Base;
using System.Collections.Generic;
using System.Linq;

namespace JAFDTC.UI.C130J
{
    /// <summary>
    /// route page: choose which waypoints the upload strings into the route, and preview the result.
    /// </summary>
    public sealed partial class C130JEditRoutePage : SystemEditorPageBase
    {
        private const string SYSTEM_NAME = "Route";

        protected override SystemBase SystemConfig => ((C130JConfiguration)Config).RTE;
        protected override string SystemTag => RTESystem.SystemTag;
        protected override string SystemName => SYSTEM_NAME;

        public static ConfigEditorPageInfo PageInfo
            => new(RTESystem.SystemTag, SYSTEM_NAME, "RTE", Glyphs.RTE, typeof(C130JEditRoutePage));

        public C130JEditRoutePage()
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
                return "Waypoints are loaded into the CNI database only; the LEGS page is not changed.";

            List<CniWaypoint> wypts = ((C130JConfiguration)Config).WYPT.ToCniWaypoints();
            if (wypts.Count == 0)
                return "There are no valid waypoints yet, so no route will be built.";

            List<CniWaypoint> route = C130JCni.RouteWaypoints(wypts, edit.ToSpec());
            if (route.Count == 0)
                return "No waypoints match this selection, so no route will be built.";

            Dictionary<int, string> idents = C130JCni.Idents(wypts);
            string legs = string.Join(" > ", route.Select(w => idents[w.Number]));
            return $"Route ({route.Count} legs): {legs}";
        }
    }
}
