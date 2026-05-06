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
     * Implementation for Excel EFFECT() function.
     * Syntax: EFFECT(nominal_rate, npery)
     *
     * Returns the effective annual interest rate given the nominal annual rate
     * and the number of compounding periods per year.
     */
    public class Effect : Fixed2ArgFunction, FreeRefFunction
    {
        public static FreeRefFunction instance = new Effect();

        public override ValueEval Evaluate(int srcRowIndex, int srcColumnIndex, ValueEval nominalRateVE, ValueEval nperyVE)
        {
            double nominalRate;
            int npery;
            try
            {
                ValueEval rateResolved = OperandResolver.GetSingleValue(nominalRateVE, srcRowIndex, srcColumnIndex);
                nominalRate = OperandResolver.CoerceValueToDouble(rateResolved);

                ValueEval nperyResolved = OperandResolver.GetSingleValue(nperyVE, srcRowIndex, srcColumnIndex);
                npery = (int)Math.Floor(OperandResolver.CoerceValueToDouble(nperyResolved));
            }
            catch (EvaluationException e)
            {
                return e.GetErrorEval();
            }

            if (nominalRate <= 0 || npery < 1)
            {
                return ErrorEval.NUM_ERROR;
            }

            return new NumberEval(Math.Pow(1 + nominalRate / npery, npery) - 1);
        }

        public ValueEval Evaluate(ValueEval[] args, OperationEvaluationContext ec)
        {
            if (args.Length != 2)
            {
                return ErrorEval.VALUE_INVALID;
            }
            return Evaluate(ec.RowIndex, ec.ColumnIndex, args[0], args[1]);
        }
    }
}
