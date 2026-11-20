// ********************************************************************************************************************
//
// C130JConfiguration.cs -- c-130j airframe configuration
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

using JAFDTC.Models.C130J.Radio;
using JAFDTC.Models.C130J.RTE;
using JAFDTC.Models.C130J.WYPT;
using JAFDTC.Models.Core;
using JAFDTC.UI.C130J;
using JAFDTC.Utilities;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JAFDTC.Models.C130J
{
    /// <summary>
    /// c-130j configuration: waypoints, route selection and radio presets, loaded through the CNI-MU with cockpit
    /// key presses ("Load to Jet").
    /// </summary>
    public partial class C130JConfiguration : ConfigurationBase
    {
        private const string _versionCfg = "C130J-1.0";

        public WYPTSystem WYPT { get; set; }

        public RTESystem RTE { get; set; }

        public RadioSystem Radio { get; set; }

        [JsonIgnore]
        public override IUploadAgent UploadAgent => new C130JUploadAgent(this);

        public C130JConfiguration(string uid, string name, Dictionary<string, string> linkedSysMap)
            : base(_versionCfg, AirframeTypes.C130J, uid, name, linkedSysMap)
        {
            WYPT = new WYPTSystem();
            RTE = new RTESystem();
            Radio = new RadioSystem();
            ConfigurationUpdated();
        }

        public override IConfiguration Clone()
        {
            Dictionary<string, string> linkedSysMap = [ ];
            foreach (KeyValuePair<string, string> kvp in LinkedSysMap)
                linkedSysMap[new(kvp.Key)] = new(kvp.Value);
            C130JConfiguration clone = new(UID, Name, linkedSysMap)
            {
                WYPT = (WYPTSystem)WYPT.Clone(),
                RTE = (RTESystem)RTE.Clone(),
                Radio = (RadioSystem)Radio.Clone(),
            };
            clone.ConfigurationUpdated();
            return clone;
        }

        public override void CloneSystemFrom(string systemTag, IConfiguration other)
        {
            C130JConfiguration otherHerc = (C130JConfiguration)other;
            switch (systemTag)
            {
                case WYPTSystem.SystemTag: WYPT = (WYPTSystem)otherHerc.WYPT.Clone(); break;
                case RTESystem.SystemTag: RTE = (RTESystem)otherHerc.RTE.Clone(); break;
                case RadioSystem.SystemTag: Radio = (RadioSystem)otherHerc.Radio.Clone(); break;
                default: break;
            }
        }

        public override void Sanitize(bool isResetUID = false)
        {
            base.Sanitize(isResetUID);
            WYPT.Sanitize();
            RTE.Sanitize();
            Radio.Sanitize();
        }

        public override ISystem SystemForTag(string tag)
            => tag switch
            {
                WYPTSystem.SystemTag => WYPT,
                RTESystem.SystemTag => RTE,
                RadioSystem.SystemTag => Radio,
                _ => null,
            };

        public override void ConfigurationUpdated(string updateSysTag = null)
        {
            C130JConfigurationEditor editor = new(this);
            Dictionary<string, string> updatesStrings = editor.BuildUpdatesStrings(this);

            string wypts = "";
            if (!WYPT.IsDefault)
                wypts = $" along with {WYPT.Count} waypoint" + ((WYPT.Count > 1) ? "s" : "");
            SystemInfoTextUI = updatesStrings["SystemInfoTextUI"] + wypts;
            SystemInfoIconsUI = updatesStrings["SystemInfoIconsUI"];
            SystemInfoIconBadgesUI = updatesStrings["SystemInfoIconBadgesUI"];
        }

        public override string Serialize(string systemTag = null)
            => systemTag switch
            {
                null => JsonSerializer.Serialize(this, ConfigurationBase.JsonOptions),
                WYPTSystem.SystemTag => JsonSerializer.Serialize(WYPT, ConfigurationBase.JsonOptions),
                RTESystem.SystemTag => JsonSerializer.Serialize(RTE, ConfigurationBase.JsonOptions),
                RadioSystem.SystemTag => JsonSerializer.Serialize(Radio, ConfigurationBase.JsonOptions),
                _ => null
            };

        public override void AfterLoadFromJSON()
        {
            WYPT ??= new WYPTSystem();
            RTE ??= new RTESystem();
            Radio ??= new RadioSystem();
            Radio.Sanitize();
            Version = _versionCfg;
            ConfigurationUpdated();
        }

        public override bool CanAcceptPasteForSystem(string cboardTag, string systemTag = null)
            => !string.IsNullOrEmpty(cboardTag) &&
               (((systemTag != null) && cboardTag.StartsWith(systemTag)) ||
                ((systemTag == null) && (cboardTag == WYPTSystem.SystemTag)) ||
                ((systemTag == null) && (cboardTag == RTESystem.SystemTag)) ||
                ((systemTag == null) && (cboardTag == RadioSystem.SystemTag)));

        public override bool Deserialize(string systemTag, string json)
        {
            bool isSuccess = false;
            bool isHandled = true;
            try
            {
                switch (systemTag)
                {
                    case WYPTSystem.SystemTag: WYPT = JsonSerializer.Deserialize<WYPTSystem>(json); break;
                    case RTESystem.SystemTag: RTE = JsonSerializer.Deserialize<RTESystem>(json); break;
                    case RadioSystem.SystemTag:
                        Radio = JsonSerializer.Deserialize<RadioSystem>(json);
                        Radio.Sanitize();
                        break;
                    default: isHandled = false; break;
                }
                if (isHandled)
                {
                    ConfigurationUpdated(systemTag);
                    isSuccess = true;
                }
            }
            catch (Exception ex)
            {
                FileManager.Log($"C130JConfiguration:Deserialize exception {ex}");
            }
            return isSuccess;
        }
    }
}
