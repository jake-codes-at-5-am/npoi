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
using System.Collections.Generic;
using System.IO;
using System.Xml;
using NPOI.OpenXml4Net.OPC;
using NPOI.XSSF.Model;
using NPOI.XSSF.UserModel;

namespace NPOI.XSSF.EventUserModel
{
    /// <summary>
    /// Read-side entry point for streaming XLSX parsing. Opens an OPCPackage and
    /// exposes the styles table, shared strings, and per-sheet XML part streams
    /// without constructing an XSSFWorkbook DOM.
    /// </summary>
    public class XSSFReader
    {
        public sealed class SheetRef
        {
            public string Name { get; internal set; }
            public int Index { get; internal set; }
            internal PackagePart Part { get; set; }
        }

        private readonly OPCPackage _pkg;
        private readonly PackagePart _workbookPart;
        private readonly List<SheetRef> _sheets = new List<SheetRef>();
        private StylesTable _styles;
        private ReadOnlySharedStringsTable _sst;

        public XSSFReader(OPCPackage pkg)
        {
            _pkg = pkg ?? throw new ArgumentNullException(nameof(pkg));
            List<PackagePart> wbParts = pkg.GetPartsByContentType(XSSFRelation.WORKBOOK.ContentType);
            if (wbParts.Count == 0)
            {
                // Some producers use the macro/template content type.
                wbParts = pkg.GetPartsByContentType(XSSFRelation.MACROS_WORKBOOK.ContentType);
            }
            if (wbParts.Count == 0)
            {
                throw new ArgumentException("Package does not contain a workbook part (is this a valid .xlsx?).");
            }
            _workbookPart = wbParts[0];
            LoadSheetRefs();
        }

        public StylesTable GetStylesTable()
        {
            if (_styles == null)
            {
                List<PackagePart> parts = _pkg.GetPartsByContentType(XSSFRelation.STYLES.ContentType);
                _styles = parts.Count > 0 ? new StylesTable(parts[0]) : new StylesTable();
            }
            return _styles;
        }

        public ReadOnlySharedStringsTable GetSharedStringsTable()
        {
            return _sst ?? (_sst = new ReadOnlySharedStringsTable(_pkg));
        }

        public IReadOnlyList<SheetRef> GetSheets() => _sheets;

        public Stream GetSheetStream(int index)
        {
            if (index < 0 || index >= _sheets.Count)
            {
                throw new ArgumentException($"Sheet index {index} is out of range (0..{_sheets.Count - 1}).");
            }
            return _sheets[index].Part.GetInputStream();
        }

        public Stream GetSheetStream(string name)
        {
            foreach (SheetRef s in _sheets)
            {
                if (string.Equals(s.Name, name, StringComparison.Ordinal))
                {
                    return s.Part.GetInputStream();
                }
            }
            throw new ArgumentException($"Sheet '{name}' was not found.");
        }

        private void LoadSheetRefs()
        {
            const string relNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
            using (Stream wb = _workbookPart.GetInputStream())
            using (XmlReader reader = XmlReader.Create(wb, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit }))
            {
                int index = 0;
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "sheet")
                    {
                        string name = reader.GetAttribute("name");
                        string rid = reader.GetAttribute("id", relNs);
                        PackageRelationship rel = _workbookPart.GetRelationship(rid);
                        PackagePart part = _workbookPart.GetRelatedPart(rel);
                        _sheets.Add(new SheetRef
                        {
                            Name = name,
                            Index = index++,
                            Part = part,
                        });
                    }
                }
            }
        }
    }
}
