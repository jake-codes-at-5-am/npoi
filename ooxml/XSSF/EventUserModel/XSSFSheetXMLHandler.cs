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

using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;
using NPOI.SS.UserModel;
using NPOI.SS.Util;
using NPOI.XSSF.Model;
using NPOI.XSSF.UserModel;

namespace NPOI.XSSF.EventUserModel
{
    /// <summary>
    /// Incrementally parses a single worksheet's XML, emitting ISheetContentsHandler
    /// callbacks one row at a time. Faithful in shape to Apache POI's
    /// XSSFSheetXMLHandler, driven by a pull XmlReader so an IEnumerable consumer
    /// can advance row-by-row.
    /// </summary>
    public class XSSFSheetXMLHandler : IDisposable
    {
        private readonly XmlReader _reader;
        private readonly StylesTable _styles;
        private readonly ReadOnlySharedStringsTable _strings;
        private readonly ISheetContentsHandler _handler;
        private readonly DataFormatter _formatter = new DataFormatter();
        private readonly bool _use1904Windowing;

        // Both the row's `r` and a cell's `r` are OPTIONAL in the schema; when absent, position
        // is implied by document order. These counters track the next implicit index. An explicit
        // `r` re-syncs the counter so a later bare element continues from the right place.
        private int _nextRow;
        private int _nextCol;

        public XSSFSheetXMLHandler(Stream sheetStream, StylesTable styles,
            ReadOnlySharedStringsTable strings, ISheetContentsHandler handler,
            bool use1904Windowing = false)
        {
            _styles = styles;
            _strings = strings;
            _handler = handler;
            _use1904Windowing = use1904Windowing;
            // CloseInput = true hands stream ownership to the XmlReader: disposing this handler
            // (which disposes _reader) then closes the wrapped sheet Stream, so the caller does
            // not have to. The ctor already takes ownership of the XmlReader, so owning the
            // underlying stream too is the less-surprising contract.
            _reader = XmlReader.Create(sheetStream,
                new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, CloseInput = true });
        }

        /// <summary>Parse forward until one full &lt;row&gt; is emitted. False at end of sheet.</summary>
        public bool ParseNextRow()
        {
            while (_reader.Read())
            {
                if (_reader.NodeType == XmlNodeType.Element && _reader.LocalName == "row")
                {
                    // `r` is optional; when absent the row's index is implied by document order.
                    // An explicit `r` re-syncs the running counter so later bare rows follow on.
                    string r = _reader.GetAttribute("r");
                    int rowNum = r != null ? ParseInt(r, 1) - 1 : _nextRow;
                    _nextRow = rowNum + 1;
                    bool empty = _reader.IsEmptyElement;
                    _handler.StartRow(rowNum);
                    if (!empty)
                    {
                        ReadRowCells(rowNum);
                    }
                    _handler.EndRow(rowNum);
                    return true;
                }
            }
            return false;
        }

        private void ReadRowCells(int rowNum)
        {
            int depth = _reader.Depth;
            _nextCol = 0;
            while (_reader.Read())
            {
                if (_reader.NodeType == XmlNodeType.EndElement && _reader.LocalName == "row" && _reader.Depth == depth)
                {
                    return;
                }
                if (_reader.NodeType == XmlNodeType.Element && _reader.LocalName == "c")
                {
                    ReadCell(rowNum);
                }
            }
        }

        private void ReadCell(int rowNum)
        {
            // `r` is optional on <c>; when absent the column is implied by document order. When
            // present, still advance the counter from it so a later bare cell follows on correctly.
            string r = _reader.GetAttribute("r");
            int col;
            string cellRef;
            if (!string.IsNullOrEmpty(r))
            {
                col = ColumnFromRef(r);
                cellRef = r;
            }
            else
            {
                col = _nextCol;
                cellRef = CellReference.ConvertNumToColString(col)
                    + (rowNum + 1).ToString(CultureInfo.InvariantCulture);
            }
            _nextCol = col + 1;

            string type = _reader.GetAttribute("t");
            string styleIdx = _reader.GetAttribute("s");
            bool cellIsEmpty = _reader.IsEmptyElement;

            string vText = null;
            string inlineText = null;
            bool isFormula = false;

            if (!cellIsEmpty)
            {
                int depth = _reader.Depth;
                while (_reader.Read())
                {
                    if (_reader.NodeType == XmlNodeType.EndElement && _reader.LocalName == "c" && _reader.Depth == depth)
                    {
                        break;
                    }
                    if (_reader.NodeType == XmlNodeType.Element && _reader.LocalName == "v")
                    {
                        vText = ReadElementText();
                    }
                    else if (_reader.NodeType == XmlNodeType.Element && _reader.LocalName == "f")
                    {
                        isFormula = true;
                        // Formula text itself is unused (cached <v> supplies Value/Text), but the
                        // element must still be consumed so the outer loop doesn't miss its end tag.
                        if (!_reader.IsEmptyElement)
                        {
                            ReadElementText();
                        }
                    }
                    else if (_reader.NodeType == XmlNodeType.Element && _reader.LocalName == "is")
                    {
                        inlineText = ReadInlineString();
                    }
                }
            }

            EmitCell(cellRef, type, styleIdx, vText, inlineText, isFormula);
        }

        /// <summary>
        /// Reads the text content of a simple leaf element (e.g. &lt;v&gt;123&lt;/v&gt;) whose
        /// current node is its StartElement. Loops to the matching EndElement (same depth),
        /// appending every Text, SignificantWhitespace, and CDATA node, and breaks ON that
        /// EndElement - leaving the reader positioned on it. The caller's own enclosing
        /// while(_reader.Read()) loop then advances past that EndElement on its next iteration;
        /// it matches neither a start-element branch nor the caller's boundary check, so it is a
        /// harmless no-op there. Deliberately NOT XmlReader.ReadElementContentAsString(): that
        /// over-advances past the EndElement onto the *next* node, which the caller's loop would
        /// then skip (typically the enclosing &lt;/c&gt;), overrunning into the next sibling and
        /// corrupting cell/row boundaries. Mirrors the &lt;t&gt; reader in
        /// ReadOnlySharedStringsTable.ReadFrom, which follows the identical contract.
        /// </summary>
        private string ReadElementText()
        {
            if (_reader.IsEmptyElement)
            {
                return string.Empty;
            }
            int depth = _reader.Depth;
            var sb = new StringBuilder();
            while (_reader.Read())
            {
                if (_reader.NodeType == XmlNodeType.EndElement && _reader.Depth == depth)
                {
                    break;
                }
                if (_reader.NodeType == XmlNodeType.Text
                    || _reader.NodeType == XmlNodeType.SignificantWhitespace
                    || _reader.NodeType == XmlNodeType.CDATA)
                {
                    sb.Append(_reader.Value);
                }
            }
            return sb.ToString();
        }

        private string ReadInlineString()
        {
            if (_reader.IsEmptyElement)
            {
                return string.Empty;
            }
            var sb = new StringBuilder();
            int depth = _reader.Depth;
            // Phonetic (ruby) guides appear as <rPh ...><t>...</t></rPh> runs inside <is> and are
            // pronunciation metadata, not part of the cell's base text - skip their <t> bodies.
            bool isPhoneticRun = false;
            while (_reader.Read())
            {
                if (_reader.NodeType == XmlNodeType.EndElement && _reader.LocalName == "is" && _reader.Depth == depth)
                {
                    break;
                }
                if (_reader.NodeType == XmlNodeType.Element && _reader.LocalName == "rPh")
                {
                    // A self-closing <rPh/> carries no <t> and raises no EndElement - leave the flag.
                    if (!_reader.IsEmptyElement)
                    {
                        isPhoneticRun = true;
                    }
                }
                else if (_reader.NodeType == XmlNodeType.EndElement && _reader.LocalName == "rPh")
                {
                    isPhoneticRun = false;
                }
                else if (_reader.NodeType == XmlNodeType.Element && _reader.LocalName == "t" && !isPhoneticRun)
                {
                    sb.Append(ReadElementText());
                }
            }
            return sb.ToString();
        }

        private void EmitCell(string cellRef, string type, string styleIdx, string vText, string inlineText, bool isFormula)
        {
            object raw;
            string text;
            StreamValueKind kind;

            switch (type)
            {
                case "s":
                    if (!string.IsNullOrEmpty(vText))
                    {
                        int sstIndex = ParseSharedStringIndex(vText, cellRef);
                        try
                        {
                            raw = text = _strings.GetEntryAt(sstIndex);
                        }
                        catch (ArgumentOutOfRangeException ex)
                        {
                            throw new FormatException(
                                $"Cell {cellRef} references shared-string index {sstIndex}, which is out of range.", ex);
                        }
                    }
                    else
                    {
                        raw = text = string.Empty;
                    }
                    kind = StreamValueKind.String;
                    break;
                case "inlineStr":
                    raw = text = inlineText ?? string.Empty;
                    kind = StreamValueKind.String;
                    break;
                case "str":
                    // A cached formula-string result. Its VALUE type is String; formula-ness is
                    // reported separately via the isFormula flag computed below.
                    raw = text = vText ?? string.Empty;
                    kind = StreamValueKind.String;
                    break;
                case "b":
                    bool b = vText == "1";
                    raw = b;
                    text = b ? "TRUE" : "FALSE";
                    kind = StreamValueKind.Boolean;
                    break;
                case "e":
                    raw = text = vText ?? string.Empty;
                    kind = StreamValueKind.Error;
                    break;
                case "d":
                    // ISO-8601 date cell: the <v> carries a textual date (e.g. "2026-08-06"), not
                    // a numeric serial, so it is epoch-independent (no 1900/1904 windowing).
                    if (string.IsNullOrEmpty(vText))
                    {
                        raw = null;
                        text = string.Empty;
                        kind = StreamValueKind.Blank;
                    }
                    else
                    {
                        raw = ParseIsoDate(vText, cellRef);
                        text = vText;
                        kind = StreamValueKind.Date;
                    }
                    break;
                default: // "n" or absent
                    if (string.IsNullOrEmpty(vText))
                    {
                        raw = null;
                        text = string.Empty;
                        kind = StreamValueKind.Blank;
                    }
                    else
                    {
                        double d = ParseNumber(vText, cellRef);
                        int numFmtId = 0;
                        string fmtString = null;
                        if (!string.IsNullOrEmpty(styleIdx))
                        {
                            XSSFCellStyle cs = _styles.GetStyleAt(int.Parse(styleIdx, CultureInfo.InvariantCulture));
                            if (cs != null)
                            {
                                numFmtId = cs.DataFormat;
                                fmtString = cs.GetDataFormatString();
                            }
                        }
                        // A numeric cell with no style (or a style whose format string can't be
                        // resolved) uses Excel's implicit "General" format. DataFormatter.GetFormat
                        // calls formatStrIn.IndexOf(';') with no null guard (unlike its ICell
                        // overload), so a null fmtString here would NRE - this is spec-valid and
                        // common: SXSSF and other minimal writers emit unstyled numeric cells like
                        // <c r="A1"><v>123</v></c> with no "s" attribute at all.
                        if (string.IsNullOrEmpty(fmtString))
                        {
                            fmtString = "General";
                        }
                        if (DateUtil.IsADateFormat(numFmtId, fmtString))
                        {
                            raw = DateUtil.GetJavaDate(d, _use1904Windowing);
                            kind = StreamValueKind.Date;
                        }
                        else
                        {
                            raw = d;
                            kind = StreamValueKind.Number;
                        }
                        text = _formatter.FormatRawCellContents(d, numFmtId, fmtString, _use1904Windowing);
                    }
                    break;
            }

            // Formula-ness is orthogonal to the value type: a cell is a formula when it carries an
            // <f> child, OR when it is typed t="str" (a cached formula-string result). The value's
            // own kind (Number/String/Date/... or Blank for an unevaluated "<f>B1*2</f><v/>") is
            // preserved above and reported alongside the flag, so consumers see BOTH.
            bool cellIsFormula = isFormula || type == "str";

            _handler.Cell(cellRef, text, raw, kind, cellIsFormula);
        }

        private static int ParseInt(string s, int fallback)
            => int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v) ? v : fallback;

        // Value-parse helpers that name the offending cell on failure. A malformed value in a
        // third-party file should abort with a message pointing at the exact cell, not an
        // anonymous FormatException/out-of-range from deep in the switch.
        private static double ParseNumber(string vText, string cellRef)
        {
            if (double.TryParse(vText, NumberStyles.Float | NumberStyles.AllowThousands,
                    CultureInfo.InvariantCulture, out double d))
            {
                return d;
            }
            throw new FormatException(
                $"Cell {cellRef} has a numeric value '{vText}' that could not be parsed as a number.");
        }

        private static int ParseSharedStringIndex(string vText, string cellRef)
        {
            if (int.TryParse(vText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int i))
            {
                return i;
            }
            throw new FormatException(
                $"Cell {cellRef} has a shared-string index '{vText}' that could not be parsed as an integer.");
        }

        private static DateTime ParseIsoDate(string vText, string cellRef)
        {
            if (DateTime.TryParse(vText, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind,
                    out DateTime dt))
            {
                return dt;
            }
            throw new FormatException(
                $"Cell {cellRef} has an ISO date value '{vText}' that could not be parsed as a date.");
        }

        /// <summary>Zero-based column index from a cell reference (e.g. "AB12" -> 27).</summary>
        private static int ColumnFromRef(string r)
        {
            int i = 0;
            while (i < r.Length && !char.IsDigit(r[i]))
            {
                i++;
            }
            return CellReference.ConvertColStringToIndex(r.Substring(0, i));
        }

        public void Dispose() => _reader?.Dispose();
    }
}
