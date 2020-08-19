using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VStorage;

namespace VXML
{
    /////////////////////////////////////////////////////////////
    ////////////////////// VXmlElement //////////////////////////
    /////////////////////////////////////////////////////////////
    public class VXmlElement : VXmlNode
    {
        public VXmlElement(VSpace ns, VSpace cs)
            : base(ns, cs)
        {
        }
    }
}
