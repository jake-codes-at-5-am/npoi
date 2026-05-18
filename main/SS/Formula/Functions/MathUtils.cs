/* ====================================================================
   Licensed to the Apache Software Foundation (ASF) under one or more
   contributor license agreements.  See the NOTICE file distributed with
   this work for additional information regarding copyright ownership.
   The ASF licenses this file to You under the Apache License, Version 2.0
   (the "License"); you may not use this file except in compliance with
   the License.  You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
==================================================================== */

namespace NPOI.SS.Formula.Functions
{
    using System;

    /// <summary>
    /// Numeric helpers shared by several formula functions (GCD, LCM, MULTINOMIAL,
    /// ERF/ERFC, etc.). These are intentionally allocation-free and stateless so
    /// they can be reused inside hot evaluation paths without leaking memory.
    /// </summary>
    internal static class MathUtils
    {
        internal static long Gcd(long a, long b)
        {
            a = Math.Abs(a);
            b = Math.Abs(b);
            while (b != 0)
            {
                long temp = b;
                b = a % b;
                a = temp;
            }
            return a;
        }

        internal static long Lcm(long a, long b)
        {
            if (a == 0 || b == 0)
            {
                return 0;
            }
            return Math.Abs(a / Gcd(a, b) * b);
        }

        /// <summary>
        /// Abramowitz &amp; Stegun 7.1.26 polynomial approximation of erf(x).
        /// Maximum absolute error ~1.5e-7, which matches Excel's reported precision
        /// for the engineering-toolpack ERF function.
        /// </summary>
        internal static double Erf(double x)
        {
            const double a1 = 0.254829592;
            const double a2 = -0.284496736;
            const double a3 = 1.421413741;
            const double a4 = -1.453152027;
            const double a5 = 1.061405429;
            const double p = 0.3275911;

            int sign = x < 0 ? -1 : 1;
            double absX = Math.Abs(x);
            double t = 1.0 / (1.0 + p * absX);
            double y = 1.0 - (((((a5 * t + a4) * t) + a3) * t + a2) * t + a1) * t * Math.Exp(-absX * absX);
            return sign * y;
        }

        internal static double Erfc(double x)
        {
            return 1.0 - Erf(x);
        }
    }
}
