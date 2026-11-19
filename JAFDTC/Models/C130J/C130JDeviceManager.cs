// ********************************************************************************************************************
//
// C130JDeviceManager.cs -- c-130j airframe device manager
//
// Copyright(C) 2023-2024 ilominar/raven, JAFDTC contributors
// Copyright(C) 2026 442 vWG
//
// CNI-MU device and key codes are from DCS-DTC (the-paid-actor/dcs-dtc, GPL-3, C130Uploader.json, commit ae9637d).
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

namespace JAFDTC.Models.C130J
{
    /// <summary>
    /// the c-130j CNI-MU (dcs device 25). key names match JAFDTC.Airlift.Cni.CniKeys so planned steps map straight
    /// to actions.
    /// </summary>
    internal class C130JDeviceManager : AirframeDeviceManagerBase, IAirframeDeviceManager
    {
        public const int DefaultDelayMs = 150;          // dcs-dtc C-130 base delay
        public const int ClrHoldFactor = 6;             // CLR is held 6x the base delay to clear the scratchpad

        public C130JDeviceManager()
        {
            int delay = (Settings.CommandDelaysMs.TryGetValue(AirframeTypes.C130J, out int d) && (d > 0))
                        ? d : DefaultDelayMs;

            AirframeDevice cni = new(25, "CNI");

            cni.AddAction(3001, CniKeys.LskL(1), delay, 1);
            cni.AddAction(3002, CniKeys.LskL(2), delay, 1);
            cni.AddAction(3003, CniKeys.LskL(3), delay, 1);
            cni.AddAction(3004, CniKeys.LskL(4), delay, 1);
            cni.AddAction(3005, CniKeys.LskL(5), delay, 1);
            cni.AddAction(3006, CniKeys.LskL(6), delay, 1);
            cni.AddAction(3007, CniKeys.LskR(1), delay, 1);
            cni.AddAction(3008, CniKeys.LskR(2), delay, 1);
            cni.AddAction(3009, CniKeys.LskR(3), delay, 1);
            cni.AddAction(3010, CniKeys.LskR(4), delay, 1);
            cni.AddAction(3011, CniKeys.LskR(5), delay, 1);
            cni.AddAction(3012, CniKeys.LskR(6), delay, 1);

            cni.AddAction(3013, CniKeys.COMM_TUNE, delay, 1);
            cni.AddAction(3016, CniKeys.INDEX, delay, 1);
            cni.AddAction(3019, CniKeys.LEGS, delay, 1);
            cni.AddAction(3020, CniKeys.MARK, delay, 1);
            cni.AddAction(3026, CniKeys.NEXT, delay, 1);
            cni.AddAction(3027, CniKeys.PREV, delay, 1);
            cni.AddAction(3028, CniKeys.DEL, delay, 1);
            cni.AddAction(3029, CniKeys.CLR, delay * ClrHoldFactor, 1);

            cni.AddAction(3030, "0", delay, 1);
            cni.AddAction(3031, "1", delay, 1);
            cni.AddAction(3032, "2", delay, 1);
            cni.AddAction(3033, "3", delay, 1);
            cni.AddAction(3034, "4", delay, 1);
            cni.AddAction(3035, "5", delay, 1);
            cni.AddAction(3036, "6", delay, 1);
            cni.AddAction(3037, "7", delay, 1);
            cni.AddAction(3038, "8", delay, 1);
            cni.AddAction(3039, "9", delay, 1);
            cni.AddAction(3040, CniKeys.POINT, delay, 1);
            cni.AddAction(3068, CniKeys.SLASH, delay, 1);

            for (char c = 'A'; c <= 'Z'; c++)
                cni.AddAction(3042 + (c - 'A'), c.ToString(), delay, 1);

            AddDevice(cni);
        }
    }
}
