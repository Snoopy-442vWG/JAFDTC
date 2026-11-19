// ********************************************************************************************************************
//
// Ah64Dtc.cs : read / merge AH-64D sections of a dcs .dtc (json) file
//
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

using JAFDTC.AH64D.Geo;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace JAFDTC.AH64D.Dtc
{
    /// <summary>
    /// reads and merges the AH-64D sections of a dcs .dtc file. the layout follows .dtc files saved by the dcs
    /// dtc manager (AH-64D_BLK_II):
    ///
    ///   data.terrain                      theater name, selects the x/y projection
    ///   data.NAV.Mission_1 / Mission_2    { ADF, Areas, Lines, Points { CTRLM, TGT, WPTHZ }, Routes, Zones }
    ///   data.Presets.COMM[10]             { COMMS, DoNotUploadTabData, InitialMode }
    ///
    /// coordinates are dcs map x (north) / y (east) meters for data.terrain; point and route altitudes are meters
    /// MSL. everything not listed above (IDM, Laser, MISC, Radios, Weapon, ...) is left exactly as the template
    /// has it.
    /// </summary>
    public static class Ah64Dtc
    {
        public const string DtcType = "AH-64D_BLK_II";

        // ------------------------------------------------------------------------------------------------------------
        //
        // files
        //
        // ------------------------------------------------------------------------------------------------------------

        public static JsonNode ParseFile(string json)
        {
            JsonNode root = JsonNode.Parse(json, documentOptions: new JsonDocumentOptions()
                {
                    AllowTrailingCommas = true,
                    CommentHandling = JsonCommentHandling.Skip
                }) ?? throw new InvalidDataException("The file is not a valid DCS DTC (JSON) file.");
            if (root["data"] is not JsonObject)
                throw new InvalidDataException("The file has no \"data\" section, so it is not a DCS DTC file.");
            string type = root["type"]?.GetValue<string>() ?? root["data"]["type"]?.GetValue<string>() ?? "";
            if (!type.StartsWith("AH-64D", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException($"The DTC is for \"{type}\", not the AH-64D.");
            return root;
        }

        public static string GetTheater(JsonNode dataRoot)
            => DcsTheaters.Canonical(dataRoot?["terrain"]?.GetValue<string>() ?? "");

        public static int MissionCount(JsonNode dataRoot)
            => ((dataRoot?["NAV"]?["Mission_1"] != null) ? 1 : 0) + ((dataRoot?["NAV"]?["Mission_2"] != null) ? 1 : 0);

        // ------------------------------------------------------------------------------------------------------------
        //
        // import
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// read Mission_1 (index 1) or Mission_2 (index 2) from the dtc "data" object.
        /// </summary>
        public static Ah64Mission ReadMission(JsonNode dataRoot, int index, List<string> warnings = null)
        {
            warnings ??= [];
            string theater = GetTheater(dataRoot);
            DcsTheaters.TheaterProjection proj = DcsTheaters.Find(theater)
                ?? throw new InvalidDataException($"The DTC terrain \"{theater}\" is not a known DCS theater.");

            JsonNode mNode = dataRoot["NAV"]?[$"Mission_{index}"]
                ?? throw new InvalidDataException($"The DTC has no NAV Mission_{index}.");

            Ah64Mission m = new() { Theater = proj.Name };
            m.Routes.Clear();

            // points
            foreach ((PointCategory cat, string key) in new[] { (PointCategory.WPTHZ, "WPTHZ"),
                                                                 (PointCategory.CTRLM, "CTRLM"),
                                                                 (PointCategory.TGT, "TGT") })
            {
                JsonNode db = mNode["Points"]?[key];
                if (db == null)
                    continue;
                bool isEnabled = Bool(db["isEnabled"], true);
                switch (cat)
                {
                    case PointCategory.WPTHZ: m.LoadWptHz = isEnabled; break;
                    case PointCategory.CTRLM: m.LoadCtrlm = isEnabled; break;
                    default: m.LoadTgt = isEnabled; break;
                }
                foreach (JsonNode p in Arr(db["POINTS"]))
                {
                    int num = Int(p["num"]);
                    int id = Int(p["id"]);
                    PointIdent ident = PointIdents.FromDtcId(cat, id);
                    if (ident == null)
                        warnings.Add($"Mission {index} {PointIdents.Label(cat, num)}: unknown IDENT id {id}, set to " +
                                     $"{PointIdents.Default(cat).Code}.");
                    (double lat, double lon) = proj.ToLatLon(Dbl(p["x"]), Dbl(p["y"]));
                    m.Points.Add(new Ah64Point()
                    {
                        Category = cat,
                        Number = num,
                        Ident = (ident ?? PointIdents.Default(cat)).Code,
                        Free = Str(p["text"]),
                        Note = Str(p["note"]),
                        Lat = lat,
                        Lon = lon,
                        AltFt = Dbl(p["alt"]) / Ah64Limits.MetersPerFoot
                    });
                }
            }

            // routes
            int iRoute = 0;
            foreach (JsonNode r in Arr(mNode["Routes"]))
            {
                Ah64Route route = new()
                {
                    Name = Str(r["Name"]),
                    IsEnabled = Bool(r["isEnabled"], false)
                };
                bool isFirst = true;
                foreach (JsonNode rp in Arr(r["POINTS"]))
                {
                    if (isFirst)
                        route.StartTimeSec = Dbl(rp["eta"]);
                    isFirst = false;
                    route.Points.Add(new Ah64RoutePoint()
                    {
                        Number = Int(rp["num"]),
                        SpeedKts = Dbl(rp["speed"]),
                        AltFt = Dbl(rp["alt"]) / Ah64Limits.MetersPerFoot,
                        Fix = Bool(rp["fix"], false)
                    });
                }
                m.Routes.Add(route);
                iRoute++;
            }
            while (m.Routes.Count < Ah64Limits.Routes)
                m.Routes.Add(new Ah64Route() { Name = Ah64Limits.DefaultRouteNames[m.Routes.Count] });

            // lines
            foreach (JsonNode l in Arr(mNode["Lines"]))
            {
                m.Lines.Add(new Ah64Line()
                {
                    TypeNum = Int(l["type_num"], 1),
                    Text = Str(l["text"]),
                    Note = Str(l["note"]),
                    Vertices = ReadVertices(proj, l["vertices"])
                });
            }

            // areas, zones
            foreach (JsonNode a in Arr(mNode["Areas"]))
                m.Shapes.Add(ReadShape(proj, a, ShapeKind.Area));
            foreach (JsonNode z in Arr(mNode["Zones"]?["NFZ"]))
                m.Shapes.Add(ReadShape(proj, z, ShapeKind.NoFireZone));
            foreach (JsonNode z in Arr(mNode["Zones"]?["PFZ"]))
                m.Shapes.Add(ReadShape(proj, z, ShapeKind.PriorityFireZone));

            // adf
            JsonNode adf = mNode["ADF"];
            if (adf != null)
            {
                m.AdfEnabled = Bool(adf["isEnabled"], false);
                for (int i = 0; i < Ah64Limits.AdfPresets; i++)
                {
                    JsonNode p = adf[$"Preset_{i + 1}"];
                    if (p != null)
                        m.Adf[i] = new Ah64AdfPreset() { Id = Str(p["ID"]), FreqKHz = Dbl(p["Freq"], 100.0) };
                }
            }

            return m;
        }

        /// <summary>
        /// read the 10 comm presets from the dtc "data" object. every preset read is marked IsSet.
        /// </summary>
        public static List<Ah64CommPreset> ReadComms(JsonNode dataRoot)
        {
            List<Ah64CommPreset> presets = [];
            foreach (JsonNode p in Arr(dataRoot["Presets"]?["COMM"]))
            {
                JsonNode c = p["COMMS"];
                JsonNode im = p["InitialMode"];
                presets.Add(new Ah64CommPreset()
                {
                    IsSet = true,
                    UnitId = Str(im?["UnitID"]),
                    CallSign = Str(im?["CallSign"]),
                    Primary = (PrimaryRadio)Int(im?["PrimaryFrequency"]),
                    DlNet = Bool(im?["DLNet"], false),
                    VHF = ReadRadio(c?["VHF"]),
                    UHF = ReadRadio(c?["UHF"]),
                    FM1 = ReadRadio(c?["FM1"]),
                    FM2 = ReadRadio(c?["FM2"]),
                    HFTx = ReadRadio(c?["HF_Tx"]),
                    HFRx = ReadRadio(c?["HF_Rx"]),
                    HfAle = Int(c?["HF_ALE"], 1),
                    HfPre = Int(c?["HF_Pre"], 1),
                    DoNotUploadTabData = Bool(p["DoNotUploadTabData"], false)
                });
            }
            while (presets.Count < Ah64Limits.CommPresets)
                presets.Add(new Ah64CommPreset());
            return presets;
        }

        private static Ah64RadioSetting ReadRadio(JsonNode n)
            => new()
            {
                Frequency = Dbl(n?["Frequency"]),
                Modulation = Int(n?["Modulation"]),
                Encryption = Int(n?["Encryption"])
            };

        private static List<GeoPoint> ReadVertices(DcsTheaters.TheaterProjection proj, JsonNode verts)
        {
            List<GeoPoint> list = [];
            foreach (JsonNode v in Arr(verts))
            {
                (double lat, double lon) = proj.ToLatLon(Dbl(v["x"]), Dbl(v["y"]));
                list.Add(new GeoPoint(lat, lon));
            }
            return list;
        }

        private static Ah64Shape ReadShape(DcsTheaters.TheaterProjection proj, JsonNode n, ShapeKind kind)
        {
            Ah64Shape shape = new()
            {
                Kind = kind,
                Note = Str(n["note"]),
                Vertices = ReadVertices(proj, n["vertices"]),
            };
            if (n["caption_pos"] is JsonArray caps)
            {
                shape.CaptionsJson = caps.ToJsonString();
                shape.CaptionsVertexKey = shape.VertexKey();
            }
            return shape;
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // merge (export)
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// replace data.NAV.Mission_{index} with the mission, and set data.terrain to the mission's theater.
        /// throws InvalidDataException if the mission does not validate.
        /// </summary>
        public static JsonNode MergeMission(JsonNode dataRoot, int index, Ah64Mission m)
        {
            List<string> errors = Ah64Validator.ValidateMission(m);
            if (errors.Count > 0)
                throw new InvalidDataException($"Mission {index} has errors:\n" + string.Join("\n", errors));

            DcsTheaters.TheaterProjection proj = DcsTheaters.Get(m.Theater);
            dataRoot["terrain"] = proj.Name;

            if (dataRoot["NAV"] is not JsonObject nav)
            {
                nav = [];
                dataRoot["NAV"] = nav;
            }
            nav[$"Mission_{index}"] = BuildMission(proj, m);
            return dataRoot;
        }

        /// <summary>
        /// overwrite the comm presets that are marked IsSet; presets that are not set keep the template's values.
        /// </summary>
        public static JsonNode MergeComms(JsonNode dataRoot, IList<Ah64CommPreset> presets)
        {
            List<string> errors = Ah64Validator.ValidateComms(presets);
            if (errors.Count > 0)
                throw new InvalidDataException("Comm presets have errors:\n" + string.Join("\n", errors));

            if (dataRoot["Presets"] is not JsonObject presetsRoot)
            {
                presetsRoot = [];
                dataRoot["Presets"] = presetsRoot;
            }
            if (presetsRoot["COMM"] is not JsonArray comm)
            {
                comm = [];
                presetsRoot["COMM"] = comm;
            }
            for (int i = 0; i < Math.Min(presets.Count, Ah64Limits.CommPresets); i++)
            {
                if (!presets[i].IsSet)
                    continue;
                JsonObject node = BuildCommPreset(presets[i]);
                if (i < comm.Count)
                    comm[i] = node;
                else
                {
                    while (comm.Count < i)
                        comm.Add(BuildCommPreset(new Ah64CommPreset()));
                    comm.Add(node);
                }
            }
            return dataRoot;
        }

        public static JsonObject BuildCommPreset(Ah64CommPreset p)
            => new()
            {
                ["COMMS"] = new JsonObject()
                {
                    ["FM1"] = Radio(p.FM1),
                    ["FM2"] = Radio(p.FM2),
                    ["HF_ALE"] = p.HfAle,
                    ["HF_Pre"] = p.HfPre,
                    ["HF_Rx"] = Radio(p.HFRx),
                    ["HF_Tx"] = Radio(p.HFTx),
                    ["UHF"] = Radio(p.UHF),
                    ["VHF"] = Radio(p.VHF)
                },
                ["DoNotUploadTabData"] = p.DoNotUploadTabData,
                ["InitialMode"] = new JsonObject()
                {
                    ["CallSign"] = (p.CallSign ?? "").ToUpperInvariant(),
                    ["DLNet"] = p.DlNet,
                    ["PrimaryFrequency"] = (int)p.Primary,
                    ["UnitID"] = (p.UnitId ?? "").ToUpperInvariant()
                }
            };

        private static JsonObject Radio(Ah64RadioSetting r)
            => new()
            {
                ["Encryption"] = r.Encryption,
                ["Frequency"] = Round(r.Frequency, 4),
                ["Modulation"] = r.Modulation
            };

        public static JsonObject BuildMission(DcsTheaters.TheaterProjection proj, Ah64Mission m)
        {
            // ADF
            JsonObject adf = new() { ["isEnabled"] = m.AdfEnabled };
            for (int i = 0; i < Ah64Limits.AdfPresets; i++)
            {
                Ah64AdfPreset p = (i < m.Adf.Count) ? m.Adf[i] : new Ah64AdfPreset();
                adf[$"Preset_{i + 1}"] = new JsonObject()
                {
                    ["Freq"] = Round(p.FreqKHz, 1),
                    ["ID"] = (p.Id ?? "").ToUpperInvariant()
                };
            }

            // areas, zones
            JsonArray areas = [];
            JsonArray nfz = [];
            JsonArray pfz = [];
            foreach (Ah64Shape s in m.Shapes)
            {
                JsonObject node = BuildShape(proj, s);
                switch (s.Kind)
                {
                    case ShapeKind.Area: areas.Add(node); break;
                    case ShapeKind.NoFireZone: nfz.Add(node); break;
                    default: pfz.Add(node); break;
                }
            }

            // lines
            JsonArray lines = [];
            foreach (Ah64Line l in m.Lines)
                lines.Add(new JsonObject()
                {
                    ["note"] = l.Note ?? "",
                    ["text"] = l.Text ?? "",
                    ["type_num"] = l.TypeNum,
                    ["vertices"] = Vertices(proj, l.Vertices)
                });

            // points
            JsonObject points = new()
            {
                ["CTRLM"] = BuildPointDb(proj, m, PointCategory.CTRLM, m.LoadCtrlm),
                ["TGT"] = BuildPointDb(proj, m, PointCategory.TGT, m.LoadTgt),
                ["WPTHZ"] = BuildPointDb(proj, m, PointCategory.WPTHZ, m.LoadWptHz)
            };

            // routes
            JsonArray routes = [];
            for (int i = 0; i < Ah64Limits.Routes; i++)
            {
                Ah64Route r = (i < m.Routes.Count) ? m.Routes[i] : new Ah64Route() { Name = Ah64Limits.DefaultRouteNames[i] };
                routes.Add(BuildRoute(proj, m, r, i));
            }

            return new JsonObject()
            {
                ["ADF"] = adf,
                ["Areas"] = areas,
                ["Lines"] = lines,
                ["Points"] = points,
                ["Routes"] = routes,
                ["Zones"] = new JsonObject() { ["NFZ"] = nfz, ["PFZ"] = pfz }
            };
        }

        private static JsonObject BuildPointDb(DcsTheaters.TheaterProjection proj, Ah64Mission m, PointCategory cat,
                                               bool isEnabled)
        {
            JsonArray list = [];
            foreach (Ah64Point p in m.PointsIn(cat))
            {
                DcsXY xy = proj.ToXY(p.Lat, p.Lon);
                PointIdent ident = PointIdents.Find(cat, p.Ident) ?? PointIdents.Default(cat);
                list.Add(new JsonObject()
                {
                    ["alt"] = Round(p.AltFt * Ah64Limits.MetersPerFoot, 2),
                    ["id"] = ident.DtcId,
                    ["note"] = p.Note ?? "",
                    ["num"] = p.Number,
                    ["text"] = (p.Free ?? "").ToUpperInvariant(),
                    ["x"] = Round(xy.X, 8),
                    ["y"] = Round(xy.Y, 8)
                });
            }
            return new JsonObject()
            {
                ["isEnabled"] = isEnabled && (list.Count > 0),
                ["POINTS"] = list
            };
        }

        /// <summary>
        /// leg distance in meters between two points in dcs map coordinates (the projection the dcs mission editor
        /// measures in).
        /// </summary>
        public static double LegMeters(DcsXY a, DcsXY b) => Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));

        private static JsonObject BuildRoute(DcsTheaters.TheaterProjection proj, Ah64Mission m, Ah64Route r, int index)
        {
            JsonArray pts = [];
            DcsXY? prev = null;
            foreach (Ah64RoutePoint rp in r.Points)
            {
                Ah64Point p = m.FindPoint(rp.Category, rp.Number);
                DcsXY xy = proj.ToXY(p.Lat, p.Lon);
                double dist = (prev == null) ? 0.0 : LegMeters(prev.Value, xy);
                double eta = (prev == null) ? r.StartTimeSec
                                            : ((rp.SpeedKts > 0.0) ? dist / (rp.SpeedKts * Ah64Limits.MetersPerSecondPerKnot)
                                                                   : 0.0);
                pts.Add(new JsonObject()
                {
                    ["alt"] = Round(rp.AltFt * Ah64Limits.MetersPerFoot, 3),
                    ["dist"] = Round(dist, 3),
                    ["eta"] = Round(eta, 3),
                    ["fix"] = rp.Fix,
                    ["num"] = rp.Number,
                    ["speed"] = Round(rp.SpeedKts, 2)
                });
                prev = xy;
            }
            // names are written as given (dcs saves "MARS", unpadded); only dcs' own defaults carry padding
            // ("ECHO ", "LIMA ").
            string name = (string.IsNullOrWhiteSpace(r.Name) ? Ah64Limits.DefaultRouteNames[index] : r.Name)
                          .ToUpperInvariant();
            if (name.Length > Ah64Limits.RouteNameLength)
                name = name[..Ah64Limits.RouteNameLength];
            return new JsonObject()
            {
                ["isEnabled"] = r.IsEnabled && (r.Points.Count > 0),
                ["Name"] = name,
                ["POINTS"] = pts
            };
        }

        private static JsonArray Vertices(DcsTheaters.TheaterProjection proj, IEnumerable<GeoPoint> verts)
        {
            JsonArray list = [];
            foreach (GeoPoint v in verts)
            {
                DcsXY xy = proj.ToXY(v.Lat, v.Lon);
                list.Add(new JsonObject() { ["x"] = Round(xy.X, 8), ["y"] = Round(xy.Y, 8) });
            }
            return list;
        }

        private static JsonObject BuildShape(DcsTheaters.TheaterProjection proj, Ah64Shape s)
        {
            JsonNode captions = null;
            if (!string.IsNullOrEmpty(s.CaptionsJson) && (s.CaptionsVertexKey == s.VertexKey()))
                captions = JsonNode.Parse(s.CaptionsJson);
            captions ??= BuildCaptions(proj, s.Vertices);
            return new JsonObject()
            {
                ["caption_pos"] = captions,
                ["center_pos"] = new JsonObject() { ["x"] = 0, ["y"] = 0 },
                ["note"] = s.Note ?? "",
                ["vertices"] = Vertices(proj, s.Vertices)
            };
        }

        /// <summary>
        /// caption anchors the way the dcs dtc manager lays them out for a new zone: the midpoints of the four sides
        /// of the shape's bounding box (south, west, north, east in dcs x/y) with fixed text offsets.
        /// </summary>
        public static JsonArray BuildCaptions(DcsTheaters.TheaterProjection proj, IList<GeoPoint> verts)
        {
            List<DcsXY> xy = [.. verts.Select(v => proj.ToXY(v.Lat, v.Lon)) ];
            if (xy.Count == 0)
                return [];
            double minX = xy.Min(p => p.X), maxX = xy.Max(p => p.X);
            double minY = xy.Min(p => p.Y), maxY = xy.Max(p => p.Y);
            double midX = (minX + maxX) / 2.0, midY = (minY + maxY) / 2.0;
            static JsonObject Cap(double x, double y, double dx, double dy)
                => new() { ["angle"] = 0, ["txt_dx"] = dx, ["txt_dy"] = dy, ["x"] = Round(x, 8), ["y"] = Round(y, 8) };
            return
            [
                Cap(midX, minY, 4.2, 4.5),
                Cap(minX, midY, -10.8, -10.5),
                Cap(midX, maxY, -25.8, 4.5),
                Cap(maxX, midY, -10.8, 19.5)
            ];
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // json helpers
        //
        // ------------------------------------------------------------------------------------------------------------

        private static double Round(double v, int places) => Math.Round(v, places, MidpointRounding.AwayFromZero);

        private static IEnumerable<JsonNode> Arr(JsonNode n)
            => (n is JsonArray a) ? a.Where(x => x != null) : [];

        private static string Str(JsonNode n)
        {
            if (n is JsonValue v)
                return (v.GetValueKind() == JsonValueKind.String) ? v.GetValue<string>() : v.ToJsonString();
            return (n == null) ? "" : n.ToJsonString();
        }

        // works for values parsed from a file (JsonElement backed) and for values created in memory (int, double,
        // long, ...), which System.Text.Json does not convert between on TryGetValue.
        private static double Dbl(JsonNode n, double dflt = 0.0)
        {
            if (n is JsonValue v)
            {
                switch (v.GetValueKind())
                {
                    case JsonValueKind.Number:
                        return double.Parse(v.ToJsonString(), System.Globalization.NumberStyles.Float,
                                            System.Globalization.CultureInfo.InvariantCulture);
                    case JsonValueKind.String:
                        if (double.TryParse(v.GetValue<string>(), System.Globalization.NumberStyles.Float,
                                            System.Globalization.CultureInfo.InvariantCulture, out double d))
                            return d;
                        break;
                }
            }
            return dflt;
        }

        private static int Int(JsonNode n, int dflt = 0)
        {
            double d = Dbl(n, double.NaN);
            return double.IsNaN(d) ? dflt : (int)Math.Round(d);
        }

        private static bool Bool(JsonNode n, bool dflt)
        {
            if (n is JsonValue v)
            {
                switch (v.GetValueKind())
                {
                    case JsonValueKind.True: return true;
                    case JsonValueKind.False: return false;
                    case JsonValueKind.Number: return Dbl(n) != 0.0;
                }
            }
            return dflt;
        }
    }
}
