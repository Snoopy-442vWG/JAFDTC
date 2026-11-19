// ********************************************************************************************************************
//
// CH47FEditWaypointListHelper.cs : IEditNavpointListPageHelper for the ch-47f waypoint list
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

using JAFDTC.Models;
using JAFDTC.Models.Base;
using JAFDTC.Models.CH47F;
using JAFDTC.Models.CH47F.WYPT;
using JAFDTC.Models.POI;
using JAFDTC.UI.App;
using JAFDTC.UI.Base;
using JAFDTC.UI.Controls.Map;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using static JAFDTC.Utilities.Networking.WyptCaptureDataRx;

namespace JAFDTC.UI.CH47F
{
    /// <summary>
    /// helper for the generic EditNavpointListPage editing the ch-47f waypoint list.
    /// </summary>
    internal class CH47FEditWaypointListHelper : EditWaypointListHelperBase
    {
        public static ConfigEditorPageInfo PageInfo
            => new(WYPTSystem.SystemTag, "Waypoints", "WYPT", Glyphs.WYPT,
                   typeof(EditNavpointListPage), typeof(CH47FEditWaypointListHelper));

        public override INavpointSystemImport NavptSystem(IConfiguration config)
            => ((CH47FConfiguration)config).WYPT;

        public override NavpointSystemInfo SystemInfo => WYPTSystem.SystemInfo;

        public override object NavptEditorArg(Page parentEditor, IMapControlVerbMirror verbMirror,
                                              IConfiguration config, int indexNavpt)
        {
            bool isUnlinked = string.IsNullOrEmpty(config.SystemLinkedTo(SystemInfo.SystemTag));
            return new EditNavpointPageNavArgs(parentEditor, verbMirror, config, indexNavpt, isUnlinked,
                                               typeof(CH47FEditWaypointHelper));
        }

        public override void CopyConfigToEdit(IConfiguration config, ObservableCollection<INavpointInfo> edit)
        {
            edit.Clear();
            foreach (WaypointInfo wypt in ((CH47FConfiguration)config).WYPT.Points)
                edit.Add(new WaypointInfo(wypt));
        }

        public override bool CopyEditToConfig(ObservableCollection<INavpointInfo> edit, IConfiguration config)
        {
            CH47FConfiguration hook = (CH47FConfiguration)config;
            hook.WYPT.Points.Clear();
            foreach (WaypointInfo wypt in edit.Cast<WaypointInfo>())
                hook.WYPT.Points.Add(new WaypointInfo(wypt));
            return true;
        }

        public override void ResetSystem(IConfiguration config)
            => ((CH47FConfiguration)config).WYPT.Reset();

        public override int AddNavpoint(IConfiguration config, int atIndex = -1)
        {
            WaypointInfo wypt = ((CH47FConfiguration)config).WYPT.Add(null, atIndex);
            return ((CH47FConfiguration)config).WYPT.Points.IndexOf(wypt);
        }

        public override void AddNavpointsFromPOIs(IEnumerable<PointOfInterest> pois, IConfiguration config)
        {
            WYPTSystem wyptSys = ((CH47FConfiguration)config).WYPT;
            foreach (PointOfInterest poi in pois)
            {
                string name = poi.Name;
                wyptSys.Add(new WaypointInfo()
                {
                    Name = name,
                    Lat = poi.Latitude,
                    Lon = poi.Longitude,
                    Alt = poi.Elevation
                });
            }
        }

        public override bool PasteNavpoints(IConfiguration config, string cbData, bool isReplace = false)
            => ((CH47FConfiguration)config).WYPT.ImportSerializedNavpoints(cbData, isReplace);

        public override void CaptureNavpoints(IConfiguration config, WyptCaptureData[] wypts, int startIndex)
        {
            WYPTSystem wyptSys = ((CH47FConfiguration)config).WYPT;
            for (int i = 0; i < wypts.Length; i++)
            {
                if (wypts[i].IsTarget)
                    continue;
                if (startIndex < wyptSys.Count)
                {
                    wyptSys.Points[startIndex].Name = $"CAP{i + 1}";
                    wyptSys.Points[startIndex].Lat = wypts[i].Latitude;
                    wyptSys.Points[startIndex].Lon = wypts[i].Longitude;
                    wyptSys.Points[startIndex].Alt = wypts[i].Elevation;
                }
                else
                {
                    wyptSys.Add(new WaypointInfo()
                    {
                        Name = $"CAP{i + 1}",
                        Lat = wypts[i].Latitude,
                        Lon = wypts[i].Longitude,
                        Alt = wypts[i].Elevation
                    });
                }
                startIndex++;
            }
        }
    }
}
