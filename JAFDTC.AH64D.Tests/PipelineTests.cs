// ********************************************************************************************************************
//
// PipelineTests.cs : end-to-end tape build exactly the way JAFDTC's ConfigurationBase.SaveMergedSimDTC does it
//
// ********************************************************************************************************************

using JAFDTC.AH64D.Csv;
using JAFDTC.AH64D.Dtc;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace JAFDTC.AH64D.Tests
{
    [TestClass]
    public class PipelineTests
    {
        private static string Data(string name) => Path.Combine(AppContext.BaseDirectory, "TestData", name);

        // same options as JAFDTC's Globals.JSONOptions
        private static readonly JsonSerializerOptions JafdtcJson = new() { WriteIndented = true };

        [TestMethod]
        public void ShippedTemplate_IsCleanAh64Tape()
        {
            JsonNode root = Ah64Dtc.ParseFile(File.ReadAllText(Data("AH-64D_BLK_II.dtc")));
            Assert.AreEqual("AH-64D_BLK_II", root["type"].GetValue<string>());
            JsonNode data = root["data"];
            for (int i = 1; i <= 2; i++)
            {
                Ah64Mission m = Ah64Dtc.ReadMission(data, i);
                Assert.IsTrue(m.IsDefault, $"template Mission_{i} is not empty");
            }
            Assert.AreEqual(10, Ah64Dtc.ReadComms(data).Count);
        }

        [TestMethod]
        public void BuildTape_AsJafdtcDoes_ThenReadBack()
        {
            // build the configuration from the IQT-4 worksheets
            Ah64Mission m1 = new() { Theater = "GermanyCW" };
            Ah64Csv.ImportNav(m1, File.ReadAllText(Data("iqt4_day_control_measures.csv")));
            Ah64Csv.ImportNav(m1, File.ReadAllText(Data("iqt4_day_targets_threats.csv")));
            Ah64Csv.ImportNav(m1, File.ReadAllText(Data("iqt4_day_route.csv")));
            Ah64Mission m2 = m1.Clone();
            List<Ah64CommPreset> comms = [.. Enumerable.Range(0, 10).Select(_ => new Ah64CommPreset()) ];
            Ah64Csv.ImportRadios(comms, File.ReadAllText(Data("iqt4_radio_presets.csv")));

            // ConfigurationBase.SaveMergedSimDTC: parse template, replace dom["data"] with the merged data, name
            // the tape after the file, serialize with Globals.JSONOptions.
            string name = "AH-64D IQT-4 DAY CHAOS JAFDTC";
            JsonNode dom = JsonNode.Parse(File.ReadAllText(Data("AH-64D_BLK_II.dtc")));
            dom["name"] = name;
            JsonNode data = dom["data"];
            data = Ah64Dtc.MergeMission(data, 1, m1);          // NAV1System.MergeIntoSimDTC
            data = Ah64Dtc.MergeMission(data, 2, m2);          // NAV2System.MergeIntoSimDTC
            data = Ah64Dtc.MergeComms(data, comms);            // COMMSystem.MergeIntoSimDTC
            dom["data"] = data;
            dom["data"]["name"] = name;
            string json = dom.ToJsonString(JafdtcJson);

            string outPath = Path.Combine(Path.GetTempPath(), name + ".dtc");
            File.WriteAllText(outPath, json);

            // read the tape back as a dcs file
            JsonNode back = Ah64Dtc.ParseFile(File.ReadAllText(outPath))["data"];
            Assert.AreEqual(name, back["name"].GetValue<string>());
            Assert.AreEqual("GermanyCW", back["terrain"].GetValue<string>());
            for (int i = 1; i <= 2; i++)
            {
                Ah64Mission r = Ah64Dtc.ReadMission(back, i);
                Assert.AreEqual(m1.Points.Count, r.Points.Count);
                Assert.AreEqual(12, r.Routes.Single(x => x.Name == "VENUS").Points.Count);
                Assert.AreEqual(0, Ah64Validator.ValidateMission(r).Count);
            }
            List<Ah64CommPreset> rc = Ah64Dtc.ReadComms(back);
            Assert.AreEqual("ETAR GND", rc[0].UnitId);
            Assert.AreEqual(75.5, rc[7].FM1.Frequency, 1e-9);

            // the dtc manager's other sections came through untouched from the template
            JsonNode tmpl = JsonNode.Parse(File.ReadAllText(Data("AH-64D_BLK_II.dtc")))["data"];
            foreach (string key in new[] { "IDM", "Laser", "MISC", "Radios", "Weapon", "type" })
                Assert.IsTrue(JsonNode.DeepEquals(tmpl[key], JsonNode.Parse(back[key].ToJsonString())), key);

            // every top-level key the dcs-saved card has is present, nothing extra
            JsonNode dcs = JsonNode.Parse(File.ReadAllText(Data("tti_hannover.dtc")));
            CollectionAssert.AreEquivalent(dcs.AsObject().Select(kv => kv.Key).ToList(),
                                           JsonNode.Parse(json).AsObject().Select(kv => kv.Key).ToList());
            CollectionAssert.AreEquivalent(dcs["data"].AsObject().Select(kv => kv.Key).ToList(),
                                           back.AsObject().Select(kv => kv.Key).ToList());
        }
    }
}
