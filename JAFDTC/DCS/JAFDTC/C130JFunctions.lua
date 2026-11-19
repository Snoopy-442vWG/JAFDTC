--[[
********************************************************************************************************************

C130JFunctions.lua -- c-130j airframe-specific lua functions

Copyright(C) 2021-2023 the-paid-actor & dcs-dtc contributors
Copyright(C) 2026 442 vWG

CNI-MU page detection and the upload trigger are ported from DCS-DTC (the-paid-actor/dcs-dtc, GPL-3,
DCS/DCSDTC/C130Functions.lua, commit ae9637d).

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

function JAFDTC_C130J_Fn_NOP(msg)
    -- do nothing...
end

-- --------------------------------------------------------------------------------------------------------------------
--
-- cni-mu (indicator 8)
--
-- --------------------------------------------------------------------------------------------------------------------

-- returns the current page number from the "n/10" page counter the COMM TUNE pages show, nil if none is shown.
--
function JAFDTC_C130J_GetCommPage()
    local ok, txt = pcall(list_indication, 8)
    if not ok or type(txt) ~= "string" or #txt == 0 then
        return nil
    end
    for line in (txt .. "\n"):gmatch("(.-)\n") do
        line = line:gsub("%s+$", "")
        local page = line:match("(%d+)%s*/%s*10$")
        if page then
            return tonumber(page)
        end
    end
    return nil
end

-- true while the COMM TUNE page is past page 1. false when on page 1 or when no page counter is found, so the
-- caller's PREV PAGE loop can never run away.
--
function JAFDTC_C130J_Fn_IsCommPageNot1()
    local page = JAFDTC_C130J_GetCommPage()
    return (page ~= nil) and (page > 1)
end

-- --------------------------------------------------------------------------------------------------------------------
--
-- frame processing
--
-- --------------------------------------------------------------------------------------------------------------------

-- the upload hotkey is the unused cockpit argument 1166 that DCS-DTC binds; bind it in DCS controls the same way
-- (see the JAFDTC C-130J guide).
--
function JAFDTC_C130J_AfterNextFrame(params)
    local mainPanel = GetDevice(0)
    local uploadBtn = mainPanel:get_argument_value(1166)
    if uploadBtn == 1 then params["uploadCommand"] = "1" end
end
