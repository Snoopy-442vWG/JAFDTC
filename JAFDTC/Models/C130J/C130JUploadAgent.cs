// ********************************************************************************************************************
//
// C130JUploadAgent.cs -- c-130j upload agent and CNI-MU command builder
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

using JAFDTC.Airlift.C130J;
using JAFDTC.Airlift.Cni;
using JAFDTC.Models.C130J.Radio;
using JAFDTC.Models.DCS;
using System.Collections.Generic;
using System.Text;

namespace JAFDTC.Models.C130J
{
    /// <summary>
    /// turns planned CNI steps (JAFDTC.Airlift) into the JAFDTC cockpit command stream.
    /// </summary>
    internal class C130JCniBuilder(C130JDeviceManager dcsCmds, StringBuilder sb, IReadOnlyList<CniStep> steps)
        : BuilderBase(dcsCmds, sb), IBuilder
    {
        // longest the CNI could need to page back to COMM TUNE page 1 (10 pages, with margin).
        private const int FirstPageMaxPresses = 12;

        public override void Build(Dictionary<string, object> state = null)
        {
            if ((steps == null) || (steps.Count == 0))
                return;

            AirframeDevice cni = _aircraft.GetDevice("CNI");
            foreach (CniStep step in steps)
            {
                switch (step.Kind)
                {
                    case CniStepKind.Key:
                        AddAction(cni, step.Key);
                        break;
                    case CniStepKind.Wait:
                        AddWait(step.Value);
                        break;
                    case CniStepKind.FirstPage:
                        AddWhileBlock("IsCommPageNot1", true, null, delegate ()
                        {
                            AddAction(cni, CniKeys.PREV);
                            AddWait(WAIT_SHORT);
                        }, FirstPageMaxPresses);
                        break;
                    case CniStepKind.Note:
                        AddExecFunction("NOP", [ $"==== {step.Key}" ]);
                        break;
                }
            }
        }
    }

    /// <summary>
    /// c-130j upload agent: waypoints into the CNI database, the route on LEGS, then UHF-1 and VHF-1 presets. the
    /// order matches DCS-DTC.
    /// </summary>
    internal class C130JUploadAgent : UploadAgentBase, IUploadAgent
    {
        private readonly C130JConfiguration _cfg;
        private readonly C130JDeviceManager _dcsCmds;

        public C130JUploadAgent(C130JConfiguration cfg) => (_cfg, _dcsCmds) = (cfg, new C130JDeviceManager());

        /// <summary>
        /// every planned step for the configuration, in upload order (also used by the command log / tests).
        /// </summary>
        public static List<CniStep> PlanAll(C130JConfiguration cfg)
        {
            List<CniWaypoint> wypts = cfg.WYPT.ToCniWaypoints();
            List<CniStep> steps = [ ];
            steps.AddRange(C130JCni.PlanWaypoints(wypts));
            steps.AddRange(C130JCni.PlanRoute(wypts, cfg.RTE.ToSpec()));
            steps.AddRange(C130JCni.PlanRadio(C130JRadio.UHF1, cfg.Radio.ToSpec(RadioSystem.Radios.UHF1)));
            steps.AddRange(C130JCni.PlanRadio(C130JRadio.VHF1, cfg.Radio.ToSpec(RadioSystem.Radios.VHF1)));
            return steps;
        }

        public override void BuildSystems(StringBuilder sb)
        {
            new C130JCniBuilder(_dcsCmds, sb, PlanAll(_cfg)).Build();
        }
    }
}
