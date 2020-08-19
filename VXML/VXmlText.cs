using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VStorage;

namespace VXML
{
    public class VXmlText: VXmlDummy
    {
        /////////////////////////////////////////////////////////////
        ///////////////////////// VXmlText //////////////////////////
        /////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructor - read text node
        /// </summary>
        /// <param name="node"></param>
        /// <param name="value"></param>
        public VXmlText(VXmlNode node, string name)
            : base(node, DEFX.NODE_TYPE_TEXT, name) { }
    }
}
