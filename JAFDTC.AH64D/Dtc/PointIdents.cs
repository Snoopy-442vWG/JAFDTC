// ********************************************************************************************************************
//
// PointIdents.cs : AH-64D tactical point IDENT table
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

namespace JAFDTC.AH64D.Dtc
{
    /// <summary>
    /// AH-64D point databases. waypoints/hazards are W01-W50, control measures are C51-C99, targets/threats are
    /// T01-T50. these match the "WPTHZ", "CTRLM" and "TGT" sections of the dcs .dtc file.
    /// </summary>
    public enum PointCategory
    {
        WPTHZ = 0,
        CTRLM = 1,
        TGT = 2
    }

    /// <summary>
    /// one IDENT (the 2- or 3-character point type shown on the TSD) with the numeric value the .dtc stores.
    /// </summary>
    public sealed record PointIdent(PointCategory Category, string Code, int PointId, string Description)
    {
        /// <summary>
        /// value written to the .dtc "id" field. WPTHZ and CTRLM store the dcs Point_ID directly; TGT stores
        /// Point_ID - Point_ID.US (79), which is what the module's own getTTID() in DTU/DTC.lua does.
        /// </summary>
        public int DtcId => (Category == PointCategory.TGT) ? PointId - PointIdents.TargetIdOffset : PointId;

        public string DisplayName => $"{Code} – {Description}";
    }

    /// <summary>
    /// the IDENT table, transcribed from Point_ID in the dcs AH-64D module
    /// (Mods/aircraft/AH-64D/Cockpit/Scripts/Computers/DTU/DataTypes.lua). ranges: 1-10 waypoints/hazards,
    /// 11-79 control measures, 80-140 targets/threats. 141 (TRN, "for terrain") is internal and not offered.
    /// </summary>
    public static class PointIdents
    {
        public const int TargetIdOffset = 79;              // Point_ID.US

        public static int MinNumber(PointCategory cat) => (cat == PointCategory.CTRLM) ? 51 : 1;
        public static int MaxNumber(PointCategory cat) => (cat == PointCategory.CTRLM) ? 99 : 50;
        public static int MaxCount(PointCategory cat) => MaxNumber(cat) - MinNumber(cat) + 1;

        public static char Prefix(PointCategory cat) => cat switch
        {
            PointCategory.WPTHZ => 'W',
            PointCategory.CTRLM => 'C',
            _ => 'T'
        };

        public static string CategoryName(PointCategory cat) => cat switch
        {
            PointCategory.WPTHZ => "Waypoint / Hazard",
            PointCategory.CTRLM => "Control Measure",
            _ => "Target / Threat"
        };

        /// <summary>
        /// TSD point label for a category and number, e.g. "W01", "C51", "T07".
        /// </summary>
        public static string Label(PointCategory cat, int number) => $"{Prefix(cat)}{number:D2}";

        /// <summary>
        /// parse a TSD point label ("W01", "c51", "T7") into category and number. false if malformed or out of the
        /// category's number range.
        /// </summary>
        public static bool TryParseLabel(string label, out PointCategory cat, out int number)
        {
            cat = PointCategory.WPTHZ;
            number = 0;
            if (string.IsNullOrWhiteSpace(label))
                return false;
            label = label.Trim().ToUpperInvariant();
            switch (label[0])
            {
                case 'W': cat = PointCategory.WPTHZ; break;
                case 'H': cat = PointCategory.WPTHZ; break;         // hazards share the W/H 01-50 database
                case 'C': cat = PointCategory.CTRLM; break;
                case 'T': cat = PointCategory.TGT; break;
                default: return false;
            }
            if (!int.TryParse(label[1..], out number))
                return false;
            return (number >= MinNumber(cat)) && (number <= MaxNumber(cat));
        }

        private static readonly List<PointIdent> _all =
        [
            // waypoints, hazards
            new(PointCategory.WPTHZ, "LZ", 1, "Landing Zone"),
            new(PointCategory.WPTHZ, "CC", 2, "Communication Checkpoint"),
            new(PointCategory.WPTHZ, "PP", 3, "Passage Point"),
            new(PointCategory.WPTHZ, "RP", 4, "Release Point"),
            new(PointCategory.WPTHZ, "SP", 5, "Start Point"),
            new(PointCategory.WPTHZ, "WP", 6, "Waypoint"),
            new(PointCategory.WPTHZ, "TO", 7, "Tower Over 1000' AGL"),
            new(PointCategory.WPTHZ, "TU", 8, "Tower Under 1000' AGL"),
            new(PointCategory.WPTHZ, "WL", 9, "Wires - Power Lines"),
            new(PointCategory.WPTHZ, "WS", 10, "Wires - Telephone"),

            // control measures
            new(PointCategory.CTRLM, "AB", 11, "Friendly Airborne"),
            new(PointCategory.CTRLM, "AD", 12, "Friendly Air Defense"),
            new(PointCategory.CTRLM, "AH", 13, "Friendly Attack Helicopter"),
            new(PointCategory.CTRLM, "AM", 14, "Friendly Armor"),
            new(PointCategory.CTRLM, "AS", 15, "Friendly Air Assault"),
            new(PointCategory.CTRLM, "AV", 16, "Friendly Air Cavalry"),
            new(PointCategory.CTRLM, "CA", 17, "Friendly Armored Cavalry"),
            new(PointCategory.CTRLM, "CF", 18, "Friendly Chemical"),
            new(PointCategory.CTRLM, "DF", 19, "Friendly Decontamination"),
            new(PointCategory.CTRLM, "EN", 20, "Friendly Engineer"),
            new(PointCategory.CTRLM, "FG", 21, "Friendly General Army Helicopter"),
            new(PointCategory.CTRLM, "FI", 22, "Friendly Infantry"),
            new(PointCategory.CTRLM, "FL", 23, "Friendly Field Artillery"),
            new(PointCategory.CTRLM, "FU", 24, "Friendly Unit ID"),
            new(PointCategory.CTRLM, "FW", 25, "Friendly Electronic Warfare"),
            new(PointCategory.CTRLM, "HO", 26, "Friendly Hospital / Aid Station"),
            new(PointCategory.CTRLM, "MA", 27, "Friendly Aviation Maintenance"),
            new(PointCategory.CTRLM, "MD", 28, "Friendly Medical"),
            new(PointCategory.CTRLM, "MI", 29, "Friendly Mechanized Infantry"),
            new(PointCategory.CTRLM, "TF", 30, "Friendly Tactical Operations Center"),
            new(PointCategory.CTRLM, "WF", 31, "Friendly Fixed Wing"),
            new(PointCategory.CTRLM, "CE", 32, "Enemy Chemical"),
            new(PointCategory.CTRLM, "DE", 33, "Enemy Decontamination"),
            new(PointCategory.CTRLM, "AE", 34, "Enemy Armor"),
            new(PointCategory.CTRLM, "EB", 35, "Enemy Airborne"),
            new(PointCategory.CTRLM, "EC", 36, "Enemy Armored Cavalry"),
            new(PointCategory.CTRLM, "ED", 37, "Enemy Air Defense"),
            new(PointCategory.CTRLM, "EE", 38, "Enemy Engineer"),
            new(PointCategory.CTRLM, "EF", 39, "Enemy Field Artillery"),
            new(PointCategory.CTRLM, "EH", 40, "Enemy Hospital / Aid Station"),
            new(PointCategory.CTRLM, "EI", 41, "Enemy Infantry"),
            new(PointCategory.CTRLM, "EK", 42, "Enemy Attack Helicopter"),
            new(PointCategory.CTRLM, "EM", 43, "Enemy Mechanized Infantry"),
            new(PointCategory.CTRLM, "ES", 44, "Enemy Air Assault"),
            new(PointCategory.CTRLM, "ET", 45, "Enemy Tactical Operations Center"),
            new(PointCategory.CTRLM, "EU", 46, "Enemy Unit ID"),
            new(PointCategory.CTRLM, "EV", 47, "Enemy Air Cavalry"),
            new(PointCategory.CTRLM, "EX", 48, "Enemy Medical"),
            new(PointCategory.CTRLM, "HG", 49, "Enemy General Army Helicopter"),
            new(PointCategory.CTRLM, "ME", 50, "Enemy Aviation Maintenance"),
            new(PointCategory.CTRLM, "WE", 51, "Enemy Fixed Wing"),
            new(PointCategory.CTRLM, "WR", 52, "Enemy Electronic Warfare"),
            new(PointCategory.CTRLM, "BR", 53, "Bridge or Gap"),
            new(PointCategory.CTRLM, "CP", 54, "Checkpoint"),
            new(PointCategory.CTRLM, "BE", 55, "Nondirectional Beacon (NDB)"),
            new(PointCategory.CTRLM, "RH", 56, "Railhead"),
            new(PointCategory.CTRLM, "AA", 57, "Assembly Area"),
            new(PointCategory.CTRLM, "AP", 58, "Air Control Point"),
            new(PointCategory.CTRLM, "BP", 59, "Battle Position"),
            new(PointCategory.CTRLM, "FA", 60, "Forward Assembly Area"),
            new(PointCategory.CTRLM, "HA", 61, "Holding Area"),
            new(PointCategory.CTRLM, "AG", 62, "Airfield - General"),
            new(PointCategory.CTRLM, "AI", 63, "Airfield - Instrumented"),
            new(PointCategory.CTRLM, "AL", 64, "Lighted Airport"),
            new(PointCategory.CTRLM, "GL", 65, "Ground Lights / Small Town"),
            new(PointCategory.CTRLM, "F1", 66, "Artillery Fire Registration Point - Part 1"),
            new(PointCategory.CTRLM, "F2", 67, "Artillery Fire Registration Point - Part 2"),
            new(PointCategory.CTRLM, "FC", 68, "FARP - Fuel and Ammunition"),
            new(PointCategory.CTRLM, "FF", 69, "FARP - Fuel Only"),
            new(PointCategory.CTRLM, "FM", 70, "FARP - Ammunition Only"),
            new(PointCategory.CTRLM, "ID", 71, "IDM Subscriber"),
            new(PointCategory.CTRLM, "BD", 72, "Brigade"),
            new(PointCategory.CTRLM, "BN", 73, "Battalion"),
            new(PointCategory.CTRLM, "CO", 74, "Company"),
            new(PointCategory.CTRLM, "CR", 75, "Corps"),
            new(PointCategory.CTRLM, "DI", 76, "Division"),
            new(PointCategory.CTRLM, "GP", 77, "Regiment / Group"),
            new(PointCategory.CTRLM, "NB", 78, "NBC Contaminated Area"),
            new(PointCategory.CTRLM, "US", 79, "US Army"),

            // targets, threats
            new(PointCategory.TGT, "TG", 80, "Target"),
            new(PointCategory.TGT, "GU", 81, "Generic ADU"),
            new(PointCategory.TGT, "1", 82, "SA-1 ADU"),
            new(PointCategory.TGT, "2", 83, "SA-2 ADU"),
            new(PointCategory.TGT, "3", 84, "SA-3 ADU"),
            new(PointCategory.TGT, "4", 85, "SA-4 ADU"),
            new(PointCategory.TGT, "5", 86, "SA-5 ADU"),
            new(PointCategory.TGT, "6", 87, "SA-6 ADU"),
            new(PointCategory.TGT, "7", 88, "SA-7 ADU"),
            new(PointCategory.TGT, "8", 89, "SA-8 ADU"),
            new(PointCategory.TGT, "9", 90, "SA-9 ADU"),
            new(PointCategory.TGT, "10", 91, "SA-10 ADU"),
            new(PointCategory.TGT, "11", 92, "SA-11 ADU"),
            new(PointCategory.TGT, "12", 93, "SA-12 ADU"),
            new(PointCategory.TGT, "13", 94, "SA-13 / SA-19 ADU"),
            new(PointCategory.TGT, "14", 95, "SA-14 ADU"),
            new(PointCategory.TGT, "15", 96, "SA-15 ADU"),
            new(PointCategory.TGT, "16", 97, "SA-16 ADU"),
            new(PointCategory.TGT, "17", 98, "SA-17 ADU"),
            new(PointCategory.TGT, "S6", 99, "2S6 ADU"),
            new(PointCategory.TGT, "ZU", 100, "ZSU-23-4 ADU"),
            new(PointCategory.TGT, "AS", 101, "ASPIDE ADU"),
            new(PointCategory.TGT, "M83", 102, "M1983 ADU"),
            new(PointCategory.TGT, "HK", 103, "HAWK / I-HAWK ADU"),
            new(PointCategory.TGT, "RO", 104, "ROLAND ADU"),
            new(PointCategory.TGT, "AA", 105, "AAA (> 57mm) ADU"),
            new(PointCategory.TGT, "C2", 106, "CSA-21/X ADU"),
            new(PointCategory.TGT, "CT", 107, "CROTALE ADU"),
            new(PointCategory.TGT, "RA", 108, "RAPIER ADU"),
            new(PointCategory.TGT, "GT", 109, "Towed Air Defense Gun (> 57mm)"),
            new(PointCategory.TGT, "GS", 110, "Self-Propelled Air Defense Gun (< 57mm)"),
            new(PointCategory.TGT, "TR", 111, "Target Acquisition Radar"),
            new(PointCategory.TGT, "U", 112, "Unknown ADU"),
            new(PointCategory.TGT, "SA", 113, "Towed Multi-vehicle SAM ADU"),
            new(PointCategory.TGT, "SP", 114, "Self-Propelled SAM ADU"),
            new(PointCategory.TGT, "R70", 115, "RBS-70 ADU"),
            new(PointCategory.TGT, "SR", 116, "Battlefield Surveillance Radar"),
            new(PointCategory.TGT, "NV", 117, "Naval ADU"),
            new(PointCategory.TGT, "G1", 118, "Growth 1 ADU"),
            new(PointCategory.TGT, "G2", 119, "Growth 2 ADU"),
            new(PointCategory.TGT, "G3", 120, "Growth 3 ADU"),
            new(PointCategory.TGT, "G4", 121, "Growth 4 ADU"),
            new(PointCategory.TGT, "PT", 122, "PATRIOT ADU"),
            new(PointCategory.TGT, "ST", 123, "STINGER / LAW-ADS ADU"),
            new(PointCategory.TGT, "RE", 124, "REDEYE ADU"),
            new(PointCategory.TGT, "CH", 125, "CHAPARRAL ADU"),
            new(PointCategory.TGT, "TC", 126, "TIGERCAT ADU"),
            new(PointCategory.TGT, "SD", 127, "SPADA ADU"),
            new(PointCategory.TGT, "BH", 128, "BLOODHOUND ADU"),
            new(PointCategory.TGT, "SS", 129, "STARSTREAK ADU"),
            new(PointCategory.TGT, "JA", 130, "JAVELIN (Shorts) ADU"),
            new(PointCategory.TGT, "BP", 131, "BLOWPIPE ADU"),
            new(PointCategory.TGT, "SM", 132, "SAMP ADU"),
            new(PointCategory.TGT, "SC", 133, "SATCP ADU"),
            new(PointCategory.TGT, "SH", 134, "SHAHINE / R440 ADU"),
            new(PointCategory.TGT, "GP", 135, "GEPARD ADG (< 57mm)"),
            new(PointCategory.TGT, "VU", 136, "VULCAN ADG (< 57mm)"),
            new(PointCategory.TGT, "MK", 137, "MARKSMAN ADU"),
            new(PointCategory.TGT, "SB", 138, "SABRE ADU"),
            new(PointCategory.TGT, "AX", 139, "AMX-13 ADU"),
            new(PointCategory.TGT, "AD", 140, "Friendly ADU"),
        ];

        public static IReadOnlyList<PointIdent> All => _all;

        public static IReadOnlyList<PointIdent> ForCategory(PointCategory cat)
            => [.. _all.Where(i => i.Category == cat) ];

        /// <summary>
        /// default IDENT for a new point in a category.
        /// </summary>
        public static PointIdent Default(PointCategory cat) => cat switch
        {
            PointCategory.WPTHZ => Find(cat, "WP"),
            PointCategory.CTRLM => Find(cat, "CP"),
            _ => Find(cat, "TG")
        };

        /// <summary>
        /// find an IDENT by category and code. for targets the SA numbers can be given as "6", "SA6", "SA-6",
        /// "SA 6"; "M3"/"R7" are accepted for M83/R70. returns null if not found.
        /// </summary>
        public static PointIdent Find(PointCategory cat, string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return null;
            string c = code.Trim().ToUpperInvariant();
            if (cat == PointCategory.TGT)
            {
                if (c.StartsWith("SA") && (c.Length > 2))
                {
                    string rest = c[2..].TrimStart('-', ' ');
                    if (int.TryParse(rest, out int sa))
                        c = sa.ToString();
                }
                if (c == "M3") c = "M83";
                if (c == "R7") c = "R70";
                if (int.TryParse(c, out int num))
                    c = num.ToString();
            }
            return _all.FirstOrDefault(i => (i.Category == cat) && (i.Code == c));
        }

        /// <summary>
        /// find an IDENT from the numeric "id" value stored in a .dtc file for the given category.
        /// </summary>
        public static PointIdent FromDtcId(PointCategory cat, int dtcId)
        {
            int pointId = (cat == PointCategory.TGT) ? dtcId + TargetIdOffset : dtcId;
            return _all.FirstOrDefault(i => (i.Category == cat) && (i.PointId == pointId));
        }
    }
}
