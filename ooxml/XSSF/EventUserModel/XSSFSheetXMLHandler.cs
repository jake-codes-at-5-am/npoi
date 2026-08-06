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
using NPOI.XSSF.Model;
using NPOI.XSSF.UserModel;

namespace NPOI.XSSF.EventUserModel
{
    /// <summary>
    /// Incrementally parses a single worksheet's XML, emitting SheetContentsHandler
    /// callbacks one row at a time. Faithful in shape to Apache POI's
    /// XSSFSheetXMLHandler, driven by a pull XmlReader so an IEnumerable consumer
    /// can advance row-by-row.
    /// </summary>
    public class XSSFSheetXMLHandler : IDisposable
    {
        private readonly XmlReader _reader;
        private readonly StylesTable _styles;
        private readonly ReadOnlySharedStringsTable _strings;
        private readonly SheetContentsHandler _handler;
        private readonly DataFormatter _formatter = new DataFormatter();

        public XSSFSheetXMLHandler(Stream sheetStream, StylesTable styles,
            ReadOnlySharedStringsTable strings, SheetContentsHandler handler)
        {
            _styles = styles;
            _strings = strings;
            _handler = handler;
            _reader = XmlReader.Create(sheetStream, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit });
        }

        /// <summary>Parse forward until one full &lt;row&gt; is emitted. False at end of sheet.</summary>
        public bool ParseNextRow()
        {
            while (_reader.Read())
            {
                if (_reader.NodeType == XmlNodeType.Element && _reader.LocalName == "row")
                {
                    int rowNum = ParseInt(_reader.GetAttribute("r"), 1) - 1;
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
            while (_reader.Read())
            {
                if (_reader.NodeType == XmlNodeType.EndElement && _reader.LocalName == "row" && _reader.Depth == depth)
                {
                    return;
                }
                if (_reader.NodeType == XmlNodeType.Element && _reader.LocalName == "c")
                {
                    ReadCell();
                }
            }
        }

        private void ReadCell()
        {
            string cellRef = _reader.GetAttribute("r") ?? string.Empty;
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
        /// current node is its StartElement, WITHOUT using XmlReader.ReadElementContentAsString().
        /// That method advances the reader past the element's EndElement onto the *next* node,
        /// but every caller here sits inside a while(_reader.Read()) loop that scopes its own
        /// boundary by depth/name on EndElement; skipping straight past the EndElement would
        /// make the loop's next Read() consume (and lose) whatever node follows - typically the
        /// enclosing element's own EndElement (e.g. &lt;/c&gt;), which would then overrun into the
        /// next sibling and corrupt cell/row boundaries. Instead: manually step onto the Text
        /// node and read its Value, leaving the EndElement for the caller's loop to consume on
        /// its own next iteration (matches neither the "v"/"f" start branch nor the boundary
        /// check, so it's a harmless no-op there). Mirrors the fix already applied in
        /// ReadOnlySharedStringsTable.ReadFrom for the identical class of bug.
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
                if (_reader.NodeType == XmlNodeType.Text || _reader.NodeType == XmlNodeType.SignificantWhitespace)
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
            while (_reader.Read())
            {
                if (_reader.NodeType == XmlNodeType.EndElement && _reader.LocalName == "is" && _reader.Depth == depth)
                {
                    break;
                }
                if (_reader.NodeType == XmlNodeType.Element && _reader.LocalName == "t")
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
                    raw = text = !string.IsNullOrEmpty(vText)
                        ? _strings.GetEntryAt(int.Parse(vText, CultureInfo.InvariantCulture))
                        : string.Empty;
                    kind = StreamValueKind.String;
                    break;
                case "inlineStr":
                    raw = text = inlineText ?? string.Empty;
                    kind = StreamValueKind.String;
                    break;
                case "str":
                    raw = text = vText ?? string.Empty;
                    kind = StreamValueKind.Formula;
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
                default: // "n" or absent
                    if (string.IsNullOrEmpty(vText))
                    {
                        raw = null;
                        text = string.Empty;
                        kind = StreamValueKind.Blank;
                    }
                    else
                    {
                        double d = double.Parse(vText, CultureInfo.InvariantCulture);
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
                        if (DateUtil.IsADateFormat(numFmtId, fmtString))
                        {
                            raw = DateUtil.GetJavaDate(d);
                            kind = StreamValueKind.Date;
                        }
                        else
                        {
                            raw = d;
                            kind = StreamValueKind.Number;
                        }
                        text = _formatter.FormatRawCellContents(d, numFmtId, fmtString);
                    }
                    break;
            }

            // Presence of <f> marks the cell as a formula regardless of the resolved value's
            // own type - including when the cached <v> is a self-closing/empty element (NPOI
            // itself writes unevaluated formulas as e.g. "<f>B1*2</f><v/>", which would otherwise
            // resolve to Blank above and hide the fact that this is a formula cell).
            if (isFormula && kind != StreamValueKind.Formula)
            {
                kind = StreamValueKind.Formula;
            }

            _handler.Cell(cellRef, text, raw, kind);
        }

        private static int ParseInt(string s, int fallback)
            => int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v) ? v : fallback;

        public void Dispose() => _reader?.Dispose();
    }
}
