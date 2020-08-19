using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VStorage
{
    ///////////////////////////////////////
    //////// Balanced Binary Tree  ////////
    ///////////////////////////////////////
    public class VSBBTree
    {
        public const int COND_EQ = 0;   // =
        public const int COND_LE = -1;  // <=
        public const int COND_LT = -2;  // <
        public const int COND_GE = 1;   // >=
        public const int COND_GT = 2;   // >

        /// <summary>
        /// Return tree item (single)
        /// </summary>
        public struct BTResult
        {
            public string KeyString;        // Item key (string)
            public long Key;                // Item key (long)
            public long Value;              // Item value
        }

        /// <summary>
        /// Return tree item (multiple)
        /// </summary>
        public struct BTResultList
        {
            public string KeyString;        // Item key (string)
            public long Key;                // Item key
            public long[] Value;            // Item values
        }


        /// <summary>
        /// Trre item
        /// </summary>
        public struct TreeItem
        {
            public byte[] Key;              // Item key
            public List<long> Value;        // Item value(s)
            public int Balance;             // Item balance
            public int Parent;              // -1 - top ; or parent item
            public int Left;                // Left item
            public int Right;               // Right item
            public bool Used;               // true - used
        }

        private const int INITIAL_SIZE_DEFAULT = 2048;                        // Initial array size
        private int INITIAL_SIZE = 0;                                         // Initial array size

        private int CURRENT_SIZE = 0;                   // Current array size
        private int CURRENT_USED = 0;                   // Current used size
        private int MAX_USED = 0;                       // Maximum index of the used item
        private int ROOT = -1;
        private bool UNIQUE = true;

        private TreeItem[] BTree = null;                // Binary tree
        private List<int> FreeItems = null;             // Free (deleted) items

        public string Name = "";

        /// <summary>
        /// Constructor
        /// </summary>
        public VSBBTree(string name, int size = 0, bool unique = true)
        {
            Name = name;
            INITIAL_SIZE = Math.Max(size, INITIAL_SIZE_DEFAULT);
            BTree = new TreeItem[INITIAL_SIZE];
            FreeItems = new List<int>(INITIAL_SIZE);
            ROOT = -1;
            CURRENT_SIZE = BTree.Length;
            CURRENT_USED = 0;
            MAX_USED = 0;
            UNIQUE = unique;
        }

        /// <summary>
        /// Add key (long)
        /// </summary>
        /// <param name="Value"></param>
        /// <returns></returns>
        public bool Insert(long key, long value)
        {
            return this.insert(VSLib.ConvertLongToByteReverse(key), value);
        }

        /// <summary>
        /// Add key (string)
        /// </summary>
        /// <param name="Value"></param>
        /// <returns></returns>
        public bool Insert(string key, long value)
        {
            return this.insert(VSLib.ConvertStringToByte(key), value);
        }

        /// <summary>
        /// Delete key (long)
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool Delete(long key, long value = -1)
        {
            return this.delete(VSLib.ConvertLongToByteReverse(key), value);
        }

        /// <summary>
        /// Delete key (string)
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool Delete(string key, long value = -1)
        {
            return this.delete(VSLib.ConvertStringToByte(key), value);
        }

        /// <summary>
        ///  Find key (long)
        /// </summary>
        /// <param name="key"></param>
        /// <param name="op">0 - equal; -1 - nearest left (less than); 1 - nearest right (more than) </param>
        /// <returns>Value</returns>
        public BTResult Find(long key, int op)
        {
            int i = find(VSLib.ConvertLongToByteReverse(key), op);
            BTResult ret = new BTResult();

            ret.Key = (i < 0) ? -1 : VSLib.ConvertByteToLongReverse(BTree[i].Key);
            ret.Value = (i < 0) ? -1 : BTree[i].Value[0];

            return ret;
        }

        /// <summary>
        ///  Find key (string)
        /// </summary>
        /// <param name="key"></param>
        /// <param name="op">0 - equal; -1 - nearest left (less than); 1 - nearest right (more than) </param>
        /// <returns>Value</returns>
        public BTResult Find(string key, int op)
        {
            int i = find(VSLib.ConvertStringToByte(key), op);
            BTResult ret = new BTResult();

            ret.KeyString = (i < 0) ? "" : VSLib.ConvertByteToString(BTree[i].Key);
            ret.Value = (i < 0) ? -1 : BTree[i].Value[0];

            return ret;
        }

        /// <summary>
        ///  Find all keys (non-unique), long
        /// </summary>
        /// <param name="key"></param>
        /// <param name="op">0 - equal; -1 - nearest left (less than); 1 - nearest right (more than) </param>
        /// <returns></returns>
        public BTResultList FindAll(long key, int op)
        {
            int i = find(VSLib.ConvertLongToByteReverse(key), op);

            BTResultList ret = new BTResultList();

            ret.Key = (i < 0) ? -1 : VSLib.ConvertByteToLongReverse(BTree[i].Key);
            ret.Value = (i < 0) ? new long[0] : BTree[i].Value.ToArray();

            return ret;
        }

        /// <summary>
        ///  Find all keys (non-unique), string
        /// </summary>
        /// <param name="key"></param>
        /// <param name="op">0 - equal; -1 - nearest left (less than); 1 - nearest right (more than) </param>
        /// <returns></returns>
        public BTResultList FindAll(string key, int op)
        {
            int i = find(VSLib.ConvertStringToByte(key), op);

            BTResultList ret = new BTResultList();

            ret.KeyString = (i < 0) ? "" : VSLib.ConvertByteToString(BTree[i].Key);
            ret.Value = (i < 0) ? new long[0] : BTree[i].Value.ToArray();

            return ret;
        }

        ////////////////////////////////////////////////////////////////////////
        ///////////////////  PRIVATE METHODS ///////////////////////////////////
        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        ///  Find key
        /// </summary>
        /// <param name="key"></param>
        /// <param name="op">0 - equal; -1 - less or equal; -2 - less than; 1 - equal or more; 2 - more than</param>
        /// <returns>Index or -1</returns>
        private int find(byte[] key, int op)
        {
            int ret = -1;
            int ret_left = -1;
            int ret_right = -1;
            int nodeID = ROOT;
            while (nodeID >= 0)
            {
                int compare = VSLib.CompareKeys(key, BTree[nodeID].Key);

                if (compare < 0)
                {
                    ret_left = nodeID;                  // Node Key > key
                    nodeID = BTree[nodeID].Left;
                }
                else if (compare > 0)
                {
                    ret_right = nodeID;                 // Node key < key
                    nodeID = BTree[nodeID].Right;
                }
                else
                {
                    ret = nodeID;
                    break;
                }
            }

            if (ret >= 0)           // Key found
            {
                if ((op == COND_EQ) | (op == COND_LE) | (op == COND_GE))
                    return ret;
                else
                {
                    if (op > 0)
                        return find_successor(ret);
                    else
                        return
                            find_predecessor(ret);
                }
            }
            else  // Key not found
            {
                if (op == COND_EQ)
                    return ret;
                else
                {
                    if ((op == COND_LE) | (op == COND_LT))
                        return ret_right;
                    else
                        return ret_left;
                }
            }
        }

        /// <summary>
        /// Find sucessor for node
        /// </summary>
        /// <param name="nodeID"></param>
        /// <returns></returns>
        private int find_successor(int nodeID)
        {
            int current = nodeID;
            if (BTree[current].Right >= 0)
                return find_min_recursively(BTree[current].Right);
            else
            {
                int tempParent = BTree[current].Parent;
                while (tempParent >= 0)
                {
                    if (current != BTree[tempParent].Right)
                        break;
                    current = tempParent;
                    tempParent = BTree[tempParent].Parent;
                }
                if (tempParent >= 0)
                    return tempParent;
                else
                    return -1;
            }
        }

        /// <summary>
        /// Recursive search min
        /// </summary>
        /// <param name="root"></param>
        /// <returns></returns>
        public int find_min_recursively(int root)
        {
            int current = root;
            if (BTree[current].Left < 0)
            {
                return current;
            }
            return find_min_recursively(BTree[current].Left);
        }

        /// <summary>
        /// Find predecessor
        /// </summary>
        /// <param name="nodeID"></param>
        /// <returns></returns>
        private int find_predecessor(int nodeID)
        {
            int current = nodeID;
            if (BTree[current].Left >= 0)
                return find_max_recursive(BTree[current].Left);
            else
            {
                int tempParent = BTree[current].Parent;
                while (tempParent >= 0)
                {
                    if (current != BTree[tempParent].Left)
                        break;
                    current = tempParent;
                    tempParent = BTree[tempParent].Parent;
                }
                if (tempParent >= 0)
                    return tempParent;
                else
                    return -1;
            }
        }

        /// <summary>
        /// Recursively search max
        /// </summary>
        /// <param name="root"></param>
        /// <returns></returns>
        public int find_max_recursive(int root)
        {
            int current = root;
            if (BTree[current].Right < 0)
            {
                return current;
            }
            return find_max_recursive(BTree[current].Right);
        }

        /// <summary>
        /// Inser new node
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="parent"></param>
        /// <returns></returns>
        private int create_node(byte[] key, long value, int parent)
        {
            int idx = -1;

            // Find place to add
            if (MAX_USED < CURRENT_SIZE)                // Append
            {
                idx = MAX_USED;
                MAX_USED++;
            }
            else if (CURRENT_USED < CURRENT_SIZE)         // Insert
            {
                int n = FreeItems.Count - 1;
                idx = FreeItems[n];
                FreeItems.RemoveAt(n);
            }

            // Add item
            BTree[idx].Key = key;

            BTree[idx].Value = new List<long>();
            BTree[idx].Value.Add(value);

            BTree[idx].Parent = parent;
            BTree[idx].Left = -1;
            BTree[idx].Right = -1;
            BTree[idx].Balance = 0;
            BTree[idx].Used = true;

            CURRENT_USED++;

            return idx;
        }

        /// <summary>
        /// Delete node from array
        /// </summary>
        /// <param name="node"></param>
        private void delete_node(int nodeId)
        {
            //VSDebug.StopPoint(nodeId, 4);
            CURRENT_USED--;

            if (nodeId == (MAX_USED - 1))
                MAX_USED--;     // If last
            else
            {
                if (CURRENT_USED > 0)
                    FreeItems.Add(nodeId);
            }

            BTree[nodeId].Key = null;
            BTree[nodeId].Parent = -1;
            BTree[nodeId].Left = -1;
            BTree[nodeId].Right = -1;
            BTree[nodeId].Balance = 0;
            BTree[nodeId].Value = null;
            BTree[nodeId].Used = false;

            if (MAX_USED > 0)
            {
                while (BTree[MAX_USED - 1].Key == null)
                {
                    MAX_USED--;
                    for (int i = 0; i < FreeItems.Count; i++)
                        if (FreeItems[i] == MAX_USED)
                        {
                            FreeItems.RemoveAt(i);
                            break;
                        }
                    if (MAX_USED == 0)
                        break;
                }
            }
        }

        /// <summary>
        /// Delete reference
        /// </summary>
        /// <param name="node"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private bool delete_ref(int node, long value)
        {
            for (int i = 0; i < BTree[node].Value.Count; i++)
                if (BTree[node].Value[i] == value)
                {
                    BTree[node].Value.RemoveAt(i);
                    return true;
                }
            return false;
        }

        /// <summary>
        /// Replace node
        /// </summary>
        /// <param name="n1"></param>
        /// <param name="n2"></param>
        private void replace_node(int target, int repl)
        {
            int parent = BTree[target].Parent;

            BTree[repl].Parent = parent;

            if (target == ROOT)
            {
                ROOT = repl;
            }
            else
            {
                if (BTree[parent].Left == target)
                    BTree[parent].Left = repl;
                else
                    BTree[parent].Right = repl;

                balance_delete(repl, 0);
            }
        }

        /// <summary>
        /// Balance tree after insertion
        /// bal = -1 : LEFT node added; +1: RIGHT node added
        /// </summary>
        /// <param name="nodeId"></param>
        /// <param name="balance"></param>
        private void balance_insert(int nodeId, int bal)
        {
            int node = nodeId;
            bool ret = false;
            int balance = bal;

            while ((node >= 0) & (!ret))
            {
                balance = (BTree[node].Balance += balance);

                if (BTree[node].Balance == 0)
                    ret = true;
                else
                {
                    if (BTree[node].Balance == -2)
                    {
                        int left = BTree[node].Left;

                        if (BTree[left].Balance == -1)
                            rotate_right(node);
                        else
                            rotate_left_right(node);

                        ret = true;
                    }
                    else if (BTree[node].Balance == 2)
                    {
                        int right = BTree[node].Right;

                        if (BTree[right].Balance == 1)
                            rotate_left(node);
                        else
                            rotate_right_left(node);

                        ret = true;
                    }

                    int parent = BTree[node].Parent;
                    if (parent >= 0)
                    {
                        // If not root node - update parent node balance
                        // If current node is LEFT: by -1; 
                        // If current node is RIGHT: by 1;
                        balance = (BTree[parent].Left == node) ? -1 : 1;
                    }
                    node = parent;
                }
            }
            /* 
            //>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
            for (int i = 0; i < MAX_USED; i++)
            {
                if (BTree[i].Used)
                {
                    int QQ = 0;
                    if ((BTree[i].Left < 0) & (BTree[i].Right < 0) & (BTree[i].Balance != 0))
                    {
                        QQ++;
                    }
                    QQ = 1;
                }
            }
            //>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
            */
        }

        /// <summary>
        /// Balance tree after deletion
        /// bal = +1 : LEFT node deleted; -1: RIGHT node deleted
        /// </summary>
        /// <param name="node"></param>
        /// <param name="balance"></param>
        private void balance_delete(int nodeId, int bal)
        {
            int balance = bal;
            int node = nodeId;
            bool ret = false;

            while ((node >= 0) & (!ret))
            {
                balance = (BTree[node].Balance += balance);             // Set new node balance

                if (BTree[node].Balance == -2)
                {
                    int left = BTree[node].Left;

                    if (BTree[left].Balance <= 0)
                    {
                        node = rotate_right(node);

                        if (BTree[node].Balance == 1)       // Node tree is right-heavy?
                            ret = true;
                    }
                    else
                        node = rotate_left_right(node);
                }
                else if (BTree[node].Balance == 2)
                {
                    int right = BTree[node].Right;

                    if (BTree[right].Balance >= 0)
                    {
                        node = rotate_left(node);
                        if (BTree[node].Balance == -1)      // Node is left-heavy?
                            ret = true;
                    }
                    else
                        node = rotate_right_left(node);
                }
                else if (balance != 0)
                    ret = true;

                int parent = BTree[node].Parent;

                if (parent >= 0)
                {
                    // If not root node - update parent node balance
                    // If current node is LEFT: by -1; 
                    // If current node is RIGHT: by 1;
                    balance = (BTree[parent].Left == node) ? 1 : -1;
                }
                node = parent;
            }

            /*
            //>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
            for (int i = 0; i < MAX_USED; i++)
            {
                if (BTree[i].Used)
                {
                    int QQ = 0;

                    //if ((BTree[i].Left < 0) & (BTree[i].Right < 0) & (BTree[i].Balance != 0))
                    if ((BTree[i].Left >= 0) & (BTree[i].Right >= 0))
                    {
                        int l = BTree[i].Left;
                        int r = BTree[i].Right;
                        if ((BTree[l].Left < 0) & (BTree[l].Right < 0) & (BTree[r].Left < 0) & (BTree[r].Right < 0) & (BTree[i].Balance != 0))
                        {
                            QQ++;
                        }
                    }
                    QQ = 1;
                }
            }
            //>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
            */

        }

        /// <summary>
        /// Add new value
        /// </summary>
        /// <param name="node"></param>
        /// <param name="value"></param>
        private bool add_ref(int nodeID, long value)
        {
            for (int i = 0; i < BTree[nodeID].Value.Count; i++)
                if (BTree[nodeID].Value[i] == value)
                    return false;

            BTree[nodeID].Value.Add(value);

            return true;
        }

        /// <summary>
        /// Extend array
        /// </summary>
        private void extend()
        {
            TreeItem[] old_BTree = BTree;
            BTree = new TreeItem[(int)(old_BTree.Length * 1.618)];  // + (int)(CURRENT_SIZE * 0.38)];
            for (int i = 0; i < old_BTree.Length; i++)
            {
                BTree[i].Key = old_BTree[i].Key;
                BTree[i].Value = old_BTree[i].Value;
                BTree[i].Balance = old_BTree[i].Balance;
                BTree[i].Parent = old_BTree[i].Parent;
                BTree[i].Left = old_BTree[i].Left;
                BTree[i].Right = old_BTree[i].Right;
            }

            old_BTree = null;

            CURRENT_SIZE = BTree.Length;
        }

        /// <summary>
        /// Rotate right if tree is left-heavy (balance= -2)
        /// 
        /// </summary>
        /// <param name="node"></param>
        private int rotate_right(int nodeId)
        {
            int left = BTree[nodeId].Left;
            int leftRight = BTree[left].Right;
            int parent = BTree[nodeId].Parent;

            BTree[left].Parent = parent;
            BTree[left].Right = nodeId;

            BTree[nodeId].Left = leftRight;
            BTree[nodeId].Parent = left;

            if (leftRight >= 0)
            {
                BTree[leftRight].Parent = nodeId;
            }

            if (nodeId == ROOT)
                ROOT = left;
            else if (BTree[parent].Left == nodeId)
                BTree[parent].Left = left;
            else
                BTree[parent].Right = left;

            BTree[left].Balance++;

            BTree[nodeId].Balance = -BTree[left].Balance;

            return left;
        }

        /// <summary>
        /// Rotate Left then Right
        /// </summary>
        /// <param name="node"></param>
        private int rotate_left_right(int node)
        {
            int left = BTree[node].Left;
            int leftRight = BTree[left].Right;
            int parent = BTree[node].Parent;
            int leftRightRight = BTree[leftRight].Right;
            int leftRightLeft = BTree[leftRight].Left;

            BTree[leftRight].Parent = parent;
            BTree[node].Left = leftRightRight;
            BTree[left].Right = leftRightLeft;
            BTree[leftRight].Left = left;
            BTree[leftRight].Right = node;
            BTree[left].Parent = leftRight;
            BTree[node].Parent = leftRight;

            if (leftRightRight >= 0)
            {
                BTree[leftRightRight].Parent = node;
            }

            if (leftRightLeft >= 0)
            {
                BTree[leftRightLeft].Parent = left;
            }

            if (node == ROOT)
            {
                ROOT = leftRight;
            }
            else if (BTree[parent].Left == node)
            {
                BTree[parent].Left = leftRight;
            }
            else
            {
                BTree[parent].Right = leftRight;
            }

            if (BTree[leftRight].Balance == 1)         //?????????????
            {
                BTree[node].Balance = 0;
                BTree[left].Balance = -1;
            }
            else if (BTree[leftRight].Balance == 0)
            {
                BTree[node].Balance = 0;
                BTree[left].Balance = 0;
            }
            else
            {
                BTree[node].Balance = 1;
                BTree[left].Balance = 0;
            }

            BTree[leftRight].Balance = 0;

            return leftRight;
        }


        /// <summary>
        /// Rotate left if tree is right-heavy (balance = +2)
        /// </summary>
        /// <param name="node"></param>
        private int rotate_left(int nodeId)
        {
            int right = BTree[nodeId].Right;
            int rightLeft = BTree[right].Left;
            int parent = BTree[nodeId].Parent;


            BTree[right].Parent = parent;
            BTree[right].Left = nodeId;

            BTree[nodeId].Right = rightLeft;
            BTree[nodeId].Parent = right;

            if (rightLeft >= 0)
            {
                BTree[rightLeft].Parent = nodeId;
            }

            if (nodeId == ROOT)
                ROOT = right;
            else if (BTree[parent].Right == nodeId)
                BTree[parent].Right = right;
            else
                BTree[parent].Left = right;


            BTree[right].Balance--;

            BTree[nodeId].Balance = -BTree[right].Balance;

            return right;
        }


        /// <summary>
        /// Rotate Right then Left
        /// </summary>
        /// <param name="node"></param>
        private int rotate_right_left(int node)
        {

            int right = BTree[node].Right;
            int rightLeft = BTree[right].Left;
            int parent = BTree[node].Parent;
            int rightLeftLeft = BTree[rightLeft].Left;
            int rightLeftRight = BTree[rightLeft].Right;

            BTree[rightLeft].Parent = parent;
            BTree[node].Right = rightLeftLeft;
            BTree[right].Left = rightLeftRight;
            BTree[rightLeft].Right = right;
            BTree[rightLeft].Left = node;
            BTree[right].Parent = rightLeft;
            BTree[node].Parent = rightLeft;

            if (rightLeftLeft >= 0)
            {
                BTree[rightLeftLeft].Parent = node;
            }

            if (rightLeftRight >= 0)
            {
                BTree[rightLeftRight].Parent = right;
            }

            if (node == ROOT)
            {
                ROOT = rightLeft;
            }
            else if (BTree[parent].Right == node)
            {
                BTree[parent].Right = rightLeft;
            }
            else
            {
                BTree[parent].Left = rightLeft;
            }

            if (BTree[rightLeft].Balance == -1)
            {
                BTree[node].Balance = 0;
                BTree[right].Balance = 1;
            }
            else if (BTree[rightLeft].Balance == 0)
            {
                BTree[node].Balance = 0;
                BTree[right].Balance = 0;
            }
            else
            {
                BTree[node].Balance = -1;
                BTree[right].Balance = 0;
            }

            BTree[rightLeft].Balance = 0;

            return rightLeft;
        }


        /// <summary>
        /// Delete key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        private bool delete(byte[] key, long value = -1)
        {
            //VSDebug.StopPoint(Name, "SIZE");

            int nodeID = ROOT;
            while (nodeID >= 0)
            {
                int compare = VSLib.CompareKeys(key, BTree[nodeID].Key);

                if (compare < 0)
                    nodeID = BTree[nodeID].Left;
                else if (compare > 0)
                    nodeID = BTree[nodeID].Right;
                else
                    return delete_avl_node(nodeID, value);
            }
            return false;
        }

        /// <summary>
        /// Delete avl node
        /// </summary>
        /// <param name="nodeID"></param>
        /// <param name="id"></param>
        private bool delete_avl_node(int nodeID, long value)
        {

            // If not-unique reference - delete ref
            if ((BTree[nodeID].Value.Count > 1) & (value >= 0))
            {
                return delete_ref(nodeID, value);
            }

            int left = BTree[nodeID].Left;
            int right = BTree[nodeID].Right;

            if (value >= 0)
                if (value != BTree[nodeID].Value[0])
                    return false;

            if (left < 0)                       // If no Left childs
            {
                if (right < 0)                  // If no Right childs (leaf)
                {
                    if (nodeID == ROOT)         // If not Root
                        ROOT = -1;
                    else
                    {
                        int parent = BTree[nodeID].Parent;

                        if (BTree[parent].Left == nodeID)       // Node is left of parent
                        {
                            BTree[parent].Left = -1;
                            balance_delete(parent, 1);
                        }
                        else                                    // Node is right of parent
                        {
                            BTree[parent].Right = -1;
                            balance_delete(parent, -1);
                        }
                    }
                }
                else
                    replace_node(nodeID, right);                // Only Right exists
            }
            else if (right < 0)
                replace_node(nodeID, left);                     // Only Left exists
            else
            {
                // BTree[nodeID].Left >= 0 (Left exists)

                int successor = right;                    // Both Left and Right exist 

                if (BTree[successor].Left < 0)
                {   // BTree[successor].Left < 0 (doesn't exist)
                    int parent = BTree[nodeID].Parent;

                    BTree[successor].Parent = parent;
                    BTree[successor].Left = left;
                    BTree[successor].Balance = BTree[nodeID].Balance;

                    if (left >= 0)
                        BTree[left].Parent = successor;

                    if (nodeID == ROOT)            //Is it root?
                        ROOT = successor;
                    else
                    {
                        if (BTree[parent].Left == nodeID)
                        {
                            BTree[parent].Left = successor;
                        }
                        else
                        {
                            BTree[parent].Right = successor;
                        }
                    }
                    balance_delete(successor, -1);
                }
                else
                {   // BTree[successor].Left >= 0 (exists)
                    while (BTree[successor].Left >= 0)                                                  // Find last successor Left
                        successor = BTree[successor].Left;

                    int parent = BTree[nodeID].Parent;
                    int successorParent = BTree[successor].Parent;
                    int successorRight = BTree[successor].Right;

                    if (BTree[successorParent].Left == successor)
                    {
                        BTree[successorParent].Left = successorRight;
                    }
                    else
                    {
                        BTree[successorParent].Right = successorRight;
                    }

                    if (successorRight >= 0)
                    {
                        BTree[successorRight].Parent = successorParent;
                    }

                    BTree[successor].Parent = parent;
                    BTree[successor].Left = left;
                    BTree[successor].Right = right;
                    BTree[successor].Balance = BTree[nodeID].Balance;

                    // Replace parent for node left and right to successor
                    BTree[right].Parent = successor;

                    if (left >= 0)
                        BTree[left].Parent = successor;


                    if (nodeID == ROOT)                //Root?
                        ROOT = successor;
                    else
                    {
                        if (BTree[parent].Left == nodeID)
                        {
                            BTree[parent].Left = successor;
                        }
                        else
                        {
                            BTree[parent].Right = successor;
                        }
                    }
                    balance_delete(successorParent, 1);
                }
            }
            delete_node(nodeID);

            return true;
        }


        /// <summary>
        /// Add key (bytes)
        /// </summary>
        /// <param name="Value"></param>
        /// <returns></returns>
        private bool insert(byte[] key, long value)
        {

            if (CURRENT_USED == CURRENT_SIZE)           // Extend if full
                this.extend();

            bool rc = false;

            if (ROOT < 0)                                                       // Empty tree
            {
                ROOT = create_node(key, value, -1);
                rc = true;
            }
            else
            {
                int nodeID = ROOT;

                while (nodeID >= 0)
                {

                    int compare = VSLib.CompareKeys(key, BTree[nodeID].Key);

                    if (compare < 0)
                    {
                        if (BTree[nodeID].Left < 0)
                        {
                            // Create left node
                            BTree[nodeID].Left = create_node(key, value, nodeID);         // Add to array
                            balance_insert(nodeID, -1);
                            return true;
                        }
                        else
                            nodeID = BTree[nodeID].Left;
                    }
                    else if (compare > 0)
                    {
                        if (BTree[nodeID].Right < 0)
                        {
                            // Create right node
                            BTree[nodeID].Right = create_node(key, value, nodeID);
                            balance_insert(nodeID, 1);
                            rc = true;
                            break;
                        }
                        else
                            nodeID = BTree[nodeID].Right;
                    }
                    else
                    { // Index value already exists
                        if (this.UNIQUE)
                            break;
                        else
                        {
                            if (add_ref(nodeID, value))          // Add reference for non-unique value
                                rc = true;
                            break;
                        }
                    }
                }
            }
            return rc;
        }

        //////////////////////////////////////////////
        //////////// ENUMERATOR //////////////////////
        //////////////////////////////////////////////

        private int _current;
        private int _right;
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

                    while (BTree[_current].Left >= 0)
                    {
                        _current = BTree[_current].Left;
                    }

                    _action = (BTree[_current].Right >= 0) ? Action.Right : Action.Parent;

                    if (_action == Action.Right)
                        _right = BTree[_current].Right;

                    return true;

                case Action.Parent:
                    while (BTree[_current].Parent >= 0)
                    {
                        int previous = _current;

                        _current = BTree[_current].Parent;

                        if (BTree[_current].Left == previous)
                        {
                            _action = (BTree[_current].Right >= 0) ? Action.Right : Action.Parent;

                            if (_action == Action.Right)
                                _right = BTree[_current].Right;

                            return true;
                        }
                    }

                    _action = Action.End;

                    return false;

                case Action.Current:
                    _action = (BTree[_current].Right >= 0) ? Action.Right : Action.Parent;

                    if (BTree[_current].Right >= 0)
                        _right = BTree[_current].Right;

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
            _current = -1;

            if (ROOT >= 0)
            {
                _right = _current = ROOT;
                _action = Action.Right;
            }
            else
                _action = Action.End;
        }

        /// <summary>
        /// Reset by key
        /// </summary>
        /// <param name="key"></param>
        public void Reset(long key, bool partial = false)
        {
            _current = -1;

            if (ROOT >= 0)
            {
                int id = find(VSLib.ConvertLongToByteReverse(key), (partial? 1 : 0));
                if (id >= 0)
                {
                    _action = Action.Current;
                    _current =_right = id;
                }
                else
                    _action = Action.End;
            }
            else
                _action = Action.End;
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
                    return BTree[_current].Value.ToArray(); 
            }
        }

        /// <summary>
        /// All keys of this index
        /// </summary>
        public long[] Keys
        {
            get
            {
                List<long> k = new List<long>(1024);
                this.Reset();
                while (this.Next())
                    k.Add(CurrentKey);
                return k.ToArray();
            }
        }

        /// <summary>
        /// Current key (bytes)
        /// </summary>
        public long CurrentKey
        {
            get
            {
                if (_action == Action.End)
                    return -1;
                else
                    return VSLib.ConvertByteToLongReverse(BTree[_current].Key);
            }
        }
    }
}
