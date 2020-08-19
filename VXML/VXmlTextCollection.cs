using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VStorage;

namespace VXML
{
    /////////////////////////////////////////////////////////////
    ///////////////// VXmlTextCollection      ///////////////////
    /////////////////////////////////////////////////////////////
    public class VXmlTextCollection : VXmlDummyCollection
    {
        internal VXmlTextCollection(VXmlNode node)
            : base(node, DEFX.PREFIX_TEXT, "*")
        {
        }

        /// <summary>
        /// Item (child node) by index
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public new VXmlText this[int index]
        {
            get { return (VXmlText)base[index]; }
        }

        /// <summary>
        /// Item (child node) by name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public new VXmlText this[string name]
        {
            get { return (VXmlText)base[name]; }
        }

    }
}
