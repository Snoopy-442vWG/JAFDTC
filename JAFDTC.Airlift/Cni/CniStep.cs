// ********************************************************************************************************************
//
// CniStep.cs : one step in a planned CNI-MU key sequence
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

namespace JAFDTC.Airlift.Cni
{
    /// <summary>
    /// kinds of step in a planned key sequence.
    /// </summary>
    public enum CniStepKind
    {
        Key,            // press a CNI-MU key (Key holds the key name, see CniKeys)
        Wait,           // wait Value milliseconds
        FirstPage,      // press PREV PAGE until the CNI shows page 1 (needs cockpit feedback, done in Lua)
        Exec,           // run the airframe lua function Key with argument Arg (cockpit feedback, done in Lua)
        Note            // marker for the command log, no cockpit effect
    }

    /// <summary>
    /// one step in a planned CNI-MU key sequence. the planner emits these; the app maps them to dcs clickable
    /// actions. ToString() gives a compact form used by the unit tests ("LSK_L1", "W200", "FIRSTPAGE", "#note").
    /// </summary>
    public sealed record CniStep(CniStepKind Kind, string Key = null, int Value = 0, string Arg = null)
    {
        public static CniStep Press(string key) => new(CniStepKind.Key, key);

        public static CniStep Wait(int ms) => new(CniStepKind.Wait, null, ms);

        public static CniStep FirstPage() => new(CniStepKind.FirstPage);

        public static CniStep Note(string text) => new(CniStepKind.Note, text);

        public static CniStep Exec(string fn, string arg) => new(CniStepKind.Exec, fn, 0, arg);

        public override string ToString() => Kind switch
        {
            CniStepKind.Key => Key,
            CniStepKind.Wait => $"W{Value}",
            CniStepKind.FirstPage => "FIRSTPAGE",
            CniStepKind.Exec => $"{Key}({Arg})",
            _ => $"#{Key}"
        };
    }

    /// <summary>
    /// key names the planners use. the app's device manager defines an action for each of these.
    /// </summary>
    public static class CniKeys
    {
        public const string CLR = "CLR";
        public const string DEL = "DEL";
        public const string INDEX = "INDEX";
        public const string LEGS = "LEGS";
        public const string MARK = "MARK";
        public const string NEXT = "NEXT";
        public const string PREV = "PREV";
        public const string COMM_TUNE = "COMM_TUNE";
        public const string IDX = "IDX";                            // ch-47f cdu
        public const string FPLN = "FPLN";                          // ch-47f cdu
        public const string MSN = "MSN";                            // ch-47f cdu
        public const string UP = "UP";                              // ch-47f cdu
        public const string DOWN = "DOWN";                          // ch-47f cdu
        public const string ARC186_MODE_MANUAL = "ARC186_MODE_MANUAL";  // ch-47f arc-186 mode knob (other device)
        public const string ARC186_MODE_PRESET = "ARC186_MODE_PRESET";
        public const string SLASH = "/";
        public const string POINT = ".";

        public static string LskL(int n) => $"LSK_L{n}";

        public static string LskR(int n) => $"LSK_R{n}";

        /// <summary>
        /// characters with a scratchpad key: A-Z, 0-9, "/" and ".".
        /// </summary>
        public static bool IsTypeable(char c) => (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '/' || c == '.';
    }
}
