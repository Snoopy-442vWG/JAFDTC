// ********************************************************************************************************************
//
// BattleBookProfiles.cs -- v303 A-10C Battle Book DSMS weapon profiles (Z-sled data)
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

using System.Collections.Generic;
using System.Linq;

namespace JAFDTC.Models.A10C.DSMS
{
    /// <summary>
    /// A weapon profile from the v303 A-10C Battle Book Z diagrams ("Z-sleds"), expressed as the DSMS settings the
    /// pilot would enter. Release is ripple single (per 476TTP 5.4.1.1) when the quantity is more than one, and
    /// fuzing is nose/tail, as shown on the Z diagrams. Values are as printed on the diagrams.
    /// </summary>
    public class BattleBookProfile
    {
        public string Name { get; }                 // 8-character DSMS profile name, e.g., "82 20LD2"
        public int MunitionID { get; }              // munition (A10CMunition.ID) the profile is built from
        public string Source { get; }
        public EscapeManeuvers Escape { get; }      // "SEM" on the Z diagram
        public string MinAlt { get; }               // "MALT" on the Z diagram, feet ("" if blank or not a number)
        public string TOF { get; }                  // "DTOF" on the Z diagram, seconds
        public string RippleQty { get; }            // "Qty" on the Z diagram
        public string RippleFt { get; }             // "Int" on the Z diagram, feet ("" if single)
        public bool IsHighDrag { get; }             // DTOF is the high-drag TOF (Mk-82 Air) instead of the desired TOF

        public string ImageFile => Name.Replace(' ', '_') + ".png";      // Z diagram image in Images\zsled

        public string Display
        {
            get
            {
                string tof = IsHighDrag ? "HD TOF" : "DTOF";
                string alt = string.IsNullOrEmpty(MinAlt) ? "-" : MinAlt;
                string qty = (RippleQty == "1") ? "1 bomb" : $"{RippleQty} @ {RippleFt} ft";
                return $"{Name}   ({tof} {TOF}, MALT {alt}, {qty})";
            }
        }

        public BattleBookProfile(string name, int munitionID, string source, EscapeManeuvers escape, string minAlt,
                                 string tof, string rippleQty, string rippleFt, bool isHighDrag)
        {
            Name = name;
            MunitionID = munitionID;
            Source = source;
            Escape = escape;
            MinAlt = minAlt;
            TOF = tof;
            RippleQty = rippleQty;
            RippleFt = rippleFt;
            IsHighDrag = isHighDrag;
        }

        /// <summary>
        /// copy the Z diagram values on to settings. only values the munition's profile has fields for are set.
        /// </summary>
        public void ApplyTo(MunitionSettings settings)
        {
            A10CMunition munition = A10CMunition.GetMunitionFromID(MunitionID);

            settings.EscapeManeuver = ((int)Escape).ToString();
            settings.FuzeOption = ((int)FuzeOptions.NoseTail).ToString();
            settings.ReleaseMode = (int.Parse(RippleQty) > 1) ? ((int)ReleaseModes.RIP_SGL).ToString()
                                                              : ((int)ReleaseModes.SGL).ToString();
            settings.RippleQty = RippleQty;
            if (!string.IsNullOrEmpty(RippleFt))
                settings.RippleFt = RippleFt;
            if (munition.MinAltField)
                settings.MinAlt = MinAlt;
            if (IsHighDrag)
            {
                if (munition.DragTOF)
                    settings.HDTOF = TOF;
            }
            else if (munition.DesTOF)
                settings.DesTOF = TOF;
        }
    }

    /// <summary>
    /// static list of the Battle Book profiles.
    /// </summary>
    public static class BattleBookProfiles
    {
        private static readonly List<BattleBookProfile> _profiles = new()
        {
            new("82 20LD2", 14, "Mk-82 LDGP Z Diagrams", EscapeManeuvers.CLB, "1000", "7.1", "2", "70", false),
            new("82 20LD4", 14, "Mk-82 LDGP Z Diagrams", EscapeManeuvers.CLB, "1000", "7", "4", "50", false),
            new("82 20LD6", 14, "Mk-82 LDGP Z Diagrams", EscapeManeuvers.CLB, "1000", "7.5", "6", "50", false),
            new("82 30DB2", 14, "Mk-82 LDGP Z Diagrams", EscapeManeuvers.CLB, "1000", "7.1", "2", "70", false),
            new("82 30DB4", 14, "Mk-82 LDGP Z Diagrams", EscapeManeuvers.CLB, "1000", "7.3", "4", "50", false),
            new("82 30DB6", 14, "Mk-82 LDGP Z Diagrams", EscapeManeuvers.CLB, "1000", "7.5", "6", "50", false),
            new("82 45DB2", 14, "Mk-82 LDGP Z Diagrams", EscapeManeuvers.CLB, "1000", "8.3", "2", "70", false),
            new("82 45DB4", 14, "Mk-82 LDGP Z Diagrams", EscapeManeuvers.CLB, "1000", "8.4", "4", "50", false),
            new("82 45DB6", 14, "Mk-82 LDGP Z Diagrams", EscapeManeuvers.CLB, "1000", "8.8", "6", "50", false),
            new("82 30HA2", 14, "Mk-82 LDGP Z Diagrams", EscapeManeuvers.CLB, "4500", "12.7", "2", "70", false),
            new("82 30HA4", 14, "Mk-82 LDGP Z Diagrams", EscapeManeuvers.CLB, "4500", "12.8", "4", "50", false),
            new("82 30HA6", 14, "Mk-82 LDGP Z Diagrams", EscapeManeuvers.CLB, "4500", "12.9", "6", "50", false),
            new("82 45HA2", 14, "Mk-82 LDGP Z Diagrams", EscapeManeuvers.CLB, "4500", "12.7", "2", "70", false),
            new("82 45HA4", 14, "Mk-82 LDGP Z Diagrams", EscapeManeuvers.CLB, "4500", "12.8", "4", "50", false),
            new("82 45HA6", 14, "Mk-82 LDGP Z Diagrams", EscapeManeuvers.CLB, "4500", "13.0", "6", "50", false),
            new("A 10HD2", 15, "Mk-82 Air Z Diagrams", EscapeManeuvers.TLT, "", "6", "2", "70", true),
            new("A 10HD4", 15, "Mk-82 Air Z Diagrams", EscapeManeuvers.TLT, "", "6", "4", "50", true),
            new("A 10HD6", 15, "Mk-82 Air Z Diagrams", EscapeManeuvers.TLT, "", "6.5", "6", "50", true),
            new("A 20HD2", 15, "Mk-82 Air Z Diagrams", EscapeManeuvers.TLT, "", "4.1", "2", "75", true),
            new("A 20HD4", 15, "Mk-82 Air Z Diagrams", EscapeManeuvers.TLT, "", "6.2", "4", "75", true),
            new("A 20HD6", 15, "Mk-82 Air Z Diagrams", EscapeManeuvers.TLT, "", "6.2", "6", "75", true),
            new("A 30HD2", 15, "Mk-82 Air Z Diagrams", EscapeManeuvers.CLB, "", "6.7", "2", "75", true),
            new("A 30HD4", 15, "Mk-82 Air Z Diagrams", EscapeManeuvers.CLB, "", "7.5", "4", "75", true),
            new("A 30HD6", 15, "Mk-82 Air Z Diagrams", EscapeManeuvers.CLB, "", "6.8", "6", "75", true),
            new("84 30LD1", 16, "Mk-84 Z Diagrams", EscapeManeuvers.CLB, "", "6.9", "1", "", false),
            new("84 30LD2", 16, "Mk-84 Z Diagrams", EscapeManeuvers.CLB, "1000", "7.8", "2", "100", false),
            new("84 45DB1", 16, "Mk-84 Z Diagrams", EscapeManeuvers.CLB, "1000", "7.3", "1", "", false),
            new("84 45DB2", 16, "Mk-84 Z Diagrams", EscapeManeuvers.CLB, "1000", "8.3", "2", "100", false),
            new("84 45HA1", 16, "Mk-84 Z Diagrams", EscapeManeuvers.CLB, "1000", "11.9", "1", "", false),
            new("84 45HA2", 16, "Mk-84 Z Diagrams", EscapeManeuvers.CLB, "1000", "12.6", "2", "100", false),
            new("87E20HD2", 1, "CBU-87 Z Diagrams", EscapeManeuvers.CLB, "1000", "11.1", "2", "60", false),
            new("87E20HD4", 1, "CBU-87 Z Diagrams", EscapeManeuvers.CLB, "1000", "11.4", "4", "30", false),
            new("87E30HD2", 1, "CBU-87 Z Diagrams", EscapeManeuvers.CLB, "1000", "10.1", "2", "60", false),
            new("87E30HD4", 1, "CBU-87 Z Diagrams", EscapeManeuvers.CLB, "1000", "10.5", "4", "30", false),
            new("87E45HD2", 1, "CBU-87 Z Diagrams", EscapeManeuvers.CLB, "4500", "14.6", "2", "60", false),
            new("87E45HD4", 1, "CBU-87 Z Diagrams", EscapeManeuvers.CLB, "4500", "15.1", "4", "30", false),
            new("B2 20LD1", 17, "BDU-33 Z Diagrams", EscapeManeuvers.CLB, "1000", "7.5", "2", "70", false),
            new("B2 20LD2", 17, "BDU-33 Z Diagrams", EscapeManeuvers.CLB, "1000", "5", "2", "70", false),
            new("B2 20LD4", 17, "BDU-33 Z Diagrams", EscapeManeuvers.CLB, "1000", "7.4", "4", "50", false),
            new("B2 30DB1", 17, "BDU-33 Z Diagrams", EscapeManeuvers.CLB, "1000", "6.9", "1", "", false),
            new("B2 30DB2", 17, "BDU-33 Z Diagrams", EscapeManeuvers.CLB, "1000", "7.6", "2", "70", false),
            new("B2 30DB4", 17, "BDU-33 Z Diagrams", EscapeManeuvers.CLB, "1000", "7.7", "4", "50", false),
            new("B2 45DB1", 17, "BDU-33 Z Diagrams", EscapeManeuvers.CLB, "1000", "7.8", "1", "", false),
            new("B2 45DB2", 17, "BDU-33 Z Diagrams", EscapeManeuvers.CLB, "1000", "8.9", "2", "70", false),
            new("B2 45DB4", 17, "BDU-33 Z Diagrams", EscapeManeuvers.CLB, "1000", "9", "4", "50", false),
            new("B2 30HA1", 17, "BDU-33 Z Diagrams", EscapeManeuvers.CLB, "4500", "13.2", "1", "", false),
            new("B2 30HA2", 17, "BDU-33 Z Diagrams", EscapeManeuvers.CLB, "4500", "13.7", "2", "70", false),
            new("B2 30HA4", 17, "BDU-33 Z Diagrams", EscapeManeuvers.CLB, "4500", "13.8", "4", "50", false),
            new("B2 45HA1", 17, "BDU-33 Z Diagrams", EscapeManeuvers.CLB, "4500", "13.1", "1", "", false),
            new("B2 45HA2", 17, "BDU-33 Z Diagrams", EscapeManeuvers.CLB, "4500", "13.9", "2", "70", false),
            new("B2 45HA4", 17, "BDU-33 Z Diagrams", EscapeManeuvers.CLB, "4500", "14", "4", "50", false),
            new("50 20LD2", 18, "BDU-50LD Z Diagrams (Mk-82 LDGP)", EscapeManeuvers.CLB, "1000", "7.1", "2", "70", false),
            new("50 20LD4", 18, "BDU-50LD Z Diagrams (Mk-82 LDGP)", EscapeManeuvers.CLB, "1000", "7", "4", "50", false),
            new("50 20LD6", 18, "BDU-50LD Z Diagrams (Mk-82 LDGP)", EscapeManeuvers.CLB, "1000", "7.5", "6", "50", false),
            new("50 30DB2", 18, "BDU-50LD Z Diagrams (Mk-82 LDGP)", EscapeManeuvers.CLB, "1000", "7.1", "2", "70", false),
            new("50 30DB4", 18, "BDU-50LD Z Diagrams (Mk-82 LDGP)", EscapeManeuvers.CLB, "1000", "7.3", "4", "50", false),
            new("50 30DB6", 18, "BDU-50LD Z Diagrams (Mk-82 LDGP)", EscapeManeuvers.CLB, "1000", "7.5", "6", "50", false),
            new("50 45DB2", 18, "BDU-50LD Z Diagrams (Mk-82 LDGP)", EscapeManeuvers.CLB, "1000", "8.3", "2", "70", false),
            new("50 45DB4", 18, "BDU-50LD Z Diagrams (Mk-82 LDGP)", EscapeManeuvers.CLB, "1000", "8.4", "4", "50", false),
            new("50 45DB6", 18, "BDU-50LD Z Diagrams (Mk-82 LDGP)", EscapeManeuvers.CLB, "1000", "8.8", "6", "50", false),
            new("50 30HA2", 18, "BDU-50LD Z Diagrams (Mk-82 LDGP)", EscapeManeuvers.CLB, "4500", "12.7", "2", "70", false),
            new("50 30HA4", 18, "BDU-50LD Z Diagrams (Mk-82 LDGP)", EscapeManeuvers.CLB, "4500", "12.8", "4", "50", false),
            new("50 30HA6", 18, "BDU-50LD Z Diagrams (Mk-82 LDGP)", EscapeManeuvers.CLB, "4500", "12.9", "6", "50", false),
            new("50 45HA2", 18, "BDU-50LD Z Diagrams (Mk-82 LDGP)", EscapeManeuvers.CLB, "4500", "12.7", "2", "70", false),
            new("50 45HA4", 18, "BDU-50LD Z Diagrams (Mk-82 LDGP)", EscapeManeuvers.CLB, "4500", "12.8", "4", "50", false),
            new("50 45HA6", 18, "BDU-50LD Z Diagrams (Mk-82 LDGP)", EscapeManeuvers.CLB, "4500", "13.0", "6", "50", false),
            new("50A10HD2", 19, "BDU-50PO Z Diagrams (Mk-82 Air)", EscapeManeuvers.TLT, "", "6", "2", "70", true),
            new("50A10HD4", 19, "BDU-50PO Z Diagrams (Mk-82 Air)", EscapeManeuvers.TLT, "", "6", "4", "50", true),
            new("50A10HD6", 19, "BDU-50PO Z Diagrams (Mk-82 Air)", EscapeManeuvers.TLT, "", "6.5", "6", "50", true),
            new("50A20HD2", 19, "BDU-50PO Z Diagrams (Mk-82 Air)", EscapeManeuvers.TLT, "", "4.1", "2", "75", true),
            new("50A20HD4", 19, "BDU-50PO Z Diagrams (Mk-82 Air)", EscapeManeuvers.TLT, "", "6.2", "4", "75", true),
            new("50A20HD6", 19, "BDU-50PO Z Diagrams (Mk-82 Air)", EscapeManeuvers.TLT, "", "6.2", "6", "75", true),
            new("50A30HD2", 19, "BDU-50PO Z Diagrams (Mk-82 Air)", EscapeManeuvers.CLB, "", "6.7", "2", "75", true),
            new("50A30HD4", 19, "BDU-50PO Z Diagrams (Mk-82 Air)", EscapeManeuvers.CLB, "", "7.5", "4", "75", true),
            new("50A30HD6", 19, "BDU-50PO Z Diagrams (Mk-82 Air)", EscapeManeuvers.CLB, "", "6.8", "6", "75", true),
        };

        public static List<BattleBookProfile> ForMunition(int munitionID)
            => _profiles.Where(p => p.MunitionID == munitionID).ToList();
    }
}
