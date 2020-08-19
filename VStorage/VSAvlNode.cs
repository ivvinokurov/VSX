using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VStorage
{
    /// <summary>
    /// AVL Node
    /// </summary>
    /// 

    // Use of Allocation UDF fields
    // UDF1 - Parent
    // UDF2 - Index
    // UDF3 - Left
    // UDF4 - Rigth
    //

    public class VSAvlNode
    {
        /// <summary>
        /// Default "empty" constructor
        /// </summary>

        private VSAllocation ALLOCATION = null;

        VSAvlNode()
        {
        }
        public VSAvlNode(VSIndex x)
        {
            ix = x;
            sp = x.sp;
        }


        private VSpace sp;
        private VSIndex ix;

        ////////////////////////////////////////////////////////////////////////////
        /////////////////// Internal (ADSC mapped) fields //////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This node ID
        /// </summary>
        public long ID
        {
            get { return ALLOCATION.Id; }
        }


        ////////////////////////////////////////////////////////////////////////////
        /////////////////// Fixed length fields ////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Signature (4 bytes)
        /// </summary>
        private const long SG_POS = 0;
        private const long SG_LEN = 4;
        public string SG
        {
            get { return ALLOCATION.ReadString(SG_POS, SG_LEN); }
            set { ALLOCATION.Write(SG_POS, value); }
        }



        /// <summary>
        /// Parent
        /// </summary>
        private const long PARENT_POS = SG_POS + SG_LEN;
        private const long PARENT_LEN = 8;
        public long PARENT
        {
            get { return ALLOCATION.ReadLong(PARENT_POS); }
            set { ALLOCATION.Write(PARENT_POS, value); }
        }

        /// <summary>
        /// Index ID
        /// </summary>
        internal const long INDEX_POS = PARENT_POS + PARENT_LEN;
        private const long INDEX_LEN = 8;
        internal long INDEX
        {
            get { return ALLOCATION.ReadLong(INDEX_POS); }
            set { ALLOCATION.Write(INDEX_POS, value); }
        }

        /// <summary>
        /// Left
        /// </summary>
        private const long LEFT_POS = INDEX_POS + INDEX_LEN;
        private const long LEFT_LEN = 8;
        public long LEFT
        {
            get { return ALLOCATION.ReadLong(LEFT_POS); }
            set { ALLOCATION.Write(LEFT_POS, value); }
        }

        /// <summary>
        /// Right
        /// </summary>
        private const long RIGHT_POS = LEFT_POS + LEFT_LEN;
        private const long RIGHT_LEN = 8;
        public long RIGHT
        {
            get { return ALLOCATION.ReadLong(RIGHT_POS); }
            set { ALLOCATION.Write(RIGHT_POS, value); }
        }

        /// <summary>
        /// Balance
        /// </summary>
        private const long BALANCE_POS = RIGHT_POS + RIGHT_LEN;
        private const long BALANCE_LEN = 4;
        public int BALANCE
        {
            get { return ALLOCATION.ReadInt(BALANCE_POS); }
            set { ALLOCATION.Write(BALANCE_POS, value); }
        }





        /// <summary>
        /// Number of references
        /// </summary>
        private const long REF_COUNT_POS = BALANCE_POS + BALANCE_LEN;    
        private const long REF_COUNT_LEN = 4;
        public int REF_COUNT
        {
            get { return ALLOCATION.ReadInt(REF_COUNT_POS); }
            set { ALLOCATION.Write(REF_COUNT_POS, value); }
        }

        /// <summary>
        /// Key length
        /// </summary>
        private const long KEYLEN_POS = REF_COUNT_POS + REF_COUNT_LEN;   
        private const long KEYLEN_LEN = 4;
        public int KEYLEN
        {
            get { return ALLOCATION.ReadInt(KEYLEN_POS); }
            set { ALLOCATION.Write(KEYLEN_POS, value); }
        }

        private const long VARIABLE_POS = KEYLEN_POS + KEYLEN_LEN;

        ///////////////////////////////////////////////////////////////////
        ///////////  VARIABLE PART    /////////////////////////////////////
        ///////////////////////////////////////////////////////////////////


        /// <summary>
        /// Key value
        /// </summary>
        private const long KEY_POS = VARIABLE_POS;            
        public byte[] KEY
        {
            get { return ALLOCATION.ReadBytes(KEY_POS, (long)KEYLEN); }
        }


        /// <summary>
        /// 1st reference
        /// </summary>
        public long REF
        {
            get { return ALLOCATION.ReadLong(REF_POS); }
            set { ALLOCATION.Write(REF_POS, value); }
        }

        /// <summary>
        /// Return all refs
        /// </summary>
        public long[] REFS
        {
            get 
            {
                byte[] b = ALLOCATION.ReadBytes(REF_POS, (long)(REF_COUNT * 8));
                long cnt = REF_COUNT;
                long[] refs = new long[cnt];

                for (int i = 0; i < cnt; i++)
                    refs[i] = VSLib.ConvertByteToLong(VSLib.GetByteArray(b, (i*8), 8));

                return refs;
            }
        }

        ///////////////////////////////////////////////////////////////////
        ///////////  CALULATED FIELDS /////////////////////////////////////
        ///////////////////////////////////////////////////////////////////

        /// <summary>
        /// Refs position
        /// </summary>
        private long REF_POS
        {
            get { return (long)(VARIABLE_POS + KEYLEN); }
        }

        /// <summary>
        /// Node size
        /// </summary>
        private long AVLNODE_SIZE
        {
            get { return (long)(VARIABLE_POS + KEYLEN + (REF_COUNT * 8)); }
        }

        /// <summary>
        /// Unique property
        /// </summary>
        public bool UNIQUE
        {
            get {return ix.UniqueIndex;}
        }

        public string KEY_STRING
        {
            get { return VSLib.ConvertByteToString(KEY); }
        }

        ///////////////////////////////////////////////////////////////////
        ///////////  METHODS //////   /////////////////////////////////////
        ///////////////////////////////////////////////////////////////////
        /// <summary>
        /// Read node
        /// </summary>
        /// <param name="id"></param>
        public void Read(long id)
        {
            //VSDebug.StopPoint(id, 1323);
            ALLOCATION = sp.GetAllocation(id);
            
            string s = ALLOCATION.ReadString(0, 4);
            if (s != DEFS.AVL_SIGNATURE)
                throw new VSException(DEFS.E0006_INVALID_SIGNATURE_CODE, "- VSAVL, at address 0x" + ALLOCATION.Address.ToString("X"));
        }

        /// <summary>
        /// Create new node
        /// </summary>
        /// <returns>id</returns>
        public long Create(byte[] key, long value, long parent, short pool)
        {
            long base_size = VARIABLE_POS + key.Length + 8;

            long alloc_size = (ix.UniqueIndex) ? base_size : DEFS.MIN_SPACE_ALLOCATION_CHUNK * 2;

            this.ALLOCATION = sp.AllocateSpace(alloc_size, pool);

            this.SG = DEFS.AVL_SIGNATURE;

            this.REF_COUNT = 1;

            this.KEYLEN = key.Length;
            
            this.ALLOCATION.Write(KEY_POS, key, key.Length);

            this.PARENT = parent;

            this.INDEX = ix.Id;                                          // Index Id

            this.REF = value;              // 1st ref value

            return ID;
        }

        /// <summary>
        /// Delete this node and free space
        /// </summary>
        public bool Delete()
        {
            //VSDebug.StopPoint(this.ID, 41);
            bool root_deleted = ((PARENT == 0) & (LEFT == 0) & (RIGHT == 0));
            sp.Free(ALLOCATION);
            ALLOCATION = null;
            return root_deleted;
        }

        /// <summary>
        /// Add reference
        /// </summary>
        /// <param name="rf"></param>
        /// <returns></returns>
        internal bool add_ref(long rf)
        {
            long[] refs = REFS;

            for (int i = 0; i < refs.Length; i++)
                if (refs[i] == rf)
                    return true;

            if ((ALLOCATION.Size - AVLNODE_SIZE) < 8)
            {
                long ext_size = this.UNIQUE ? DEFS.MIN_SPACE_ALLOCATION_CHUNK : (DEFS.MIN_SPACE_ALLOCATION_CHUNK * 3);
                sp.ExtendSpace(ALLOCATION, ext_size);
            }

            ALLOCATION.Write(REF_POS + (REF_COUNT * 8), rf);

            REF_COUNT++;

            return true;
        }

        /// <summary>
        /// Delete reference
        /// </summary>
        /// <param name="rf"></param>
        /// <returns></returns>
        internal bool delete_ref(long rf)
        {
            int x = -1;
            List<long> refs = REFS.ToList<long>();

            int count = refs.Count;

            for (int i = 0; i < count; i++)
            {
                int i2 = count - 1 - i;
                if (i > i2)
                    break;

                if (refs[i] == rf)
                    x = i;
                else if (refs[i2] == rf)
                    x = i2;

                if (x > 0)
                    break;
            }

            if (x < 0)
                return false;
            else
                refs.RemoveAt(x);

            byte[] refs_new = new byte[refs.Count * 8];

            for (int i = 0; i < refs.Count; i++)
                VSLib.CopyBytes(refs_new, VSLib.ConvertLongToByte(refs[i]), (i * 8), 8);

            ALLOCATION.Write(this.REF_POS, refs_new, refs_new.Length);        // Write 

            REF_COUNT = refs.Count;

            return true;
        }
    }
}
