using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VStorage;

namespace VXML
{
    public class VXmlTag : VXmlDummy
    {
        /////////////////////////////////////////////////////////////
        ///////////////////////// VXmlTag  //////////////////////////
        /////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructor - read tag node
        /// </summary>
        /// <param name="node"></param>
        /// <param name="value"></param>
        public VXmlTag(VXmlNode node, string name)
            : base(node, DEFX.NODE_TYPE_TAG, name) { }

        /// <summary>
        /// Value
        /// </summary>
        public new string Value
        {
            get { return base.Value; }
        }
    }
}
