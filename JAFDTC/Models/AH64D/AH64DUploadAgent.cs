// ********************************************************************************************************************
//
// AH64DUploadAgent.cs : AH-64D upload agent (dtc file output only)
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
using JAFDTC.Models.DCS;
using JAFDTC.Utilities;
using JAFDTC.Utilities.Networking;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace JAFDTC.Models.AH64D
{
    /// <summary>
    /// the AH-64D is set up through the dcs native data transfer cartridge: jafdtc writes the .dtc file and the
    /// crew loads it with the DTC manager / DTU page. there is no clickable-cockpit upload, so this agent does
    /// nothing (dcs never reports an AH-64D to jafdtc, so "load to jet" stays disabled for these configurations).
    /// </summary>
    public sealed class AH64DUploadAgent : IUploadAgent
    {
        private readonly AH64DConfiguration _cfg;

        public AH64DUploadAgent(AH64DConfiguration cfg) => _cfg = cfg;

        /// <summary>
        /// "load to jet" for the apache: (re)write the merged dtc tape to its output file and tell the crew, in the
        /// cockpit, to load it from the DCS DTC interface. returns false if there is no output file or the merge
        /// fails.
        /// </summary>
        public Task<bool> Load()
        {
            string path = _cfg.DTE.OutputPath;
            if (string.IsNullOrEmpty(path))
            {
                // no output file yet: use Saved Games\DCS\DTC\<configuration name>.dtc (the folder dcs lists), merge
                // every apache system, and remember the choice in the configuration.
                path = DefaultOutputPath(_cfg.Name);
                if (path == null)
                {
                    StatusMessageTx.Send("AH-64D: set the DTC output file on the DCS DTC Tape page in JAFDTC");
                    FileManager.Log($"AH64DUploadAgent:Load \"{_cfg.Name}\": no DTC output file, no DCS DTC folder");
                    return Task.FromResult(false);
                }
                _cfg.DTE.OutputPath = path;
                if (_cfg.DTE.MergedSystemTags.Count == 0)
                    foreach (string tag in _cfg.MergeableSysTags)
                        _cfg.DTE.MergedSystemTags.Add(tag);
                // persist without raising ConfigurationSaved: load can run off the ui thread (cockpit trigger).
                FileManager.SaveConfigurationFile(_cfg);
                FileManager.Log($"AH64DUploadAgent:Load \"{_cfg.Name}\": output file set to {path}");
            }
            if (!_cfg.SaveMergedSimDTC())
            {
                StatusMessageTx.Send($"AH-64D DTC not written: {_cfg.LastSimDTCError}");
                return Task.FromResult(false);
            }
            StatusMessageTx.Send($"DTC \"{Path.GetFileNameWithoutExtension(path)}\" written. " +
                                 "Load it from the DTC interface (F8 Ground Crew > DTC).");
            FileManager.Log($"AH64DUploadAgent:Load \"{_cfg.Name}\": wrote {path}");
            return Task.FromResult(true);
        }

        /// <summary>
        /// Saved Games\DCS\DTC\<name>.dtc (or DCS.openbeta), with characters windows does not allow in file names
        /// replaced. null if neither saved games folder exists.
        /// </summary>
        private static string DefaultOutputPath(string cfgName)
        {
            string savedGames = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile),
                                             "Saved Games");
            string dcs = null;
            foreach (string variant in new[] { "DCS", "DCS.openbeta" })
                if (Directory.Exists(Path.Combine(savedGames, variant)))
                {
                    dcs = Path.Combine(savedGames, variant);
                    break;
                }
            if (dcs == null)
                return null;
            string dir = Path.Combine(dcs, "DTC");
            Directory.CreateDirectory(dir);
            string name = string.Join("-", (cfgName ?? "AH-64D").Split(Path.GetInvalidFileNameChars(),
                                                                         System.StringSplitOptions.RemoveEmptyEntries))
                          .Trim();
            while (name.Contains("--"))
                name = name.Replace("--", "-");
            if (string.IsNullOrEmpty(name))
                name = "AH-64D";
            return Path.Combine(dir, name + ".dtc");
        }

        public Task<bool> OpenDCSDTCEditor() => Task.FromResult(_cfg.SaveMergedSimDTC());

        public void BuildSystems(StringBuilder sb) { }

        public IBuilder SetupBuilder(StringBuilder sb) => null;

        public IBuilder TeardownBuilder(StringBuilder sb) => null;

        public IBuilder OpenDCSDTCBuilder(StringBuilder sb) => null;
    }
}
