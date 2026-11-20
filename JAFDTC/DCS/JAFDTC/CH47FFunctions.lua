--[[
********************************************************************************************************************

CH47FFunctions.lua -- ch-47f airframe-specific lua functions

Copyright(C) 2021-2023 the-paid-actor & dcs-dtc contributors
Copyright(C) 2026 442 vWG

The AN/ARC-186 frequency knob logic and the upload trigger are ported from DCS-DTC (the-paid-actor/dcs-dtc, GPL-3,
DCS/DCSDTC/CH47FFunctions.lua, commit ae9637d).

This program is free software: you can redistribute it and/or modify it under the terms of the GNU General
Public License as published by the Free Software Foundation, either version 3 of the License, or (at your
option) any later version.

This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the
implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General Public License
for more details.

You should have received a copy of the GNU General Public License along with this program.  If not, see
<https://www.gnu.org/licenses/>.

********************************************************************************************************************
--]]

-- --------------------------------------------------------------------------------------------------------------------
--
-- general functions
--
-- --------------------------------------------------------------------------------------------------------------------

function JAFDTC_CH47F_Fn_NOP(msg)
    -- do nothing...
end

-- returns the cdu (indicator 1) as a name -> value table.
--
function JAFDTC_CH47F_GetCDU()
    local t = {}
    local ok, li = pcall(list_indication, 1)
    if not ok or type(li) ~= "string" then
        return t
    end
    for name, value in li:gmatch("-----------------------------------------\n([^\n]+)\n([^\n]*)\n") do
        t[name] = value
    end
    return t
end

-- --------------------------------------------------------------------------------------------------------------------
--
-- an/arc-186 vhf (device 51)
--
-- --------------------------------------------------------------------------------------------------------------------

-- splits "124.775" into the four knob positions { 12, 4, 7, 3 }: tens of MHz (3-15), MHz (0-9), 100 kHz (0-9)
-- and 25 kHz steps (0-3). returns nil if the text is not a frequency.
--
function JAFDTC_CH47F_ParseV3Freq(text)
    local b, c = string.match(JAFDTC_TrimString(text or ""), "^(%d+)%.(%d+)$")
    if not b or not c or (#b ~= 2 and #b ~= 3) or #c < 1 then
        return nil
    end
    c = (c .. "000"):sub(1, 3)
    local k1 = tonumber(b:sub(1, #b - 1))
    local k2 = tonumber(b:sub(#b, #b))
    local k3 = tonumber(c:sub(1, 1))
    local k4s = c:sub(2, 3)
    local k4 = 0
    if k4s == "25" then k4 = 1 elseif k4s == "50" then k4 = 2 elseif k4s == "75" then k4 = 3 end
    return { k1, k2, k3, k4 }
end

-- SetV3(string freq)
--
-- turn the four arc-186 frequency knobs (clickable 3009, 3011, 3013, 3015) from the frequency the cdu shows
-- (V3_freq) to freq. dcs-dtc first nudges knob 1 so the cdu refreshes after a preset was in use; we do the same.
-- each knob turns the direct difference (no wrap-around assumption), so it is correct whether or not the knobs
-- wrap. gives up quietly if either frequency cannot be read.
--
function JAFDTC_CH47F_Fn_SetV3(needed)
    JAFDTC_Core_PerformAction(51, 3009, 0.1, 0.1, 500)

    local cur = JAFDTC_CH47F_ParseV3Freq(JAFDTC_CH47F_GetCDU()["V3_freq"])
    local need = JAFDTC_CH47F_ParseV3Freq(needed)
    if cur == nil or need == nil then
        JAFDTC_Log("CH47F SetV3: cannot read current or requested frequency (" .. tostring(needed) .. ")")
        return
    end

    local cmd = 3009
    for i = 1, 4 do
        local turns = need[i] - cur[i]
        local step = 0.1
        local wait = 250
        if i == 4 then
            step = 0.25
            wait = 450
        end
        if turns < 0 then
            step = -step
        end
        for _ = 1, math.abs(turns) do
            JAFDTC_Core_PerformAction(51, cmd, step, step, wait)
        end
        cmd = cmd + 2
    end
end

-- --------------------------------------------------------------------------------------------------------------------
--
-- frame processing
--
-- --------------------------------------------------------------------------------------------------------------------

-- upload hotkey: cockpit argument 423 (the mfd DATA button DCS-DTC uses).
--
function JAFDTC_CH47F_AfterNextFrame(params)
    local mainPanel = GetDevice(0)
    local dataBtn = mainPanel:get_argument_value(423)
    if dataBtn == 1 then params["uploadCommand"] = "1" end
end
