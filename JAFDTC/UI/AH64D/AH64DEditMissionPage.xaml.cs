// ********************************************************************************************************************
//
// AH64DEditMissionPage.xaml.cs : ui c# for the AH-64D dtc mission (points, routes, lines, areas / zones, adf) editor
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

using JAFDTC.AH64D.Csv;
using JAFDTC.AH64D.Dtc;
using JAFDTC.AH64D.Geo;
using JAFDTC.Models;
using JAFDTC.Models.AH64D;
using JAFDTC.UI.App;
using JAFDTC.UI.Base;
using JAFDTC.Utilities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Windows.Storage.Pickers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;

namespace JAFDTC.UI.AH64D
{
    // ----------------------------------------------------------------------------------------------------------------
    // display rows (bound with {Binding}, so plain public string properties)
    // ----------------------------------------------------------------------------------------------------------------

    public sealed class ApPointRow
    {
        public Ah64Point Point { get; init; }
        public string Label => Point.Label;
        public string Ident => Point.Ident;
        public string Free => Point.Free;
        public string Mgrs => Point.Mgrs8;
        public string Alt => Point.AltFt.ToString("F0", CultureInfo.InvariantCulture);
        public string Note => Point.Note;
    }

    public sealed class ApRouteRow
    {
        public string Seq { get; init; }
        public string Label { get; init; }
        public string Ident { get; init; }
        public string Free { get; init; }
        public string Speed { get; init; }
        public string Leg { get; init; }
        public string Ete { get; init; }
        public string Note { get; init; }
    }

    public sealed class ApShapeRow
    {
        public string Seq { get; init; }
        public string Kind { get; init; }
        public string Text { get; init; }
        public string Count { get; init; }
        public string Note { get; init; }
    }

    /// <summary>
    /// editor for one AH-64D dtc mission (Mission_1 or Mission_2, chosen by the page helper). edits go straight to
    /// the configuration and are saved as they are made.
    /// </summary>
    public sealed partial class AH64DEditMissionPage : SystemEditorPageBase
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        private IAH64DMissionHelper _helper;

        private IAH64DMissionHelper Helper
            => _helper ??= (IAH64DMissionHelper)Activator.CreateInstance(NavArgs.EditorHelperType);

        protected override SystemBase SystemConfig => Helper.System(Config);

        protected override string SystemTag => Helper.SystemTag;

        protected override string SystemName => Helper.SystemName;

        private NavMissionSystemBase NavSys => Helper.System(Config);

        private Ah64Mission Mission => NavSys.Mission;

        private bool _isEditable = true;
        private bool _isRefreshing;

        private Ah64Point _editPoint;                        // point loaded in the editor (null: new point)
        private int _editLine = -1;
        private int _editShape = -1;

        private readonly List<(TextBox Id, TextBox Freq)> _adfRows = [];

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public AH64DEditMissionPage()
        {
            InitializeComponent();
            InitializeBase(null, null, uiCtlLinkResetBtns);

            foreach (string theater in DcsTheaters.Names)
                uiComboTheater.Items.Add(theater);

            for (int i = 0; i < Ah64Limits.AdfPresets; i++)
            {
                uiAdfGrid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
                TextBlock lbl = new() { Text = $"ADF {i + 1}", VerticalAlignment = VerticalAlignment.Center,
                                        Margin = new Thickness(0, 6, 0, 0) };
                TextBox id = new() { MaxLength = 3, CharacterCasing = CharacterCasing.Upper, PlaceholderText = "ID",
                                     Margin = new Thickness(0, 6, 8, 0) };
                TextBox freq = new() { MaxLength = 6, PlaceholderText = "kHz", Margin = new Thickness(0, 6, 0, 0) };
                ToolTipService.SetToolTip(freq, "NDB frequency, 100-2199.5 kHz");
                Grid.SetRow(lbl, i); Grid.SetColumn(lbl, 0);
                Grid.SetRow(id, i); Grid.SetColumn(id, 1);
                Grid.SetRow(freq, i); Grid.SetColumn(freq, 2);
                uiAdfGrid.Children.Add(lbl);
                uiAdfGrid.Children.Add(id);
                uiAdfGrid.Children.Add(freq);
                _adfRows.Add((id, freq));
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // save / refresh
        //
        // ------------------------------------------------------------------------------------------------------------

        private void SaveMission()
        {
            Config.Save(this, SystemTag);
            RefreshAll();
        }

        protected override void UpdateUICustom(bool isEditable)
        {
            _isEditable = isEditable;
            RefreshAll();
        }

        private void RefreshAll()
        {
            if (_isRefreshing)
                return;
            _isRefreshing = true;
            try
            {
                string theater = DcsTheaters.Canonical(Mission.Theater);
                uiComboTheater.SelectedItem = DcsTheaters.Names.Contains(theater) ? theater : null;

                foreach (Control c in new Control[] { uiComboTheater, uiBarImportDtc, uiBarImportCsv, uiBarCopyOther,
                                                      uiCkLoadW, uiCkLoadC, uiCkLoadT, uiAdfEnabled })
                    c.IsEnabled = _isEditable;
                foreach (FrameworkElement p in new FrameworkElement[] { uiPanelPoint, uiPanelRoute, uiPanelLine,
                                                                        uiPanelShape, uiPanelAdf })
                    if (p is Control ctl) ctl.IsEnabled = _isEditable;
                    else if (p is Panel) SetPanelEnabled((Panel)p, _isEditable);
                SetPanelEnabled((Panel)uiPanelPoint.Content, _isEditable);

                RefreshPoints();
                RefreshRoutes();
                RefreshLines();
                RefreshShapes();
                RefreshAdf();
                RefreshIssues();
            }
            finally
            {
                _isRefreshing = false;
            }
        }

        private static void SetPanelEnabled(Panel panel, bool isEnabled)
        {
            foreach (UIElement child in panel.Children)
            {
                if (child is Control c)
                    c.IsEnabled = isEnabled;
                else if (child is Panel p)
                    SetPanelEnabled(p, isEnabled);
            }
        }

        private void RefreshIssues()
        {
            List<string> errs = (Mission.IsDefault) ? [] : Ah64Validator.ValidateMission(Mission);
            uiInfoIssues.IsOpen = errs.Count > 0;
            uiInfoIssues.Message = string.Join("\n", errs.Take(6)) +
                                   ((errs.Count > 6) ? $"\n... and {errs.Count - 6} more" : "");
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // theater, import / export
        //
        // ------------------------------------------------------------------------------------------------------------

        private void ComboTheater_SelectionChanged(object sender, SelectionChangedEventArgs args)
        {
            if (_isRefreshing || (uiComboTheater.SelectedItem is not string theater))
                return;
            if (Mission.Theater != theater)
            {
                Mission.Theater = theater;
                SaveMission();
            }
        }

        private static FileOpenPicker OpenPicker(string commit, params string[] types)
        {
            FileOpenPicker picker = new((Application.Current as JAFDTC.App).Window.AppWindow.Id)
            {
                CommitButtonText = commit,
                SuggestedStartLocation = PickerLocationId.Desktop,
                ViewMode = PickerViewMode.List
            };
            foreach (string t in types)
                picker.FileTypeFilter.Add(t);
            return picker;
        }

        private async void CmdImportDtc_Click(object sender, RoutedEventArgs args)
        {
            PickFileResult pick = await OpenPicker("Import", ".dtc").PickSingleFileAsync();
            if (pick == null)
                return;
            try
            {
                JsonNode data = Ah64Dtc.ParseFile(FileManager.ReadFile(pick.Path))["data"];

                int index = 1;
                bool hasM1 = data["NAV"]?["Mission_1"] != null;
                bool hasM2 = data["NAV"]?["Mission_2"] != null;
                if (hasM1 && hasM2)
                {
                    ContentDialog dlg = new()
                    {
                        XamlRoot = Content.XamlRoot,
                        Title = "Import Which Mission?",
                        Content = $"“{Path.GetFileName(pick.Path)}” has two DTC missions. Which one should replace " +
                                  $"{Helper.SystemName}?",
                        PrimaryButtonText = "Mission 1",
                        SecondaryButtonText = "Mission 2",
                        CloseButtonText = "Cancel",
                        DefaultButton = (Helper.MissionIndex == 1) ? ContentDialogButton.Primary
                                                                   : ContentDialogButton.Secondary
                    };
                    ContentDialogResult res = await dlg.ShowAsync();
                    if (res == ContentDialogResult.None)
                        return;
                    index = (res == ContentDialogResult.Primary) ? 1 : 2;
                }
                else if (hasM2)
                    index = 2;

                List<string> warnings = [];
                Ah64Mission m = Ah64Dtc.ReadMission(data, index, warnings);
                NavSys.Mission = m;
                ClearEditors();
                SaveMission();

                string msg = $"Imported Mission {index} from “{Path.GetFileName(pick.Path)}”: {m.Points.Count} points, " +
                             $"{m.Routes.Count(r => r.Points.Count > 0)} routes, {m.Lines.Count} lines, " +
                             $"{m.Shapes.Count} areas / zones ({m.Theater}).";
                if (warnings.Count > 0)
                    msg += "\n\n" + string.Join("\n", warnings);
                await Utilities.Message1BDialog(Content.XamlRoot, "DTC Imported", msg);
            }
            catch (Exception ex)
            {
                FileManager.Log($"AH64DEditMissionPage:CmdImportDtc_Click exception {ex}");
                await Utilities.Message1BDialog(Content.XamlRoot, "Import Failed", ex.Message);
            }
        }

        private async void CmdImportCsv_Click(object sender, RoutedEventArgs args)
        {
            PickFileResult pick = await OpenPicker("Import", ".csv").PickSingleFileAsync();
            if (pick == null)
                return;
            try
            {
                CsvImportResult res = Ah64Csv.ImportNav(Mission, FileManager.ReadFile(pick.Path));
                if (string.IsNullOrEmpty(Mission.Theater))
                    Mission.Theater = GuessTheater(Mission) ?? "";
                SaveMission();
                string msg = res.Summary() + ".";
                if (string.IsNullOrEmpty(Mission.Theater))
                    msg += "\n\nSelect the DCS theater at the top of the page.";
                if (res.Warnings.Count > 0)
                    msg += "\n\n" + string.Join("\n", res.Warnings);
                await Utilities.Message1BDialog(Content.XamlRoot, "CSV Imported", msg);
            }
            catch (Exception ex)
            {
                FileManager.Log($"AH64DEditMissionPage:CmdImportCsv_Click exception {ex}");
                await Utilities.Message1BDialog(Content.XamlRoot, "Import Failed", ex.Message);
            }
        }

        private async void CmdExportCsv_Click(object sender, RoutedEventArgs args)
        {
            try
            {
                FileSavePicker picker = new((Application.Current as JAFDTC.App).Window.AppWindow.Id)
                {
                    CommitButtonText = "Export",
                    SuggestedStartLocation = PickerLocationId.Desktop,
                    SuggestedFileName = $"{Config.Name} M{Helper.MissionIndex} points.csv",
                    DefaultFileExtension = ".csv"
                };
                picker.FileTypeChoices.Add("CSV", [ ".csv" ]);
                PickFileResult pick = await picker.PickSaveFileAsync();
                if (pick == null)
                    return;
                string routesPath = Path.Combine(Path.GetDirectoryName(pick.Path),
                                                 Path.GetFileNameWithoutExtension(pick.Path) + " routes.csv");
                FileManager.WriteFile(pick.Path, Ah64Csv.ExportPoints(Mission));
                FileManager.WriteFile(routesPath, Ah64Csv.ExportRoutes(Mission));
                await Utilities.Message1BDialog(Content.XamlRoot, "CSV Exported",
                                                $"Points: {pick.Path}\nRoutes: {routesPath}");
            }
            catch (Exception ex)
            {
                FileManager.Log($"AH64DEditMissionPage:CmdExportCsv_Click exception {ex}");
                await Utilities.Message1BDialog(Content.XamlRoot, "Export Failed", ex.Message);
            }
        }

        private async void CmdCopyOther_Click(object sender, RoutedEventArgs args)
        {
            AH64DConfiguration cfg = (AH64DConfiguration)Config;
            NavMissionSystemBase other = (Helper.MissionIndex == 1) ? cfg.NAV2 : cfg.NAV1;
            ContentDialogResult res = await Utilities.Message2BDialog(Content.XamlRoot, "Copy Other Mission",
                $"Replace everything in {Helper.SystemName} with a copy of DTC Mission {3 - Helper.MissionIndex}?",
                "Replace");
            if (res != ContentDialogResult.Primary)
                return;
            NavSys.Mission = other.Mission.Clone();
            ClearEditors();
            SaveMission();
        }

        /// <summary>
        /// best-guess theater from the points: the theater whose projection origin is nearest the points' centroid
        /// (dcs map x/y are small near the middle of a theater).
        /// </summary>
        private static string GuessTheater(Ah64Mission m)
        {
            if (m.Points.Count == 0)
                return null;
            double lat = m.Points.Average(p => p.Lat);
            double lon = m.Points.Average(p => p.Lon);
            string best = null;
            double bestD = double.MaxValue;
            foreach (string t in DcsTheaters.Names)
            {
                DcsXY xy = DcsTheaters.Get(t).ToXY(lat, lon);
                double d = Math.Sqrt(xy.X * xy.X + xy.Y * xy.Y);
                if (d < bestD)
                    (best, bestD) = (t, d);
            }
            return (bestD < 1500000.0) ? best : null;
        }

        private void ClearEditors()
        {
            _editPoint = null;
            _editLine = -1;
            _editShape = -1;
            uiPtStatus.Text = uiRtStatus.Text = uiLnStatus.Text = uiShStatus.Text = uiAdfStatus.Text = "";
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // points
        //
        // ------------------------------------------------------------------------------------------------------------

        private PointCategory EditCategory => (PointCategory)Math.Max(0, uiPtCategory.SelectedIndex);

        private void RefreshPoints()
        {
            uiCkLoadW.IsChecked = Mission.LoadWptHz;
            uiCkLoadC.IsChecked = Mission.LoadCtrlm;
            uiCkLoadT.IsChecked = Mission.LoadTgt;

            List<ApPointRow> rows = [];
            foreach (PointCategory cat in new[] { PointCategory.WPTHZ, PointCategory.CTRLM, PointCategory.TGT })
                rows.AddRange(Mission.PointsIn(cat).Select(p => new ApPointRow() { Point = p }));
            uiListPoints.ItemsSource = rows;

            if (uiPtCategory.SelectedIndex < 0)
            {
                uiPtCategory.SelectedIndex = 0;
                LoadPointEditor(null);
            }
        }

        private void FillIdentCombo(PointCategory cat, string selectCode)
        {
            uiPtIdent.Items.Clear();
            int sel = 0, i = 0;
            foreach (PointIdent ident in PointIdents.ForCategory(cat))
            {
                uiPtIdent.Items.Add(new ComboBoxItem() { Content = ident.DisplayName, Tag = ident.Code });
                if (ident.Code == selectCode)
                    sel = i;
                i++;
            }
            if (selectCode == null)
                sel = PointIdents.ForCategory(cat).ToList().IndexOf(PointIdents.Default(cat));
            uiPtIdent.SelectedIndex = sel;
        }

        private void LoadPointEditor(Ah64Point p)
        {
            _editPoint = p;
            bool wasRefreshing = _isRefreshing;
            _isRefreshing = true;
            try
            {
                if (p == null)
                {
                    PointCategory cat = EditCategory;
                    int next = Mission.NextFreeNumber(cat);
                    uiPtNumber.Text = (next > 0) ? next.ToString(CultureInfo.InvariantCulture) : "";
                    FillIdentCombo(cat, null);
                    uiPtFree.Text = uiPtNote.Text = uiPtMgrs.Text = uiPtLatLon.Text = "";
                    uiPtAlt.Text = "0";
                    uiPtStatus.Text = (next > 0) ? "" : $"The {PointIdents.CategoryName(cat)} database is full.";
                }
                else
                {
                    uiPtCategory.SelectedIndex = (int)p.Category;
                    uiPtNumber.Text = p.Number.ToString(CultureInfo.InvariantCulture);
                    FillIdentCombo(p.Category, p.Ident);
                    uiPtFree.Text = p.Free;
                    uiPtNote.Text = p.Note;
                    uiPtMgrs.Text = p.Mgrs8;
                    uiPtLatLon.Text = FormatLatLon(p.Lat, p.Lon);
                    uiPtAlt.Text = p.AltFt.ToString("F0", CultureInfo.InvariantCulture);
                    uiPtStatus.Text = "";
                }
            }
            finally
            {
                _isRefreshing = wasRefreshing;
            }
        }

        private static string FormatLatLon(double lat, double lon)
            => $"{lat.ToString("F6", CultureInfo.InvariantCulture)}, {lon.ToString("F6", CultureInfo.InvariantCulture)}";

        private static bool TryParseLatLon(string s, out double lat, out double lon)
        {
            lat = lon = double.NaN;
            string[] parts = (s ?? "").Split(new[] { ',', ' ', ';' }, StringSplitOptions.RemoveEmptyEntries);
            return (parts.Length == 2) &&
                   double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out lat) &&
                   double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out lon) &&
                   (lat >= -90.0) && (lat <= 90.0) && (lon >= -180.0) && (lon <= 180.0);
        }

        /// <summary>
        /// parse one vertex / position line: mgrs ("32U LV 9850 7696") or decimal "lat, lon".
        /// </summary>
        private static bool TryParsePosition(string s, out double lat, out double lon)
        {
            if (Mgrs.TryParse(s, out lat, out lon))
                return true;
            return TryParseLatLon(s, out lat, out lon);
        }

        private void ListPoints_SelectionChanged(object sender, SelectionChangedEventArgs args)
        {
            if (_isRefreshing)
                return;
            if ((uiListPoints.SelectedItems.Count == 1) && (uiListPoints.SelectedItem is ApPointRow row))
                LoadPointEditor(row.Point);
        }

        private void PtCategory_SelectionChanged(object sender, SelectionChangedEventArgs args)
        {
            if (_isRefreshing)
                return;
            if ((_editPoint == null) || (_editPoint.Category != EditCategory))
                LoadPointEditor(null);
        }

        private void PtMgrs_LostFocus(object sender, RoutedEventArgs args)
        {
            if (Mgrs.TryParse(uiPtMgrs.Text, out double lat, out double lon))
            {
                uiPtLatLon.Text = FormatLatLon(lat, lon);
                uiPtStatus.Text = "";
            }
            else if (!string.IsNullOrWhiteSpace(uiPtMgrs.Text))
                uiPtStatus.Text = "MGRS is not valid (example: 32U LV 9850 7696).";
        }

        private void PtLatLon_LostFocus(object sender, RoutedEventArgs args)
        {
            if (TryParseLatLon(uiPtLatLon.Text, out double lat, out double lon))
            {
                uiPtMgrs.Text = Mgrs.FromLatLon(lat, lon, 8);
                uiPtStatus.Text = "";
            }
            else if (!string.IsNullOrWhiteSpace(uiPtLatLon.Text))
                uiPtStatus.Text = "Lat, Lon must be decimal degrees, e.g. 49.436899, 7.600077.";
        }

        private void PtNew_Click(object sender, RoutedEventArgs args)
        {
            uiListPoints.SelectedItem = null;
            LoadPointEditor(null);
        }

        private void PtSave_Click(object sender, RoutedEventArgs args)
        {
            PointCategory cat = EditCategory;
            if (!int.TryParse(uiPtNumber.Text, out int number) ||
                (number < PointIdents.MinNumber(cat)) || (number > PointIdents.MaxNumber(cat)))
            {
                uiPtStatus.Text = $"Number must be {PointIdents.MinNumber(cat)}-{PointIdents.MaxNumber(cat)} for a " +
                                  $"{PointIdents.CategoryName(cat)}.";
                return;
            }
            double lat, lon;
            if (!string.IsNullOrWhiteSpace(uiPtLatLon.Text) && TryParseLatLon(uiPtLatLon.Text, out lat, out lon))
            {
                // lat/lon box is kept in sync with mgrs on lost-focus; it carries full precision.
            }
            else if (!Mgrs.TryParse(uiPtMgrs.Text, out lat, out lon))
            {
                uiPtStatus.Text = "Enter a valid MGRS or Lat, Lon.";
                return;
            }
            if (!double.TryParse(string.IsNullOrWhiteSpace(uiPtAlt.Text) ? "0" : uiPtAlt.Text, NumberStyles.Float,
                                 CultureInfo.InvariantCulture, out double alt) || (alt < -1500) || (alt > 30000))
            {
                uiPtStatus.Text = "Elevation must be a number of feet MSL (-1500 to 30000).";
                return;
            }
            string free = (uiPtFree.Text ?? "").Trim().ToUpperInvariant();
            if (!System.Text.RegularExpressions.Regex.IsMatch(free, "^[A-Z0-9 ]{0,3}$"))
            {
                uiPtStatus.Text = "Free text is up to 3 letters / digits.";
                return;
            }
            string ident = (uiPtIdent.SelectedItem as ComboBoxItem)?.Tag as string ?? PointIdents.Default(cat).Code;

            Ah64Point existing = Mission.FindPoint(cat, number);
            bool isRenumber = (_editPoint != null) && ((_editPoint.Category != cat) || (_editPoint.Number != number));
            if ((existing != null) && (existing != _editPoint))
            {
                // saving onto another point's number replaces that point (same as re-entering it in the KU).
                Mission.Points.Remove(existing);
            }
            if (isRenumber && (_editPoint != null))
            {
                RenumberRouteRefs(_editPoint.Category, _editPoint.Number, cat, number);
                Mission.Points.Remove(_editPoint);
            }

            Ah64Point p = ((_editPoint != null) && !isRenumber) ? _editPoint : new Ah64Point();
            p.Category = cat;
            p.Number = number;
            p.Ident = ident;
            p.Free = free;
            p.Note = (uiPtNote.Text ?? "").Trim();
            p.Lat = lat;
            p.Lon = lon;
            p.AltFt = alt;
            if (!Mission.Points.Contains(p))
                Mission.Points.Add(p);

            SaveMission();
            LoadPointEditor(null);
            uiPtStatus.Text = "";
        }

        private void RenumberRouteRefs(PointCategory oldCat, int oldNum, PointCategory newCat, int newNum)
        {
            bool oldRoutable = oldCat != PointCategory.TGT;
            bool newRoutable = newCat != PointCategory.TGT;
            foreach (Ah64Route r in Mission.Routes)
            {
                if (!oldRoutable)
                    continue;
                if (newRoutable)
                {
                    foreach (Ah64RoutePoint rp in r.Points.Where(rp => rp.Number == oldNum))
                        rp.Number = newNum;
                }
                else
                    r.Points.RemoveAll(rp => rp.Number == oldNum);
            }
        }

        private async void PtDelete_Click(object sender, RoutedEventArgs args)
        {
            List<Ah64Point> sel = [.. uiListPoints.SelectedItems.OfType<ApPointRow>().Select(r => r.Point) ];
            if ((sel.Count == 0) && (_editPoint != null))
                sel.Add(_editPoint);
            if (sel.Count == 0)
                return;

            List<string> inRoutes = [.. sel.Where(p => (p.Category != PointCategory.TGT) &&
                                                       Mission.Routes.Any(r => r.Points.Any(rp => rp.Number == p.Number)))
                                           .Select(p => p.Label) ];
            string msg = $"Delete {sel.Count} point(s)?";
            if (inRoutes.Count > 0)
                msg += $"\n\n{string.Join(", ", inRoutes)} will also be removed from the routes that use them.";
            if (await Utilities.Message2BDialog(Content.XamlRoot, "Delete Points", msg, "Delete")
                != ContentDialogResult.Primary)
                return;

            foreach (Ah64Point p in sel)
            {
                Mission.Points.Remove(p);
                if (p.Category != PointCategory.TGT)
                    foreach (Ah64Route r in Mission.Routes)
                        r.Points.RemoveAll(rp => rp.Number == p.Number);
            }
            SaveMission();
            LoadPointEditor(null);
        }

        private void CkLoad_Click(object sender, RoutedEventArgs args)
        {
            Mission.LoadWptHz = uiCkLoadW.IsChecked == true;
            Mission.LoadCtrlm = uiCkLoadC.IsChecked == true;
            Mission.LoadTgt = uiCkLoadT.IsChecked == true;
            SaveMission();
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // routes
        //
        // ------------------------------------------------------------------------------------------------------------

        private int RouteIndex => Math.Max(0, uiRtSelect.SelectedIndex);

        private Ah64Route CurRoute => Mission.Routes[Math.Min(RouteIndex, Mission.Routes.Count - 1)];

        private void RefreshRoutes()
        {
            while (Mission.Routes.Count < Ah64Limits.Routes)
                Mission.Routes.Add(new Ah64Route() { Name = Ah64Limits.DefaultRouteNames[Mission.Routes.Count] });

            int sel = RouteIndex;
            uiRtSelect.Items.Clear();
            for (int i = 0; i < Ah64Limits.Routes; i++)
            {
                Ah64Route r = Mission.Routes[i];
                string pts = (r.Points.Count == 0) ? "empty" : $"{r.Points.Count} pts";
                uiRtSelect.Items.Add($"{i + 1}: {r.Name.Trim()} ({pts})");
            }
            uiRtSelect.SelectedIndex = sel;

            Ah64Route route = CurRoute;
            uiRtName.Text = route.Name.Trim();
            uiRtStart.Text = TimeSpan.FromSeconds(Math.Max(0, route.StartTimeSec) % 86400).ToString(@"hh\:mm\:ss");
            uiRtEnabled.IsChecked = route.IsEnabled;

            // route point choices: every W and C point
            object prevChoice = uiRtAddPoint.SelectedItem;
            string prevTag = (prevChoice as ComboBoxItem)?.Tag as string;
            uiRtAddPoint.Items.Clear();
            int iSel = -1, idx = 0;
            foreach (Ah64Point p in Mission.PointsIn(PointCategory.WPTHZ).Concat(Mission.PointsIn(PointCategory.CTRLM)))
            {
                string tag = p.Number.ToString(CultureInfo.InvariantCulture);
                uiRtAddPoint.Items.Add(new ComboBoxItem() { Content = $"{p.Label}  {p.Ident}  {p.Free}  {p.Note}", Tag = tag });
                if (tag == prevTag)
                    iSel = idx;
                idx++;
            }
            uiRtAddPoint.SelectedIndex = (iSel >= 0) ? iSel : ((uiRtAddPoint.Items.Count > 0) ? 0 : -1);

            // legs, computed in the theater projection (what dcs measures in); fall back to great-circle-ish
            // planar distance on the utm grid when no theater is set.
            List<ApRouteRow> rows = [];
            DcsTheaters.TheaterProjection proj = DcsTheaters.Find(Mission.Theater);
            DcsXY? prev = null;
            double totalM = 0.0, totalS = 0.0;
            int seq = 0;
            foreach (Ah64RoutePoint rp in route.Points)
            {
                Ah64Point p = Mission.FindPoint(rp.Category, rp.Number);
                string leg = "", ete = "";
                if ((p != null) && (proj != null))
                {
                    DcsXY xy = proj.ToXY(p.Lat, p.Lon);
                    if (prev != null)
                    {
                        double m = Ah64Dtc.LegMeters(prev.Value, xy);
                        double s = (rp.SpeedKts > 0) ? m / (rp.SpeedKts * Ah64Limits.MetersPerSecondPerKnot) : 0.0;
                        totalM += m;
                        totalS += s;
                        leg = (m / 1852.0).ToString("F1", CultureInfo.InvariantCulture);
                        ete = TimeSpan.FromSeconds(s).ToString(@"mm\:ss");
                    }
                    prev = xy;
                }
                rows.Add(new ApRouteRow()
                {
                    Seq = seq.ToString(CultureInfo.InvariantCulture),
                    Label = rp.Label,
                    Ident = p?.Ident ?? "??",
                    Free = p?.Free ?? "",
                    Speed = rp.SpeedKts.ToString("F0", CultureInfo.InvariantCulture),
                    Leg = leg,
                    Ete = ete,
                    Note = p?.Note ?? "(not in database)"
                });
                seq++;
            }
            uiListRoute.ItemsSource = rows;
            uiRtTotals.Text = (route.Points.Count < 2) ? "" :
                ((proj == null) ? "Select a theater to see leg distances." :
                 $"Total {totalM / 1852.0:F1} nm, {TimeSpan.FromSeconds(totalS):hh\\:mm\\:ss} en route");
        }

        private void RtSelect_SelectionChanged(object sender, SelectionChangedEventArgs args)
        {
            if (_isRefreshing || (uiRtSelect.SelectedIndex < 0))
                return;
            uiRtStatus.Text = "";
            RefreshAll();
        }

        private void RtProps_LostFocus(object sender, RoutedEventArgs args)
        {
            if (_isRefreshing || !_isEditable)
                return;
            Ah64Route route = CurRoute;
            string name = (uiRtName.Text ?? "").Trim().ToUpperInvariant();
            if (!System.Text.RegularExpressions.Regex.IsMatch(name, "^[A-Z0-9 ]{1,5}$"))
            {
                uiRtStatus.Text = "Route name is 1-5 letters / digits.";
                return;
            }
            string ts = (uiRtStart.Text ?? "").Trim();
            if (!TimeSpan.TryParseExact(ts, [@"h\:mm\:ss", @"hh\:mm\:ss", @"h\:mm", @"hh\:mm"],
                                        CultureInfo.InvariantCulture, out TimeSpan start) || (start.TotalDays >= 1.0))
            {
                uiRtStatus.Text = "Start time is HH:MM or HH:MM:SS.";
                return;
            }
            bool changed = (route.Name.Trim() != name) || (Math.Abs(route.StartTimeSec - start.TotalSeconds) > 0.5);
            if (route.Name.Trim() != name)
                route.Name = name;
            route.StartTimeSec = start.TotalSeconds;
            uiRtStatus.Text = "";
            if (changed)
                SaveMission();
        }

        private void RtEnabled_Click(object sender, RoutedEventArgs args)
        {
            CurRoute.IsEnabled = uiRtEnabled.IsChecked == true;
            SaveMission();
        }

        private bool TryGetAddPoint(out int number, out double speed)
        {
            number = 0;
            speed = 0.0;
            if ((uiRtAddPoint.SelectedItem as ComboBoxItem)?.Tag is not string tag || !int.TryParse(tag, out number))
            {
                uiRtStatus.Text = "Add W or C points on the Points tab first.";
                return false;
            }
            if (!double.TryParse(uiRtSpeed.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out speed) ||
                (speed < 0) || (speed > 200))
            {
                uiRtStatus.Text = "Speed is 0-200 kt.";
                return false;
            }
            uiRtStatus.Text = "";
            return true;
        }

        private void RtAdd_Click(object sender, RoutedEventArgs args)
        {
            if (!TryGetAddPoint(out int number, out double speed))
                return;
            Ah64Route route = CurRoute;
            route.Points.Add(new Ah64RoutePoint() { Number = number, SpeedKts = speed });
            if (route.Points.Count == 1)
                route.IsEnabled = true;
            SaveMission();
            uiListRoute.SelectedIndex = route.Points.Count - 1;
        }

        private void RtInsert_Click(object sender, RoutedEventArgs args)
        {
            if (!TryGetAddPoint(out int number, out double speed))
                return;
            int at = Math.Max(0, uiListRoute.SelectedIndex);
            CurRoute.Points.Insert(Math.Min(at, CurRoute.Points.Count), new Ah64RoutePoint() { Number = number, SpeedKts = speed });
            CurRoute.IsEnabled = true;
            SaveMission();
            uiListRoute.SelectedIndex = at;
        }

        private void RtSetSpeed_Click(object sender, RoutedEventArgs args)
        {
            int i = uiListRoute.SelectedIndex;
            if (i < 0)
            {
                uiRtStatus.Text = "Select a route point.";
                return;
            }
            if (!double.TryParse(uiRtSpeed.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double speed) ||
                (speed < 0) || (speed > 200))
            {
                uiRtStatus.Text = "Speed is 0-200 kt.";
                return;
            }
            CurRoute.Points[i].SpeedKts = speed;
            SaveMission();
            uiListRoute.SelectedIndex = i;
        }

        private void ListRoute_SelectionChanged(object sender, SelectionChangedEventArgs args)
        {
            int i = uiListRoute.SelectedIndex;
            if ((i >= 0) && (i < CurRoute.Points.Count))
                uiRtSpeed.Text = CurRoute.Points[i].SpeedKts.ToString("F0", CultureInfo.InvariantCulture);
        }

        private void MoveRoutePoint(int delta)
        {
            int i = uiListRoute.SelectedIndex;
            int j = i + delta;
            List<Ah64RoutePoint> pts = CurRoute.Points;
            if ((i < 0) || (j < 0) || (j >= pts.Count))
                return;
            (pts[i], pts[j]) = (pts[j], pts[i]);
            SaveMission();
            uiListRoute.SelectedIndex = j;
        }

        private void RtUp_Click(object sender, RoutedEventArgs args) => MoveRoutePoint(-1);

        private void RtDown_Click(object sender, RoutedEventArgs args) => MoveRoutePoint(+1);

        private void RtRemove_Click(object sender, RoutedEventArgs args)
        {
            int i = uiListRoute.SelectedIndex;
            if (i < 0)
                return;
            CurRoute.Points.RemoveAt(i);
            if (CurRoute.Points.Count == 0)
                CurRoute.IsEnabled = false;
            SaveMission();
            uiListRoute.SelectedIndex = Math.Min(i, CurRoute.Points.Count - 1);
        }

        private async void RtClear_Click(object sender, RoutedEventArgs args)
        {
            if (CurRoute.Points.Count == 0)
                return;
            if (await Utilities.Message2BDialog(Content.XamlRoot, "Clear Route",
                    $"Remove all points from route {CurRoute.Name.Trim()}?", "Clear") != ContentDialogResult.Primary)
                return;
            CurRoute.Points.Clear();
            CurRoute.IsEnabled = false;
            SaveMission();
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // lines
        //
        // ------------------------------------------------------------------------------------------------------------

        private static string VerticesText(IEnumerable<GeoPoint> verts)
            => string.Join("\r", verts.Select(v => Mgrs.FromLatLon(v.Lat, v.Lon, 10)));

        private static bool TryParseVertices(string text, int min, int max, out List<GeoPoint> verts, out string error)
        {
            verts = [];
            error = null;
            string[] lines = (text ?? "").Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string line in lines.Select(l => l.Trim()).Where(l => l.Length > 0))
            {
                if (!TryParsePosition(line, out double lat, out double lon))
                {
                    error = $"“{line}” is not a valid MGRS or lat, lon.";
                    return false;
                }
                verts.Add(new GeoPoint(lat, lon));
            }
            if ((verts.Count < min) || (verts.Count > max))
            {
                error = (max == int.MaxValue) ? $"Enter at least {min} vertices." : $"Enter {min}-{max} vertices.";
                return false;
            }
            return true;
        }

        private void RefreshLines()
        {
            List<ApShapeRow> rows = [];
            for (int i = 0; i < Mission.Lines.Count; i++)
            {
                Ah64Line l = Mission.Lines[i];
                rows.Add(new ApShapeRow()
                {
                    Seq = (i + 1).ToString(CultureInfo.InvariantCulture),
                    Kind = l.TypeNum.ToString(CultureInfo.InvariantCulture),
                    Text = l.Text,
                    Count = l.Vertices.Count.ToString(CultureInfo.InvariantCulture),
                    Note = l.Note
                });
            }
            uiListLines.ItemsSource = rows;
        }

        private void ListLines_SelectionChanged(object sender, SelectionChangedEventArgs args)
        {
            int i = uiListLines.SelectedIndex;
            if (_isRefreshing || (i < 0) || (i >= Mission.Lines.Count))
                return;
            _editLine = i;
            Ah64Line l = Mission.Lines[i];
            uiLnType.Text = l.TypeNum.ToString(CultureInfo.InvariantCulture);
            uiLnText.Text = l.Text;
            uiLnNote.Text = l.Note;
            uiLnVerts.Text = VerticesText(l.Vertices);
            uiLnStatus.Text = "";
        }

        private void LnNew_Click(object sender, RoutedEventArgs args)
        {
            _editLine = -1;
            uiListLines.SelectedItem = null;
            uiLnType.Text = "1";
            uiLnText.Text = uiLnNote.Text = uiLnVerts.Text = uiLnStatus.Text = "";
        }

        private void LnSave_Click(object sender, RoutedEventArgs args)
        {
            if (!int.TryParse(uiLnType.Text, out int type) || (type < 0) || (type > 99))
            {
                uiLnStatus.Text = "Type is a number.";
                return;
            }
            if (!TryParseVertices(uiLnVerts.Text, 2, Ah64Limits.LineVertices, out List<GeoPoint> verts, out string err))
            {
                uiLnStatus.Text = err;
                return;
            }
            if ((_editLine < 0) && (Mission.Lines.Count >= Ah64Limits.Lines))
            {
                uiLnStatus.Text = $"The DTC holds {Ah64Limits.Lines} lines.";
                return;
            }
            Ah64Line l = (_editLine >= 0) ? Mission.Lines[_editLine] : new Ah64Line();
            l.TypeNum = type;
            l.Text = (uiLnText.Text ?? "").Trim().ToUpperInvariant();
            l.Note = (uiLnNote.Text ?? "").Trim();
            l.Vertices = verts;
            if (_editLine < 0)
                Mission.Lines.Add(l);
            SaveMission();
            LnNew_Click(null, null);
        }

        private void LnDelete_Click(object sender, RoutedEventArgs args)
        {
            if ((_editLine < 0) || (_editLine >= Mission.Lines.Count))
                return;
            Mission.Lines.RemoveAt(_editLine);
            SaveMission();
            LnNew_Click(null, null);
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // areas / zones
        //
        // ------------------------------------------------------------------------------------------------------------

        private static string KindName(ShapeKind k) => k switch
        {
            ShapeKind.NoFireZone => "NFZ",
            ShapeKind.PriorityFireZone => "PFZ",
            _ => "Area"
        };

        private void RefreshShapes()
        {
            List<ApShapeRow> rows = [];
            for (int i = 0; i < Mission.Shapes.Count; i++)
            {
                Ah64Shape s = Mission.Shapes[i];
                rows.Add(new ApShapeRow()
                {
                    Seq = (i + 1).ToString(CultureInfo.InvariantCulture),
                    Kind = KindName(s.Kind),
                    Count = s.Vertices.Count.ToString(CultureInfo.InvariantCulture),
                    Note = s.Note
                });
            }
            uiListShapes.ItemsSource = rows;
            if (uiShKind.SelectedIndex < 0)
                uiShKind.SelectedIndex = 0;
        }

        private void ListShapes_SelectionChanged(object sender, SelectionChangedEventArgs args)
        {
            int i = uiListShapes.SelectedIndex;
            if (_isRefreshing || (i < 0) || (i >= Mission.Shapes.Count))
                return;
            _editShape = i;
            Ah64Shape s = Mission.Shapes[i];
            uiShKind.SelectedIndex = (int)s.Kind;
            uiShNote.Text = s.Note;
            uiShVerts.Text = VerticesText(s.Vertices);
            uiShStatus.Text = "";
        }

        private void ShNew_Click(object sender, RoutedEventArgs args)
        {
            _editShape = -1;
            uiListShapes.SelectedItem = null;
            uiShKind.SelectedIndex = 0;
            uiShNote.Text = uiShVerts.Text = uiShStatus.Text = "";
        }

        private void ShSave_Click(object sender, RoutedEventArgs args)
        {
            ShapeKind kind = (ShapeKind)Math.Max(0, uiShKind.SelectedIndex);
            if (!TryParseVertices(uiShVerts.Text, 3, int.MaxValue, out List<GeoPoint> verts, out string err))
            {
                uiShStatus.Text = err;
                return;
            }
            int nAreas = Mission.Shapes.Count(s => s.Kind == ShapeKind.Area);
            int nZones = Mission.Shapes.Count - nAreas;
            bool isNew = _editShape < 0;
            ShapeKind oldKind = isNew ? kind : Mission.Shapes[_editShape].Kind;
            bool addsArea = (kind == ShapeKind.Area) && (isNew || (oldKind != ShapeKind.Area));
            bool addsZone = (kind != ShapeKind.Area) && (isNew || (oldKind == ShapeKind.Area));
            if (addsArea && (nAreas >= Ah64Limits.Areas))
            {
                uiShStatus.Text = $"The DTC holds {Ah64Limits.Areas} areas.";
                return;
            }
            if (addsZone && (nZones >= Ah64Limits.Zones))
            {
                uiShStatus.Text = $"The DTC holds {Ah64Limits.Zones} zones.";
                return;
            }
            Ah64Shape shape = isNew ? new Ah64Shape() : Mission.Shapes[_editShape];
            shape.Kind = kind;
            shape.Note = (uiShNote.Text ?? "").Trim();
            // keep dcs' own caption anchors when the vertices did not change (to the 10-digit grid shown).
            if (VerticesText(shape.Vertices) != VerticesText(verts))
                shape.Vertices = verts;
            if (isNew)
                Mission.Shapes.Add(shape);
            SaveMission();
            ShNew_Click(null, null);
        }

        private void ShDelete_Click(object sender, RoutedEventArgs args)
        {
            if ((_editShape < 0) || (_editShape >= Mission.Shapes.Count))
                return;
            Mission.Shapes.RemoveAt(_editShape);
            SaveMission();
            ShNew_Click(null, null);
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // adf
        //
        // ------------------------------------------------------------------------------------------------------------

        private void RefreshAdf()
        {
            uiAdfEnabled.IsChecked = Mission.AdfEnabled;
            for (int i = 0; i < _adfRows.Count; i++)
            {
                Ah64AdfPreset p = (i < Mission.Adf.Count) ? Mission.Adf[i] : new Ah64AdfPreset();
                _adfRows[i].Id.Text = p.Id;
                _adfRows[i].Freq.Text = p.FreqKHz.ToString("0.#", CultureInfo.InvariantCulture);
                _adfRows[i].Id.IsEnabled = _adfRows[i].Freq.IsEnabled = _isEditable;
            }
        }

        private void AdfEnabled_Click(object sender, RoutedEventArgs args)
        {
            Mission.AdfEnabled = uiAdfEnabled.IsChecked == true;
            SaveMission();
        }

        private void AdfSave_Click(object sender, RoutedEventArgs args)
        {
            List<Ah64AdfPreset> presets = [];
            for (int i = 0; i < _adfRows.Count; i++)
            {
                string id = (_adfRows[i].Id.Text ?? "").Trim().ToUpperInvariant();
                if (!double.TryParse(_adfRows[i].Freq.Text, NumberStyles.Float, CultureInfo.InvariantCulture,
                                     out double f) || !Ah64RadioRanges.InRange(f, Ah64RadioRanges.ADFKHz))
                {
                    uiAdfStatus.Text = $"ADF {i + 1}: frequency must be 100-2199.5 kHz.";
                    return;
                }
                presets.Add(new Ah64AdfPreset() { Id = id, FreqKHz = f });
            }
            Mission.Adf = presets;
            uiAdfStatus.Text = "";
            SaveMission();
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // navigation
        //
        // ------------------------------------------------------------------------------------------------------------

        protected override void OnNavigatedTo(NavigationEventArgs args)
        {
            // the page serves both missions; pick the helper before the base class asks for SystemTag.
            ConfigEditorPageNavArgs navArgs = (ConfigEditorPageNavArgs)args.Parameter;
            _helper = (IAH64DMissionHelper)Activator.CreateInstance(navArgs.EditorHelperType);

            base.OnNavigatedTo(args);

            NavSys.Mission ??= new Ah64Mission();
            ClearEditors();
            uiPtCategory.SelectedIndex = -1;
            RefreshAll();
        }
    }
}
