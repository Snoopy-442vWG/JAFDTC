// ********************************************************************************************************************
//
// CsvTests.cs : worksheet csv import / export tests (442CWG IQT-4 day worksheets)
//
// ********************************************************************************************************************

using JAFDTC.AH64D.Csv;
using JAFDTC.AH64D.Dtc;
using JAFDTC.AH64D.Geo;
using System.Text.Json.Nodes;

namespace JAFDTC.AH64D.Tests
{
    [TestClass]
    public class CsvTests
    {
        private static string Data(string name) => Path.Combine(AppContext.BaseDirectory, "TestData", name);

        private static Ah64Mission ImportIqt4(CsvImportResult[] results = null)
        {
            Ah64Mission m = new() { Theater = "GermanyCW" };
            CsvImportResult a = Ah64Csv.ImportNav(m, File.ReadAllText(Data("iqt4_day_control_measures.csv")));
            CsvImportResult b = Ah64Csv.ImportNav(m, File.ReadAllText(Data("iqt4_day_targets_threats.csv")));
            CsvImportResult c = Ah64Csv.ImportNav(m, File.ReadAllText(Data("iqt4_day_route.csv")));
            if (results != null)
                (results[0], results[1], results[2]) = (a, b, c);
            return m;
        }

        [TestMethod]
        public void Detect_Kinds()
        {
            Assert.AreEqual(Ah64Csv.CsvKind.Points, Ah64Csv.Detect(File.ReadAllText(Data("iqt4_day_control_measures.csv"))));
            Assert.AreEqual(Ah64Csv.CsvKind.Route, Ah64Csv.Detect(File.ReadAllText(Data("iqt4_day_route.csv"))));
            Assert.AreEqual(Ah64Csv.CsvKind.Radios, Ah64Csv.Detect(File.ReadAllText(Data("iqt4_radio_presets.csv"))));
            Assert.AreEqual(Ah64Csv.CsvKind.Unknown, Ah64Csv.Detect("a,b,c\n1,2,3\n"));
        }

        [TestMethod]
        public void Import_Iqt4Worksheets_MatchesGeneratedCard()
        {
            CsvImportResult[] results = new CsvImportResult[3];
            Ah64Mission m = ImportIqt4(results);
            Assert.AreEqual(0, results.Sum(r => r.Warnings.Count), string.Join("\n", results.SelectMany(r => r.Warnings)));
            Assert.AreEqual(11, m.PointsIn(PointCategory.CTRLM).Count());
            Assert.AreEqual(10, m.PointsIn(PointCategory.TGT).Count());
            Assert.AreEqual(11, m.PointsIn(PointCategory.WPTHZ).Count());

            Ah64Route venus = m.Routes.Single(r => r.Name.Trim() == "VENUS");
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 1 },
                                      venus.Points.Select(p => p.Number).ToArray());
            Assert.IsTrue(venus.IsEnabled);

            // the 442CWG IQT-4 DAY card was generated from these worksheets (lat/lon to 6 decimals for C/T points,
            // 4 decimals + 8-digit mgrs for the route): positions must agree.
            JsonNode card = Ah64Dtc.ParseFile(File.ReadAllText(Data("iqt4_day_generated.dtc")))["data"];
            JsonNode m1 = card["NAV"]["Mission_1"];
            DcsTheaters.TheaterProjection proj = DcsTheaters.Get("GermanyCW");
            // the card's generator placed some points from the mission's unit positions, so allow mgrs-grid error.
            foreach ((string key, PointCategory cat, double tol) in new[] { ("CTRLM", PointCategory.CTRLM, 15.0),
                                                                            ("TGT", PointCategory.TGT, 15.0),
                                                                            ("WPTHZ", PointCategory.WPTHZ, 15.0) })
            {
                foreach (JsonNode p in m1["Points"][key]["POINTS"].AsArray())
                {
                    int num = p["num"].GetValue<int>();
                    Ah64Point ip = m.FindPoint(cat, num);
                    Assert.IsNotNull(ip, $"{key} {num}");
                    DcsXY xy = proj.ToXY(ip.Lat, ip.Lon);
                    double d = Ah64Dtc.LegMeters(xy, new DcsXY(p["x"].GetValue<double>(), p["y"].GetValue<double>()));
                    Assert.IsTrue(d < tol, $"{ip.Label} is {d:F2} m from the card");
                    Assert.AreEqual(p["id"].GetValue<int>(), PointIdents.Find(cat, ip.Ident).DtcId, $"{ip.Label} id");
                    Assert.AreEqual(p["text"].GetValue<string>(), ip.Free, $"{ip.Label} text");
                }
            }
        }

        [TestMethod]
        public void Import_Iqt4_MergesIntoValidDtc()
        {
            Ah64Mission m = ImportIqt4();
            Assert.AreEqual(0, Ah64Validator.ValidateMission(m).Count, string.Join("\n", Ah64Validator.ValidateMission(m)));
            JsonNode data = Ah64Dtc.ParseFile(File.ReadAllText(Data("tti_hannover.dtc")))["data"];
            data = Ah64Dtc.MergeMission(data, 1, m);
            JsonNode r = data["NAV"]["Mission_1"]["Routes"].AsArray().Single(x => x["Name"].GetValue<string>() == "VENUS");
            Assert.AreEqual(12, r["POINTS"].AsArray().Count);
            double total = r["POINTS"].AsArray().Sum(p => p["dist"].GetValue<double>()) / 1852.0;
            Assert.IsTrue((total > 20.0) && (total < 40.0), $"VENUS is {total:F1} nm (syllabus routes are 20-40 nm)");

            // and it reads back the same
            Ah64Mission back = Ah64Dtc.ReadMission(data, 1);
            Assert.AreEqual(m.Points.Count, back.Points.Count);
            foreach (Ah64Point p in m.Points)
            {
                Ah64Point q = back.FindPoint(p.Category, p.Number);
                Assert.AreEqual(p.Lat, q.Lat, 1e-9);
                Assert.AreEqual(p.Lon, q.Lon, 1e-9);
                Assert.AreEqual(p.Ident, q.Ident);
                Assert.AreEqual(p.AltFt, q.AltFt, 0.05);
            }
        }

        [TestMethod]
        public void Import_Radios_MatchesCard()
        {
            List<Ah64CommPreset> presets = [.. Enumerable.Range(0, 10).Select(_ => new Ah64CommPreset()) ];
            CsvImportResult res = Ah64Csv.ImportRadios(presets, File.ReadAllText(Data("iqt4_radio_presets.csv")));
            Assert.AreEqual(10, res.PresetsSet);
            JsonNode card = Ah64Dtc.ParseFile(File.ReadAllText(Data("iqt4_day_generated.dtc")))["data"];
            List<Ah64CommPreset> fromCard = Ah64Dtc.ReadComms(card);
            for (int i = 0; i < 10; i++)
            {
                Assert.AreEqual(fromCard[i].UnitId, presets[i].UnitId, $"preset {i + 1} unit");
                Assert.AreEqual(fromCard[i].CallSign, presets[i].CallSign, $"preset {i + 1} callsign");
                Assert.AreEqual(fromCard[i].Primary, presets[i].Primary, $"preset {i + 1} primary");
                Assert.AreEqual(fromCard[i].DlNet, presets[i].DlNet, $"preset {i + 1} dl");
                if ((i != 7) && (i != 8))       // presets 8 and 9 have no UHF in the worksheet (card has a placeholder)
                    Assert.AreEqual(fromCard[i].UHF.Frequency, presets[i].UHF.Frequency, 1e-6, $"preset {i + 1} uhf");
            }
            Assert.AreEqual(75.5, presets[7].FM1.Frequency, 1e-9);
            Assert.AreEqual(PrimaryRadio.FM1, presets[7].Primary);
        }

        [TestMethod]
        public void Export_ThenImport_RoundTrips()
        {
            Ah64Mission m = ImportIqt4();
            string pts = Ah64Csv.ExportPoints(m);
            string routes = Ah64Csv.ExportRoutes(m);

            Ah64Mission m2 = new() { Theater = "GermanyCW" };
            CsvImportResult r1 = Ah64Csv.ImportNav(m2, pts);
            CsvImportResult r2 = Ah64Csv.ImportNav(m2, routes);
            Assert.AreEqual(0, r1.Warnings.Count + r2.Warnings.Count,
                            string.Join("\n", r1.Warnings.Concat(r2.Warnings)));
            Assert.AreEqual(m.Points.Count, m2.Points.Count);
            foreach (Ah64Point p in m.Points)
            {
                Ah64Point q = m2.FindPoint(p.Category, p.Number);
                Assert.AreEqual(p.Ident, q.Ident, p.Label);
                Assert.AreEqual(p.Free, q.Free, p.Label);
                Assert.AreEqual(p.Note, q.Note, p.Label);
                Assert.AreEqual(p.Lat, q.Lat, 1e-6, p.Label);
                Assert.AreEqual(p.Lon, q.Lon, 1e-6, p.Label);
            }
            CollectionAssert.AreEqual(m.Routes[0].Points.Select(p => p.Number).ToArray(),
                                      m2.Routes[0].Points.Select(p => p.Number).ToArray());
        }

        [TestMethod]
        public void Import_WarnsOnBadRows()
        {
            string csv = "TSD CP,IDENT,FREE Text,Name,MGRS,Elev_ft\n" +
                         "C51,ZZ,ABCD,bad ident,32U LV 9850 7696,800\n" +
                         "Q01,WP,X,not a point,32U LV 9850 7696,800\n" +
                         "W02,WP,X,bad grid,32U LV 9850 769,800\n";
            Ah64Mission m = new() { Theater = "GermanyCW" };
            CsvImportResult r = Ah64Csv.ImportNav(m, csv);
            Assert.IsTrue(r.Warnings.Any(w => w.Contains("IDENT \"ZZ\"")));
            Assert.IsTrue(r.Warnings.Any(w => w.Contains("shortened")));
            Assert.IsTrue(r.Warnings.Any(w => w.Contains("Q01")));
            Assert.IsTrue(r.Warnings.Any(w => w.Contains("MGRS")));
            Assert.AreEqual("CP", m.FindPoint(PointCategory.CTRLM, 51).Ident);
            Assert.AreEqual("ABC", m.FindPoint(PointCategory.CTRLM, 51).Free);
        }

        [TestMethod]
        public void Parse_HandlesQuotesAndBom()
        {
            List<List<string>> rows = Ah64Csv.Parse("﻿a,\"b, c\",\"d \"\"q\"\"\"\r\n1,\"multi\nline\",3");
            Assert.AreEqual(2, rows.Count);
            CollectionAssert.AreEqual(new[] { "a", "b, c", "d \"q\"" }, rows[0]);
            CollectionAssert.AreEqual(new[] { "1", "multi\nline", "3" }, rows[1]);
        }
    }
}
