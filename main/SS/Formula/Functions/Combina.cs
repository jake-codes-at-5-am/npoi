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
     * COMBINA(number, number_chosen) — combinations with repetition.
     * = C(number + number_chosen - 1, number_chosen).
     */
    public class Combina : Fixed2ArgFunction, FreeRefFunction
    {
        public static FreeRefFunction instance = new Combina();

        public override ValueEval Evaluate(int srcRowIndex, int srcColumnIndex, ValueEval arg0, ValueEval arg1)
        {
            double n;
            double k;
            try
            {
                n = OperandResolver.CoerceValueToDouble(OperandResolver.GetSingleValue(arg0, srcRowIndex, srcColumnIndex));
                k = OperandResolver.CoerceValueToDouble(OperandResolver.GetSingleValue(arg1, srcRowIndex, srcColumnIndex));
            }
            catch (EvaluationException e)
            {
                return e.GetErrorEval();
            }
            if (n < 0 || k < 0 || double.IsNaN(n) || double.IsNaN(k))
            {
                return ErrorEval.NUM_ERROR;
            }
            int nInt = (int)Math.Floor(n);
            int kInt = (int)Math.Floor(k);
            if (nInt == 0 && kInt == 0)
            {
                return new NumberEval(1);
            }
            if (nInt == 0)
            {
                return ErrorEval.NUM_ERROR;
            }
            return new NumberEval(MathX.NChooseK(nInt + kInt - 1, kInt));
        }

        public ValueEval Evaluate(ValueEval[] args, OperationEvaluationContext ec)
        {
            return args.Length != 2 ? ErrorEval.VALUE_INVALID : Evaluate(ec.RowIndex, ec.ColumnIndex, args[0], args[1]);
        }
    }

    /**
     * PERMUTATIONA(number, number_chosen) — permutations with repetition.
     * = number ^ number_chosen.
     */
    public class PermutationA : Fixed2ArgFunction, FreeRefFunction
    {
        public static FreeRefFunction instance = new PermutationA();

        public override ValueEval Evaluate(int srcRowIndex, int srcColumnIndex, ValueEval arg0, ValueEval arg1)
        {
            double n;
            double k;
            try
            {
                n = OperandResolver.CoerceValueToDouble(OperandResolver.GetSingleValue(arg0, srcRowIndex, srcColumnIndex));
                k = OperandResolver.CoerceValueToDouble(OperandResolver.GetSingleValue(arg1, srcRowIndex, srcColumnIndex));
            }
            catch (EvaluationException e)
            {
                return e.GetErrorEval();
            }
            if (n < 0 || k < 0 || double.IsNaN(n) || double.IsNaN(k))
            {
                return ErrorEval.NUM_ERROR;
            }
            double result = Math.Pow(Math.Floor(n), Math.Floor(k));
            if (double.IsInfinity(result) || double.IsNaN(result))
            {
                return ErrorEval.NUM_ERROR;
            }
            return new NumberEval(result);
        }

        public ValueEval Evaluate(ValueEval[] args, OperationEvaluationContext ec)
        {
            return args.Length != 2 ? ErrorEval.VALUE_INVALID : Evaluate(ec.RowIndex, ec.ColumnIndex, args[0], args[1]);
        }
    }
}
