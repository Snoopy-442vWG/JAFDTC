// ********************************************************************************************************************
//
// C130JCniTests.cs : tests for the C-130J CNI-MU key sequence planner
//
// Copyright(C) 2026 442 vWG
//
// GPL-3; see LICENSE.
//
// ********************************************************************************************************************

using JAFDTC.Airlift.C130J;
using JAFDTC.Airlift.Cni;
using System.Globalization;

namespace JAFDTC.Airlift.Tests
{
    /// <summary>
    /// a literal transcription of the DCS-DTC C-130 uploader (Waypoints.cs, Radios.cs at ae9637d) emitting key names.
    /// the planner must produce the same presses for inputs DCS-DTC handles cleanly.
    /// </summary>
    internal static class DcsDtcReference
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

        public static List<string> Waypoints(List<CniWaypoint> wpts)
        {
            List<string> o = [ "CLR", "INDEX", "NEXT", "LSK_R2" ];
            foreach (var w in wpts)
            {
                var nm = w.Name.Replace(" ", "");
                if (nm.Length > 6) nm = nm[..6];
                Str(o, nm); o.Add("LSK_L1");
                Str(o, C130JCni.Mgrs8(w.Lat, w.Lon)); o.Add("LSK_L2");
                Str(o, w.ElevationFt.ToString(CultureInfo.InvariantCulture)); o.Add("LSK_L3");
                o.Add("LSK_R6");
            }
            return o;
        }

        private static string RouteId(CniWaypoint w)
        {
            var name = string.IsNullOrWhiteSpace(w.Name) ? w.Number.ToString() : w.Name;
            return (name.Length > 6) ? name[..6] : name;
        }

        public static List<string> Route(List<CniWaypoint> ordered)
        {
            List<string> o = [ ];
            if (ordered.Count == 0) return o;
            o.AddRange([ "MARK", "LSK_L1", "LEGS", "LSK_R6", "LSK_L1" ]);
            Str(o, RouteId(ordered[^1]));
            o.Add("LSK_R1");
            o.Add("NEXT");
            var slot = 1;
            foreach (var w in ordered)
            {
                Str(o, RouteId(w));
                o.Add("LSK_R" + slot);
                slot++;
                if (slot > 4) { o.Add("NEXT"); slot = 1; }
            }
            o.Add("LSK_L6");
            o.Add("LSK_R6");
            return o;
        }

        public static List<string> Radio(int type, bool freqMode, string selFreq, string selPreset,
                                         List<(int Number, string Name, string Freq)> presets)
        {
            List<string> o = [ "CLR", "COMM_TUNE", (type == 1) ? "LSK_L1" : "LSK_L3", "W200", "FIRSTPAGE" ];
            if (freqMode && !string.IsNullOrEmpty(selFreq)) { Str(o, selFreq); o.Add("LSK_L1"); }
            if (presets != null && presets.Count > 0)
            {
                var inPage = 1;
                foreach (var p in presets.OrderBy(x => x.Number))
                {
                    var pageNo = Math.Ceiling((decimal)p.Number / 4);
                    while (inPage < pageNo) { o.Add("NEXT"); inPage++; }
                    if (p.Name != null && p.Name.Length > 0)
                    {
                        var name = p.Name;
                        if (name.Length > 5) name = name[..5];
                        if (name.Length < 5) name = name.PadRight(5, '0');
                        Str(o, name);
                    }
                    o.Add("/");
                    Str(o, p.Freq);
                    var btn = p.Number - (int)Math.Round((pageNo - 1) * 4, 0);
                    o.Add("LSK_L" + (btn + 1));
                }
                if (!freqMode && selPreset != null) { Str(o, selPreset); o.Add("LSK_L1"); }
            }
            return o;
        }
    }

    [TestClass]
    public class C130JCniTests
    {
        private static List<string> Keys(IEnumerable<CniStep> steps)
            => [.. steps.Where(s => s.Kind != CniStepKind.Note).Select(s => s.ToString()) ];

        private static List<CniWaypoint> SampleRoute(int count)
        {
            // points around ramstein ab (germany, utm 32U).
            List<CniWaypoint> list = [ ];
            for (int i = 1; i <= count; i++)
                list.Add(new CniWaypoint(i, $"RAM{i:D2}", 49.4369 + (0.05 * i), 7.6003 + (0.03 * i), 700 + (10 * i)));
            return list;
        }

        // ---- waypoints

        [TestMethod]
        public void Waypoints_MatchDcsDtc()
        {
            List<CniWaypoint> wpts = SampleRoute(7);
            CollectionAssert.AreEqual(DcsDtcReference.Waypoints(wpts), Keys(C130JCni.PlanWaypoints(wpts)));
        }

        [TestMethod]
        public void Waypoints_EmptyListPlansNothing()
        {
            Assert.AreEqual(0, C130JCni.PlanWaypoints([ ]).Count);
            Assert.AreEqual(0, C130JCni.PlanWaypoints(null).Count);
        }

        [TestMethod]
        public void Waypoints_FirstPointSequence()
        {
            List<CniWaypoint> wpts = [ new(1, "Ramstein", 49.4369, 7.6003, 782) ];
            List<string> keys = Keys(C130JCni.PlanWaypoints(wpts));
            string mgrs = C130JCni.Mgrs8(49.4369, 7.6003);
            Assert.AreEqual(13, mgrs.Length, mgrs);
            Assert.IsTrue(mgrs.StartsWith("32U"), mgrs);
            List<string> expected = [ "CLR", "INDEX", "NEXT", "LSK_R2", "R", "A", "M", "S", "T", "E", "LSK_L1" ];
            expected.AddRange(mgrs.Select(c => c.ToString()));
            expected.AddRange([ "LSK_L2", "7", "8", "2", "LSK_L3", "LSK_R6" ]);
            CollectionAssert.AreEqual(expected, keys);
        }

        [TestMethod]
        public void Waypoints_NegativeElevationEntersZero()
        {
            List<string> keys = Keys(C130JCni.PlanWaypoints([ new(1, "DEAD", 31.5, 35.4, -1300) ]));
            int l2 = keys.IndexOf("LSK_L2");
            CollectionAssert.AreEqual(new List<string> { "0", "LSK_L3" }, keys.GetRange(l2 + 1, 2));
        }

        // ---- identifiers

        [TestMethod]
        public void Ident_SanitizesAndTruncates()
        {
            Assert.AreEqual("LZEAGL", C130JCni.Ident("lz eagle", 3));
            Assert.AreEqual("IP1", C130JCni.Ident("IP-1", 3));
            Assert.AreEqual("WP12", C130JCni.Ident("  --  ", 12));
            Assert.AreEqual("WP7", C130JCni.Ident(null, 7));
        }

        [TestMethod]
        public void Idents_AreUnique()
        {
            List<CniWaypoint> wpts =
            [
                new(1, "OBJECTIVE A", 50, 8, 0),
                new(2, "OBJECTIVE B", 50, 8, 0),
                new(3, "OBJECTIVE C", 50, 8, 0),
                new(4, "WP2", 50, 8, 0),
                new(5, "", 50, 8, 0),
            ];
            Dictionary<int, string> ids = C130JCni.Idents(wpts);
            Assert.AreEqual("OBJECT", ids[1]);
            Assert.AreEqual("OBJEC2", ids[2]);
            Assert.AreEqual("OBJEC3", ids[3]);
            Assert.AreEqual("WP2", ids[4]);
            Assert.AreEqual("WP5", ids[5]);
            Assert.AreEqual(ids.Count, ids.Values.Distinct().Count());
            Assert.IsTrue(ids.Values.All(v => v.Length <= C130JCni.MaxIdentLength));
        }

        [TestMethod]
        public void Route_UsesSameIdentAsWaypointWhenNameHasSpaces()
        {
            // dcs-dtc truncates before removing spaces on the route side ("LZ EAGLE" -> "LZEAG") but after on the
            // waypoint side ("LZEAGL"), so its route could name a point that does not exist. we use one identifier.
            List<CniWaypoint> wpts = [ new(1, "LZ EAGLE", 50, 8, 0), new(2, "LZ HAWK", 50.1, 8.1, 0) ];
            List<string> wp = Keys(C130JCni.PlanWaypoints(wpts));
            List<string> rt = Keys(C130JCni.PlanRoute(wpts, new CniRouteSpec(CniRouteMode.All)));
            string Joined(List<string> k) => string.Concat(k.Where(x => x.Length == 1));
            StringAssert.Contains(Joined(wp), "LZEAGL");
            StringAssert.Contains(Joined(rt), "LZEAGL");
        }

        // ---- route

        [TestMethod]
        public void Route_All_MatchesDcsDtc()
        {
            foreach (int n in new[] { 1, 3, 4, 5, 8, 9, 13 })
            {
                List<CniWaypoint> wpts = SampleRoute(n);
                CollectionAssert.AreEqual(DcsDtcReference.Route(wpts),
                                          Keys(C130JCni.PlanRoute(wpts, new CniRouteSpec(CniRouteMode.All))), $"n={n}");
            }
        }

        [TestMethod]
        public void Route_Range_MatchesDcsDtc()
        {
            List<CniWaypoint> wpts = SampleRoute(10);
            List<CniWaypoint> expectedSet = [.. wpts.Where(w => w.Number >= 3 && w.Number <= 8) ];
            CollectionAssert.AreEqual(DcsDtcReference.Route(expectedSet),
                                      Keys(C130JCni.PlanRoute(wpts, new CniRouteSpec(CniRouteMode.Range, 8, 3))));
        }

        [TestMethod]
        public void Route_List_KeepsOrderAndSkipsMissing()
        {
            List<CniWaypoint> wpts = SampleRoute(6);
            CniRouteSpec spec = new(CniRouteMode.List, List: CniRouteSpec.ParseList("5, 2,99,1"));
            List<CniWaypoint> route = C130JCni.RouteWaypoints(wpts, spec);
            CollectionAssert.AreEqual(new[] { 5, 2, 1 }, route.Select(w => w.Number).ToArray());
            CollectionAssert.AreEqual(DcsDtcReference.Route(route), Keys(C130JCni.PlanRoute(wpts, spec)));
        }

        [TestMethod]
        public void Route_NoneOrEmptyPlansNothing()
        {
            Assert.AreEqual(0, C130JCni.PlanRoute(SampleRoute(4), CniRouteSpec.NoRoute).Count);
            Assert.AreEqual(0, C130JCni.PlanRoute([ ], new CniRouteSpec(CniRouteMode.All)).Count);
            Assert.AreEqual(0, C130JCni.PlanRoute(SampleRoute(4), new CniRouteSpec(CniRouteMode.Range, 7, 9)).Count);
        }

        [TestMethod]
        public void Route_PagesEveryFourRows()
        {
            List<string> keys = Keys(C130JCni.PlanRoute(SampleRoute(5), new CniRouteSpec(CniRouteMode.All)));
            // header NEXT + one page break after the 4th row.
            Assert.AreEqual(2, keys.Count(k => k == "NEXT"));
            int lastR4 = keys.LastIndexOf("LSK_R4");
            Assert.AreEqual("NEXT", keys[lastR4 + 1]);
        }

        [TestMethod]
        public void RouteList_Parse()
        {
            CollectionAssert.AreEqual(new[] { 1, 3, 5, 6, 7, 8 }, CniRouteSpec.ParseList("1,3,5-8").ToArray());
            CollectionAssert.AreEqual(new[] { 6, 5, 4 }, CniRouteSpec.ParseList(" 6 - 4 ").ToArray());
            Assert.IsTrue(CniRouteSpec.IsValidList("2"));
            Assert.IsFalse(CniRouteSpec.IsValidList("1,,2"));
            Assert.IsFalse(CniRouteSpec.IsValidList("a,b"));
            Assert.IsFalse(CniRouteSpec.IsValidList(""));
            Assert.AreEqual(0, CniRouteSpec.ParseList("x").Count);
        }

        // ---- radios

        private static readonly List<(int, string, string)> Presets =
        [
            (1, "TOWER", "257.800"),
            (2, "GND", "275.800"),
            (5, "DEPAR", "251.000"),
            (8, "MAGIC", "263.000"),
            (9, "ARCO", "225.450"),
            (20, "TXCO", "295.400"),
        ];

        private static CniRadioSpec Spec(string selection)
            => new([.. Presets.Select(p => new CniPreset(p.Item1, p.Item2, p.Item3)) ], selection);

        [TestMethod]
        public void Radio_FrequencyMode_MatchesDcsDtc()
        {
            CollectionAssert.AreEqual(DcsDtcReference.Radio(1, true, "251.000", null, Presets),
                                      Keys(C130JCni.PlanRadio(C130JRadio.UHF1, Spec("251.000"))));
        }

        [TestMethod]
        public void Radio_PresetMode_MatchesDcsDtc()
        {
            CollectionAssert.AreEqual(DcsDtcReference.Radio(2, false, null, "5", Presets),
                                      Keys(C130JCni.PlanRadio(C130JRadio.VHF1, Spec("5"))));
        }

        [TestMethod]
        public void Radio_PresetRowsAndPages()
        {
            List<string> keys = Keys(C130JCni.PlanRadio(C130JRadio.UHF1, Spec(null)));
            // preset 5 is page 2 row 1 -> L2; preset 8 page 2 row 4 -> L5; preset 20 page 5 row 4 -> L5.
            string flat = string.Join(" ", keys);
            StringAssert.Contains(flat, "NEXT D E P A R / 2 5 1 . 0 0 0 LSK_L2");
            StringAssert.Contains(flat, "M A G I C / 2 6 3 . 0 0 0 LSK_L5");
            StringAssert.Contains(flat, "NEXT A R C O 0 / 2 2 5 . 4 5 0 LSK_L2");
            StringAssert.Contains(flat, "LSK_L2 NEXT NEXT T X C O 0 / 2 9 5 . 4 0 0 LSK_L5");
            Assert.IsFalse(keys.Contains("PREV"));
        }

        [TestMethod]
        public void Radio_SelectionOnly()
        {
            List<string> keys = Keys(C130JCni.PlanRadio(C130JRadio.VHF1, new CniRadioSpec([ ], "121.5")));
            CollectionAssert.AreEqual(new List<string>
            {
                "CLR", "COMM_TUNE", "LSK_L3", "W200", "FIRSTPAGE", "1", "2", "1", ".", "5", "0", "0", "LSK_L1"
            }, keys);
        }

        [TestMethod]
        public void Radio_EmptySpecPlansNothing()
        {
            Assert.AreEqual(0, C130JCni.PlanRadio(C130JRadio.UHF1, new CniRadioSpec([ ], null)).Count);
            Assert.AreEqual(0, C130JCni.PlanRadio(C130JRadio.UHF1, null).Count);
        }

        [TestMethod]
        public void Radio_SelectionKind()
        {
            Assert.IsTrue(new CniRadioSpec([ ], "3").IsPresetSelection);
            Assert.IsTrue(new CniRadioSpec([ ], "20").IsPresetSelection);
            Assert.IsTrue(new CniRadioSpec([ ], "251").IsFrequencySelection);
            Assert.IsTrue(new CniRadioSpec([ ], "251.000").IsFrequencySelection);
            Assert.IsFalse(new CniRadioSpec([ ], "").IsFrequencySelection);
        }

        [TestMethod]
        public void Frequency_Validation()
        {
            Assert.IsTrue(C130JCni.IsFrequencyValid(C130JRadio.UHF1, "251.000"));
            Assert.IsTrue(C130JCni.IsFrequencyValid(C130JRadio.UHF1, "225.450"));
            Assert.IsFalse(C130JCni.IsFrequencyValid(C130JRadio.UHF1, "121.500"));
            Assert.IsFalse(C130JCni.IsFrequencyValid(C130JRadio.UHF1, "251.010"));
            Assert.IsTrue(C130JCni.IsFrequencyValid(C130JRadio.VHF1, "121.5"));
            Assert.IsTrue(C130JCni.IsFrequencyValid(C130JRadio.VHF1, "173.975"));
            Assert.IsFalse(C130JCni.IsFrequencyValid(C130JRadio.VHF1, "251.000"));
            Assert.IsFalse(C130JCni.IsFrequencyValid(C130JRadio.VHF1, "abc"));
        }

        [TestMethod]
        public void PresetName_PadsAndSanitizes()
        {
            Assert.AreEqual("ARCO0", C130JCni.PresetName("arco"));
            Assert.AreEqual("TEXAC", C130JCni.PresetName("TEXACO 2"));
            Assert.AreEqual("AB120", C130JCni.PresetName("A-B 12"));
            Assert.AreEqual("", C130JCni.PresetName(" - "));
        }
    }
}
