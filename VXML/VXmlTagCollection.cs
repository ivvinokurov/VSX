using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VStorage;

namespace VXML
{
    /////////////////////////////////////////////////////////////
    ///////////////// VXmlTagCollection       ///////////////////
    /////////////////////////////////////////////////////////////
    public class VXmlTagCollection : VXmlDummyCollection
    {

        internal VXmlTagCollection(VXmlNode node)
            : base(node, DEFX.PREFIX_TAG, "*")
        {
        }

        internal VXmlTagCollection()
            : base(null, "", "")
        {
            object_fields =  new string[0];
        }

        /// <summary>
        /// Item (child node) by index
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public new VXmlTag this[int index]
        {
            get { return (VXmlTag)base[index]; }
        }

        /// <summary>
        /// Item (child node) by name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public new VXmlTag this[string name]
        {
            get { return (VXmlTag)base[name]; }
        }

    }
}
