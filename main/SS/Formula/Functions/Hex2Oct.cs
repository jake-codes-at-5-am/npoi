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
     * Implementation for Excel HEX2OCT() function.
     * Syntax: HEX2OCT(number, [places])
     *
     * Converts a hexadecimal number to octal. The decoded decimal value must
     * fit in 30 signed bits (-2^29..2^29-1).
     */
    public class Hex2Oct : Var1or2ArgFunction, FreeRefFunction
    {
        public static FreeRefFunction instance = new Hex2Oct();

        private const int HexBase = 16;
        private const int MaxInputPlaces = 10;

        public override ValueEval Evaluate(int srcRowIndex, int srcColumnIndex, ValueEval numberVE)
        {
            return Evaluate(srcRowIndex, srcColumnIndex, numberVE, null);
        }

        public override ValueEval Evaluate(int srcRowIndex, int srcColumnIndex, ValueEval numberVE, ValueEval placesVE)
        {
            return BaseConversionUtils.Convert(numberVE, placesVE, srcRowIndex, srcColumnIndex,
                HexBase, MaxInputPlaces, BaseConversionUtils.OutputBase.Octal);
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
