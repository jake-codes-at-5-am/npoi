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
     * Implementation for Excel IMABS() function.
     * Syntax: IMABS(inumber)
     *
     * Returns the absolute value (modulus) of a complex number expressed in
     * x + yi or x + yj text form.
     */
    public class ImAbs : Fixed1ArgFunction, FreeRefFunction
    {
        public static FreeRefFunction instance = new ImAbs();

        public override ValueEval Evaluate(int srcRowIndex, int srcColumnIndex, ValueEval arg0)
        {
            if (!ComplexNumberParts.TryEvaluateArg(arg0, srcRowIndex, srcColumnIndex, out var parts, out var error))
            {
                return error;
            }
            return new NumberEval(Math.Sqrt(parts.Real * parts.Real + parts.Imaginary * parts.Imaginary));
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
