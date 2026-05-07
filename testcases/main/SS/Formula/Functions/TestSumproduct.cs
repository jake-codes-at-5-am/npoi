/* ====================================================================
   Licensed to the Apache Software Foundation (ASF) under one or more
   contributor license agreements.  See the NOTICE file distributed with
   this work for Additional information regarding copyright ownership.
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
    using NUnit.Framework;
    using NPOI.SS.Formula.Functions;

    /**
     * Test cases for SUMPRODUCT()
     *
     * @author Josh Micich
     */
    [TestFixture]
    public class TestSumproduct
    {

        private static ValueEval invokeSumproduct(ValueEval[] args)
        {
            // srcCellRow and srcCellColumn are ignored by SUMPRODUCT
            return new Sumproduct().Evaluate(args, -1, (short)-1);
        }
        private static void ConfirmDouble(double expected, ValueEval actualEval)
        {
            if (!(actualEval is NumericValueEval))
            {
                Assert.Fail("Expected numeric result");
            }
            NumericValueEval nve = (NumericValueEval)actualEval;
            Assert.AreEqual(expected, nve.NumberValue, 0);
        }
        [Test]
        public void TestScalarSimple()
        {

            RefEval refEval = EvalFactory.CreateRefEval("A1", new NumberEval(3));
            ValueEval[] args = {
			refEval,
			new NumberEval(2),
		};
            ValueEval result = invokeSumproduct(args);
            ConfirmDouble(6D, result);
        }
        [Test]
        public void TestAreaSimple()
        {
            ValueEval[] aValues = {
			new NumberEval(2),
			new NumberEval(4),
			new NumberEval(5),
		};
            ValueEval[] bValues = {
			new NumberEval(3),
			new NumberEval(6),
			new NumberEval(7),
		};
            AreaEval aeA = EvalFactory.CreateAreaEval("A1:A3", aValues);
            AreaEval aeB = EvalFactory.CreateAreaEval("B1:B3", bValues);

            ValueEval[] args = { aeA, aeB, };
            ValueEval result = invokeSumproduct(args);
            ConfirmDouble(65D, result);
        }

        /**
         * For scalar products, the terms may be 1x1 area refs
         */
        [Test]
        public void TestOneByOneArea()
        {

            AreaEval ae = EvalFactory.CreateAreaEval("A1:A1", new ValueEval[] { new NumberEval(7), });

            ValueEval[] args = {
				ae,
				new NumberEval(2),
			};
            ValueEval result = invokeSumproduct(args);
            ConfirmDouble(14D, result);
        }
        [Test]
        public void TestMismatchAreaDimensions()
        {

            AreaEval aeA = EvalFactory.CreateAreaEval("A1:A3", new ValueEval[3]);
            AreaEval aeB = EvalFactory.CreateAreaEval("B1:D1", new ValueEval[3]);

            ValueEval[] args;
            args = new ValueEval[] { aeA, aeB, };
            Assert.AreEqual(ErrorEval.VALUE_INVALID, invokeSumproduct(args));

            args = new ValueEval[] { aeA, new NumberEval(5), };
            Assert.AreEqual(ErrorEval.VALUE_INVALID, invokeSumproduct(args));
        }
        [Test]
        public void TestAreaWithErrorCell()
        {
            ValueEval[] aValues = {
			ErrorEval.REF_INVALID,
			null,
		};
            AreaEval aeA = EvalFactory.CreateAreaEval("A1:A2", aValues);
            AreaEval aeB = EvalFactory.CreateAreaEval("B1:B2", new ValueEval[2]);

            ValueEval[] args = { aeA, aeB, };
            Assert.AreEqual(ErrorEval.REF_INVALID, invokeSumproduct(args));
        }

        // ───── ErrorEval propagation regression tests ─────
        // Pre-fix, passing an ErrorEval directly as a SUMPRODUCT argument crashed
        // with NPOI.Util.RuntimeException: "Invalid arg type for SUMPRODUCT (ErrorEval)".
        // Excel itself returns the error. These tests pin that behaviour so it
        // cannot silently regress.

        [Test]
        public void TestErrorEvalAsFirstArgPropagates()
        {
            ValueEval result = invokeSumproduct(new ValueEval[] { ErrorEval.REF_INVALID, new NumberEval(5) });
            Assert.AreEqual(ErrorEval.REF_INVALID, result, "first error must propagate as the result");
        }

        [Test]
        public void TestErrorEvalAsLaterArgPropagates()
        {
            // Second argument is the error; first is a normal numeric scalar.
            ValueEval result = invokeSumproduct(new ValueEval[] { new NumberEval(5), ErrorEval.NA });
            Assert.AreEqual(ErrorEval.NA, result);
        }

        [Test]
        public void TestErrorEvalThroughRefArgPropagates()
        {
            // ErrorEval delivered through a RefEval (the realistic path —
            // a referenced cell whose own formula evaluated to #DIV/0!).
            RefEval r = EvalFactory.CreateRefEval("A1", ErrorEval.DIV_ZERO);
            ValueEval result = invokeSumproduct(new ValueEval[] { r, new NumberEval(2) });
            Assert.AreEqual(ErrorEval.DIV_ZERO, result);
        }

        [Test]
        public void TestUnsupportedArgKindReturnsValueErrorNotRuntimeException()
        {
            // StringEval at the top level previously fell through to the
            // "Invalid arg type for SUMPRODUCT" RuntimeException. After the fix
            // it returns #VALUE! the way Excel does. (BoolEval is a NumericValueEval
            // so it works as 0/1; StringEval is the actual unsupported case.)
            ValueEval result = invokeSumproduct(new ValueEval[] { new StringEval("hello") });
            Assert.AreEqual(ErrorEval.VALUE_INVALID, result);
        }

        [Test]
        public void TestEmptyArgListReturnsValueError()
        {
            Assert.AreEqual(ErrorEval.VALUE_INVALID, invokeSumproduct(new ValueEval[0]));
        }

        [Test]
        public void TestMultipleErrorsReturnFirst()
        {
            // When multiple args are errors, propagate the first (left-to-right) —
            // matches Excel's evaluation order.
            ValueEval result = invokeSumproduct(new ValueEval[] {
                ErrorEval.NUM_ERROR, ErrorEval.NA, ErrorEval.REF_INVALID
            });
            Assert.AreEqual(ErrorEval.NUM_ERROR, result);
        }
    }

}