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
     * Implementation for Excel MULTINOMIAL() function.
     * Syntax: MULTINOMIAL(number1, [number2], ...)
     *
     * Returns the multinomial coefficient (sum a_i)! / prod(a_i!).
     * All arguments must be non-negative integers; non-integer inputs are
     * truncated to match Excel's documented behaviour.
     */
    public class Multinomial : FreeRefFunction
    {
        public static FreeRefFunction instance = new Multinomial();

        public ValueEval Evaluate(ValueEval[] args, OperationEvaluationContext ec)
        {
            if (args == null || args.Length == 0)
            {
                return ErrorEval.VALUE_INVALID;
            }

            double[] values;
            try
            {
                values = AggregateFunction.ValueCollector.CollectValues(args);
            }
            catch (EvaluationException e)
            {
                return e.GetErrorEval();
            }

            if (values.Length == 0)
            {
                return ErrorEval.VALUE_INVALID;
            }

            long sum = 0;
            double denominator = 1.0;
            foreach (double raw in values)
            {
                if (double.IsNaN(raw) || double.IsInfinity(raw) || raw < 0)
                {
                    return ErrorEval.NUM_ERROR;
                }
                int truncated = (int)Math.Floor(raw);
                sum += truncated;
                if (sum > int.MaxValue)
                {
                    return ErrorEval.NUM_ERROR;
                }
                denominator *= MathX.Factorial(truncated);
            }

            double numerator = MathX.Factorial((int)sum);
            double result = numerator / denominator;
            if (double.IsNaN(result) || double.IsInfinity(result))
            {
                return ErrorEval.NUM_ERROR;
            }
            return new NumberEval(result);
        }
    }
}
