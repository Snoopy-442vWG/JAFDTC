// ********************************************************************************************************************
//
// DtcTests.cs : AH-64D .dtc import / merge tests against .dtc files saved by the dcs dtc manager
//
// ********************************************************************************************************************

using JAFDTC.AH64D.Dtc;
using JAFDTC.AH64D.Geo;
using System.Text.Json.Nodes;

namespace JAFDTC.AH64D.Tests
{
    [TestClass]
    public class DtcTests
    {
        private static string Data(string name) => Path.Combine(AppContext.BaseDirectory, "TestData", name);

        private static JsonNode LoadData(string name) => Ah64Dtc.ParseFile(File.ReadAllText(Data(name)))["data"];

        // files saved by dcs itself (padded dcs json formatting) - these define what "correct" looks like.
        public static IEnumerable<object[]> DcsSavedFiles =>
        [
            ["tti_hannover.dtc"],
            ["orontes_gate.dtc"],
            ["iqt4_day_dcs.dtc"],
        ];

        [TestMethod]
        public void PointIdents_MatchDcsDataTypes()
        {
            // spot checks against Point_ID in Mods/aircraft/AH-64D/Cockpit/Scripts/Computers/DTU/DataTypes.lua
            Assert.AreEqual(6, PointIdents.Find(PointCategory.WPTHZ, "WP").DtcId);
            Assert.AreEqual(1, PointIdents.Find(PointCategory.WPTHZ, "LZ").DtcId);
            Assert.AreEqual(61, PointIdents.Find(PointCategory.CTRLM, "HA").DtcId);
            Assert.AreEqual(63, PointIdents.Find(PointCategory.CTRLM, "AI").DtcId);
            Assert.AreEqual(1, PointIdents.Find(PointCategory.TGT, "TG").DtcId);       // getTTID(80) = 1
            Assert.AreEqual(8, PointIdents.Find(PointCategory.TGT, "SA6").DtcId);      // 87 - 79
            Assert.AreEqual(8, PointIdents.Find(PointCategory.TGT, "6").DtcId);
            Assert.AreEqual(61, PointIdents.Find(PointCategory.TGT, "AD").DtcId);      // 140 - 79
            Assert.AreEqual(57, PointIdents.Find(PointCategory.CTRLM, "AA").PointId);  // same code, other category
            Assert.AreEqual(105, PointIdents.Find(PointCategory.TGT, "AA").PointId);
            Assert.IsNull(PointIdents.Find(PointCategory.WPTHZ, "AI"));
            Assert.AreEqual(10, PointIdents.ForCategory(PointCategory.WPTHZ).Count);
            Assert.AreEqual(69, PointIdents.ForCategory(PointCategory.CTRLM).Count);
            Assert.AreEqual(61, PointIdents.ForCategory(PointCategory.TGT).Count);
            foreach (PointIdent i in PointIdents.All)
                Assert.AreSame(i, PointIdents.FromDtcId(i.Category, i.DtcId), i.Code);
        }

        [TestMethod]
        public void Labels_ParseAndFormat()
        {
            Assert.IsTrue(PointIdents.TryParseLabel("W01", out PointCategory c, out int n));
            Assert.AreEqual(PointCategory.WPTHZ, c);
            Assert.AreEqual(1, n);
            Assert.IsTrue(PointIdents.TryParseLabel("c51", out c, out n));
            Assert.AreEqual(PointCategory.CTRLM, c);
            Assert.IsTrue(PointIdents.TryParseLabel("T7", out c, out n));
            Assert.AreEqual("T07", PointIdents.Label(c, n));
            Assert.IsFalse(PointIdents.TryParseLabel("C50", out _, out _));
            Assert.IsFalse(PointIdents.TryParseLabel("W51", out _, out _));
            Assert.IsFalse(PointIdents.TryParseLabel("X01", out _, out _));
        }

        [TestMethod]
        public void Read_TtiHannover()
        {
            JsonNode data = LoadData("tti_hannover.dtc");
            Ah64Mission m = Ah64Dtc.ReadMission(data, 1);
            Assert.AreEqual("GermanyCW", m.Theater);
            Assert.AreEqual(3, m.PointsIn(PointCategory.WPTHZ).Count());
            Assert.AreEqual(10, m.PointsIn(PointCategory.CTRLM).Count());
            Ah64Point c51 = m.FindPoint(PointCategory.CTRLM, 51);
            Assert.AreEqual("HA", c51.Ident);
            Assert.AreEqual("C51", c51.Free);
            Assert.AreEqual("HOLD N", c51.Note);
            Assert.AreEqual(53.0 / 0.3048, c51.AltFt, 1e-9);
            Ah64Point w01 = m.FindPoint(PointCategory.WPTHZ, 1);
            Assert.AreEqual("HOME", w01.Note);
            // hannover is ~52.46N 9.69E
            Assert.AreEqual(52.46, w01.Lat, 0.05);
            Assert.AreEqual(9.68, w01.Lon, 0.05);
            Assert.AreEqual(5, m.Lines.Count);
            Assert.AreEqual(7, m.Shapes.Count(s => s.Kind == ShapeKind.Area));
            Assert.AreEqual(1, m.Shapes.Count(s => s.Kind == ShapeKind.PriorityFireZone));
            Assert.IsTrue(m.AdfEnabled);
            Assert.AreEqual("HAN", m.Adf[0].Id);
            Assert.AreEqual(1139.0, m.Adf[0].FreqKHz);
            Assert.AreEqual(10, m.Routes.Count);
            Assert.AreEqual("ECHO ", m.Routes[3].Name);

            List<Ah64CommPreset> comms = Ah64Dtc.ReadComms(data);
            Assert.AreEqual(10, comms.Count);
            Assert.AreEqual("PRESET 1", comms[0].UnitId);
            Assert.AreEqual(PrimaryRadio.VHF, comms[0].Primary);
            Assert.AreEqual(305.0, comms[0].UHF.Frequency);
        }

        /// <summary>
        /// import every mission + the comm presets from a dcs-saved file and merge them back into the same file:
        /// the NAV and COMM sections must come back the same (coordinates to 0.1 mm) and every other section must
        /// be untouched.
        /// </summary>
        [TestMethod]
        [DynamicData(nameof(DcsSavedFiles))]
        public void RoundTrip_DcsSavedFile(string file)
        {
            JsonNode original = LoadData(file);
            JsonNode merged = original.DeepClone();

            for (int i = 1; i <= 2; i++)
            {
                List<string> warnings = [];
                Ah64Mission m = Ah64Dtc.ReadMission(original, i, warnings);
                Assert.AreEqual(0, warnings.Count, string.Join("\n", warnings));
                merged = Ah64Dtc.MergeMission(merged, i, m);
            }
            merged = Ah64Dtc.MergeComms(merged, Ah64Dtc.ReadComms(original));

            for (int i = 1; i <= 2; i++)
                AssertJsonClose(original["NAV"][$"Mission_{i}"], merged["NAV"][$"Mission_{i}"], $"Mission_{i}");
            AssertJsonClose(original["Presets"]["COMM"], merged["Presets"]["COMM"], "COMM");

            foreach (string key in new[] { "IDM", "Laser", "MISC", "Radios", "Radios_HF", "Weapon", "type", "name" })
                Assert.IsTrue(JsonNode.DeepEquals(original[key], merged[key]), $"{key} changed");
            Assert.AreEqual(original["terrain"].GetValue<string>(), merged["terrain"].GetValue<string>());
            Assert.IsTrue(JsonNode.DeepEquals(original["Presets"]["MPS"], merged["Presets"]["MPS"]));
        }

        [TestMethod]
        public void Merge_GeneratedZoneCaptions_MatchDcsLayout()
        {
            // the PFZ in TTI Hannover was drawn in the dcs dtc manager; regenerating its captions from the vertices
            // must give dcs' own caption anchors.
            JsonNode data = LoadData("tti_hannover.dtc");
            Ah64Mission m = Ah64Dtc.ReadMission(data, 1);
            Ah64Shape pfz = m.Shapes.Single(s => s.Kind == ShapeKind.PriorityFireZone);
            JsonArray caps = Ah64Dtc.BuildCaptions(DcsTheaters.Get(m.Theater), pfz.Vertices);
            JsonArray dcsCaps = data["NAV"]["Mission_1"]["Zones"]["PFZ"][0]["caption_pos"].AsArray();
            Assert.AreEqual(dcsCaps.Count, caps.Count);
            for (int i = 0; i < caps.Count; i++)
            {
                Assert.AreEqual(dcsCaps[i]["x"].GetValue<double>(), caps[i]["x"].GetValue<double>(), 0.01, $"cap {i} x");
                Assert.AreEqual(dcsCaps[i]["y"].GetValue<double>(), caps[i]["y"].GetValue<double>(), 0.01, $"cap {i} y");
                Assert.AreEqual(dcsCaps[i]["txt_dx"].GetValue<double>(), caps[i]["txt_dx"].GetValue<double>(), 1e-9);
                Assert.AreEqual(dcsCaps[i]["txt_dy"].GetValue<double>(), caps[i]["txt_dy"].GetValue<double>(), 1e-9);
            }
        }

        [TestMethod]
        public void Merge_EditedShape_RegeneratesCaptions()
        {
            JsonNode data = LoadData("tti_hannover.dtc");
            Ah64Mission m = Ah64Dtc.ReadMission(data, 1);
            Ah64Shape area = m.Shapes.First(s => s.Kind == ShapeKind.Area);
            area.Vertices[0].Lat += 0.001;
            JsonNode merged = Ah64Dtc.MergeMission(data.DeepClone(), 1, m);
            JsonArray caps = merged["NAV"]["Mission_1"]["Areas"][0]["caption_pos"].AsArray();
            Assert.AreEqual(4, caps.Count);
            Assert.AreEqual(4.2, caps[0]["txt_dx"].GetValue<double>());
        }

        [TestMethod]
        public void Merge_RouteDistancesAndTimes()
        {
            Ah64Mission m = new() { Theater = "GermanyCW" };
            m.Points.Add(new Ah64Point() { Category = PointCategory.CTRLM, Number = 51, Ident = "AI", Free = "RMS",
                                           Lat = 49.436899, Lon = 7.600077, AltFt = 804 });
            m.Points.Add(new Ah64Point() { Category = PointCategory.WPTHZ, Number = 1, Ident = "SP", Free = "SP1",
                                           Lat = 49.4797, Lon = 7.5991, AltFt = 861 });
            m.Points.Add(new Ah64Point() { Category = PointCategory.TGT, Number = 1, Ident = "6", Free = "SA6",
                                           Lat = 49.6, Lon = 7.3, AltFt = 1400 });
            m.Routes[0].Name = "venus";
            m.Routes[0].IsEnabled = true;
            m.Routes[0].StartTimeSec = 13 * 3600;
            m.Routes[0].Points.Add(new Ah64RoutePoint() { Number = 51, SpeedKts = 80 });
            m.Routes[0].Points.Add(new Ah64RoutePoint() { Number = 1, SpeedKts = 80 });

            JsonNode data = LoadData("tti_hannover.dtc").DeepClone();
            data = Ah64Dtc.MergeMission(data, 2, m);
            JsonNode m2 = data["NAV"]["Mission_2"];

            JsonNode r0 = m2["Routes"][0];
            Assert.AreEqual("VENUS", r0["Name"].GetValue<string>());
            Assert.IsTrue(r0["isEnabled"].GetValue<bool>());
            r0 = JsonNode.Parse(r0.ToJsonString());
            Assert.AreEqual(46800.0, r0["POINTS"][0]["eta"].GetValue<double>());
            Assert.AreEqual(0.0, r0["POINTS"][0]["dist"].GetValue<double>());
            double dist = r0["POINTS"][1]["dist"].GetValue<double>();
            Assert.AreEqual(4813.9, dist, 5.0);    // the 442CWG IQT-4 card has 4813.885 m for this leg (W01 -> W02)
            Assert.AreEqual(dist / (80 * 1852.0 / 3600.0), r0["POINTS"][1]["eta"].GetValue<double>(), 0.01);
            Assert.IsFalse(m2["Routes"][1]["isEnabled"].GetValue<bool>());
            Assert.AreEqual("BRAVO", m2["Routes"][1]["Name"].GetValue<string>());

            JsonNode t01 = JsonNode.Parse(m2["Points"]["TGT"]["POINTS"][0].ToJsonString());
            Assert.AreEqual(8, t01["id"].GetValue<int>());
            Assert.AreEqual(1400 * 0.3048, t01["alt"].GetValue<double>(), 0.01);
            Assert.IsTrue(m2["Points"]["TGT"]["isEnabled"].GetValue<bool>());
            Assert.AreEqual("GermanyCW", data["terrain"].GetValue<string>());

            // mission 1 untouched
            Assert.IsTrue(JsonNode.DeepEquals(LoadData("tti_hannover.dtc")["NAV"]["Mission_1"], data["NAV"]["Mission_1"]));
        }

        [TestMethod]
        public void Validate_CatchesErrors()
        {
            Ah64Mission m = new() { Theater = "GermanyCW" };
            m.Points.Add(new Ah64Point() { Category = PointCategory.WPTHZ, Number = 1, Ident = "WP", Lat = 50, Lon = 8 });
            m.Points.Add(new Ah64Point() { Category = PointCategory.WPTHZ, Number = 1, Ident = "WP", Lat = 50, Lon = 8 });
            m.Points.Add(new Ah64Point() { Category = PointCategory.CTRLM, Number = 40, Ident = "AI", Lat = 50, Lon = 8 });
            m.Points.Add(new Ah64Point() { Category = PointCategory.TGT, Number = 2, Ident = "XX", Lat = 50, Lon = 8 });
            m.Points.Add(new Ah64Point() { Category = PointCategory.WPTHZ, Number = 3, Ident = "WP", Free = "TOOLONG",
                                           Lat = 50, Lon = 8 });
            m.Routes[0].Points.Add(new Ah64RoutePoint() { Number = 9 });
            m.Lines.Add(new Ah64Line() { Vertices = [new GeoPoint(50, 8)] });
            m.Adf[0].FreqKHz = 50;

            List<string> errs = Ah64Validator.ValidateMission(m);
            Assert.IsTrue(errs.Any(e => e.Contains("W01 is used by 2")), "duplicate");
            Assert.IsTrue(errs.Any(e => e.Contains("C40")), "c number range");
            Assert.IsTrue(errs.Any(e => e.Contains("IDENT \"XX\"")), "ident");
            Assert.IsTrue(errs.Any(e => e.Contains("free text")), "free text");
            Assert.IsTrue(errs.Any(e => e.Contains("W09 is not in the point database")), "route ref");
            Assert.IsTrue(errs.Any(e => e.Contains("Line 1")), "line vertices");
            Assert.IsTrue(errs.Any(e => e.Contains("ADF preset 1")), "adf");

            Assert.ThrowsException<InvalidDataException>(() => Ah64Dtc.MergeMission(LoadData("tti_hannover.dtc"), 1, m));

            Ah64Mission noTheater = new();
            Assert.IsTrue(Ah64Validator.ValidateMission(noTheater).Any(e => e.Contains("Theater")));
        }

        [TestMethod]
        public void MergeComms_OnlySetPresetsChange()
        {
            JsonNode data = LoadData("tti_hannover.dtc").DeepClone();
            JsonNode before = data["Presets"]["COMM"].DeepClone();
            List<Ah64CommPreset> presets = [.. Enumerable.Range(0, 10).Select(_ => new Ah64CommPreset()) ];
            presets[2] = new Ah64CommPreset()
            {
                IsSet = true, UnitId = "helo cmn", CallSign = "helo", Primary = PrimaryRadio.FM1, DlNet = true,
            };
            presets[2].FM1.Frequency = 75.5;
            data = Ah64Dtc.MergeComms(data, presets);
            JsonArray comm = data["Presets"]["COMM"].AsArray();
            for (int i = 0; i < 10; i++)
                if (i != 2)
                    Assert.IsTrue(JsonNode.DeepEquals(before[i], comm[i]), $"preset {i + 1} changed");
            Assert.AreEqual("HELO CMN", comm[2]["InitialMode"]["UnitID"].GetValue<string>());
            Assert.AreEqual(3, comm[2]["InitialMode"]["PrimaryFrequency"].GetValue<int>());
            Assert.IsTrue(comm[2]["InitialMode"]["DLNet"].GetValue<bool>());
            Assert.AreEqual(75.5, JsonNode.Parse(comm[2].ToJsonString())["COMMS"]["FM1"]["Frequency"].GetValue<double>());

            presets[2].UnitId = "HELICOPTER";
            Assert.AreEqual(1, Ah64Validator.CommWarnings(presets).Count);
            presets[2].UHF.Frequency = 500;
            Assert.ThrowsException<InvalidDataException>(() => Ah64Dtc.MergeComms(data, presets));
        }

        [TestMethod]
        public void ParseFile_RejectsOtherAirframes()
        {
            string viper = "{\"data\":{\"type\":\"F-16C_50\"},\"name\":\"x\",\"type\":\"F-16C_50\"}";
            Assert.ThrowsException<InvalidDataException>(() => Ah64Dtc.ParseFile(viper));
            Assert.ThrowsException<InvalidDataException>(() => Ah64Dtc.ParseFile("{\"name\":\"x\"}"));
        }

        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// structural json comparison: same keys and array lengths, strings / bools equal, numbers equal within a
        /// tolerance (coordinates go through lat/lon and back).
        /// </summary>
        private static void AssertJsonClose(JsonNode a, JsonNode b, string path)
        {
            // normalize in-memory nodes (typed int/double values) to parsed json so numbers compare as numbers.
            a = (a == null) ? null : JsonNode.Parse(a.ToJsonString());
            b = (b == null) ? null : JsonNode.Parse(b.ToJsonString());
            AssertJsonCloseParsed(a, b, path);
        }

        private static void AssertJsonCloseParsed(JsonNode a, JsonNode b, string path)
        {
            if ((a == null) || (b == null))
            {
                Assert.IsTrue((a == null) && (b == null), $"{path}: null mismatch");
                return;
            }
            switch (a)
            {
                case JsonObject oa:
                    JsonObject ob = b.AsObject();
                    CollectionAssert.AreEquivalent(oa.Select(kv => kv.Key).ToList(), ob.Select(kv => kv.Key).ToList(),
                                                   $"{path}: keys");
                    foreach (var kv in oa)
                        AssertJsonCloseParsed(kv.Value, ob[kv.Key], $"{path}.{kv.Key}");
                    break;
                case JsonArray aa:
                    JsonArray ab = b.AsArray();
                    Assert.AreEqual(aa.Count, ab.Count, $"{path}: count");
                    for (int i = 0; i < aa.Count; i++)
                        AssertJsonCloseParsed(aa[i], ab[i], $"{path}[{i}]");
                    break;
                default:
                    JsonValue va = a.AsValue();
                    JsonValue vb = b.AsValue();
                    if (va.TryGetValue(out double da))
                    {
                        Assert.IsTrue(vb.TryGetValue(out double db), $"{path}: type");
                        // route leg times are derived data: older cards used 0.514 m/s per knot, jafdtc uses the
                        // exact 1852/3600. allow 0.1% there; everything else must match to 0.1 mm.
                        double tol = path.EndsWith(".eta") ? Math.Max(1e-4, Math.Abs(da) * 1e-3) : 1e-4;
                        Assert.AreEqual(da, db, tol, path);
                    }
                    else if (va.TryGetValue(out bool ba))
                        Assert.AreEqual(ba, vb.GetValue<bool>(), path);
                    else
                        Assert.AreEqual(va.ToString(), vb.ToString(), path);
                    break;
            }
        }
    }
}
