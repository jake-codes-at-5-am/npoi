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
     * Implementation for Excel CUMIPMT() function.
     * Syntax: CUMIPMT(rate, nper, pv, start_period, end_period, type)
     *
     * Returns the cumulative interest paid on a loan between two periods
     * (inclusive). type=0 means payments are due at the end of each period,
     * type=1 means at the beginning.
     */
    public class CumIPmt : FreeRefFunction
    {
        public static FreeRefFunction instance = new CumIPmt();

        public ValueEval Evaluate(ValueEval[] args, OperationEvaluationContext ec)
        {
            return CumulativePaymentSolver.Evaluate(args, ec, isInterest: true);
        }
    }
}
