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
     * Implementation for Excel IMCOS() function.
     * Syntax: IMCOS(inumber)
     *
     * Returns the cosine of a complex number expressed in x + yi or x + yj text
     * form. Identity used: cos(a+bi) = cos(a)cosh(b) - i*sin(a)sinh(b).
     */
    public class ImCos : Fixed1ArgFunction, FreeRefFunction
    {
        public static FreeRefFunction instance = new ImCos();

        public override ValueEval Evaluate(int srcRowIndex, int srcColumnIndex, ValueEval arg0)
        {
            if (!ComplexNumberParts.TryEvaluateArg(arg0, srcRowIndex, srcColumnIndex, out var parts, out var error))
            {
                return error;
            }
            double real = Math.Cos(parts.Real) * Math.Cosh(parts.Imaginary);
            double imag = -Math.Sin(parts.Real) * Math.Sinh(parts.Imaginary);
            return new StringEval(ImaginaryFunctionSupport.FormatComplexResult(real, imag, parts.Suffix));
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
