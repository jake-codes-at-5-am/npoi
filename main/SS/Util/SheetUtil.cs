﻿/* ====================================================================
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

namespace NPOI.SS.Util
{
    using NPOI.SS.UserModel;
    using SixLabors.Fonts;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// Helper methods for when working with Usermodel sheets
    /// @author Yegor Kozlov
    /// </summary>
    public class SheetUtil
    {

        // /**
        // * Excel measures columns in units of 1/256th of a character width
        // * but the docs say nothing about what particular character is used.
        // * '0' looks to be a good choice.
        // */

        // ====== Default Constant ======
        private const char defaultChar = '0';
        private static readonly int dpi = 144;
        private const int CELL_PADDING_PIXEL = 8;
        private const int DEFAULT_PADDING_PIXEL = 20;
        private const double WIDTH_CORRECTION = 1.05;
        private const double MAXIMUM_ROW_HEIGH_IN_POINTS = 409.5;
        private const double POINTS_PER_INCH = 72.0;
        private const double HEIGHT_POINT_CORRECTION = 1.33;
        private static readonly int SixLaborsFontsMajorVersion = typeof(SixLabors.Fonts.Font).Assembly.GetName().Version.Major;
        // /**
        // * This is the multiple that the font height is scaled by when determining the
        // * boundary of rotated text.
        // */
        //private static double fontHeightMultiple = 2.0;

        /**
         *  Dummy formula Evaluator that does nothing.
         *  YK: The only reason of having this class is that
         *  {@link NPOI.SS.UserModel.DataFormatter#formatCellValue(NPOI.SS.UserModel.Cell)}
         *  returns formula string for formula cells. Dummy Evaluator Makes it to format the cached formula result.
         *
         *  See Bugzilla #50021 
         */
        private static IFormulaEvaluator dummyEvaluator = new DummyEvaluator();
        public class DummyEvaluator : IFormulaEvaluator
        {
            public void ClearAllCachedResultValues() { }
            public void NotifySetFormula(ICell cell) { }
            public void NotifyDeleteCell(ICell cell) { }
            public void NotifyUpdateCell(ICell cell) { }
            public CellValue Evaluate(ICell cell) { return null; }
            public ICell EvaluateInCell(ICell cell) { return null; }
            public bool IgnoreMissingWorkbooks { get; set; }
            public void SetupReferencedWorkbooks(Dictionary<String, IFormulaEvaluator> workbooks) { }
            public void EvaluateAll() { }

            public CellType EvaluateFormulaCell(ICell cell)
            {
                return cell.CachedFormulaResultType;
            }

            public bool DebugEvaluationOutputForNextEval
            {
                get
                {
                    throw new NotImplementedException();
                }
                set
                {
                    throw new NotImplementedException();
                }
            }
        }

        public sealed class MergeIndex
        {
            private readonly Dictionary<int, List<CellRangeAddress>> _rows = new();

            public static MergeIndex Build(ISheet sheet)
            {
                var idx = new MergeIndex();
                for (int i = 0; i < sheet.NumMergedRegions; i++)
                {
                    var region = sheet.GetMergedRegion(i);
                    for (int row = region.FirstRow; row <= region.LastRow; row++)
                    {
                        if (!idx._rows.TryGetValue(row, out var list))
                            idx._rows[row] = list = new();
                        list.Add(region);
                    }
                }
                return idx;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool TryGetRegion(int row, int col, out CellRangeAddress region)
            {
                if (_rows.TryGetValue(row, out var list))
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        var r = list[i];
                        if (r.FirstColumn <= col && col <= r.LastColumn &&
                            r.FirstRow <= row && row <= r.LastRow)
                        {
                            region = r;
                            return true;
                        }
                    }
                }
                region = null;
                return false;
            }

            public IEnumerable<CellRangeAddress> RegionsForRow(int row)
                => _rows.TryGetValue(row, out var list) ? list.Distinct() : Enumerable.Empty<CellRangeAddress>();
        }

        public static IRow CopyRow(ISheet sourceSheet, int sourceRowIndex, ISheet targetSheet, int targetRowIndex)
        {
            // Get the source / new row
            IRow newRow = targetSheet.GetRow(targetRowIndex);
            IRow sourceRow = sourceSheet.GetRow(sourceRowIndex);

            // If the row exist in destination, push down all rows by 1 else create a new row
            if (newRow != null)
            {
                targetSheet.RemoveRow(newRow);
            }
            newRow = targetSheet.CreateRow(targetRowIndex);
            if (sourceRow == null)
                throw new ArgumentNullException("source row doesn't exist");
            // Loop through source columns to add to new row
            for (int i = sourceRow.FirstCellNum; i < sourceRow.LastCellNum; i++)
            {
                // Grab a copy of the old/new cell
                ICell oldCell = sourceRow.GetCell(i);

                // If the old cell is null jump to next cell
                if (oldCell == null)
                {
                    continue;
                }
                ICell newCell = newRow.CreateCell(i);

                if (oldCell.CellStyle != null)
                {
                    // apply style from old cell to new cell 
                    newCell.CellStyle = oldCell.CellStyle;
                }

                // If there is a cell comment, copy
                if (oldCell.CellComment != null)
                {
                    sourceSheet.CopyComment(oldCell, newCell);
                }

                // If there is a cell hyperlink, copy
                if (oldCell.Hyperlink != null)
                {
                    newCell.Hyperlink = oldCell.Hyperlink;
                }

                // Set the cell data type
                newCell.SetCellType(oldCell.CellType);

                // Set the cell data value
                switch (oldCell.CellType)
                {
                    case CellType.Blank:
                        newCell.SetCellValue(oldCell.StringCellValue);
                        break;
                    case CellType.Boolean:
                        newCell.SetCellValue(oldCell.BooleanCellValue);
                        break;
                    case CellType.Error:
                        newCell.SetCellErrorValue(oldCell.ErrorCellValue);
                        break;
                    case CellType.Formula:
                        newCell.SetCellFormula(oldCell.CellFormula);
                        break;
                    case CellType.Numeric:
                        newCell.SetCellValue(oldCell.NumericCellValue);
                        break;
                    case CellType.String:
                        newCell.SetCellValue(oldCell.RichStringCellValue);
                        break;
                }
            }

            // If there are are any merged regions in the source row, copy to new row
            for (int i = 0; i < sourceSheet.NumMergedRegions; i++)
            {
                CellRangeAddress cellRangeAddress = sourceSheet.GetMergedRegion(i);

                if (cellRangeAddress != null && cellRangeAddress.FirstRow == sourceRow.RowNum)
                {
                    CellRangeAddress newCellRangeAddress = new CellRangeAddress(newRow.RowNum,
                            (newRow.RowNum +
                                    (cellRangeAddress.LastRow - cellRangeAddress.FirstRow
                                            )),
                            cellRangeAddress.FirstColumn,
                            cellRangeAddress.LastColumn);
                    targetSheet.AddMergedRegion(newCellRangeAddress);
                }
            }
            return newRow;
        }

        public static IRow CopyRow(ISheet sheet, int sourceRowIndex, int targetRowIndex)
        {
            if (sourceRowIndex == targetRowIndex)
                throw new ArgumentException("sourceIndex and targetIndex cannot be same");
            // Get the source / new row
            IRow newRow = sheet.GetRow(targetRowIndex);
            IRow sourceRow = sheet.GetRow(sourceRowIndex);

            // If the row exist in destination, push down all rows by 1 else create a new row
            if (newRow != null)
            {
                sheet.ShiftRows(targetRowIndex, sheet.LastRowNum, 1);
            }

            if (sourceRow != null)
            {
                newRow = sheet.CreateRow(targetRowIndex);
                newRow.Height = sourceRow.Height;   //copy row height

                // Loop through source columns to add to new row
                for (int i = sourceRow.FirstCellNum; i < sourceRow.LastCellNum; i++)
                {
                    // Grab a copy of the old/new cell
                    ICell oldCell = sourceRow.GetCell(i);

                    // If the old cell is null jump to next cell
                    if (oldCell == null)
                    {
                        continue;
                    }
                    ICell newCell = newRow.CreateCell(i);

                    if (oldCell.CellStyle != null)
                    {
                        // apply style from old cell to new cell 
                        newCell.CellStyle = oldCell.CellStyle;
                    }

                    // If there is a cell comment, copy
                    if (oldCell.CellComment != null)
                    {
                        sheet.CopyComment(oldCell, newCell);
                    }

                    // If there is a cell hyperlink, copy
                    if (oldCell.Hyperlink != null)
                    {
                        newCell.Hyperlink = oldCell.Hyperlink;
                    }

                    // Set the cell data type
                    newCell.SetCellType(oldCell.CellType);

                    // Set the cell data value
                    switch (oldCell.CellType)
                    {
                        case CellType.Blank:
                            newCell.SetCellValue(oldCell.StringCellValue);
                            break;
                        case CellType.Boolean:
                            newCell.SetCellValue(oldCell.BooleanCellValue);
                            break;
                        case CellType.Error:
                            newCell.SetCellErrorValue(oldCell.ErrorCellValue);
                            break;
                        case CellType.Formula:
                            newCell.SetCellFormula(oldCell.CellFormula);
                            break;
                        case CellType.Numeric:
                            newCell.SetCellValue(oldCell.NumericCellValue);
                            break;
                        case CellType.String:
                            newCell.SetCellValue(oldCell.RichStringCellValue);
                            break;
                    }
                }

                // If there are are any merged regions in the source row, copy to new row
                for (int i = 0; i < sheet.NumMergedRegions; i++)
                {
                    CellRangeAddress cellRangeAddress = sheet.GetMergedRegion(i);
                    if (cellRangeAddress != null && cellRangeAddress.FirstRow == sourceRow.RowNum)
                    {
                        CellRangeAddress newCellRangeAddress = new CellRangeAddress(newRow.RowNum,
                                (newRow.RowNum +
                                        (cellRangeAddress.LastRow - cellRangeAddress.FirstRow
                                                )),
                                cellRangeAddress.FirstColumn,
                                cellRangeAddress.LastColumn);
                        sheet.AddMergedRegion(newCellRangeAddress);
                    }
                }
            }

            return newRow;
        }

        public static double GetRowHeight(IRow row, bool useMergedCells, int firstColumnIdx, int lastColumnIdx, MergeIndex merge = null)
        {
            if (row == null)
                return 0;

            merge ??= MergeIndex.Build(row.Sheet);
            double height = 0;

            for (int cellIdx = firstColumnIdx; cellIdx <= lastColumnIdx; ++cellIdx)
            {
                ICell cell = row.GetCell(cellIdx);
                if (row != null && cell != null)
                {
                    double cellHeight = GetCellHeight(cell, useMergedCells, merge);
                    height = Math.Max(height, cellHeight);
                }
            }

            return height;
        }

        public static double GetRowHeight(ISheet sheet, int rowIdx, bool useMergedCells, int firstColumnIdx, int lastColumnIdx)
        {
            IRow row = sheet.GetRow(rowIdx);
            if (row == null)
                return 0;

            var merge = MergeIndex.Build(sheet);
            return GetRowHeight(row, useMergedCells, firstColumnIdx, lastColumnIdx, merge);
        }

        public static double GetRowHeight(IRow row, bool useMergedCells, MergeIndex merge = null)
        {
            if (row == null)
            {
                return -1;
            }

            double rowHeight = -1;
            merge ??= MergeIndex.Build(row.Sheet);

            foreach (var cell in row.Cells)
            {
                double cellHeight = GetCellHeight(cell, useMergedCells, merge);
                rowHeight = Math.Max(rowHeight, cellHeight);
            }

            return rowHeight;
        }

        public static double GetRowHeight(ISheet sheet, int rowIdx, bool useMergedCells)
        {
            IRow row = sheet.GetRow(rowIdx);
            if (row == null)
                return -1;

            var merge = MergeIndex.Build(sheet);
            double defaultPt = sheet.DefaultRowHeightInPoints;

            // Base height (POINTS) from truly NON-MERGED cells only
            double basePointSelected = RowBaseFromNonMergedPoints(row, merge);
            double finalPt = basePointSelected;

            // ---------- Rule 1: useMergedCells == false ----------
            // Ignore ALL merged cells (horizontal/vertical/rectangular). Selected row only.
            if (!useMergedCells)
                return Math.Min(finalPt, MAXIMUM_ROW_HEIGH_IN_POINTS);

            // ---------- Rule 2: useMergedCells == true ----------

            // 2a) Horizontal-only merges on the selected row: ensure the row can fit them.
            foreach (var region in merge.RegionsForRow(rowIdx))
            {
                if (region.FirstRow == region.LastRow) // merged columns only (same row)
                {
                    double mergedPoint = MergedBlockTotalPoints(sheet, region); // POINTS
                    if (mergedPoint > finalPt)
                        finalPt = mergedPoint;
                }
            }

            // 2b) Vertical/rectangular merges: fit content across the whole merged area.
            var seen = new HashSet<(int fr, int fc, int lr, int lc)>();
            foreach (var region in merge.RegionsForRow(rowIdx))
            {
                if (region.LastRow == region.FirstRow)
                    continue; // skip horizontal-only here

                var key = (region.FirstRow, region.FirstColumn, region.LastRow, region.LastColumn);
                if (!seen.Add(key))
                    continue;

                int r0 = region.FirstRow, r1 = region.LastRow, n = r1 - r0 + 1;

                // Gather bases per row (POINTS) from NON-MERGED cells only
                var basePt = new double[n];
                var hasContent = new bool[n];
                for (int r = r0; r <= r1; r++)
                {
                    var rr = sheet.GetRow(r) ?? sheet.CreateRow(r);
                    double b = RowBaseFromNonMergedPoints(rr, merge); // POINTS; ignores any merged cells
                    basePt[r - r0] = b;
                    hasContent[r - r0] = RowHasNonMergedContent(rr, merge) || b > defaultPt + 0.1;
                }

                // Total height needed for the merged block (POINTS)
                double totalPt = MergedBlockTotalPoints(sheet, region);

                // Baseline assignment: content rows get their base; others start at default
                var assign = new double[n];
                double sumContent = 0;
                int emptyCount = 0;
                for (int i = 0; i < n; i++)
                {
                    assign[i] = hasContent[i] ? basePt[i] : defaultPt;
                    if (hasContent[i])
                        sumContent += basePt[i];
                    else
                        emptyCount++;
                }

                if (emptyCount == n)
                {
                    // No non-merged content in any row: split evenly (but never below base/default)
                    double even = Math.Max(defaultPt, totalPt / n);
                    for (int i = 0; i < n; i++)
                        assign[i] = Math.Max(basePt[i], even);
                }
                else
                {
                    // Some rows have content: distribute the remainder
                    double remainder = Math.Max(0, totalPt - sumContent);
                    if (remainder > 0)
                    {
                        if (emptyCount > 0)
                        {
                            // Spread remainder evenly across rows without non-merged content
                            double perEmpty = Math.Max(defaultPt, remainder / emptyCount);
                            for (int i = 0; i < n; i++)
                                if (!hasContent[i])
                                    assign[i] = Math.Max(assign[i], perEmpty);
                        }
                        else
                        {
                            // All rows have content: add remainder to the row with the highest base
                            int anchor = 0;
                            double best = basePt[0];
                            for (int i = 1; i < n; i++)
                                if (basePt[i] > best)
                                { best = basePt[i]; anchor = i; }
                            assign[anchor] = basePt[anchor] + remainder;
                        }
                    }
                }

                // Apply heights to all rows in the merged region (POINTS), clamped to Excel max
                for (int i = 0; i < n; i++)
                {
                    var rr = sheet.GetRow(r0 + i) ?? sheet.CreateRow(r0 + i);
                    double current = rr.HeightInPoints > 0 ? rr.HeightInPoints : defaultPt;
                    double target = Math.Min(assign[i], MAXIMUM_ROW_HEIGH_IN_POINTS);
                    rr.HeightInPoints = (float)Math.Max(current, target);
                }

                // Track selected row’s assigned height for return value
                double selectedAssigned = Math.Min(assign[rowIdx - r0], MAXIMUM_ROW_HEIGH_IN_POINTS);
                if (selectedAssigned > finalPt)
                    finalPt = selectedAssigned;
            }

            return Math.Min(finalPt, MAXIMUM_ROW_HEIGH_IN_POINTS);
        }

        public static double GetCellHeight(ICell cell, bool useMergedCells, MergeIndex merge = null)
        {
            if (cell == null)
                return 0;
            merge ??= MergeIndex.Build(cell.Sheet);
            var mergedRegion = GetMergedRegionForCell(cell, merge);
            if (mergedRegion == null || !useMergedCells)
            {
                // Not merged
                return cell.CellStyle.WrapText
                    ? MeasureWrapTextHeight(cell, cell.ColumnIndex, cell.ColumnIndex, cell.RowIndex, cell.RowIndex)
                    : GetActualHeight(cell);
            }

            // Use the cell at the top-left of the merged region for the value
            var topLeftCell = cell.Sheet.GetRow(mergedRegion.FirstRow)?.GetCell(mergedRegion.FirstColumn);
            if (topLeftCell == null)
                return cell.Sheet.DefaultRowHeightInPoints;


            int mergedRowCount = 1 + mergedRegion.LastRow - mergedRegion.FirstRow;
            double mergedWidth = 0;

            mergedWidth = GetMergedPixelWidth(cell.Sheet, mergedRegion.FirstRow, mergedRegion.FirstColumn, mergedRegion.LastRow, mergedRegion.LastColumn, cell);

            // Measure the total height for the text, with all columns combined
            double totalHeight = MeasureWrapTextHeight(topLeftCell, mergedRegion.FirstColumn, mergedRegion.LastColumn, mergedRegion.FirstRow, mergedRegion.LastRow, mergedWidth);

            // Divide height over all rows in merged region
            return totalHeight / Math.Max(mergedRowCount, 1);
        }

        /// <summary>
        /// Converts Excel's column width (units of 1/256th of a character width) to pixels.
        /// </summary>
        private static float GetColumnWidthInPixels(ISheet sheet, int columnIndex, ICell cell)
        {
            sheet.GetType();
            var type = sheet.GetType();
            // 1. Get the width in terms of number of default characters
            double widthInChars = sheet.GetColumnWidth(columnIndex) / 256.0;

            // 2. Get the pixel width of a single default character
            int defaultCharWidth = GetDefaultCharWidth(sheet.Workbook);

            // 3. check is HSSFSheet or not (old format .xls) if true, return with slight pixel adjustment. if false, return normal calclation
            return sheet is NPOI.HSSF.UserModel.HSSFSheet ? (float)(widthInChars * defaultCharWidth) * (float)WIDTH_CORRECTION : (float)(widthInChars * defaultCharWidth);
        }
        private static ICell GetFirstCellFromMergedRegion(ICell cell)
        {
            foreach (var region in cell.Sheet.MergedRegions)
            {
                if (region.IsInRange(cell.RowIndex, cell.ColumnIndex))
                {
                    return cell.Sheet.GetRow(region.FirstRow).GetCell(region.FirstColumn);
                }
            }

            return cell;
        }

        private static double GetActualHeight(ICell cell)
        {
            string? stringValue = GetCellStringValue(cell);

            if (string.IsNullOrEmpty(stringValue))
                return 0;

            var style = cell.CellStyle;
            var windowsFont = GetWindowsFont(cell);

            if(!style.WrapText && style.Rotation == 0 && stringValue.IndexOf('\n') < 0)
            {
                var lineHeight = GetLineHeight(windowsFont);
                return Math.Round(lineHeight, 0, MidpointRounding.ToEven);
            }

            if (style.Rotation != 0)
            {
                return GetRotatedContentHeight(cell, stringValue, windowsFont);
            }

            return GetContentHeight(stringValue, windowsFont, cell);
        }

        private static int GetNumberOfRowsInMergedRegion(ICell cell)
        {
            foreach (var region in cell.Sheet.MergedRegions)
            {
                if (region.IsInRange(cell.RowIndex, cell.ColumnIndex))
                {
                    return 1 + region.LastRow - region.FirstRow;
                }
            }

            return 1;
        }

        private static double GetCellContentHeight(double actualHeight, int numberOfRowsInMergedRegion)
        {
            return numberOfRowsInMergedRegion <= 1 ? actualHeight : Math.Max(-1, actualHeight / numberOfRowsInMergedRegion);
        }

        private static string GetCellStringValue(ICell cell)
        {
            CellType cellType = cell.CellType == CellType.Formula ? cell.CachedFormulaResultType : cell.CellType;

            if (cellType == CellType.String)
            {
                return cell.RichStringCellValue.String;
            }

            if (cellType == CellType.Boolean)
            {
                return cell.BooleanCellValue.ToString().ToUpper() + defaultChar;
            }

            if (cellType == CellType.Numeric)
            {
                string stringValue;

                try
                {
                    DataFormatter formatter = new DataFormatter();
                    stringValue = formatter.FormatCellValue(cell, dummyEvaluator);
                }
                catch
                {
                    stringValue = cell.NumericCellValue.ToString();
                }

                return stringValue + defaultChar;
            }

            return null;
        }

        private static Font GetWindowsFont(ICell cell)
        {
            var wb = cell.Sheet.Workbook;
            var style = cell.CellStyle;
            var font = wb.GetFontAt(style.FontIndex);

            return IFont2Font(font);
        }

        private static double GetRotatedContentHeight(ICell cell, string stringValue, Font windowsFont)
        {
            var angle = cell.CellStyle.Rotation * 2.0 * Math.PI / 360.0;
            var measureResult = TextMeasurer.MeasureAdvance(stringValue, new TextOptions(windowsFont) { Dpi = dpi });

            var x1 = Math.Abs(measureResult.Height * Math.Cos(angle));
            var x2 = Math.Abs(measureResult.Width * Math.Sin(angle));

            return Math.Round(x1 + x2, 0, MidpointRounding.ToEven);
        }

        private static double GetContentHeight(string stringValue, Font windowsFont, ICell cell)
        {
            TextOptions options = new(windowsFont) { Dpi = dpi };
            if (cell.CellStyle.WrapText)
            {
                ISheet sheet = cell.Sheet;
                int columnIndex = cell.ColumnIndex;
                var pixelWidth = GetColumnWidthInPixels(sheet, columnIndex, cell);
                options.WrappingLength = pixelWidth <= 0
                    ? (float)sheet.GetColumnWidth(columnIndex)
                    : pixelWidth;
            }
            var measureResult = TextMeasurer.MeasureAdvance(stringValue,options);

            return Math.Round(measureResult.Height, 0, MidpointRounding.ToEven);
        }

        /// <summary>
        /// Measures the height of a cell when wrap text is applied.
        /// </summary>
        /// <param name="cell">The cell whose height will be calculated.</param>
        /// <param name="firstCol">The first column index for width calculation.</param>
        /// <param name="lastCol">The last column index for width calculation.</param>
        /// <param name="customMergedWidth">If specified, the total width (in pixels) to use for wrapping.</param>
        /// <returns>The calculated height of the cell in pixels.</returns>
        private static double MeasureWrapTextHeight(
            ICell cell,
            int firstCol,
            int lastCol,
            int firstRow,
            int lastRow,
            double? customMergedWidth = null)
        {
            if (cell == null || cell.Row == null)
                return cell?.Sheet?.DefaultRowHeightInPoints ?? 0;

            ISheet sheet = cell.Sheet;
            string text = GetCellStringValue(cell);
            if (string.IsNullOrEmpty(text))
                return sheet.DefaultRowHeightInPoints;

            // Determine the font to use
            Font font = GetWindowsFont(cell);

            // Determine the width in pixels to wrap by (sum columns if merged)
            double wrapWidthPixels = customMergedWidth ?? 0;
            if (!customMergedWidth.HasValue)
            {
                // Account for merged pixel width

                // check version of SixLabors.Fonts for calculation
                if (SixLaborsFontsMajorVersion >= 2)
                {
                    var numberOfColumns = lastCol - firstCol + 1;
                    wrapWidthPixels = GetMergedPixelWidth(cell.Sheet, firstRow, firstCol, lastRow, lastCol, cell) + numberOfColumns * CELL_PADDING_PIXEL;
                }
                else
                {
                    wrapWidthPixels = GetMergedPixelWidth(cell.Sheet, firstRow, firstCol, lastRow, lastCol, cell);
                }
            }

            wrapWidthPixels = wrapWidthPixels <= 0 ? DEFAULT_PADDING_PIXEL : wrapWidthPixels; // fallback
            wrapWidthPixels = Math.Ceiling(wrapWidthPixels);

            var cacheOptions = GetTextOptions(font);

            var textOptions = new TextOptions(cacheOptions.Font)
            {
                Dpi = cacheOptions.Dpi,
                WrappingLength = (float)wrapWidthPixels
            };

            FontRectangle totalBounds = TextMeasurer.MeasureAdvance(text, textOptions);
            return Math.Round(totalBounds.Height, 0, MidpointRounding.ToEven);
        }


        /**
         * Compute width of a single cell
         *
         * @param cell the cell whose width is to be calculated
         * @param defaultCharWidth the width of a single character
         * @param formatter formatter used to prepare the text to be measured
         * @param useMergedCells    whether to use merged cells
         * @return  the width in pixels or -1 if cell is empty
         */
        public static double GetCellWidth(ICell cell, int defaultCharWidth, DataFormatter formatter, bool useMergedCells)
        {
            ISheet sheet = cell.Sheet;
            IWorkbook wb = sheet.Workbook;
            IRow row = cell.Row;
            int column = cell.ColumnIndex;

            int colspan = 1;
            for (int i = 0; i < sheet.NumMergedRegions; i++)
            {
                CellRangeAddress region = sheet.GetMergedRegion(i);
                if (ContainsCell(region, row.RowNum, column))
                {
                    if (!useMergedCells)
                    {
                        // If we're not using merged cells, skip this one and move on to the next.
                        return -1;
                    }
                    cell = row.GetCell(region.FirstColumn);
                    colspan = 1 + region.LastColumn - region.FirstColumn;
                }
            }

            ICellStyle style = cell.CellStyle;
            CellType cellType = cell.CellType;

            // for formula cells we compute the cell width for the cached formula result
            if (cellType == CellType.Formula)
                cellType = cell.CachedFormulaResultType;

            IFont font = wb.GetFontAt(style.FontIndex);
            Font windowsFont = IFont2Font(font);

            double width = -1;

            if (cellType == CellType.String)
            {
                IRichTextString rt = cell.RichStringCellValue;
                String[] lines = rt.String.Split("\n".ToCharArray());
                for (int i = 0; i < lines.Length; i++)
                {
                    String txt = lines[i];

                    //AttributedString str = new AttributedString(txt);
                    //copyAttributes(font, str, 0, txt.length());
                    if (rt.NumFormattingRuns > 0)
                    {
                        // TODO: support rich text fragments
                    }

                    width = GetCellWidth(defaultCharWidth, colspan, style, width, txt, windowsFont, cell);
                }
            }
            else
            {
                String sval = null;
                if (cellType == CellType.Numeric)
                {
                    // Try to get it formatted to look the same as excel
                    try
                    {
                        sval = formatter.FormatCellValue(cell, dummyEvaluator);
                    }
                    catch
                    {
                        sval = cell.NumericCellValue.ToString();
                    }
                }
                else if (cellType == CellType.Boolean)
                {
                    sval = cell.BooleanCellValue.ToString().ToUpper();
                }
                if (sval != null)
                {
                    String txt = sval;
                    //str = new AttributedString(txt);
                    //copyAttributes(font, str, 0, txt.length());
                    width = GetCellWidth(defaultCharWidth, colspan, style, width, txt, windowsFont, cell);
                }
            }

            return width;
        }

        private static double GetCellWidth(
            int defaultCharWidth,
            int colspan,
            ICellStyle style,
            double width,
            string str,
            Font windowsFont,
            ICell cell)
        {
            // If the string is null or empty, no calculation is needed.
            if (string.IsNullOrEmpty(str))
            {
                return width;
            }

            // Use ReadOnlySpan for zero-allocation trimming and slicing.
            ReadOnlySpan<char> textSpan = str.AsSpan();
            ReadOnlySpan<char> trimmedSpan = textSpan.Trim();

            var cacheOptions = GetTextOptions(windowsFont);

            // --- Consolidate Text Measurement ---
            // 1. Measure a single space. This is needed for leading/trailing spaces.
            float spaceWidth = GetSpaceWidth(cacheOptions.Font);

            // 2. Measure the trimmed text content. Use a fallback for valid height on empty/whitespace strings.
            // This single call gets us both width and height, reducing measurement overhead.
            var contentToMeasure = trimmedSpan.IsEmpty ? "A".AsSpan() : trimmedSpan;
            var contentSize = TextMeasurer.MeasureSize(contentToMeasure, cacheOptions);

            float trimmedWidth = trimmedSpan.IsEmpty ? 0f : contentSize.Width;
            float lineHeight = contentSize.Height;

            // Calculate the total unrotated width more directly.
            int totalSpaces = textSpan.Length - trimmedSpan.Length;
            double baseWidth = trimmedWidth + (totalSpaces * spaceWidth);

            // --- Rotation Logic ---
            double actualWidth;

            switch (style.Rotation)
            {
                case 0: // No rotation
                    actualWidth = baseWidth;
                    break;

                default: // Angled rotation
                    double angle = style.Rotation * Math.PI / 180.0;
                    // The bounding box of a rotated rectangle.
                    actualWidth = Math.Abs(lineHeight * Math.Sin(angle)) + Math.Abs(baseWidth * Math.Cos(angle));
                    break;
            }

            // Round the final pixel width once.
            double roundedWidth = Math.Round(actualWidth, 0, MidpointRounding.ToEven);

            // --- Final Calculation ---
            int padding = CELL_PADDING_PIXEL;
            double correction = SixLaborsFontsMajorVersion >= 2 ? 1.0 : WIDTH_CORRECTION;
            int safeColspan = Math.Max(colspan, 1); // Avoid division by zero.

            double finalWidth = (roundedWidth + padding) / safeColspan / defaultCharWidth * correction;      
            return Math.Max(width, finalWidth);
        }

        // --- Units ---
        private static double PxToPt(double px) => px * (POINTS_PER_INCH / dpi);

        // Measure a single cell (no merged allocation). Returns POINTS.
        private static double MeasureCellHeightPoints(ICell cell)
        {
            if (cell == null)
                return -1;

            double hPx = cell.CellStyle.WrapText
                ? MeasureWrapTextHeight(cell, cell.ColumnIndex, cell.ColumnIndex, cell.RowIndex, cell.RowIndex)
                : GetActualHeight(cell);

            if (hPx <= 0)
                return cell.Sheet.DefaultRowHeightInPoints;

            double finalWidth = (hPx + CELL_PADDING_PIXEL) * WIDTH_CORRECTION;

            return PxToPt(hPx) * HEIGHT_POINT_CORRECTION;

        }

        // True if the cell is inside any merged region (horizontal, vertical, or rectangular)
        private static bool InAnyMerge(ICell cell, MergeIndex merge, out CellRangeAddress region)
        {
            region = GetMergedRegionForCell(cell, merge);
            return region != null;
        }

        private static bool RowHasNonMergedContent(IRow row, MergeIndex merge)
        {
            if (row == null)
                return false;

            foreach (var cell in row.Cells)
            {
                if (cell == null)
                    continue;
                if (InAnyMerge(cell, merge, out _))
                    continue; // skip any merged cell

                var type = cell.CellType == CellType.Formula ? cell.CachedFormulaResultType : cell.CellType;
                switch (type)
                {
                    case CellType.String:
                        if (!string.IsNullOrEmpty(cell.RichStringCellValue?.String))
                            return true;
                        break;
                    case CellType.Numeric:
                    case CellType.Boolean:
                    case CellType.Error:
                        return true;
                }
            }
            return false;
        }

        // Base height for a row from NON-MERGED cells only (POINTS).
        private static double RowBaseFromNonMergedPoints(IRow row, SheetUtil.MergeIndex merge)
        {
            if (row == null)
                return -1;

            double basePt = -1;
            foreach (var cell in row.Cells)
            {
                if (cell == null)
                    continue;
                if (InAnyMerge(cell, merge, out _))
                    continue; // ignore vertical merges here
                double hPt = MeasureCellHeightPoints(cell);        // POINTS
                if (hPt > basePt)
                    basePt = hPt;
            }
            if (basePt < 0)
                basePt = row.Sheet.DefaultRowHeightInPoints;
            return basePt;
        }

        // Total height of merged wrap-text block (POINTS), ignoring rotation,
        // measured using top-left cell's text/style and the SUM of merged column widths.
        private static double MergedBlockTotalPoints(ISheet sheet, CellRangeAddress region)
        {
            var tl = sheet.GetRow(region.FirstRow)?.GetCell(region.FirstColumn);
            if (tl == null)
                return sheet.DefaultRowHeightInPoints;

            // Sum merged columns width (px)
            double mergedWidthPx = GetMergedPixelWidth(sheet, region.FirstRow, region.FirstColumn, region.LastRow, region.LastColumn, tl);

            // var ver = typeof(SixLabors.Fonts.Font).Assembly.GetName().Version; // e.g., 2.1.3.0 vs 1.0.0.0

            if (SixLaborsFontsMajorVersion >= 2)
            {
                var n = region.LastColumn - region.FirstColumn + 1;
                mergedWidthPx = Math.Ceiling(Math.Max(mergedWidthPx + n * CELL_PADDING_PIXEL, DEFAULT_PADDING_PIXEL));
            }
            else
            {
                mergedWidthPx = Math.Ceiling(Math.Max(mergedWidthPx, DEFAULT_PADDING_PIXEL));
            }

            // use the FULL region span (rows + columns), not just the TL row.
            double totalPx = MeasureWrapTextHeight(
                tl,
                region.FirstColumn,
                region.LastColumn,
                region.FirstRow,
                region.LastRow,
                mergedWidthPx);

            if (totalPx <= 0)
                return sheet.DefaultRowHeightInPoints;

            // Return POINTS so callers compare/assign correctly

            return PxToPt(totalPx) * HEIGHT_POINT_CORRECTION;
        }

        // /**
        // * Drawing context to measure text
        // */
        //private static FontRenderContext fontRenderContext = new FontRenderContext(null, true, true);

        /**
         * Compute width of a column and return the result
         *
         * @param sheet the sheet to calculate
         * @param column    0-based index of the column
         * @param useMergedCells    whether to use merged cells
         * @param maxRows   limit the scope to maxRows rows to speed up the function, or leave 0 (optional)
         * @return  the width in pixels or -1 if all cells are empty
         */
        public static double GetColumnWidth(ISheet sheet, int column, bool useMergedCells, int maxRows = 0)
        {
            return GetColumnWidth(sheet, column, useMergedCells, sheet.FirstRowNum, sheet.LastRowNum, maxRows);
        }
        /**
         * Compute width of a column based on a subset of the rows and return the result
         *
         * @param sheet the sheet to calculate
         * @param column    0-based index of the column
         * @param useMergedCells    whether to use merged cells
         * @param firstRow  0-based index of the first row to consider (inclusive)
         * @param lastRow   0-based index of the last row to consider (inclusive)
         * @param maxRows   limit the scope to maxRows rows to speed up the function, or leave 0 (optional)
         * @return  the width in pixels or -1 if cell is empty
         */
        public static double GetColumnWidth(ISheet sheet, int column, bool useMergedCells, int firstRow, int lastRow, int maxRows = 0)
        {
            DataFormatter formatter = new DataFormatter();
            int defaultCharWidth = GetDefaultCharWidth(sheet.Workbook);

            // No need to explore the whole sheet: explore only the first maxRows lines
            if (maxRows > 0 && lastRow - firstRow > maxRows)
                lastRow = firstRow + maxRows;

            double width = -1;
            for (int rowIdx = firstRow; rowIdx <= lastRow; ++rowIdx)
            {
                IRow row = sheet.GetRow(rowIdx);
                if (row != null)
                {
                    double cellWidth = GetColumnWidthForRow(row, column, defaultCharWidth, formatter, useMergedCells);
                    width = Math.Max(width, cellWidth);
                }
            }
            return width;
        }

        /**
         * Get default character width
         *
         * @param wb the workbook to get the default character width from
         * @return default character width
         */
        public static int GetDefaultCharWidth(IWorkbook wb)
        {
            IFont defaultFont = wb.GetFontAt((short)0);
            Font sixLaborsFont = IFont2Font(defaultFont);
            var cacheTextOptions = GetTextOptions(sixLaborsFont);
            return (int)Math.Ceiling(GetDefaultCharWidthCache(cacheTextOptions.Font));
        }

        /// <summary>
        /// Gets the width of a standard character ('0') using the specific font and style of the given cell.
        /// This provides a font-specific benchmark for layout calculations like column width or text wrapping.
        /// </summary>
        public static int GetCellFontCharWidth(ICell cell)
        {
            // 1. Guard clause for null cells.
            if (cell == null)
            {
                return 0;
            }

            // 2. Get the workbook and the cell's specific style.
            IWorkbook workbook = cell.Sheet.Workbook;
            ICellStyle style = cell.CellStyle;

            // 3. Get the IFont object from the style's font index.
            // Every style points to a font in the workbook's font table.
            IFont npoiFont = workbook.GetFontAt(style.FontIndex);

            // 4. Convert the NPOI IFont to a SixLabors.Fonts.Font object
            // using the existing helper method.
            Font sixLaborsFont = IFont2Font(npoiFont);

            // 5. Measure the width of the single 'defaultChar' ('0') which is defined
            // at the class level. We use MeasureAdvance as it's efficient for single-line width.
            var textOptions = GetTextOptions(sixLaborsFont);
            var sizeWidth = GetDefaultCharWidthCache(textOptions.Font);

            // 6. Return the calculated width.
            return (int)Math.Ceiling(sizeWidth);
        }

        /**
         * Compute width of a single cell in a row
         * Convenience method for {@link getCellWidth}
         *
         * @param row the row that contains the cell of interest
         * @param column the column number of the cell whose width is to be calculated
         * @param defaultCharWidth the width of a single character
         * @param formatter formatter used to prepare the text to be measured
         * @param useMergedCells    whether to use merged cells
         * @return  the width in pixels or -1 if cell is empty
         */

        private static double GetColumnWidthForRow(
                IRow row, int column, int defaultCharWidth, DataFormatter formatter, bool useMergedCells)
        {
            if (row == null)
            {
                return -1;
            }

            ICell cell = row.GetCell(column);

            if (cell == null)
            {
                return -1;
            }

            return GetCellWidth(cell, defaultCharWidth, formatter, useMergedCells);
        }

        /**
         * Check if the Fonts are installed correctly so that Java can compute the size of
         * columns. 
         * 
         * If a Cell uses a Font which is not available on the operating system then Java may 
         * fail to return useful Font metrics and thus lead to an auto-computed size of 0.
         * 
         *  This method allows to check if computing the sizes for a given Font will succeed or not.
         *
         * @param font The Font that is used in the Cell
         * @return true if computing the size for this Font will succeed, false otherwise
         */
        public static bool CanComputeColumnWidth(IFont font)
        {
            //AttributedString str = new AttributedString("1w");
            //copyAttributes(font, str, 0, "1w".length());

            //TextLayout layout = new TextLayout(str.getIterator(), fontRenderContext);
            //if (layout.getBounds().getWidth() > 0)
            //{
            //    return true;
            //}

            return true;
        }
        // /**
        // * Copy text attributes from the supplied Font to Java2D AttributedString
        // */
        //private static void copyAttributes(IFont font, AttributedString str, int startIdx, int endIdx)
        //{
        //    str.AddAttribute(TextAttribute.FAMILY, font.FontName, startIdx, endIdx);
        //    str.AddAttribute(TextAttribute.SIZE, (float)font.FontHeightInPoints);
        //    if (font.Boldweight == (short)FontBoldWeight.BOLD) str.AddAttribute(TextAttribute.WEIGHT, TextAttribute.WEIGHT_BOLD, startIdx, endIdx);
        //    if (font.IsItalic) str.AddAttribute(TextAttribute.POSTURE, TextAttribute.POSTURE_OBLIQUE, startIdx, endIdx);
        //    TODO-Fonts: not supported: if (font.Underline == (byte)FontUnderlineType.SINGLE) str.AddAttribute(TextAttribute.UNDERLINE, TextAttribute.UNDERLINE_ON, startIdx, endIdx);
        //}

        private readonly struct FontCacheKey : IEquatable<FontCacheKey>
        {
            public FontCacheKey(string fontName, float fontHeightInPoints, FontStyle style)
            {
                FontName = fontName;
                FontHeightInPoints = fontHeightInPoints;
                Style = style;
            }

            public readonly string FontName;
            public readonly float FontHeightInPoints;
            public readonly FontStyle Style;

            public bool Equals(FontCacheKey other)
            {
                return FontName == other.FontName && FontHeightInPoints.Equals(other.FontHeightInPoints) && Style == other.Style;
            }

            public override bool Equals(object obj)
            {
                return obj is FontCacheKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = FontName != null ? FontName.GetHashCode() : 0;
                    hashCode = (hashCode * 397) ^ FontHeightInPoints.GetHashCode();
                    hashCode = (hashCode * 397) ^ (int)Style;
                    return hashCode;
                }
            }
        }

        private static readonly CultureInfo StartupCulture = CultureInfo.CurrentCulture;
        private static readonly ConcurrentDictionary<FontCacheKey, Font> FontCache = new();

        /// <summary>
        /// Convert HSSFFont to Font.
        /// </summary>
        /// <param name="font1">The font.</param>
        /// <returns></returns>
        /// <exception cref="FontException">Will throw this if no font are 
        /// found by SixLabors in the current environment.</exception>
        public static Font IFont2Font(IFont font1)
        {
            FontStyle style = FontStyle.Regular;
            if (font1.IsBold)
            {
                style |= FontStyle.Bold;
            }
            if (font1.IsItalic)
                style |= FontStyle.Italic;

            /* TODO-Fonts: not supported
            if (font1.Underline == FontUnderlineType.Single)
            {
                style |= FontStyle.Underline;
            }
            */

            var key = new FontCacheKey(font1.FontName, (float)font1.FontHeightInPoints, style);

            // only use cache if font size is an integer and culture is original to prevent cache size explosion
            if (font1.FontHeightInPoints == (int)font1.FontHeightInPoints && CultureInfo.CurrentCulture.Equals(StartupCulture))
            {
                return FontCache.GetOrAdd(key, IFont2FontImpl);
            }

            // skip cache
            return IFont2FontImpl(key);
        }

        private static Font IFont2FontImpl(FontCacheKey cacheKey)
        {
            // Try to find font in system fonts. If we can not find out,
            // use "Arial". TODO-Fonts: More fallbacks.

            if (!SystemFonts.TryGet(cacheKey.FontName, CultureInfo.CurrentCulture, out var fontFamily))
            {
                if (!SystemFonts.TryGet("Arial", CultureInfo.CurrentCulture, out fontFamily))
                {
                    if (!SystemFonts.Families.Any())
                    {
                        throw new FontException("No fonts found installed on the machine.");
                    }

                    fontFamily = SystemFonts.Families.First();
                }
            }

            return new Font(fontFamily, cacheKey.FontHeightInPoints, cacheKey.Style);
        }

        private static readonly ConcurrentDictionary<FontCacheKey, float> _lineHeights = new();
        private static readonly ConcurrentDictionary<FontCacheKey, float> _spaceWidths = new();
        private static readonly ConcurrentDictionary<FontCacheKey, float> _defaultCharWdths = new();
        private static readonly ConcurrentDictionary<FontCacheKey, TextOptions> _optsCache = new();
        private static readonly ConcurrentDictionary<(ISheet, int, int, int, int), double> _mergedWidthCache = new(); // memoize total pixel width of merged region once

        private static FontStyle GetStyle(Font font)
        {
            var style = FontStyle.Regular;
            if (font.IsBold)
                style |= FontStyle.Bold;
            if (font.IsItalic)
                style |= FontStyle.Italic;
            return style;
        }
        private static FontCacheKey KeyFrom(Font font)
            => new FontCacheKey(font.Family.Name, font.Size, GetStyle(font));

        private static float GetLineHeight(Font font)
        {
            var key = KeyFrom(font);
            return _lineHeights.GetOrAdd(key,
                _ => TextMeasurer.MeasureAdvance("Hg", new TextOptions(font) { Dpi = dpi }).Height);
        }

        private static float GetSpaceWidth(Font font)
        {
            var key = KeyFrom(font);
            return _spaceWidths.GetOrAdd(key,
                _ => TextMeasurer.MeasureSize(" ", new TextOptions(font) { Dpi = dpi }).Width);
        }

        private static TextOptions GetTextOptions(Font font)
        {
            var key = KeyFrom(font);
            return _optsCache.GetOrAdd(key, _ => new TextOptions(font) { Dpi = dpi });
        }

        private static float GetDefaultCharWidthCache(Font font)
        {
            var key = KeyFrom(font);
            return _defaultCharWdths.GetOrAdd(key,
                _ => TextMeasurer.MeasureSize(defaultChar.ToString(), new TextOptions(font) { Dpi = dpi }).Width);
        }

        private static double GetMergedPixelWidth(ISheet sheet, int firstRow, int firstColumn, int lastRow, int lastColumn, ICell refCell)
        {
            var key = (sheet, firstRow, firstColumn, lastRow, lastColumn);
            if (_mergedWidthCache.TryGetValue(key, out var w))
                return w;
            double sum = 0;
            for (int col = firstColumn; col <= lastColumn; col++)
                sum += GetColumnWidthInPixels(sheet, col, refCell);
            _mergedWidthCache[key] = sum;
            return sum;
        }

        /// <summary>
        /// Check if the cell is in the specified cell range
        /// </summary>
        /// <param name="cr">the cell range to check in</param>
        /// <param name="rowIx">the row to check</param>
        /// <param name="colIx">the column to check</param>
        /// <returns>return true if the range contains the cell [rowIx, colIx]</returns>
        [Obsolete("deprecated 3.15 beta 2. Use {@link CellRangeAddressBase#isInRange(int, int)}.")]
        public static bool ContainsCell(CellRangeAddress cr, int rowIx, int colIx)
        {
            return cr.IsInRange(rowIx, colIx);
        }

        /**
         * Generate a valid sheet name based on the existing one. Used when cloning sheets.
         *
         * @param srcName the original sheet name to
         * @return clone sheet name
         */
        public static String GetUniqueSheetName(IWorkbook wb, String srcName)
        {
            if (wb.GetSheetIndex(srcName) == -1)
            {
                return srcName;
            }
            int uniqueIndex = 2;
            String baseName = srcName;
            int bracketPos = srcName.LastIndexOf('(');
            if (bracketPos > 0 && srcName.EndsWith(")"))
            {
                String suffix = srcName.Substring(bracketPos + 1, srcName.Length - bracketPos - 2);
                try
                {
                    uniqueIndex = Int32.Parse(suffix.Trim());
                    uniqueIndex++;
                    baseName = srcName.Substring(0, bracketPos).Trim();
                }
                catch (FormatException)
                {
                    // contents of brackets not numeric
                }
            }
            while (true)
            {
                // Try and find the next sheet name that is unique
                String index = (uniqueIndex++).ToString();
                String name;
                if (baseName.Length + index.Length + 2 < 31)
                {
                    name = baseName + " (" + index + ")";
                }
                else
                {
                    name = baseName.Substring(0, 31 - index.Length - 2) + "(" + index + ")";
                }

                //If the sheet name is unique, then Set it otherwise Move on to the next number.
                if (wb.GetSheetIndex(name) == -1)
                {
                    return name;
                }
            }
        }

        /**
         * Return the cell, taking account of merged regions. Allows you to find the
         *  cell who's contents are Shown in a given position in the sheet.
         * 
         * <p>If the cell at the given co-ordinates is a merged cell, this will
         *  return the primary (top-left) most cell of the merged region.</p>
         * <p>If the cell at the given co-ordinates is not in a merged region,
         *  then will return the cell itself.</p>
         * <p>If there is no cell defined at the given co-ordinates, will return
         *  null.</p>
         */
        public static ICell GetCellWithMerges(ISheet sheet, int rowIx, int colIx)
        {
            IRow r = sheet.GetRow(rowIx);
            if (r != null)
            {
                ICell c = r.GetCell(colIx);
                if (c != null)
                {
                    // Normal, non-merged cell
                    return c;
                }
            }

            for (int mr = 0; mr < sheet.NumMergedRegions; mr++)
            {
                CellRangeAddress mergedRegion = sheet.GetMergedRegion(mr);
                if (mergedRegion.IsInRange(rowIx, colIx))
                {
                    // The cell wanted is in this merged range
                    // Return the primary (top-left) cell for the range
                    r = sheet.GetRow(mergedRegion.FirstRow);
                    if (r != null)
                    {
                        return r.GetCell(mergedRegion.FirstColumn);
                    }
                }
            }

            // If we Get here, then the cell isn't defined, and doesn't
            //  live within any merged regions
            return null;
        }

        public static CellRangeAddress GetMergedRegionForCell(ICell cell, MergeIndex merge)
        {
            return merge.TryGetRegion(cell.RowIndex, cell.ColumnIndex, out var region)
                ? region
                : null;
        }

    }
}
