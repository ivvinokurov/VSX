using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VStorage;

namespace VXML
{
    /////////////////////////////////////////////////////////////
    ////////////////////// VXmlAttribute ////////////////////////
    /////////////////////////////////////////////////////////////
    public class VXmlAttribute : VXmlDummy
    {
        public VXmlAttribute(VXmlNode node, string name)
            : base(node, DEFX.NODE_TYPE_ATTRIBUTE, name) { }
    }
}
