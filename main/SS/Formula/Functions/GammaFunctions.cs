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
    using NPOI.SS.Formula.Eval;

    /// <summary>
    /// Lanczos approximation of the gamma and log-gamma functions, accurate to
    /// roughly 15 significant digits for x &gt; 0.5. Handles the reflection
    /// formula for the small range so that Gamma(x) is defined for all real x
    /// except non-positive integers.
    /// </summary>
    internal static class GammaApproximation
    {
        // Standard Lanczos coefficients (g = 7, n = 9) — same constants used by
        // boost, scipy and many reference implementations.
        private static readonly double[] LanczosG7 =
        {
            0.99999999999980993,
            676.5203681218851,
            -1259.1392167224028,
            771.32342877765313,
            -176.61502916214059,
            12.507343278686905,
            -0.13857109526572012,
            9.9843695780195716e-6,
            1.5056327351493116e-7
        };

        internal static double LogGamma(double x)
        {
            if (x < 0.5)
            {
                return Math.Log(Math.PI / Math.Sin(Math.PI * x)) - LogGamma(1 - x);
            }

            x -= 1;
            double a = LanczosG7[0];
            const double t = 7.5;
            for (int i = 1; i < LanczosG7.Length; i++)
            {
                a += LanczosG7[i] / (x + i);
            }
            return 0.5 * Math.Log(2 * Math.PI) + (x + 0.5) * Math.Log(x + t) - (x + t) + Math.Log(a);
        }

        internal static double Gamma(double x)
        {
            if (x == Math.Floor(x) && x <= 0)
            {
                return double.NaN;
            }
            return Math.Exp(LogGamma(x));
        }
    }

    /**
     * GAMMA(x) — gamma function (Excel 2013+).
     */
    public class Gamma : Fixed1ArgFunction, FreeRefFunction
    {
        public static FreeRefFunction instance = new Gamma();

        public override ValueEval Evaluate(int srcRowIndex, int srcColumnIndex, ValueEval arg0)
        {
            double x;
            try
            {
                x = OperandResolver.CoerceValueToDouble(OperandResolver.GetSingleValue(arg0, srcRowIndex, srcColumnIndex));
            }
            catch (EvaluationException e)
            {
                return e.GetErrorEval();
            }
            double value = GammaApproximation.Gamma(x);
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                return ErrorEval.NUM_ERROR;
            }
            return new NumberEval(value);
        }

        public ValueEval Evaluate(ValueEval[] args, OperationEvaluationContext ec)
        {
            return args.Length != 1 ? ErrorEval.VALUE_INVALID : Evaluate(ec.RowIndex, ec.ColumnIndex, args[0]);
        }
    }

    /**
     * GAMMALN.PRECISE(x) — natural log of |Gamma(x)| for positive x. Excel
     * differs from GAMMALN only in calling out that the result uses
     * double-precision (not single).
     */
    public class GammaLnPrecise : Fixed1ArgFunction, FreeRefFunction
    {
        public static FreeRefFunction instance = new GammaLnPrecise();

        public override ValueEval Evaluate(int srcRowIndex, int srcColumnIndex, ValueEval arg0)
        {
            double x;
            try
            {
                x = OperandResolver.CoerceValueToDouble(OperandResolver.GetSingleValue(arg0, srcRowIndex, srcColumnIndex));
            }
            catch (EvaluationException e)
            {
                return e.GetErrorEval();
            }
            if (x <= 0)
            {
                return ErrorEval.NUM_ERROR;
            }
            return new NumberEval(GammaApproximation.LogGamma(x));
        }

        public ValueEval Evaluate(ValueEval[] args, OperationEvaluationContext ec)
        {
            return args.Length != 1 ? ErrorEval.VALUE_INVALID : Evaluate(ec.RowIndex, ec.ColumnIndex, args[0]);
        }
    }
}
