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
     * Implementation for Excel ERF() function.
     * Syntax: ERF(lower_limit, [upper_limit])
     *
     * Returns the error function integrated between the limits. With one
     * argument, integrates from 0 to lower_limit. With two arguments, returns
     * ERF(upper) - ERF(lower).
     */
    public class Erf : Var1or2ArgFunction, FreeRefFunction
    {
        public static FreeRefFunction instance = new Erf();

        public override ValueEval Evaluate(int srcRowIndex, int srcColumnIndex, ValueEval arg0)
        {
            return Evaluate(srcRowIndex, srcColumnIndex, arg0, null);
        }

        public override ValueEval Evaluate(int srcRowIndex, int srcColumnIndex, ValueEval lowerVE, ValueEval upperVE)
        {
            double lower;
            try
            {
                ValueEval ve = OperandResolver.GetSingleValue(lowerVE, srcRowIndex, srcColumnIndex);
                lower = OperandResolver.CoerceValueToDouble(ve);
            }
            catch (EvaluationException e)
            {
                return e.GetErrorEval();
            }

            if (upperVE == null)
            {
                return new NumberEval(MathUtils.Erf(lower));
            }

            double upper;
            try
            {
                ValueEval ve = OperandResolver.GetSingleValue(upperVE, srcRowIndex, srcColumnIndex);
                upper = OperandResolver.CoerceValueToDouble(ve);
            }
            catch (EvaluationException e)
            {
                return e.GetErrorEval();
            }

            return new NumberEval(MathUtils.Erf(upper) - MathUtils.Erf(lower));
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
