// ********************************************************************************************************************
//
// DSMSSystem.cs -- a-10c dsms system
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace JAFDTC.Models.A10C.DSMS
{
    public class DSMSSystem : SystemBase
    {
        public const string SystemTag = "JAFDTC:A10C:DSMS";


        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        private static readonly Regex _laserCodeRegex = new(@"^1[1-7][1-8][1-8]$");

        // ---- properties that post change and validation events

        private string _laserCode;                           // laser code, [1][1-7][1-8][1-8]
        public string LaserCode
        {
            get => _laserCode;
            set
            {
                string error = (string.IsNullOrEmpty(value)) ? null : "Invalid format";
                if (IsRegexFieldValid(value, _laserCodeRegex))
                {
                    value = FixupIntegerField(value);
                    error = null;
                }
                SetProperty(ref _laserCode, value, error);
            }
        }

        [JsonPropertyName("MunitionSettings")]
        public Dictionary<int, MunitionSettings> MunitionSettingMap
        {
            get => _munitionSettingMap;
            set => _munitionSettingMap = value;
        }
        private Dictionary<int, MunitionSettings> _munitionSettingMap;

        public List<int> ProfileOrder { get; set; }

        // custom (named) profiles the pilot wants created on the jet in addition to the jet's default profile for a
        // munition. key is "<munition ID>|<profile name>"; the profile's settings are held in a MunitionSettings.
        [JsonPropertyName("CustomProfiles")]
        public Dictionary<string, MunitionSettings> CustomProfileMap
        {
            get => _customProfileMap;
            set => _customProfileMap = value ?? new Dictionary<string, MunitionSettings>();
        }
        private Dictionary<string, MunitionSettings> _customProfileMap = new();

        // ---- synthesized properties

        public readonly static DSMSSystem ExplicitDefaults = new()
        {
            LaserCode = "1688"
        };

        [JsonIgnore]
        public override bool IsDefault
        {
            get
            {
                if (!IsLaserCodeDefault || !IsProfileOrderDefault || HasCustomProfiles)
                    return false;
                return AreAllMunitionSettingsDefault;
            }
        }

        [JsonIgnore]
        public bool IsLaserCodeDefault => string.IsNullOrEmpty(LaserCode) || LaserCode == ExplicitDefaults.LaserCode;

        [JsonIgnore]
        public bool IsProfileOrderDefault => ProfileOrder == null || ProfileOrder.Count == 0;

        [JsonIgnore]
        public bool AreAllMunitionSettingsDefault
        {
            get
            {
                if (HasCustomProfiles)
                    return false;
                foreach (MunitionSettings setting in _munitionSettingMap.Values)
                {
                    if (!setting.IsDefault)
                        return false;
                }
                return true;
            }
        }
           

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public DSMSSystem()
        {
            Reset();
        }

        public DSMSSystem(DSMSSystem other)
        {
            LaserCode = other.LaserCode;
            _munitionSettingMap = other._munitionSettingMap;
            ProfileOrder = other.ProfileOrder;
            _customProfileMap = other._customProfileMap;
        }

        public virtual object Clone() => new DSMSSystem(this);

        // ------------------------------------------------------------------------------------------------------------
        //
        // Methods
        //
        // ------------------------------------------------------------------------------------------------------------

        private Dictionary<string, int> _orderedProfilePositions;
        /// <summary>
        /// Return the configured position, according to the "profile order" screen, for the provided profile name.
        /// </summary>
        /// <param name="profileName"></param>
        /// <returns></returns>
        public int GetOrderedProfilePosition(string profileName)
        {
            if (_orderedProfilePositions == null)
            {
                if (ProfileOrder != null || ProfileOrder.Count > 0)
                {
                    _orderedProfilePositions = new Dictionary<string, int>(ProfileOrder.Count);
                    for (int i = 0; i < ProfileOrder.Count; i++)
                    {
                        A10CMunition m = A10CMunition.GetMunitionFromID(ProfileOrder[i]);
                        _orderedProfilePositions.Add(m.Profile, i);
                    }
                }
            }

            if (_orderedProfilePositions == null)
                return 0;
            else
            {
                if (_orderedProfilePositions.TryGetValue(profileName, out int position))
                    return position;
                else return int.MaxValue; // send unknown profiles to the end of the list
            }
        }

        public MunitionSettings GetMunitionSettings(A10CMunition munition)
        {
            MunitionSettings settings;
            if (!_munitionSettingMap.TryGetValue(munition.ID, out settings))
            {
                settings = new MunitionSettings(munition);
                _munitionSettingMap.Add(munition.ID, settings);
            }
            return settings;
        }

        //
        // custom profile accessors
        //

        /// <summary>
        /// describes a custom profile: the munition it is built from, its name, and its settings.
        /// </summary>
        public class CustomProfileInfo
        {
            public A10CMunition Munition { get; set; }
            public string Name { get; set; }
            public MunitionSettings Settings { get; set; }
        }

        [JsonIgnore]
        public bool HasCustomProfiles => (_customProfileMap != null) && (_customProfileMap.Count > 0);

        private static string CustomProfileKey(A10CMunition munition, string name) => $"{munition.ID}|{name}";

        /// <summary>
        /// return the names of the custom profiles for a munition, sorted by name.
        /// </summary>
        public List<string> GetCustomProfileNames(A10CMunition munition)
        {
            string prefix = $"{munition.ID}|";
            return _customProfileMap.Keys.Where(k => k.StartsWith(prefix, StringComparison.Ordinal))
                                         .Select(k => k.Substring(prefix.Length))
                                         .OrderBy(n => n, StringComparer.Ordinal).ToList();
        }

        public bool HasCustomProfile(A10CMunition munition, string name)
            => _customProfileMap.ContainsKey(CustomProfileKey(munition, name));

        /// <summary>
        /// true if name is already used by any custom profile (for any munition).
        /// </summary>
        public bool IsCustomProfileNameInUse(string name)
            => _customProfileMap.Keys.Any(k => k.Substring(k.IndexOf('|') + 1) == name);

        /// <summary>
        /// return the settings for a custom profile, or null if there is no such profile.
        /// </summary>
        public MunitionSettings GetCustomProfileSettings(A10CMunition munition, string name)
        {
            if (_customProfileMap.TryGetValue(CustomProfileKey(munition, name), out MunitionSettings settings))
            {
                settings.Munition = munition;
                return settings;
            }
            return null;
        }

        /// <summary>
        /// add a custom profile (or replace the settings of an existing one), copying initial settings from seed
        /// if provided.
        /// </summary>
        public MunitionSettings AddCustomProfile(A10CMunition munition, string name, MunitionSettings seed = null)
        {
            MunitionSettings settings = new MunitionSettings(munition);
            if (seed != null)
                settings.CopyFrom(seed);
            _customProfileMap[CustomProfileKey(munition, name)] = settings;
            return settings;
        }

        public void RemoveCustomProfile(A10CMunition munition, string name)
            => _customProfileMap.Remove(CustomProfileKey(munition, name));

        /// <summary>
        /// return all the custom profiles in the configuration.
        /// </summary>
        public List<CustomProfileInfo> GetAllCustomProfiles()
        {
            List<CustomProfileInfo> list = new();
            foreach (KeyValuePair<string, MunitionSettings> kv in _customProfileMap)
            {
                int sep = kv.Key.IndexOf('|');
                if ((sep > 0) && int.TryParse(kv.Key.Substring(0, sep), out int munitionID))
                {
                    A10CMunition munition = A10CMunition.GetMunitionFromID(munitionID);
                    kv.Value.Munition = munition;
                    list.Add(new CustomProfileInfo()
                    {
                        Munition = munition,
                        Name = kv.Key.Substring(sep + 1),
                        Settings = kv.Value
                    });
                }
            }
            return list;
        }

        /// <summary>
        /// make this instance's custom profiles match those of other, keeping existing settings objects where
        /// possible so any change handlers hooked to them stay valid.
        /// </summary>
        public void SyncCustomProfilesFrom(DSMSSystem other)
        {
            foreach (string key in _customProfileMap.Keys.Where(k => !other._customProfileMap.ContainsKey(k)).ToList())
                _customProfileMap.Remove(key);
            foreach (KeyValuePair<string, MunitionSettings> kv in other._customProfileMap)
            {
                if (_customProfileMap.TryGetValue(kv.Key, out MunitionSettings mine))
                    mine.CopyFrom(kv.Value);
                else
                {
                    int sep = kv.Key.IndexOf('|');
                    A10CMunition munition = A10CMunition.GetMunitionFromID(int.Parse(kv.Key.Substring(0, sep)));
                    MunitionSettings copy = new MunitionSettings(munition);
                    copy.CopyFrom(kv.Value);
                    _customProfileMap[kv.Key] = copy;
                }
            }
        }

        // reset the instance to defaults
        public override void Reset()
        {
            LaserCode = "";
            _munitionSettingMap = new Dictionary<int, MunitionSettings>();
            ProfileOrder = null;
            _customProfileMap = new Dictionary<string, MunitionSettings>();
        }

        internal void FixupMunitionReferences()
        {
            List<A10CMunition> munitions = A10CMunition.GetMunitions();
            foreach (var munition in munitions)
            {
                if (_munitionSettingMap.TryGetValue(munition.ID, out MunitionSettings setting))
                    setting.Munition = munition;
            }
            foreach (KeyValuePair<string, MunitionSettings> kv in _customProfileMap)
            {
                int sep = kv.Key.IndexOf('|');
                if ((sep > 0) && int.TryParse(kv.Key.Substring(0, sep), out int munitionID))
                    kv.Value.Munition = A10CMunition.GetMunitionFromID(munitionID);
            }
        }

        //
        // MunitionSettings accessors
        //

        // Get munition settings that have non-default settings on the jet's INV page.
        // The returned Dictionary key is the munition INV_Key.
        public Dictionary<string, MunitionSettings> GetNonDefaultInvSettings()
        {
            Dictionary<string, MunitionSettings> settings = new Dictionary<string, MunitionSettings>();
            foreach (MunitionSettings munSettings in _munitionSettingMap.Values)
            {
                if (!munSettings.IsInvDefault)
                    AddMunitionSettingsWithAllInvKeysToDictionary(settings, munSettings);
                else if (munSettings != null && munSettings.Munition.Laser && !IsLaserCodeDefault)
                    AddMunitionSettingsWithAllInvKeysToDictionary(settings, munSettings);
            }

            // If laser code is non-default, ensure all laser weapons are added to list
            // even if they have no other non-default settings.
            List<A10CMunition> munitions = A10CMunition.GetMunitions();
            if (!IsLaserCodeDefault)
            {
                foreach (A10CMunition munition in munitions)
                    if (munition.Laser && !settings.ContainsKey(munition.INV_Keys[0]))
                        AddMunitionSettingsWithAllInvKeysToDictionary(settings, GetMunitionSettings(munition));
            }
            return settings;
        }

        private void AddMunitionSettingsWithAllInvKeysToDictionary(
            Dictionary<string, MunitionSettings> dict, 
            MunitionSettings settings)
        {
            foreach (string invKey in settings.Munition.INV_Keys)
                dict.Add(invKey, settings);
        }

        // Get munition settings that have non-default settings on the profiles page.
        public Dictionary<int, MunitionSettings> GetNonDefaultProfileSettings()
        {
            Dictionary<int, MunitionSettings> settings = new Dictionary<int, MunitionSettings>();
            foreach (KeyValuePair<int, MunitionSettings> kv in _munitionSettingMap)
            {
                if (!kv.Value.IsProfileDefault)
                    settings.Add(kv.Key, kv.Value);
            }
            return settings;
        }

        public void SetAutoLase(A10CMunition munition, string value) => GetMunitionSettings(munition).AutoLase = value;
        public string GetAutoLase(A10CMunition munition) => GetMunitionSettings(munition).AutoLase;
        public bool GetAutoLaseValue(A10CMunition munition)
        {
            string s = GetAutoLase(munition);
            return bool.Parse(string.IsNullOrEmpty(s) ? MunitionSettings.ExplicitDefaults.AutoLase : s);
        }

        public void SetLaseSeconds(A10CMunition munition, string value) => GetMunitionSettings(munition).LaseSeconds = value;
        public string GetLaseSeconds(A10CMunition munition) => GetMunitionSettings(munition).LaseSeconds;

        public void SetDeliveryMode(A10CMunition munition, string value) => GetMunitionSettings(munition).DeliveryMode = value;
        public string GetDeliveryMode(A10CMunition munition) => GetMunitionSettings(munition).DeliveryMode;
        public DeliveryModes GetDeliveryModeValue(A10CMunition munition)
        {
            string s = GetDeliveryMode(munition);
            return (DeliveryModes)int.Parse(string.IsNullOrEmpty(s) ? MunitionSettings.ExplicitDefaults.DeliveryMode : s);
        }

        public void SetEscapeManeuver(A10CMunition munition, string value) => GetMunitionSettings(munition).EscapeManeuver = value;
        public string GetEscapeManeuver(A10CMunition munition) => GetMunitionSettings(munition).EscapeManeuver;
        public EscapeManeuvers GetEscapeManeuverValue(A10CMunition munition)
        {
            string s = GetEscapeManeuver(munition);
            return (EscapeManeuvers)int.Parse(string.IsNullOrEmpty(s) ? MunitionSettings.ExplicitDefaults.EscapeManeuver : s);
        }

        public void SetReleaseMode(A10CMunition munition, string value) => GetMunitionSettings(munition).ReleaseMode = value;
        public string GetReleaseMode(A10CMunition munition) => GetMunitionSettings(munition).ReleaseMode;
        public ReleaseModes GetReleaseModeValue(A10CMunition munition)
        {
            string s = GetReleaseMode(munition);
            return (ReleaseModes)int.Parse(string.IsNullOrEmpty(s) ? MunitionSettings.ExplicitDefaults.ReleaseMode : s);
        }

        public void SetRippleQty(A10CMunition munition, string value) => GetMunitionSettings(munition).RippleQty = value;
        public string GetRippleQty(A10CMunition munition) => GetMunitionSettings(munition).RippleQty;

        public void SetRippleFt(A10CMunition munition, string value) => GetMunitionSettings(munition).RippleFt = value;
        public string GetRippleFt(A10CMunition munition) => GetMunitionSettings(munition).RippleFt;

        public void SetHOFOption(A10CMunition munition, string value) => GetMunitionSettings(munition).HOFOption = value;
        public string GetHOFOption(A10CMunition munition) => GetMunitionSettings(munition).HOFOption;
        public HOFOptions GetHOFOptionValue(A10CMunition munition)
        {
            string s = GetHOFOption(munition);
            return (HOFOptions)int.Parse(string.IsNullOrEmpty(s) ? MunitionSettings.ExplicitDefaults.HOFOption : s);
        }

        public void SetRPMOption(A10CMunition munition, string value) => GetMunitionSettings(munition).RPMOption = value;
        public string GetRPMOption(A10CMunition munition) => GetMunitionSettings(munition).RPMOption;
        public RPMOptions GetRPMOptionValue(A10CMunition munition)
        {
            string s = GetRPMOption(munition);
            return (RPMOptions)int.Parse(string.IsNullOrEmpty(s) ? MunitionSettings.ExplicitDefaults.RPMOption : s);
        }

        public void SetFuzeOption(A10CMunition munition, string value) => GetMunitionSettings(munition).FuzeOption = value;
        public string GetFuzeOption(A10CMunition munition) => GetMunitionSettings(munition).FuzeOption;
        public FuzeOptions GetFuzeOptionValue(A10CMunition munition)
        {
            string s = GetFuzeOption(munition);
            return (FuzeOptions)int.Parse(string.IsNullOrEmpty(s) ? MunitionSettings.ExplicitDefaults.FuzeOption : s);
        }
    }
}
