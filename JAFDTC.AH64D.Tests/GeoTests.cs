// ********************************************************************************************************************
//
// GeoTests.cs : dcs theater projection and mgrs tests
//
// ********************************************************************************************************************

using JAFDTC.AH64D.Geo;

namespace JAFDTC.AH64D.Tests
{
    [TestClass]
    public class GeoTests
    {
        // ground truth: trigger zones placed at these lat/lon with dcs-mcp (which reads ED's terrain data), x/y read
        // back from the mission table. (theater, lat, lon, dcs x, dcs y)
        private static readonly (string T, double Lat, double Lon, double X, double Y)[] DcsTruth =
        [
            ("GermanyCW", 49.437, 7.6, -498684.71525638085, -934645.3181960878),
            ("GermanyCW", 51.0, 10.0, -353982.3584117936, -735429.1519006138),
            ("GermanyCW", 52.5, 13.4, -217786.40226344857, -480089.35159202345),
            ("GermanyCW", 50.0, 12.0, -484077.16712341364, -609108.0089995492),
            ("Caucasus", 42.0, 41.6, -312415.3286316758, 612991.2055643339),
            ("Caucasus", 44.0, 40.0, -102364.39560355898, 461749.9218877816),
            ("Caucasus", 43.0, 44.5, -172665.03366305493, 838243.4037847074),
            ("Kola", 68.9, 33.0, 146648.95815720037, 416788.40442261915),
            ("Kola", 67.5, 28.0, -39636.13147414848, 235708.34934715932),
            ("Kola", 69.5, 20.0, 166810.4987674635, -101784.7627227486),
            ("Kola", 66.0, 25.0, -217925.68717378937, 118745.55939502959),
            ("Afghanistan", 34.5, 69.2, 75435.99535253923, 269430.6410072034),
            ("Afghanistan", 31.6, 65.7, -260392.6926745479, -43976.553074953554),
            ("Afghanistan", 36.7, 67.1, 309776.818515365, 66170.05992708419),
            ("Afghanistan", 33.0, 62.0, -107926.02588762343, -393567.778207761),
            ("Iraq", 33.3, 44.4, 4649.355549776927, 16430.544918425585),
            ("Iraq", 30.5, 47.8, -302531.016469853, 341031.71588751447),
            ("Iraq", 36.2, 43.9, 326635.44274439337, -26601.30666420178),
            ("Iraq", 32.0, 40.0, -128678.05439896882, -400251.35812354315),
            ("Sinai", 30.0, 31.2, -5163.753960264847, -4398.220368790295),
            ("SinaiMap", 29.5, 34.9, -60425.478167122696, 353401.76206172694),
            ("SinaiMap", 31.8, 35.2, 195062.26787052676, 377496.4916592119),
            ("SinaiMap", 28.5, 33.5, -172619.16537400754, 218154.81676452575),
            ("Nevada", 36.2, -115.0, -402042.3937576907, -14186.66450422944),
            ("Nevada", 37.0, -116.5, -315038.8697081548, -149508.61928541833),
            ("Nevada", 35.5, -114.5, -478660.7671977598, 32759.56302888537),
            ("PersianGulf", 25.3, 55.4, -95804.7018627706, -85318.67434701917),
            ("PersianGulf", 26.5, 56.2, 36372.94813675713, -3965.7437401265197),
            ("PersianGulf", 27.2, 52.0, 123554.66589974798, -419807.8820151128),
            ("Syria", 33.5, 36.3, -169883.38881274546, 31973.93183625242),
            ("Syria", 35.0, 36.0, -2709.3083553262986, 9002.903660397744),
            ("Syria", 36.5, 37.5, 160588.20040242327, 148463.5752425836),
            ("MarianaIslands", 13.45, 144.75, -3826.6455266643316, -5184.198109068617),
            ("MarianaIslands", 15.1, 145.7, 177959.41157826548, 98713.02397743735),
            ("MarianaIslandsWWII", 14.1, 145.2, 67689.66664540884, 44090.58461989529),
            ("Falklands", -51.7, -57.9, 87361.41295073647, 85442.94077366317),
            ("Falklands", -52.0, -59.5, 51427.6332765203, -23973.223756000923),
            ("Falklands", -51.0, -60.5, 159760.05031717476, -97921.90896161817),
            ("Normandy", 49.2, -0.4, -31869.693522887304, -6127.470030102675),
            ("Normandy", 50.0, 1.0, 61487.84739155881, 91101.95103603037),
            ("Normandy", 48.8, -1.5, -78504.88144095335, -85375.25145050733),
            ("TheChannel", 51.0, 1.5, 14006.706710670143, -5875.909494935229),
            ("TheChannel", 50.8, 0.0, -5013.969696521759, -112017.14136388473),
            ("TheChannel", 51.3, 2.5, 46416.58439861238, 64518.03236982408),
        ];

        [TestMethod]
        public void Theater_Forward_MatchesDcs()
        {
            foreach (var t in DcsTruth)
            {
                DcsXY xy = DcsTheaters.Get(t.T).ToXY(t.Lat, t.Lon);
                Assert.AreEqual(t.X, xy.X, 0.01, $"{t.T} {t.Lat},{t.Lon} x");
                Assert.AreEqual(t.Y, xy.Y, 0.01, $"{t.T} {t.Lat},{t.Lon} y");
            }
        }

        [TestMethod]
        public void Theater_Inverse_MatchesDcs()
        {
            foreach (var t in DcsTruth)
            {
                (double lat, double lon) = DcsTheaters.Get(t.T).ToLatLon(t.X, t.Y);
                Assert.AreEqual(t.Lat, lat, 1e-8, $"{t.T} lat");
                Assert.AreEqual(t.Lon, lon, 1e-8, $"{t.T} lon");
            }
        }

        [TestMethod]
        public void Theater_AllNamesResolve()
        {
            foreach (string name in DcsTheaters.Names)
                Assert.IsNotNull(DcsTheaters.Find(name), name);
            Assert.AreEqual("SinaiMap", DcsTheaters.Canonical("Sinai"));
            Assert.AreEqual("GermanyCW", DcsTheaters.Canonical("germanycw"));
            Assert.IsNull(DcsTheaters.Find("Atlantis"));
        }

        // reference values from the python "mgrs" package (GeoTrans), 8-digit precision.
        private static readonly (string Mgrs, double Lat, double Lon)[] MgrsForward =
        [
            ("32ULV98507696", 49.436899, 7.600077),
            ("32ULV98507696", 49.4369, 7.6001),
            ("21FVC37807194", -51.7, -57.9),
            ("32VKM76975815", 60.0, 5.0),                    // norway exception
            ("36WWB00005436", 69.0, 33.0),
            ("33XWG13698076", 78.2, 15.6),                   // svalbard exception
            ("55PBQ56398801", 13.45, 144.75),
            ("56HLH34415192", -33.86, 151.21),
            ("31NAA66030001", 0.0001, 0.0001),
            ("11SPA79810798", 36.2, -115.0),
            ("41RQQ56179926", 31.6, 65.7),
            ("37TGG15335304", 42.0, 41.6),
            ("26XMT62107326", 83.5, -30.0),
            ("32CNS20347404", -79.5, 10.0),
        ];

        [TestMethod]
        public void Mgrs_FromLatLon_MatchesReference()
        {
            foreach (var t in MgrsForward)
            {
                string s = Mgrs.FromLatLon(t.Lat, t.Lon, 8).Replace(" ", "");
                Assert.AreEqual(t.Mgrs, s, $"{t.Lat},{t.Lon}");
            }
        }

        // reference south-west corners from python "mgrs" toLatLon.
        private static readonly (string Mgrs, double Lat, double Lon)[] MgrsInverse =
        [
            ("32ULV98507696", 49.436829145, 7.599993020),
            ("21FVC37807194", -51.700039348, -57.900043335),
            ("32VKM76975815", 59.999930101, 4.999830271),
            ("36WWB00005436", 68.999980803, 33.000000000),
            ("33XWG13698076", 78.200000161, 15.599695751),
            ("55PBQ56398801", 13.449969047, 144.749928261),
            ("56HLH34415192", -33.860047318, 151.209929900),
            ("11SPA79810798", 36.199948937, -115.000002917),
            ("41RQQ56179926", 31.599961949, 65.699962584),
            ("37TGG15335304", 41.999941355, 41.599953291),
            ("32CNS20347404", -79.500057891, 9.999846738),
            ("32UMV06468773", 49.534955978, 7.707208059),
            ("32ULA98000170", 49.659208330, 7.586694168),
        ];

        [TestMethod]
        public void Mgrs_Parse_MatchesReference()
        {
            foreach (var t in MgrsInverse)
            {
                Assert.IsTrue(Mgrs.TryParse(t.Mgrs, out double lat, out double lon), t.Mgrs);
                Assert.AreEqual(t.Lat, lat, 1e-6, $"{t.Mgrs} lat");
                Assert.AreEqual(t.Lon, lon, 1e-6, $"{t.Mgrs} lon");
            }
        }

        [TestMethod]
        public void Mgrs_ParseAcceptsSpacingAndPrecision()
        {
            Assert.IsTrue(Mgrs.TryParse("32U LV 9850 7696", out double a1, out double o1));
            Assert.IsTrue(Mgrs.TryParse("32ulv98507696", out double a2, out double o2));
            Assert.AreEqual(a1, a2, 1e-12);
            Assert.AreEqual(o1, o2, 1e-12);
            Assert.IsTrue(Mgrs.TryParse("32U LV 98507 76967", out double a3, out double o3));
            Assert.AreEqual(49.4369, a3, 1e-4);
            Assert.AreEqual(7.6001, o3, 1e-4);
            Assert.AreEqual("32U LV 9850 7696", Mgrs.Normalize("32ULV9850076960"));
        }

        [TestMethod]
        public void Mgrs_RejectsBadInput()
        {
            Assert.IsFalse(Mgrs.IsValid(""));
            Assert.IsFalse(Mgrs.IsValid("32U LV 985 7696"));       // odd digit count
            Assert.IsFalse(Mgrs.IsValid("61U LV 9850 7696"));      // zone
            Assert.IsFalse(Mgrs.IsValid("32I LV 9850 7696"));      // band I
            Assert.IsFalse(Mgrs.IsValid("32U IV 9850 7696"));      // column I
            Assert.IsFalse(Mgrs.IsValid("32U AV 9850 7696"));      // A is not a zone-32 column letter
            Assert.IsFalse(Mgrs.IsValid("hello"));
        }

        [TestMethod]
        public void Mgrs_RoundTripsGrid()
        {
            Random rnd = new(442);
            for (int i = 0; i < 2000; i++)
            {
                double lat = rnd.NextDouble() * 160.0 - 79.0;
                double lon = rnd.NextDouble() * 359.0 - 179.5;
                string s = Mgrs.FromLatLon(lat, lon, 10);
                Assert.IsTrue(Mgrs.TryParse(s, out double lat2, out double lon2), s);
                Assert.AreEqual(s, Mgrs.FromLatLon(lat2, lon2, 10), $"{lat},{lon}");
            }
        }
    }
}
