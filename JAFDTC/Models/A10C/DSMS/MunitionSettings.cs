// ********************************************************************************************************************
//
// MunitionSettings.cs -- munition settings for a-10c dsms system
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

using System.Reflection;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace JAFDTC.Models.A10C.DSMS
{
    // defines the possible delivery mode settings
    // must match combobox order in ui
    public enum DeliveryModes
    {
        CCIP = 0,
        CCRP = 1
    }

    // defines the possible escape maneuver settings
    // must match combobox order in ui
    public enum EscapeManeuvers
    {
        NONE = 0,
        CLB = 1, // jet default
        TRN = 2,
        TLT = 3
    }

    // defines the possible release mode settings
    // must match combobox order in ui
    public enum ReleaseModes
    {
        SGL = 0, // jet default
        PRS = 1,
        RIP_SGL = 2,
        RIP_PRS = 3
    }

    // defines the possible height of function (HOF) settings
    // must match combobox order in ui
    public enum HOFOptions
    {
        HOF_300 = 0,
        HOF_500 = 1,
        HOF_700 = 2,
        HOF_900 = 3,
        HOF_1200 = 4,
        HOF_1500 = 5,
        HOF_1800 = 6, // jet default
        HOF_2200 = 7,
        HOF_2600 = 8,
        HOF_3000 = 9
    }

    // defines the RPM settings
    // must match combobox order in ui
    public enum RPMOptions
    {
        RPM_0 = 0,
        RPM_500 = 1,
        RPM_1000 = 2,
        RPM_1500 = 3, // jet default
        RPM_2000 = 4,
        RPM_2500 = 5
    }

    // defines the Fuze settings
    // must match combobox order in ui
    public enum FuzeOptions
    {
        NoseTail = 0, // jet default
        Nose = 1,
        Tail = 2
    }

    public class MunitionSettings : SystemBase
    {
        private string _autoLase;                              // string (boolean)
        public string AutoLase
        {
            get => _autoLase;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsBooleanFieldValid(value)) ? null : "Invalid format";
                SetProperty(ref _autoLase, value, error);
            }
        }

        private string _laseSeconds;                          // integer [0..99]
        public string LaseSeconds
        {
            get => _laseSeconds;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 0, 99)) ? null : "Invalid format";
                SetProperty(ref _laseSeconds, value, error);
            }
        }

        private string _deliveryMode;                          // integer [0, 1]
        public string DeliveryMode
        {
            get => _deliveryMode;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 0, 1)) ? null : "Invalid format";
                SetProperty(ref _deliveryMode, value, error);
            }
        }

        private string _escapeManeuver;                        // integer [0..3]
        public string EscapeManeuver
        {
            get => _escapeManeuver;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, -1, 3)) ? null : "Invalid format";
                SetProperty(ref _escapeManeuver, value, error);
            }
        }

        private string _releaseMode;                          // integer [0..3]
        public string ReleaseMode
        {
            get => _releaseMode;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 0, 3)) ? null : "Invalid format";
                SetProperty(ref _releaseMode, value, error);
            }
        }

        private string _rippleQty;                          // integer [1..99]
        public string RippleQty
        {
            get => _rippleQty;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 1, 99)) ? null : "Invalid format";
                SetProperty(ref _rippleQty, value, error);
            }
        }

        private string _rippleFt;                          // integer [10..990]
        public string RippleFt
        {
            get => _rippleFt;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 10, 990)) ? null : "Invalid format";
                SetProperty(ref _rippleFt, value, error);
            }
        }

        private string _HOFOption;                          // integer [-1..9] -1 indicates no selection
        public string HOFOption
        {
            get => _HOFOption;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, -1, 9)) ? null : "Invalid format";
                SetProperty(ref _HOFOption, value, error);
            }
        }

        private string _RPMOption;                          // integer [-1..5] -1 indicates no selection
        public string RPMOption
        {
            get => _RPMOption;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, -1, 5)) ? null : "Invalid format";
                SetProperty(ref _RPMOption, value, error);
            }
        }

        private string _FuzeOption;                          // integer [-1..2] -1 indicates no selection
        public string FuzeOption
        {
            get => _FuzeOption;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, -1, 2)) ? null : "Invalid format";
                SetProperty(ref _FuzeOption, value, error);
            }
        }

        private string _impactAzimuth;                          // integer [0, 359] degrees, "" = unset
        public string ImpactAzimuth
        {
            get => _impactAzimuth;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 0, 359)) ? null : "Invalid format";
                SetProperty(ref _impactAzimuth, value, error);
            }
        }

        private string _impactAngle;                          // integer [30, 90] degrees, "" = unset
        public string ImpactAngle
        {
            get => _impactAngle;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 30, 90)) ? null : "Invalid format";
                SetProperty(ref _impactAngle, value, error);
            }
        }

        private string _desTOF;                          // decimal [0.0, 99.9] seconds (up to two decimal places), "" = unset
        public string DesTOF
        {
            get => _desTOF;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsRegexFieldValid(value, _tofRegex)) ? null : "Invalid format";
                SetProperty(ref _desTOF, value, error);
            }
        }

        private string _minAlt;                          // integer [0, 9999] feet, "" = unset
        public string MinAlt
        {
            get => _minAlt;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 0, 9999)) ? null : "Invalid format";
                SetProperty(ref _minAlt, value, error);
            }
        }

        private string _minTOF;                          // decimal [0.0, 99.9] seconds (up to two decimal places), "" = unset
        public string MinTOF
        {
            get => _minTOF;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsRegexFieldValid(value, _tofRegex)) ? null : "Invalid format";
                SetProperty(ref _minTOF, value, error);
            }
        }

        private string _hDTOF;                          // decimal [0.0, 99.9] seconds (up to two decimal places), "" = unset
        public string HDTOF
        {
            get => _hDTOF;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsRegexFieldValid(value, _tofRegex)) ? null : "Invalid format";
                SetProperty(ref _hDTOF, value, error);
            }
        }

        private string _lDTOF;                          // decimal [0.0, 99.9] seconds (up to two decimal places), "" = unset
        public string LDTOF
        {
            get => _lDTOF;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsRegexFieldValid(value, _tofRegex)) ? null : "Invalid format";
                SetProperty(ref _lDTOF, value, error);
            }
        }

        // time-of-fall fields: up to two digits with an optional single decimal place, e.g., "4", "4.5", "12.0", "0.00"
        private static readonly Regex _tofRegex = new(@"^\d{1,2}(\.\d{1,2})?$");

        [JsonIgnore]
        public override bool IsDefault =>
            IsAutoLaseDefault && IsLaseSecondsDefault && IsDeliveryModeDefault && IsEscapeManeuverDefault &&
            IsReleaseModeDefault && IsHOFOptionDefault && IsRPMOptionDefault && IsFuzeOptionDefault &&
            IsImpactAzimuthDefault && IsImpactAngleDefault && IsDesTOFDefault && IsMinAltDefault && IsMinTOFDefault && IsHDTOFDefault && IsLDTOFDefault;

        /// <summary>
        /// Does this configuration require changes on the DSMS INV page?
        /// </summary>
        [JsonIgnore]
        public bool IsInvDefault =>
            IsHOFOptionDefault && IsRPMOptionDefault;

        /// <summary>
        /// Does this configuration require changes to the DSMS Profile?
        /// </summary>
        [JsonIgnore]
        public bool IsProfileDefault =>
            IsAutoLaseDefault && IsLaseSecondsDefault && IsDeliveryModeDefault && IsEscapeManeuverDefault &&
            IsReleaseModeDefault && IsFuzeOptionDefault &&
            IsImpactAzimuthDefault && IsImpactAngleDefault && IsDesTOFDefault && IsMinAltDefault && IsMinTOFDefault && IsHDTOFDefault && IsLDTOFDefault;

        /// <summary>
        /// Does this configuration require changes to the "change settings" page of the DSMS Profile?
        /// </summary>
        [JsonIgnore]
        public bool IsProfileCHGSETDefault =>
            IsAutoLaseDefault && IsLaseSecondsDefault && IsEscapeManeuverDefault &&
            IsImpactAzimuthDefault && IsImpactAngleDefault && IsDesTOFDefault && IsMinAltDefault && IsMinTOFDefault && IsHDTOFDefault && IsLDTOFDefault;

        [JsonIgnore]
        public A10CMunition Munition
        {
            get => _munition;
            set => _munition = value;
        }
        private A10CMunition _munition;

        [JsonIgnore]
        public bool IsAutoLaseDefault => string.IsNullOrEmpty(AutoLase) || AutoLase == ExplicitDefaults.AutoLase;
        [JsonIgnore]
        public bool IsLaseSecondsDefault => string.IsNullOrEmpty(LaseSeconds) || LaseSeconds == ExplicitDefaults.LaseSeconds;
        [JsonIgnore]
        public bool IsDeliveryModeDefault 
        {
            get
            {
                if (string.IsNullOrEmpty(DeliveryMode))
                    return true;
                if (_munition == null)
                    return true;
                return DeliveryMode == DefaultDeliveryMode;
            }
        }
        [JsonIgnore]
        public bool IsEscapeManeuverDefault => string.IsNullOrEmpty(EscapeManeuver) || EscapeManeuver == ExplicitDefaults.EscapeManeuver;
        [JsonIgnore]
        public bool IsReleaseModeDefault => string.IsNullOrEmpty(ReleaseMode) || ReleaseMode == ExplicitDefaults.ReleaseMode;
        [JsonIgnore]
        public bool IsRippleQtyDefault => string.IsNullOrEmpty(RippleQty) || RippleQty == ExplicitDefaults.RippleQty;
        [JsonIgnore]
        public bool IsRippleFtDefault => string.IsNullOrEmpty(RippleFt) || RippleFt == ExplicitDefaults.RippleFt;
        [JsonIgnore]
        public bool IsHOFOptionDefault => string.IsNullOrEmpty(HOFOption) || HOFOption == ExplicitDefaults.HOFOption;
        [JsonIgnore]
        public bool IsRPMOptionDefault => string.IsNullOrEmpty(RPMOption) || RPMOption == ExplicitDefaults.RPMOption;
        [JsonIgnore]
        public bool IsFuzeOptionDefault => string.IsNullOrEmpty(FuzeOption) || FuzeOption == ExplicitDefaults.FuzeOption;
        [JsonIgnore]
        public bool IsImpactAzimuthDefault => string.IsNullOrEmpty(ImpactAzimuth) || ImpactAzimuth == ExplicitDefaults.ImpactAzimuth;
        [JsonIgnore]
        public bool IsImpactAngleDefault => string.IsNullOrEmpty(ImpactAngle) || ImpactAngle == ExplicitDefaults.ImpactAngle;
        [JsonIgnore]
        public bool IsDesTOFDefault => string.IsNullOrEmpty(DesTOF) || DesTOF == ExplicitDefaults.DesTOF;
        [JsonIgnore]
        public bool IsMinAltDefault => string.IsNullOrEmpty(MinAlt) || MinAlt == ExplicitDefaults.MinAlt;
        [JsonIgnore]
        public bool IsMinTOFDefault => string.IsNullOrEmpty(MinTOF) || MinTOF == ExplicitDefaults.MinTOF;
        [JsonIgnore]
        public bool IsHDTOFDefault => string.IsNullOrEmpty(HDTOF) || HDTOF == ExplicitDefaults.HDTOF;
        [JsonIgnore]
        public bool IsLDTOFDefault => string.IsNullOrEmpty(LDTOF) || LDTOF == ExplicitDefaults.LDTOF;

        [JsonIgnore]
        public string DefaultDeliveryMode
        {
            get
            {
                if (_munition == null || _munition.CCIP)
                    return "0";
                return "1";
            }
        }

        private static MunitionSettings _default;
        public static MunitionSettings ExplicitDefaults
        {
            get
            {
                if (_default == null)
                {
                    _default = new MunitionSettings();
                    Reset(_default);
                }
                return _default;
            }
        }

        private static void Reset(MunitionSettings settings)
        {
            settings.AutoLase = "False";
            settings.LaseSeconds = "0";
            settings.DeliveryMode = "";
            settings.EscapeManeuver = "1";   // CLB
            settings.ReleaseMode = "0";      // SGL
            settings.RippleQty = "1";
            settings.RippleFt = "75";
            settings.HOFOption = "6";        // 1800
            settings.RPMOption = "3";        // 1500
            settings.FuzeOption = "0";       // N/T
            settings.ImpactAzimuth = "";
            settings.ImpactAngle = "";
            settings.DesTOF = "";
            settings.MinAlt = "";
            settings.MinTOF = "";
            settings.HDTOF = "";
            settings.LDTOF = "";
        }


        private MunitionSettings()
        {
            Reset();
        }

        public MunitionSettings(A10CMunition munition) : this()
        {
            _munition = munition;
        }

        public override void Reset()
        {
            Reset(this);
        }

        /// <summary>
        /// copy the settings (string-valued properties) from other on to this instance.
        /// </summary>
        public void CopyFrom(MunitionSettings other)
        {
            foreach (PropertyInfo prop in typeof(MunitionSettings).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                if ((prop.PropertyType == typeof(string)) && prop.CanRead && prop.CanWrite)
                    prop.SetValue(this, prop.GetValue(other));
            }
        }
    }
}
