// ********************************************************************************************************************
//
// A10CEditDSMSMunitionSettingsPage.cs : ui c# for warthog dsms munitions editor page
//
// Copyright(C) 2024 fizzle, JAFDTC contributors
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

using System;
using JAFDTC.Models;
using JAFDTC.Models.A10C;
using JAFDTC.Models.A10C.DSMS;
using JAFDTC.Utilities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System.Collections.Generic;
using static JAFDTC.UI.A10C.A10CEditDSMSPage;
using System.Reflection;
using System.ComponentModel;
using Microsoft.UI.Xaml.Input;
using JAFDTC.UI.Base;
using System.Linq;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace JAFDTC.UI.A10C
{
    /// <summary>
    /// UI Code for the A-10 DSMS A10CMunition Settings Frame
    /// </summary>
    public sealed partial class A10CEditDSMSMunitionSettingsPage : SystemEditorPageBase, IA10CDSMSEditorTab
    {

        protected override SystemBase SystemConfig => ((A10CConfiguration)Config).DSMS;
        protected override string SystemTag => DSMSSystem.SystemTag;
        protected override string SystemName => "DSMS";

        private DSMSSystem DSMSEditState => (DSMSSystem)EditState;
        private DSMSSystem DSMSConfig => (DSMSSystem)SystemConfig;

        private DSMSEditorNavArgs _dsmsEditorNavArgs;
        private readonly List<A10CMunition> _munitions;

        // name of the custom profile being edited, null when editing the jet's default profile for the munition.
        private string _selectedProfileName = null;
        private MunitionSettings _hookedSettings = null;
        private bool _isProfileListRebuilding = false;
        private List<string> _profileListNames = new();        // custom profile names, in uiComboProfile order (after default)

        public A10CEditDSMSMunitionSettingsPage()
        {
            EditState = new DSMSSystem();

            InitializeComponent();
            InitializeBase(EditState, uiTextLaserCode, null);

            _munitions = FileManager.LoadA10Munitions();
        }

        // ---- UI helpers  -------------------------------------------------------------------------------------------

        public void DSMSEditorCopyConfigToEditState() => CopyConfigToEditState();

        protected override void GetControlEditStateProperty(FrameworkElement ctrl,
                                                           out PropertyInfo prop, out BindableObject obj)
        {
            GetControlPropertyHelper(SettingLocation.Edit, ctrl, out prop, out obj);
        }

        protected override void GetControlConfigProperty(FrameworkElement ctrl,
                                                         out PropertyInfo prop, out BindableObject obj)
        {
            GetControlPropertyHelper(SettingLocation.Config, ctrl, out prop, out obj);
        }

        private void GetControlPropertyHelper(SettingLocation settingLocation, FrameworkElement ctrl,
                                              out PropertyInfo prop, out BindableObject obj)
        {
            string propName = ctrl.Tag.ToString();

            // check the "normal" DSMSSystem class properties first.
            if (settingLocation == SettingLocation.Edit)
            {
                prop = EditState.GetType().GetProperty(propName);
                obj = EditState;
            }
            else
            {
                prop = SystemConfig.GetType().GetProperty(propName);
                obj = SystemConfig;
            }

            // if it comes up empty, use the MunitionSettings class for the selected munition.
            if (prop == null)
            {
                prop = typeof(MunitionSettings).GetProperty(propName);
                if (IsMunitionSelectionValid(out A10CMunition selectedMunition))
                    obj = GetSelectedSettings(settingLocation, selectedMunition);
                else
                    obj = null;
            }
        }

        /// <summary>
        /// The base UI update will correctly set values and link-related enablement, but this editor has 
        /// unique visibility and enablement behavior we need to perform here.
        /// </summary>
        protected override void UpdateUICustom(bool isEditable)
        {
            if (!IsMunitionSelectionValid(out A10CMunition selectedMunition) || EditState == null)
               return;

            uiTextMuniDesc.Text = selectedMunition.DescrUI;

            SetLabelColorMatchingControlEnabledState(uiLabelLaserCode, uiTextLaserCode);

            Visibility autoLaseVisible = (selectedMunition.AutoLase) ? Visibility.Visible : Visibility.Collapsed;
            MunitionSettings selectedSettings = GetSelectedSettings(SettingLocation.Edit, selectedMunition);
            bool isAutoLaseOn = bool.Parse(string.IsNullOrEmpty(selectedSettings.AutoLase)
                                           ? MunitionSettings.ExplicitDefaults.AutoLase : selectedSettings.AutoLase);
            Visibility autoLaseFieldVisible = (selectedMunition.AutoLase && isAutoLaseOn) 
                ? Visibility.Visible : Visibility.Collapsed;
            uiLabelAutoLase.Visibility = autoLaseVisible;
            uiStackAutoLase.Visibility = autoLaseVisible;
            uiTextLaseTime.Visibility = autoLaseFieldVisible;
            uiLabelLaseTimeUnits.Visibility = autoLaseFieldVisible;
            if (selectedMunition.AutoLase) SetLabelColorMatchingControlEnabledState(new TextBlock[] { uiLabelAutoLase, uiLabelLaseTimeUnits } , uiTextLaseTime);

            // Delivery Mode (CCIP/CCRP)
            if (selectedMunition.CCIP ^ selectedMunition.CCRP)
            {
                if (selectedMunition.CCIP)
                    uiComboDeliveryMode.SelectedIndex = 0;
                else
                    uiComboDeliveryMode.SelectedIndex = 1;
                uiComboDeliveryMode.IsEnabled = false;
                uiLabelDeliveryMode.Foreground = uiTextLaserCode.PlaceholderForeground;
            }
            SetLabelColorMatchingControlEnabledState(uiLabelDeliveryMode, uiComboDeliveryMode);

            // Escape Maneuver
            Visibility escVisible = (selectedMunition.EscMnvr) ? Visibility.Visible : Visibility.Collapsed;
            uiLabelEscMnvr.Visibility = escVisible;
            uiComboEscMnvr.Visibility = escVisible;

            // Release Mode (SGL, PRS, RIP SGL, RIP PRS)
            if (selectedMunition.SingleReleaseOnly)
            {
                // For munitions allowing only SGL release, select it and disable.
                uiComboReleaseMode.SelectedIndex = 0;
                uiComboReleaseMode.IsEnabled = false;
                uiStackRipple.Visibility = Visibility.Collapsed;
            }
            else
            {
                uiStackRipple.Visibility = (uiComboReleaseMode.SelectedIndex < 2) ? Visibility.Collapsed : Visibility.Visible;
            }
            SetLabelColorMatchingControlEnabledState(uiLabelReleaseMode, uiComboReleaseMode);

            // Ripple Qty and Distance
            bool enableRippleOptions = selectedMunition.Ripple && uiComboReleaseMode.SelectedIndex > 1; // disabled when SGL or PRS release is selected
            uiTextRippleQty.IsEnabled = enableRippleOptions;
            uiTextRippleFt.IsEnabled = enableRippleOptions && selectedMunition.RipFt;
            SetLabelColorMatchingControlEnabledState(new TextBlock[] { uiLabelRipple, uiLabelRippleAt, uiLabelRippleUnits }, uiComboReleaseMode);

            // HOF
            Visibility hofVisible = (selectedMunition.HOF) ? Visibility.Visible : Visibility.Collapsed;
            uiLabelHOF.Visibility = hofVisible;
            uiComboHOF.Visibility = hofVisible;
            if (selectedMunition.HOF) SetLabelColorMatchingControlEnabledState(uiLabelHOF, uiComboHOF);

            // RPM
            uiStackRPM.Visibility = selectedMunition.RPM ? Visibility.Visible : Visibility.Collapsed;
            if (selectedMunition.RPM) SetLabelColorMatchingControlEnabledState(uiLabelRPM, uiComboRPM);

            // Fuze
            Visibility fuzeVisible = (selectedMunition.Fuze) ? Visibility.Visible : Visibility.Collapsed;
            uiLabelFuse.Visibility = fuzeVisible;
            uiComboFuze.Visibility = fuzeVisible;
            if (selectedMunition.Fuze) SetLabelColorMatchingControlEnabledState(uiLabelFuse, uiComboFuze);

            // Impact Azimuth
            Visibility impactAzimuthVisible = (selectedMunition.ImpactParams) ? Visibility.Visible : Visibility.Collapsed;
            uiLabelImpactAzimuth.Visibility = impactAzimuthVisible;
            uiTextImpactAzimuth.Visibility = impactAzimuthVisible;
            uiLabelImpactAzimuthUnits.Visibility = impactAzimuthVisible;
            if (selectedMunition.ImpactParams) SetLabelColorMatchingControlEnabledState(new TextBlock[] { uiLabelImpactAzimuth, uiLabelImpactAzimuthUnits }, uiTextImpactAzimuth);

            // Impact Angle
            Visibility impactAngleVisible = (selectedMunition.ImpactParams) ? Visibility.Visible : Visibility.Collapsed;
            uiLabelImpactAngle.Visibility = impactAngleVisible;
            uiTextImpactAngle.Visibility = impactAngleVisible;
            uiLabelImpactAngleUnits.Visibility = impactAngleVisible;
            if (selectedMunition.ImpactParams) SetLabelColorMatchingControlEnabledState(new TextBlock[] { uiLabelImpactAngle, uiLabelImpactAngleUnits }, uiTextImpactAngle);

            // Desired TOF
            Visibility desTOFVisible = (selectedMunition.DesTOF) ? Visibility.Visible : Visibility.Collapsed;
            uiLabelDesTOF.Visibility = desTOFVisible;
            uiTextDesTOF.Visibility = desTOFVisible;
            uiLabelDesTOFUnits.Visibility = desTOFVisible;
            if (selectedMunition.DesTOF) SetLabelColorMatchingControlEnabledState(new TextBlock[] { uiLabelDesTOF, uiLabelDesTOFUnits }, uiTextDesTOF);

            // Min Alt
            Visibility minAltVisible = (selectedMunition.MinAltField) ? Visibility.Visible : Visibility.Collapsed;
            uiLabelMinAlt.Visibility = minAltVisible;
            uiTextMinAlt.Visibility = minAltVisible;
            uiLabelMinAltUnits.Visibility = minAltVisible;
            if (selectedMunition.MinAltField) SetLabelColorMatchingControlEnabledState(new TextBlock[] { uiLabelMinAlt, uiLabelMinAltUnits }, uiTextMinAlt);

            // Min TOF
            Visibility minTOFVisible = (selectedMunition.MinTOF) ? Visibility.Visible : Visibility.Collapsed;
            uiLabelMinTOF.Visibility = minTOFVisible;
            uiTextMinTOF.Visibility = minTOFVisible;
            uiLabelMinTOFUnits.Visibility = minTOFVisible;
            if (selectedMunition.MinTOF) SetLabelColorMatchingControlEnabledState(new TextBlock[] { uiLabelMinTOF, uiLabelMinTOFUnits }, uiTextMinTOF);

            // High-Drag TOF
            Visibility hDTOFVisible = (selectedMunition.DragTOF) ? Visibility.Visible : Visibility.Collapsed;
            uiLabelHDTOF.Visibility = hDTOFVisible;
            uiTextHDTOF.Visibility = hDTOFVisible;
            uiLabelHDTOFUnits.Visibility = hDTOFVisible;
            if (selectedMunition.DragTOF) SetLabelColorMatchingControlEnabledState(new TextBlock[] { uiLabelHDTOF, uiLabelHDTOFUnits }, uiTextHDTOF);

            // Low-Drag TOF
            Visibility lDTOFVisible = (selectedMunition.DragTOF) ? Visibility.Visible : Visibility.Collapsed;
            uiLabelLDTOF.Visibility = lDTOFVisible;
            uiTextLDTOF.Visibility = lDTOFVisible;
            uiLabelLDTOFUnits.Visibility = lDTOFVisible;
            if (selectedMunition.DragTOF) SetLabelColorMatchingControlEnabledState(new TextBlock[] { uiLabelLDTOF, uiLabelLDTOFUnits }, uiTextLDTOF);

            UpdateNonDefaultMunitionIcons();

            MunitionSettings newSettings = GetSelectedSettings(SettingLocation.Edit, selectedMunition);
            bool isNotLinked = !Config.IsLinked(SystemTag);
            Utilities.SetEnableState(uiMuniBtnReset, isNotLinked && !newSettings.IsDefault);

            // Z diagram image for Battle Book profiles
            BattleBookProfile bbSelected = string.IsNullOrEmpty(_selectedProfileName) ? null
                : BattleBookProfiles.ForMunition(selectedMunition.ID).FirstOrDefault(bb => bb.Name == _selectedProfileName);
            string zsledPath = (bbSelected != null)
                ? System.IO.Path.Combine(AppContext.BaseDirectory, "Images", "zsled", bbSelected.ImageFile) : null;
            if ((zsledPath != null) && System.IO.File.Exists(zsledPath))
            {
                uiImageZSled.Source = new BitmapImage(new Uri(zsledPath));
                uiImageZSled.Visibility = Visibility.Visible;
            }
            else
            {
                uiImageZSled.Source = null;
                uiImageZSled.Visibility = Visibility.Collapsed;
            }

            // profile selection controls
            bool isCustom = !string.IsNullOrEmpty(_selectedProfileName);
            uiBtnNewProfile.IsEnabled = isNotLinked;
            uiBtnDeleteProfile.IsEnabled = isNotLinked && isCustom;
            uiComboBattleBook.IsEnabled = isNotLinked && (uiComboBattleBook.Items.Count > 0);
        }

        private void SetLabelColorMatchingControlEnabledState(TextBlock[] labels, Control control)
        {
            foreach (TextBlock label in labels)
                SetLabelColorMatchingControlEnabledState(label, control);
        }

        private void SetLabelColorMatchingControlEnabledState(TextBlock label, Control control)
        {
            label.Foreground = control.IsEnabled ? uiTextLaserCode.Foreground : uiTextLaserCode.PlaceholderForeground;
        }

        private bool IsMunitionSelectionValid(out A10CMunition selectedMunition)
        {
            selectedMunition = (A10CMunition)uiComboMunition.SelectedItem;
            return selectedMunition != null;
        }

        private void UpdateNonDefaultMunitionIcons()
        {
            foreach (A10CMunition munition in uiComboMunition.Items)
            {
                UIElement container = (UIElement)uiComboMunition.ContainerFromItem(munition);
                FontIcon icon = Utilities.FindControl<FontIcon>(container, typeof(FontIcon), "icon");
                if (icon != null)
                {
                    ISystem system = DSMSConfig.GetMunitionSettings(munition);
                    bool hasCustom = DSMSConfig.GetCustomProfileNames(munition).Count > 0;
                    icon.Visibility = hasCustom ? Visibility.Visible : Utilities.HiddenIfDefault(system);
                }
            }
        }

        // ---- event handlers -------------------------------------------------------------------------------------------

        private void ComboMunition_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UnhookSettings();

            if (e.AddedItems.Count > 0)
            {
                A10CMunition newSelectedMunition = (A10CMunition)e.AddedItems[0];
                if (newSelectedMunition != null)
                {
                    _selectedProfileName = null;
                    DSMSEditState.SyncCustomProfilesFrom(DSMSConfig);
                    RebuildProfileLists(newSelectedMunition);
                    CopyConfigToEditState();
                    HookSettings();
                }
            }
        }

        // ---- custom profile support ----------------------------------------------------------------------------------

        /// <summary>
        /// return the settings object for the profile being edited (the munition's default profile or the selected
        /// custom profile) from the edit state or the configuration.
        /// </summary>
        private MunitionSettings GetSelectedSettings(SettingLocation location, A10CMunition munition)
        {
            DSMSSystem system = (location == SettingLocation.Edit) ? DSMSEditState : DSMSConfig;
            if (!string.IsNullOrEmpty(_selectedProfileName))
            {
                MunitionSettings custom = system.GetCustomProfileSettings(munition, _selectedProfileName);
                if (custom != null)
                    return custom;
            }
            return system.GetMunitionSettings(munition);
        }

        protected override void CopyConfigToEditState()
        {
            DSMSEditState.SyncCustomProfilesFrom(DSMSConfig);
            if (!string.IsNullOrEmpty(_selectedProfileName) && IsMunitionSelectionValid(out A10CMunition munition) &&
                !DSMSConfig.HasCustomProfile(munition, _selectedProfileName))
            {
                // selected custom profile went away (config linked/reset), fall back to the default profile.
                UnhookSettings();
                _selectedProfileName = null;
                RebuildProfileLists(munition);
            }
            base.CopyConfigToEditState();
            HookSettings();
        }

        private void HookSettings()
        {
            UnhookSettings();
            if (IsMunitionSelectionValid(out A10CMunition munition))
            {
                _hookedSettings = GetSelectedSettings(SettingLocation.Edit, munition);
                _hookedSettings.PropertyChanged += EditState_PropertyChanged;
            }
        }

        private void UnhookSettings()
        {
            if (_hookedSettings != null)
            {
                _hookedSettings.PropertyChanged -= EditState_PropertyChanged;
                _hookedSettings = null;
            }
        }

        /// <summary>
        /// rebuild the contents of the profile selection and Battle Book dropdowns for the munition, selecting the
        /// current profile.
        /// </summary>
        private void RebuildProfileLists(A10CMunition munition)
        {
            _isProfileListRebuilding = true;

            _profileListNames = DSMSConfig.GetCustomProfileNames(munition);
            List<string> items = new() { $"{munition.Profile}  (jet default profile)" };
            items.AddRange(_profileListNames);
            uiComboProfile.ItemsSource = items;
            int index = string.IsNullOrEmpty(_selectedProfileName) ? 0 : (_profileListNames.IndexOf(_selectedProfileName) + 1);
            uiComboProfile.SelectedIndex = (index > 0) ? index : 0;

            uiComboBattleBook.ItemsSource = BattleBookProfiles.ForMunition(munition.ID);
            uiComboBattleBook.SelectedIndex = -1;

            _isProfileListRebuilding = false;
        }

        /// <summary>
        /// switch the editor over to a different profile (null for the jet's default profile).
        /// </summary>
        private void SelectProfile(A10CMunition munition, string profileName)
        {
            UnhookSettings();
            _selectedProfileName = profileName;
            RebuildProfileLists(munition);
            CopyConfigToEditState();
        }

        private void ComboProfile_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isProfileListRebuilding || !IsMunitionSelectionValid(out A10CMunition munition))
                return;
            int index = uiComboProfile.SelectedIndex;
            if (index < 0)
                return;
            string name = (index == 0) ? null : _profileListNames[index - 1];
            if (name != _selectedProfileName)
                SelectProfile(munition, name);
        }

        private async void ComboBattleBook_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isProfileListRebuilding || !IsMunitionSelectionValid(out A10CMunition munition))
                return;
            if (uiComboBattleBook.SelectedItem is BattleBookProfile bbProfile)
            {
                if (!DSMSConfig.HasCustomProfile(munition, bbProfile.Name))
                {
                    ContentDialogResult result = await Utilities.Message2BDialog(Content.XamlRoot, "Add Battle Book Profile?",
                        $"Add the profile \"{bbProfile.Name}\" to this configuration? It will be created on the jet at upload.", "Add");
                    if (result != ContentDialogResult.Primary)
                    {
                        _isProfileListRebuilding = true;
                        uiComboBattleBook.SelectedIndex = -1;
                        _isProfileListRebuilding = false;
                        return;
                    }
                    MunitionSettings seed = new MunitionSettings(munition);
                    bbProfile.ApplyTo(seed);
                    DSMSConfig.AddCustomProfile(munition, bbProfile.Name, seed);
                    Config.Save(this, SystemTag);
                }
                SelectProfile(munition, bbProfile.Name);
                UpdateNonDefaultMunitionIcons();
            }
        }

        /// <summary>
        /// return null if name is a valid name for a new custom profile, otherwise a message describing the problem.
        /// </summary>
        private string ValidateProfileName(string name)
        {
            if (string.IsNullOrEmpty(name) || (name.Length > 8))
                return "Profile names must be 1 to 8 characters long.";
            if (!name.All(ch => ((ch >= 'A') && (ch <= 'Z')) || ((ch >= '0') && (ch <= '9')) || (ch == ' ') || (ch == '.')))
                return "Profile names may only use letters, digits, spaces, and periods (the CDU keys that can be typed).";
            if (_munitions.Any(m => m.Profile == name) || DSMSConfig.IsCustomProfileNameInUse(name))
                return "A profile with that name already exists.";
            return null;
        }

        private async void BtnNewProfile_Click(object sender, RoutedEventArgs e)
        {
            if (!IsMunitionSelectionValid(out A10CMunition munition))
                return;

            TextBox nameBox = new() { MaxLength = 8, PlaceholderText = "e.g., 82 20LD2", Width = 240 };
            StackPanel content = new() { Spacing = 8 };
            content.Children.Add(new TextBlock()
            {
                Text = $"Name for the new {munition.Name} profile (up to 8 characters; letters, digits, spaces, periods):",
                TextWrapping = TextWrapping.Wrap
            });
            content.Children.Add(nameBox);

            ContentDialogResult result = await new ContentDialog()
            {
                XamlRoot = Content.XamlRoot,
                Title = "New Profile",
                Content = content,
                PrimaryButtonText = "Create",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary
            }.ShowAsync(ContentDialogPlacement.Popup);
            if (result != ContentDialogResult.Primary)
                return;

            string name = nameBox.Text.Trim().ToUpperInvariant();
            string problem = ValidateProfileName(name);
            if (problem != null)
            {
                await Utilities.Message1BDialog(Content.XamlRoot, "Invalid Profile Name", problem);
                return;
            }

            DSMSConfig.AddCustomProfile(munition, name);
            Config.Save(this, SystemTag);
            SelectProfile(munition, name);
            UpdateNonDefaultMunitionIcons();
        }

        private async void BtnDeleteProfile_Click(object sender, RoutedEventArgs e)
        {
            if (!IsMunitionSelectionValid(out A10CMunition munition) || string.IsNullOrEmpty(_selectedProfileName))
                return;

            ContentDialogResult result = await Utilities.Message2BDialog(Content.XamlRoot, "Delete Profile?",
                $"Delete the custom profile \"{_selectedProfileName}\"? This cannot be undone.", "Delete");
            if (result != ContentDialogResult.Primary)
                return;

            UnhookSettings();
            DSMSConfig.RemoveCustomProfile(munition, _selectedProfileName);
            DSMSEditState.RemoveCustomProfile(munition, _selectedProfileName);
            Config.Save(this, SystemTag);
            SelectProfile(munition, null);
            UpdateNonDefaultMunitionIcons();
        }

        private void MuniBtnReset_Click(object sender, RoutedEventArgs e)
        {
            if (IsMunitionSelectionValid(out A10CMunition selectedMunition))
            {
                GetSelectedSettings(SettingLocation.Edit, selectedMunition).Reset();
                SaveEditStateToConfig();
                UpdateUIFromEditState();
            }
        }

        private void TextBox_LosingFocus(UIElement sender, LosingFocusEventArgs args)
        {
            TextBox_LostFocus(sender, args);
        }

        protected override void EditState_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            UpdateUIFromEditState();
            base.EditState_PropertyChanged(sender, e);
        }

        protected override void OnNavigatedTo(NavigationEventArgs args)
        {
            _dsmsEditorNavArgs = (DSMSEditorNavArgs)args.Parameter;

            if (_munitions.Count > 0)
                uiComboMunition.SelectedIndex = 0;

            base.OnNavigatedTo(_dsmsEditorNavArgs.BaseArgs);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            if (_hookedSettings != null)
                _hookedSettings.ErrorsChanged -= EditState_ErrorsChanged;
            UnhookSettings();

            base.OnNavigatedFrom(e);
        }
    }
}
