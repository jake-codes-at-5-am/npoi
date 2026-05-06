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
     * Implementation for Excel LCM() function.
     * Syntax: LCM(number1, [number2], ...)
     *
     * Returns the least common multiple of one or more positive integers.
     * Each argument is truncated to an integer; negative values produce #NUM!.
     * If any argument is zero, the result is zero (matches Excel).
     */
    public class Lcm : FreeRefFunction
    {
        public static FreeRefFunction instance = new Lcm();

        public ValueEval Evaluate(ValueEval[] args, OperationEvaluationContext ec)
        {
            if (args == null || args.Length == 0)
            {
                return ErrorEval.VALUE_INVALID;
            }

            long result = 1L;
            bool hasZero = false;
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
                    if (current == 0)
                    {
                        hasZero = true;
                        continue;
                    }
                    result = MathUtils.Lcm(result, current);
                    if (result < 0)
                    {
                        // overflow guard
                        return ErrorEval.NUM_ERROR;
                    }
                }
            }
            catch (EvaluationException e)
            {
                return e.GetErrorEval();
            }

            return new NumberEval(hasZero ? 0 : result);
        }
    }
}
