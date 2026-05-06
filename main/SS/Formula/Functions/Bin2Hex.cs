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
     * Implementation for Excel BIN2HEX() function.
     * Syntax: BIN2HEX(number, [places])
     *
     * Converts a binary number to hexadecimal. Number cannot contain more than
     * 10 binary characters (10 bits). The most significant bit of number is the
     * sign bit; negative numbers are represented using two's-complement notation.
     */
    public class Bin2Hex : Var1or2ArgFunction, FreeRefFunction
    {
        public static FreeRefFunction instance = new Bin2Hex();

        private const int MaxNumberOfPlaces = 10;
        private const int BinaryBase = 2;

        public override ValueEval Evaluate(int srcRowIndex, int srcColumnIndex, ValueEval numberVE)
        {
            return Evaluate(srcRowIndex, srcColumnIndex, numberVE, null);
        }

        public override ValueEval Evaluate(int srcRowIndex, int srcColumnIndex, ValueEval numberVE, ValueEval placesVE)
        {
            return BaseConversionUtils.Convert(numberVE, placesVE, srcRowIndex, srcColumnIndex,
                BinaryBase, MaxNumberOfPlaces, BaseConversionUtils.OutputBase.Hex);
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
