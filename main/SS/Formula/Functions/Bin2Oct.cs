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
     * Implementation for Excel BIN2OCT() function.
     * Syntax: BIN2OCT(number, [places])
     *
     * Converts a binary number to octal. Number cannot contain more than 10
     * binary characters (10 bits). Negative numbers are represented using
     * two's-complement notation across 10 octal characters.
     */
    public class Bin2Oct : Var1or2ArgFunction, FreeRefFunction
    {
        public static FreeRefFunction instance = new Bin2Oct();

        private const int BinaryBase = 2;
        private const int MaxInputPlaces = 10;

        public override ValueEval Evaluate(int srcRowIndex, int srcColumnIndex, ValueEval numberVE)
        {
            return Evaluate(srcRowIndex, srcColumnIndex, numberVE, null);
        }

        public override ValueEval Evaluate(int srcRowIndex, int srcColumnIndex, ValueEval numberVE, ValueEval placesVE)
        {
            return BaseConversionUtils.Convert(numberVE, placesVE, srcRowIndex, srcColumnIndex,
                BinaryBase, MaxInputPlaces, BaseConversionUtils.OutputBase.Octal);
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
