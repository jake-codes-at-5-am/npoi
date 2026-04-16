using System;
using System.Collections.Generic;
using System.ComponentModel;

using System.Text;
using System.Xml.Serialization;
using System.Xml;
using NPOI.OpenXml4Net.Util;
using System.IO;
using NPOI.SS.Util;

namespace NPOI.OpenXmlFormats.Spreadsheet
{
    [Serializable]
    [XmlType(Namespace = "http://schemas.openxmlformats.org/spreadsheetml/2006/main")]
    public class CT_Row
    {

        private List<CT_Cell> cField = null; // optional element

        private CT_ExtensionList extLstField = null; // optional element

        // the following are all optional attributes
        private uint rField;

        private string spansField = null; // a region is contained in this field, e.g. "1:3"

        private uint sField;

        // Memory optimization: pack 7 boolean fields into a single byte using flags.
        // Each individual bool field consumes 1 byte + alignment padding = 4 bytes
        // in practice on 64-bit. 7 bools = 28 bytes. A single flags byte = 1 byte.
        // Savings: ~24 bytes per row × 10K rows = ~240KB.
        [Flags]
        private enum RowFlags : byte
        {
            None = 0,
            CustomFormat = 1,
            Hidden = 2,
            CustomHeight = 4,
            Collapsed = 8,
            ThickTop = 16,
            ThickBot = 32,
            Ph = 64
        }
        private RowFlags _flags;

        private double htField = -1;

        private byte outlineLevelField;

        private double dyDescentField; //x14ac:dyDescent

        private int lastCellField = -1;

        public static CT_Row Parse(XmlNode node, XmlNamespaceManager namespaceManager)
        {
            if (node == null)
                return null;
            CT_Row ctObj = new CT_Row();
            ctObj.r = XmlHelper.ReadUInt(node.Attributes["r"]);
            ctObj.spans = XmlHelper.ReadString(node.Attributes["spans"]);
            ctObj.s = XmlHelper.ReadUInt(node.Attributes["s"]);
            ctObj.customFormat = XmlHelper.ReadBool(node.Attributes["customFormat"]);
            ctObj.dyDescentField = XmlHelper.ReadDouble(node.Attributes["x14ac:dyDescent"]);
            if (node.Attributes["ht"] != null)
                ctObj.ht = XmlHelper.ReadDouble(node.Attributes["ht"]);
            ctObj.hidden = XmlHelper.ReadBool(node.Attributes["hidden"]);
            ctObj.outlineLevel = XmlHelper.ReadByte(node.Attributes["outlineLevel"]);
            ctObj.customHeight = XmlHelper.ReadBool(node.Attributes["customHeight"]);
            ctObj.collapsed = XmlHelper.ReadBool(node.Attributes["collapsed"]);
            ctObj.thickTop = XmlHelper.ReadBool(node.Attributes["thickTop"]);
            ctObj.thickBot = XmlHelper.ReadBool(node.Attributes["thickBot"]);
            ctObj.ph = XmlHelper.ReadBool(node.Attributes["ph"]);
            ctObj.c = new List<CT_Cell>(node.ChildNodes.Count);
            foreach (XmlNode childNode in node.ChildNodes)
            {
                if (childNode.LocalName == "extLst")
                {
                    ctObj.extLst = CT_ExtensionList.Parse(childNode, namespaceManager);
                }
                else if (childNode.LocalName == "c")
                {
                    CT_Cell cell = CT_Cell.Parse(childNode, namespaceManager);
                    ctObj.c.Add(cell);

                    if (cell.r == null)
                    {
                        continue;
                    }

                    var cellNum = new CellReference(cell.r).Col;

                    if (cellNum > ctObj.lastCellField)
                    {
                        ctObj.lastCellField = cellNum;
                    }
                }
            }
            return ctObj;
        }

        public bool IsSetR()
        {
            return this.rField > 0;
        }

        internal void Write(StreamWriter sw, string nodeName)
        {
            sw.Write("<");
            sw.Write(nodeName);
            XmlHelper.WriteAttribute(sw, "r", this.r);
            XmlHelper.WriteAttribute(sw, "spans", this.spans);
            XmlHelper.WriteAttribute(sw, "s", this.s);
            XmlHelper.WriteAttribute(sw, "customFormat", this.customFormat, false);
            if (this.ht >= 0)
                XmlHelper.WriteAttribute(sw, "ht", this.ht);
            XmlHelper.WriteAttribute(sw, "hidden", this.hidden, false);
            XmlHelper.WriteAttribute(sw, "customHeight", this.customHeight, false);
            XmlHelper.WriteAttribute(sw, "outlineLevel", this.outlineLevel);
            XmlHelper.WriteAttribute(sw, "collapsed", this.collapsed, false);
            XmlHelper.WriteAttribute(sw, "thickTop", this.thickTop, false);
            XmlHelper.WriteAttribute(sw, "thickBot", this.thickBot, false);
            XmlHelper.WriteAttribute(sw, "ph", this.ph, false);
            XmlHelper.WriteAttribute(sw, "x14ac:dyDescent", this.dyDescentField, false);
            sw.Write(">");
            if (this.extLst != null)
                this.extLst.Write(sw, "extLst");
            if (this.c != null)
            {
                foreach (CT_Cell x in this.c)
                {
                    x.Write(sw, "c");
                }
            }
            sw.WriteEndElement(nodeName);
        }



        public void Set(CT_Row row)
        {
            cField = row.cField;
            extLstField = row.extLstField;
            rField = row.rField;
            spansField = row.spansField;
            sField = row.sField;
            _flags = row._flags;
            htField = row.htField;
            outlineLevelField = row.outlineLevelField;
        }
        public CT_Cell AddNewC()
        {
            if (null == cField)
            { cField = new List<CT_Cell>(); }
            CT_Cell cell = new CT_Cell();
            this.cField.Add(cell);
            return cell;
        }
        public void UnsetCollapsed()
        {
            _flags &= ~RowFlags.Collapsed;
        }
        public void UnsetS()
        {
            this.sField = 0;
        }
        public void UnsetCustomFormat()
        {
            _flags &= ~RowFlags.CustomFormat;
        }
        public bool IsSetHidden()
        {
            return (_flags & RowFlags.Hidden) != 0;
        }
        public bool IsSetCollapsed()
        {
            return (_flags & RowFlags.Collapsed) != 0;
        }
        public bool IsSetHt()
        {
            return this.htField >= 0;
        }
        public void UnsetHt()
        {
            this.htField = -1;
        }
        public bool IsSetCustomHeight()
        {
            return (_flags & RowFlags.CustomHeight) != 0;
        }
        public void UnsetCustomHeight()
        {
            _flags &= ~RowFlags.CustomHeight;
        }
        public bool IsSetS()
        {
            return this.sField != 0;
        }
        public void UnsetHidden()
        {
            _flags &= ~RowFlags.Hidden;
        }

        public int SizeOfCArray()
        {
            return (null == cField) ? 0 : cField.Count;
        }
        public CT_Cell GetCArray(int index)
        {
            return (null == cField) ? null : cField[index];
        }
        public void SetCArray(CT_Cell[] array)
        {
            cField = new List<CT_Cell>(array);
        }
        [XmlElement("c")]
        public List<CT_Cell> c
        {
            get
            {
                return this.cField;
            }
            set
            {
                this.cField = value;
            }
        }
        [XmlElement("extLst")]
        public CT_ExtensionList extLst
        {
            get
            {
                return this.extLstField;
            }
            set
            {
                this.extLstField = value;
            }
        }

        [XmlAttribute("r")]
        public uint r
        {
            get
            {
                return this.rField;
            }
            set
            {
                this.rField = value;
            }
        }

        [XmlAttribute]
        public string spans
        {
            get
            {
                return this.spansField;
            }
            set
            {
                this.spansField = value;
            }
        }

        //[DefaultValue(typeof(uint), "0")]
        [XmlAttribute]
        public uint s
        {
            get
            {
                return this.sField;
            }
            set
            {
                this.sField = value;
            }
        }

        //[DefaultValue(false)]
        [XmlAttribute]
        public bool customFormat
        {
            get
            {
                return (_flags & RowFlags.CustomFormat) != 0;
            }
            set
            {
                if (value)
                    _flags |= RowFlags.CustomFormat;
                else
                    _flags &= ~RowFlags.CustomFormat;
            }
        }
        [XmlAttribute]
        public double ht
        {
            get
            {
                return this.htField;
            }
            set
            {
                this.htField = value;
            }
        }


        //[DefaultValue(false)]
        [XmlAttribute]
        public bool hidden
        {
            get
            {
                return (_flags & RowFlags.Hidden) != 0;
            }
            set
            {
                if (value)
                    _flags |= RowFlags.Hidden;
                else
                    _flags &= ~RowFlags.Hidden;
            }
        }

        //[DefaultValue(false)]
        [XmlAttribute]
        public bool customHeight
        {
            get
            {
                return (_flags & RowFlags.CustomHeight) != 0;
            }
            set
            {
                if (value)
                    _flags |= RowFlags.CustomHeight;
                else
                    _flags &= ~RowFlags.CustomHeight;
            }
        }

        [DefaultValue(typeof(byte), "0")]
        [XmlAttribute]
        public byte outlineLevel
        {
            get
            {
                return this.outlineLevelField;
            }
            set
            {
                this.outlineLevelField = value;
            }
        }

        //[DefaultValue(false)]
        [XmlAttribute]
        public bool collapsed
        {
            get
            {
                return (_flags & RowFlags.Collapsed) != 0;
            }
            set
            {
                if (value)
                    _flags |= RowFlags.Collapsed;
                else
                    _flags &= ~RowFlags.Collapsed;
            }
        }

        [DefaultValue(false)]
        [XmlAttribute]
        public bool thickTop
        {
            get
            {
                return (_flags & RowFlags.ThickTop) != 0;
            }
            set
            {
                if (value)
                    _flags |= RowFlags.ThickTop;
                else
                    _flags &= ~RowFlags.ThickTop;
            }
        }

        [DefaultValue(false)]
        [XmlAttribute]
        public bool thickBot
        {
            get
            {
                return (_flags & RowFlags.ThickBot) != 0;
            }
            set
            {
                if (value)
                    _flags |= RowFlags.ThickBot;
                else
                    _flags &= ~RowFlags.ThickBot;
            }
        }

        [DefaultValue(false)]
        [XmlAttribute]
        public bool ph
        {
            get
            {
                return (_flags & RowFlags.Ph) != 0;
            }
            set
            {
                if (value)
                    _flags |= RowFlags.Ph;
                else
                    _flags &= ~RowFlags.Ph;
            }
        }

        /// <summary>
        /// An index of the last non-empty cell in the row
        /// </summary>
        public int lastCell
        {
            get
            {
                return this.lastCellField;
            }
        }
    }

}
