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
     * Implementation for Excel GCD() function.
     * Syntax: GCD(number1, [number2], ...)
     *
     * Returns the greatest common divisor of one or more positive integers.
     * Each argument is truncated to an integer; negative values produce #NUM!.
     */
    public class Gcd : FreeRefFunction
    {
        public static FreeRefFunction instance = new Gcd();

        public ValueEval Evaluate(ValueEval[] args, OperationEvaluationContext ec)
        {
            if (args == null || args.Length == 0)
            {
                return ErrorEval.VALUE_INVALID;
            }

            long result = 0L;
            try
            {
                double[] values = AggregateFunction.ValueCollector.CollectValues(args);
                if (values.Length == 0)
                {
                    return ErrorEval.VALUE_INVALID;
                }
                foreach (double raw in values)
                {
                    if (double.IsNaN(raw) || double.IsInfinity(raw) || raw < 0)
                    {
                        return ErrorEval.NUM_ERROR;
                    }
                    long current = (long)Math.Floor(raw);
                    result = result == 0 ? current : MathUtils.Gcd(result, current);
                }
            }
            catch (EvaluationException e)
            {
                return e.GetErrorEval();
            }

            return new NumberEval(result);
        }
    }
}
