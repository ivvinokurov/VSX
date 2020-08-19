using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VStorage
{
    /// <summary>
    /// Index Descriptor
    /// </summary>

    public class VSIndex
    {
        internal VSpace sp;

        internal VSIndex XRefs = null;           // References index for this index

        public string Error = "";

        private VSAllocation ALLOCATION = null;

        private string space_name = "";
        private string index_name = "";


        /// <summary>
        /// Constructor - read index descriptor
        /// </summary>
        /// <param name="s"></param>
        /// <param name="addr"></param>
        internal VSIndex(VSpace space, long addr)
        {
            this.sp = space;

            this.ALLOCATION = sp.GetAllocationByDescriptor(addr);

            string s = ALLOCATION.ReadString(0, 4);

            if (s != DEFS.INDEX_SIGNATURE)
                throw new VSException(DEFS.E0006_INVALID_SIGNATURE_CODE, " (Index Descriptor)");
            this.space_name = DEFS.ParseIndexSpace(this.Name);
            this.index_name = DEFS.ParseIndexName(this.Name);
        }

        /// <summary>
        /// Constructor - create index descriptor
        /// </summary>
        /// <param name="s"></param>
        /// <param name="name"></param>
        /// <param name="unique"></param>
        internal VSIndex(VSpace space, string name, bool unique)
        {
            this.sp = space;

            this.ALLOCATION = sp.AllocateSpace(VSIndex.INDEX_DESCRIPTOR_LEN, DEFS.POOL_INDEX, true);

            this.ALLOCATION.Write(0, DEFS.INDEX_SIGNATURE);
            
            this.Name = name.Trim().ToLower();

            this.ROOT = 0;

            this.POOL = 0;

            this.UNIQUE = (short)(unique ? 1 : 0);

            this.space_name = DEFS.ParseIndexSpace(this.Name);
            this.index_name = DEFS.ParseIndexName(this.Name);

        }

        /// <summary>
        /// Index Name
        /// </summary>
        private const long NAME_POS = 4;
        private const long NAME_LEN = 32;
        public string Name
        {
            get { return ALLOCATION.ReadString(NAME_POS, NAME_LEN).Trim(); }
            set { ALLOCATION.Write(NAME_POS, value.ToLower().PadRight((int)NAME_LEN)); }
        }

        /// <summary>
        /// Root node
        /// </summary>
        private const long ROOT_POS = NAME_POS + NAME_LEN;
        private const long ROOT_LEN = 8;
        public long ROOT
        {
            get { return ALLOCATION.ReadLong(ROOT_POS); }
            set { ALLOCATION.Write(ROOT_POS, value); }
        }

        /// <summary>
        /// Unique 1-yes; 0-no
        /// </summary>
        private const long UNIQUE_POS = ROOT_POS + ROOT_LEN;
        private const long UNIQUE_LEN = 2;
        private short UNIQUE
        {
            get { return ALLOCATION.ReadShort(UNIQUE_POS); }
            set { ALLOCATION.Write(UNIQUE_POS, value); }
        }

        /// <summary>
        /// Data pool for nodes
        /// </summary>
        private const long POOL_POS = UNIQUE_POS + UNIQUE_LEN;
        private const long POOL_LEN = 2;
        private short POOL
        {
            get { return ALLOCATION.ReadShort(POOL_POS); }
            set { ALLOCATION.Write(POOL_POS, value); }
        }

        private const long INDEX_DESCRIPTOR_LEN = POOL_POS + POOL_LEN;

        // NON-persistent

        internal long PREV
        {
            get { return ALLOCATION.PREV; }
        }
        internal long NEXT
        {
            get { return ALLOCATION.NEXT; }
        }

        internal long Id
        {
            get { return ALLOCATION.Id; }
        }


        /// <summary>
        /// Delete index
        /// </summary>
        internal void purge()
        {
            if (ALLOCATION == null)
                return;

            // Delete all cross-references

            if (POOL > 0)
                sp.ReleasePool(POOL);                  // Delete all nodes

            sp.Free(ALLOCATION);                     // Delete descriptor

            ALLOCATION = null;
        }

        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// If index is unique or no
        /// </summary>
        public bool UniqueIndex
        {
            get { return (UNIQUE == 0) ? false : true; }
        }

        /// <summary>
        /// Add new index (string key)
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool Insert(string key, long value)
        {
            return this.Insert(System.Text.Encoding.Default.GetBytes(key), value);
        }

        /// <summary>
        /// Add new index (bytes)
        /// </summary>
        /// <param name="key"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool Insert(byte[] key, long id)
        {
            if (this.index_name == DEFS.INDEX_CROSS_REFERENCES)
                throw new VSException(DEFS.E0055_INDEX_INVALID_OP_CODE, " - 'Insert' for '" + DEFS.INDEX_CROSS_REFERENCES + "'");

            long av_id = this.insert_node(key, id);
            if (av_id > 0)
            {
                byte[] obj_key = VSLib.ConvertLongToByteReverse(id);
                XRefs.insert_node(obj_key, av_id);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Add new index (bytes)
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public long insert_node(byte[] key, long value)
        {
            long rc = -1;

            _current = null;

            VSAvlNode node = null;

            if (ROOT == 0)
            {
                ROOT = create_node(key, value, 0).ID;
                return ROOT;
            }
            else
            {
                long nodeID = ROOT;

                while (nodeID > 0)
                {
                    node = read_node(nodeID);

                    int compare = compare_keys(key, node.KEY);

                    if (compare < 0)
                    {
                        if (node.LEFT == 0)
                        {
                            node.LEFT = create_node(key, value, node.ID).ID;
                            balance_insert(node, -1);
                            return node.LEFT;  
                        }
                        else
                            nodeID = node.LEFT;
                    }
                    else if (compare > 0)
                    {
                        if (node.RIGHT == 0)
                        {
                            node.RIGHT = create_node(key, value, node.ID).ID;
                            balance_insert(node, 1);
                            return node.RIGHT; 
                        }
                        else
                            nodeID = node.RIGHT;
                    }
                    else
                    { // Index value already exists
                        if (node.UNIQUE)
                        {
                            this.Error = "Index value already exists";
                            break;
                        }
                        else
                        {
                            if (node.add_ref(value))          // Add reference for non-unique value
                                rc = node.ID;
                            break;
                        }
                    }
                }
            }

            return rc;
        }

        /// <summary>
        /// Get ID by full or partial string key (first if if non-unique index)
        /// </summary>
        /// <param name="k"></param>
        /// <returns></returns>
        public long Find(string key, bool partial = false)
        {
            byte[] k = VSLib.ConvertStringToByte(key);
            return Find(k, partial);
        }

        /// <summary>
        /// Get ID by full or partial string key (first if if non-unique index)
        /// </summary>
        /// <param name="k"></param>
        /// <returns></returns>
        public long Find(byte[] key, bool partial = false)
        {
            long id = search_node(key, partial);
            if (id < 0)
                return -1;

            VSAvlNode a = read_node(id);
            return a.REF;
        }

        /// <summary>
        /// Get all IDs by full key (single ID if unique index)
        /// </summary>
        /// <param name="k"></param>
        /// <returns></returns>
        public long[] FindAll(string k, bool partial = false)
        {
            return this.FindAll(System.Text.Encoding.Default.GetBytes(k), partial);
        }

        /// <summary>
        /// Get all IDs by full byte key (single ID if unique index)
        /// </summary>
        /// <param name="k"></param>
        /// <returns></returns>
        public long[] FindAll(byte[] key, bool partial = false)
        {
            if (partial)
            {
                //string k = VSLib.ConvertByteToString(key);
                List<long> l = new List<long>();
                reset(key, true);

                while (Next())
                {
                    bool eq = true;
                    byte[] b = CurrentKeyBytes;

                    for (int i = 0; i < key.Length; i++)
                        if (key[i] != b[i])
                        {
                            eq = false;
                            break;
                        }

                    if (!eq)
                        break;

                    int j = CurrentRefs.Length;

                    for (int i = 0; i < j; i++)
                        l.Add(this.CurrentRefs[i]);
                }

                return l.ToArray();
            }
            else
            {
                long id = search_node(key, false);

                if (id < 0)
                    return new long[0];

                VSAvlNode a = read_node(id);
                return a.REFS;
            }
        }

        /// <summary>
        /// Check if node exists (string key)
        /// </summary>
        /// <param name="key"></param>
        /// <param name="partial"></param>
        /// <returns>true/false</returns>
        public bool Exists(string key, bool partial = false)
        {
            return Exists(VSLib.ConvertStringToByte(key), partial);
        }

        /// <summary>
        /// Check if node exists (byte key)
        /// </summary>
        /// <param name="key"></param>
        /// <param name="partial"></param>
        /// <returns>true/false</returns>
        public bool Exists(byte[] key, bool partial = false)
        {
            return (search_node(key, partial) >= 0);
        }

        /// <summary>
        /// Delete index (string key)
        /// </summary>
        /// <param name="key"></param>
        /// <param name="id">For non-unique index. 0 - delete a </param>
        /// <returns></returns>
        public bool Delete(string key, long id)
        {
            byte[] k = VSLib.ConvertStringToByte(key);
            return this.Delete(k, id);
        }

        /// <summary>
        /// Delete index (byte key), internal - no reference delete
        /// </summary>
        /// <param name="key"></param>
        /// <param name="id">For non-unique index. 0 - delete a </param>
        /// <returns>id of the deleted AVL node</returns>
        internal long delete_node(byte[] key, long id)
        {
            if ((id < 0) & (this.Name.IndexOf(DEFS.INDEX_CROSS_REFERENCES) < 0))
                throw new VSException(DEFS.E0052_DELETE_INDEX_ERROR_CODE, "Object ID is not specified");

            VSAvlNode node;

            long nodeID = ROOT;

            while (nodeID != 0)
            {
                node = read_node(nodeID);

                int result = compare_keys(key, node.KEY);

                if (result < 0)
                    nodeID = node.LEFT;
                else if (result > 0)
                    nodeID = node.RIGHT;
                else
                {
                    this.delete_avl_node(nodeID, id);
                    return nodeID;
                }
            }

            return -1;
        }

        /// <summary>
        /// Delete index (byte key)
        /// </summary>
        /// <param name="key"></param>
        /// <param name="id">For non-unique index. 0 - delete a </param>
        /// <returns></returns>
        public bool Delete(byte[] key, long id)
        {
            if (this.index_name == DEFS.INDEX_CROSS_REFERENCES)
                throw new VSException(DEFS.E0055_INDEX_INVALID_OP_CODE, " - 'Delete' for '" + DEFS.INDEX_CROSS_REFERENCES + "'");

            long av_id = this.delete_node(key, id);
            if (av_id > 0)
            {
                byte[] obj_key = VSLib.ConvertLongToByteReverse(id);
                XRefs.delete_node(obj_key, av_id);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Delete index by AvlNode Id and Object Id
        /// </summary>
        /// <param name="key"></param>
        /// <param name="obj_id">For non-unique index. 0 - delete all </param>
        /// <returns></returns>
        internal bool delete_avl_node(long avlnode_id, long obj_id)
        {
            VSAvlNode node = read_node(avlnode_id);

            if ((node.REF_COUNT > 1) & (obj_id > 0))
            {
                return node.delete_ref(obj_id);
            }

            if (obj_id > 0)
                if (obj_id != node.REF)
                {
                    Error = "ID " + obj_id.ToString() + " is not found for index node ID " + avlnode_id.ToString();
                    return false;
                }

            VSAvlNode left;
            VSAvlNode right;

            if (node.LEFT == 0)
            {
                if (node.RIGHT == 0)
                {
                    if (node.ID == ROOT)
                    {
                        ROOT = 0;
                        POOL = 0;
                    }
                    else
                    {
                        VSAvlNode parent = read_node(node.PARENT);

                        if (parent.LEFT == node.ID)
                        {
                            parent.LEFT = 0;
                            balance_delete(parent, 1);
                        }
                        else
                        {
                            parent.RIGHT = 0;
                            balance_delete(parent, -1);
                        }
                    }
                }
                else
                    replace_node(node, read_node(node.RIGHT));
            }
            else if (node.RIGHT == 0)
                replace_node(node, read_node(node.LEFT));
            else
            {
                VSAvlNode successor = read_node(node.RIGHT);

                if (successor.LEFT == 0)
                {

                    successor.PARENT = node.PARENT;
                    successor.LEFT = node.LEFT;
                    successor.BALANCE = node.BALANCE;

                    if (node.LEFT > 0)
                    {
                        left = read_node(node.LEFT);
                        left.PARENT = successor.ID;
                    }

                    if (node.ID == ROOT)            //Is it root?
                        ROOT = successor.ID;
                    else
                    {
                        VSAvlNode parent = read_node(node.PARENT);
                        if (parent.LEFT == node.ID)
                            parent.LEFT = successor.ID;
                        else
                            parent.RIGHT = successor.ID;
                    }

                    balance_delete(successor, -1);
                }
                else
                {
                    while (successor.LEFT > 0)
                        successor = read_node(successor.LEFT);

                    VSAvlNode successorRight;
                    VSAvlNode successorParent = read_node(successor.PARENT);

                    if (successorParent.LEFT == successor.ID)
                        successorParent.LEFT = successor.RIGHT;
                    else
                        successorParent.RIGHT = successor.RIGHT;

                    if (successor.RIGHT > 0)
                    {
                        successorRight = read_node(successor.RIGHT);
                        successorRight.PARENT = successorParent.ID;
                    }

                    successor.PARENT = node.PARENT;
                    successor.LEFT = node.LEFT;
                    successor.RIGHT = node.RIGHT;
                    successor.BALANCE = node.BALANCE;

                    right = read_node(node.RIGHT);
                    right.PARENT = successor.ID;

                    if (node.LEFT > 0)
                    {
                        left = read_node(node.LEFT);
                        left.PARENT = successor.ID;
                    }

                    if (node.ID == ROOT)                //Root?
                        ROOT = successor.ID;
                    else
                    {
                        VSAvlNode parent = read_node(node.PARENT);
                        if (parent.LEFT == node.ID)
                            parent.LEFT = successor.ID;
                        else
                            parent.RIGHT = successor.ID;
                    }

                    balance_delete(successorParent, 1);
                }
            }
            _current = null;
            node.Delete();

            return true;
        }

        /// <summary>
        /// Search node by key
        /// </summary>
        /// <param name="key"></param>
        /// <returns>-1 - not found</returns>
        private long search_node(byte[] key, bool partial = false)
        {
            long ret_node = -1;
            long nodeID = ROOT;
            while (nodeID > 0)
            {
                VSAvlNode node = read_node(nodeID);

                int compare = compare_keys(key, node.KEY, partial);
              //  __compares++;
                if (compare < 0)
                {
                    if (partial)
                    {
                        if (ret_node >= 0)
                            break;
                    }
                    nodeID = node.LEFT;
                }
                else if (compare > 0)
                {
                    nodeID = node.RIGHT;
                }
                else
                {
                    ret_node = node.ID;
                    if (!partial)
                        break;
                    else
                    {
                        nodeID = node.LEFT;
                    }
                }
            }

            return ret_node;
        }

        /// <summary>
        /// Balance tree after insertion
        /// </summary>
        /// <param name="b_node"></param>
        /// <param name="balance"></param>
        private void balance_insert(VSAvlNode bnode, int bal)
        {
            VSAvlNode node = bnode;
            bool ret = false;
            int balance = bal;

            while ((node != null) & (!ret))
            {
                balance = (node.BALANCE += balance);

                if (balance == 0)
                    ret = true;
                else
                { 
                    if (balance == -2)
                    {
                        VSAvlNode left = read_node(node.LEFT);
                        if (left.BALANCE == -1)
                            rotate_right(node);
                        else
                            rotate_left_right(node);

                        ret = true;
                    }
                    else if (balance == 2)
                    {
                        VSAvlNode right = read_node(node.RIGHT);
                        if (right.BALANCE == 1)
                            rotate_left(node);
                        else
                            rotate_right_left(node);

                        ret = true;
                    }

                    VSAvlNode parent = null;
                    if (node.PARENT > 0)
                    {
                        parent = read_node(node.PARENT);
                        balance = parent.LEFT == node.ID ? -1 : 1;
                    }
                    node = parent;
                }
            }
        }

        /// <summary>
        /// Create new AVLNODE - private method
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="parent"></param>
        /// <returns></returns>
        private VSAvlNode create_node(byte[] key, long value, long parent)
        {
            VSAvlNode a = new VSAvlNode(this);
            if (POOL == 0)
                POOL = sp.GetFreePoolNumber();
            if (POOL < 0)
                throw new VSException(DEFS.E0013_SPACE_NOT_AVAILABLE_CODE, "- no free pools for dynamic allocation");

            a.Create(key, value, parent, POOL);
            if (ROOT == 0)
                ROOT = a.ID;

            return a;
        }

        /// <summary>
        /// Read node
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public VSAvlNode read_node(long id)
        {
            VSAvlNode av = new VSAvlNode(this);
            av.Read(id);
            return av;
        }

        /// <summary>
        /// Compare keys
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="partial">true - "starts with"</param>
        /// <returns></returns>
        private int compare_keys(byte[] x, byte[] y, bool partial = false)
        {
            int l = Math.Min(x.Length, y.Length);

            for (int i = 0; i < l; i++)
            {
                if (x[i] > y[i])
                    return 1;
                else if (x[i] < y[i])
                    return -1;
            }
            if (!partial)
            {
                if (x.Length > y.Length)
                    return 1;
                else if (x.Length < y.Length)
                    return -1;
            }
            return 0;
        }

        /// <summary>
        /// Replace Node, used by "DeleteNode"
        /// </summary>
        /// <param name="node_id"></param>
        /// <param name="replacement_id"></param>
        private void replace_node(VSAvlNode target, VSAvlNode repl)
        {
            repl.PARENT = target.PARENT;

            if (target.ID == ROOT)
                ROOT = repl.ID;
            else
            {
                VSAvlNode parent = read_node(target.PARENT);
                if (parent.LEFT == target.ID)
                    parent.LEFT = repl.ID;
                else
                    parent.RIGHT = repl.ID;

                balance_delete(repl, 0);

            }
        }

        /// <summary>
        /// Balance after deletion
        /// </summary>
        /// <param name="node"></param>
        /// <param name="balance"></param>
        private void balance_delete(VSAvlNode bnode, int bal)
        {
            int balance = bal;
            VSAvlNode node = bnode;
            bool ret = false;

            while ((node != null) & (!ret))
            {
                balance = (node.BALANCE += balance);

                if (balance == -2)
                {
                    VSAvlNode left = read_node(node.LEFT);
                    if (left.BALANCE <= 0)
                    {
                        node = rotate_right(node);

                        if (node.BALANCE == 1)
                            ret = true;
                    }
                    else
                        node = rotate_left_right(node);
                }
                else if (balance == 2)
                {
                    VSAvlNode right = read_node(node.RIGHT);
                    if (right.BALANCE >= 0)
                    {
                        node = rotate_left(node);

                        if (node.BALANCE == -1)
                            ret = true;
                    }
                    else
                        node = rotate_right_left(node);
                }
                else if (balance != 0)
                    ret = true;

                VSAvlNode parent = null;
                if (node.PARENT > 0)
                {
                    parent = read_node(node.PARENT);
                    balance = (parent.LEFT == node.ID) ? 1 : -1;
                }
                node = parent;
            }
        }

        /// <summary>
        /// Rotate Left
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private VSAvlNode rotate_left(VSAvlNode node)
        {
            VSAvlNode right = read_node(node.RIGHT);
            long ParentID = node.PARENT;
            long RightLeftID= right.LEFT;


            right.PARENT = node.PARENT;
            right.LEFT = node.ID;

            node.RIGHT = RightLeftID;
            node.PARENT = right.ID;

            if (RightLeftID > 0)
            {
                VSAvlNode rightLeft = read_node(RightLeftID);
                rightLeft.PARENT = node.ID;
            }

            if (node.ID == ROOT)            //Root?
                ROOT = right.ID;
            else
            {
                VSAvlNode parent = read_node(ParentID);

                if (parent.RIGHT == node.ID)
                    parent.RIGHT = right.ID;
                else if (parent.LEFT == node.ID)
                    parent.LEFT = right.ID;
            }

            right.BALANCE--;

            node.BALANCE = -right.BALANCE;

            return right;
        }

        /// <summary>
        /// Rotate node right
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private VSAvlNode rotate_right(VSAvlNode node)
        {
            VSAvlNode left = read_node(node.LEFT);
            long LeftRightID = left.RIGHT;
            long ParentID = node.PARENT;

            left.PARENT = ParentID;
            left.RIGHT = node.ID;

            node.LEFT = LeftRightID;
            node.PARENT = left.ID;

            if (LeftRightID > 0)
            {
                VSAvlNode leftRight = read_node(LeftRightID);
                leftRight.PARENT = node.ID;
            }

            if (node.ID == ROOT)            //Root?
                ROOT = left.ID;
            else
            {
                VSAvlNode parent = read_node(ParentID);

                if (parent.LEFT == node.ID)
                    parent.LEFT = left.ID;
                else if (parent.RIGHT == node.ID)
                    parent.RIGHT = left.ID;
            }
            left.BALANCE++;

            node.BALANCE = -left.BALANCE;

            return left;
        }

        /// <summary>
        /// Rotate Left Right
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private VSAvlNode rotate_left_right(VSAvlNode node)
        {
            VSAvlNode left = read_node(node.LEFT);
            VSAvlNode leftRight = read_node(left.RIGHT);
            long parentID = node.PARENT;

            long leftRightRightID = leftRight.RIGHT;
            long leftRightLeftID = leftRight.LEFT;

            leftRight.PARENT = node.PARENT;
            node.LEFT = leftRightRightID;
            left.RIGHT = leftRightLeftID;
            leftRight.LEFT = left.ID;
            leftRight.RIGHT = node.ID;
            left.PARENT = leftRight.ID;
            node.PARENT = leftRight.ID;

            if (leftRightRightID > 0)
            {
                VSAvlNode leftRightRight = read_node(leftRightRightID);
                leftRightRight.PARENT = node.ID;
            }

            if (leftRightLeftID > 0)
            {
                VSAvlNode leftRightLeft = read_node(leftRightLeftID);
                leftRightLeft.PARENT = left.ID;
            }

            if (node.ID == ROOT)                //Root?
                ROOT = leftRight.ID;
            else 
            {
                VSAvlNode parent = read_node(parentID);
                if (parent.LEFT == node.ID)
                    parent.LEFT = leftRight.ID;
                else
                    parent.RIGHT = leftRight.ID;
            }

            if (leftRight.BALANCE == 1)
            {
                node.BALANCE = 0;
                left.BALANCE = -1;
            }
            else if (leftRight.BALANCE == 0)
            {
                node.BALANCE = 0;
                left.BALANCE = 0;
            }
            else
            {
                node.BALANCE = 1;
                left.BALANCE = 0;
            }

            leftRight.BALANCE = 0;

            return leftRight;
        }

        /// <summary>
        /// Rotate Right Left
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private VSAvlNode rotate_right_left(VSAvlNode node)
        {
            VSAvlNode right = read_node(node.RIGHT);
            VSAvlNode rightLeft = read_node(right.LEFT);
            long parentID = node.PARENT;
            long rightLeftLeftID = rightLeft.LEFT;
            long rightLeftRightID = rightLeft.RIGHT;

            rightLeft.PARENT = parentID;
            node.RIGHT = rightLeftLeftID;
            right.LEFT = rightLeftRightID;

            rightLeft.RIGHT = right.ID;
            rightLeft.LEFT = node.ID;
            right.PARENT = node.PARENT = rightLeft.ID;

            if (rightLeftLeftID > 0)
            {
                VSAvlNode rightLeftLeft = read_node(rightLeftLeftID);
                rightLeftLeft.PARENT = node.ID;
            }

            if (rightLeftRightID > 0)
            {
                VSAvlNode rightLeftRight = read_node(rightLeftRightID);
                rightLeftRight.PARENT = right.ID;
            }

            if (node.ID == ROOT)            //Root?
                ROOT = rightLeft.ID;
            else 
            {
                VSAvlNode parent = read_node(parentID);
                if (parent.RIGHT == node.ID)
                    parent.RIGHT = rightLeft.ID;
                else
                    parent.LEFT = rightLeft.ID;
            }
            if (rightLeft.BALANCE == -1)
            {
                node.BALANCE = 0;
                right.BALANCE = 1;
            }
            else if (rightLeft.BALANCE == 0)
            {
                node.BALANCE = 0;
                right.BALANCE = 0;
            }
            else
            {
                node.BALANCE = -1;
                right.BALANCE = 0;
            }

            rightLeft.BALANCE = 0;
            return rightLeft;
        }

        //////////////////////////////////////////////
        //////////// ENUMERATOR //////////////////////
        //////////////////////////////////////////////

        private VSAvlNode _current;// =  new VSAvlNode();
        private VSAvlNode _right;
        private Action _action = Action.End;

        enum Action
        {
            Parent,
            Right,
            End,
            Current
        }

        /// <summary>
        /// Next node
        /// </summary>
        /// <returns></returns>
        public bool Next()
        {
            switch (_action)
            {
                case Action.Right:
                    _current = _right;

                    while (_current.LEFT != 0)
                    {
                        _current = read_node(_current.LEFT);
                    }

                    _action = _current.RIGHT != 0 ? Action.Right : Action.Parent;

                    if (_action == Action.Right)
                        _right = read_node(_current.RIGHT);

                    return true;

                case Action.Parent:
                    while (_current.PARENT != 0)
                    {
                        VSAvlNode previous = _current;

                        _current = read_node(_current.PARENT);

                        if (_current.LEFT == previous.ID)
                        {
                            _action = _current.RIGHT != 0 ? Action.Right : Action.Parent;

                            if (_action == Action.Right)
                                _right = read_node(_current.RIGHT);

                            return true;
                        }
                    }

                    _action = Action.End;

                    return false;

                case Action.Current:
                    _action = _current.RIGHT != 0 ? Action.Right : Action.Parent;

                    if (_current.RIGHT > 0)
                        _right = read_node(_current.RIGHT);

                    return true;


                default:
                    return false;
            }

        }

        /// <summary>
        /// Reset
        /// </summary>
        public void Reset()
        {
            _current = new VSAvlNode(this);
            if (ROOT > 0)
            {
                _right = _current = read_node(ROOT);

                _action = Action.Right;
            }
            else
                _action = Action.End;
        }

        /// <summary>
        /// Reset by key
        /// </summary>
        /// <param name="key"></param>
        private void reset(byte[] key, bool partial = false)
        {
            _current = new VSAvlNode(this);

            if (ROOT > 0)
            {
                long id = search_node(key, partial);
                if (id > 0)
                {
                    _action = Action.Current;
                    _right = read_node(id);
                    _current = _right;
                }
                else
                    _action = Action.End;
            }
            else
                _action = Action.End;
        }

        /// <summary>
        /// Reset by string key
        /// </summary>
        /// <param name="key"></param>
        public void Reset(string key, bool partial = false)
        {
            byte[] b = System.Text.Encoding.Default.GetBytes(key);
            reset(b, partial);
        }

        /// <summary>
        /// Current node
        /// </summary>
        private VSAvlNode CurrentAVL
        {
            get { return _current; }
        }

        /// <summary>
        /// Current value(s)
        /// </summary>
        public long[] CurrentRefs
        {
            get
            {
                if (_action == Action.End)
                    return new long[0];
                else
                {
                    long[] ret = new long[1];
                    ret = CurrentAVL.REFS;
                    return ret;
                }
            }
        }

        /// <summary>
        /// All keys of this index
        /// </summary>
        public string[] Keys
        {
            get
            {
                List<string> k = new List<string>(1024);
                this.Reset();
                while (this.Next())
                    k.Add(CurrentKey);
                return k.ToArray();
            }
        }

        /// <summary>
        /// Current key (string)
        /// </summary>
        public string CurrentKey
        {
            get { return VSLib.ConvertByteToString(this.CurrentKeyBytes); }
        }

        /// <summary>
        /// Current key (bytes)
        /// </summary>
        public byte[] CurrentKeyBytes
        {
            get
            {
                if (_action == Action.End)
                    return new byte[0];
                else
                    return CurrentAVL.KEY;
            }
        }

        /// <summary>
        /// Current AVLNode
        /// </summary>
        public VSAvlNode CurrentAvlNode
        {
            get { return CurrentAVL; }
        }
    }
}
