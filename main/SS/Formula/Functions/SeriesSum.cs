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

    /**
     * Implementation for Excel SERIESSUM() function.
     * Syntax: SERIESSUM(x, n, m, coefficients)
     *
     * Returns the sum of a power series:
     *   a1 * x^n + a2 * x^(n+m) + a3 * x^(n+2m) + ... + ak * x^(n+(k-1)m)
     */
    public class SeriesSum : Fixed4ArgFunction, FreeRefFunction
    {
        public static FreeRefFunction instance = new SeriesSum();

        public override ValueEval Evaluate(int srcRowIndex, int srcColumnIndex,
            ValueEval xVE, ValueEval nVE, ValueEval mVE, ValueEval coefficientsVE)
        {
            double x;
            double n;
            double m;
            double[] coefficients;
            try
            {
                ValueEval xResolved = OperandResolver.GetSingleValue(xVE, srcRowIndex, srcColumnIndex);
                x = OperandResolver.CoerceValueToDouble(xResolved);

                ValueEval nResolved = OperandResolver.GetSingleValue(nVE, srcRowIndex, srcColumnIndex);
                n = OperandResolver.CoerceValueToDouble(nResolved);

                ValueEval mResolved = OperandResolver.GetSingleValue(mVE, srcRowIndex, srcColumnIndex);
                m = OperandResolver.CoerceValueToDouble(mResolved);

                coefficients = AggregateFunction.ValueCollector.CollectValues(coefficientsVE);
            }
            catch (EvaluationException e)
            {
                return e.GetErrorEval();
            }

            if (coefficients.Length == 0)
            {
                return ErrorEval.VALUE_INVALID;
            }

            if (x == 0 && n <= 0)
            {
                return ErrorEval.NUM_ERROR;
            }

            double sum = 0;
            for (int i = 0; i < coefficients.Length; i++)
            {
                double exponent = n + i * m;
                sum += coefficients[i] * Math.Pow(x, exponent);
            }

            if (double.IsNaN(sum) || double.IsInfinity(sum))
            {
                return ErrorEval.NUM_ERROR;
            }
            return new NumberEval(sum);
        }

        public ValueEval Evaluate(ValueEval[] args, OperationEvaluationContext ec)
        {
            if (args.Length != 4)
            {
                return ErrorEval.VALUE_INVALID;
            }
            return Evaluate(ec.RowIndex, ec.ColumnIndex, args[0], args[1], args[2], args[3]);
        }
    }
}
