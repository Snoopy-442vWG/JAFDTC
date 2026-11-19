// ********************************************************************************************************************
//
// AH64DEditCommPage.xaml.cs : ui c# for the AH-64D dtc comm preset editor
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
    public sealed class ApPresetRow
    {
        public string Num { get; init; }
        public string Unit { get; init; }
        public string CallSign { get; init; }
        public string Primary { get; init; }
        public string DL { get; init; }
        public string VHF { get; init; }
        public string UHF { get; init; }
        public string FM1 { get; init; }
        public string FM2 { get; init; }
        public string Source { get; init; }
    }

    /// <summary>
    /// editor for the ten AH-64D comm presets. a preset that is not "set" is left as the dtc base tape has it.
    /// </summary>
    public sealed partial class AH64DEditCommPage : SystemEditorPageBase
    {
        public static ConfigEditorPageInfo PageInfo
            => new(COMMSystem.SystemTag, "Comm Presets", "COMM", Glyphs.COMM, typeof(AH64DEditCommPage));

        protected override SystemBase SystemConfig => ((AH64DConfiguration)Config).COMM;

        protected override string SystemTag => COMMSystem.SystemTag;

        protected override string SystemName => "Comm Presets";

        private COMMSystem Comm => ((AH64DConfiguration)Config).COMM;

        private bool _isEditable = true;
        private bool _isRefreshing;

        private int CurIndex => Math.Max(0, uiListPresets.SelectedIndex);

        public AH64DEditCommPage()
        {
            InitializeComponent();
            InitializeBase(null, null, uiCtlLinkResetBtns);
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // ui
        //
        // ------------------------------------------------------------------------------------------------------------

        protected override void UpdateUICustom(bool isEditable)
        {
            _isEditable = isEditable;
            Refresh();
        }

        private static string F(double v) => v.ToString("0.0##", CultureInfo.InvariantCulture);

        private void Refresh()
        {
            if (_isRefreshing)
                return;
            _isRefreshing = true;
            try
            {
                Comm.Normalize();
                int sel = CurIndex;
                List<ApPresetRow> rows = [];
                for (int i = 0; i < Comm.Presets.Count; i++)
                {
                    Ah64CommPreset p = Comm.Presets[i];
                    rows.Add(p.IsSet
                        ? new ApPresetRow()
                        {
                            Num = (i + 1).ToString(CultureInfo.InvariantCulture),
                            Unit = p.UnitId, CallSign = p.CallSign,
                            Primary = (p.Primary == PrimaryRadio.None) ? "" : p.Primary.ToString(),
                            DL = p.DlNet ? "DL" : "",
                            VHF = F(p.VHF.Frequency), UHF = F(p.UHF.Frequency),
                            FM1 = F(p.FM1.Frequency), FM2 = F(p.FM2.Frequency),
                            Source = "JAFDTC"
                        }
                        : new ApPresetRow()
                        {
                            Num = (i + 1).ToString(CultureInfo.InvariantCulture),
                            Source = "base tape"
                        });
                }
                uiListPresets.ItemsSource = rows;
                uiListPresets.SelectedIndex = sel;
                LoadEditor(sel);

                uiBarImportDtc.IsEnabled = uiBarImportCsv.IsEnabled = _isEditable;
                uiPanelPreset.IsHitTestVisible = _isEditable;
                uiPanelPreset.Opacity = _isEditable ? 1.0 : 0.5;

                List<string> warns = Ah64Validator.CommWarnings(Comm.Presets);
                warns.AddRange(Ah64Validator.ValidateComms(Comm.Presets));
                uiInfoWarn.IsOpen = warns.Count > 0;
                uiInfoWarn.Message = string.Join("\n", warns);
            }
            finally
            {
                _isRefreshing = false;
            }
        }

        private void LoadEditor(int i)
        {
            Ah64CommPreset p = Comm.Presets[i];
            uiPresetTitle.Text = $"Preset {i + 1}" + (p.IsSet ? "" : " (not set: base tape value is used)");
            uiUnit.Text = p.UnitId;
            uiCallSign.Text = p.CallSign;
            uiPrimary.SelectedIndex = (int)p.Primary;
            uiDlNet.IsChecked = p.DlNet;
            uiVhf.Text = F(p.VHF.Frequency);
            uiUhf.Text = F(p.UHF.Frequency);
            uiFm1.Text = F(p.FM1.Frequency);
            uiFm2.Text = F(p.FM2.Frequency);
            uiHf.Text = p.HFTx.Frequency.ToString("0.0###", CultureInfo.InvariantCulture);
            uiStatus.Text = "";
        }

        private void ListPresets_SelectionChanged(object sender, SelectionChangedEventArgs args)
        {
            if (!_isRefreshing && (uiListPresets.SelectedIndex >= 0))
                LoadEditor(uiListPresets.SelectedIndex);
        }

        private bool ReadFreq(TextBox box, (double Min, double Max) range, string name, out double f)
        {
            if (double.TryParse(box.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out f) &&
                Ah64RadioRanges.InRange(f, range))
                return true;
            uiStatus.Text = $"{name} must be {range.Min}-{range.Max} MHz.";
            return false;
        }

        private void PresetSave_Click(object sender, RoutedEventArgs args)
        {
            int i = CurIndex;
            if (!ReadFreq(uiVhf, Ah64RadioRanges.VHF, "VHF", out double vhf) ||
                !ReadFreq(uiUhf, Ah64RadioRanges.UHF, "UHF", out double uhf) ||
                !ReadFreq(uiFm1, Ah64RadioRanges.FM, "FM1", out double fm1) ||
                !ReadFreq(uiFm2, Ah64RadioRanges.FM, "FM2", out double fm2) ||
                !ReadFreq(uiHf, Ah64RadioRanges.HF, "HF", out double hf))
                return;

            Ah64CommPreset p = Comm.Presets[i].Clone();
            p.IsSet = true;
            p.UnitId = (uiUnit.Text ?? "").Trim().ToUpperInvariant();
            p.CallSign = (uiCallSign.Text ?? "").Trim().ToUpperInvariant();
            p.Primary = (PrimaryRadio)Math.Max(0, uiPrimary.SelectedIndex);
            p.DlNet = uiDlNet.IsChecked == true;
            p.VHF.Frequency = vhf;
            p.UHF.Frequency = uhf;
            p.FM1.Frequency = fm1;
            p.FM2.Frequency = fm2;
            p.HFTx.Frequency = hf;
            p.HFRx.Frequency = hf;
            Comm.Presets[i] = p;
            Config.Save(this, SystemTag);
            Refresh();
        }

        private void PresetClear_Click(object sender, RoutedEventArgs args)
        {
            Comm.Presets[CurIndex] = new Ah64CommPreset();
            Config.Save(this, SystemTag);
            Refresh();
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // import / export
        //
        // ------------------------------------------------------------------------------------------------------------

        private static FileOpenPicker OpenPicker(params string[] types)
        {
            FileOpenPicker picker = new((Application.Current as JAFDTC.App).Window.AppWindow.Id)
            {
                CommitButtonText = "Import",
                SuggestedStartLocation = PickerLocationId.Desktop,
                ViewMode = PickerViewMode.List
            };
            foreach (string t in types)
                picker.FileTypeFilter.Add(t);
            return picker;
        }

        private async void CmdImportDtc_Click(object sender, RoutedEventArgs args)
        {
            PickFileResult pick = await OpenPicker(".dtc").PickSingleFileAsync();
            if (pick == null)
                return;
            try
            {
                JsonNode data = Ah64Dtc.ParseFile(FileManager.ReadFile(pick.Path))["data"];
                Comm.Presets = Ah64Dtc.ReadComms(data);
                Config.Save(this, SystemTag);
                Refresh();
                await Utilities.Message1BDialog(Content.XamlRoot, "Presets Imported",
                    $"Imported the 10 comm presets from “{Path.GetFileName(pick.Path)}”.");
            }
            catch (Exception ex)
            {
                FileManager.Log($"AH64DEditCommPage:CmdImportDtc_Click exception {ex}");
                await Utilities.Message1BDialog(Content.XamlRoot, "Import Failed", ex.Message);
            }
        }

        private async void CmdImportCsv_Click(object sender, RoutedEventArgs args)
        {
            PickFileResult pick = await OpenPicker(".csv").PickSingleFileAsync();
            if (pick == null)
                return;
            try
            {
                string csv = FileManager.ReadFile(pick.Path);
                if (Ah64Csv.Detect(csv) != Ah64Csv.CsvKind.Radios)
                    throw new InvalidDataException("The CSV is not a radio preset worksheet (it needs Preset, UnitID, " +
                                                   "CallSign, Primary, DLNet, VHF, UHF, FM1, FM2 columns).");
                CsvImportResult res = Ah64Csv.ImportRadios(Comm.Presets, csv);
                Config.Save(this, SystemTag);
                Refresh();
                string msg = res.Summary() + ".";
                if (res.Warnings.Count > 0)
                    msg += "\n\n" + string.Join("\n", res.Warnings);
                await Utilities.Message1BDialog(Content.XamlRoot, "CSV Imported", msg);
            }
            catch (Exception ex)
            {
                FileManager.Log($"AH64DEditCommPage:CmdImportCsv_Click exception {ex}");
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
                    SuggestedFileName = $"{Config.Name} radio presets.csv",
                    DefaultFileExtension = ".csv"
                };
                picker.FileTypeChoices.Add("CSV", [ ".csv" ]);
                PickFileResult pick = await picker.PickSaveFileAsync();
                if (pick == null)
                    return;
                FileManager.WriteFile(pick.Path, Ah64Csv.ExportRadios(Comm.Presets));
                await Utilities.Message1BDialog(Content.XamlRoot, "CSV Exported", pick.Path);
            }
            catch (Exception ex)
            {
                FileManager.Log($"AH64DEditCommPage:CmdExportCsv_Click exception {ex}");
                await Utilities.Message1BDialog(Content.XamlRoot, "Export Failed", ex.Message);
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs args)
        {
            base.OnNavigatedTo(args);
            Comm.Normalize();
            uiListPresets.SelectedIndex = -1;
            Refresh();
            uiListPresets.SelectedIndex = 0;
        }
    }
}
