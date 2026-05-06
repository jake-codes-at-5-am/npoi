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
    using NPOI.SS.Formula.Eval;
    using NPOI.SS.Formula.Functions;
    using NUnit.Framework;

    /// <summary>
    /// Tests for IMABS, IMSIN and IMCOS. These all share the complex-number
    /// parser, so the parser edge cases live here too.
    /// </summary>
    [TestFixture]
    public class TestComplexImFunctions
    {
        private const double Tolerance = 1e-9;

        private static ValueEval Invoke(Function func, string complex)
        {
            ValueEval[] args = new ValueEval[] { new StringEval(complex) };
            return func.Evaluate(args, -1, -1);
        }

        [Test]
        public void TestImAbsBasic()
        {
            Assert.AreEqual(5, AsNumber(Invoke(new ImAbs(), "3+4i")), Tolerance);
            Assert.AreEqual(5, AsNumber(Invoke(new ImAbs(), "-3-4i")), Tolerance);
            Assert.AreEqual(1, AsNumber(Invoke(new ImAbs(), "i")), Tolerance);
            Assert.AreEqual(1, AsNumber(Invoke(new ImAbs(), "-i")), Tolerance);
            Assert.AreEqual(0, AsNumber(Invoke(new ImAbs(), "0")), Tolerance);
            Assert.AreEqual(7, AsNumber(Invoke(new ImAbs(), "7")), Tolerance);
            Assert.AreEqual(2, AsNumber(Invoke(new ImAbs(), "2j")), Tolerance);
        }

        [Test]
        public void TestImAbsInvalid()
        {
            Assert.AreEqual(ErrorEval.NUM_ERROR, Invoke(new ImAbs(), "I"));
            Assert.AreEqual(ErrorEval.NUM_ERROR, Invoke(new ImAbs(), "abc"));
        }

        [Test]
        public void TestImSinIdentity()
        {
            // sin(0+0i) = 0
            ValueEval result = Invoke(new ImSin(), "0");
            Assert.AreEqual(typeof(StringEval), result.GetType());
            Assert.AreEqual("0", ((StringEval)result).StringValue);
        }

        [Test]
        public void TestImSinPureReal()
        {
            // sin(pi/2 + 0i) = 1 + 0i = "1"
            ValueEval result = Invoke(new ImSin(), (Math.PI / 2).ToString("R", System.Globalization.CultureInfo.InvariantCulture));
            string formatted = ((StringEval)result).StringValue;
            // Real-only result should not have any 'i' suffix; allow tiny numerical wobble.
            double parsed = double.Parse(formatted, System.Globalization.CultureInfo.InvariantCulture);
            Assert.AreEqual(1, parsed, 1e-9);
        }

        [Test]
        public void TestImCosIdentity()
        {
            // cos(0+0i) = 1
            ValueEval result = Invoke(new ImCos(), "0");
            Assert.AreEqual(typeof(StringEval), result.GetType());
            Assert.AreEqual("1", ((StringEval)result).StringValue);
        }

        [Test]
        public void TestImSinPureImaginary()
        {
            // sin(0+i) = sin(0)cosh(1) + i cos(0)sinh(1) = 0 + i sinh(1)
            ValueEval result = Invoke(new ImSin(), "i");
            Assert.AreEqual(typeof(StringEval), result.GetType());
            string formatted = ((StringEval)result).StringValue;
            // Should end with "i" and the leading number ~ sinh(1) ~ 1.1752
            Assert.That(formatted, Does.EndWith("i"));
            string magnitude = formatted.Substring(0, formatted.Length - 1);
            double parsed = double.Parse(magnitude, System.Globalization.CultureInfo.InvariantCulture);
            Assert.AreEqual(Math.Sinh(1), parsed, 1e-9);
        }

        [Test]
        public void TestSuffixPropagation()
        {
            ValueEval result = Invoke(new ImSin(), "0+1j");
            Assert.AreEqual(typeof(StringEval), result.GetType());
            Assert.That(((StringEval)result).StringValue, Does.EndWith("j"));
        }

        private static double AsNumber(ValueEval result)
        {
            Assert.AreEqual(typeof(NumberEval), result.GetType(), "expected NumberEval, got " + result.GetType().Name);
            return ((NumberEval)result).NumberValue;
        }
    }
}
