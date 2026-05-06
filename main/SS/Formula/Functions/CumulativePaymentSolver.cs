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
    /// Shared body for CUMIPMT and CUMPRINC. Both walk the same amortization
    /// schedule between [start_period, end_period] and only differ in whether
    /// they accumulate the interest or principal portion of each payment.
    /// </summary>
    internal static class CumulativePaymentSolver
    {
        internal static ValueEval Evaluate(ValueEval[] args, OperationEvaluationContext ec, bool isInterest)
        {
            if (args == null || args.Length != 6)
            {
                return ErrorEval.VALUE_INVALID;
            }

            double rate;
            int nper;
            double pv;
            int start;
            int end;
            int type;
            try
            {
                rate = ResolveDouble(args[0], ec);
                nper = (int)Math.Floor(ResolveDouble(args[1], ec));
                pv = ResolveDouble(args[2], ec);
                start = (int)Math.Floor(ResolveDouble(args[3], ec));
                end = (int)Math.Floor(ResolveDouble(args[4], ec));
                type = (int)Math.Floor(ResolveDouble(args[5], ec));
            }
            catch (EvaluationException e)
            {
                return e.GetErrorEval();
            }

            if (rate <= 0 || nper <= 0 || pv <= 0 || start < 1 || end < 1
                || start > end || end > nper || (type != 0 && type != 1))
            {
                return ErrorEval.NUM_ERROR;
            }

            double total = 0;
            for (int period = start; period <= end; period++)
            {
                double portion = isInterest
                    ? Finance.IPMT(rate, period, nper, pv, 0, type)
                    : Finance.PPMT(rate, period, nper, pv, 0, type);
                total += portion;
            }
            return new NumberEval(total);
        }

        private static double ResolveDouble(ValueEval arg, OperationEvaluationContext ec)
        {
            ValueEval ve = OperandResolver.GetSingleValue(arg, ec.RowIndex, ec.ColumnIndex);
            return OperandResolver.CoerceValueToDouble(ve);
        }
    }
}
