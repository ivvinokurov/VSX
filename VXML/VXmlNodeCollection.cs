using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VStorage;

namespace VXML
{
    /////////////////////////////////////////////////////////////
    /////////////////// VXmlNodeCollection //////////////////////
    /////////////////////////////////////////////////////////////

    public class VXmlNodeCollection : System.Collections.IEnumerable
    {
        private VXmlNode node = null;
        private List<long> node_ids = null;


        public VXmlNodeCollection(VXmlNode _node, long[] _nodeids)
        {
            node = _node;

            node_ids = new List<long>(256);
            if (_nodeids != null)
                for (int i = 0; i < _nodeids.Length; i++)
                    node_ids.Add(_nodeids[i]);
        }

        // Implementation for the GetEnumerator method.
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return (System.Collections.IEnumerator)GetEnumerator();
        }

        public VXmlNodeCollectionEnum GetEnumerator()
        {
            return new VXmlNodeCollectionEnum(node, node_ids.ToArray());
        }

        /// <summary>
        /// Add item to the collection
        /// </summary>
        /// <param name="id"></param>
        internal void Add(long id)
        {
            node_ids.Add(id);
        }

        /// <summary>
        /// Remove by index
        /// </summary>
        /// <param name="n"></param>
        public void RemoveAt(int n)
        {
            if (node_ids != null)
                if (n < node_ids.Count)
                    node_ids.RemoveAt(n);
        }


        /// <summary>
        /// Item (child node) by index
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public virtual VXmlNode this[int index]
        {
            get
            {
                if (node_ids == null)
                    return null;
                else
                    return ((index >= node_ids.Count) | (index < 0)) ? null : node.GetNode(node_ids[index]);
            }
        }



        /// <summary>
        /// Item (child node) by name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public virtual VXmlNode this[string name]
        {
            get
            {
                long id = get_node_by_name(name);
                if (id == 0)
                    return null;
                else
                    return node.GetNode(id);
            }
        }

        /// <summary>
        /// Get node ID by index
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        public long GetNodeId(int n)
        {
            return node_ids[n];
        }

        /// <summary>
        /// Set node ID by index
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        public void SetNodeId(int n, long value)
        {
            node_ids[n] = value;
        }


        /// <summary>
        /// Number of elements
        /// </summary>
        public int Count
        {
            get
            {
                return (node_ids == null) ? 0 : node_ids.Count;
            }
        }

        /// <summary>
        /// Get id by name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        protected long get_node_by_name(string name)
        {
            if (node_ids == null)
                return 0;

            for (int i = 0; i < node_ids.Count; i++)
            {
                VXmlNode n = node.GetNode(node_ids[i]);
                if (VSLib.Compare(name, n.Name))
                    return n.Id;
            }
            return 0;
        }
    }
    /// <summary>
    /// ENUMERATOR CLASS
    /// </summary>
    public class VXmlNodeCollectionEnum : System.Collections.IEnumerator
    {
        private long[] node_ids = null;
        private VXmlNode node = null;
        private int pos = -1;
        /// <summary>
        /// Initialize collection by the array of ids
        /// </summary>
        /// <param name="_node"></param>
        /// <param name="_nodeids"></param>
        public VXmlNodeCollectionEnum(VXmlNode _node, long[] _nodeids)
        {
            node = _node;
            node_ids = _nodeids;
            //if (node_ids.Length > 0)
            //    pos = 0;
        }

        object System.Collections.IEnumerator.Current
        {
            get
            {
                return Current;
            }
        }

        /// <summary>
        /// Current node
        /// </summary>
        public VXmlNode Current
        {
            get 
            {
                if ((pos < 0) | (pos >= node_ids.Length))
                    return null;
                else
                    return this.node.GetNode(node_ids[pos]); ;
            }
        }
        /// <summary>
        /// Current node
        /// </summary>
        public void Reset() { pos = (node_ids.Length == 0)? -1 : 0; }

        /// <summary>
        /// Move to the next item
        /// </summary>
        /// <returns></returns>
        public bool MoveNext() 
        {
            if (node_ids.Length == 0)
                return false;
            pos++;

            if (pos < node_ids.Length)
                return true;

            return false;
        }
    }
}
