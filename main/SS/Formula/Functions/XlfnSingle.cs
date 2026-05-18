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
     * Implementation for Excel _xlfn.SINGLE() (the implicit-intersection "@"
     * operator emitted by Microsoft 365 when a workbook is opened in a
     * pre-dynamic-array context).
     *
     * SINGLE returns the single value at the intersection of the supplied
     * reference and the calling cell's row/column. For a scalar argument it is
     * an identity passthrough. POI does not maintain a spilled-array model, so
     * we resolve the argument to its single value (the same behaviour Excel
     * exhibits for non-array contexts) which is sufficient to evaluate workbooks
     * that previously raised NotImplementedFunctionException.
     */
    public class XlfnSingle : Fixed1ArgFunction, FreeRefFunction
    {
        public static FreeRefFunction instance = new XlfnSingle();

        public override ValueEval Evaluate(int srcRowIndex, int srcColumnIndex, ValueEval arg0)
        {
            try
            {
                return OperandResolver.GetSingleValue(arg0, srcRowIndex, srcColumnIndex);
            }
            catch (EvaluationException e)
            {
                return e.GetErrorEval();
            }
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
