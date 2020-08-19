using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VStorage;

namespace VXML
{
    /////////////////////////////////////////////////////////////
    //////////////////////// VXmlComment ////////////////////////
    /////////////////////////////////////////////////////////////

    public class VXmlComment : VXmlDummy
    {
        /// <summary>
        /// Constructor - read comment
        /// </summary>
        /// <param name="node"></param>
        /// <param name="value"></param>
        public VXmlComment(VXmlNode node, string name)
            : base(node, DEFX.NODE_TYPE_COMMENT, name) { }
    }
}
