// ********************************************************************************************************************
//
// TransverseMercator.cs : ellipsoidal transverse mercator (Krüger series, 6th order) on WGS-84
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

namespace JAFDTC.AH64D.Geo
{
    /// <summary>
    /// ellipsoidal transverse mercator projection using the 6th-order Krüger series (Karney 2011). accuracy is
    /// well under a millimeter within a few thousand kilometers of the central meridian, which covers every dcs
    /// theater and every utm zone. used both for the dcs theater x/y projections and for utm/mgrs.
    /// </summary>
    public sealed class TransverseMercator
    {
        // WGS-84
        public const double A = 6378137.0;
        public const double F = 1.0 / 298.257223563;

        private static readonly double N = F / (2.0 - F);
        private static readonly double E = Math.Sqrt(F * (2.0 - F));
        private static readonly double E2 = F * (2.0 - F);
        private static readonly double RectA;                       // rectifying radius, A in Karney's notation
        private static readonly double[] Alpha = new double[7];     // 1-based
        private static readonly double[] Beta = new double[7];      // 1-based

        public double CentralMeridianDeg { get; }
        public double ScaleFactor { get; }
        public double FalseEasting { get; }
        public double FalseNorthing { get; }

        static TransverseMercator()
        {
            double n = N, n2 = n * n, n3 = n2 * n, n4 = n3 * n, n5 = n4 * n, n6 = n5 * n;

            RectA = A / (1.0 + n) * (1.0 + n2 / 4.0 + n4 / 64.0 + n6 / 256.0);

            Alpha[1] = n / 2.0 - 2.0 * n2 / 3.0 + 5.0 * n3 / 16.0 + 41.0 * n4 / 180.0 - 127.0 * n5 / 288.0
                       + 7891.0 * n6 / 37800.0;
            Alpha[2] = 13.0 * n2 / 48.0 - 3.0 * n3 / 5.0 + 557.0 * n4 / 1440.0 + 281.0 * n5 / 630.0
                       - 1983433.0 * n6 / 1935360.0;
            Alpha[3] = 61.0 * n3 / 240.0 - 103.0 * n4 / 140.0 + 15061.0 * n5 / 26880.0 + 167603.0 * n6 / 181440.0;
            Alpha[4] = 49561.0 * n4 / 161280.0 - 179.0 * n5 / 168.0 + 6601661.0 * n6 / 7257600.0;
            Alpha[5] = 34729.0 * n5 / 80640.0 - 3418889.0 * n6 / 1995840.0;
            Alpha[6] = 212378941.0 * n6 / 319334400.0;

            Beta[1] = n / 2.0 - 2.0 * n2 / 3.0 + 37.0 * n3 / 96.0 - n4 / 360.0 - 81.0 * n5 / 512.0
                      + 96199.0 * n6 / 604800.0;
            Beta[2] = n2 / 48.0 + n3 / 15.0 - 437.0 * n4 / 1440.0 + 46.0 * n5 / 105.0 - 1118711.0 * n6 / 3870720.0;
            Beta[3] = 17.0 * n3 / 480.0 - 37.0 * n4 / 840.0 - 209.0 * n5 / 4480.0 + 5569.0 * n6 / 90720.0;
            Beta[4] = 4397.0 * n4 / 161280.0 - 11.0 * n5 / 504.0 - 830251.0 * n6 / 7257600.0;
            Beta[5] = 4583.0 * n5 / 161280.0 - 108847.0 * n6 / 3991680.0;
            Beta[6] = 20648693.0 * n6 / 638668800.0;
        }

        public TransverseMercator(double centralMeridianDeg, double scaleFactor, double falseEasting,
                                  double falseNorthing)
            => (CentralMeridianDeg, ScaleFactor, FalseEasting, FalseNorthing)
                = (centralMeridianDeg, scaleFactor, falseEasting, falseNorthing);

        /// <summary>
        /// project latitude/longitude (decimal degrees, WGS-84) to easting/northing (meters).
        /// </summary>
        public (double Easting, double Northing) Forward(double latDeg, double lonDeg)
        {
            double phi = latDeg * Math.PI / 180.0;
            double lambda = NormalizeLonDeg(lonDeg - CentralMeridianDeg) * Math.PI / 180.0;

            double cosL = Math.Cos(lambda);
            double sinL = Math.Sin(lambda);

            double tau = Math.Tan(phi);
            double sigma = Math.Sinh(E * Atanh(E * tau / Math.Sqrt(1.0 + tau * tau)));
            double tauP = tau * Math.Sqrt(1.0 + sigma * sigma) - sigma * Math.Sqrt(1.0 + tau * tau);

            double xiP = Math.Atan2(tauP, cosL);
            double etaP = Asinh(sinL / Math.Sqrt(tauP * tauP + cosL * cosL));

            double xi = xiP;
            double eta = etaP;
            for (int j = 1; j <= 6; j++)
            {
                xi += Alpha[j] * Math.Sin(2 * j * xiP) * Math.Cosh(2 * j * etaP);
                eta += Alpha[j] * Math.Cos(2 * j * xiP) * Math.Sinh(2 * j * etaP);
            }

            double easting = FalseEasting + ScaleFactor * RectA * eta;
            double northing = FalseNorthing + ScaleFactor * RectA * xi;
            return (easting, northing);
        }

        /// <summary>
        /// unproject easting/northing (meters) to latitude/longitude (decimal degrees, WGS-84).
        /// </summary>
        public (double LatDeg, double LonDeg) Inverse(double easting, double northing)
        {
            double xi = (northing - FalseNorthing) / (ScaleFactor * RectA);
            double eta = (easting - FalseEasting) / (ScaleFactor * RectA);

            double xiP = xi;
            double etaP = eta;
            for (int j = 1; j <= 6; j++)
            {
                xiP -= Beta[j] * Math.Sin(2 * j * xi) * Math.Cosh(2 * j * eta);
                etaP -= Beta[j] * Math.Cos(2 * j * xi) * Math.Sinh(2 * j * eta);
            }

            double sinhEtaP = Math.Sinh(etaP);
            double sinXiP = Math.Sin(xiP);
            double cosXiP = Math.Cos(xiP);

            double tauP = sinXiP / Math.Sqrt(sinhEtaP * sinhEtaP + cosXiP * cosXiP);

            // newton-raphson for tau from tau' (Karney 2011, eqs 19-21).
            double tau = tauP;
            for (int i = 0; i < 20; i++)
            {
                double sigma = Math.Sinh(E * Atanh(E * tau / Math.Sqrt(1.0 + tau * tau)));
                double tauI = tau * Math.Sqrt(1.0 + sigma * sigma) - sigma * Math.Sqrt(1.0 + tau * tau);
                double dTau = (tauP - tauI) / Math.Sqrt(1.0 + tauI * tauI)
                              * (1.0 + (1.0 - E2) * tau * tau) / ((1.0 - E2) * Math.Sqrt(1.0 + tau * tau));
                tau += dTau;
                if (Math.Abs(dTau) < 1e-14)
                    break;
            }

            double phi = Math.Atan(tau);
            double lambda = Math.Atan2(sinhEtaP, cosXiP);

            return (phi * 180.0 / Math.PI, NormalizeLonDeg(CentralMeridianDeg + lambda * 180.0 / Math.PI));
        }

        private static double NormalizeLonDeg(double lon)
        {
            while (lon > 180.0) lon -= 360.0;
            while (lon <= -180.0) lon += 360.0;
            return lon;
        }

        private static double Atanh(double x) => 0.5 * Math.Log((1.0 + x) / (1.0 - x));

        private static double Asinh(double x) => Math.Log(x + Math.Sqrt(x * x + 1.0));
    }
}
