using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VStorage
{
    class VSFreeSpaceManager
    {
        // History
        // 4/25/17 - Redesign for optimization: keep values in memory for read
        // 4/26/17 - new approach for FBQE access - memory buffer

        /// <summary>
        /// Free Space Allocation 
        /// </summary>

        /////////////////////////////////////////////////////////////////////////
        /////////////////////////////// FBQE ////////////////////////////////////
        /////////////////////////////////////////////////////////////////////////

        internal struct FBQE
        {
            public string SG;
            public long ADDRESS_START;
            public long ADDRESS_END;
            public long LENGTH;
            public int PREV;
            public int NEXT;
            public int index;
            public int address;
        }

        private const int FBQE_SG_POS = 0;
        private const int FBQE_SG_LEN = 4;

        private const int ADDRESS_START_POS = FBQE_SG_POS + FBQE_SG_LEN;
        private const int ADDRESS_START_LEN = 8;

        private const int ADDRESS_END_POS = ADDRESS_START_POS + ADDRESS_START_LEN; //12
        private const int ADDRESS_END_LEN = 8;

        private const int LENGTH_POS = ADDRESS_END_POS + ADDRESS_END_LEN; //20
        private const int LENGTH_LEN = 8;

        private const int PREV_POS = LENGTH_POS + LENGTH_LEN; //28
        private const int PREV_LEN = 4;

        private const int NEXT_POS = PREV_POS + PREV_LEN; //32
        private const int NEXT_LEN = 4;

        internal const int FBQE_LENGTH = (int)(NEXT_POS + NEXT_LEN);                                              // Element length

        /////////////////////////// END FBQE ////////////////////////////////////////



        private byte[] buffer = null;                            // F-block buffer
        private bool fbqe_is_expanding = false;                 // lock to prevent recurrent FBQE extension (use in insertFBQE method)

        private VSpace Space;
        private VSAllocation H_Object = null;                   // Header object
        private VSAllocation F_Object = null;                   // Fbque block object

        private VSVirtualMemoryManager vm = null;

        public VSFreeSpaceManager(VSpace _sp)
        {
            Space = _sp;
            vm = Space.VM;
            long hdr_address = Space.GetFirstAddress(DEFS.POOL_FREE_SPACE);
            if (hdr_address == 0)
            { // Initialization

                // 1. Create H-block (header)
                H_Object = new VSAllocation(vm, DEFS.FBQE_HEADER_ADDRESS, DEFS.FBQE_HEADER_LENGTH_FULL, 0);
                H_Object.Write(0, DEFS.FBQE_HEADER_SIGNATURE);
                this.MAX = DEFS.FBQE_ALLOCATION_NUMBER;                                            // Max number of FBQE
                this.FREE = MAX;
                this.FIRST = -1;
                this.LAST = -1;
                this.FIRST_Q = 0;
                this.LAST_Q = FREE - 1;

                // 2. Save H-block address (1st object)
                Space.SetFirstAddress(DEFS.POOL_FREE_SPACE, H_Object.DescriptorAddress);      // 1st object

                // 3. Create 1st FBQE block
                F_Object = new VSAllocation(vm, H_Object.DescriptorAddress + H_Object.FullLength, (DEFS.FBQE_ALLOCATION_LENGTH + VSAllocation.DescriptorLength), 0);

                buffer = new byte[F_Object.Length];                   // Create buffer

                // 4. Set references
                F_Object.PREV = H_Object.DescriptorAddress;
                H_Object.NEXT = F_Object.DescriptorAddress;                                     // Address of the 1st block F-block


                // 5. Save F-block address (last object)
                Space.SetLastAddress(DEFS.POOL_FREE_SPACE, F_Object.DescriptorAddress);       //last object

                // 1.3 Initiate Free queue
                for (int i = 0; i < FREE; i++)
                    CreateFBQE(i);

                // 1.4 Create initial FBQE
                long fa = F_Object.DescriptorAddress + F_Object.FullLength;                    //1st free address
                AddFBQE(fa, (Space.Size - fa), -1, -1);                             //Create 1st FBQE
            }
            else
            {
                H_Object = Space.GetAllocationByDescriptor(hdr_address);
                F_Object = Space.GetAllocationByDescriptor(H_Object.NEXT);

                byte[] b = H_Object.ReadBytes(0, FBQE_HEADER_LENGTH);

                
                this.v_MAX = VSLib.ConvertByteToInt(VSLib.GetByteArray(b, MAX_POS, MAX_LEN));
                this.v_FREE = VSLib.ConvertByteToInt(VSLib.GetByteArray(b, FREE_POS, FREE_LEN));
                this.v_FIRST = VSLib.ConvertByteToInt(VSLib.GetByteArray(b, FIRST_POS, FIRST_LEN));
                this.v_LAST = VSLib.ConvertByteToInt(VSLib.GetByteArray(b, LAST_POS, LAST_LEN));
                this.v_FIRST_Q = VSLib.ConvertByteToInt(VSLib.GetByteArray(b, FIRST_Q_POS, FIRST_Q_LEN));
                this.v_LAST_Q = VSLib.ConvertByteToInt(VSLib.GetByteArray(b, LAST_Q_POS, LAST_Q_LEN));

                buffer = F_Object.ReadBytes(0, F_Object.Length);                   // Read buffer
            }
        }

        /// <summary>
        /// Expand F-block if required
        /// </summary>
        internal void CheckFreeQueueSize()
        {
            if (!fbqe_is_expanding)
            {
                if (this.FREE < DEFS.FBQE_MIN_FREE_NUMBER)
                {
                    fbqe_is_expanding = true;
                    Space.ExtendSpace(F_Object, DEFS.FBQE_ALLOCATION_LENGTH);

                    FBQE fp = this.GetFBQE(this.LAST_Q);
                    fp.NEXT = this.MAX;                          // 1st FBQE in the last block
                    int old_maxn = this.MAX;                     // Max (before)
                    this.MAX += DEFS.FBQE_ALLOCATION_NUMBER;     // Max(after)
                    this.FREE += DEFS.FBQE_ALLOCATION_NUMBER;    // Free(after)
                    this.LAST_Q = this.MAX - 1;                  // Last in free queue (after)

                    for (int i = 0; i < DEFS.FBQE_ALLOCATION_NUMBER; i++)
                        this.CreateFBQE(old_maxn + i);

                    fbqe_is_expanding = false;
                }
            }
        }



        private const int SG_POS = 0;
        private const int SG_LEN = 4;

        /// <summary>
        /// Max number of elements in the block
        /// </summary>
        private const int MAX_POS = SG_POS + SG_LEN;
        private const int MAX_LEN = 4;
        public int MAX              
        {
            get { return v_MAX; }
            set { v_MAX = value; H_Object.Write(MAX_POS, v_MAX); }
        }
        private int v_MAX = 0;

        /// <summary>
        /// Number of the free queue
        /// </summary>
        private const int FREE_POS = MAX_POS + MAX_LEN; 
        private const int FREE_LEN = 4;
        public int FREE
        {
            get { return v_FREE; }
            set { v_FREE = value; H_Object.Write(FREE_POS, v_FREE); }
        }
        private int v_FREE = 0;

        /// <summary>
        /// 1st element in the queue
        /// </summary>
        private const int FIRST_POS = FREE_POS + FREE_LEN; 
        private const int FIRST_LEN = 4;
        public int FIRST                                          
        {
            get { return v_FIRST; }
            set { v_FIRST = value; H_Object.Write(FIRST_POS, v_FIRST); }
        }
        private int v_FIRST = 0;

        /// <summary>
        /// Last element in the queue
        /// </summary>
        private const int LAST_POS = FIRST_POS + FIRST_LEN;
        private const int LAST_LEN = 4;
        public int LAST                               
        {
            get { return v_LAST; }
            set { v_LAST = value; H_Object.Write(LAST_POS, v_LAST); }
        }
        private int v_LAST = 0;

        /// <summary>
        /// 1st free queue element
        /// </summary>
        private const int FIRST_Q_POS = LAST_POS + LAST_LEN;
        private const int FIRST_Q_LEN = 4;
        public int FIRST_Q                              
        {
            get { return v_FIRST_Q; }
            set { v_FIRST_Q = value; H_Object.Write(FIRST_Q_POS, v_FIRST_Q); }
        }
        private int v_FIRST_Q = 0;

        /// <summary>
        /// Last free queue element
        /// </summary>
        private const int LAST_Q_POS = FIRST_Q_POS + FIRST_Q_LEN;
        private const int LAST_Q_LEN = 4;
        public int LAST_Q               
        {
            get { return v_LAST_Q; }
            set { v_LAST_Q = value; H_Object.Write(LAST_Q_POS, v_LAST_Q); }
        }
        private int v_LAST_Q = 0;

        /// <summary>
        /// Header length
        /// </summary>
        internal const long FBQE_HEADER_LENGTH = LAST_Q_POS + LAST_Q_LEN;


        ///////////////////////////////////////////////////
        ///////////////////// METHODS /////////////////////
        ///////////////////////////////////////////////////
        /// <summary>
        /// Get by index
        /// </summary>
        /// <param name="idx"></param>
        /// <returns></returns>
        public FBQE GetFBQE(int idx)
        {
            FBQE f = new FBQE();
            f.index = idx;
            f.address = idx * FBQE_LENGTH;

            f.SG = VSLib.ConvertByteToString(VSLib.GetByteArray(buffer, f.address + SG_POS, SG_LEN));
            f.ADDRESS_START = VSLib.ConvertByteToLong(VSLib.GetByteArray(buffer, f.address + ADDRESS_START_POS, ADDRESS_START_LEN));
            f.ADDRESS_END = VSLib.ConvertByteToLong(VSLib.GetByteArray(buffer, f.address + ADDRESS_END_POS, ADDRESS_END_LEN));
            f.LENGTH = VSLib.ConvertByteToLong(VSLib.GetByteArray(buffer, f.address + LENGTH_POS, LENGTH_LEN));
            f.PREV = VSLib.ConvertByteToInt(VSLib.GetByteArray(buffer, f.address + PREV_POS, PREV_LEN));
            f.NEXT = VSLib.ConvertByteToInt(VSLib.GetByteArray(buffer, f.address + NEXT_POS, NEXT_LEN));

            if (f.SG != DEFS.FBQE_SIGNATURE)
                throw new VSException(VSException.E0006_INVALID_SIGNATURE_CODE, "(FBQE)");

            return f;
        }

        /// <summary>
        /// Create FBQE by index
        /// </summary>
        /// <param name="idx"></param>
        /// <returns></returns>
        internal FBQE CreateFBQE(int idx)
        {
            FBQE f = new FBQE();
            f.SG = DEFS.FBQE_SIGNATURE;
            f.index = idx;
            f.address = idx * FBQE_LENGTH;
            f.PREV = idx - 1;
            f.NEXT = (idx < this.LAST_Q) ? idx + 1 : -1;
            f.ADDRESS_START = 0;
            f.ADDRESS_END = 0;
            f.LENGTH = 0;
            SerializeFBQE(f);
            return f;
        }

        /// <summary>
        /// Write FBQE
        /// </summary>
        /// <param name="f"></param>
        private void SerializeFBQE(FBQE f)
        {
            VSLib.CopyBytes(buffer, VSLib.ConvertStringToByte(DEFS.FBQE_SIGNATURE), f.address + SG_POS, SG_LEN);
            VSLib.CopyBytes(buffer, VSLib.ConvertLongToByte(f.ADDRESS_START), f.address + ADDRESS_START_POS, ADDRESS_START_LEN);
            VSLib.CopyBytes(buffer, VSLib.ConvertLongToByte(f.ADDRESS_END), f.address + ADDRESS_END_POS, ADDRESS_END_LEN);
            VSLib.CopyBytes(buffer, VSLib.ConvertLongToByte(f.LENGTH), f.address + LENGTH_POS, LENGTH_LEN);
            VSLib.CopyBytes(buffer, VSLib.ConvertIntToByte(f.PREV), f.address + PREV_POS, PREV_LEN);
            VSLib.CopyBytes(buffer, VSLib.ConvertIntToByte(f.NEXT), f.address + NEXT_POS, NEXT_LEN);

            F_Object.Write(f.address, VSLib.GetByteArray(buffer, f.address, FBQE_LENGTH), FBQE_LENGTH);
        }

        /// <summary>
        /// Add new FBQE. Returns FBQE index
        /// </summary>
        /// <param name="address"></param>
        /// <param name="length"></param>
        /// <param name="left"></param>
        /// <param name="right"></param>
        public int AddFBQE(long address, long length, int left, int right)
        {
            FBQE f, fu;

            // Get free element
            int n = FIRST_Q;
            f = GetFBQE(n);

            // Update free queue
            FIRST_Q = f.NEXT;
            FBQE f1 = GetFBQE(f.NEXT);
            f1.PREV = -1;
            SerializeFBQE(f1);

            // Update FBQE
            f.SG = DEFS.FBQE_SIGNATURE;
            f.ADDRESS_START = address;
            f.ADDRESS_END = address + length;
            f.LENGTH = length;
            f.PREV = left;
            f.NEXT = right;
            f.index = n;
            SerializeFBQE(f);

            // Update chain
            if (left < 0)
                FIRST = n;
            else
            {
                fu = GetFBQE(left);
                fu.NEXT = n;
                SerializeFBQE(fu);
            }

            if (right < 0)
                LAST = n;
            else
            {
                fu = GetFBQE(right);
                fu.PREV = n;
                SerializeFBQE(fu);
            }

            FREE -= 1;
            return n;
        }
        /// <summary>
        /// Update 
        /// </summary>
        /// <param name="index"></param>
        /// <param name="address"></param>
        /// <param name="length"></param>
        public int UpdateFBQE(int index, long address, long length)
        {
            FBQE f = GetFBQE(index);
            f.ADDRESS_START = address;
            f.LENGTH = length;
            f.ADDRESS_END = address + length;
            SerializeFBQE(f);
            return index;
        }

        /// <summary>
        /// Delete FBQE
        /// </summary>
        /// <param name="index"></param>
        public int DeleteFBQE(int index)
        {
            FBQE f = GetFBQE(index);

            // Update chain
            if (f.NEXT >= 0)
            {
                FBQE fn = GetFBQE(f.NEXT);
                fn.PREV = f.PREV;
                SerializeFBQE(fn);
            }
            else
                LAST = f.PREV;

            if (f.PREV >= 0)
            {
                FBQE fp = GetFBQE(f.PREV);
                fp.NEXT = f.NEXT;
                SerializeFBQE(fp);
            }
            else 
                FIRST = f.NEXT;

            // Add FBQE to the free queue
            f.ADDRESS_START = 0;
            f.ADDRESS_END = 0;
            f.LENGTH = 0;
            f.NEXT = -1;
            f.PREV = LAST_Q;
            SerializeFBQE(f);

            FBQE f1 = GetFBQE(f.PREV);
            f1.NEXT = index;
            SerializeFBQE(f1);

            LAST_Q = index;
            FREE += 1;

            return index;
        }
    }

}
