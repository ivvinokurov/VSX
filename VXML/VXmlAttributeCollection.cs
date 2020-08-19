using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VStorage;

namespace VXML
{
    /////////////////////////////////////////////////////////////
    ///////////////// VXmlAttributeCollection ///////////////////
    /////////////////////////////////////////////////////////////
    public class VXmlAttributeCollection: VXmlDummyCollection
    {
        internal VXmlAttributeCollection(VXmlNode _node, string name = "*")
            : base(_node, DEFX.PREFIX_ATTRIBUTE, name)
        {
        }

        internal VXmlAttributeCollection()
            : base(null, "", "")
        {
            object_fields =  new string[0];
        }


        
        /// <summary>
        /// Item (child node) by index
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public new VXmlAttribute this[int index]
        {
            get { return (VXmlAttribute)base[index]; }
        }
        
        /// <summary>
        /// Item (child node) by name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public new VXmlAttribute this[string name]
        {
            get { return (VXmlAttribute)base[name]; }
        }
    }
}
