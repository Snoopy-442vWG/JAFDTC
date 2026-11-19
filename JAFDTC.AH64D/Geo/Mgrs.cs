// ********************************************************************************************************************
//
// Mgrs.cs : WGS-84 utm / mgrs conversion
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

using System.Globalization;
using System.Text.RegularExpressions;

namespace JAFDTC.AH64D.Geo
{
    /// <summary>
    /// WGS-84 mgrs (military grid reference system) conversion. supports the norway / svalbard zone exceptions and
    /// both hemispheres. mgrs references are truncated (not rounded) to the requested precision, as the standard
    /// requires; a parsed reference resolves to the south-west corner of its grid square, which is the position
    /// an AH-64D crew gets when they key the same 8-digit grid into the KU.
    /// </summary>
    public static partial class Mgrs
    {
        private const string BandLetters = "CDEFGHJKLMNPQRSTUVWXX";
        private static readonly string[] ColLetters = ["ABCDEFGH", "JKLMNPQR", "STUVWXYZ"];
        private const string RowLetters = "ABCDEFGHJKLMNPQRSTUV";

        [GeneratedRegex(@"^\s*(\d{1,2})\s*([C-HJ-NP-X])\s*([A-HJ-NP-Z])\s*([A-HJ-NP-V])\s*(\d*)\s*(\d*)\s*$",
                        RegexOptions.IgnoreCase)]
        private static partial Regex MgrsRegex();

        public readonly record struct Utm(int Zone, char Band, bool IsSouth, double Easting, double Northing);

        // ------------------------------------------------------------------------------------------------------------
        //
        // utm
        //
        // ------------------------------------------------------------------------------------------------------------

        public static int ZoneFor(double lat, double lon)
        {
            lon = ((lon + 180.0) % 360.0 + 360.0) % 360.0 - 180.0;
            int zone = (int)Math.Floor((lon + 180.0) / 6.0) + 1;
            if (zone > 60)
                zone = 60;

            // norway
            if ((lat >= 56.0) && (lat < 64.0) && (lon >= 3.0) && (lon < 12.0))
                zone = 32;

            // svalbard
            if ((lat >= 72.0) && (lat < 84.0))
            {
                if ((lon >= 0.0) && (lon < 9.0)) zone = 31;
                else if ((lon >= 9.0) && (lon < 21.0)) zone = 33;
                else if ((lon >= 21.0) && (lon < 33.0)) zone = 35;
                else if ((lon >= 33.0) && (lon < 42.0)) zone = 37;
            }
            return zone;
        }

        public static char BandFor(double lat)
        {
            if ((lat < -80.0) || (lat > 84.0))
                throw new ArgumentOutOfRangeException(nameof(lat), "MGRS is defined from 80S to 84N");
            int index = (int)Math.Floor((lat + 80.0) / 8.0);
            return BandLetters[Math.Min(index, 20)];
        }

        private static TransverseMercator UtmProjection(int zone, bool isSouth)
            => new((zone - 1) * 6 - 180 + 3, 0.9996, 500000.0, (isSouth) ? 10000000.0 : 0.0);

        public static Utm ToUtm(double lat, double lon, int forceZone = 0)
        {
            int zone = (forceZone > 0) ? forceZone : ZoneFor(lat, lon);
            bool isSouth = lat < 0.0;
            (double e, double n) = UtmProjection(zone, isSouth).Forward(lat, lon);
            return new Utm(zone, BandFor(lat), isSouth, e, n);
        }

        public static (double Lat, double Lon) FromUtm(int zone, bool isSouth, double easting, double northing)
            => UtmProjection(zone, isSouth).Inverse(easting, northing);

        // ------------------------------------------------------------------------------------------------------------
        //
        // mgrs
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// format lat/lon as an mgrs reference with the given total number of digits (2..10, even). the AH-64D
        /// uses 8 digits ("32U LV 9850 7696").
        /// </summary>
        public static string FromLatLon(double lat, double lon, int digits = 8)
        {
            if ((digits < 2) || (digits > 10) || ((digits % 2) != 0))
                throw new ArgumentOutOfRangeException(nameof(digits));

            Utm utm = ToUtm(lat, lon);

            // guard against floating point leaving us at x99999.9999999 after truncation edge cases.
            double e = Math.Floor(utm.Easting + 1e-7);
            double n = Math.Floor(utm.Northing + 1e-7);

            int col = (int)Math.Floor(e / 100000.0);
            char colLetter = ColLetters[(utm.Zone - 1) % 3][col - 1];

            int row = (int)Math.Floor(n / 100000.0) % 20;
            if ((utm.Zone % 2) == 0)
                row = (row + 5) % 20;
            char rowLetter = RowLetters[row];

            int half = digits / 2;
            double scale = Math.Pow(10, 5 - half);
            long eDigits = (long)Math.Floor((e % 100000.0) / scale);
            long nDigits = (long)Math.Floor((n % 100000.0) / scale);

            string fmt = new('0', half);
            return $"{utm.Zone}{utm.Band} {colLetter}{rowLetter} " +
                   $"{eDigits.ToString(fmt, CultureInfo.InvariantCulture)} " +
                   $"{nDigits.ToString(fmt, CultureInfo.InvariantCulture)}";
        }

        /// <summary>
        /// returns true if the string is a syntactically valid mgrs reference with a grid zone designator.
        /// </summary>
        public static bool IsValid(string mgrs) => TryParse(mgrs, out _, out _);

        /// <summary>
        /// parse an mgrs reference ("32U LV 9850 7696", "32ULV98507696", 2 to 10 digits) into lat/lon of the
        /// south-west corner of the referenced grid square. returns false on syntax or range errors.
        /// </summary>
        public static bool TryParse(string mgrs, out double lat, out double lon)
        {
            lat = lon = double.NaN;
            if (string.IsNullOrWhiteSpace(mgrs))
                return false;

            Match m = MgrsRegex().Match(mgrs.ToUpperInvariant());
            if (!m.Success)
                return false;

            int zone = int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
            if ((zone < 1) || (zone > 60))
                return false;
            char band = m.Groups[2].Value[0];
            char colLetter = m.Groups[3].Value[0];
            char rowLetter = m.Groups[4].Value[0];

            string digits = m.Groups[5].Value + m.Groups[6].Value;
            if ((digits.Length % 2) != 0 || (digits.Length > 10))
                return false;
            int half = digits.Length / 2;
            double scale = Math.Pow(10, 5 - half);
            double eIn = (half == 0) ? 0.0 : double.Parse(digits[..half], CultureInfo.InvariantCulture) * scale;
            double nIn = (half == 0) ? 0.0 : double.Parse(digits[half..], CultureInfo.InvariantCulture) * scale;

            int col = ColLetters[(zone - 1) % 3].IndexOf(colLetter);
            if (col < 0)
                return false;
            double e100k = (col + 1) * 100000.0;

            int row = RowLetters.IndexOf(rowLetter);
            if (row < 0)
                return false;
            if ((zone % 2) == 0)
                row = (row - 5 + 20) % 20;
            double n100k = row * 100000.0;

            int bandIndex = BandLetters.IndexOf(band);
            if (bandIndex < 0)
                return false;
            double latBand = -80.0 + bandIndex * 8.0;
            bool isSouth = band < 'N';

            // northing of the band's southern edge on the central meridian is the smallest northing any point in
            // the band can have, so step up by 2,000 km cycles until we reach it.
            TransverseMercator utmProj = UtmProjection(zone, isSouth);
            double nBand = utmProj.Forward(latBand, (zone - 1) * 6 - 180 + 3).Northing;
            nBand = Math.Floor(nBand / 100000.0) * 100000.0;

            double northing = n100k + nIn;
            while (northing < nBand)
                northing += 2000000.0;
            double easting = e100k + eIn;

            (lat, lon) = utmProj.Inverse(easting, northing);

            // sanity: the result has to land in (or right next to) the band the reference claims.
            if ((lat < latBand - 0.5) || (lat > latBand + ((band == 'X') ? 12.0 : 8.0) + 0.5))
                return false;
            return true;
        }

        /// <summary>
        /// normalize an mgrs string to "ZZB CR EEEE NNNN" form with the given digits. returns null if invalid.
        /// </summary>
        public static string Normalize(string mgrs, int digits = 8)
            => TryParse(mgrs, out double lat, out double lon) ? FromLatLon(lat, lon, digits) : null;
    }
}
