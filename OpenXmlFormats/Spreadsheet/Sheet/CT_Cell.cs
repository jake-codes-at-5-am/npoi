using System;
using System.Collections.Generic;
using System.ComponentModel;

using System.Text;
using System.Xml.Serialization;
using System.Xml;
using NPOI.OpenXml4Net.Util;
using System.IO;

namespace NPOI.OpenXmlFormats.Spreadsheet
{
    [Serializable]
    [XmlType(Namespace = "http://schemas.openxmlformats.org/spreadsheetml/2006/main")]
    public class CT_Cell
    {

        private CT_CellFormula fField = null;

        private string vField = null;

        private CT_Rst isField = null;

        private CT_ExtensionList extLstField = null;

        private string rField = null;

        // Memory optimization: replaced nullable fields (uint?, ST_CellType?, bool?)
        // with plain value-type fields. Each Nullable<T> costs 8-12 bytes due to the
        // hasValue flag + alignment padding. For 150K cells, this saves ~5 MB.
        //
        // Behavioral equivalence:
        //   - IsSetS() checked (sField != null && sField != 0)  →  now checks (sField != 0)
        //     Both are equivalent: unsetS() sets to 0, Parse sets 0 when attribute absent.
        //   - IsSetT() checked (tField != ST_CellType.n)  →  unchanged logic
        //   - The s/cm/vm/ph property getters returned default when null; now return field directly.
        //   - Write() uses value-based checks (== 0, == false), not null checks — unchanged.
        private uint sField;

        private ST_CellType tField = ST_CellType.n;

        private uint cmField;

        private uint vmField;

        private bool phField;

        public static CT_Cell Parse(XmlNode node, XmlNamespaceManager namespaceManager)
        {
            if (node == null)
                return null;
            CT_Cell ctObj = new CT_Cell();
            ctObj.r = XmlHelper.ReadString(node.Attributes["r"]);
            ctObj.s = XmlHelper.ReadUInt(node.Attributes["s"]);
            if (node.Attributes["t"] != null)
                ctObj.t = (ST_CellType)Enum.Parse(typeof(ST_CellType), node.Attributes["t"].Value);
            ctObj.cm = XmlHelper.ReadUInt(node.Attributes["cm"]);
            ctObj.vm = XmlHelper.ReadUInt(node.Attributes["vm"]);
            ctObj.ph = XmlHelper.ReadBool(node.Attributes["ph"]);
            foreach (XmlNode childNode in node.ChildNodes)
            {
                if (childNode.LocalName == "f")
                    ctObj.f = CT_CellFormula.Parse(childNode, namespaceManager);
                else if (childNode.LocalName == "v")
                    ctObj.v = childNode.InnerText;
                else if (childNode.LocalName == "is")
                    ctObj.@is = CT_Rst.Parse(childNode, namespaceManager);
                else if (childNode.LocalName == "extLst")
                    ctObj.extLst = CT_ExtensionList.Parse(childNode, namespaceManager);
            }
            return ctObj;
        }



        internal void Write(StreamWriter sw, string nodeName)
        {
            sw.Write("<");
            sw.Write(nodeName);

            XmlHelper.WriteAttribute(sw, "r", this.r);
            XmlHelper.WriteAttribute(sw, "s", this.s, true);
            if (this.t != ST_CellType.n)
                XmlHelper.WriteAttribute(sw, "t", this.t.ToString());
            XmlHelper.WriteAttribute(sw, "cm", this.cm);
            XmlHelper.WriteAttribute(sw, "vm", this.vm);
            XmlHelper.WriteAttribute(sw, "ph", this.ph, false);
            if (this.f == null
                && this.v == null
                && this.@is == null
                && this.extLstField == null)
            {
                sw.Write("/>");
            }
            else
            {
                sw.Write(">");
                if (this.f != null)
                    this.f.Write(sw, "f");
                if (!string.IsNullOrEmpty(this.v))
                    sw.WriteElementAndContent("v", XmlHelper.EncodeXml(this.v));
                else
                    sw.Write("<v/>");
                if (this.@is != null)
                    this.@is.Write(sw, "is");
                if (this.extLst != null)
                    this.extLst.Write(sw, "extLst");
                sw.WriteEndElement(nodeName);
            }
        }


        public void Set(CT_Cell cell)
        {
            fField = cell.fField;
            vField = cell.vField;
            isField = cell.isField;
            extLstField = cell.extLstField;
            rField = cell.rField;
            sField = cell.sField;
            tField = cell.tField;
            cmField = cell.cmField;
            vmField = cell.vmField;
            phField = cell.phField;
        }
        public bool IsSetT()
        {
            return tField != ST_CellType.n;
        }
        public bool IsSetS()
        {
            return sField != 0;
        }
        public bool IsSetF()
        {
            return fField != null;
        }
        public bool IsSetV()
        {
            return vField != null;
        }
        public bool IsSetIs()
        {
            return isField != null;
        }
        public bool IsSetR()
        {
            return rField != null;
        }
        public void unsetF()
        {
            this.fField = null;
        }

        public void unsetV()
        {
            this.vField = null;
        }

        public void unsetS()
        {
            this.sField = 0;
        }
        public void unsetT()
        {
            this.tField = ST_CellType.n;
        }

        public void unsetIs()
        {
            this.isField = null;
        }
        public void unsetR()
        {
            this.rField = null;
        }
        [XmlElement]
        public CT_CellFormula f
        {
            get
            {
                return this.fField;
            }
            set
            {
                this.fField = value;
            }
        }
        [XmlElement]
        public string v
        {
            get
            {
                return this.vField;
            }
            set
            {
                this.vField = value;
            }
        }

        [XmlElement("is")]
        public CT_Rst @is
        {
            get
            {
                return this.isField;
            }
            set
            {
                this.isField = value;
            }
        }
        [XmlElement]
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
        [XmlAttribute]
        public string r
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
        [DefaultValue(typeof(uint), "0")]
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
        [XmlAttribute]
        [DefaultValue(ST_CellType.n)]
        public ST_CellType t
        {
            get
            {
                return this.tField;
            }
            set
            {
                this.tField = value;
            }
        }
        [XmlAttribute]
        [DefaultValue(typeof(uint), "0")]
        public uint cm
        {
            get
            {
                return this.cmField;
            }
            set
            {
                this.cmField = value;
            }
        }
        [XmlAttribute]
        [DefaultValue(typeof(uint), "0")]
        public uint vm
        {
            get
            {
                return this.vmField;
            }
            set
            {
                this.vmField = value;
            }
        }
        [XmlAttribute]
        [DefaultValue(false)]
        public bool ph
        {
            get
            {
                return this.phField;
            }
            set
            {
                this.phField = value;
            }
        }
    }
}
