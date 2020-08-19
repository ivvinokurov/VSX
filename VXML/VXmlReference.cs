using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VStorage;

namespace VXML
{
    /////////////////////////////////////////////////////////////
    ///////////////////// VXmlReference /////////////////////////
    /////////////////////////////////////////////////////////////
    public class VXmlReference : VXmlNode
    {
        public VXmlReference(VSpace ns, VSpace cs)
            : base(ns, cs)
        {
        }

        /// <summary>
        /// Get/Set REF_ID
        /// </summary>
        public long ReferenceId
        {
            get { return REF_ID; }
            //set { RefId = value; }
        }

        /// <summary>
        /// Get reference node
        /// </summary>
        public VXmlNode ReferenceNode
        {
            get { return (REF_ID == 0)? null : GetNode(REF_ID); }
            //set { RefId = (value == null)? 0 : value.Id; }
        }

        /// <summary>
        /// Set reference node
        /// </summary>
        internal void set_reference_node(VXmlNode value)
        {
            REF_ID = (value == null)? 0 : value.Id;
        }

    }
}
