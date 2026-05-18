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
    using NPOI.SS.Formula.Eval;

    /**
     * Implementation for Excel XOR() function (Excel 2013+).
     * Syntax: XOR(logical1, [logical2], ...)
     *
     * Returns TRUE when an odd number of the supplied logical arguments are
     * TRUE; FALSE otherwise.  Following Excel, numeric values are TRUE if
     * non-zero and arrays/ranges are flattened.
     */
    public class Xor : FreeRefFunction
    {
        public static FreeRefFunction instance = new Xor();

        public ValueEval Evaluate(ValueEval[] args, OperationEvaluationContext ec)
        {
            if (args == null || args.Length == 0)
            {
                // No arguments supplied at all — Excel rejects this with #VALUE!.
                return ErrorEval.VALUE_INVALID;
            }

            int trueCount = 0;
            foreach (ValueEval arg in args)
            {
                if (arg is AreaEval area)
                {
                    for (int r = 0; r < area.Height; r++)
                    {
                        for (int c = 0; c < area.Width; c++)
                        {
                            if (TryAddTruthValue(area.GetRelativeValue(r, c), ref trueCount, out var error))
                            {
                                continue;
                            }
                            return error;
                        }
                    }
                }
                else if (arg is RefEval refEval)
                {
                    if (!TryAddTruthValue(refEval.GetInnerValueEval(refEval.FirstSheetIndex), ref trueCount, out var error))
                    {
                        return error;
                    }
                }
                else
                {
                    if (!TryAddTruthValue(arg, ref trueCount, out var error))
                    {
                        return error;
                    }
                }
            }

            // Excel treats blank/missing inputs as FALSE rather than erroring,
            // so a supplied-but-all-blank arg list naturally falls through here
            // and yields FALSE (trueCount == 0 → even parity). Only the "no
            // args at all" case (handled at the top) returns #VALUE!.
            return (trueCount & 1) == 1 ? BoolEval.TRUE : BoolEval.FALSE;
        }

        private static bool TryAddTruthValue(ValueEval cell, ref int trueCount, out ValueEval error)
        {
            error = null;
            if (cell is BlankEval || cell is MissingArgEval)
            {
                // Excel treats blank / missing cells as FALSE in XOR — they don't
                // contribute to the TRUE count but they also don't cause #VALUE!.
                return true;
            }
            if (cell is BoolEval be)
            {
                if (be.BooleanValue) trueCount++;
                return true;
            }
            if (cell is NumberEval ne)
            {
                if (ne.NumberValue != 0) trueCount++;
                return true;
            }
            if (cell is ErrorEval ee)
            {
                error = ee;
                return false;
            }
            if (cell is StringEval se)
            {
                if (bool.TryParse(se.StringValue, out bool parsed))
                {
                    if (parsed) trueCount++;
                    return true;
                }
                error = ErrorEval.VALUE_INVALID;
                return false;
            }
            error = ErrorEval.VALUE_INVALID;
            return false;
        }
    }
}
