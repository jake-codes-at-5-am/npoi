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
     * Implementation for Excel ERFC() function.
     * Syntax: ERFC(x)
     *
     * Returns the complementary error function 1 - ERF(x).
     */
    public class ErfC : Fixed1ArgFunction, FreeRefFunction
    {
        public static FreeRefFunction instance = new ErfC();

        public override ValueEval Evaluate(int srcRowIndex, int srcColumnIndex, ValueEval arg0)
        {
            double x;
            try
            {
                ValueEval ve = OperandResolver.GetSingleValue(arg0, srcRowIndex, srcColumnIndex);
                x = OperandResolver.CoerceValueToDouble(ve);
            }
            catch (EvaluationException e)
            {
                return e.GetErrorEval();
            }

            return new NumberEval(MathUtils.Erfc(x));
        }

        public ValueEval Evaluate(ValueEval[] args, OperationEvaluationContext ec)
        {
            if (args.Length != 1)
            {
                return ErrorEval.VALUE_INVALID;
            }
            return Evaluate(ec.RowIndex, ec.ColumnIndex, args[0]);
        }
    }
}
