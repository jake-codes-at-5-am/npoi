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

using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using NPOI.OpenXml4Net.OPC;
using NPOI.XSSF.UserModel;

namespace NPOI.XSSF.Model
{
    /// <summary>
    /// Forward-only, read-only view of a workbook's shared strings table.
    /// Reads sharedStrings.xml once into memory (plain text per &lt;si&gt;),
    /// without building the rich-text DOM. Memory scales with unique-string
    /// count, not row count.
    /// </summary>
    public class ReadOnlySharedStringsTable
    {
        private readonly List<string> _strings = new List<string>();

        public ReadOnlySharedStringsTable(OPCPackage pkg)
        {
            List<PackagePart> parts = pkg.GetPartsByContentType(XSSFRelation.SHARED_STRINGS.ContentType);
            if (parts.Count == 0)
            {
                return;
            }
            using (Stream stream = parts[0].GetInputStream())
            {
                ReadFrom(stream);
            }
        }

        public ReadOnlySharedStringsTable(Stream sharedStringsXml)
        {
            ReadFrom(sharedStringsXml);
        }

        public int Count => _strings.Count;

        public string GetEntryAt(int index) => _strings[index];

        private void ReadFrom(Stream stream)
        {
            XmlReaderSettings settings = new XmlReaderSettings
            {
                IgnoreComments = true,
                IgnoreProcessingInstructions = true,
                DtdProcessing = DtdProcessing.Prohibit,
            };
            using (XmlReader reader = XmlReader.Create(stream, settings))
            {
                StringBuilder current = null;
                // Phonetic (ruby) guides appear as <rPh ...><t>...</t></rPh> runs inside an <si>
                // (or <is>). Their <t> text is pronunciation metadata, NOT part of the base string,
                // so it must be skipped or CJK values like "漢字" get corrupted into "漢字かんじ".
                bool isPhoneticRun = false;
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "si")
                    {
                        if (reader.IsEmptyElement)
                        {
                            // A self-closing <si/> is schema-valid and represents an empty shared
                            // string. XmlReader never raises a separate EndElement for it, so if we
                            // set `current` here we would never see the matching "si end" branch
                            // below to flush it - the next <si> would just overwrite `current`,
                            // silently dropping this entry and shifting every later index by one.
                            // Record it immediately instead, and leave `current` untouched (null).
                            _strings.Add(string.Empty);
                        }
                        else
                        {
                            current = new StringBuilder();
                            isPhoneticRun = false;
                        }
                    }
                    else if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "rPh")
                    {
                        // A self-closing <rPh/> carries no <t>, so there is nothing to skip and no
                        // matching EndElement will arrive - leave the flag alone in that case.
                        if (!reader.IsEmptyElement)
                        {
                            isPhoneticRun = true;
                        }
                    }
                    else if (reader.NodeType == XmlNodeType.EndElement && reader.LocalName == "rPh")
                    {
                        isPhoneticRun = false;
                    }
                    else if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "t"
                        && current != null && !isPhoneticRun)
                    {
                        if (!reader.IsEmptyElement)
                        {
                            // Loop to the matching </t> (same depth) appending every Text,
                            // SignificantWhitespace, AND CDATA node so a <t> that interleaves plain
                            // text with CDATA sections (e.g. abc<![CDATA[def]]>) is preserved whole.
                            // Deliberately not using ReadElementContentAsString(): it advances the
                            // reader onto the node *after* </t>, which the outer while(reader.Read())
                            // would then skip (typically the enclosing </si>), corrupting boundary
                            // tracking. Break ON the </t> instead and let the outer loop consume it
                            // on its next iteration (harmless: matches no branch here).
                            int depth = reader.Depth;
                            while (reader.Read())
                            {
                                if (reader.NodeType == XmlNodeType.EndElement && reader.Depth == depth)
                                {
                                    break;
                                }
                                if (reader.NodeType == XmlNodeType.Text
                                    || reader.NodeType == XmlNodeType.SignificantWhitespace
                                    || reader.NodeType == XmlNodeType.CDATA)
                                {
                                    current.Append(reader.Value);
                                }
                            }
                        }
                    }
                    else if (reader.NodeType == XmlNodeType.EndElement && reader.LocalName == "si" && current != null)
                    {
                        _strings.Add(current.ToString());
                        current = null;
                    }
                }
            }
        }
    }
}
