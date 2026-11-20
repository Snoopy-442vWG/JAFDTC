// ********************************************************************************************************************
//
// CH47FUploadAgent.cs -- ch-47f upload agent and CDU command builder
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

using JAFDTC.Airlift.CH47F;
using JAFDTC.Airlift.Cni;
using JAFDTC.Models.CH47F.Radio;
using JAFDTC.Models.DCS;
using System.Collections.Generic;
using System.Text;

namespace JAFDTC.Models.CH47F
{
    /// <summary>
    /// turns planned CDU steps (JAFDTC.Airlift) into the JAFDTC cockpit command stream.
    /// </summary>
    internal class CH47FCduBuilder(CH47FDeviceManager dcsCmds, StringBuilder sb, IReadOnlyList<CniStep> steps)
        : BuilderBase(dcsCmds, sb), IBuilder
    {
        public override void Build(Dictionary<string, object> state = null)
        {
            if ((steps == null) || (steps.Count == 0))
                return;

            AirframeDevice cdu = _aircraft.GetDevice("CDU");
            AirframeDevice arc186 = _aircraft.GetDevice("ARC186");
            foreach (CniStep step in steps)
            {
                switch (step.Kind)
                {
                    case CniStepKind.Key:
                        AddAction(step.Key.StartsWith("ARC186_") ? arc186 : cdu, step.Key);
                        break;
                    case CniStepKind.Wait:
                        AddWait(step.Value);
                        break;
                    case CniStepKind.Exec:
                        AddExecFunction(step.Key, [ step.Arg ?? "" ]);
                        break;
                    case CniStepKind.Note:
                        AddExecFunction("NOP", [ $"==== {step.Key}" ]);
                        break;
                    default:
                        break;
                }
            }
        }
    }

    /// <summary>
    /// ch-47f upload agent: ACP waypoints, the flight plan, then UHF-1 and VHF-1. the order matches DCS-DTC.
    /// </summary>
    internal class CH47FUploadAgent : UploadAgentBase, IUploadAgent
    {
        private readonly CH47FConfiguration _cfg;
        private readonly CH47FDeviceManager _dcsCmds;

        public CH47FUploadAgent(CH47FConfiguration cfg) => (_cfg, _dcsCmds) = (cfg, new CH47FDeviceManager());

        public static List<CniStep> PlanAll(CH47FConfiguration cfg)
        {
            List<CniWaypoint> wypts = cfg.WYPT.ToCniWaypoints();
            List<CniStep> steps = [ ];
            steps.AddRange(CH47FCdu.PlanWaypoints(wypts));
            steps.AddRange(CH47FCdu.PlanRoute(wypts, cfg.RTE.ToSpec()));
            steps.AddRange(CH47FCdu.PlanRadio(CH47FRadio.UHF1, cfg.Radio.ToSpec(RadioSystem.Radios.UHF1)));
            steps.AddRange(CH47FCdu.PlanRadio(CH47FRadio.VHF1, cfg.Radio.ToSpec(RadioSystem.Radios.VHF1)));
            return steps;
        }

        public override void BuildSystems(StringBuilder sb)
        {
            new CH47FCduBuilder(_dcsCmds, sb, PlanAll(_cfg)).Build();
        }
    }
}
