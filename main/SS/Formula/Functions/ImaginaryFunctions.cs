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

using static NPOI.SS.Formula.Functions.ImComplexHelpers;

namespace NPOI.SS.Formula.Functions
{
    using System;
    using NPOI.SS.Formula.Eval;

    /// <summary>
    /// Shared invocation harness for the IM* engineering functions. Handles
    /// argument coercion, complex-number parsing and formatting so each
    /// function below collapses to the actual mathematical identity.
    /// </summary>
    internal static class ImaginaryFunctionSupport
    {
        internal delegate ComplexNumberParts UnaryOp(ComplexNumberParts z);
        internal delegate double UnaryReal(ComplexNumberParts z);
        internal delegate ComplexNumberParts BinaryOp(ComplexNumberParts a, ComplexNumberParts b, string suffix);

        /// <summary>
        /// Format a complex result for IM* functions, snapping near-zero
        /// floating-point noise (e.g. <c>cos(pi/2) ~ 6e-17</c>) so that
        /// IMSQRT(-1) returns "i" rather than "6.12E-17+i". The threshold is
        /// scaled by the dominant component to preserve genuine small values.
        /// </summary>
        internal static string FormatComplexResult(double real, double imaginary, string suffix)
        {
            double scale = Math.Max(Math.Abs(real), Math.Abs(imaginary));
            double threshold = scale * 1e-14;
            if (Math.Abs(real) <= threshold) real = 0;
            if (Math.Abs(imaginary) <= threshold) imaginary = 0;
            real = SnapToNearestInteger(real);
            imaginary = SnapToNearestInteger(imaginary);
            return ComplexNumberParts.Format(real, imaginary, suffix);
        }

        /// <summary>
        /// Snap values within ~13 significant digits of an integer up to that
        /// integer. This compensates for the accumulated rounding in identities
        /// such as <c>log2(8)</c> or <c>(1+i)^2</c> so the user-visible result
        /// reads as the mathematical answer rather than 2.9999999999999996.
        /// </summary>
        private static double SnapToNearestInteger(double v)
        {
            if (double.IsNaN(v) || double.IsInfinity(v))
            {
                return v;
            }
            double rounded = Math.Round(v);
            double tolerance = 1e-13 * Math.Max(1.0, Math.Abs(v));
            return Math.Abs(v - rounded) <= tolerance ? rounded : v;
        }

        internal static ValueEval EvaluateUnary(ValueEval[] args, OperationEvaluationContext ec, UnaryOp op)
        {
            if (args.Length != 1) return ErrorEval.VALUE_INVALID;
            if (!ComplexNumberParts.TryEvaluateArg(args[0], ec.RowIndex, ec.ColumnIndex, out var parts, out var error))
            {
                return error;
            }
            ComplexNumberParts result = op(parts);
            if (result == null || double.IsNaN(result.Real) || double.IsNaN(result.Imaginary))
            {
                return ErrorEval.NUM_ERROR;
            }
            return new StringEval(FormatComplexResult(result.Real, result.Imaginary, result.Suffix));
        }

        internal static ValueEval EvaluateUnaryReal(ValueEval[] args, OperationEvaluationContext ec, UnaryReal op)
        {
            if (args.Length != 1) return ErrorEval.VALUE_INVALID;
            if (!ComplexNumberParts.TryEvaluateArg(args[0], ec.RowIndex, ec.ColumnIndex, out var parts, out var error))
            {
                return error;
            }
            double v = op(parts);
            if (double.IsNaN(v) || double.IsInfinity(v))
            {
                return ErrorEval.NUM_ERROR;
            }
            return new NumberEval(v);
        }

        internal static ValueEval EvaluateBinary(ValueEval[] args, OperationEvaluationContext ec, BinaryOp op)
        {
            if (args.Length != 2) return ErrorEval.VALUE_INVALID;
            if (!ComplexNumberParts.TryEvaluateArg(args[0], ec.RowIndex, ec.ColumnIndex, out var a, out var error)) return error;
            if (!ComplexNumberParts.TryEvaluateArg(args[1], ec.RowIndex, ec.ColumnIndex, out var b, out error)) return error;
            string suffix = ComplexNumberParts.MergeSuffix(a, b, out bool mismatch);
            if (mismatch) return ErrorEval.VALUE_INVALID;
            ComplexNumberParts result = op(a, b, suffix);
            if (result == null) return ErrorEval.NUM_ERROR;
            if (double.IsNaN(result.Real) || double.IsNaN(result.Imaginary)) return ErrorEval.NUM_ERROR;
            return new StringEval(FormatComplexResult(result.Real, result.Imaginary, result.Suffix));
        }

        internal static ValueEval EvaluateMany(ValueEval[] args, OperationEvaluationContext ec, BinaryOp combine, ComplexNumberParts identity)
        {
            if (args == null || args.Length == 0) return ErrorEval.VALUE_INVALID;
            ComplexNumberParts acc = identity;
            string suffix = identity.Suffix;
            bool first = true;
            foreach (ValueEval arg in args)
            {
                if (!ComplexNumberParts.TryEvaluateArg(arg, ec.RowIndex, ec.ColumnIndex, out var parts, out var error))
                {
                    return error;
                }
                if (first)
                {
                    suffix = parts.Suffix;
                    acc = parts;
                    first = false;
                    continue;
                }
                string merged = ComplexNumberParts.MergeSuffix(acc, parts, out bool mismatch);
                if (mismatch) return ErrorEval.VALUE_INVALID;
                suffix = merged;
                acc = combine(acc, parts, suffix);
                if (acc == null) return ErrorEval.NUM_ERROR;
            }
            return new StringEval(FormatComplexResult(acc.Real, acc.Imaginary, acc.Suffix));
        }
    }

    /// <summary>IMSUM(inumber1, [inumber2], ...) — sum of complex numbers.</summary>
    public class ImSum : FreeRefFunction
    {
        public static FreeRefFunction instance = new ImSum();
        public ValueEval Evaluate(ValueEval[] args, OperationEvaluationContext ec) =>
            ImaginaryFunctionSupport.EvaluateMany(args, ec, ComplexNumberParts.Add, new ComplexNumberParts(0, 0, "i"));
    }

    /// <summary>IMSUB(inumber1, inumber2) — complex subtraction.</summary>
    public class ImSub : FreeRefFunction
    {
        public static FreeRefFunction instance = new ImSub();
        public ValueEval Evaluate(ValueEval[] args, OperationEvaluationContext ec) =>
            ImaginaryFunctionSupport.EvaluateBinary(args, ec, ComplexNumberParts.Subtract);
    }

    /// <summary>IMPRODUCT(inumber1, [inumber2], ...) — product of complex numbers.</summary>
    public class ImProduct : FreeRefFunction
    {
        public static FreeRefFunction instance = new ImProduct();
        public ValueEval Evaluate(ValueEval[] args, OperationEvaluationContext ec) =>
            ImaginaryFunctionSupport.EvaluateMany(args, ec, ComplexNumberParts.Multiply, new ComplexNumberParts(1, 0, "i"));
    }

    /// <summary>IMDIV(inumber1, inumber2) — complex division.</summary>
    public class ImDiv : FreeRefFunction
    {
        public static FreeRefFunction instance = new ImDiv();
        public ValueEval Evaluate(ValueEval[] args, OperationEvaluationContext ec) =>
            ImaginaryFunctionSupport.EvaluateBinary(args, ec, ComplexNumberParts.Divide);
    }

    /// <summary>IMCONJUGATE(inumber) — complex conjugate.</summary>
    public class ImConjugate : FreeRefFunction
    {
        public static FreeRefFunction instance = new ImConjugate();
        public ValueEval Evaluate(ValueEval[] args, OperationEvaluationContext ec) =>
            ImaginaryFunctionSupport.EvaluateUnary(args, ec, z => new ComplexNumberParts(z.Real, -z.Imaginary, z.Suffix));
    }

    /// <summary>IMARGUMENT(inumber) — argument (theta) of complex number.</summary>
    public class ImArgument : FreeRefFunction
    {
        public static FreeRefFunction instance = new ImArgument();
        public ValueEval Evaluate(ValueEval[] args, OperationEvaluationContext ec)
        {
            if (args.Length != 1) return ErrorEval.VALUE_INVALID;
            if (!ComplexNumberParts.TryEvaluateArg(args[0], ec.RowIndex, ec.ColumnIndex, out var parts, out var error))
            {
                return error;
            }
            if (parts.Real == 0 && parts.Imaginary == 0)
            {
                return ErrorEval.DIV_ZERO;
            }
            return new NumberEval(parts.Argument);
        }
    }

    /// <summary>IMSQRT(inumber) — principal square root.</summary>
    public class ImSqrt : FreeRefFunction
    {
        public static FreeRefFunction instance = new ImSqrt();
        public ValueEval Evaluate(ValueEval[] args, OperationEvaluationContext ec) =>
            ImaginaryFunctionSupport.EvaluateUnary(args, ec, z =>
            {
                double r = Math.Sqrt(z.Magnitude);
                double half = z.Argument / 2;
                return new ComplexNumberParts(r * Math.Cos(half), r * Math.Sin(half), z.Suffix);
            });
    }

    /// <summary>IMPOWER(inumber, number) — complex z raised to real power.</summary>
    public class ImPower : FreeRefFunction
    {
        public static FreeRefFunction instance = new ImPower();
        public ValueEval Evaluate(ValueEval[] args, OperationEvaluationContext ec)
        {
            if (args.Length != 2) return ErrorEval.VALUE_INVALID;
            if (!ComplexNumberParts.TryEvaluateArg(args[0], ec.RowIndex, ec.ColumnIndex, out var z, out var error)) return error;
            double power;
            try
            {
                power = OperandResolver.CoerceValueToDouble(OperandResolver.GetSingleValue(args[1], ec.RowIndex, ec.ColumnIndex));
            }
            catch (EvaluationException e)
            {
                return e.GetErrorEval();
            }
            if (z.Real == 0 && z.Imaginary == 0)
            {
                if (power == 0) return ErrorEval.NUM_ERROR;
                return new StringEval("0");
            }
            double r = Math.Pow(z.Magnitude, power);
            double theta = z.Argument * power;
            ComplexNumberParts result = new ComplexNumberParts(r * Math.Cos(theta), r * Math.Sin(theta), z.Suffix);
            return new StringEval(ImaginaryFunctionSupport.FormatComplexResult(result.Real, result.Imaginary, result.Suffix));
        }
    }

    /// <summary>IMEXP(inumber) — exponential of complex number.</summary>
    public class ImExp : FreeRefFunction
    {
        public static FreeRefFunction instance = new ImExp();
        public ValueEval Evaluate(ValueEval[] args, OperationEvaluationContext ec) =>
            ImaginaryFunctionSupport.EvaluateUnary(args, ec, z =>
            {
                double e = Math.Exp(z.Real);
                return new ComplexNumberParts(e * Math.Cos(z.Imaginary), e * Math.Sin(z.Imaginary), z.Suffix);
            });
    }

    /// <summary>IMLN(inumber) — natural log of complex number.</summary>
    public class ImLn : FreeRefFunction
    {
        public static FreeRefFunction instance = new ImLn();
        public ValueEval Evaluate(ValueEval[] args, OperationEvaluationContext ec) =>
            ImaginaryFunctionSupport.EvaluateUnary(args, ec, z =>
            {
                if (z.Real == 0 && z.Imaginary == 0) return null;
                return new ComplexNumberParts(Math.Log(z.Magnitude), z.Argument, z.Suffix);
            });
    }

    /// <summary>IMLOG10(inumber) — base-10 log of complex number.</summary>
    public class ImLog10 : FreeRefFunction
    {
        public static FreeRefFunction instance = new ImLog10();
        public ValueEval Evaluate(ValueEval[] args, OperationEvaluationContext ec) =>
            ImaginaryFunctionSupport.EvaluateUnary(args, ec, z =>
            {
                if (z.Real == 0 && z.Imaginary == 0) return null;
                double scale = 1.0 / Math.Log(10);
                return new ComplexNumberParts(scale * Math.Log(z.Magnitude), scale * z.Argument, z.Suffix);
            });
    }

    /// <summary>IMLOG2(inumber) — base-2 log of complex number.</summary>
    public class ImLog2 : FreeRefFunction
    {
        public static FreeRefFunction instance = new ImLog2();
        public ValueEval Evaluate(ValueEval[] args, OperationEvaluationContext ec) =>
            ImaginaryFunctionSupport.EvaluateUnary(args, ec, z =>
            {
                if (z.Real == 0 && z.Imaginary == 0) return null;
                double scale = 1.0 / Math.Log(2);
                return new ComplexNumberParts(scale * Math.Log(z.Magnitude), scale * z.Argument, z.Suffix);
            });
    }

    /// <summary>IMTAN(inumber) — tan(z) = sin(z)/cos(z).</summary>
    public class ImTan : FreeRefFunction
    {
        public static FreeRefFunction instance = new ImTan();
        public ValueEval Evaluate(ValueEval[] args, OperationEvaluationContext ec) =>
            ImaginaryFunctionSupport.EvaluateUnary(args, ec, z =>
            {
                double sinR = Math.Sin(z.Real), cosR = Math.Cos(z.Real);
                double sinhI = Math.Sinh(z.Imaginary), coshI = Math.Cosh(z.Imaginary);
                double denom = cosR * cosR * coshI * coshI + sinR * sinR * sinhI * sinhI;
                if (denom == 0) return null;
                return new ComplexNumberParts(
                    (sinR * cosR) / denom,
                    (sinhI * coshI) / denom,
                    z.Suffix);
            });
    }

    /// <summary>IMSEC(inumber) — secant = 1/cos(z).</summary>
    public class ImSec : FreeRefFunction
    {
        public static FreeRefFunction instance = new ImSec();
        public ValueEval Evaluate(ValueEval[] args, OperationEvaluationContext ec) =>
            ImaginaryFunctionSupport.EvaluateUnary(args, ec, z => Reciprocal(Cos(z), z.Suffix));
    }

    /// <summary>IMCSC(inumber) — cosecant = 1/sin(z).</summary>
    public class ImCsc : FreeRefFunction
    {
        public static FreeRefFunction instance = new ImCsc();
        public ValueEval Evaluate(ValueEval[] args, OperationEvaluationContext ec) =>
            ImaginaryFunctionSupport.EvaluateUnary(args, ec, z => Reciprocal(Sin(z), z.Suffix));
    }

    /// <summary>IMCOT(inumber) — cotangent = cos(z)/sin(z).</summary>
    public class ImCot : FreeRefFunction
    {
        public static FreeRefFunction instance = new ImCot();
        public ValueEval Evaluate(ValueEval[] args, OperationEvaluationContext ec) =>
            ImaginaryFunctionSupport.EvaluateUnary(args, ec, z =>
            {
                ComplexNumberParts s = Sin(z);
                if (s.Real == 0 && s.Imaginary == 0) return null;
                ComplexNumberParts c = Cos(z);
                return ComplexNumberParts.Divide(c, s, z.Suffix);
            });
    }

    /// <summary>IMSINH(inumber) — sinh(a+bi) = sinh(a)cos(b) + i cosh(a)sin(b).</summary>
    public class ImSinh : FreeRefFunction
    {
        public static FreeRefFunction instance = new ImSinh();
        public ValueEval Evaluate(ValueEval[] args, OperationEvaluationContext ec) =>
            ImaginaryFunctionSupport.EvaluateUnary(args, ec, z => new ComplexNumberParts(
                Math.Sinh(z.Real) * Math.Cos(z.Imaginary),
                Math.Cosh(z.Real) * Math.Sin(z.Imaginary),
                z.Suffix));
    }

    /// <summary>IMCOSH(inumber) — cosh(a+bi) = cosh(a)cos(b) + i sinh(a)sin(b).</summary>
    public class ImCosh : FreeRefFunction
    {
        public static FreeRefFunction instance = new ImCosh();
        public ValueEval Evaluate(ValueEval[] args, OperationEvaluationContext ec) =>
            ImaginaryFunctionSupport.EvaluateUnary(args, ec, z => new ComplexNumberParts(
                Math.Cosh(z.Real) * Math.Cos(z.Imaginary),
                Math.Sinh(z.Real) * Math.Sin(z.Imaginary),
                z.Suffix));
    }

    /// <summary>IMTANH(inumber) — tanh(z) = sinh(z)/cosh(z).</summary>
    public class ImTanh : FreeRefFunction
    {
        public static FreeRefFunction instance = new ImTanh();
        public ValueEval Evaluate(ValueEval[] args, OperationEvaluationContext ec) =>
            ImaginaryFunctionSupport.EvaluateUnary(args, ec, z =>
            {
                double sinhR = Math.Sinh(z.Real), coshR = Math.Cosh(z.Real);
                double sinI = Math.Sin(z.Imaginary), cosI = Math.Cos(z.Imaginary);
                double denom = coshR * coshR * cosI * cosI + sinhR * sinhR * sinI * sinI;
                if (denom == 0) return null;
                return new ComplexNumberParts(
                    (sinhR * coshR) / denom,
                    (sinI * cosI) / denom,
                    z.Suffix);
            });
    }

    /// <summary>IMSECH(inumber) — sech(z) = 1/cosh(z).</summary>
    public class ImSech : FreeRefFunction
    {
        public static FreeRefFunction instance = new ImSech();
        public ValueEval Evaluate(ValueEval[] args, OperationEvaluationContext ec) =>
            ImaginaryFunctionSupport.EvaluateUnary(args, ec, z => Reciprocal(Cosh(z), z.Suffix));
    }

    /// <summary>IMCSCH(inumber) — csch(z) = 1/sinh(z).</summary>
    public class ImCsch : FreeRefFunction
    {
        public static FreeRefFunction instance = new ImCsch();
        public ValueEval Evaluate(ValueEval[] args, OperationEvaluationContext ec) =>
            ImaginaryFunctionSupport.EvaluateUnary(args, ec, z => Reciprocal(Sinh(z), z.Suffix));
    }

    // -- shared helpers used by the trig/hyperbolic IM* implementations --
    internal static class ImComplexHelpers
    {
        internal static ComplexNumberParts Sin(ComplexNumberParts z) => new ComplexNumberParts(
            Math.Sin(z.Real) * Math.Cosh(z.Imaginary),
            Math.Cos(z.Real) * Math.Sinh(z.Imaginary),
            z.Suffix);

        internal static ComplexNumberParts Cos(ComplexNumberParts z) => new ComplexNumberParts(
            Math.Cos(z.Real) * Math.Cosh(z.Imaginary),
            -Math.Sin(z.Real) * Math.Sinh(z.Imaginary),
            z.Suffix);

        internal static ComplexNumberParts Sinh(ComplexNumberParts z) => new ComplexNumberParts(
            Math.Sinh(z.Real) * Math.Cos(z.Imaginary),
            Math.Cosh(z.Real) * Math.Sin(z.Imaginary),
            z.Suffix);

        internal static ComplexNumberParts Cosh(ComplexNumberParts z) => new ComplexNumberParts(
            Math.Cosh(z.Real) * Math.Cos(z.Imaginary),
            Math.Sinh(z.Real) * Math.Sin(z.Imaginary),
            z.Suffix);

        internal static ComplexNumberParts Reciprocal(ComplexNumberParts z, string suffix)
        {
            double denom = z.Real * z.Real + z.Imaginary * z.Imaginary;
            if (denom == 0) return null;
            return new ComplexNumberParts(z.Real / denom, -z.Imaginary / denom, suffix);
        }
    }
}
