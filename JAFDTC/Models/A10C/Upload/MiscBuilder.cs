// ********************************************************************************************************************
//
// MiscBuilder.cs -- a-10c misc system builder
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

using JAFDTC.Models.A10C.Misc;
using JAFDTC.Models.A10C.WYPT;
using JAFDTC.Models.DCS;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace JAFDTC.Models.A10C.Upload
{
    /// <summary>
    /// command builder for miscellaneous systems in the warthog. translates cmds setup in A10CConfiguration into
    /// commands that drive the dcs clickable cockpit.
    /// </summary>
    internal class MiscBuilder : A10CBuilderBase, IBuilder
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public MiscBuilder(A10CConfiguration cfg, A10CDeviceManager dcsCmds, StringBuilder sb) : base(cfg, dcsCmds, sb) { }

        // ------------------------------------------------------------------------------------------------------------
        //
        // build methods
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// configure misc systems via the cdu and ffds according to the non-default programming settings (this function is
        /// safe to call with a configuration with default settings: defaults are skipped as necessary).
        /// </summary>
        public override void Build(Dictionary<string, object> state = null)
        {
            if (_cfg.Misc.IsDefault)
                return;
                
            AddExecFunction("NOP", new() { "==== MiscBuilder:Build()" });

            AirframeDevice cdu = _aircraft.GetDevice("CDU");
            AirframeDevice ufc = _aircraft.GetDevice("UFC");
            AirframeDevice aap = _aircraft.GetDevice("AAP"); // "auxiliary avionics panel"
            AirframeDevice ap  = _aircraft.GetDevice("AUTOPILOT");
            AirframeDevice tacan  = _aircraft.GetDevice("TACAN_CTRL_PANEL");
            AirframeDevice iff  = _aircraft.GetDevice("IFF");
            AirframeDevice lmfd = _aircraft.GetDevice("LMFD");
            AirframeDevice rmfd = _aircraft.GetDevice("RMFD");

            BuildCoordSystem(cdu, _cfg.Misc);
            BuildBullseyeOnHUD(cdu, ufc, _cfg.Misc);
            BuildFlightPlan1Manual(cdu, _cfg.Misc);
            BuildFlightPlans(cdu, _cfg.Misc);
            BuildSpeedDisplay(cdu, ufc, _cfg.Misc);
            BuildAapSteerPt(aap, _cfg.Misc);
            BuildAapPage(aap, _cfg.Misc);
            BuildAutopilot(ap, _cfg.Misc);
            BuildAltitudeWarnings(ufc, _cfg.Misc);
            BuildTACAN(tacan, _cfg.Misc);
            BuildIFF(iff, _cfg.Misc);
            BuildMFCDOptions(lmfd, rmfd, _cfg.Misc);
        }

        /// <summary>
        /// configure the coordinate system according to the non-default programming settings
        /// </summary>
        /// <param name="cdu"></param>
        /// <param name="miscSystem"></param>
        private void BuildCoordSystem(AirframeDevice cdu, MiscSystem miscSystem)
        {
            if (miscSystem.IsCoordSystemDefault)
                return;

            // CDU
            AddActions(cdu, new() { "WP", "LSK_3L" });
            AddWait(WAIT_BASE);
            AddIfBlock("IsCoordFmtLL", true, null, delegate ()
            {
                AddAction(cdu, "LSK_9R");
            });

            // TAD
            // Leaving this as a reference for changing the coordinate system on the TAD
            // when we add it as its own system later.
            // AddActions(lmfd, new() { "LMFD_15", "LMFD_09" });
        }

        /// <summary>
        /// configure the bullseye on hud setting according to the non-default programming settings
        /// </summary>
        /// <param name="cdu"></param>
        /// <param name="miscSystem"></param>
        private void BuildBullseyeOnHUD(AirframeDevice cdu, AirframeDevice ufc, MiscSystem miscSystem)
        {
            if (miscSystem.IsBullseyeOnHUDDefault)
                return;

            // Navigate to WAYPT page with UFC
            AddActions(ufc, new() { "FN", "SPC" });

            // Navigate to ANCHOR PT page on  CDU
            AddActions(cdu, new() { "CLR", "LSK_7R" });
            // Turn BULLS ON if not already
            AddIfBlock("IsBullsNotOnHUD", true, null, delegate ()
            {
                AddAction(cdu, "LSK_9L");
            });
        }

        /// <summary>
        /// configure the first flight plan's manual/auto setting according to the non-default programming settings
        /// </summary>
        /// <param name="cdu"></param>
        /// <param name="miscSystem"></param>
        private void BuildFlightPlan1Manual(AirframeDevice cdu, MiscSystem miscSystem)
        {
            if (miscSystem.IsFlightPlan1ManualDefault)
                return;

            // CDU
            AddAction(cdu, "FPM");
            AddIfBlock("IsFlightPlanNotManual", true, null, delegate ()
            {
                AddAction(cdu, "LSK_3L");
            });
        }

        /// <summary>
        /// builds flight plan 1 (and optionally flight plans 2 and 3) on the cdu from the waypoints in the
        /// configuration. waypoints must already be on the jet (the waypoint system uploads first). entry 01 of
        /// flight plan 1 (INIT POSIT) is never touched, entries start at 02.
        /// </summary>
        private void BuildFlightPlans(AirframeDevice cdu, MiscSystem miscSystem)
        {
            if (!miscSystem.IsBuildFlightPlanValue || _cfg.WYPT.Points.Count == 0)
                return;

            List<WaypointInfo> all = new();
            foreach (WaypointInfo w in _cfg.WYPT.Points)
                if (w.IsValid)
                    all.Add(w);
            if (all.Count == 0)
                return;

            AddExecFunction("NOP", new() { "==== MiscBuilder:BuildFlightPlans()" });

            // plan 1: every waypoint, starting at entry 02
            BuildOnePlan(cdu, 1, null, 2, all);

            AddAction(cdu, "FPM");
        }

        /// <summary>
        /// enters the waypoints into a flight plan, three entries per cdu page (LSK 5L/7L/9L), paging with the PG
        /// rocker. startEntry is the 1-based entry to start at.
        /// </summary>
        private void BuildOnePlan(AirframeDevice cdu, int planNum, string newPlanName, int startEntry, List<WaypointInfo> wypts)
        {
            string[] planKeys = { "LSK_3R", "LSK_5R", "LSK_7R" };
            string[] entryKeys = { "LSK_5L", "LSK_7L", "LSK_9L" };

            AddAction(cdu, "FPM");
            AddWait(WAIT_BASE);
            AddActions(cdu, new() { "CLR", "CLR" });

            if (newPlanName != null)
            {
                string planName = Regex.Replace(newPlanName.ToUpper(), "[^A-Z0-9 ]", "");
                AddActions(cdu, ActionsForString(planName), new() { "LSK_9L" });
                AddWait(WAIT_BASE);
                AddActions(cdu, new() { "CLR", "CLR" });
            }

            // open FPBUILD for the plan
            AddAction(cdu, planKeys[planNum - 1]);
            AddWait(WAIT_BASE);

            int entry = startEntry;
            int page = (entry - 1) / 3;
            for (int i = 0; i < page; i++)
            {
                AddWait(WAIT_LONG);
                AddAction(cdu, "PG_DOWN");
                AddWait(WAIT_LONG);
            }

            foreach (WaypointInfo wypt in wypts)
            {
                int wantPage = (entry - 1) / 3;
                while (page < wantPage)
                {
                    AddWait(WAIT_LONG);
                    AddAction(cdu, "PG_DOWN");
                    AddWait(WAIT_LONG);
                    page++;
                }
                string name = WYPTBuilder.NavpointCduName(wypt.Number.ToString(), wypt.Name);
                AddActions(cdu, ActionsForString(name), new() { entryKeys[(entry - 1) % 3] });
                AddWait(WAIT_BASE);
                entry++;
            }
        }

        /// <summary>
        /// configure the CDU Steerpoint page speed display setting according to the non-default programming settings
        /// </summary>
        /// <param name="cdu"></param>
        /// <param name="miscSystem"></param>
        private void BuildSpeedDisplay(AirframeDevice cdu, AirframeDevice ufc, MiscSystem miscSystem)
        {
            if (miscSystem.IsSpeedDisplayDefault)
                return; // IAS

            // Navigate to STEER page with UFC
            AddActions(ufc, new() { "FN", "0" });

            // Alter speed display setting itself on the CDU

            // During startup, before alignment, the speed setting is not yet visible but you can still change it
            // with button presses. In this state we assume it's still at the default IAS and press the button
            // once for TAS, twice for GS.
            AddIfBlock("IsSpeedAvailable", false, null, delegate ()
            {
                AddAction(cdu, "LSK_9R"); // TAS
                if (miscSystem.SpeedDisplayValue == SpeedDisplayOptions.GS)
                    AddAction(cdu, "LSK_9R"); // GS
            });

            // TODO consider an "else" delegate for if blocks?

            // If we're aligned and can see the setting, we can just press the button until it says what we want.
            AddIfBlock("IsSpeedAvailable", true, null, delegate ()
            {
                AddWhileBlock("SpeedIsNot", true, new() { $"{miscSystem.SpeedDisplayValue}" }, delegate ()
                {
                    AddAction(cdu, "LSK_9R");
                });
            });
        }

        /// <summary>
        /// configure the AAP Steerpoint knob setting according to the non-default programming settings
        /// </summary>
        /// <param name="cdu"></param>
        /// <param name="miscSystem"></param>
        private void BuildAapSteerPt(AirframeDevice aap, MiscSystem miscSystem)
        {
            if (miscSystem.IsAapSteerPtDefault)
                return; // Flt Plan

            switch (miscSystem.AapSteerPtValue)
            {
                case AapSteerPtOptions.Mark:
                    AddAction(aap, "STEER_MARK");
                    break;
                case AapSteerPtOptions.Mission:
                    AddAction(aap, "STEER_MISSION");
                    break;
            }
        }

        /// <summary>
        /// configure the AAP Page knob setting according to the non-default programming settings
        /// </summary>
        /// <param name="cdu"></param>
        /// <param name="miscSystem"></param>
        private void BuildAapPage(AirframeDevice aap, MiscSystem miscSystem)
        {
            if (miscSystem.IsAapPageDefault)
                return; // Other

            switch (miscSystem.AapPageValue)
            {
                case AapPageOptions.Position:
                    AddAction(aap, "PAGE_POSITION");
                    break;
                case AapPageOptions.Steer:
                    AddAction(aap, "PAGE_STEER");
                    break;
                case AapPageOptions.Waypt:
                    AddAction(aap, "PAGE_WAYPT");
                    break;
            }
        }

        private void BuildAutopilot(AirframeDevice ap, MiscSystem miscSystem)
        {
            if (miscSystem.IsAutopilotModeDefault)
                return; // Alt/Hdg

            int setValue = miscSystem.AutopilotModeValue switch
            {
                AutopilotModeOptions.Path => 1,
                AutopilotModeOptions.AltHdg => 0,
                AutopilotModeOptions.Alt => -1,
                _ => throw new NotImplementedException()
            };
            AddDynamicAction(ap, "AP_MODE", setValue, setValue);
        }

        private void BuildAltitudeWarnings(AirframeDevice ufc, MiscSystem miscSystem)
        {
            if (miscSystem.IsAGLFloorDefault && miscSystem.IsMSLFloorDefault && miscSystem.IsMSLCeilingDefault)
                return;

            AddAction(ufc, "CLR");

            int queuedAltAlrtPresses = 0;
            if (!miscSystem.IsAGLFloorDefault)
            {
                AddAction(ufc, "ALT_ALRT");
                AddActions(ufc, ActionsForCleanNum(miscSystem.AGLFloor));
                AddAction(ufc, "ENTER");
            }
            else
                queuedAltAlrtPresses++;

            if (!miscSystem.IsMSLFloorDefault)
            {
                queuedAltAlrtPresses = UnqueueAltAlrtPresses(ufc, queuedAltAlrtPresses);
                AddAction(ufc, "ALT_ALRT");
                AddActions(ufc, ActionsForCleanNum(miscSystem.MSLFloor));
                AddAction(ufc, "ENTER");
            }
            else
                queuedAltAlrtPresses++;

            if (!miscSystem.IsMSLCeilingDefault)
            {
                UnqueueAltAlrtPresses(ufc, queuedAltAlrtPresses);
                AddAction(ufc, "ALT_ALRT");
                AddActions(ufc, ActionsForCleanNum(miscSystem.MSLCeiling));
                AddAction(ufc, "ENTER");
                AddAction(ufc, "ALT_ALRT");
            }
        }

        private int UnqueueAltAlrtPresses(AirframeDevice ufc, int count)
        {
            for (int i = 0; i < count; i++)
                AddAction(ufc, "ALT_ALRT");
            return 0;
        }

        private void BuildTACAN(AirframeDevice tacan, MiscSystem miscSystem)
        {
            // Channel
            if (!miscSystem.IsTACANChannelDefault)
            {
                int onesValue = miscSystem.TACANChannelValue % 10;
                int tensValue = (miscSystem.TACANChannelValue - onesValue) / 10;

                for (int i = 0; i < tensValue; i++)
                    AddAction(tacan, "TENS_UP");
                for (int i = 0; i < onesValue; i++)
                    AddAction(tacan, "ONES_UP");
            }

            // Band
            if (!miscSystem.IsTACANBandDefault)
            {
                if (miscSystem.TACANBandValue == TACANBandOptions.X)
                    AddAction(tacan, "X_BAND");
                else
                    AddAction(tacan, "Y_BAND");

            }

            // Mode
            if (!miscSystem.IsTACANModeDefault)
            {
                switch (miscSystem.TACANModeValue)
                {
                    case TACANModeOptions.Off:
                        AddAction(tacan, "MODE_OFF");
                        break;
                    case TACANModeOptions.Rec:
                        AddAction(tacan, "MODE_REC");
                        break;
                    case TACANModeOptions.Tr:
                        AddAction(tacan, "MODE_TR");
                        break;
                    case TACANModeOptions.AaRec:
                        AddAction(tacan, "MODE_AA_REC");
                        break;
                    case TACANModeOptions.AaTr:
                        AddAction(tacan, "MODE_AA_TR");
                        break;
                    default:
                        throw new ApplicationException("Unexpected TACAN Mode: " + miscSystem.TACANModeValue);
                }
            }
        }

        // OSB (on the MFCD page-select menu) that selects each page, indexed by MFCDPageOptions
        //
        private static readonly Dictionary<MFCDPageOptions, string> _mfcdPageOSB = new()
        {
            { MFCDPageOptions.COMM, "06" },
            { MFCDPageOptions.STAT, "07" },
            { MFCDPageOptions.MAV,  "09" },
            { MFCDPageOptions.CDU,  "16" },
            { MFCDPageOptions.MSG,  "17" },
            { MFCDPageOptions.DSMS, "18" },
            { MFCDPageOptions.TGP,  "19" },
            { MFCDPageOptions.TAD,  "20" }
        };

        /// <summary>
        /// configure the page assigned to MFCD OSB 12-15 on both MFCDs according to the non-default settings
        /// </summary>
        private void BuildMFCDOptions(AirframeDevice lmfd, AirframeDevice rmfd, MiscSystem miscSystem)
        {
            BuildMFCDSide(lmfd, "LMFD", new string[] {
                miscSystem.LeftMFCDOSB12, miscSystem.LeftMFCDOSB13, miscSystem.LeftMFCDOSB14, miscSystem.LeftMFCDOSB15 });
            BuildMFCDSide(rmfd, "RMFD", new string[] {
                miscSystem.RightMFCDOSB12, miscSystem.RightMFCDOSB13, miscSystem.RightMFCDOSB14, miscSystem.RightMFCDOSB15 });
        }

        private void BuildMFCDSide(AirframeDevice mfd, string prefix, string[] settings)
        {
            bool isAnySet = false;
            foreach (string setting in settings)
                isAnySet |= (!string.IsNullOrEmpty(setting) && (setting != "0"));
            if (!isAnySet)
                return;

            // long press OSB 12 to open the page-assignment menu
            AddAction(mfd, prefix + "_12_LONG");
            for (int i = 0; i < settings.Length; i++)
            {
                if (string.IsNullOrEmpty(settings[i]) || (settings[i] == "0"))
                    continue;
                MFCDPageOptions page = (MFCDPageOptions)int.Parse(settings[i]);
                if (_mfcdPageOSB.TryGetValue(page, out string pageOSB))
                {
                    AddAction(mfd, prefix + "_" + pageOSB);                  // pick the page from the menu...
                    AddAction(mfd, prefix + "_" + (12 + i).ToString());      // ...and assign it to the OSB
                }
            }

            // once the bottom layout is changed, press OSB 12 (left MFCD) or OSB 13 (right MFCD) to leave the assignment menu
            AddAction(mfd, prefix + ((prefix == "LMFD") ? "_12" : "_13"));
        }

        private void BuildIFF(AirframeDevice iff, MiscSystem miscSystem)
        {
            // Master Mode
            if (!miscSystem.IsIFFMasterModeDefault)
            {
                switch (miscSystem.IFFMasterModeValue)
                {
                    case IFFMasterOptions.OFF:
                        AddAction(iff, "MASTER_OFF");
                        break;
                    case IFFMasterOptions.STBY:
                        AddAction(iff, "MASTER_STBY");
                        break;
                    case IFFMasterOptions.NORM:
                        AddAction(iff, "MASTER_NORM");
                        break;
                    default:
                        throw new ApplicationException("Unexpected IFF Master Mode: " + miscSystem.IFFMasterModeValue);
                }
            }

            // Mode 4 ON
            if (!miscSystem.IsIFFMode4OnDefault)
                AddAction(iff, "MODE4_ON");

            // Mode 3 Code
            if (!miscSystem.IsIFFMode3CodeDefault)
            {
                int codeVal = int.Parse(miscSystem.IFFMode3CodeValue);
                int ones = codeVal % 10;
                int tens = (codeVal - ones) / 10 % 10;
                int hundreds = (codeVal - tens) / 100 % 10;
                int thousands = (codeVal - hundreds) / 1000 % 10;

                AddDynamicAction(iff, "MODE3A-WHEEL1_UP", thousands * 0.1, thousands * 0.1);
                AddDynamicAction(iff, "MODE3A-WHEEL2_UP", hundreds * 0.1, hundreds * 0.1);
                AddDynamicAction(iff, "MODE3A-WHEEL3_UP", tens * 0.1, tens * 0.1);
                AddDynamicAction(iff, "MODE3A-WHEEL4_UP", ones * 0.1, ones * 0.1);
            }
        }
    }
}
