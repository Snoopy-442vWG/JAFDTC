// ********************************************************************************************************************
//
// C130JConfigurationEditor.cs : supports editors for the c-130j configuration
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
using JAFDTC.Models.C130J;
using JAFDTC.Models.C130J.WYPT;
using JAFDTC.Models.DCS;
using JAFDTC.Models.POI;
using JAFDTC.UI.App;
using JAFDTC.UI.Base;
using JAFDTC.UI.Controls.Map;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace JAFDTC.UI.C130J
{
    internal class Glyphs
    {
        public const string RADIO = UI.Glyphs.Radio;
        public const string WYPT = UI.Glyphs.Navigation;
        public const string RTE = "\xE81E";
    }

    public class C130JConfigurationEditor : ConfigurationEditorBase, IMapControlVerbHandler
    {
        private C130JConfiguration ConfigC130J => (C130JConfiguration)Config;

        public C130JConfigurationEditor(IConfiguration config, ConfigurationPage configPage = null)
            => (Config, ConfigPage) = (config, configPage);

        public override ObservableCollection<ConfigEditorPageInfo> ConfigEditorPageInfo
            => [
                // order in the ui, matching the dcs-dtc layout: waypoints, route, radios.
                C130JEditWaypointListHelper.PageInfo,
                C130JEditRoutePage.PageInfo,
                C130JEditRadioPageHelper.PageInfo,
            ];

        public override void SetupMapWindow()
        {
            JAFDTC.App application = Application.Current as JAFDTC.App;
            MapWindow mapWindow = application.CreateMapWindow(true, true);

            string theater = mapWindow.Theater;
            Dictionary<string, List<INavpointInfo>> routes = new()
            {
                [WYPTSystem.SystemInfo.RouteNames[0]] = [.. ConfigC130J.WYPT.Points ]
            };
            List<INavpointInfo> allRoutes = [ ];
            foreach (string route in routes.Keys)
                allRoutes.AddRange(routes[route]);
            List<string> theaters = NavpointUIHelper.TheatersForNavpoints(allRoutes);
            if (theaters.Count > 0)
                theater = theaters[0];

            Dictionary<string, PointOfInterest> marks = [ ];
            if (theater != null)
            {
                PointOfInterestDbaseQuery query = new(PointOfInterestTypeMask.ANY, theater);
                foreach (PointOfInterest poi in PointOfInterestDbase.Instance.Find(query))
                    marks[poi.UniqueID] = poi;
            }

            bool isLinked = !string.IsNullOrEmpty(Config.SystemLinkedTo(WYPTSystem.SystemTag));

            mapWindow.Theater = theater;
            mapWindow.OpenMask = MapMarkerInfo.MarkerTypeMask.NAV_PT;
            mapWindow.EditMask = ((isLinked) ? 0 : MapMarkerInfo.MarkerTypeMask.NAV_PT) |
                                 ((isLinked) ? 0 : MapMarkerInfo.MarkerTypeMask.PATH_EDIT_HANDLE);
            mapWindow.CoordFormat = WYPTSystem.SystemInfo.NavptCoordFmt;
            mapWindow.MaxRouteLength = WYPTSystem.SystemInfo.NavptMaxCount;

            mapWindow.SetupMapContent(routes, marks, [ ], ConfigC130J.LastMapSetup);
        }

        // IMapControlMarkerExplainer overrides

        public override string MarkerDisplayType(MapMarkerInfo info)
            => (info.Tag.Type == MapMarkerInfo.MarkerType.NAV_PT) ? ConfigC130J.WYPT.SysInfo.NavptName
                                                                  : base.MarkerDisplayType(info);

        public override string MarkerDisplayName(MapMarkerInfo info)
        {
            if (info.Tag.Type == MapMarkerInfo.MarkerType.NAV_PT)
            {
                string name = ConfigC130J.WYPT.Points[info.Tag.Int - 1].Name;
                return (string.IsNullOrEmpty(name)) ? $"WP{info.Tag.Int}" : name;
            }
            return base.MarkerDisplayName(info);
        }

        public override string MarkerDisplayElevation(MapMarkerInfo info, string units = "")
        {
            if (info.Tag.Type == MapMarkerInfo.MarkerType.NAV_PT)
            {
                string elev = ConfigC130J.WYPT.Points[info.Tag.Int - 1].Alt;
                return (string.IsNullOrEmpty(elev)) ? "Ground" : $"{elev}{units}";
            }
            return base.MarkerDisplayElevation(info, units);
        }

        // IMapControlVerbHandler

        public string VerbHandlerTag => "C130JConfigurationEditor";

        public void VerbMarkerSelected(IMapControlVerbHandler sender, MapMarkerInfo info, int param = 0)
        {
            Debug.WriteLine($"{VerbHandlerTag}:VerbMarkerSelected({param}) {info.Tag}");
        }

        public void VerbMarkerOpened(IMapControlVerbHandler sender, MapMarkerInfo info, int param = 0)
        {
            JAFDTC.App application = Application.Current as JAFDTC.App;
            application.Window.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
            {
                ConfigPage.SwitchToEditorForSystem(WYPTSystem.SystemTag);
                application.Window.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
                {
                    application.MapWindow?.MirrorVerbMarkerOpened(sender, info, param);
                });
            });
        }

        public void VerbMarkerUpdated(IMapControlVerbHandler sender, MapMarkerInfo info, int param = 0)
        {
            Debug.WriteLine($"{VerbHandlerTag}:VerbMarkerUpdated({param}) {info.Tag}");
        }

        public void VerbMarkerMoved(IMapControlVerbHandler sender, MapMarkerInfo info, int param = 0)
        {
            if (info.Tag.Str == WYPTSystem.SystemInfo.RouteNames[0])
            {
                string alt = "";
                if (!info.TagAux.IsUnknown)
                {
                    PointOfInterest poi = PointOfInterestDbase.Instance.Find(info.TagAux.Str);
                    if (poi != null)
                        alt = poi.Elevation;
                }
                ConfigC130J.WYPT.Points[info.Tag.Int - 1].Lat = info.Lat;
                ConfigC130J.WYPT.Points[info.Tag.Int - 1].Lon = info.Lon;
                ConfigC130J.WYPT.Points[info.Tag.Int - 1].Alt = alt;
                Config.Save(this, WYPTSystem.SystemTag);
                ConfigPage.ForceSystemListIconRebuild(WYPTSystem.SystemTag);
            }
        }

        public void VerbMarkerAdded(IMapControlVerbHandler sender, MapMarkerInfo info, int param = 0)
        {
            if (info.Tag.Str == WYPTSystem.SystemInfo.RouteNames[0])
            {
                WaypointInfo wypt = ConfigC130J.WYPT.Add(null, info.Tag.Int - 1);
                wypt.Lat = info.Lat;
                wypt.Lon = info.Lon;
                Config.Save(this, WYPTSystem.SystemTag);
                ConfigPage.ForceSystemListIconRebuild(WYPTSystem.SystemTag);
            }
        }

        public void VerbMarkerDeleted(IMapControlVerbHandler sender, MapMarkerInfo info, int param = 0)
        {
            if (info.Tag.Str == WYPTSystem.SystemInfo.RouteNames[0])
            {
                ConfigC130J.WYPT.Delete(ConfigC130J.WYPT.Points[info.Tag.Int - 1]);
                Config.Save(this, WYPTSystem.SystemTag);
                ConfigPage.ForceSystemListIconRebuild(WYPTSystem.SystemTag);
            }
        }
    }
}
