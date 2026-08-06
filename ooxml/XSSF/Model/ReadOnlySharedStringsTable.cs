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
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "si")
                    {
                        current = new StringBuilder();
                    }
                    else if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "t" && current != null)
                    {
                        if (!reader.IsEmptyElement)
                        {
                            // Deliberately not using ReadElementContentAsString() here: it leaves the
                            // reader positioned on the node *after* </t>, and the outer while(reader.Read())
                            // would then skip that node (typically the enclosing </si>), corrupting the
                            // si-boundary tracking below. Advance onto the text node manually instead and
                            // let the outer loop consume </t> on its next iteration (harmless, matches
                            // neither the "si" nor "t"-start branch).
                            reader.Read();
                            if (reader.NodeType == XmlNodeType.Text || reader.NodeType == XmlNodeType.SignificantWhitespace)
                            {
                                current.Append(reader.Value);
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
