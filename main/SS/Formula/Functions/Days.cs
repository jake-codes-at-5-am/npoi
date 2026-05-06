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
     * Implementation for Excel DAYS() function (Excel 2013+).
     * Syntax: DAYS(end_date, start_date)
     *
     * Returns the integer number of days between two dates.  Both arguments are
     * Excel date serials; the result is positive when end_date follows
     * start_date.
     */
    public class Days : Fixed2ArgFunction, FreeRefFunction
    {
        public static FreeRefFunction instance = new Days();

        public override ValueEval Evaluate(int srcRowIndex, int srcColumnIndex, ValueEval endDateVE, ValueEval startDateVE)
        {
            double end;
            double start;
            try
            {
                end = NumericFunction.SingleOperandEvaluate(endDateVE, srcRowIndex, srcColumnIndex);
                start = NumericFunction.SingleOperandEvaluate(startDateVE, srcRowIndex, srcColumnIndex);
            }
            catch (EvaluationException e)
            {
                return e.GetErrorEval();
            }
            return new NumberEval((long)Math.Floor(end) - (long)Math.Floor(start));
        }

        public ValueEval Evaluate(ValueEval[] args, OperationEvaluationContext ec)
        {
            return args.Length != 2 ? ErrorEval.VALUE_INVALID : Evaluate(ec.RowIndex, ec.ColumnIndex, args[0], args[1]);
        }
    }
}
