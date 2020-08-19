using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VStorage;

namespace VXML
{
    /////////////////////////////////////////////////////////////
    ///////////////// VXmlCommentCollection ///////////////////
    /////////////////////////////////////////////////////////////
    public class VXmlCommentCollection : VXmlDummyCollection
    {
        internal VXmlCommentCollection(VXmlNode node)
            : base(node, DEFX.PREFIX_COMMENT, "*")
        {
        }

        /// <summary>
        /// Item (child node) by index
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public new VXmlComment this[int index]
        {
            get { return (VXmlComment)base[index]; }
        }

        /// <summary>
        /// Item (child node) by name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public new VXmlComment this[string name]
        {
            get { return (VXmlComment)base[name]; }
        }

    }
}
