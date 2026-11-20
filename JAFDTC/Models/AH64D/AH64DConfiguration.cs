// ********************************************************************************************************************
//
// AH64DConfiguration.cs : AH-64D Apache (BLK.II) airframe configuration
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

using JAFDTC.Models.Base;
using JAFDTC.Models.Core;
using JAFDTC.UI.AH64D;
using JAFDTC.Utilities;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace JAFDTC.Models.AH64D
{
    /// <summary>
    /// configuration for the AH-64D. jafdtc builds AH-64D setups as dcs native data transfer cartridge (.dtc)
    /// files: dtc missions 1 and 2 (points, routes, lines, areas, zones, ADF) and the comm presets are merged into
    /// a dcs AH-64D .dtc base tape. everything else on the tape (ASE, weapons, laser codes, IFF, ...) comes from the
    /// base tape, so set those in the dcs DTC manager and import that tape as the base.
    /// </summary>
    public partial class AH64DConfiguration : ConfigurationBase
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // constants
        //
        // ------------------------------------------------------------------------------------------------------------

        private const string _versionCfg = "AH64D-1.0";         // current version

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        public NAV1System NAV1 { get; set; }

        public NAV2System NAV2 { get; set; }

        public COMMSystem COMM { get; set; }

        public CoreSimDTCSystem DTE { get; set; }

        [JsonIgnore]
        public override List<string> MergeableSysTags =>
        [
            NAV1System.SystemTag,
            NAV2System.SystemTag,
            COMMSystem.SystemTag
        ];

        [JsonIgnore]
        public override IUploadAgent UploadAgent => new AH64DUploadAgent(this);

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public AH64DConfiguration(string uid, string name, Dictionary<string, string> linkedSysMap)
            : base(_versionCfg, AirframeTypes.AH64D, uid, name, linkedSysMap)
        {
            NAV1 = new NAV1System();
            NAV2 = new NAV2System();
            COMM = new COMMSystem();
            DTE = new CoreSimDTCSystem();
            ConfigurationUpdated();
        }

        public override IConfiguration Clone()
        {
            Dictionary<string, string> linkedSysMap = [ ];
            foreach (KeyValuePair<string, string> kvp in LinkedSysMap)
                linkedSysMap[new(kvp.Key)] = new(kvp.Value);
            AH64DConfiguration clone = new("", Name, linkedSysMap)
            {
                NAV1 = (NAV1System)NAV1.Clone(),
                NAV2 = (NAV2System)NAV2.Clone(),
                COMM = (COMMSystem)COMM.Clone(),
                DTE = CloneDTE(DTE)
            };
            clone.ResetUID();
            clone.ConfigurationUpdated();
            return clone;
        }

        private static CoreSimDTCSystem CloneDTE(CoreSimDTCSystem src)
        {
            // CoreSimDTCSystem's copy constructor does not carry MergedSystemTags, so copy them here.
            CoreSimDTCSystem dte = (CoreSimDTCSystem)src.Clone();
            foreach (string tag in src.MergedSystemTags)
                dte.MergedSystemTags.Add(tag);
            return dte;
        }

        public override void CloneSystemFrom(string systemTag, IConfiguration other)
        {
            AH64DConfiguration otherApache = other as AH64DConfiguration;
            switch (systemTag)
            {
                case NAV1System.SystemTag: NAV1 = (NAV1System)otherApache.NAV1.Clone(); break;
                case NAV2System.SystemTag: NAV2 = (NAV2System)otherApache.NAV2.Clone(); break;
                case COMMSystem.SystemTag: COMM = (COMMSystem)otherApache.COMM.Clone(); break;
                case CoreSimDTCSystem.SystemTag: DTE = CloneDTE(otherApache.DTE); break;
                default: break;
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // overriden class methods
        //
        // ------------------------------------------------------------------------------------------------------------

        public override void Sanitize(bool isResetUID = false)
        {
            base.Sanitize(isResetUID);

            NAV1.Sanitize();
            NAV2.Sanitize();
            COMM.Sanitize();
            DTE.Sanitize();
        }

        public override ISystem SystemForTag(string tag)
        {
            return tag switch
            {
                NAV1System.SystemTag => NAV1,
                NAV2System.SystemTag => NAV2,
                COMMSystem.SystemTag => COMM,
                CoreSimDTCSystem.SystemTag => DTE,
                _ => null,
            };
        }

        public override void ConfigurationUpdated(string updateSysTag = null)
        {
            AH64DConfigurationEditor editor = new(this);
            Dictionary<string, string> updatesStrings = editor.BuildUpdatesStrings(this);

            int nPoints = NAV1.Mission.Points.Count + NAV2.Mission.Points.Count;
            string extra = (nPoints > 0) ? $" with {nPoints} point" + ((nPoints > 1) ? "s" : "") : "";
            SystemInfoTextUI = updatesStrings["SystemInfoTextUI"] + extra;
            SystemInfoIconsUI = updatesStrings["SystemInfoIconsUI"];
            SystemInfoIconBadgesUI = updatesStrings["SystemInfoIconBadgesUI"];
        }

        public override string Serialize(string systemTag = null)
        {
            return systemTag switch
            {
                null => JsonSerializer.Serialize(this, ConfigurationBase.JsonOptions),
                NAV1System.SystemTag => JsonSerializer.Serialize(NAV1, ConfigurationBase.JsonOptions),
                NAV2System.SystemTag => JsonSerializer.Serialize(NAV2, ConfigurationBase.JsonOptions),
                COMMSystem.SystemTag => JsonSerializer.Serialize(COMM, ConfigurationBase.JsonOptions),
                CoreSimDTCSystem.SystemTag => JsonSerializer.Serialize(DTE, ConfigurationBase.JsonOptions),
                _ => null
            };
        }

        public override void AfterLoadFromJSON()
        {
            NAV1 ??= new NAV1System();
            NAV2 ??= new NAV2System();
            COMM ??= new COMMSystem();
            DTE ??= new CoreSimDTCSystem();

            NAV1.Mission ??= new();
            NAV2.Mission ??= new();
            COMM.Normalize();

            // TODO: if the version number is older than current, may need to update object
            Version = _versionCfg;

            ConfigurationUpdated();
        }

        public override bool SaveMergedSimDTC() => (DTE.IsDefault) || SaveMergedSimDTC(DTE);

        public override void AfterSystemEditorCompletes(string systemTag)
        {
            if (MergeableSysTags.Contains(systemTag))
            {
                // rebuild the tape (if the user asked for that) on a clone, holding the merge mutex, so the ui does
                // not wait on file i/o. same approach as the f-16c.
                AH64DConfiguration mergeCfg = (AH64DConfiguration)Clone();
                Task.Run(() =>
                {
                    if (mergeCfg.MergeMutexAcquire())
                    {
                        if (!mergeCfg.DTE.IsDefault && mergeCfg.DTE.EnableRebuildValue)
                            mergeCfg.SaveMergedSimDTC();
                        mergeCfg.MergeMutexRelease();
                    }
                });
            }
        }

        public override bool CanAcceptPasteForSystem(string cboardTag, string systemTag = null)
        {
            return (!string.IsNullOrEmpty(cboardTag) &&
                    (((systemTag != null) && (cboardTag.StartsWith(systemTag))) ||
                     ((systemTag == null) && ((cboardTag == NAV1System.SystemTag) ||
                                              (cboardTag == NAV2System.SystemTag) ||
                                              (cboardTag == COMMSystem.SystemTag) ||
                                              (cboardTag == CoreSimDTCSystem.SystemTag)))));
        }

        public override bool Deserialize(string systemTag, string json)
        {
            bool isSuccess = false;
            bool isHandled = true;
            try
            {
                switch (systemTag)
                {
                    case NAV1System.SystemTag: NAV1 = JsonSerializer.Deserialize<NAV1System>(json); break;
                    case NAV2System.SystemTag: NAV2 = JsonSerializer.Deserialize<NAV2System>(json); break;
                    case COMMSystem.SystemTag: COMM = JsonSerializer.Deserialize<COMMSystem>(json); COMM.Normalize(); break;
                    case CoreSimDTCSystem.SystemTag: DTE = JsonSerializer.Deserialize<CoreSimDTCSystem>(json); break;
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
                FileManager.Log($"AH64DConfiguration:Deserialize exception {ex}");
            }
            return isSuccess;
        }
    }
}
