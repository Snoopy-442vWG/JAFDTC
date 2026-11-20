// ********************************************************************************************************************
//
// CH47FCduTests.cs : tests for the CH-47F CDU key sequence planner
//
// Copyright(C) 2026 442 vWG
//
// GPL-3; see LICENSE.
//
// ********************************************************************************************************************

using JAFDTC.Airlift.C130J;
using JAFDTC.Airlift.CH47F;
using JAFDTC.Airlift.Cni;
using System.Globalization;

namespace JAFDTC.Airlift.Tests
{
    /// <summary>
    /// literal transcription of the DCS-DTC CH-47F uploader (Waypoints.cs, Radios.cs at ae9637d) emitting key names.
    /// positions use the DCS-DTC stored DDM text ("N 49°26.214’"), built here with the planner's Ddm().
    /// </summary>
    internal static class DcsDtcCh47Reference
    {
        private static void Str(List<string> o, string s)
        {
            foreach (char c in s.ToUpper())
            {
                if (c == ' ') continue;
                if (c == '.') { o.Add("."); continue; }
                if ((c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '/') o.Add(c.ToString());
            }
        }

        private static string Stored(double v, bool isLat)
        {
            string d = CH47FCdu.Ddm(v, isLat);                                  // "N4926.214"
            int degLen = isLat ? 2 : 3;
            return $"{d[0]} {d.Substring(1, degLen)}°{d.Substring(1 + degLen)}’";  // "N 49°26.214’"
        }

        private static string Cmd(CniWaypoint w)
        {
            string Clean(string s) => s.Replace(".", "").Replace("'", "").Replace("\"", "").Replace("°", "")
                                       .Replace(" ", "").Replace("’", "");
            var x = Clean(Stored(w.Lat, true));
            var y = Clean(Stored(w.Lon, false));
            return x[..5] + "." + x[^3..] + y[..6] + "." + y[^3..];
        }

        public static List<string> Acp(List<CniWaypoint> wpts)
        {
            List<string> o = [ "CLR", "IDX", "W75", "LSK_L6", "LSK_L6" ];
            foreach (var w in wpts)
            {
                Str(o, w.Number.ToString(CultureInfo.InvariantCulture)); o.Add("LSK_L1");
                Str(o, Cmd(w)); o.Add("LSK_L1");
                Str(o, w.ElevationFt.ToString(CultureInfo.InvariantCulture)); o.Add("LSK_L3");
                if (w.Name != "") { o.Add("/"); Str(o, w.Name); o.Add("LSK_L2"); }
            }
            return o;
        }

        public static List<string> Fpln(List<CniWaypoint> wpts)
        {
            List<string> o = [ "CLR", "FPLN", "LSK_L6", "LSK_R1", "LSK_R1", "CLR" ];
            var submitted = 0;
            foreach (var w in wpts)
            {
                var slot = 5;
                if (submitted < 4) slot = 2 + submitted; else o.Add("DOWN");
                Str(o, Cmd(w));
                o.Add("LSK_L" + slot);
                submitted++;
            }
            return o;
        }

        public static List<string> Radio(int type, bool freqMode, string selFreq, string selPreset,
                                         List<(int Number, string Name, string Freq)> presets)
        {
            List<string> o = [ "CLR", "MSN" ];
            if (type == 1 && freqMode && !string.IsNullOrEmpty(selFreq)) { Str(o, selFreq); o.Add("LSK_R2"); }
            o.Add("CLR");
            if (type == 2 && freqMode) o.Add("ARC186_MODE_MANUAL");
            if (type == 2 && !freqMode) o.Add("ARC186_MODE_PRESET");
            if (type == 2 && freqMode && !string.IsNullOrEmpty(selFreq)) o.Add($"SetV3({selFreq})");
            var inPage = 1;
            if (presets != null && presets.Count > 0)
            {
                o.Add(type == 1 ? "LSK_L2" : "LSK_L3");
                o.Add("LSK_L1");
                foreach (var p in presets.OrderBy(x => x.Number))
                {
                    var pageNo = Math.Ceiling((decimal)p.Number / 5);
                    var btn = p.Number - (int)Math.Round((pageNo - 1) * 5, 0);
                    while (inPage < pageNo) { o.Add("DOWN"); inPage++; }
                    if (!string.IsNullOrEmpty(p.Name)) { Str(o, p.Name.Length > 8 ? p.Name[..8] : p.Name); o.Add("LSK_L" + btn); }
                    Str(o, p.Freq);
                    o.Add("LSK_R" + btn);
                }
            }
            if (!freqMode && selPreset != null)
            {
                var presetNo = int.Parse(selPreset);
                var pageNo = Math.Ceiling((decimal)presetNo / 5);
                for (var i = 1; i <= 2; i++)
                {
                    while (inPage < pageNo) { o.Add("DOWN"); inPage++; }
                    while (inPage > pageNo) { o.Add("UP"); inPage--; }
                    var btn = presetNo - (int)Math.Round((pageNo - 1) * 5, 0);
                    o.Add("LSK_L" + btn);
                    inPage = 1;
                }
            }
            return o;
        }
    }

    [TestClass]
    public class CH47FCduTests
    {
        private static List<string> Keys(IEnumerable<CniStep> steps)
            => [.. steps.Where(s => s.Kind != CniStepKind.Note).Select(s => s.ToString()) ];

        private static List<CniWaypoint> Sample(int count)
        {
            List<CniWaypoint> list = [ ];
            for (int i = 1; i <= count; i++)
                list.Add(new CniWaypoint(i, $"LZ{i}", 49.4369 + (0.05 * i), 7.6003 + (0.03 * i), 700 + (10 * i)));
            return list;
        }

        [TestMethod]
        public void Ddm_Format()
        {
            Assert.AreEqual("N4926.214", CH47FCdu.Ddm(49.4369, true));
            Assert.AreEqual("E00736.018", CH47FCdu.Ddm(7.6003, false));
            Assert.AreEqual("S3352.000", CH47FCdu.Ddm(-33.866666667, true));
            Assert.AreEqual("W11815.000", CH47FCdu.Ddm(-118.25, false));
            // rounding that reaches 60.000' carries into the degrees.
            Assert.AreEqual("N5000.000", CH47FCdu.Ddm(49.9999999, true));
            Assert.AreEqual("N4926.214E00736.018", CH47FCdu.Position(49.4369, 7.6003));
        }

        [TestMethod]
        public void Waypoints_MatchDcsDtc()
        {
            List<CniWaypoint> w = Sample(6);
            CollectionAssert.AreEqual(DcsDtcCh47Reference.Acp(w), Keys(CH47FCdu.PlanWaypoints(w)));
        }

        [TestMethod]
        public void Waypoints_UnnamedSkipsName()
        {
            List<string> keys = Keys(CH47FCdu.PlanWaypoints([ new(3, " - ", 50, 8, 100) ]));
            Assert.IsFalse(keys.Contains("/"));
            Assert.IsFalse(keys.Contains("LSK_L2"));
            Assert.AreEqual("LSK_L3", keys[^1]);
        }

        [TestMethod]
        public void Waypoints_EmptyPlansNothing() => Assert.AreEqual(0, CH47FCdu.PlanWaypoints([ ]).Count);

        [TestMethod]
        public void Route_MatchesDcsDtc()
        {
            foreach (int n in new[] { 1, 4, 5, 9 })
            {
                List<CniWaypoint> w = Sample(n);
                CollectionAssert.AreEqual(DcsDtcCh47Reference.Fpln(w),
                                          Keys(CH47FCdu.PlanRoute(w, new CniRouteSpec(CniRouteMode.All))), $"n={n}");
            }
            List<CniWaypoint> all = Sample(8);
            CniRouteSpec list = new(CniRouteMode.List, List: CniRouteSpec.ParseList("6,2,4"));
            CollectionAssert.AreEqual(DcsDtcCh47Reference.Fpln(C130JCni.RouteWaypoints(all, list)),
                                      Keys(CH47FCdu.PlanRoute(all, list)));
            Assert.AreEqual(0, CH47FCdu.PlanRoute(all, CniRouteSpec.NoRoute).Count);
        }

        private static readonly List<(int, string, string)> Presets =
        [
            (1, "TOWER", "257.800"),
            (3, "GROUND", "275.800"),
            (6, "APPROACH", "251.000"),
            (11, "MAGIC", "263.000"),
            (20, "TEXACO2", "295.400"),
        ];

        private static CniRadioSpec Spec(string sel, List<(int, string, string)> p = null)
            => new([.. (p ?? Presets).Select(x => new CniPreset(x.Item1, x.Item2, x.Item3)) ], sel);

        [TestMethod]
        public void Radio_Uhf_Frequency_MatchesDcsDtc()
            => CollectionAssert.AreEqual(DcsDtcCh47Reference.Radio(1, true, "251.000", null, Presets),
                                         Keys(CH47FCdu.PlanRadio(CH47FRadio.UHF1, Spec("251.000"))));

        [TestMethod]
        public void Radio_Uhf_Preset_MatchesDcsDtc()
            => CollectionAssert.AreEqual(DcsDtcCh47Reference.Radio(1, false, null, "11", Presets),
                                         Keys(CH47FCdu.PlanRadio(CH47FRadio.UHF1, Spec("11"))));

        private static readonly List<(int, string, string)> VhfPresets =
        [
            (1, "TWR", "121.500"),
            (7, "FM1", "41.250"),
        ];

        [TestMethod]
        public void Radio_Vhf_Frequency_MatchesDcsDtc()
        {
            List<string> keys = Keys(CH47FCdu.PlanRadio(CH47FRadio.VHF1, Spec("124.775", VhfPresets)));
            CollectionAssert.AreEqual(DcsDtcCh47Reference.Radio(2, true, "124.775", null, VhfPresets), keys);
            Assert.IsTrue(keys.Contains("SetV3(124.775)"));
        }

        [TestMethod]
        public void Radio_Vhf_Preset_MatchesDcsDtc()
            => CollectionAssert.AreEqual(DcsDtcCh47Reference.Radio(2, false, null, "7", VhfPresets),
                                         Keys(CH47FCdu.PlanRadio(CH47FRadio.VHF1, Spec("7", VhfPresets))));

        [TestMethod]
        public void Radio_PresetSelectionWithoutPresetsOpensPresetPage()
        {
            // dcs-dtc would press L3 on the MSN page here; we open the preset page first.
            List<string> keys = Keys(CH47FCdu.PlanRadio(CH47FRadio.UHF1, new CniRadioSpec([ ], "2")));
            CollectionAssert.AreEqual(new List<string> { "CLR", "MSN", "CLR", "LSK_L2", "LSK_L1", "LSK_L2", "LSK_L2" }, keys);
        }

        [TestMethod]
        public void Radio_PresetNameTruncatedToEight()
        {
            Assert.AreEqual("APPROACH", CH47FCdu.PresetName("APPROACH 1"));
            Assert.AreEqual("TEXACO2", CH47FCdu.PresetName("texaco-2"));
        }

        [TestMethod]
        public void Radio_EmptyPlansNothing() => Assert.AreEqual(0, CH47FCdu.PlanRadio(CH47FRadio.VHF1, new CniRadioSpec([ ], "")).Count);

        [TestMethod]
        public void Frequency_Validation()
        {
            Assert.IsTrue(CH47FCdu.IsFrequencyValid(CH47FRadio.UHF1, "251.000"));
            Assert.IsFalse(CH47FCdu.IsFrequencyValid(CH47FRadio.UHF1, "151.000"));
            Assert.IsTrue(CH47FCdu.IsFrequencyValid(CH47FRadio.VHF1, "30.000"));
            Assert.IsTrue(CH47FCdu.IsFrequencyValid(CH47FRadio.VHF1, "151.975"));
            Assert.IsFalse(CH47FCdu.IsFrequencyValid(CH47FRadio.VHF1, "160.000"));
            Assert.IsFalse(CH47FCdu.IsFrequencyValid(CH47FRadio.VHF1, "124.010"));
        }
    }
}
