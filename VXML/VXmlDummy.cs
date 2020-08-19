using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VStorage;

namespace VXML
{
    /// <summary>
    /// Dummy node: root classfor attributes
    /// </summary>
    public class VXmlDummy
    {
        /// <summary>
        /// Base node
        /// </summary>
        protected VXmlNode BASE_NODE = null;
        protected VSObject BASE_OBJECT = null;

        /// <summary>
        /// Object fields
        /// </summary>
        protected string field_name = "";
        protected short field_type = -1;
        protected string field_value = "";
        protected string field_prefix = "";

        /// <summary>
        /// Constructor "Open"
        /// </summary>
        /// <param name="node"></param>
        internal VXmlDummy(VXmlNode node, short type, string name)
        {
            BASE_NODE = node;
            BASE_OBJECT = node.OBJ;
            field_type = type;
            field_name = name;
            field_value = "";
            field_prefix = DEFX.NODE_TYPE_INTERNAL_FIELD_PREFIX[type - 100];
        }
        /// <summary>
        /// Numeric id (empty)
        /// </summary>
        public long Id
        {
            get { return 0; }
        }

        /// <summary>
        /// String id (N/A)
        /// </summary>
        public string ID
        {
            get { return "0"; }
        }

        /// <summary>
        /// Name
        /// </summary>
        public string Name
        {
            get { return field_name; }
        }

        /// <summary>
        /// Value
        /// </summary>
        public string Value
        {
            get
            {
                return BASE_OBJECT.GetString(field_prefix + field_name);
            }
            set
            {
                BASE_OBJECT.Set(field_prefix + field_name, value);
            }
        }

        /// <summary>
        /// Node type property
        /// </summary>
        public string NodeType
        {
            get { return DEFX.GET_NODETYPE(field_type); }
        }

        /// <summary>
        /// Node type code property
        /// </summary>
        public short NodeTypeCode
        {
            get { return field_type; }
        }


        /// <summary>
        /// Check is object field already exists
        /// </summary>
        //internal bool Exists
        //{
        //    get { return BASE_OBJECT.Exists(field_prefix + field_name); }
        //}

    }
}
