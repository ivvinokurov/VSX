using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace VStorage
{
    public class VSpace
    {
#if DEBUG
        public VSTimer __TIMER = new VSTimer();
#endif
        VSAllocation A_POOL_USER_DEFINED = null;
        VSAllocation A_POOL_DYNAMIC = null;

        /// <summary>
        /// Private fields
        /// </summary>
        private FBQE_CACHE Cache = null;                        // Cache object
        public VSKeyManager key_manager;                        
        private VSFreeSpaceManager fbqe_header;

        private VSCatalogDescriptor DESCRIPTOR = null;
        private string error = "";

        // Indexes
        public List<VSIndex> index_list = null;
        public List<VSIndex> index_list_full = null;

        /// <summary>
        /// Public fields
        /// </summary>
        private VSVirtualMemoryManager vm;

        internal VSpace()
        {
        }


        ////////////////////////////////////////////////////////////////////////////
        //////////////////////  COMMON METHODS   ///////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Attach new address space. Return 0 - successful; -1 - error
        /// </summary>
        /// <param name="_path"></param>
        /// <param name="_cat_file"></param>
        /// <param name="idx"></param>
        /// <returns></returns>
        internal string Attach(VSCatalogDescriptor desc, VSVirtualMemoryManager vmm, VSTransaction ta)
        {
            DESCRIPTOR = desc;
            vm = vmm;

            // Check if restore is not completed
            long l = vm.ReadLong(DEFS.SYSTEM_STATUS_ADDRESS);
            if (l != 0)
                return VSException.GetMessage(VSException.E0009_RESTORE_NOT_COMPLETED_CODE) + " Space: " + Name;


            // Build index list
            index_list = new List<VSIndex>();
            index_list_full = new List<VSIndex>();
            long addr = GetRootAddress(DEFS.POOL_INDEX);            // Get 1st ADSC addredd
            while (addr > 0)
            {
                VSIndex xd = new VSIndex(this, addr);
                index_list_full.Add(xd);
                if (xd.Name.IndexOf(DEFS.INDEX_CROSS_REFERENCES) < 0)
                    index_list.Add(xd);

                addr = xd.NEXT;
            }

            // Set ref indexes
            for (int i = 0; i < index_list.Count; i++)
                set_ref_index(index_list[i]);

            return "";
        }


        /// <summary>
        /// Detach space. Return 0-successful, -1 - error
        /// </summary>
        /// <returns></returns>
        internal int Detach()
        {
            if (vm == null)
            {
                error = "Space is already closed";
                return -1;
            }

            int rc = vm.Close();
            if (rc < 0)
                this.error = vm.error;
            vm = null;
            index_list = null;
            index_list_full = null;
            return rc;
        }

        /// <summary>
        /// Roll Changes (create FBQE for new space, process new allocation)
        /// Call from VSEngine when 'Open' or after extesion
        /// </summary>
        internal void VerifySpaceChanges()
        {
            fbqe_header = new VSFreeSpaceManager(this);

            // Process new allocations if exists
            short n_new = vm.ReadShort(DEFS.SYSTEM_ALLOCATION_ADDRESS);
            if (n_new > 0)
            {
                long e_address = DEFS.SYSTEM_ALLOCATION_ADDRESS + 2;
                for (int i = 0; i < n_new; i++)
                {
                    long fstart = vm.ReadLong(e_address);
                    long fend = vm.ReadLong(e_address + 8);
                    if (fbqe_header.LAST < 0)
                    {
                        Cache.UpdateCache(fbqe_header.AddFBQE(fstart, fend - fstart, fbqe_header.LAST, -1));
                    }
                    else
                    {
                        VSFreeSpaceManager.FBQE fp = fbqe_header.GetFBQE(fbqe_header.LAST);
                        if (fp.ADDRESS_END == fstart)      //Extend last FBQE
                            Cache.UpdateCache(fbqe_header.UpdateFBQE(fbqe_header.LAST, fp.ADDRESS_START, fp.LENGTH + fend - fstart));
                        else
                            Cache.UpdateCache(fbqe_header.AddFBQE(fstart, fend - fstart, fbqe_header.LAST, -1));
                    }
                    e_address += 16;
                }
                // Cleanup allocation table
                vm.Write(DEFS.SYSTEM_ALLOCATION_ADDRESS, (short)0);
                
                // Expand FBQE block if needed
                fbqe_header.CheckFreeQueueSize();
            }

            // Create cache
             Cache = new FBQE_CACHE(fbqe_header);

            //Initialize KeyHelper
            key_manager = new VSKeyManager(this);

            //Check pool areas descriptors, create if nulls
            A_POOL_USER_DEFINED = GetRootAllocation(DEFS.POOL_USER_DEFINED);
            if (A_POOL_USER_DEFINED == null)
                A_POOL_USER_DEFINED = AllocateSpace(DEFS.ALLOCATION_USER_DEFINED, DEFS.POOL_USER_DEFINED, false, 0);

            A_POOL_DYNAMIC = GetRootAllocation(DEFS.POOL_DYNAMIC);
            if (A_POOL_DYNAMIC == null)
                A_POOL_DYNAMIC = AllocateSpace(DEFS.ALLOCATION_DYNAMIC, DEFS.POOL_DYNAMIC, false, 0);

        }

        /************************************************************************************/
        /************************   Space management    *************************************/
        /************************************************************************************/
        /************************************************************************************/
        /******************   Protected space management (system methods) *******************/
        /************************************************************************************/

        /// <summary>
        /// Allocate space
        /// </summary>
        /// <param name="size"></param>
        /// <param name="pool"></param>
        /// <param name="generateID"></param>
        /// <returns></returns>
        internal VSAllocation ALLOCATE_SPACE(long size, short pool, bool generateID = true, long chunk = 0, short fixed_size = 0)
        {
            long ch = size;
            long s = 0;

            if (chunk > 0)
                ch = chunk;

            VSAllocation a = AllocateSpaceSegment(ch, pool, generateID);
            a.FIXED = (ushort)fixed_size;
            a.ChunkSize = (int)chunk;

            s += ch;
            while (s < size)
            {
                long a_size = (ch > (size - s)) ? (size - s) : ch;
                AllocateSpaceSegment(a_size, 0, false, a);
                s += a_size;
            }

            return a;
        }

        /// <summary>
        /// Allocate space (internal)
        /// </summary>
        /// <param name="size"></param>
        /// <param name="pool"></param>
        /// <param name="generateID"></param>
        /// <returns></returns>
        internal VSAllocation AllocateSpace(long size, short pool, bool generateID = true, long chunk = 0)
        {
            return ALLOCATE_SPACE(size, pool, generateID, chunk);
        }

        /// <summary>
        /// Create object
        /// </summary>
        /// <param name="size"></param>
        /// <param name="pool"></param>
        /// <param name="chunk"></param>
        /// <returns></returns>
        public VSObject Allocate(long size, short pool, long chunk = 0, short fixed_size = 0)
        {
            // Cannot be a system pool (0 or less)
            if (pool < 1)      
                throw new VSException(VSException.E0012_INVALID_POOL_NUMBER_CODE, "- " + pool.ToString() + " (Allocate)");

            VSAllocation a = ALLOCATE_SPACE(size, pool, true, chunk, fixed_size);
            return GetObject(a.Id);
        }

        /// <summary>
        /// Release allocated space by object
        /// </summary>
        /// <param name="a"></param>
        /// <param name="deleteID"></param>
        public void Release(VSObject a, bool deleteID = true)
        {
            // Cannot be a system pool (0 or less)
            if (a.Pool < 1)
                throw new VSException(VSException.E0012_INVALID_POOL_NUMBER_CODE, "- " + a.Pool.ToString() + " (Release)");

            Free(a, deleteID);
        }

        /// <summary>
        /// Free space by allocation address (protected)
        /// </summary>
        internal void Free(VSAllocation a, bool deleteID = true)
        {
            VSAllocation nxt = null;
            if (a.Chunk != 0)
                nxt = GetAllocationByDescriptor(a.NEXT);

            FreeSpaceSegment(a, deleteID);

            while (nxt != null)
            {
                long nextAddr = nxt.NEXT;
                short ch = nxt.Chunk;
                FreeSpaceSegment(nxt, false);
                if ((nextAddr > 0) & (ch > 0))
                    nxt = GetAllocationByDescriptor(nextAddr);
                else
                    nxt = null;
            }
        }

        /// <summary>
        /// Remove all related indexes for object by ID
        /// ONLY for objects and indexes located in THIS space
        /// </summary>
        /// <param name="a"></param>
        private void remove_all_indexes(string space_name, long id)
        {
            VSIndex ref_index = this.get_index(VSLib.PrepareFullIndexName(space_name, DEFS.INDEX_CROSS_REFERENCES));
            if (ref_index != null)
            {
                byte[] key = VSLib.ConvertLongToByte(id);

                long[] ref_nodes = ref_index.FindAll(key, false);                              // Get all avl node ids for obj id

                ref_index.delete_node(key, -1);                           // Remove reference record

                for (int i = 0; i < ref_nodes.Length; i++)
                    this.get_index(this.GetAllocation(ref_nodes[i]).ReadLong(VSAvlNode.INDEX_POS)).delete_avl_node(ref_nodes[i], id);

            }
        }

        /// <summary>
        /// Free space by ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public int ReleaseID(long id)
        {
            VSAllocation a = GetAllocation(id);
            if (a == null)
                return -1;
            Free(a);
            return 0;
        }

        /// <summary>
        /// Free all pool data
        /// </summary>
        /// <param name="n"></param>
        public void ReleasePool(short n)
        {
            if (n < 1)
                throw new VSException(VSException.E0014_INVALID_POOL_NUMBER_CODE, "- " + n.ToString());

            long addr = GetRootAddress(n);
            while (addr > 0)
            {
                VSAllocation a = GetAllocationByDescriptor(addr);
                addr = a.NEXT;
                
                FreeSpaceSegment(a);
                addr = GetRootAddress(n);
            }
        }

        /// <summary>
        /// Extend space (protected)
        /// </summary>
        /// <param name="address">Data address to extend</param>
        /// <param name="size">Additional size to add if > 0. Otherwise - min space chunk</param>
        internal void ExtendSpace(VSAllocation o, long size)
        {
            VSAllocation a = AllocateSpaceSegment(size, 0, false, o);
        }

        /// <summary>
        /// Shrink space
        /// </summary>
        internal void ShrinkObject(VSObject o, long new_size)
        {
            if (o.Chunk == 1)
            {
                long current_size = o.Size;
                VSAllocation a = o.LastChunk;
                while ((current_size - a.Length) >= new_size)
                {
                    VSAllocation prev_alloc = a.PrevChunk;


                    if (prev_alloc.Chunk == 1)          // 1st segment
                    {
                        o.LAST = 0;
                        o.Chunk = 0;
                    }
                    else
                    {
                        o.LAST = prev_alloc.DescriptorAddress;
                        prev_alloc.Chunk = (short)(prev_alloc.Chunk * (- 1));
                    }

                    current_size -= a.Length;
                    o.SetSize(current_size);
                    FreeSpaceSegment(a, false);
                    a = prev_alloc;
                }
            }
        }

        
        /************************************************************************************/
        /************************   Memory management (system methods)***********************/
        /************************************************************************************/

        /// <summary>
        /// Extend space (protected)
        /// </summary>
        /// <param name="address">Data address to extend</param>
        /// <param name="size">Additional size to add if > 0. Otherwise - min space chunk</param>
        public void Extend(VSAllocation o, long size)
        {
            VSAllocation a = AllocateSpaceSegment(size, 0, false, o);
        }
        /************************************************************************************/
        /************************   Memory management (system methods)***********************/
        /************************************************************************************/


        /// <summary>
        /// Allocate space (system method)
        /// </summary>
        /// <param name="size">Allocation size</param>
        /// <param name="pool">Allocation pool. 0 if base_address != 0</param>
        /// <param name="generateID">True id ID is required. false if base_address != 0</param>
        /// <param name="base_address"> 1st allocated address (extend)</param>
        /// <returns>
        /// 0 - address
        /// 1 - ID (or 0)
        /// </returns>
        internal VSAllocation AllocateSpaceSegment(long size, short pool, bool generateID = true, VSAllocation base_alloc = null)
        {
#if (DEBUG)
            __TIMER.START("alloc:main");
#endif

            if ((base_alloc != null) & ((pool != 0) | (generateID == true)))
                throw new VSException(VSException.E0020_INVALID_EXTENSION_PARAMETERS_ERROR_CODE);

            VSFreeSpaceManager.FBQE f;
            long alloc_addr = 0;
            long length_desc = (base_alloc == null)? VSAllocation.DescriptorLength: VSAllocation.ExtensionLength;       // Descriptor length


            // Calculate allocation size)
            long length = size / DEFS.MIN_SPACE_ALLOCATION_CHUNK;

            if (length == 0)
                length++;
            else if ((length * DEFS.MIN_SPACE_ALLOCATION_CHUNK) < size)
                length++;

            long length_use = (length * DEFS.MIN_SPACE_ALLOCATION_CHUNK);

            length = length_use + length_desc;


            //Repeat cycle if no free space (dyn extension)
            for (int repeat = 0; repeat < 2; repeat++)
            {

#if (DEBUG)
                __TIMER.START("alloc:cache_search");
#endif

                // Lookup in cache
                int current = Cache.FindFreeSpace(length);

#if (DEBUG)
                __TIMER.END("alloc:cache_search");
#endif

#if (DEBUG)
                __TIMER.START("alloc:search_fbqe");
#endif

                // Not in cache, search
                if (current < 0)
                {
                    int idx = fbqe_header.FIRST;
                    int idx2 = fbqe_header.LAST;
                    bool sw = true;

                    while (true)
                    {
                        int n = sw ? idx : idx2;

                        if (n < 0)
                            break;

                        f = fbqe_header.GetFBQE(n);             // Read next
                        if (f.LENGTH >= length)                 // Found
                        {
                            current = n;
                            break;
                        }
                        if (idx == idx2)
                            break;                              // Not found

                        if (sw)
                            idx = f.NEXT;
                        else
                            idx2 = f.PREV;

                        sw = !sw;
                    }
                }
#if (DEBUG)
                __TIMER.END("alloc:search_fbqe");
#endif

                if (current < 0)
                {
#if (DEBUG)
                    __TIMER.START("alloc:extend");
#endif
                    if (repeat > 0)
                        throw new VSException(VSException.E0013_SPACE_NOT_AVAILABLE_CODE, "Requested size: " + length.ToString());
                    else if (vm.Extend() != 0)
                        throw new VSException(VSException.E0013_SPACE_NOT_AVAILABLE_CODE, "Requested size: " + length.ToString());
                    else
                        this.VerifySpaceChanges();         //Process new if space is successfully extended
#if (DEBUG)
                    __TIMER.END("alloc:extend");
#endif
                }
                else
                {
#if (DEBUG)
                    __TIMER.START("alloc:fbqe_upd");
#endif

                    f = fbqe_header.GetFBQE(current);
                    int u_idx = -1;
                    alloc_addr = f.ADDRESS_START;
                    if (f.LENGTH == length)                 // Remove current 
                    {
                        u_idx = fbqe_header.DeleteFBQE(current);
                    }
                    else
                    {
                        f.ADDRESS_START += length;
                        f.LENGTH -= length;
                        u_idx = fbqe_header.UpdateFBQE(current, f.ADDRESS_START, f.LENGTH);
                    }

#if (DEBUG)
                    __TIMER.START("alloc:fbqe_cache_upd");
#endif
                    Cache.UpdateCache(u_idx);
#if (DEBUG)
                    __TIMER.END("alloc:fbqe_cache_upd");
#endif

#if (DEBUG)
                    __TIMER.END("alloc:fbqe_upd");
#endif
                    break;
                }
            }

            short new_pool = pool;
            // If base_address != 0 - take pool# from the root descriptor
            if (base_alloc != null)
            {
                new_pool = base_alloc.Pool;
            }

            // Create allocated space descriptor
            VSAllocation new_obj = new VSAllocation(vm, alloc_addr, length, new_pool);

            long last_obj = GetLastAddress(new_pool);

            VSAllocation prev_obj;
            if (base_alloc == null)
            { // Initial allocation
#if (DEBUG)
                __TIMER.START("alloc:new_alloc");
#endif
#if (DEBUG)
                __TIMER.START("alloc:gen_id");
#endif
                if (generateID)
                    new_obj.Id = key_manager.Add(new_obj);              // Generate ID if required
#if (DEBUG)
                __TIMER.END("alloc:gen_id");
#endif

                if (last_obj == 0)
                { // First allocation for pool
                    SetFirstAddress(new_pool, alloc_addr);
                    SetLastAddress(new_pool, alloc_addr);
                }
                else
                {
                    prev_obj = GetAllocationByDescriptor(last_obj);
                    prev_obj.NEXT = new_obj.DescriptorAddress;
                    new_obj.PREV = prev_obj.DescriptorAddress;
                    SetLastAddress(new_pool, new_obj.DescriptorAddress);        // New Last
                }
#if (DEBUG)
                __TIMER.END("alloc:new_alloc");
#endif
            }
            else 
            { //Extend
#if (DEBUG)
                __TIMER.START("alloc:extend_obj");
#endif

                prev_obj = base_alloc;
                if (base_alloc.Chunk == 0)
                {
                    base_alloc.Chunk = 1;
                    new_obj.Chunk = -2;
                }
                else
                {
                    prev_obj = GetAllocationByDescriptor(base_alloc.LAST);
                    short ch = (short)(prev_obj.Chunk * -1);
                    if (ch == DEFS.MAX_SPACE_ALLOCATION_CHUNKS)
                        throw new VSException(VSException.E0021_MAX_ALLOCATION_CHUNKS_REACHED_CODE, "- " + DEFS.MAX_SPACE_ALLOCATION_CHUNKS.ToString());
                    prev_obj.Chunk = ch;
                    new_obj.Chunk = (short)((ch + 1) * -1);
                }

                base_alloc.LAST = new_obj.DescriptorAddress;                      // New pointer to last

                if (prev_obj.NEXT == 0)
                {
                    prev_obj.NEXT = new_obj.DescriptorAddress;
                    new_obj.PREV = prev_obj.DescriptorAddress;
                    SetLastAddress(new_pool, new_obj.DescriptorAddress);        // New last
                }
                else
                {
                    VSAllocation next_obj = GetAllocationByDescriptor(prev_obj.NEXT);
                    prev_obj.NEXT = new_obj.DescriptorAddress;
                    new_obj.NEXT = next_obj.DescriptorAddress;
                    new_obj.PREV = prev_obj.DescriptorAddress;
                    next_obj.PREV = new_obj.DescriptorAddress;
                }
#if (DEBUG)
                __TIMER.END("alloc:extend_obj");
#endif

            }

            if (base_alloc == null)
                new_obj.SetSize(length_use);
            else
                base_alloc.SetSize(base_alloc.Size + length_use);
#if (DEBUG)
            __TIMER.END("alloc:main");
#endif

            return new_obj;
        }


        /// <summary>
        /// Get the 1st key
        /// </summary>
        /// <returns></returns>
        public long GetRootID(short pool)
        {
            VSAllocation a = GetAllocationByDescriptor(GetFirstAddress(pool));
            return (a == null) ? 0 : a.Id;
        }

        /// <summary>
        /// Get address of the 1st allocation
        /// </summary>
        /// <returns></returns>
        internal long GetRootAddress(short pool)
        {
            return GetFirstAddress(pool);
        }

        /// <summary>
        /// Get VSAllocation object of the 1st allocation
        /// </summary>
        /// <returns></returns>
        public VSObject GetRootObject(short pool)
        {
            long a = GetFirstAddress(pool);
            return (a == 0) ? null : this.GetObjectByDescriptor(a);
        }

        /// <summary>
        /// Get address of the 1st allocation
        /// </summary>
        /// <returns></returns>
        internal VSAllocation GetRootAllocation(short pool)
        {
            long a = GetFirstAddress(pool);
            return (a == 0) ? null : this.GetAllocationByDescriptor(a);
        }


        /// <summary>
        /// Get object
        /// </summary>
        /// <returns></returns>
        internal VSAllocation GetAllocation(long id)
        {
            long addr = key_manager.Get(id);
            return (addr > 0) ? new VSAllocation(vm, addr) : null;
        }

        /// <summary>
        /// Get object by ID
        /// </summary>
        /// <returns></returns>
        public VSObject GetObject(long id)
        {
            long addr = key_manager.Get(id);
            return (addr > 0) ? new VSObject(this, addr) : null;
        }

        /// <summary>
        /// Get object allocation pool
        /// </summary>
        /// <returns></returns>
        public short GetObjectPool(long id)
        {
            long addr = key_manager.Get(id);
            return (addr > 0) ? vm.ReadShort(addr +VSAllocation.POOL_POS) : (short)0;
        }


        /// <summary>
        /// Return unused pool number
        /// </summary>
        /// <returns></returns>
        public short GetFreePoolNumber()
        {
            long n = A_POOL_DYNAMIC.Size / DEFS.SYSTEM_POOL_DESCRIPTOR_LENGTH;
            for (int iteration = 0; iteration < 2; iteration++)
            {
                for (long i = 0; i < n; i++)
                {
                    long a = A_POOL_DYNAMIC.ReadLong(i * DEFS.SYSTEM_POOL_DESCRIPTOR_LENGTH);
                    if (a == 0)
                        return (short)(i + DEFS.POOL_MIN_DYNAMIC);
                }
                ExtendSpace(A_POOL_DYNAMIC, DEFS.ALLOCATION_DYNAMIC);
            }
            return -1;
        }


        /// <summary>
        /// Free space
        /// </summary>
        /// <param name="address"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        private int FreeSpaceSegment(VSAllocation a, bool deleteID = true)
        {
#if (DEBUG)
            __TIMER.START("main");
#endif

            long a_prev = a.PREV;
            long a_next = a.NEXT;
            short a_pool = a.Pool;
            long a_id = a.Id;

#if (DEBUG)
            __TIMER.START("check_free_queue");
#endif
            // Expand FBQE block if needed
            fbqe_header.CheckFreeQueueSize();
#if (DEBUG)
            __TIMER.END("check_free_queue");
#endif


#if (DEBUG)
            __TIMER.START("remove_all_indexes");
#endif
            //
            if (deleteID & (a_id > 0) & (a_pool > 0))
            {
                if (this.IndexSpace == null)
                    remove_all_indexes("", a_id);                                           // Remove all local indexes
                else
                    this.IndexSpace.remove_all_indexes(this.Name, a_id);                    // Remove all external indexes
            }
#if (DEBUG)
            __TIMER.END("remove_all_indexes");
#endif

            long address = a.DescriptorAddress;

            long length = a.FullLength;

            long end_address = address + length;

            VSFreeSpaceManager.FBQE f, fn;

            if (end_address > this.Size)
                throw new VSException(VSException.E0015_INVALID_ADDRESS_CODE, "Address is out of space boundaries - " + address.ToString());

            // Delete index if required
            if (deleteID & (a_id > 0))
                key_manager.Delete(a_id);

            // Lookup FBQE cache ///////////////////////////
#if (DEBUG)
            __TIMER.START("lookup_cache");
#endif

            int f_idx = Cache.FindStartPoint(address);


#if (DEBUG)
            __TIMER.END("lookup_cache");
#endif
            ////////////////////////////////////////////////

            fn = fbqe_header.GetFBQE(f_idx);
            f = fn;
            bool f_found = false;
            bool fn_found = true;

#if (DEBUG)
            __TIMER.START("find_place");
#endif

            //Search place to add/update 
            if (end_address > fn.ADDRESS_START)
            {
                f = fn;
                f_found = true;
                fn_found = false;
                while (f.NEXT >= 0)
                {
                    fn = fbqe_header.GetFBQE(f.NEXT);
                    fn_found = true;
                    if ((address >= f.ADDRESS_END) & (end_address <= fn.ADDRESS_START))
                        break;
                    f = fn;
                    fn_found = false;
                }
            }

#if (DEBUG)
            __TIMER.END("find_place");
#endif

            //Update FBQE per search result
#if (DEBUG)
            __TIMER.START("update_cache");
#endif

            if (!f_found)               //Update 1st or insert befor 1st)
            {
                if (fn.ADDRESS_START == end_address)    // Extend 1st
                    Cache.UpdateCache(fbqe_header.UpdateFBQE(fbqe_header.FIRST, address, fn.LENGTH + length));
                else
                    Cache.UpdateCache(fbqe_header.AddFBQE(address, length, -1, fbqe_header.FIRST));
            }
            else if (!fn_found)         //Update last or append 
            {
                if (f.ADDRESS_END == address)      // Extend last
                    Cache.UpdateCache(fbqe_header.UpdateFBQE(fbqe_header.LAST, f.ADDRESS_START, f.LENGTH += length));
                else                                   //Append
                    Cache.UpdateCache(fbqe_header.AddFBQE(address, length, fbqe_header.LAST, -1));
            }
            else                                    //Inser between (or update) 
            {
                if (address < f.ADDRESS_END)
                    throw new VSException(VSException.E0015_INVALID_ADDRESS_CODE, " Not allocated space - " + address.ToString());

                if ((address > f.ADDRESS_END) & (end_address < fn.ADDRESS_START))                                            //insert new between f and fn
                    Cache.UpdateCache(fbqe_header.AddFBQE(address, length, fn.PREV, f.NEXT));
                else if ((address == f.ADDRESS_END) & (end_address == fn.ADDRESS_START))    //all space betwee f and fn - merge all to f
                {
                    Cache.UpdateCache(fbqe_header.UpdateFBQE(fn.PREV, f.ADDRESS_START, f.LENGTH + fn.LENGTH + length));
                    Cache.UpdateCache(fbqe_header.DeleteFBQE(f.NEXT));
                }

                else if (address == f.ADDRESS_END)                   //extend lower
                    Cache.UpdateCache(fbqe_header.UpdateFBQE(fn.PREV, f.ADDRESS_START, f.LENGTH + length));
                else if (end_address == fn.ADDRESS_START)           // extend upper
                    Cache.UpdateCache(fbqe_header.UpdateFBQE(f.NEXT, address, fn.LENGTH + length));
            }
#if (DEBUG)
            __TIMER.END("update_cache");
#endif

#if (DEBUG)
            __TIMER.START("update_desc");
#endif

            //Update descriptors
            //Ref to prev
            if (a_prev == 0)
            {
                SetFirstAddress(a_pool, a_next);
            }
            else
            {
                VSAllocation p = GetAllocationByDescriptor(a_prev);
                p.NEXT = a_next;
            }

            //Ref to next
            if (a_next == 0)
            {
                SetLastAddress(a_pool, a_prev);
            }
            else
            {
                VSAllocation n = GetAllocationByDescriptor(a_next);
                n.PREV = a_prev;
            }

#if (DEBUG)
            __TIMER.END("update_desc");
#endif

            //Clear memory
            byte[] b = new byte[length];
            vm.Write(address, b, length);


#if (DEBUG)
            __TIMER.END("main");
#endif

            return 0;
        }
        /// <summary>
        /// Get/set pool descriptot address (first or last
        /// </summary>
        /// <param name="pool"></param>
        /// <param name="op">0 - read; 1 - write</param>
        /// <param name="n">0 - first; 1 - last</param>
        /// <param name="value">optional. of op=1 only</param>
        /// <returns></returns>
        private long PoolDescriptorAddressIO(int pool, int op, int n, long value = 0)
        {
            long offs = (n == 0) ? 0 : 8;                                               // Offset in the descriptor
            long addr = 0;

            if (pool < DEFS.POOL_MIN_USER_DEFINED)
            { // System pool
                if (Math.Abs(pool) >= DEFS.SYSTEM_POOL_DESCRIPTOR_NUMBER)
                    throw new VSException(VSException.E0012_INVALID_POOL_NUMBER_CODE, "- " + pool.ToString());

                addr = DEFS.SYSTEM_POOL_AREA_ADDRESS + (long)(Math.Abs(pool) * DEFS.SYSTEM_POOL_DESCRIPTOR_LENGTH + offs);

                if (op == 0)
                    return vm.ReadLong(addr);
                else
                {
                    vm.Write(addr, value);
                    return 0;
                }
            }
            else if (pool < DEFS.POOL_MIN_DYNAMIC)
            { // User-defined
                addr = (pool - DEFS.POOL_MIN_USER_DEFINED) * DEFS.SYSTEM_POOL_DESCRIPTOR_LENGTH + offs;

                while (addr >= A_POOL_USER_DEFINED.Size)
                    ExtendSpace(A_POOL_USER_DEFINED, DEFS.ALLOCATION_USER_DEFINED);            // Extend if needed

                if (op == 0)
                    return A_POOL_USER_DEFINED.ReadLong(addr);
                else
                {
                    A_POOL_USER_DEFINED.Write(addr, value);
                    return 0;
                }
            }
            else
            { // Dynamic
                addr = (pool - DEFS.POOL_MIN_DYNAMIC) * DEFS.SYSTEM_POOL_DESCRIPTOR_LENGTH + offs;

                while (addr >= A_POOL_DYNAMIC.Size)
                    ExtendSpace(A_POOL_USER_DEFINED, DEFS.ALLOCATION_DYNAMIC);                 // Extend if needed

                if (op == 0)
                    return A_POOL_DYNAMIC.ReadLong(addr);
                else
                {
                    A_POOL_DYNAMIC.Write(addr, value);
                    return 0;
                }
            }
        }

        /// <summary>
        /// Get address of the 1st allocation in the pool; 0 if no allocations
        /// </summary>
        /// <param name="pool"></param>
        /// <returns></returns>
        internal long GetFirstAddress(short pool)
        {
            return PoolDescriptorAddressIO(pool, 0, 0);
        }

        /// <summary>
        /// Get address of the last allocation in the pool; 0 if no allocations
        /// </summary>
        /// <param name="pool"></param>
        /// <returns></returns>
        internal long GetLastAddress(short pool)
        {
            return PoolDescriptorAddressIO(pool, 0, 1);
        }

        /// <summary>
        /// Set address of the 1st allocation in the pool
        /// </summary>
        /// <param name="pool"></param>
        /// <returns></returns>
        internal void SetFirstAddress(short pool, long value)
        {
            PoolDescriptorAddressIO(pool, 1, 0, value);
        }

        /// <summary>
        /// Set address of the last allocation in the pool
        /// </summary>
        /// <param name="pool"></param>
        /// <returns></returns>
        internal void SetLastAddress(short pool, long value)
        {
            PoolDescriptorAddressIO(pool, 1, 1, value);
        }

        /// <summary>
        /// Return all allocated pools
        /// </summary>
        /// <returns></returns>
        public short[] GetPools()
        {
            short pool = 0;
            int n = 0;
            // System pools
            List<short> pools = new List<short>();
            pools.Add(0);           // Free space
            for (int i = 1; i < DEFS.SYSTEM_POOL_DESCRIPTOR_NUMBER; i++)
            {
                pool = (short)(i * (-1));           // Actual pool #
                long a = GetFirstAddress(pool);
                if (a > 0)
                    pools.Add(pool);
            }

            // User-defined pools
            n = (int)(A_POOL_USER_DEFINED.Size / DEFS.SYSTEM_POOL_DESCRIPTOR_LENGTH);
            for (int i = 0; i < n; i++)
            {
                pool = (short)(i + DEFS.POOL_MIN_USER_DEFINED);
                if (GetFirstAddress(pool) > 0)
                    pools.Add(pool);
            }

            // Dynamic pools
            n = (int)(A_POOL_DYNAMIC.Size / DEFS.SYSTEM_POOL_DESCRIPTOR_LENGTH);
            for (int i = 0; i < n; i++)
            {
                pool = (short)(i + DEFS.POOL_MIN_DYNAMIC);
                if (GetFirstAddress(pool) > 0)
                    pools.Add(pool);
            }

            return pools.ToArray();
        }

        /// <summary>
        /// Return all pools for dump
        /// </summary>
        /// <returns></returns>
        internal short[] GetPoolsForDump()
        {
            List<short> pl = GetPools().ToList();
            for (int i = (pl.Count - 1); i >= 0; i--)
            {
                if (pl[i] == 0)
                    pl.RemoveAt(i);
                else if (pl[i] < 0)
                {
                    if (pl[i] != DEFS.POOL_INDEX)
                        pl.RemoveAt(i);
                }
            }
            return pl.ToArray();
        }

        /// <summary>
        /// Read allocation descriptor by descriptor address
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        internal VSAllocation GetAllocationByDescriptor(long address)
        {
            return (address == 0)? null : new VSAllocation(vm, address);
        }

        /// <summary>
        /// Read object by descriptor address
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        internal VSObject GetObjectByDescriptor(long address)
        {
            return new VSObject(this, address);
        }

        /////////////////////////////////////////////////////////////
        //////////////////////// Properties /////////////////////////
        /////////////////////////////////////////////////////////////
        /// <summary>
        /// Get space key mgr
        /// </summary>
        /// <returns></returns>
        internal VSKeyManager KeyManager
        {
            get { return key_manager; }
        }

        /// <summary>
        /// Space Size
        /// </summary>
        public long Size
        {
            get
            {
                return vm.GetSpaceSize();
            }
        }

        /// <summary>
        /// Space Name
        /// </summary>
        public string Name
        {
            get
            {
                return DESCRIPTOR.Name;
            }
        }

        /// <summary>
        /// Space ID
        /// </summary>
        public short Id
        {
            get
            {
                return DESCRIPTOR.Id;
            }
        }

        /// <summary>
        /// Space owner
        /// </summary>
        public string Owner
        {
            get { return vm.ReadString(DEFS.SYSTEM_OWNER_ADDRESS, DEFS.SYSTEM_OWNER_LENGTH).Trim(); }
            set { vm.Write(DEFS.SYSTEM_OWNER_ADDRESS, value.PadRight((int)DEFS.SYSTEM_OWNER_LENGTH)); }
        }

        /////////////////////////////////////////////////////////////////////////////////////////
        ////////////////////////// PRIVATE METHODS //////////////////////////////////////////////
        /////////////////////////////////////////////////////////////////////////////////////////
        
        /////////////////////////////////////////////////////////////////////////////////////////
        ////////////////////////// ADMIN METHODS ////////////////////////////////////////////////
        /////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Get FBQE Header
        /// </summary>
        /// <returns></returns>
        internal string[] GetFreeSpaceInfo()
        {
            List<string> info = new List<string>();

            long fsp = 0;

            info.Add("Free space allocation for '" + this.Name + "':");
            info.Add(" ");
            info.Add("    #   Start address     End address          Length");
            int fn = fbqe_header.FIRST;
            int i = 0;
            while (fn >= 0)
            {
                i++;
                VSFreeSpaceManager.FBQE f = fbqe_header.GetFBQE(fn);
                string spf = "#,#;(#,#)";
                int padf = 15;
                info.Add(i.ToString().PadLeft(5) + " " + f.ADDRESS_START.ToString(spf).PadLeft(padf) + " " + f.ADDRESS_END.ToString(spf).PadLeft(padf) + " " + f.LENGTH.ToString(spf).PadLeft(padf));
                fsp += f.LENGTH;
                fn = f.NEXT;
            }
            info.Add(" ");
            info.Add("TOTAL FREE SPACE: " + fsp.ToString("#,0;(#,0)"));
            info.Add("DESCRIPTORS FREE: " + fbqe_header.FREE.ToString() + " of " + fbqe_header.MAX.ToString());

            return info.ToArray();
        }

        /// <summary>
        /// Get pool pointers - addresses of the 1st and last object
        /// </summary>
        /// <param name="pool"></param>
        /// <returns></returns>
        public long[] GetPoolPointers(short pool)
        {
            long[] ret = new long[2];
            /*
            if (pool <= 0)
            {
                long ha = (pool * -1 * 16) + DEFS.POOL_AREA_ADDRESS;
                ret[0] = vm.ReadLong(ha);
                ret[1] = vm.ReadLong(ha + 8);
            }
            else
            {

            }
             * */
            ret[0] = GetFirstAddress(pool);
            ret[1] = GetLastAddress(pool);
            return ret;
        }

        /// <summary>
        /// Error message
        /// </summary>
        public string Error
        {
            get { return error; }
        }

        /// <summary>
        /// Virtual Memory Manager
        /// </summary>
        internal VSVirtualMemoryManager VM
        {
            get { return vm; }
        }

        /////////////////////////////////////////////////////////////////////////
        /////////////////// Indexes management //////////////////////////////////
        /////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Check if index already exists (user - accessible)
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool IndexExists(string name)
        {
            if (IndexSpace != null)
                return IndexSpace.IndexExists(VSLib.PrepareFullIndexName(this.Name, name));
            else
                return index_exists(name, false);
        }

        /// <summary>
        /// Check if index already exists (full/udf list)
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        internal bool index_exists(string name, bool full = true)
        {
            if (IndexSpace != null)
                return IndexSpace.index_exists(this.Name + ":" + name, full);

            List<VSIndex> list = full ? index_list_full : index_list;

            for (int i = 0; i < list.Count; i++)
                if (list[i].Name == name.Trim().ToLower())
                    return true;

            return false;
        }


        /// <summary>
        /// Get index by name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public VSIndex GetIndex(string name)
        {
            if (IndexSpace != null)
                return IndexSpace.GetIndex(VSLib.PrepareFullIndexName(this.Name, name));
            else
                return get_index(name, false);
        }

        /// <summary>
        /// Get index by name (private)
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        internal VSIndex get_index(string name, bool full = true)
        {
            List<VSIndex> list = full ? index_list_full : index_list;

            for (int i = 0; i < list.Count; i++)
                if (list[i].Name == name.Trim().ToLower())
                    return list[i];

                return null;
        }


        /// <summary>
        /// Get index by id
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        internal VSIndex get_index(long id)
        {

            for (int i = 0; i < index_list_full.Count; i++)
                if (index_list_full[i].Id == id)
                {
                    return index_list_full[i];
                }

            throw new VSException(VSException.E0051_OPEN_INDEX_ERROR_CODE, "- index is not found - '" + id.ToString() + "'");
        }


        /// <summary>
        /// Get index names
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public string[] Indexes
        {
            get
            {
                if (IndexSpace != null)
                    return IndexSpace.get_indexes_for_space(this.Name);
                else
                    return
                        this.get_indexes_for_space("");
            }
        }
        
        /// <summary>
        /// Gey index names by space prefix
        /// </summary>
        /// <param name="space_name"></param>
        /// <returns></returns>
        private string[] get_indexes_for_space(string space_name)
        {
            List<string> lx = new List<string>();
            string nm = space_name.Trim().ToLower();
            
            for (int i = 0; i < index_list.Count; i++)
            {
                string[] parsed_name = new string[2];
                parsed_name[0] = VSLib.ParseIndexSpace(index_list[i].Name);
                parsed_name[1] = VSLib.ParseIndexName(index_list[i].Name);
                if (nm == "")
                {
                    if (parsed_name[0] == "")
                        lx.Add(parsed_name[1]);
                }
                else
                {
                    if (VSLib.Compare(nm,  parsed_name[0]))
                        lx.Add(parsed_name[1]);
                }

            }

            return lx.ToArray();
        }

        /// <summary>
        /// Index space (optional)
        /// </summary>
        internal VSpace IndexSpace
        {
            get { return index_space; }
            set { index_space = value; }
        }
        private VSpace index_space = null;


        /// <summary>
        /// Create index
        /// </summary>
        /// <param name="name"></param>
        public VSIndex CreateIndex(string name, bool unique)
        {
            string nm = name.Trim().ToLower();

            if (nm.IndexOf(DEFS.INDEX_CROSS_REFERENCES) >= 0)
                throw new VSException(VSException.E0050_CREATE_INDEX_ERROR_CODE, "- invalid index name: '" + nm + "'");


            if (IndexSpace != null)
                return IndexSpace.CreateIndex(VSLib.PrepareFullIndexName(this.Name, name), unique);
            else
            {
                if (index_exists(nm))
                    throw new VSException(VSException.E0050_CREATE_INDEX_ERROR_CODE, "- index already exists: '" + nm + "'");

                VSIndex ix = new VSIndex(this, nm, unique);

                index_list.Add(ix);
                
                index_list_full.Add(ix);

                set_ref_index(ix);

                return ix;
            }
        }

        /// <summary>
        /// Add/Create reference
        /// </summary>
        /// <param name="index"></param>
        private void set_ref_index(VSIndex index)
        {
            string[] parsed_name = new string [2];
            parsed_name[0] = VSLib.ParseIndexSpace(index.Name);
            parsed_name[1] = VSLib.ParseIndexName(index.Name);

            string ref_name = VSLib.PrepareFullIndexName(parsed_name[0], DEFS.INDEX_CROSS_REFERENCES);

            if (this.index_exists(ref_name))
                index.XRefs = this.get_index(ref_name);
            else
            {
                VSIndex ref_index = new VSIndex(this, ref_name, false);
                index.XRefs = ref_index;
                index_list_full.Add(ref_index);
            }
        }


        /// <summary>
        /// Delete existing index
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public void DeleteIndex(string name)
        {
            if (IndexSpace != null)
                IndexSpace.DeleteIndex(VSLib.PrepareFullIndexName(this.Name, name));
            else
            {

                VSIndex x = this.get_index(name);
                if (x == null)
                    throw new VSException(VSException.E0052_DELETE_INDEX_ERROR_CODE, "- index is not found - '" + name + "'");

                // Remove all references
                VSIndex ref_x = get_index(VSLib.PrepareFullIndexName(VSLib.ParseIndexSpace(name), DEFS.INDEX_CROSS_REFERENCES));
                x.Reset();
                while (x.Next())
                {
                    long[] refs = x.CurrentRefs;
                    for (int i = 0; i < refs.Length; i++)
                    {
                        byte[] key = VSLib.ConvertLongToByte(refs[i]);
                        ref_x.delete_node(key, x.CurrentAvlNode.ID);
                    }
                }

                index_list.Remove(x);
                index_list_full.Remove(x);
                x.purge();
            }
        }
    }

    /// <summary>
    /// Classe for FBQE cache
    /// </summary>

    class FBQE_CACHE
    {
        /// <summary>
        /// FBQE cache to speed up search
        /// </summary>
        struct CacheItem
        {
            public long ADDRESS_START;          // Free space start address
            public long ADDRESS_END;            // Free space end (1st byte after free space)
            public long LENGTH;                 // Free space length
            public int  INDEX;                  // FBQE index
        }

        // Cache fields
        private List<CacheItem> f_cache = null;                            // FBQE Cache

        VSFreeSpaceManager fbqe_header = null;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="fh">Header</param>
        public FBQE_CACHE(VSFreeSpaceManager fh)
        {
            fbqe_header = fh;
            f_cache = new List<CacheItem>(DEFS.FBQE_CACHE_SIZE);
        }

        /// <summary>
        /// Update cache element to the current status
        /// </summary>
        /// <param name="upd_index">FBQE index to update</param>
        internal void UpdateCache(int upd_index)
        {
            if (upd_index < 0)
                return;

            VSFreeSpaceManager.FBQE f = fbqe_header.GetFBQE(upd_index);

            for (int i = 0; i < f_cache.Count; i++)
                if (f_cache[i].INDEX == upd_index)
                {
                    f_cache.RemoveAt(i);
                    break;
                }

            if (f_cache.Count == DEFS.FBQE_CACHE_SIZE)
                f_cache.RemoveAt(0);

            if (f.ADDRESS_START > 0)
            {
                CacheItem fc = new CacheItem();
                fc.ADDRESS_START = f.ADDRESS_START;
                fc.ADDRESS_END = f.ADDRESS_END;
                fc.LENGTH = f.LENGTH;
                fc.INDEX = upd_index;
                f_cache.Add(fc);
            }
        }

        /// <summary>
        /// Find FBQE enough for allocation
        /// Criteria: FBQE lengh = request (best) or Min(FBQE lenght > request)
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        internal int FindFreeSpace(long length)
        {
            int index = -1;
            long tl = -1;

            for (int i = 0; i < f_cache.Count; i++)
            {
                if (f_cache[i].LENGTH == length)
                    return f_cache[i].INDEX;
                else if (f_cache[i].LENGTH > length)
                {
                    if ((tl < 0) | (f_cache[i].LENGTH < tl))
                    {
                        tl = f_cache[i].LENGTH;
                        index = f_cache[i].INDEX;
                    }
                }
            }
            return index;
        }
        
        /// <summary>
        /// Check cache to find start point to find place for new free space descriptor
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        internal int FindStartPoint(long address)
        {
            for (int i = 0; i < f_cache.Count; i++)
                if (address >= f_cache[i].ADDRESS_END)
                    return f_cache[i].INDEX;

            return fbqe_header.FIRST;                       // By default - 1st FBQUE in the queue
        }
    }
}
