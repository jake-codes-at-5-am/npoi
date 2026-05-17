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

namespace TestCases.SS.Formula.Functions
{
    using System;
    using NPOI.HSSF.UserModel;
    using NPOI.SS.Formula;
    using NPOI.SS.Formula.Eval;
    using NPOI.SS.Formula.Functions;
    using NUnit.Framework;

    /// <summary>
    /// Tests for the second-wave ATP additions: bitwise, BASE/DECIMAL/DEC2OCT/ARABIC,
    /// DAYS/ISOWEEKNUM/XOR, UNICODE/UNICHAR, GAMMA family, COMBINA/PERMUTATIONA,
    /// the full IM* complex set, and ISFORMULA.
    /// </summary>
    [TestFixture]
    public class TestModernAtpFunctions
    {
        private const double Tolerance = 1e-9;

        private OperationEvaluationContext _ctx;

        [OneTimeSetUp]
        public void SetUp()
        {
            HSSFWorkbook wb = new HSSFWorkbook();
            wb.CreateSheet();
            HSSFEvaluationWorkbook workbook = HSSFEvaluationWorkbook.Create(wb);
            WorkbookEvaluator evaluator = new WorkbookEvaluator(workbook, new AlwaysFinalStability(), null);
            _ctx = new OperationEvaluationContext(evaluator, workbook, 0, 0, 0, null);
        }

        private class AlwaysFinalStability : IStabilityClassifier
        {
            public override bool IsCellFinal(int sheetIndex, int rowIndex, int columnIndex) => true;
        }

        private static ValueEval Run(Function func, params ValueEval[] args) => func.Evaluate(args, -1, -1);
        private ValueEval RunFree(FreeRefFunction func, params ValueEval[] args) => func.Evaluate(args, _ctx);

        private static double AsDouble(ValueEval result)
        {
            Assert.AreEqual(typeof(NumberEval), result.GetType(), "expected NumberEval, got " + result.GetType().Name);
            return ((NumberEval)result).NumberValue;
        }

        private static string AsString(ValueEval result)
        {
            Assert.AreEqual(typeof(StringEval), result.GetType(), "expected StringEval, got " + result.GetType().Name);
            return ((StringEval)result).StringValue;
        }

        // ---------------- Bitwise ----------------

        [Test]
        public void TestBitAndOrXor()
        {
            Assert.AreEqual(8, AsDouble(Run(new BitAnd(), new NumberEval(0xC), new NumberEval(0xA))));
            Assert.AreEqual(14, AsDouble(Run(new BitOr(), new NumberEval(0xC), new NumberEval(0xA))));
            Assert.AreEqual(6, AsDouble(Run(new BitXor(), new NumberEval(0xC), new NumberEval(0xA))));
        }

        [Test]
        public void TestBitShifts()
        {
            Assert.AreEqual(20, AsDouble(Run(new BitLShift(), new NumberEval(5), new NumberEval(2))));
            Assert.AreEqual(2, AsDouble(Run(new BitRShift(), new NumberEval(8), new NumberEval(2))));
            // Negative shift on BITLSHIFT acts as right shift
            Assert.AreEqual(2, AsDouble(Run(new BitLShift(), new NumberEval(8), new NumberEval(-2))));
        }

        [Test]
        public void TestBitwiseRangeErrors()
        {
            Assert.AreEqual(ErrorEval.NUM_ERROR, Run(new BitAnd(), new NumberEval(-1), new NumberEval(2)));
            Assert.AreEqual(ErrorEval.NUM_ERROR, Run(new BitLShift(), new NumberEval(1), new NumberEval(60)));
            Assert.AreEqual(ErrorEval.NUM_ERROR, Run(new BitAnd(), new NumberEval(BitwiseSupport.MaxValue + 1), new NumberEval(0)));
        }

        // ---------------- BASE / DECIMAL / DEC2OCT / ARABIC ----------------

        [Test]
        public void TestBaseAndDecimal()
        {
            Assert.AreEqual("FF", AsString(Run(new Base(), new NumberEval(255), new NumberEval(16))));
            Assert.AreEqual("00FF", AsString(Run(new Base(), new NumberEval(255), new NumberEval(16), new NumberEval(4))));
            Assert.AreEqual("11111111", AsString(Run(new Base(), new NumberEval(255), new NumberEval(2))));

            Assert.AreEqual(255, AsDouble(Run(new DecimalFunc(), new StringEval("FF"), new NumberEval(16))));
            Assert.AreEqual(255, AsDouble(Run(new DecimalFunc(), new StringEval("11111111"), new NumberEval(2))));
            Assert.AreEqual(0, AsDouble(Run(new DecimalFunc(), new StringEval("0"), new NumberEval(16))));
        }

        [Test]
        public void TestBaseDecimalErrors()
        {
            Assert.AreEqual(ErrorEval.NUM_ERROR, Run(new Base(), new NumberEval(-1), new NumberEval(2)));
            Assert.AreEqual(ErrorEval.NUM_ERROR, Run(new Base(), new NumberEval(10), new NumberEval(1)));
            Assert.AreEqual(ErrorEval.NUM_ERROR, Run(new DecimalFunc(), new StringEval("9"), new NumberEval(8)));
            Assert.AreEqual(ErrorEval.NUM_ERROR, Run(new DecimalFunc(), new StringEval("!@"), new NumberEval(16)));
        }

        [Test]
        public void TestDec2Oct()
        {
            Assert.AreEqual("12", AsString(Run(new Dec2Oct(), new NumberEval(10))));
            Assert.AreEqual("0012", AsString(Run(new Dec2Oct(), new NumberEval(10), new NumberEval(4))));
            // Negative: 30-bit two's complement = 8^10 - 1 = 7777777777
            Assert.AreEqual("7777777777", AsString(Run(new Dec2Oct(), new NumberEval(-1))));
        }

        [Test]
        public void TestArabic()
        {
            Assert.AreEqual(1990, AsDouble(Run(new Arabic(), new StringEval("MCMXC"))));
            Assert.AreEqual(2008, AsDouble(Run(new Arabic(), new StringEval("MMVIII"))));
            Assert.AreEqual(-1, AsDouble(Run(new Arabic(), new StringEval("-I"))));
            Assert.AreEqual(0, AsDouble(Run(new Arabic(), new StringEval(""))));
            Assert.AreEqual(ErrorEval.VALUE_INVALID, Run(new Arabic(), new StringEval("abc")));
        }

        // ---------------- DAYS / ISOWEEKNUM / XOR ----------------

        [Test]
        public void TestDays()
        {
            // 1900-Jan-01 to 1900-Jan-31 is 30 days (Excel serials 1 and 31)
            Assert.AreEqual(30, AsDouble(Run(new Days(), new NumberEval(31), new NumberEval(1))));
            Assert.AreEqual(-30, AsDouble(Run(new Days(), new NumberEval(1), new NumberEval(31))));
        }

        [Test]
        public void TestIsoWeekNum()
        {
            // 2008-12-29 is in ISO week 1 of 2009 (Excel serial = 39811)
            Assert.AreEqual(1, AsDouble(Run(new IsoWeekNum(), new NumberEval(39811))));
            // 2010-01-01 is ISO week 53 of 2009 (Excel serial = 40179)
            Assert.AreEqual(53, AsDouble(Run(new IsoWeekNum(), new NumberEval(40179))));
        }

        [Test]
        public void TestXor()
        {
            // odd number of TRUEs -> TRUE
            Assert.AreEqual(BoolEval.TRUE, RunFree(Xor.instance, BoolEval.TRUE, BoolEval.FALSE));
            Assert.AreEqual(BoolEval.FALSE, RunFree(Xor.instance, BoolEval.TRUE, BoolEval.TRUE));
            Assert.AreEqual(BoolEval.TRUE, RunFree(Xor.instance,
                new NumberEval(1), new NumberEval(0), new NumberEval(1), new NumberEval(1)));
            Assert.AreEqual(BoolEval.FALSE, RunFree(Xor.instance,
                new NumberEval(0), new NumberEval(0)));
        }

        [Test]
        public void TestXorNoArgsIsValueInvalid()
        {
            // Excel's parser rejects =XOR() outright; if we ever do get an empty
            // arg list at evaluation time, return #VALUE! rather than crash or FALSE.
            Assert.AreEqual(ErrorEval.VALUE_INVALID, RunFree(Xor.instance));
        }

        [Test]
        public void TestXorAllBlankArgsReturnsFalse()
        {
            // Excel rule: blanks/missing values are coerced to FALSE in XOR — they
            // simply don't contribute to the TRUE count. Previously this returned
            // #VALUE! because we tracked a "saw at least one usable value" flag;
            // now blanks fall through and the parity check yields FALSE.
            Assert.AreEqual(BoolEval.FALSE, RunFree(Xor.instance, BlankEval.instance));
            Assert.AreEqual(BoolEval.FALSE,
                RunFree(Xor.instance, BlankEval.instance, BlankEval.instance));
            Assert.AreEqual(BoolEval.FALSE,
                RunFree(Xor.instance, MissingArgEval.instance, MissingArgEval.instance));
        }

        [Test]
        public void TestXorMixedBlankAndTrueArgs()
        {
            // One TRUE plus blanks => one TRUE => odd => TRUE
            Assert.AreEqual(BoolEval.TRUE,
                RunFree(Xor.instance, BlankEval.instance, BoolEval.TRUE, BlankEval.instance));
            // Two TRUEs plus blanks => even => FALSE
            Assert.AreEqual(BoolEval.FALSE,
                RunFree(Xor.instance, BoolEval.TRUE, BlankEval.instance, BoolEval.TRUE));
        }

        [Test]
        public void TestXorNonCoercibleStringReturnsValueInvalid()
        {
            // Non-bool/non-numeric strings are not coerceable -> #VALUE! (matches Excel).
            Assert.AreEqual(ErrorEval.VALUE_INVALID,
                RunFree(Xor.instance, new StringEval("hello")));
        }

        [Test]
        public void TestXorPropagatesErrorArg()
        {
            // Error in any arg propagates (matches Excel).
            Assert.AreEqual(ErrorEval.NA,
                RunFree(Xor.instance, BoolEval.TRUE, ErrorEval.NA));
        }

        // ---------------- UNICODE / UNICHAR ----------------

        [Test]
        public void TestUnicodeRoundTrip()
        {
            Assert.AreEqual(65, AsDouble(Run(new Unicode(), new StringEval("A"))));
            Assert.AreEqual("A", AsString(Run(new UniChar(), new NumberEval(65))));
            // supplementary plane
            string surrogate = char.ConvertFromUtf32(0x1F600);
            Assert.AreEqual(0x1F600, AsDouble(Run(new Unicode(), new StringEval(surrogate))));
            Assert.AreEqual(surrogate, AsString(Run(new UniChar(), new NumberEval(0x1F600))));
        }

        [Test]
        public void TestUnicodeErrors()
        {
            Assert.AreEqual(ErrorEval.VALUE_INVALID, Run(new Unicode(), new StringEval("")));
            Assert.AreEqual(ErrorEval.VALUE_INVALID, Run(new UniChar(), new NumberEval(0)));
            Assert.AreEqual(ErrorEval.VALUE_INVALID, Run(new UniChar(), new NumberEval(0xD800)));
        }

        // ---------------- GAMMA / GAMMALN.PRECISE / COMBINA / PERMUTATIONA ----------------

        [Test]
        public void TestGamma()
        {
            // Gamma(n) = (n-1)! for positive integers
            Assert.AreEqual(1, AsDouble(Run(new Gamma(), new NumberEval(1))), 1e-9);
            Assert.AreEqual(1, AsDouble(Run(new Gamma(), new NumberEval(2))), 1e-9);
            Assert.AreEqual(2, AsDouble(Run(new Gamma(), new NumberEval(3))), 1e-9);
            Assert.AreEqual(120, AsDouble(Run(new Gamma(), new NumberEval(6))), 1e-9);
            // Gamma(0.5) = sqrt(pi)
            Assert.AreEqual(Math.Sqrt(Math.PI), AsDouble(Run(new Gamma(), new NumberEval(0.5))), 1e-9);
        }

        [Test]
        public void TestGammaLnPrecise()
        {
            Assert.AreEqual(0, AsDouble(Run(new GammaLnPrecise(), new NumberEval(1))), 1e-9);
            Assert.AreEqual(Math.Log(120), AsDouble(Run(new GammaLnPrecise(), new NumberEval(6))), 1e-9);
        }

        [Test]
        public void TestCombinationFunctions()
        {
            // COMBINA(4,3) = C(6,3) = 20
            Assert.AreEqual(20, AsDouble(Run(new Combina(), new NumberEval(4), new NumberEval(3))));
            // PERMUTATIONA(3,2) = 3^2 = 9
            Assert.AreEqual(9, AsDouble(Run(new PermutationA(), new NumberEval(3), new NumberEval(2))));
        }

        // ---------------- ISFORMULA ----------------

        [Test]
        public void TestIsFormulaTrueAndFalse()
        {
            HSSFWorkbook wb = new HSSFWorkbook();
            var sheet = wb.CreateSheet();
            var row = sheet.CreateRow(0);
            row.CreateCell(0).SetCellValue(42);
            row.CreateCell(1).SetCellFormula("A1*2");

            HSSFEvaluationWorkbook ew = HSSFEvaluationWorkbook.Create(wb);
            WorkbookEvaluator we = new WorkbookEvaluator(ew, new AlwaysFinalStability(), null);
            OperationEvaluationContext ec = new OperationEvaluationContext(we, ew, 0, 0, 0, null);

            ValueEval refToA1 = ec.GetRefEval(0, 0);
            ValueEval refToB1 = ec.GetRefEval(0, 1);
            Assert.AreEqual(BoolEval.FALSE, IsFormula.instance.Evaluate(new[] { refToA1 }, ec));
            Assert.AreEqual(BoolEval.TRUE, IsFormula.instance.Evaluate(new[] { refToB1 }, ec));
        }

        [Test]
        public void TestIsFormulaRejectsNonReference()
        {
            ValueEval result = IsFormula.instance.Evaluate(new ValueEval[] { new NumberEval(1) }, _ctx);
            Assert.AreEqual(ErrorEval.VALUE_INVALID, result);
        }

        // ---------------- IM* family ----------------

        [Test]
        public void TestImSumProductSubDiv()
        {
            Assert.AreEqual("4+6i", AsString(RunFree(ImSum.instance, new StringEval("1+2i"), new StringEval("3+4i"))));
            Assert.AreEqual("-2-2i", AsString(RunFree(ImSub.instance, new StringEval("1+2i"), new StringEval("3+4i"))));
            // (1+2i)(3+4i) = -5+10i
            Assert.AreEqual("-5+10i", AsString(RunFree(ImProduct.instance, new StringEval("1+2i"), new StringEval("3+4i"))));
            // (1+i)/(1-i) = i
            Assert.AreEqual("i", AsString(RunFree(ImDiv.instance, new StringEval("1+i"), new StringEval("1-i"))));
        }

        [Test]
        public void TestImConjugateAndArgument()
        {
            Assert.AreEqual("3-4i", AsString(RunFree(ImConjugate.instance, new StringEval("3+4i"))));
            // arg(1+i) = pi/4
            Assert.AreEqual(Math.PI / 4, AsDouble(RunFree(ImArgument.instance, new StringEval("1+i"))), Tolerance);
            // arg(0) -> #DIV/0!
            Assert.AreEqual(ErrorEval.DIV_ZERO, RunFree(ImArgument.instance, new StringEval("0")));
        }

        [Test]
        public void TestImSqrtAndPower()
        {
            // sqrt(-1) = i
            Assert.AreEqual("i", AsString(RunFree(ImSqrt.instance, new StringEval("-1"))));
            // (1+i)^2 = 2i
            Assert.AreEqual("2i", AsString(RunFree(ImPower.instance, new StringEval("1+i"), new NumberEval(2))));
        }

        [Test]
        public void TestImExpAndLogs()
        {
            // exp(0) = 1
            Assert.AreEqual("1", AsString(RunFree(ImExp.instance, new StringEval("0"))));
            // ln(1) = 0
            Assert.AreEqual("0", AsString(RunFree(ImLn.instance, new StringEval("1"))));
            // log10(10) = 1
            Assert.AreEqual("1", AsString(RunFree(ImLog10.instance, new StringEval("10"))));
            // log2(8) = 3
            Assert.AreEqual("3", AsString(RunFree(ImLog2.instance, new StringEval("8"))));
        }

        [Test]
        public void TestImTrigAndHyperbolic()
        {
            // sinh(0) = 0
            Assert.AreEqual("0", AsString(RunFree(ImSinh.instance, new StringEval("0"))));
            // cosh(0) = 1
            Assert.AreEqual("1", AsString(RunFree(ImCosh.instance, new StringEval("0"))));
            // tanh(large) ~= 1 (real, no imag) — verify scalar-real result
            string tanhBig = AsString(RunFree(ImTanh.instance, new StringEval("10")));
            Assert.IsFalse(tanhBig.Contains("i"), "tanh(10) should be a real value");
            Assert.AreEqual(1.0, double.Parse(tanhBig, System.Globalization.CultureInfo.InvariantCulture), 1e-6);
        }

        [Test]
        public void TestImSuffixMismatch()
        {
            Assert.AreEqual(ErrorEval.VALUE_INVALID,
                RunFree(ImSum.instance, new StringEval("1+2i"), new StringEval("3+4j")));
        }
    }
}
