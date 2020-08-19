using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace VStorage
{
    public class VSKeyManager
    {
        // Update history
        // 4/30/17 - full redesignlast V-Key2 always long.MaxValue


        /// <summary>
        /// Key Management Header  (KMH)
        /// </summary>
        private struct KeyHeaderDef
        {
            public long DescriptorAddress;              // +00(8)  Descriptor address
            public const long A_DescriptorAddress = 0;
            public int DescriptorLength;                // +08(4)  Current descriptor # of items (max)
            public const long A_DescriptorLength = 8;
            public int DescriptorLast;                  // +12(4)  Last used descriptor item.
            public const long A_DescriptorLast = 16;
            public int DescriptorUsed;                  // +16(4)  Current descriptor # of used items (including 'deleted'). Compact when 25% deleted.
            public const long A_DescriptorUsed = 24;
            public long LastKey;                        // +20(8)  Last generated key
            public const long A_LastKey = 32;
            //private byte[] reserve;                     // +28(36) Reserved

            public const long KEYROOT_SIZE = 64;
        }
        private KeyHeaderDef KeyHeader;
        private VSAllocation KeyHeaderAlloc;                              // Header allocation


        /// <summary>
        /// Key Management Descriptor
        /// </summary>
        private struct KeyDescriptorDef
        {
            public long Address;                                   // Block addess
            public const long A_Address = 0;
            public long FirstKey;                                   // First key in the block
            public const long A_FirstKey = 8;
            public int  Used;                                       // # of used keys (free block when becomes 0)
            public const long A_Used = 16;

            public const int DESCRIPTOR_ITEM_LENGTH = 32;          // The length of descriptor item
            public const int DESCRIPTOR_CHUNK_LENGTH = 256;        // Initial # of entries allocatod for descriptos
            public const long DESCRIPTOR_CHUNK_SIZE = DESCRIPTOR_ITEM_LENGTH * DESCRIPTOR_CHUNK_LENGTH;

            public const int DESCRIPTOR_THRESHOLD = DESCRIPTOR_CHUNK_LENGTH / 2;  // Min value of the DescriptorLast when reorganization condition could check

        }
        private KeyDescriptorDef[] KeyDescriptor;
        private VSAllocation KeyDescriptorAlloc;                              // Header allocation

        /// <summary>
        /// Key Management Block (KMBI)
        /// </summary>
        private struct KeyBlockDef
        {
            public long Address;                                // Allocation descriptor address: >0 - active key; 0 - not initialized key; -1 - deleted key.
            public const long A_Address = 0;

            public const long BLOCK_ITEM_LENGTH = 8;            // The length of descriptor item
            public const long BLOCK_LENGTH = 2048;              // Initial # of entries allocatod for descriptos
            public const long BLOCK_SIZE = BLOCK_ITEM_LENGTH * BLOCK_LENGTH;

            public const long KEY_NOT_CREATED = 0;
            public const long KEY_DELETED = -1;
        }
        //private KeyBlockDef KeyBlock;

        private VSAllocation KeyBlockAllocWrite;                // Block allocation for write

        private VSAllocation KeyBlockAllocRead;                 // Block allocation for read
        private int KeyBlockReadIndex = -1;                     // Index of ther read-block




        /// <summary>
        /// Private fields
        /// </summary>
        private VSpace sp;                                      // Space object


        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="_s"></param>
        public VSKeyManager(VSpace _s)
        {
            sp = _s;
            Initialize();
        }

        /// <summary>
        /// Initialize data structures
        /// </summary>
        private void Initialize()
        {
            KeyHeader = new KeyHeaderDef();
            
            // Get root allocation
            KeyHeaderAlloc = sp.GetRootAllocation(DEFS.POOL_KEY);

            // If ROOT doesn't exist yet - initial allocation
            if (KeyHeaderAlloc == null)
            {
                // Allocate header space
                KeyHeaderAlloc = sp.AllocateSpace(KeyHeaderDef.KEYROOT_SIZE, DEFS.POOL_KEY, generateID: false);
                KeyHeaderAlloc.Write(KeyHeaderDef.A_DescriptorLast, -1);

                // Allocate descriptor space
                KeyDescriptorAlloc = sp.AllocateSpace(KeyDescriptorDef.DESCRIPTOR_CHUNK_SIZE, DEFS.POOL_KEY, generateID: false);

                //Set initial values to memory object
                KeyHeader.DescriptorAddress = KeyDescriptorAlloc.DescriptorAddress;
                KeyHeader.DescriptorLength = KeyDescriptorDef.DESCRIPTOR_CHUNK_LENGTH;
                KeyHeader.DescriptorLast = -1;
                KeyHeader.DescriptorUsed = 0;
                KeyHeader.LastKey = 0;

                // Save initial values
                KeyHeaderAlloc.Write(KeyHeaderDef.A_DescriptorAddress, KeyHeader.DescriptorAddress);
                KeyHeaderAlloc.Write(KeyHeaderDef.A_DescriptorLength, KeyHeader.DescriptorLength);
                KeyHeaderAlloc.Write(KeyHeaderDef.A_DescriptorLast, KeyHeader.DescriptorLast);
                KeyHeaderAlloc.Write(KeyHeaderDef.A_DescriptorUsed, KeyHeader.DescriptorUsed);
                KeyHeaderAlloc.Write(KeyHeaderDef.A_LastKey, KeyHeader.LastKey);
                
                // Create descriptor
                KeyDescriptor = new KeyDescriptorDef[KeyHeader.DescriptorLength];
            }
            else
            {
                // Read header
                KeyHeader.DescriptorAddress = KeyHeaderAlloc.ReadLong(KeyHeaderDef.A_DescriptorAddress);
                KeyHeader.DescriptorLength = KeyHeaderAlloc.ReadInt(KeyHeaderDef.A_DescriptorLength);
                KeyHeader.DescriptorLast = KeyHeaderAlloc.ReadInt(KeyHeaderDef.A_DescriptorLast);
                KeyHeader.DescriptorUsed = KeyHeaderAlloc.ReadInt(KeyHeaderDef.A_DescriptorUsed);
                KeyHeader.LastKey = KeyHeaderAlloc.ReadLong(KeyHeaderDef.A_LastKey);


                // Read descriptor
                KeyDescriptor = new KeyDescriptorDef[KeyHeader.DescriptorLength];
                KeyDescriptorAlloc = sp.GetAllocationByDescriptor(KeyHeader.DescriptorAddress);

                for (int i = 0; i < KeyHeader.DescriptorLength; i++)
                {
                    KeyDescriptor[i].Address = KeyDescriptorAlloc.ReadLong((KeyDescriptorDef.DESCRIPTOR_ITEM_LENGTH * i) + KeyDescriptorDef.A_Address);
                    KeyDescriptor[i].FirstKey = KeyDescriptorAlloc.ReadLong((KeyDescriptorDef.DESCRIPTOR_ITEM_LENGTH * i) + KeyDescriptorDef.A_FirstKey);
                    KeyDescriptor[i].Used = KeyDescriptorAlloc.ReadInt((KeyDescriptorDef.DESCRIPTOR_ITEM_LENGTH * i) + KeyDescriptorDef.A_Used);
                }
            }
        }

        /// <summary>
        /// Create new KEY for address
        /// If use_key > 0 then add specified key (used by restore)
        /// If alloc=null - add 0 address (restore)
        /// </summary>
        /// <returns>new SID</returns>
        public long Add(VSAllocation alloc, long use_key = 0)
        {
            //VSDebug.StopPoint(alloc.Address, 45724);
            long address = (alloc == null) ? 0 : alloc.DescriptorAddress;
            long addr = 0;

            // 1. Generate new key 
            if (use_key > 0)
            {
                if (use_key <= KeyHeader.LastKey)
                    throw new VSException(DEFS.E0023_INVALID_KEY_SEQUENCE_CODE, " Lask key: " + KeyHeader.LastKey.ToString() + " Use key: " + use_key.ToString());
                KeyHeader.LastKey = use_key;
            }
            else
                KeyHeader.LastKey++;

            // Write new key
            KeyHeaderAlloc.Write(KeyHeaderDef.A_LastKey, KeyHeader.LastKey);

            // Lookup place to add
            KeyBlockAllocWrite = null;
            if (KeyHeader.DescriptorLast >= 0)
                if (KeyHeader.LastKey < (KeyDescriptor[KeyHeader.DescriptorLast].FirstKey + KeyBlockDef.BLOCK_LENGTH))
                    KeyBlockAllocWrite = sp.GetAllocationByDescriptor(KeyDescriptor[KeyHeader.DescriptorLast].Address);

            // Not found, allocate new block
            if (KeyBlockAllocWrite == null) 
            {
                KeyBlockAllocWrite = sp.AllocateSpace(KeyBlockDef.BLOCK_SIZE, DEFS.POOL_KEY, generateID: false);
                byte[] b = new byte[KeyBlockDef.BLOCK_SIZE];
                // Fill all by -1
                for (int i = 0; i < KeyBlockDef.BLOCK_SIZE; i++)
                    b[i] = 255;
                KeyBlockAllocWrite.Write(0, b, b.Length);

                KeyHeader.DescriptorLast++;
                KeyHeader.DescriptorUsed++;

                KeyHeaderAlloc.Write(KeyHeaderDef.A_DescriptorLast, KeyHeader.DescriptorLast);
                KeyHeaderAlloc.Write(KeyHeaderDef.A_DescriptorUsed, KeyHeader.DescriptorUsed);

                // Extend if required
                if (KeyHeader.DescriptorLast == KeyHeader.DescriptorLength)
                {
                    sp.Extend(KeyHeaderAlloc, KeyDescriptorDef.DESCRIPTOR_CHUNK_SIZE);
                    KeyHeader.DescriptorLength += KeyDescriptorDef.DESCRIPTOR_CHUNK_LENGTH;
                    KeyHeaderAlloc.Write(KeyHeaderDef.A_DescriptorLength, KeyHeader.DescriptorLength);
                }

                KeyDescriptor[KeyHeader.DescriptorLast].FirstKey = KeyHeader.LastKey;
                KeyDescriptor[KeyHeader.DescriptorLast].Address = KeyBlockAllocWrite.DescriptorAddress;
                KeyDescriptor[KeyHeader.DescriptorLast].Used = 0;

                addr = KeyHeader.DescriptorLast * KeyDescriptorDef.DESCRIPTOR_ITEM_LENGTH;
                KeyDescriptorAlloc.Write(addr + KeyDescriptorDef.A_Address, KeyDescriptor[KeyHeader.DescriptorLast].Address);
                KeyDescriptorAlloc.Write(addr + KeyDescriptorDef.A_FirstKey, KeyDescriptor[KeyHeader.DescriptorLast].FirstKey);
            }

            // Write to block
            KeyBlockAllocWrite = sp.GetAllocationByDescriptor(KeyDescriptor[KeyHeader.DescriptorLast].Address);
            addr = ((KeyHeader.LastKey - KeyDescriptor[KeyHeader.DescriptorLast].FirstKey) * KeyBlockDef.BLOCK_ITEM_LENGTH);
            KeyBlockAllocWrite.Write(addr + KeyBlockDef.A_Address, address);

            // Update used
            KeyDescriptor[KeyHeader.DescriptorLast].Used++;
            KeyDescriptorAlloc.Write((KeyHeader.DescriptorLast * KeyDescriptorDef.DESCRIPTOR_ITEM_LENGTH) + KeyDescriptorDef.A_Used, KeyDescriptor[KeyHeader.DescriptorLast].Used);

            // Set read cache
            KeyBlockAllocRead = KeyBlockAllocWrite;
            KeyBlockReadIndex = KeyHeader.DescriptorLast;

            // Return key
            return KeyHeader.LastKey;
        }

        /// <summary>
        /// Return index of the descriptor item of -1 - not found
        /// </summary>
        /// <param name="key"></param>
        /// <returns>
        /// ret[0] - object address (if found); -1 - not found;
        /// ret[1] - descriptor index (if found)
        /// ret[2] - block index (if found)
        /// </returns>
        private long[] SearchKey(long key)
        {
            long[] ret = new long[3];
            ret[0] = -1;
            ret[1] = -1;
            ret[2] = -1;

            long lastk = 0;

            // 1. Check last used block
            if (KeyBlockReadIndex >= 0)             // Not 1st search
            {
                lastk = KeyDescriptor[KeyBlockReadIndex].FirstKey + KeyBlockDef.BLOCK_LENGTH - 1;      // Last key in block
                if ((key < KeyDescriptor[KeyBlockReadIndex].FirstKey) | (key > lastk))
                    KeyBlockReadIndex = -1;
            }

            // 2. No last used - find descriptor
            if (KeyBlockReadIndex < 0)
            {
                for (int i = KeyHeader.DescriptorLast; i >= 0; i--)
                {
                    if (KeyDescriptor[i].Used > 0)
                    {
                        lastk = KeyDescriptor[i].FirstKey + KeyBlockDef.BLOCK_LENGTH - 1;      // Last key in block
                        if ((key >= KeyDescriptor[i].FirstKey) & (key <= lastk))
                        {
                            KeyBlockReadIndex = i;
                            break;
                        }
                    }
                }
            }

            // 3. Descriptor found - check if key is allocated
            if (KeyBlockReadIndex >= 0)
            {
                KeyBlockAllocWrite = sp.GetAllocationByDescriptor(KeyDescriptor[KeyBlockReadIndex].Address);
                long obj_index = key - KeyDescriptor[KeyBlockReadIndex].FirstKey;
                long addr = (obj_index * KeyBlockDef.BLOCK_ITEM_LENGTH) + KeyBlockDef.A_Address;
                long obj_addr = KeyBlockAllocWrite.ReadLong(addr);
                if (obj_addr >= 0)
                {
                    ret[0] = obj_addr;
                    ret[1] = KeyBlockReadIndex;
                    ret[2] = obj_index;
                }
            }

            // Assuming KeyBlockAlloc is loaded if ret[0] >= 0
            return ret;
        }

        /// <summary>
        /// Return Address by Key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public long Get(long key)
        {
            return this.SearchKey(key)[0];
        }

        /// <summary>
        /// Update refrence by Key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public void Update(long key, VSAllocation a)
        {
            long[] ret = this.SearchKey(key);

            if (ret[2] >= 0)
                KeyBlockAllocWrite.Write((ret[2] * KeyBlockDef.BLOCK_ITEM_LENGTH) + KeyBlockDef.A_Address, a.DescriptorAddress);
        }

        /// <summary>
        /// Delete existing ID
        /// </summary>
        /// <param name="sid"></param>
        public int Delete(long key)
        {
            // Get key 
            long[] keyloc = SearchKey(key);
            if (keyloc[0] == 0)
                return -1;

            // Delete key in the block
            long addr = keyloc[2] * KeyBlockDef.BLOCK_ITEM_LENGTH;
            KeyBlockAllocWrite.Write(addr + KeyBlockDef.A_Address, KeyBlockDef.KEY_DELETED);

            // Update descriptor (not last key)
            if (KeyDescriptor[keyloc[1]].Used > 1)
            {
                KeyDescriptor[keyloc[1]].Used--;
                addr = keyloc[1] * KeyDescriptorDef.DESCRIPTOR_ITEM_LENGTH;
                KeyDescriptorAlloc.Write(addr + KeyDescriptorDef.A_Used, KeyDescriptor[keyloc[1]].Used);
            }
            // If last key in the block - free block
            else
            {
                KeyBlockReadIndex = -1;
                KeyBlockAllocRead = null;

                sp.Free(KeyBlockAllocWrite, false);
                KeyDescriptor[keyloc[1]].Used = 0;
                KeyDescriptor[keyloc[1]].Address = 0;
                KeyDescriptor[keyloc[1]].FirstKey = 0;
                addr = keyloc[1] * KeyDescriptorDef.DESCRIPTOR_ITEM_LENGTH;
                KeyDescriptorAlloc.Write(addr + KeyDescriptorDef.A_Used, KeyDescriptor[keyloc[1]].Used);
                KeyDescriptorAlloc.Write(addr + KeyDescriptorDef.A_Address, KeyDescriptor[keyloc[1]].Address);
                KeyDescriptorAlloc.Write(addr + KeyDescriptorDef.A_FirstKey, KeyDescriptor[keyloc[1]].FirstKey);

                KeyHeader.DescriptorUsed--;
                KeyHeaderAlloc.Write(KeyHeaderDef.A_DescriptorUsed, KeyHeader.DescriptorUsed);

                for (int i = 0; i < KeyHeader.DescriptorLength; i++)
                {
                    if (KeyDescriptor[i].Address == 0)
                    {
                        for (int j = i + 1; j < KeyHeader.DescriptorLength; j++)
                        {
                            if (KeyDescriptor[j].Address != 0)
                            {
                                KeyDescriptor[i].Address = KeyDescriptor[j].Address;
                                KeyDescriptor[i].FirstKey = KeyDescriptor[j].FirstKey;
                                KeyDescriptor[i].Used = KeyDescriptor[j].Used;

                                KeyDescriptor[j].Address = 0;
                                KeyDescriptor[j].FirstKey = 0;
                                KeyDescriptor[j].Used = 0;

                                break;
                            }
                        }
                    }
                }

                // Update header last
                int save_last = KeyHeader.DescriptorLast;
                KeyHeader.DescriptorLast = KeyHeader.DescriptorUsed - 1;
                KeyHeaderAlloc.Write(KeyHeaderDef.A_DescriptorLast, KeyHeader.DescriptorLast);

                // Rewrite header
                for (int i = 0; i <= save_last; i++)
                {
                    addr = i * KeyDescriptorDef.DESCRIPTOR_ITEM_LENGTH;
                    KeyDescriptorAlloc.Write(addr + KeyDescriptorDef.A_Address, KeyDescriptor[i].Address);
                    KeyDescriptorAlloc.Write(addr + KeyDescriptorDef.A_FirstKey, KeyDescriptor[i].FirstKey);
                    KeyDescriptorAlloc.Write(addr + KeyDescriptorDef.A_Used, KeyDescriptor[i].Used);
                }
            }
            return 0;
        }



        //////////////////////////////////////////////
        //////////// ENUMERATOR //////////////////////
        //////////////////////////////////////////////
        private int D_current = 0;             // Current descriptor index
        private int B_current = 0;             // Current block index
        private long current = -1;             // Current Object id or -1
        private Action action = Action.End;
        long NUM = 0;                          // Total number of items in all blocks
        long n = 0;                            // Items counter
        

        enum Action
        {
            Right,
            End
        }

        /// <summary>
        /// Reset
        /// </summary>
        public void Reset()
        {
            D_current = -1;                 // Index of the current descriptor
            action = (KeyHeader.DescriptorUsed == 0) ? Action.End : Action.Right;
        }


        /// <summary>
        /// Next node
        /// </summary>
        /// <returns></returns>
        public bool Next()
        {
            if (action == Action.End)
                return false;

            current = -1;
            // Cases - assuming index is not empty (action !- Action.End)
            // 1. Initial: D_Current < 0
            if (D_current < 0)
            {
                D_current = 0;      // Set 1st desc
                B_current = -1;
                KeyBlockAllocWrite = null;
                n = 0;
                NUM = (KeyHeader.DescriptorLast + 1) * KeyBlockDef.BLOCK_LENGTH;      // Total numeber of items in all blocks (including empty and deleted)
            }

            while (n < NUM)
            {
                if (KeyDescriptor[D_current].Used == 0)     // If empty block
                {
                    n += KeyBlockDef.BLOCK_LENGTH;
                    D_current++;
                    B_current = -1;
                }
                else
                {
                    B_current++;
                    if (B_current == KeyBlockDef.BLOCK_LENGTH)   // Out of block
                    {
                        KeyBlockAllocWrite = null;
                        D_current++;
                        B_current = -1;
                    }
                    else                                        // Within block
                    {
                        if (KeyBlockAllocWrite == null)
                            KeyBlockAllocWrite = sp.GetAllocationByDescriptor(KeyDescriptor[D_current].Address);

                        if (KeyBlockAllocWrite.ReadLong(B_current * KeyBlockDef.BLOCK_ITEM_LENGTH + KeyBlockDef.A_Address) > 0)
                        {
                            current = KeyDescriptor[D_current].FirstKey + B_current;
                            n++;
                            break;
                        }
                        else
                            n++;
                    }
                }
            }


            if (current < 0)
            {
                action = Action.End;
                return false;
            }


            return true;

        }

        /// <summary>
        /// Current value
        /// </summary>
        public long Current
        {
            get { return (current < 0)? 0 : current; }
        }

        /// <summary>
        /// Is emptye
        /// </summary>
        public bool IsEmpty
        {
            get { return KeyHeader.DescriptorUsed  == 0; }
        }

    }
}
