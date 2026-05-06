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
    using NPOI.SS.Formula.Eval;
    using NPOI.SS.Formula.Functions;
    using NUnit.Framework;

    /// <summary>
    /// Tests covering BIN2HEX, BIN2OCT, HEX2BIN, HEX2OCT, OCT2BIN and OCT2HEX.
    /// All six functions share the same conversion engine, so the test cases are
    /// grouped here to avoid duplicating the harness six times.
    /// </summary>
    [TestFixture]
    public class TestBaseConversionFunctions
    {
        private static ValueEval Invoke(Function func, params ValueEval[] args)
        {
            return func.Evaluate(args, -1, -1);
        }

        private static void ConfirmString(Function func, string expected, params ValueEval[] args)
        {
            ValueEval result = Invoke(func, args);
            Assert.AreEqual(typeof(StringEval), result.GetType(), "expected StringEval, got " + result.GetType().Name);
            Assert.AreEqual(expected, ((StringEval)result).StringValue);
        }

        private static void ConfirmError(Function func, ErrorEval expected, params ValueEval[] args)
        {
            ValueEval result = Invoke(func, args);
            Assert.AreEqual(typeof(ErrorEval), result.GetType(), "expected ErrorEval, got " + result.GetType().Name);
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void TestBin2HexBasic()
        {
            ConfirmString(new Bin2Hex(), "F", new StringEval("1111"));
            ConfirmString(new Bin2Hex(), "B", new StringEval("1011"));
            ConfirmString(new Bin2Hex(), "FF", new StringEval("11111111"));
            ConfirmString(new Bin2Hex(), "00FF", new StringEval("11111111"), new NumberEval(4));
        }

        [Test]
        public void TestBin2HexNegative()
        {
            // 1111111111b = -1 -> two's-complement hex over 10 chars
            ConfirmString(new Bin2Hex(), "FFFFFFFFFF", new StringEval("1111111111"));
            // 1111111110b = -2
            ConfirmString(new Bin2Hex(), "FFFFFFFFFE", new StringEval("1111111110"));
        }

        [Test]
        public void TestBin2HexErrors()
        {
            ConfirmError(new Bin2Hex(), ErrorEval.NUM_ERROR, new StringEval("21"));
            ConfirmError(new Bin2Hex(), ErrorEval.NUM_ERROR, new StringEval("11111111110"));
            ConfirmError(new Bin2Hex(), ErrorEval.NUM_ERROR, new StringEval("1"), new NumberEval(-1));
            ConfirmError(new Bin2Hex(), ErrorEval.VALUE_INVALID,
                new StringEval("1"), new NumberEval(0), new NumberEval(0));
        }

        [Test]
        public void TestBin2OctBasic()
        {
            ConfirmString(new Bin2Oct(), "11", new StringEval("1001"));
            ConfirmString(new Bin2Oct(), "0011", new StringEval("1001"), new NumberEval(4));
            ConfirmString(new Bin2Oct(), "777", new StringEval("111111111"));
        }

        [Test]
        public void TestBin2OctNegative()
        {
            ConfirmString(new Bin2Oct(), "7777777777", new StringEval("1111111111")); // -1
        }

        [Test]
        public void TestHex2BinBasic()
        {
            ConfirmString(new Hex2Bin(), "1111", new StringEval("F"));
            ConfirmString(new Hex2Bin(), "00000000", new StringEval("0"), new NumberEval(8));
            ConfirmString(new Hex2Bin(), "111111111", new StringEval("1FF"));
        }

        [Test]
        public void TestHex2BinNegative()
        {
            // FFFFFFFFFF -> -1 in 40-bit two's complement -> 1111111111 in 10-bit binary
            ConfirmString(new Hex2Bin(), "1111111111", new StringEval("FFFFFFFFFF"));
        }

        [Test]
        public void TestHex2BinOutOfRange()
        {
            // Decimal 1024 cannot be represented in 10-bit signed binary
            ConfirmError(new Hex2Bin(), ErrorEval.NUM_ERROR, new StringEval("400"));
        }

        [Test]
        public void TestHex2OctBasic()
        {
            ConfirmString(new Hex2Oct(), "10", new StringEval("8"));
            ConfirmString(new Hex2Oct(), "0000000010", new StringEval("8"), new NumberEval(10));
            // 0x1FFFFFFF = 2^29 - 1 -> octal 3777777777 (max positive HEX2OCT input)
            ConfirmString(new Hex2Oct(), "3777777777", new StringEval("1FFFFFFF"));
        }

        [Test]
        public void TestHex2OctNegative()
        {
            ConfirmString(new Hex2Oct(), "7777777777", new StringEval("FFFFFFFFFF"));
        }

        [Test]
        public void TestOct2BinBasic()
        {
            ConfirmString(new Oct2Bin(), "111", new StringEval("7"));
            ConfirmString(new Oct2Bin(), "00000111", new StringEval("7"), new NumberEval(8));
        }

        [Test]
        public void TestOct2BinNegative()
        {
            ConfirmString(new Oct2Bin(), "1111111111", new StringEval("7777777777"));
        }

        [Test]
        public void TestOct2HexBasic()
        {
            ConfirmString(new Oct2Hex(), "8", new StringEval("10"));
            ConfirmString(new Oct2Hex(), "0008", new StringEval("10"), new NumberEval(4));
        }

        [Test]
        public void TestOct2HexNegative()
        {
            // 30-bit octal value -1 -> sign-extended to 40-bit hex
            ConfirmString(new Oct2Hex(), "FFFFFFFFFF", new StringEval("7777777777"));
        }

        [Test]
        public void TestPlacesArgumentEdgeCases()
        {
            // places of 0 (or omitted) returns minimum representation
            ConfirmString(new Bin2Hex(), "F", new StringEval("1111"), new NumberEval(0));
            // requesting fewer places than required yields #NUM!
            ConfirmError(new Bin2Hex(), ErrorEval.NUM_ERROR, new StringEval("11111111"), new NumberEval(1));
            // BlankEval for places acts as omitted
            ConfirmString(new Bin2Hex(), "F", new StringEval("1111"), BlankEval.instance);
            // string-coerced places work too
            ConfirmString(new Bin2Hex(), "00F", new StringEval("1111"), new StringEval("3"));
        }
    }
}
