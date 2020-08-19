using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VXML
{
    public class VXMLTemplate : List<VXmlParser.VXMLINT>
    {
        /////////////////////////////////////////////////////////////
        ///////////////////// VXmlTemplate //////////////////////////
        /////////////////////////////////////////////////////////////
        public VXMLTemplate()
        {
        }
        public VXMLTemplate(string name)
            : base(4096)
        {
            template_name = name;

        }

        public string template_name;

        /// <summary>
        /// Add item to template
        /// </summary>
        /// <param name="def"></param>
        /// <param name="type"></param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="idx"></param>
        /// <returns></returns>
        public VXmlParser.VXMLINT Add(string def, short type, string name = "", string value = "", int idx = 0)
        {
            VXmlParser.VXMLINT v = new VXmlParser.VXMLINT();
            v.def = def;
            v.type = type;
            v.name = name;
            v.value = value;
            v.index = idx;
            this.Add(v);
            return v;
        }
    }
}
