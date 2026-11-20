// ********************************************************************************************************************
//
// CH47FDeviceManager.cs -- ch-47f airframe device manager
//
// Copyright(C) 2023-2024 ilominar/raven, JAFDTC contributors
// Copyright(C) 2026 442 vWG
//
// CDU and AN/ARC-186 device and key codes are from DCS-DTC (the-paid-actor/dcs-dtc, GPL-3, CH47FUploader.json,
// commit ae9637d).
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

using JAFDTC.Airlift.Cni;
using JAFDTC.Models.Core;
using JAFDTC.Models.DCS;
using JAFDTC.Utilities;

namespace JAFDTC.Models.CH47F
{
    /// <summary>
    /// the ch-47f CDU (dcs device 37) and AN/ARC-186 VHF control head (device 51). key names match
    /// JAFDTC.Airlift.Cni.CniKeys; keys starting "ARC186_" belong to the ARC186 device.
    /// </summary>
    internal class CH47FDeviceManager : AirframeDeviceManagerBase, IAirframeDeviceManager
    {
        public const int DefaultDelayMs = 150;          // dcs-dtc ch-47f uses the c-130 base delay
        public const int ClrHoldFactor = 6;

        public CH47FDeviceManager()
        {
            int delay = (Settings.CommandDelaysMs.TryGetValue(AirframeTypes.CH47F, out int d) && (d > 0))
                        ? d : DefaultDelayMs;

            AirframeDevice cdu = new(37, "CDU");

            for (int i = 1; i <= 6; i++)
            {
                cdu.AddAction(3149 + i, CniKeys.LskL(i), delay, 1);         // L1 3150 .. L6 3155
                cdu.AddAction(3155 + i, CniKeys.LskR(i), delay, 1);         // R1 3156 .. R6 3161
            }

            cdu.AddAction(3164, CniKeys.IDX, delay, 1);
            cdu.AddAction(3171, CniKeys.FPLN, delay, 1);
            cdu.AddAction(3170, CniKeys.MSN, delay, 1);
            cdu.AddAction(3106, CniKeys.UP, delay, 1);
            cdu.AddAction(3107, CniKeys.DOWN, delay, 1);
            cdu.AddAction(3108, CniKeys.CLR, delay * ClrHoldFactor, 1);
            cdu.AddAction(3121, CniKeys.SLASH, delay, 1);
            cdu.AddAction(3120, CniKeys.POINT, delay, 1);

            for (int n = 0; n <= 9; n++)
                cdu.AddAction(3110 + n, n.ToString(), delay, 1);
            for (char c = 'A'; c <= 'Z'; c++)
                cdu.AddAction(3123 + (c - 'A'), c.ToString(), delay, 1);

            AddDevice(cdu);

            // arc-186 mode selector: set to a position and leave it (down value == up value, so no "release").
            AirframeDevice arc186 = new(51, "ARC186");
            arc186.AddAction(3004, CniKeys.ARC186_MODE_MANUAL, 0, 0.2, 0.2);
            arc186.AddAction(3004, CniKeys.ARC186_MODE_PRESET, 0, 0.3, 0.3);
            AddDevice(arc186);
        }
    }
}
