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
    public class VXmlDummyCollection
    {
        /// <summary>
        /// Base node
        /// </summary>
        protected VXmlNode BASE_NODE = null;
        protected VSObject BASE_OBJECT = null;

        // Field names
        protected string[] object_fields = null;
        protected string field_type = "";


        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="node"></param>
        internal VXmlDummyCollection(VXmlNode node, string type, string prefix)
        {
            // Save base objects
            BASE_NODE = node;
            BASE_OBJECT = node.OBJ;
            field_type = type;

            // Get fields list
            object_fields = BASE_OBJECT.GetFields(type + prefix);
            
            for (int i = 0; i < object_fields.Length; i++)
                object_fields[i] = object_fields[i].Remove(0, 1);

        }

        /// <summary>
        /// Items count
        /// </summary>
        public int Count
        {
            get { return object_fields.Length; }
        }

        /// <summary>
        /// Item (child node) by index
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public VXmlDummy this[int index]
        {
            get
            {
                if ((index < 0) | (index >= object_fields.Length))
                    return null;

                if (field_type == DEFX.PREFIX_ATTRIBUTE)
                    return new VXmlAttribute(BASE_NODE, object_fields[index]);
                else if (field_type == DEFX.PREFIX_TEXT)
                    return new VXmlText(BASE_NODE, object_fields[index]);
                else if (field_type == DEFX.PREFIX_COMMENT)
                    return new VXmlComment(BASE_NODE, object_fields[index]);
                else if (field_type == DEFX.PREFIX_TAG)
                    return new VXmlTag(BASE_NODE, object_fields[index]);
                else
                    return null;
            }
        }

        /// <summary>
        /// Item (child node) by name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public VXmlDummy this[string name]
        {
            get
            {
                for (int i = 0; i < object_fields.Length; i++)
                    if (object_fields[i] == name.Trim())
                        return this[i];

                return null;
            }
        }

    }
}
