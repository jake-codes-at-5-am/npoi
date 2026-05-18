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
     * Implementation for Excel GESTEP() function.
     * Syntax: GESTEP(number, [step])
     *
     * Returns 1 if number &gt;= step; otherwise returns 0. The step argument
     * defaults to zero when omitted.
     */
    public class GeStep : Var1or2ArgFunction, FreeRefFunction
    {
        public static FreeRefFunction instance = new GeStep();

        private static readonly NumberEval ONE = new NumberEval(1);
        private static readonly NumberEval ZERO = new NumberEval(0);

        public override ValueEval Evaluate(int srcRowIndex, int srcColumnIndex, ValueEval arg0)
        {
            return Evaluate(srcRowIndex, srcColumnIndex, arg0, null);
        }

        public override ValueEval Evaluate(int srcRowIndex, int srcColumnIndex, ValueEval numberVE, ValueEval stepVE)
        {
            double number;
            double step = 0;
            try
            {
                ValueEval ve = OperandResolver.GetSingleValue(numberVE, srcRowIndex, srcColumnIndex);
                number = OperandResolver.CoerceValueToDouble(ve);

                if (stepVE != null)
                {
                    ValueEval stepEval = OperandResolver.GetSingleValue(stepVE, srcRowIndex, srcColumnIndex);
                    if (!(stepEval is BlankEval))
                    {
                        step = OperandResolver.CoerceValueToDouble(stepEval);
                    }
                }
            }
            catch (EvaluationException e)
            {
                return e.GetErrorEval();
            }

            return number >= step ? ONE : ZERO;
        }

        public ValueEval Evaluate(ValueEval[] args, OperationEvaluationContext ec)
        {
            switch (args.Length)
            {
                case 1:
                    return Evaluate(ec.RowIndex, ec.ColumnIndex, args[0]);
                case 2:
                    return Evaluate(ec.RowIndex, ec.ColumnIndex, args[0], args[1]);
                default:
                    return ErrorEval.VALUE_INVALID;
            }
        }
    }
}
