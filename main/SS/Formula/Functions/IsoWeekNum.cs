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
    using System.Globalization;
    using NPOI.SS.Formula.Eval;
    using NPOI.SS.UserModel;

    /**
     * Implementation for Excel ISOWEEKNUM() function (Excel 2013+).
     * Syntax: ISOWEEKNUM(date)
     *
     * Returns the ISO 8601 week number of the year. Week 1 is the week
     * containing the first Thursday; weeks always run Monday..Sunday.
     */
    public class IsoWeekNum : Fixed1ArgFunction, FreeRefFunction
    {
        public static FreeRefFunction instance = new IsoWeekNum();

        public override ValueEval Evaluate(int srcRowIndex, int srcColumnIndex, ValueEval arg0)
        {
            double serial;
            try
            {
                serial = NumericFunction.SingleOperandEvaluate(arg0, srcRowIndex, srcColumnIndex);
            }
            catch (EvaluationException e)
            {
                return e.GetErrorEval();
            }

            DateTime date;
            try
            {
                date = DateUtil.GetJavaDate(serial, false);
            }
            catch (Exception)
            {
                return ErrorEval.NUM_ERROR;
            }

            int week = ISOWeek.GetWeekOfYear(date);
            return new NumberEval(week);
        }

        public ValueEval Evaluate(ValueEval[] args, OperationEvaluationContext ec)
        {
            return args.Length != 1 ? ErrorEval.VALUE_INVALID : Evaluate(ec.RowIndex, ec.ColumnIndex, args[0]);
        }

        /// <summary>
        /// .NET Framework on net472 lacks System.Globalization.ISOWeek, so we
        /// reproduce the small ISO-8601 week-number algorithm here.
        /// </summary>
        private static class ISOWeek
        {
            internal static int GetWeekOfYear(DateTime date)
            {
                // Shift so Monday=1..Sunday=7.
                int dayOfWeek = (int)date.DayOfWeek;
                if (dayOfWeek == 0)
                {
                    dayOfWeek = 7;
                }
                // The Thursday of the same ISO week determines the week's year.
                DateTime thursday = date.AddDays(4 - dayOfWeek);
                int yearStartDayOfYear = new DateTime(thursday.Year, 1, 1).DayOfYear;
                return (thursday.DayOfYear - yearStartDayOfYear) / 7 + 1;
            }
        }
    }
}
