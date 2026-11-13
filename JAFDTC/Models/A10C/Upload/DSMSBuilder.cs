// ********************************************************************************************************************
//
// DSMSBuilder.cs -- a-10c dsms system builder
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

using JAFDTC.Models.A10C.DSMS;
using JAFDTC.Models.DCS;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace JAFDTC.Models.A10C.Upload
{
    /// <summary>
    /// command builder for the DSMS systems in the warthog. translates cmds setup in A10CConfiguration into
    /// commands that drive the dcs clickable cockpit.
    /// </summary>
    internal partial class DSMSBuilder : A10CBuilderBase, IBuilder
    {
        LoadoutQueryBuilder _loadoutQuery;
        DSMSProfileQueryBuilder _profileQuery;

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public DSMSBuilder(A10CConfiguration cfg, A10CDeviceManager dcsCmds, StringBuilder sb) : base(cfg, dcsCmds, sb) 
        {
            StringBuilder sbLoadoutQuery = new StringBuilder();
            _loadoutQuery = new LoadoutQueryBuilder(dcsCmds, sbLoadoutQuery, "QueryLoadout", null);

            StringBuilder sbProfileQuery = new StringBuilder();
            _profileQuery = new DSMSProfileQueryBuilder(dcsCmds, sbProfileQuery, "QueryDSMSProfiles", null);
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // build methods
        //
        // ------------------------------------------------------------------------------------------------------------

        public override void Build(Dictionary<string, object> state = null)
        {
            if (_cfg.DSMS.IsDefault)
                return;

            AddExecFunction("NOP", new() { "==== DSMSBuilder:Build()" });

            AirframeDevice lmfd = _aircraft.GetDevice("LMFD");
            AirframeDevice cdu = _aircraft.GetDevice("CDU");

            AddIfBlock("IsDSMSInDefaultMFDPosition", true, null, delegate () { BuildDSMS(cdu, lmfd); });
            // TODO handle non-default DSMS
        }

        private void BuildDSMS(AirframeDevice cdu, AirframeDevice lmfd)
        {
            Build_INV(cdu, lmfd);
            Build_DefaultProfiles(cdu, lmfd);
            if (_cfg.DSMS.HasCustomProfiles)
                Build_CustomProfiles(cdu, lmfd);                    // create custom profiles, full sort follows
            else
                Build_DefaultProfileOrder(lmfd);
            Build_CustomProfileLayout(lmfd);                        // move default dumb-bomb profiles to the end
        }

        private void Build_INV(AirframeDevice cdu, AirframeDevice lmfd)
        {
            // get configs that require INV changes
            Dictionary<string, MunitionSettings> nonDefaultSettings = _cfg.DSMS.GetNonDefaultInvSettings();
            if (nonDefaultSettings == null || nonDefaultSettings.Count == 0)
                return;

            AddActions(lmfd, new() { "LMFD_14", "LMFD_05" }, null, WAIT_BASE); // Go to DSMS INV

            Dictionary<int, bool> _setupStations = GetStationIsSetupMap();
            // attempt symmetric loads on the first 5 stations
            for (int station = 1; station <= 5; station++)
            {
                int symLoadedStation = Build_INV_station(cdu, lmfd, nonDefaultSettings, station, true);
                _setupStations[station] = true;
                if (symLoadedStation > 0)
                    _setupStations[symLoadedStation] = true;
            }
            // station 6 has no symmetric station, don't attempt sym load
            Build_INV_station(cdu, lmfd, nonDefaultSettings, 6, false);
            _setupStations[6] = true;
            // set up any stations that weren't symloaded with stations 1-5
            for (int station = 7; station <= 11; station++)
            {
                if (!_setupStations[station])
                {
                    Build_INV_station(cdu, lmfd, nonDefaultSettings, station, false);
                }
            }
        }


        /// <summary>
        /// Sets the INV page settings for one station.
        /// </summary>
        /// <param name="cdu"></param>
        /// <param name="lmfd"></param>
        /// <param name="nonDefaultSettings">All the non-default INV settings</param>
        /// <param name="station">The A-10 pylon to setup</param>
        /// <param name="attemptSymLoad">Do symmetric load of the opposite pylon if it has the same munition loaded</param>
        /// <returns>Station number of a symloaded station if there was one, otherwise -1.</returns>
        private int Build_INV_station(AirframeDevice cdu, AirframeDevice lmfd, 
            Dictionary<string, MunitionSettings> nonDefaultSettings, int station, bool attemptSymLoad)
        {
            string munitionAtStation = _loadoutQuery.StationMunitionMap[station];
            if (munitionAtStation == null)
                return -1; // pylon is empty
            
            if (nonDefaultSettings.TryGetValue(munitionAtStation, out MunitionSettings setting))
            {
                // station, inv stat
                AddActions(lmfd, new() { GetButtonForStation(station), "LMFD_03" }, null, WAIT_BASE);

                // Laser Code
                // There's a single laser code setting for the whole DSMS system that applies to any Laser-capable weapon.
                if (setting.Munition.Laser && !_cfg.DSMS.IsLaserCodeDefault)
                {
                    AddActions(cdu, ActionsForCleanNum(_cfg.DSMS.LaserCode));
                    // The button for setting laser code moves depending on the munition. Grab it from the data.
                    AddAction(lmfd, "LMFD_" + setting.Munition.LaserButton);
                }

                // HOF
                if (setting.Munition.HOF && !setting.IsHOFOptionDefault)
                {
                    int hofDefault = int.Parse(MunitionSettings.ExplicitDefaults.HOFOption);
                    int hofVal = (int)_cfg.DSMS.GetHOFOptionValue(setting.Munition);
                    int numPresses = GetNumClicksForWraparoundSetting<HOFOptions>(hofDefault, hofVal);
                    for (int i = 0; i < numPresses; i++)
                        AddAction(lmfd, "LMFD_18");
                }

                // RPM
                if (setting.Munition.RPM && !setting.IsRPMOptionDefault)
                {
                    int rpmDefault = int.Parse(MunitionSettings.ExplicitDefaults.RPMOption);
                    int rpmVal = (int)_cfg.DSMS.GetRPMOptionValue(setting.Munition);
                    int numPresses = GetNumClicksForWraparoundSetting<RPMOptions>(rpmDefault, rpmVal);
                    for (int i = 0; i < numPresses; i++)
                        AddAction(lmfd, "LMFD_17");
                }

                // Load
                if (attemptSymLoad)
                {
                    int symStation = GetSymmetricStation(station);
                    if (setting.Munition.INV_Keys.Contains(_loadoutQuery.StationMunitionMap[symStation]))
                    {
                        // load sym
                        AddAction(lmfd, "LMFD_10");
                        return symStation;
                    }
                }

                // load
                AddAction(lmfd, "LMFD_09");
                return -1;
            }

            return -1;
        }

        // Modify default weapon profiles having non-default configured settings.
        private void Build_DefaultProfiles(AirframeDevice cdu, AirframeDevice lmfd)
        {
            // get configs that require profile changes
            Dictionary<int, MunitionSettings> nonDefaultSettings = _cfg.DSMS.GetNonDefaultProfileSettings();
            if (nonDefaultSettings == null || nonDefaultSettings.Count == 0)
                return;

            AddActions(lmfd, new() { "LMFD_14", "LMFD_01" }, null, WAIT_BASE); // Go to DSMS Profiles
            int selectedProfileIndex = 0;

            foreach (KeyValuePair<int, int> kv in _profileQuery.MunitionProfileIndexMap)
            {
                if (nonDefaultSettings.TryGetValue(kv.Key, out MunitionSettings settings))
                {
                    selectedProfileIndex = SelectProfile(lmfd, selectedProfileIndex, kv.Value);
                    Build_CurrentProfile(cdu, lmfd, settings);
                }
            }

            // Back to DSMS Main
            AddAction(lmfd, "LMFD_01"); // STAT
        }

        private void Build_CurrentProfile(AirframeDevice cdu, AirframeDevice lmfd, MunitionSettings settings)
        {
            AddAction(lmfd, "LMFD_03"); // VIEW PRO

            // "front page" stuff first

            // Release Mode: SGL, PRS, RIP SGL, RIP PRS
            if ((settings.Munition.Pairs || settings.Munition.Ripple) && !settings.IsReleaseModeDefault)
            {
                for (int i = 0; i < int.Parse(settings.ReleaseMode); i++)
                    AddAction(lmfd, "LMFD_06");
            }

            // Fuze
            if (settings.Munition.Fuze && !settings.IsFuzeOptionDefault) 
            {
                for (int i = 0; i < int.Parse(settings.FuzeOption); i++)
                    AddAction(lmfd, "LMFD_07");
            }

            // Ripple Qty
            if (settings.Munition.Ripple && !settings.IsRippleQtyDefault)
            {
                AddAction(cdu, "CLR");
                AddActions(cdu, ActionsForCleanNum(settings.RippleQty));
                AddAction(lmfd, "LMFD_08");
            }

            // Ripple Distance
            if (settings.Munition.RipFt && !settings.IsRippleFtDefault)
            {
                AddAction(cdu, "CLR");
                AddActions(cdu, ActionsForCleanNum(settings.RippleFt));
                AddAction(lmfd, "LMFD_09");
            }

            // Delivery Mode: CCIP/CCRP
            if (!settings.IsDeliveryModeDefault)
            {
                for (int i = 0; i < int.Parse(settings.DeliveryMode); i++)
                    AddAction(lmfd, "LMFD_10");
            }

            // Settings inside the "CHG SET" page
            if (!settings.IsProfileCHGSETDefault)
            {
                AddAction(lmfd, "LMFD_16"); // CHG SET

                // Escape Maneuver
                if (settings.Munition.EscMnvr && !settings.IsEscapeManeuverDefault)
                {
                    int escDefault = int.Parse(MunitionSettings.ExplicitDefaults.EscapeManeuver);
                    int escVal = (int)_cfg.DSMS.GetEscapeManeuverValue(settings.Munition);
                    int numPresses = GetNumClicksForWraparoundSetting<EscapeManeuvers>(escDefault, escVal);
                    for (int i = 0; i < numPresses; i++)
                        AddAction(lmfd, "LMFD_20");
                }

                // Auto Lase
                if (settings.Munition.Laser && !settings.IsAutoLaseDefault)
                    AddAction(lmfd, "LMFD_06");

                // Impact Azimuth (GBU-54)
                if (settings.Munition.ImpactParams && !settings.IsImpactAzimuthDefault)
                {
                    AddAction(cdu, "CLR");
                    AddActions(cdu, ActionsForCleanNum(settings.ImpactAzimuth));
                    AddAction(lmfd, "LMFD_07");
                }

                // Impact Angle (GBU-54)
                if (settings.Munition.ImpactParams && !settings.IsImpactAngleDefault)
                {
                    AddAction(cdu, "CLR");
                    AddActions(cdu, ActionsForCleanNum(settings.ImpactAngle));
                    AddAction(lmfd, "LMFD_08");
                }

                // Desired Time of Fall (dumb bombs)
                if (settings.Munition.DesTOF && !settings.IsDesTOFDefault)
                {
                    AddAction(cdu, "CLR");
                    AddActions(cdu, ActionsForString(settings.DesTOF));
                    AddAction(lmfd, "LMFD_" + settings.Munition.DesTOFButton);
                }

                // Minimum Altitude (dumb bombs)
                if (settings.Munition.MinAltField && !settings.IsMinAltDefault)
                {
                    AddAction(cdu, "CLR");
                    AddActions(cdu, ActionsForCleanNum(settings.MinAlt));
                    AddAction(lmfd, "LMFD_" + settings.Munition.MinAltButton);
                }

                // Minimum Time of Fall (BDU-33)
                if (settings.Munition.MinTOF && !settings.IsMinTOFDefault)
                {
                    AddAction(cdu, "CLR");
                    AddActions(cdu, ActionsForString(settings.MinTOF));
                    AddAction(lmfd, "LMFD_" + settings.Munition.MinTOFButton);
                }

                // High-Drag Time of Fall (Mk-82 Air, BDU-50PO)
                if (settings.Munition.DragTOF && !settings.IsHDTOFDefault)
                {
                    AddAction(cdu, "CLR");
                    AddActions(cdu, ActionsForString(settings.HDTOF));
                    AddAction(lmfd, "LMFD_" + settings.Munition.HDTOFButton);
                }

                // Low-Drag Time of Fall (Mk-82 Air, BDU-50PO)
                if (settings.Munition.DragTOF && !settings.IsLDTOFDefault)
                {
                    AddAction(cdu, "CLR");
                    AddActions(cdu, ActionsForString(settings.LDTOF));
                    AddAction(lmfd, "LMFD_" + settings.Munition.LDTOFButton);
                }

                // Lase Seconds
                if (settings.Munition.Laser && !settings.IsLaseSecondsDefault)
                {
                    AddAction(cdu, "CLR");
                    AddActions(cdu, ActionsForCleanNum(settings.LaseSeconds));
                    AddAction(lmfd, "LMFD_17");
                }
            }

            // Save
            AddWait(WAIT_BASE);
            AddAction(lmfd, "LMFD_03");
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // custom (named) profiles
        //
        // ------------------------------------------------------------------------------------------------------------

        private class CustomProfileBuildItem
        {
            public int BaseIndex;
            public A10CMunition Munition;
            public string Name;
            public MunitionSettings Settings;
        }

        private static string EffVal(string value, string dflt) => string.IsNullOrEmpty(value) ? dflt : value;

        /// <summary>
        /// returns the text to type for a numeric setting that should change from the inherited value to the
        /// target value, or null if no change is needed. a cleared target changes a non-empty inherited value to "0".
        /// </summary>
        private static string NumChange(string target, string inherited)
        {
            string t = target ?? "";
            string i = inherited ?? "";
            if (t == i)
                return null;
            return (t == "") ? "0" : t;
        }

        /// <summary>
        /// Create the custom (named) profiles. Each is made by selecting the profile of the munition it is based on,
        /// opening the profile (VIEW PRO), typing the new name on the CDU, pressing NEW, setting the profile's values,
        /// and saving. Runs after the default profiles are modified and sorted, so the new profile starts as a copy
        /// of the (possibly modified) default profile; settings are therefore applied relative to that copy.
        /// Profiles are created from the lowest base profile on the list up so earlier list positions do not move.
        /// </summary>
        private void Build_CustomProfiles(AirframeDevice cdu, AirframeDevice lmfd)
        {
            List<DSMSSystem.CustomProfileInfo> customs = _cfg.DSMS.GetAllCustomProfiles();
            if (customs.Count == 0)
                return;

            List<string> onJet = _profileQuery.Profiles;
            List<CustomProfileBuildItem> items = new();
            foreach (DSMSSystem.CustomProfileInfo custom in customs)
            {
                if (onJet.Contains(custom.Name))
                    continue;                                       // already exists on the jet
                int baseIndex = onJet.IndexOf(custom.Munition.Profile);
                if (baseIndex < 0)
                    continue;                                       // munition's profile is not on the jet
                items.Add(new CustomProfileBuildItem()
                {
                    BaseIndex = baseIndex,
                    Munition = custom.Munition,
                    Name = custom.Name,
                    Settings = custom.Settings
                });
            }
            items.Sort((a, b) => (a.BaseIndex != b.BaseIndex) ? b.BaseIndex.CompareTo(a.BaseIndex)
                                                              : string.CompareOrdinal(a.Name, b.Name));

            foreach (CustomProfileBuildItem item in items)
            {
                AddActions(lmfd, new() { "LMFD_14", "LMFD_01" }, null, WAIT_BASE); // Go to DSMS Profiles
                SelectProfile(lmfd, 0, item.BaseIndex);
                AddAction(lmfd, "LMFD_03");                                         // VIEW PRO

                AddAction(cdu, "CLR");
                AddActions(cdu, ActionsForString(item.Name.ToUpper()));
                AddAction(lmfd, "LMFD_18");                                         // NEW

                Build_CustomProfileSettings(cdu, lmfd, item.Settings);

                AddWait(WAIT_BASE);
                AddAction(lmfd, "LMFD_03");                                         // SAVE
                AddAction(lmfd, "LMFD_01");                                         // STAT, back to DSMS Main

                onJet.Add(item.Name);                                               // new profiles go on the end of the list
            }
        }

        // weapon category order for the profile list: precision, general purpose, rockets, maverick
        private static int WeaponCategory(A10CMunition munition)
        {
            switch (munition.ID)
            {
                case 3: case 4: case 5: case 6: case 7: case 8: case 9:     // CBU-103, CBU-105, GBU-10/12/31/38/54
                    return 0;
                case 10: case 11:                                           // APKWS
                    return 2;
                case 12: case 13:                                           // Maverick
                    return 3;
                default:                                                    // CBU-87, CBU-97, dumb bombs
                    return 1;
            }
        }

        // default profiles that are moved to the end of the profile list
        private static readonly HashSet<string> _tailProfiles = new()
        {
            "MK-82", "MK82APO", "BDU-33", "BDU-50PO", "BDU-50LD", "BDU-50LGB"
        };

        private static readonly Regex _sledNameRegex = new(@"(\d{2})(LD|DB|HA|HD)(\d)\s*$");

        private class ProfileSortKey
        {
            public string Name;
            public int Index;
            public int Tail;                                                // 1 for default profiles moved to the end
            public int Category;
            public int MunitionOrder;
            public int DeliveryRank;                                        // default profile, TOSS, HA, DB, LD, HD, other
            public int Angle;
            public int Qty;
        }

        /// <summary>
        /// Sort the profile list: weapons precision, general purpose, rockets, maverick (or the configured profile
        /// order, if set); within a weapon, TOSS, diving high-altitude to low, steep to shallow, and release quantity
        /// high to low. The default profiles named MK-82, MK82APO, BDU-33, BDU-50PO, BDU-50LD and BDU-50LGB are always
        /// moved to the end of the list. Without custom profiles only that move is made (the list is otherwise left
        /// as is). New profiles are on the end of the list when created.
        /// </summary>
        private void Build_CustomProfileLayout(AirframeDevice lmfd)
        {
            List<DSMSSystem.CustomProfileInfo> customs = _cfg.DSMS.GetAllCustomProfiles();
            List<string> list = _profileQuery.Profiles;
            bool fullSort = customs.Count > 0;                      // without custom profiles only move the tail profiles
            int sel = 0;

            // desired order of rows 1..n-1 (row 0 is WPNS OFF)
            Dictionary<string, A10CMunition> customMunitions = new();
            foreach (DSMSSystem.CustomProfileInfo c in customs)
                customMunitions[c.Name.ToUpperInvariant()] = c.Munition;

            List<ProfileSortKey> keys = new();
            for (int i = 1; i < list.Count; i++)
            {
                string name = list[i];
                A10CMunition m = customMunitions.TryGetValue(name.ToUpperInvariant(), out A10CMunition cm)
                                    ? cm : A10CMunition.GetMunitionFromProfile(name);
                ProfileSortKey key = new() { Name = name, Index = i, Category = 99, MunitionOrder = 0, DeliveryRank = 9 };
                if (_tailProfiles.Contains(name.ToUpperInvariant()))
                    key.Tail = 1;                                       // dumb bomb default profiles go after all others
                if (m != null)
                {
                    if (!_cfg.DSMS.IsProfileOrderDefault)
                    {
                        key.Category = 0;
                        key.MunitionOrder = _cfg.DSMS.GetOrderedProfilePosition(m.Profile);
                    }
                    else
                    {
                        key.Category = WeaponCategory(m);
                        key.MunitionOrder = _profileQuery.MunitionProfileIndexMap.TryGetValue(m.ID, out int idx)
                                                ? idx : 1000 + m.ID;
                    }
                    if (string.Equals(name, m.Profile, System.StringComparison.OrdinalIgnoreCase))
                        key.DeliveryRank = -2;
                    else if (name.ToUpperInvariant().Contains("TOSS"))
                        key.DeliveryRank = -1;
                    else
                    {
                        Match match = _sledNameRegex.Match(name);
                        if (match.Success)
                        {
                            key.DeliveryRank = (match.Groups[2].Value) switch { "HA" => 0, "DB" => 1, "LD" => 2, _ => 3 };
                            key.Angle = int.Parse(match.Groups[1].Value);
                            key.Qty = int.Parse(match.Groups[3].Value);
                        }
                    }
                }
                keys.Add(key);
            }
            keys.Sort((a, b) =>
            {
                int r = a.Tail.CompareTo(b.Tail);
                if (r == 0 && !fullSort) r = a.Index.CompareTo(b.Index);
                if (r == 0) r = a.Category.CompareTo(b.Category);
                if (r == 0) r = a.MunitionOrder.CompareTo(b.MunitionOrder);
                if (r == 0) r = a.DeliveryRank.CompareTo(b.DeliveryRank);
                if (r == 0) r = b.Angle.CompareTo(a.Angle);
                if (r == 0) r = b.Qty.CompareTo(a.Qty);
                if (r == 0) r = string.CompareOrdinal(a.Name, b.Name);
                if (r == 0) r = a.Index.CompareTo(b.Index);
                return r;
            });
            List<string> target = new() { list[0] };
            target.AddRange(keys.Select(k => k.Name));
            if (target.SequenceEqual(list))
                return;                                                 // already in order, nothing to do on the jet

            AddActions(lmfd, new() { "LMFD_14", "LMFD_01" }, null, WAIT_BASE); // Go to DSMS Profiles

            // place rows from the bottom up. the only reorder control moves the selected profile down one row.
            for (int row = list.Count - 1; row >= 1; row--)
            {
                int from = -1;
                for (int i = 1; i <= row; i++)
                {
                    if (list[i] == target[row])
                    {
                        from = i;
                        break;
                    }
                }
                if (from < 0 || from == row)
                    continue;
                sel = SelectProfile(lmfd, sel, from);
                for (int i = from; i < row; i++)
                {
                    AddAction(lmfd, "LMFD_07");                             // move down
                    (list[i], list[i + 1]) = (list[i + 1], list[i]);
                }
                sel = row;
            }

            AddAction(lmfd, "LMFD_01");                                     // STAT, back to DSMS Main
        }

        /// <summary>
        /// Set the values of a newly created custom profile. The new profile starts as a copy of the munition's
        /// default profile, so changes are computed relative to the default profile's settings as modified by the
        /// configuration (or the jet defaults if the default profile is not modified).
        /// </summary>
        private void Build_CustomProfileSettings(AirframeDevice cdu, AirframeDevice lmfd, MunitionSettings target)
        {
            A10CMunition m = target.Munition;
            MunitionSettings dflt = MunitionSettings.ExplicitDefaults;
            MunitionSettings baseSettings = _cfg.DSMS.GetMunitionSettings(m);
            MunitionSettings inh = baseSettings.IsProfileDefault ? dflt : baseSettings;

            // "front page" settings

            if (m.Pairs || m.Ripple)
            {
                int t = int.Parse(EffVal(target.ReleaseMode, dflt.ReleaseMode));
                int i = int.Parse(EffVal(inh.ReleaseMode, dflt.ReleaseMode));
                for (int n = 0; n < ((t - i + 4) % 4); n++)
                    AddAction(lmfd, "LMFD_06");
            }

            if (m.Fuze)
            {
                int t = int.Parse(EffVal(target.FuzeOption, dflt.FuzeOption));
                int i = int.Parse(EffVal(inh.FuzeOption, dflt.FuzeOption));
                for (int n = 0; n < ((t - i + 3) % 3); n++)
                    AddAction(lmfd, "LMFD_07");
            }

            if (m.Ripple)
            {
                string t = EffVal(target.RippleQty, dflt.RippleQty);
                string i = EffVal(inh.RippleQty, dflt.RippleQty);
                if (t != i)
                {
                    AddAction(cdu, "CLR");
                    AddActions(cdu, ActionsForCleanNum(t));
                    AddAction(lmfd, "LMFD_08");
                }
            }

            if (m.RipFt)
            {
                string t = EffVal(target.RippleFt, dflt.RippleFt);
                string i = EffVal(inh.RippleFt, dflt.RippleFt);
                if (t != i)
                {
                    AddAction(cdu, "CLR");
                    AddActions(cdu, ActionsForCleanNum(t));
                    AddAction(lmfd, "LMFD_09");
                }
            }

            if (m.CCIP && m.CCRP)
            {
                int t = int.Parse(EffVal(target.DeliveryMode, target.DefaultDeliveryMode));
                int i = int.Parse(EffVal(inh.DeliveryMode, target.DefaultDeliveryMode));
                for (int n = 0; n < ((t - i + 2) % 2); n++)
                    AddAction(lmfd, "LMFD_10");
            }

            // "CHG SET" page settings

            int escTarget = int.Parse(EffVal(target.EscapeManeuver, dflt.EscapeManeuver));
            int escInherit = int.Parse(EffVal(inh.EscapeManeuver, dflt.EscapeManeuver));
            bool doEsc = m.EscMnvr && (escTarget != escInherit);
            bool doAutoLase = m.Laser && (EffVal(target.AutoLase, dflt.AutoLase) != EffVal(inh.AutoLase, dflt.AutoLase));
            string impAz = m.ImpactParams ? NumChange(target.ImpactAzimuth, inh.ImpactAzimuth) : null;
            string impAng = m.ImpactParams ? NumChange(target.ImpactAngle, inh.ImpactAngle) : null;
            string desTOF = m.DesTOF ? NumChange(target.DesTOF, inh.DesTOF) : null;
            string minAlt = m.MinAltField ? NumChange(target.MinAlt, inh.MinAlt) : null;
            string minTOF = m.MinTOF ? NumChange(target.MinTOF, inh.MinTOF) : null;
            string hdTOF = m.DragTOF ? NumChange(target.HDTOF, inh.HDTOF) : null;
            string ldTOF = m.DragTOF ? NumChange(target.LDTOF, inh.LDTOF) : null;
            string laseSecs = m.Laser ? NumChange(target.LaseSeconds, inh.LaseSeconds) : null;

            if (doEsc || doAutoLase || (impAz != null) || (impAng != null) || (desTOF != null) || (minAlt != null) ||
                (minTOF != null) || (hdTOF != null) || (ldTOF != null) || (laseSecs != null))
            {
                AddAction(lmfd, "LMFD_16");                                         // CHG SET

                if (doEsc)
                {
                    int numPresses = GetNumClicksForWraparoundSetting<EscapeManeuvers>(escInherit, escTarget);
                    for (int i = 0; i < numPresses; i++)
                        AddAction(lmfd, "LMFD_20");
                }
                if (doAutoLase)
                    AddAction(lmfd, "LMFD_06");

                void Enter(string value, string button, bool clean)
                {
                    if (value == null)
                        return;
                    AddAction(cdu, "CLR");
                    AddActions(cdu, clean ? ActionsForCleanNum(value) : ActionsForString(value));
                    AddAction(lmfd, button);
                }

                Enter(impAz, "LMFD_07", true);
                Enter(impAng, "LMFD_08", true);
                Enter(desTOF, "LMFD_" + m.DesTOFButton, false);
                Enter(minAlt, "LMFD_" + m.MinAltButton, true);
                Enter(minTOF, "LMFD_" + m.MinTOFButton, false);
                Enter(hdTOF, "LMFD_" + m.HDTOFButton, false);
                Enter(ldTOF, "LMFD_" + m.LDTOFButton, false);
                Enter(laseSecs, "LMFD_17", true);
            }
        }

        /// <summary>
        /// Modify the order of the default weapon profiles.
        /// Important to do this last because it will invalidate the content of _profileQuery.MunitionProfileIndexMap
        /// </summary>
        /// <param name="lmfd"></param>
        private void Build_DefaultProfileOrder(AirframeDevice lmfd)
        {
            if (_cfg.DSMS.IsProfileOrderDefault)
                return;

            AddActions(lmfd, new() { "LMFD_14", "LMFD_01" }, null, WAIT_BASE); // Go to DSMS Profiles

            AddAction(lmfd, "LMFD_19"); // arrow down to first profile past WPNS OFF
            int selectedProfileIndex = 1;

            // simple bubble sort of the profiles according to configured order
            // TODO there's probably some .NET sorting base class that would be nicer here.
            List<string> profiles = _profileQuery.Profiles;
            for (int i = 1; i < profiles.Count - 1; i++)
            {
                bool changeMade = false;
                for (int j = 1; j < profiles.Count - i; j++)
                {
                    if (_cfg.DSMS.GetOrderedProfilePosition(profiles[j]) > _cfg.DSMS.GetOrderedProfilePosition(profiles[j + 1]))
                    {
                        selectedProfileIndex = bubbleSortSwap(lmfd, selectedProfileIndex, profiles, j);
                        changeMade = true;
                    }
                }

                if (changeMade == false)
                    break;
            }

            // Back to DSMS Main
            AddAction(lmfd, "LMFD_01"); // STAT
        }

        private int bubbleSortSwap(AirframeDevice lmfd, int selectedProfileIndex, List<string> profiles, int firstToSwapIndex)
        {
            // swap on the jet
            SelectProfile(lmfd, selectedProfileIndex, firstToSwapIndex);
            AddAction(lmfd, "LMFD_07"); // move down

            // swap in-memory
            string first = profiles[firstToSwapIndex];
            string second = profiles[firstToSwapIndex + 1];
            profiles[firstToSwapIndex] = second;
            profiles[firstToSwapIndex + 1] = first;

            return firstToSwapIndex + 1;
        }

        private class ProfileOrderComparer : IComparer<string>
        {
            private readonly Dictionary<string, int> _targetOrder;

            public ProfileOrderComparer(Dictionary<string, int> targetOrder)
            {
                _targetOrder = targetOrder;
            }

            public int Compare(string x, string y)
            {
                return _targetOrder[x] - _targetOrder[y];
            }
        }
        
        /// <summary>
        /// Moves the selection indicator on the DSMS profile's page to the profile at toSelectIndex.
        /// Assumes we are already on the DSMS profile page.
        /// </summary>
        /// <param name="lmfd"></param>
        /// <param name="currentSelectionIndex"></param>
        /// <param name="toSelectIndex"></param>
        /// <returns>The new, current selected index.</returns>
        private int SelectProfile(AirframeDevice lmfd, int currentSelectionIndex, int toSelectIndex)
        {
            if (toSelectIndex > currentSelectionIndex)
            {
                for (int i = 0; i < toSelectIndex - currentSelectionIndex; i++)
                    AddAction(lmfd, "LMFD_19"); // arrow down
            }
            else if (toSelectIndex < currentSelectionIndex)
            {
                for (int i = 0; i < currentSelectionIndex - toSelectIndex; i++)
                    AddAction(lmfd, "LMFD_20"); // arrow up
            }

            return toSelectIndex;
        }

        private static string GetButtonForStation(int station) => _stationButtonMap[station];
        private static readonly Dictionary<int, string> _stationButtonMap = new Dictionary<int, string>
        {
            {  1, "LMFD_16" },
            {  2, "LMFD_17" },
            {  3, "LMFD_18" },
            {  4, "LMFD_19" },
            {  5, "LMFD_20" },
            {  6, "LMFD_03" },
            {  7, "LMFD_06" },
            {  8, "LMFD_07" },
            {  9, "LMFD_08" },
            { 10, "LMFD_09" },
            { 11, "LMFD_10" },
        };

        private static int GetSymmetricStation(int station) => _symStationMap[station];
        private static readonly Dictionary<int, int> _symStationMap = new Dictionary<int, int>
        {
            {  1, 11 },
            {  2, 10 },
            {  3,  9 },
            {  4,  8 },
            {  5,  7 },
            {  6, -1 },
            {  7,  5 },
            {  8,  4 },
            {  9,  3 },
            { 10,  2 },
            { 11,  1 },
        };

        private static Dictionary<int, bool> GetStationIsSetupMap() => new Dictionary<int, bool>() 
        {
            {  1, false },
            {  2, false },
            {  3, false },
            {  4, false },
            {  5, false },
            {  6, false },
            {  7, false },
            {  8, false },
            {  9, false },
            { 10, false },
            { 11, false }
        };
    }
}
