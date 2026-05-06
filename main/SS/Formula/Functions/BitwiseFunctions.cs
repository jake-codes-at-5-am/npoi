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

    /// <summary>
    /// Shared support for the bitwise ATP functions (BITAND, BITOR, BITXOR,
    /// BITLSHIFT, BITRSHIFT). Excel restricts these to non-negative integers up
    /// to 2^48-1 and shift counts in [-53, 53].
    /// </summary>
    internal static class BitwiseSupport
    {
        internal const long MaxValue = 281474976710655L; // 2^48 - 1
        private const int MaxShift = 53;

        internal static bool TryReadOperand(ValueEval ve, int srcRow, int srcCol, out long value, out ValueEval error)
        {
            value = 0;
            error = null;
            try
            {
                ValueEval resolved = OperandResolver.GetSingleValue(ve, srcRow, srcCol);
                double parsed = OperandResolver.CoerceValueToDouble(resolved);
                if (double.IsNaN(parsed) || double.IsInfinity(parsed))
                {
                    error = ErrorEval.NUM_ERROR;
                    return false;
                }
                if (parsed < 0 || parsed > MaxValue || parsed != Math.Floor(parsed))
                {
                    error = ErrorEval.NUM_ERROR;
                    return false;
                }
                value = (long)parsed;
                return true;
            }
            catch (EvaluationException e)
            {
                error = e.GetErrorEval();
                return false;
            }
        }

        internal static bool TryReadShift(ValueEval ve, int srcRow, int srcCol, out int shift, out ValueEval error)
        {
            shift = 0;
            error = null;
            try
            {
                ValueEval resolved = OperandResolver.GetSingleValue(ve, srcRow, srcCol);
                double parsed = OperandResolver.CoerceValueToDouble(resolved);
                if (double.IsNaN(parsed) || double.IsInfinity(parsed))
                {
                    error = ErrorEval.NUM_ERROR;
                    return false;
                }
                if (parsed < -MaxShift || parsed > MaxShift)
                {
                    error = ErrorEval.NUM_ERROR;
                    return false;
                }
                shift = (int)parsed;
                return true;
            }
            catch (EvaluationException e)
            {
                error = e.GetErrorEval();
                return false;
            }
        }

        internal static ValueEval ApplyShift(long value, int shift)
        {
            // Positive shift = shift left (toward higher bits); negative = shift right.
            // After shifting, the result must still fit in the 48-bit unsigned range.
            long result;
            if (shift >= 0)
            {
                if (shift >= 48)
                {
                    return value == 0 ? new NumberEval(0) : ErrorEval.NUM_ERROR;
                }
                result = value << shift;
                if (result > MaxValue)
                {
                    return ErrorEval.NUM_ERROR;
                }
            }
            else
            {
                int rightShift = -shift;
                result = rightShift >= 48 ? 0 : value >> rightShift;
            }
            return new NumberEval(result);
        }
    }

    /**
     * BITAND(number1, number2) - bitwise AND of two non-negative integers (max 2^48-1).
     */
    public class BitAnd : Fixed2ArgFunction, FreeRefFunction
    {
        public static FreeRefFunction instance = new BitAnd();

        public override ValueEval Evaluate(int srcRowIndex, int srcColumnIndex, ValueEval arg0, ValueEval arg1)
        {
            if (!BitwiseSupport.TryReadOperand(arg0, srcRowIndex, srcColumnIndex, out long a, out var err)) return err;
            if (!BitwiseSupport.TryReadOperand(arg1, srcRowIndex, srcColumnIndex, out long b, out err)) return err;
            return new NumberEval(a & b);
        }

        public ValueEval Evaluate(ValueEval[] args, OperationEvaluationContext ec)
        {
            return args.Length != 2 ? ErrorEval.VALUE_INVALID : Evaluate(ec.RowIndex, ec.ColumnIndex, args[0], args[1]);
        }
    }

    /**
     * BITOR(number1, number2) - bitwise OR.
     */
    public class BitOr : Fixed2ArgFunction, FreeRefFunction
    {
        public static FreeRefFunction instance = new BitOr();

        public override ValueEval Evaluate(int srcRowIndex, int srcColumnIndex, ValueEval arg0, ValueEval arg1)
        {
            if (!BitwiseSupport.TryReadOperand(arg0, srcRowIndex, srcColumnIndex, out long a, out var err)) return err;
            if (!BitwiseSupport.TryReadOperand(arg1, srcRowIndex, srcColumnIndex, out long b, out err)) return err;
            return new NumberEval(a | b);
        }

        public ValueEval Evaluate(ValueEval[] args, OperationEvaluationContext ec)
        {
            return args.Length != 2 ? ErrorEval.VALUE_INVALID : Evaluate(ec.RowIndex, ec.ColumnIndex, args[0], args[1]);
        }
    }

    /**
     * BITXOR(number1, number2) - bitwise exclusive OR.
     */
    public class BitXor : Fixed2ArgFunction, FreeRefFunction
    {
        public static FreeRefFunction instance = new BitXor();

        public override ValueEval Evaluate(int srcRowIndex, int srcColumnIndex, ValueEval arg0, ValueEval arg1)
        {
            if (!BitwiseSupport.TryReadOperand(arg0, srcRowIndex, srcColumnIndex, out long a, out var err)) return err;
            if (!BitwiseSupport.TryReadOperand(arg1, srcRowIndex, srcColumnIndex, out long b, out err)) return err;
            return new NumberEval(a ^ b);
        }

        public ValueEval Evaluate(ValueEval[] args, OperationEvaluationContext ec)
        {
            return args.Length != 2 ? ErrorEval.VALUE_INVALID : Evaluate(ec.RowIndex, ec.ColumnIndex, args[0], args[1]);
        }
    }

    /**
     * BITLSHIFT(number, shift_amount) - shift left when shift_amount is positive,
     * right when negative. Excel limits the shift magnitude to 53.
     */
    public class BitLShift : Fixed2ArgFunction, FreeRefFunction
    {
        public static FreeRefFunction instance = new BitLShift();

        public override ValueEval Evaluate(int srcRowIndex, int srcColumnIndex, ValueEval arg0, ValueEval arg1)
        {
            if (!BitwiseSupport.TryReadOperand(arg0, srcRowIndex, srcColumnIndex, out long value, out var err)) return err;
            if (!BitwiseSupport.TryReadShift(arg1, srcRowIndex, srcColumnIndex, out int shift, out err)) return err;
            return BitwiseSupport.ApplyShift(value, shift);
        }

        public ValueEval Evaluate(ValueEval[] args, OperationEvaluationContext ec)
        {
            return args.Length != 2 ? ErrorEval.VALUE_INVALID : Evaluate(ec.RowIndex, ec.ColumnIndex, args[0], args[1]);
        }
    }

    /**
     * BITRSHIFT(number, shift_amount) - shift right when shift_amount is positive,
     * left when negative. Mirror of BITLSHIFT.
     */
    public class BitRShift : Fixed2ArgFunction, FreeRefFunction
    {
        public static FreeRefFunction instance = new BitRShift();

        public override ValueEval Evaluate(int srcRowIndex, int srcColumnIndex, ValueEval arg0, ValueEval arg1)
        {
            if (!BitwiseSupport.TryReadOperand(arg0, srcRowIndex, srcColumnIndex, out long value, out var err)) return err;
            if (!BitwiseSupport.TryReadShift(arg1, srcRowIndex, srcColumnIndex, out int shift, out err)) return err;
            return BitwiseSupport.ApplyShift(value, -shift);
        }

        public ValueEval Evaluate(ValueEval[] args, OperationEvaluationContext ec)
        {
            return args.Length != 2 ? ErrorEval.VALUE_INVALID : Evaluate(ec.RowIndex, ec.ColumnIndex, args[0], args[1]);
        }
    }
}
