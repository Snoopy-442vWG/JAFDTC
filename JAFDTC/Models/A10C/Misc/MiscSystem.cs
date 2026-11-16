// ********************************************************************************************************************
//
// MiscSystem.cs -- a-10c miscellaneous system
//
// Copyright(C) 2021-2023 the-paid-actor & others
// Copyright(C) 2023 ilominar/raven
// Copyright(C) 2024 fizzle
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

using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace JAFDTC.Models.A10C.Misc
{
    // defines the coordinate system options
    //
    public enum CoordSystems
    {
        LL = 0,
        MGRS = 1
    }

    // defines the flight plan auto options
    //
    public enum FlightPlanManualOptions
    {
        Auto = 0,
        Manual = 1
    }

    // defines the flight plan auto options
    //
    public enum SpeedDisplayOptions
    {
        IAS = 0,
        TAS = 1,
        GS = 2
    }

    // defines the aap steer pt knob options
    //
    public enum AapSteerPtOptions
    {
        FltPlan = 0,
        Mark = 1,
        Mission = 2
    }

    // defines the aap page options
    //
    public enum AapPageOptions
    {
        Other = 0,
        Position = 1,
        Steer = 2,
        Waypt = 3
    }

    // defines the MFCD page options for the OSB 12-15 page-assignment slots (0 = leave the slot as is)
    //
    public enum MFCDPageOptions
    {
        None = 0,
        TAD = 1,
        TGP = 2,
        DSMS = 3,
        MSG = 4,
        CDU = 5,
        COMM = 6,
        STAT = 7,
        MAV = 8
    }

    // defines the autopilot mode options
    //
    public enum AutopilotModeOptions
    {
        Path = 0,
        AltHdg,
        Alt
    }

    // defines the TACAN mode options
    //
    public enum TACANModeOptions
    {
        Off = 0,
        Rec = 1,
        Tr = 2,
        AaRec = 3,
        AaTr = 4
    }

    // defines the TACAN band options
    //
    public enum TACANBandOptions
    {
        X = 0,
        Y = 1
    }

    // defines the IFF MASTER options
    //
    public enum IFFMasterOptions
    {
        OFF = 0,
        STBY = 1,
        NORM = 2
    }

    /// <summary>
    /// TODO: document
    /// </summary>
    public class MiscSystem : SystemBase
    {
        public const string SystemTag = "JAFDTC:A10C:MISC";

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- following properties post change and validation events

        private string _coordSystem;                              // integer [0, 1]
        public string CoordSystem
        {
            get => _coordSystem;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 0, 1)) ? null : "Invalid format";
                SetProperty(ref _coordSystem, value, error);
            }
        }

        private string _bullseyeOnHUD;                           // string (boolean)
        public string BullseyeOnHUD
        {
            get => _bullseyeOnHUD;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsBooleanFieldValid(value)) ? null : "Invalid format";
                SetProperty(ref _bullseyeOnHUD, value, error);
            }
        }

        private string _flightPlan1Manual;                              // integer [0, 1]
        public string FlightPlan1Manual
        {
            get => _flightPlan1Manual;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 0, 1)) ? null : "Invalid format";
                SetProperty(ref _flightPlan1Manual, value, error);
            }
        }

        private string _buildFlightPlan;                           // string (boolean)
        public string BuildFlightPlan
        {
            get => _buildFlightPlan;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsBooleanFieldValid(value)) ? null : "Invalid format";
                SetProperty(ref _buildFlightPlan, value, error);
            }
        }

        private string _speedDisplay;                              // integer [0, 2]
        public string SpeedDisplay
        {
            get => _speedDisplay;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 0, 2)) ? null : "Invalid format";
                SetProperty(ref _speedDisplay, value, error);
            }
        }

        private string _aapSteerPt;                              // integer [0, 2]
        public string AapSteerPt
        {
            get => _aapSteerPt;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 0, 2)) ? null : "Invalid format";
                SetProperty(ref _aapSteerPt, value, error);
            }
        }

        private string _aapPage;                              // integer [0, 3]
        public string AapPage
        {
            get => _aapPage;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 0, 3)) ? null : "Invalid format";
                SetProperty(ref _aapPage, value, error);
            }
        }

        private string _autopilotMode;                              // integer [0, 2]
        public string AutopilotMode
        {
            get => _autopilotMode;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 0, 2)) ? null : "Invalid format";
                SetProperty(ref _autopilotMode, value, error);
            }
        }

        private string _tacanMode;                              // integer [0, 4]
        public string TACANMode
        {
            get => _tacanMode;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 0, 4)) ? null : "Invalid format";
                SetProperty(ref _tacanMode, value, error);
            }
        }

        private string _tacanBand;                              // integer [0, 1]
        public string TACANBand
        {
            get => _tacanBand;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 0, 1)) ? null : "Invalid format";
                SetProperty(ref _tacanBand, value, error);
            }
        }

        private string _tacanChannel;                           // integer [0, 129]
        public string TACANChannel
        {
            get => _tacanChannel;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 0, 129)) ? null : "Invalid format";
                SetProperty(ref _tacanChannel, value, error);
            }
        }

        private string _leftMFCDOSB12;                           // integer [0, 8] (MFCDPageOptions), 0 = no change
        public string LeftMFCDOSB12
        {
            get => _leftMFCDOSB12;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 0, 8)) ? null : "Invalid format";
                SetProperty(ref _leftMFCDOSB12, value, error);
            }
        }

        private string _leftMFCDOSB13;                           // integer [0, 8] (MFCDPageOptions), 0 = no change
        public string LeftMFCDOSB13
        {
            get => _leftMFCDOSB13;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 0, 8)) ? null : "Invalid format";
                SetProperty(ref _leftMFCDOSB13, value, error);
            }
        }

        private string _leftMFCDOSB14;                           // integer [0, 8] (MFCDPageOptions), 0 = no change
        public string LeftMFCDOSB14
        {
            get => _leftMFCDOSB14;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 0, 8)) ? null : "Invalid format";
                SetProperty(ref _leftMFCDOSB14, value, error);
            }
        }

        private string _leftMFCDOSB15;                           // integer [0, 8] (MFCDPageOptions), 0 = no change
        public string LeftMFCDOSB15
        {
            get => _leftMFCDOSB15;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 0, 8)) ? null : "Invalid format";
                SetProperty(ref _leftMFCDOSB15, value, error);
            }
        }

        private string _rightMFCDOSB12;                           // integer [0, 8] (MFCDPageOptions), 0 = no change
        public string RightMFCDOSB12
        {
            get => _rightMFCDOSB12;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 0, 8)) ? null : "Invalid format";
                SetProperty(ref _rightMFCDOSB12, value, error);
            }
        }

        private string _rightMFCDOSB13;                           // integer [0, 8] (MFCDPageOptions), 0 = no change
        public string RightMFCDOSB13
        {
            get => _rightMFCDOSB13;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 0, 8)) ? null : "Invalid format";
                SetProperty(ref _rightMFCDOSB13, value, error);
            }
        }

        private string _rightMFCDOSB14;                           // integer [0, 8] (MFCDPageOptions), 0 = no change
        public string RightMFCDOSB14
        {
            get => _rightMFCDOSB14;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 0, 8)) ? null : "Invalid format";
                SetProperty(ref _rightMFCDOSB14, value, error);
            }
        }

        private string _rightMFCDOSB15;                           // integer [0, 8] (MFCDPageOptions), 0 = no change
        public string RightMFCDOSB15
        {
            get => _rightMFCDOSB15;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 0, 8)) ? null : "Invalid format";
                SetProperty(ref _rightMFCDOSB15, value, error);
            }
        }

        private string _iffMasterMode;                           // integer [0, 2]
        public string IFFMasterMode
        {
            get => _iffMasterMode;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 0, 2)) ? null : "Invalid format";
                SetProperty(ref _iffMasterMode, value, error);
            }
        }

        private static readonly Regex _iffMode3CodeRegex = new(@"^[0-7][0-7][0-7][0-7]$");

        private string _iffMode3Code;                           // IFF squawk, [0-7][0-7][0-7][0-7]
        public string IFFMode3Code
        {
            get => _iffMode3Code;
            set
            {
                string error = (string.IsNullOrEmpty(value)) ? null : "Invalid format";
                if (IsRegexFieldValid(value, _iffMode3CodeRegex))
                {
                    // note: do not run through FixupIntegerField, it would strip leading zeros (valid octal digits)
                    error = null;
                }
                SetProperty(ref _iffMode3Code, value, error);
            }
        }

        private string _iffMode4On;                           // string (boolean)
        public string IFFMode4On
        {
            get => _iffMode4On;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsBooleanFieldValid(value)) ? null : "Invalid format";
                SetProperty(ref _iffMode4On, value, error);
            }
        }

        private string _aglFloor;                           // integer [0, 5000]
        public string AGLFloor
        {
            get => _aglFloor;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 0, 5000)) ? null : "Invalid format";
                SetProperty(ref _aglFloor, value, error);
            }
        }

        private string _mslFloor;                           // integer [0, 45000]
        public string MSLFloor
        {
            get => _mslFloor;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 0, 45000)) ? null : "Invalid format";
                SetProperty(ref _mslFloor, value, error);
            }
        }

        private string _mslCeiling;                           // integer [0, 45000]
        public string MSLCeiling
        {
            get => _mslCeiling;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 0, 45000)) ? null : "Invalid format";
                SetProperty(ref _mslCeiling, value, error);
            }
        }


        // ---- following properties are synthesized

        // returns a MiscSystem with the fields populated with the actual default values (note that usually the value
        // "" implies default).
        //
        // defaults are as of DCS v2.9.0.47168.
        //
        private static MiscSystem _default;
        public static MiscSystem ExplicitDefaults
        {
            get
            {
                if (_default == null)
                {
                    _default = new MiscSystem();
                    Reset(_default);
                }
                return _default;
            }
        }

        // returns true if the instance indicates a default setup (all fields are "") or the object is in explicit
        // form, false otherwise.
        //
        [JsonIgnore]
        public override bool IsDefault
        {
            get => IsCoordSystemDefault && IsBullseyeOnHUDDefault && IsFlightPlan1ManualDefault
                && IsBuildFlightPlanDefault
                && IsSpeedDisplayDefault && IsAapSteerPtDefault && IsAapPageDefault 
                && IsAutopilotModeDefault && IsTACANModeDefault && IsTACANBandDefault && IsTACANChannelDefault
                && IsIFFMasterModeDefault && IsIFFMode3CodeDefault && IsIFFMode4OnDefault
                && IsAGLFloorDefault && IsMSLFloorDefault && IsMSLCeilingDefault
                && IsLeftMFCDOSB12Default && IsLeftMFCDOSB13Default && IsLeftMFCDOSB14Default && IsLeftMFCDOSB15Default && IsRightMFCDOSB12Default && IsRightMFCDOSB13Default && IsRightMFCDOSB14Default && IsRightMFCDOSB15Default;
        }

        [JsonIgnore]
        public bool IsCoordSystemDefault
        {
            get => string.IsNullOrEmpty(CoordSystem) || CoordSystem == ExplicitDefaults.CoordSystem;
        }

        [JsonIgnore]
        public bool IsBullseyeOnHUDDefault
        {
            get => string.IsNullOrEmpty(BullseyeOnHUD) || BullseyeOnHUD == ExplicitDefaults.BullseyeOnHUD;
        }

        [JsonIgnore]
        public bool IsBuildFlightPlanDefault
        {
            get => string.IsNullOrEmpty(BuildFlightPlan) || BuildFlightPlan == ExplicitDefaults.BuildFlightPlan;
        }

        [JsonIgnore]
        public bool IsBuildFlightPlanValue
        {
            get => bool.Parse((string.IsNullOrEmpty(BuildFlightPlan)) ? ExplicitDefaults.BuildFlightPlan : BuildFlightPlan);
        }

        [JsonIgnore]
        public bool IsFlightPlan1ManualDefault
        {
            get => string.IsNullOrEmpty(FlightPlan1Manual) || FlightPlan1Manual == ExplicitDefaults.FlightPlan1Manual;
        }

        [JsonIgnore]
        public bool IsSpeedDisplayDefault
        {
            get => string.IsNullOrEmpty(SpeedDisplay) || SpeedDisplay == ExplicitDefaults.SpeedDisplay;
        }

        [JsonIgnore]
        public bool IsAapSteerPtDefault
        {
            get => string.IsNullOrEmpty(AapSteerPt) || AapSteerPt == ExplicitDefaults.AapSteerPt;
        }

        [JsonIgnore]
        public bool IsAapPageDefault
        {
            get => string.IsNullOrEmpty(AapPage) || AapPage == ExplicitDefaults.AapPage;
        }

        [JsonIgnore]
        public bool IsAutopilotModeDefault
        {
            get => string.IsNullOrEmpty(AutopilotMode) || AutopilotMode == ExplicitDefaults.AutopilotMode;
        }

        [JsonIgnore]
        public bool IsTACANModeDefault
        {
            get => string.IsNullOrEmpty(TACANMode) || TACANMode == ExplicitDefaults.TACANMode;
        }

        [JsonIgnore]
        public bool IsTACANBandDefault
        {
            get => string.IsNullOrEmpty(TACANBand) || TACANBand == ExplicitDefaults.TACANBand;
        }

        [JsonIgnore]
        public bool IsTACANChannelDefault
        {
            get => string.IsNullOrEmpty(TACANChannel) || TACANChannel == ExplicitDefaults.TACANChannel;
        }

        [JsonIgnore]
        public bool IsIFFMasterModeDefault
        {
            get => string.IsNullOrEmpty(IFFMasterMode) || IFFMasterMode == ExplicitDefaults.IFFMasterMode;
        }

        [JsonIgnore]
        public bool IsIFFMode3CodeDefault
        {
            get => string.IsNullOrEmpty(IFFMode3Code) || IFFMode3Code == ExplicitDefaults.IFFMode3Code;
        }

        [JsonIgnore]
        public bool IsIFFMode4OnDefault
        {
            get => string.IsNullOrEmpty(IFFMode4On) || IFFMode4On == ExplicitDefaults.IFFMode4On;
        }

        [JsonIgnore]
        public bool IsAGLFloorDefault
        {
            get => string.IsNullOrEmpty(AGLFloor) || AGLFloor == ExplicitDefaults.AGLFloor;
        }

        [JsonIgnore]
        public bool IsMSLFloorDefault
        {
            get => string.IsNullOrEmpty(MSLFloor) || MSLFloor == ExplicitDefaults.MSLFloor;
        }

        [JsonIgnore]
        public bool IsMSLCeilingDefault
        {
            get => string.IsNullOrEmpty(MSLCeiling) || MSLCeiling == ExplicitDefaults.MSLCeiling;
        }

        [JsonIgnore]
        public bool IsLeftMFCDOSB12Default
        {
            get => string.IsNullOrEmpty(LeftMFCDOSB12) || LeftMFCDOSB12 == ExplicitDefaults.LeftMFCDOSB12;
        }

        [JsonIgnore]
        public bool IsLeftMFCDOSB13Default
        {
            get => string.IsNullOrEmpty(LeftMFCDOSB13) || LeftMFCDOSB13 == ExplicitDefaults.LeftMFCDOSB13;
        }

        [JsonIgnore]
        public bool IsLeftMFCDOSB14Default
        {
            get => string.IsNullOrEmpty(LeftMFCDOSB14) || LeftMFCDOSB14 == ExplicitDefaults.LeftMFCDOSB14;
        }

        [JsonIgnore]
        public bool IsLeftMFCDOSB15Default
        {
            get => string.IsNullOrEmpty(LeftMFCDOSB15) || LeftMFCDOSB15 == ExplicitDefaults.LeftMFCDOSB15;
        }

        [JsonIgnore]
        public bool IsRightMFCDOSB12Default
        {
            get => string.IsNullOrEmpty(RightMFCDOSB12) || RightMFCDOSB12 == ExplicitDefaults.RightMFCDOSB12;
        }

        [JsonIgnore]
        public bool IsRightMFCDOSB13Default
        {
            get => string.IsNullOrEmpty(RightMFCDOSB13) || RightMFCDOSB13 == ExplicitDefaults.RightMFCDOSB13;
        }

        [JsonIgnore]
        public bool IsRightMFCDOSB14Default
        {
            get => string.IsNullOrEmpty(RightMFCDOSB14) || RightMFCDOSB14 == ExplicitDefaults.RightMFCDOSB14;
        }

        [JsonIgnore]
        public bool IsRightMFCDOSB15Default
        {
            get => string.IsNullOrEmpty(RightMFCDOSB15) || RightMFCDOSB15 == ExplicitDefaults.RightMFCDOSB15;
        }

        // ---- following accessors get the current value (default or non-default) for various properties

        [JsonIgnore]
        public CoordSystems CoordSystemValue
        {
            get => (CoordSystems)int.Parse((string.IsNullOrEmpty(CoordSystem)) ? ExplicitDefaults.CoordSystem : CoordSystem);
        }

        [JsonIgnore]
        public bool IsBullseyeOnHUDValue
        {
            get => bool.Parse((string.IsNullOrEmpty(BullseyeOnHUD)) ? ExplicitDefaults.BullseyeOnHUD : BullseyeOnHUD);
        }

        [JsonIgnore]
        public FlightPlanManualOptions FlightPlan1ManualValue
        {
            get => (FlightPlanManualOptions)int.Parse((string.IsNullOrEmpty(FlightPlan1Manual)) ? ExplicitDefaults.FlightPlan1Manual : FlightPlan1Manual);
        }

        [JsonIgnore]
        public SpeedDisplayOptions SpeedDisplayValue
        {
            get => (SpeedDisplayOptions)int.Parse((string.IsNullOrEmpty(SpeedDisplay)) ? ExplicitDefaults.SpeedDisplay : SpeedDisplay);
        }

        [JsonIgnore]
        public AapSteerPtOptions AapSteerPtValue
        {
            get => (AapSteerPtOptions)int.Parse((string.IsNullOrEmpty(AapSteerPt)) ? ExplicitDefaults.AapSteerPt : AapSteerPt);
        }

        [JsonIgnore]
        public AapPageOptions AapPageValue
        {
            get => (AapPageOptions)int.Parse((string.IsNullOrEmpty(AapPage)) ? ExplicitDefaults.AapPage : AapPage);
        }

        [JsonIgnore]
        public AutopilotModeOptions AutopilotModeValue
        {
            get => (AutopilotModeOptions)int.Parse((string.IsNullOrEmpty(AutopilotMode)) ? ExplicitDefaults.AutopilotMode : AutopilotMode);
        }

        [JsonIgnore]
        public TACANModeOptions TACANModeValue
        {
            get => (TACANModeOptions)int.Parse((string.IsNullOrEmpty(TACANMode)) ? ExplicitDefaults.TACANMode : TACANMode);
        }

        [JsonIgnore]
        public TACANBandOptions TACANBandValue
        {
            get => (TACANBandOptions)int.Parse((string.IsNullOrEmpty(TACANBand)) ? ExplicitDefaults.TACANBand : TACANBand);
        }

        [JsonIgnore]
        public int TACANChannelValue
        {
            get => int.Parse((string.IsNullOrEmpty(TACANChannel)) ? ExplicitDefaults.TACANChannel : TACANChannel);
        }

        [JsonIgnore]
        public IFFMasterOptions IFFMasterModeValue
        {
            get => (IFFMasterOptions)int.Parse((string.IsNullOrEmpty(IFFMasterMode)) ? ExplicitDefaults.IFFMasterMode : IFFMasterMode);
        }

        [JsonIgnore]
        public string IFFMode3CodeValue
        {
            get => string.IsNullOrEmpty(IFFMode3Code) ? ExplicitDefaults.IFFMode3Code : IFFMode3Code;
        }

        [JsonIgnore]
        public bool IffMode4OnValue
        {
            get => bool.Parse((string.IsNullOrEmpty(IFFMode4On)) ? ExplicitDefaults.IFFMode4On : IFFMode4On);
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public MiscSystem()
        {
            Reset();
        }

        public MiscSystem(MiscSystem other)
        {
            CoordSystem = new(other.CoordSystem);
            BullseyeOnHUD = new(other.BullseyeOnHUD);
            FlightPlan1Manual = new(other.FlightPlan1Manual);
            BuildFlightPlan = new(other.BuildFlightPlan ?? "");
            SpeedDisplay = new(other.SpeedDisplay);
            AapSteerPt = new(other.AapSteerPt);
            AapPage = new(other.AapPage);
            AutopilotMode = new(other.AutopilotMode);
            TACANMode = new(other.TACANMode);
            TACANBand = new(other.TACANBand);
            TACANChannel = new(other.TACANChannel);
            IFFMasterMode = new(other.IFFMasterMode);
            IFFMode3Code = new(other.IFFMode3Code);
            IFFMode4On = new(other.IFFMode4On);
            AGLFloor = new(other.AGLFloor);
            MSLFloor = new(other.MSLFloor);
            MSLCeiling = new(other.MSLCeiling);
            LeftMFCDOSB12 = new(other.LeftMFCDOSB12);
            LeftMFCDOSB13 = new(other.LeftMFCDOSB13);
            LeftMFCDOSB14 = new(other.LeftMFCDOSB14);
            LeftMFCDOSB15 = new(other.LeftMFCDOSB15);
            RightMFCDOSB12 = new(other.RightMFCDOSB12);
            RightMFCDOSB13 = new(other.RightMFCDOSB13);
            RightMFCDOSB14 = new(other.RightMFCDOSB14);
            RightMFCDOSB15 = new(other.RightMFCDOSB15);
         }

        public virtual object Clone() => new MiscSystem(this);

        // ------------------------------------------------------------------------------------------------------------
        //
        // Methods
        //
        // ------------------------------------------------------------------------------------------------------------

        // reset the instance to defaults (by definition, field value of "" implies default).
        //
        public override void Reset()
        {
            Reset(this);
        }

        private static void Reset(MiscSystem misc)
        {
            misc.CoordSystem = "0";                  // LL
            misc.BullseyeOnHUD = false.ToString();
            misc.FlightPlan1Manual = "0";            // Auto
            misc.BuildFlightPlan = false.ToString();
            misc.SpeedDisplay = "0";                 // IAS
            misc.AapSteerPt = "0";                   // FltPlan
            misc.AapPage = "0";                      // Other
            misc.AutopilotMode = "1";                // Alt/Hdg
            misc.TACANMode = "0";                    // Off
            misc.TACANBand = "0";                    // X
            misc.TACANChannel = "0";
            misc.IFFMasterMode = "0";                // Off
            misc.IFFMode3Code = "";
            misc.IFFMode4On = false.ToString();
            misc.AGLFloor = "500";
            misc.MSLFloor = "0";
            misc.MSLCeiling = "0";
            misc.LeftMFCDOSB12 = "0";
            misc.LeftMFCDOSB13 = "0";
            misc.LeftMFCDOSB14 = "0";
            misc.LeftMFCDOSB15 = "0";
            misc.RightMFCDOSB12 = "0";
            misc.RightMFCDOSB13 = "0";
            misc.RightMFCDOSB14 = "0";
            misc.RightMFCDOSB15 = "0";
        }
    }
}
