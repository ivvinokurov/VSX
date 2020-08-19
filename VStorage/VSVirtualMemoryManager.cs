using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace VStorage
{
    public class VSVirtualMemoryManager
    {
        //public VSTimer __TIMER = new VSTimer();
        /**************************************************************************/
        /// <summary>
        /// PHYSICAL DATA ACCESS LAYER (VIRTUALIZATION) 
        /// 2017-06-29 In Memory Option added
        /// </summary>
        /**************************************************************************/

        /// <summary>
        /// Properties
        /// </summary>
        private int state;                          // 0 - closed; 1 - opened; 2 - locked
        private long page_size = 0;                 // Page size
        private bool imo = false;                   // In Memory Option

        // Partitions
        private partition_descriptor[] pd;

        // Pages segmentsPartitions
        private page_segment_descriptor[] segment_table;        // Segment trable
        private int segments_used = 0;                     // Number of used segments

        private long cache_size_bytes = 0;
        private long cache_size_pages = 0;

        private VSTransaction _ta;

        public string error = "";

        public long PGRead = 0;
        public long PGWrite = 0;

        private byte[] e_key = null;                   // Encryption buffer

        private byte[] e_buf = null;

        /// <summary>
        /// Space Header
        /// </summary>
        private VSCatalogDescriptor DESCRIPTOR = null;
        /// <summary>
        /// Partition Descriptor
        /// </summary>
        public struct partition_descriptor
        {
            public VSIO fs;
            public string file_name;
            public long start_page;
            public long end_page;
        }

        /// <summary>
        /// Page Segment Descriptor
        /// </summary>
        public struct page_segment_descriptor
        {
            public long start_page;
            public long end_page;
            public page_descriptor[] page_table;
            public page_buffer[] cache;

        }

        /// <summary>
        /// Page Descriptor
        /// </summary>
        public struct page_descriptor
        {
            public int page_state;                     //0 - not loaded; 1 - loaded; 2 - dirty
            public bool page_lock;                     //0 - unlocked; 1 - locked
            public int bufno;
        }

        /// <summary>
        /// Page Buffer
        /// </summary>
        public struct page_buffer
        {
            public long page_no;
            public long access_count;
            public long last_access;
            public byte[] buf;                          // Page buffer
            public byte[] tbuf;                         // Unchanged page (for transaction logging)

            // Queue
            public int prev;                            // Previous index
            public int next;                            // Next index
        }

        /// <summary>
        /// Buffer queues management
        /// Each array has 3 items:
        /// 0 - number
        /// 1 - first element
        /// 2 - last element
        /// </summary>
        private int[] q_free;       
        private int[] q_read;
        private int[] q_write;

        private const int number = 0;                   // Array index for pages number
        private const int first = 1;                    // Array index for 1st page in the queue
        private const int last = 2;                     // Array index for last page in the queue

        private const int MIN_DISPLACE_PAGES = 16;      // Min pages to free by algorithm
        private const int MAX_DISPLACE_PAGES = 32;      // Max pages to free by algorithm

        /// <summary>
        /// Constructor
        /// </summary>
        public VSVirtualMemoryManager()
        {
        }

        /// <summary>
        /// Open new address space: _path - root folder path; _cat_file - catalog file name; idx - index in the catalog. Return: space id or -1(if error)
        /// </summary>
        /// <param name="_path"></param>
        /// <param name="_cat_file"></param>
        /// <param name="idx"></param>
        /// <returns></returns>
        public string Open(VSCatalogDescriptor desc, VSTransaction ta)
        {
            this.DESCRIPTOR = desc;
            this.page_size = DESCRIPTOR.PageSize;
            this.imo = DESCRIPTOR.IMO;

            e_key = VSLib.ConvertStringToByte(DEFS.ENCRYPT_SPACE);

            e_buf = new byte[page_size];

            _ta = ta;
            
            //////////////////////////////////////////////
            ///////////// Initiate partitions ////////////
            //////////////////////////////////////////////
            this.pd = new partition_descriptor[DESCRIPTOR.Partitions];


            //Check if all partitions exists
            if (imo)
            {
                pd[0].start_page = 0;                                           //Start Page
                pd[0].end_page = DESCRIPTOR.space_size_pg - 1;                  //End Page
            }
            else
            {
                for (int i = 0; i < DESCRIPTOR.Partitions; i++)
                {
                    short j = (short)(i + 1);
                    pd[i].file_name = DESCRIPTOR.Path + "\\" + DEFS.SPACE_FILE_NAME(DESCRIPTOR.Name, i);

                    if (!System.IO.File.Exists(pd[i].file_name))
                        return "Error: space file is not found - " + pd[i].file_name;

                    //FileStream f = null;

                    //f = File.Open(pd[i].file_name, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

                    
                    pd[i].fs = new VSIO(pd[i].file_name, VSIO.FILE_MODE_OPEN, DEFS.ENCRYPT_SPACE);
                    pd[i].fs.SetEncryption(false);

                    pd[i].start_page = (i == 0) ? 0 : pd[i - 1].end_page + 1;                         //Start Page
                    pd[i].end_page = pd[i].start_page + ((pd[i].fs.GetLength()) / page_size) - 1;     //End Page
                }
            }

            //////////////////////////////////////////////
            ///////////// Initiate cache /////////////////
            //////////////////////////////////////////////

            // Create segment table
            segment_table = new page_segment_descriptor[DEFS.PAGE_SEGMENTS_NUMBER];

            // Create page table for 1st segment
            segment_table[0] = new page_segment_descriptor();
            segment_table[0].start_page = 0;
            segment_table[0].end_page = DESCRIPTOR.space_size_pg - 1;

            // Set initial number of segments used
            segments_used = 1;

            // Calculate cache size in pages
            if (imo)
            {
                cache_size_pages = DESCRIPTOR.space_size_pg;
                cache_size_bytes = DESCRIPTOR.SpaceSize;
            }
            else
            {
                string confs = VSLib.VSGetKey(DEFS.VM_CACHE_SIZE_KEY);
                if (confs == "")
                {
                    confs = DEFS.VM_CACHE_SIZE_DEFAULT;
                    VSLib.VSSetKey(DEFS.VM_CACHE_SIZE_KEY, confs);
                }
                int csize = VSLib.ConvertStringToInt(confs);

                cache_size_bytes = ((csize < 2) ? 2 : csize) * 1048576;

                if (cache_size_bytes < (page_size * 10))
                    cache_size_bytes = page_size * 10;

                cache_size_pages = (cache_size_bytes / page_size) + 1;

            }

            // Initialize cache
            segment_table[0].cache = new page_buffer[cache_size_pages];
            for (int i = 0; i < cache_size_pages; i++)
            {
                segment_table[0].cache[i].buf = new byte[page_size];
                segment_table[0].cache[i].tbuf = null;
                segment_table[0].cache[i].last_access = 0;
                segment_table[0].cache[i].access_count = 0;
                if (imo)
                {
                    segment_table[0].cache[i].page_no = i;                  // Page no = Buf no
                    segment_table[0].cache[i].prev = -1;
                    segment_table[0].cache[i].next = -1;
                }
                else
                {
                    segment_table[0].cache[i].page_no = -1;
                    segment_table[0].cache[i].prev = i - 1;
                    segment_table[0].cache[i].next = ((i + 1) == cache_size_pages) ? -1 : i + 1;
                }
            }

            // Initialize queues (not IMO)
            if (!imo)
            {
                q_read = new int[3];
                q_read[number] = 0;
                q_read[first] = -1;
                q_read[last] = -1;

                q_write = new int[3];
                q_write[number] = 0;
                q_write[first] = -1;
                q_write[last] = -1;

                q_free = new int[3];
                q_free[number] = (int)cache_size_pages;
                q_free[first] = 0;
                q_free[last] = (int)cache_size_pages - 1;
            }

            //////////////////////////////////////////////
            ///////////// Initiate page table ////////////
            //////////////////////////////////////////////
            segment_table[0].page_table = new page_descriptor[DESCRIPTOR.space_size_pg];
            for (int i = 0; i < segment_table[0].page_table.Length; i++)
            {
                if (imo)
                {
                    segment_table[0].page_table[i].page_state = DEFS.PAGE_READ;
                    segment_table[0].page_table[i].page_lock = DEFS.PAGE_LOCKED;
                    segment_table[0].page_table[i].bufno = i;                       // For IMO bufno = page table element
                }
                else
                {
                    segment_table[0].page_table[i].page_state = DEFS.PAGE_FREE;
                    segment_table[0].page_table[i].page_lock = DEFS.PAGE_UNLOCKED;
                    segment_table[0].page_table[i].bufno = -1;
                }
            }

            // Set state 'Opened'
            this.state = DEFS.SPACE_OPENED;

            // For IMO: write owner 'undefined'; otherwise: load and lock page 0.
            if (imo)
                this.Write(DEFS.SYSTEM_OWNER_ADDRESS, DEFS.SYSTEM_OWNER_UNDEFINED.PadRight((int)DEFS.SYSTEM_OWNER_LENGTH));
            else
            {
                fetch(0, 0, 0, DEFS.PAGE_READ);
                this.lock_page(0);                                                            // lock 1st page  
            }

            return "";
        }

        /// <summary>
        /// Close address space. Return: 0 - successful; -1 - error
        /// </summary>
        /// <returns></returns>
        public int Close()
        {
            if (!this.imo)
            {
                flush(-1);
                //flush data if required
                for (int i = 0; i < pd.Length; i++)
                {
                    pd[i].fs.Close();
                    pd[i].fs = null;
                }
            }

            pd = null;
            this.state = DEFS.SPACE_CLOSED;
            this.segment_table = null;
            return 0;
        }

        /// <summary>
        /// Roll back all changes in IMO mode
        /// </summary>
        public void RollBackIMO()
        {
            int sn = 0;     // Number of segment

            for (long i = 0; i < cache_size_pages; i++)
            {
                if (i > segment_table[sn].end_page)
                    sn++;       // Switch to the next segment if required
                long page_rel = i - segment_table[sn].start_page;
                if (segment_table[sn].cache[page_rel].tbuf != null)
                {
                    copy_buffer(ref segment_table[sn].cache[page_rel].buf, ref segment_table[sn].cache[page_rel].tbuf, 0, 0, page_size);
                    segment_table[sn].cache[page_rel].tbuf = null;
                }
            }
        }

        /// <summary>
        /// Dynamically extend partition (single-partition space only). 
        /// Return: 0 - successful; -1 - error.
        /// </summary>
        /// <returns></returns>
        public int Extend()
        {
            if (DESCRIPTOR.Partitions > 1)
            {
                this.error = "Cannot extend multi-partition space";
                return -1;
            }
            //Check if extension is defined while creating space
            else if (DESCRIPTOR.Extension == 0)
            {
                this.error = "Dynamic extension is not defined for this space";
                return -1;
            }


            // Initialize new segment
            int nidx = segments_used;
            segments_used++;
            segment_table[nidx] = new page_segment_descriptor();

            // Set start-end pages
            segment_table[nidx].start_page = segment_table[nidx - 1].end_page + 1;
            segment_table[nidx].end_page = segment_table[nidx].start_page + DESCRIPTOR.extension_pg - 1;

            // Create new page table
            long stp = segment_table[nidx].start_page;
            segment_table[nidx].page_table = new page_descriptor[DESCRIPTOR.extension_pg];
            for (int i = 0; i < segment_table[nidx].page_table.Length; i++)
            {
                if (imo)
                {
                    segment_table[nidx].page_table[i].page_state = DEFS.PAGE_READ;
                    segment_table[nidx].page_table[i].page_lock = DEFS.PAGE_LOCKED;
                    segment_table[nidx].page_table[i].bufno = (int)(stp + i);                       // For IMO bufno = page table element
                }
                else
                {
                    segment_table[nidx].page_table[i].page_state = DEFS.PAGE_FREE;
                    segment_table[nidx].page_table[i].page_lock = DEFS.PAGE_UNLOCKED;
                    segment_table[nidx].page_table[i].bufno = -1;
                }
            }


            //Add extension information
            short n = ReadShort(DEFS.SYSTEM_ALLOCATION_ADDRESS);                                // The number of pending updates
            long e_addr = DEFS.SYSTEM_ALLOCATION_ADDRESS + 2 + (n * 16);
            //Write(e_addr, DESCRIPTOR.Size);
            Write(e_addr, DESCRIPTOR.SpaceSize);                                                // Start addrress
            long end_address = DESCRIPTOR.SpaceSize + DESCRIPTOR.Extension;                     // End address + 1
            Write(e_addr + 8, end_address);

            // Update number of extensions
            n++;
            Write(DEFS.SYSTEM_ALLOCATION_ADDRESS, (short)n);

            // Save old physical size
            long old_size = DESCRIPTOR.SysSpaceSize;

            // Increase space size
            DESCRIPTOR.space_size_pg += DESCRIPTOR.extension_pg;


            if (imo)    // In Memory Option extension
            {
                // Create new cache (IMO)
                segment_table[nidx].cache = new page_buffer[DESCRIPTOR.extension_pg];
                for (int i = 0; i < segment_table[nidx].cache.Length; i++)
                {
                    segment_table[nidx].cache[i].buf = new byte[page_size];
                    segment_table[nidx].cache[i].tbuf = null;
                    segment_table[nidx].cache[i].last_access = 0;
                    segment_table[nidx].cache[i].access_count = 0;
                    segment_table[nidx].cache[i].page_no = i;                  // Page no = Buf no
                    segment_table[nidx].cache[i].prev = -1;
                    segment_table[nidx].cache[i].next = -1;
                }
            }
            else
            {
                // Append pages to the physical storage
                byte[] resv = new byte[8];
                byte[] dataArray = new byte[page_size];

                try
                {
                    for (int i = 0; i < DESCRIPTOR.extension_pg; i++)
                    {
                        long addr = old_size + (i * (DESCRIPTOR.PageSize + DEFS.SYSTEM_USED_PAGE_SPACE));

                        pd[0].fs.Write(addr, DEFS.DATA_NOT_ENCRYPTED);               // + 0 (4) Encryption indicator

                        pd[0].fs.Write(-1, (uint)0);                                // + 4 (4) CRC32 placeholder

                        pd[0].fs.Write(-1, ref resv);                               // +8 (8) reserve

                        pd[0].fs.Write(-1, ref dataArray);
                    }
                }
                catch (Exception e)
                {
                    this.error = "Error while extending file: " + e.Message;
                    return -1;
                }

                // Increase file descriptor pages
                pd[0].end_page += DESCRIPTOR.extension_pg;

                DESCRIPTOR.CATALOG.Save();
                if (DESCRIPTOR.SysSpaceSize != pd[0].fs.GetLength())
                    throw new Exception("Descriptor and file size doesnt match: " + DESCRIPTOR.SysSpaceSize.ToString() + " - " + pd[0].fs.GetLength().ToString());
            }
            return 0;
        } 

        /// <summary>
        /// Read/Write generic function   
        /// </summary>
        /// <param name="address">address - address to read/write</param>
        /// <param name="data">data - data to read/write</param>
        /// <param name="length">length - length of data</param>
        /// <param name="op">PAGE_READ(1)
        ///                  PAGE_WRITE(2)
        ///</param>
        /// <param name="fill_byte">value to use by fill (PAGE_FILL mode)</param>
        /// <returns></returns>
        internal void Io(long address, ref byte[] data, long length, int op, long offset = 0)
        {
            // 1. Check if address is in the space
            if (((address + length) > this.Size) | (address < 0))
                throw new VSException(DEFS.E0007_INVALID_ADDRESS_CODE, "- " + address.ToString());


            // 2. Start transaction if op=write and not started yet
            if ((!Transaction.Started) & (!Transaction.RollMode) & (op == DEFS.PAGE_WRITE))
                Transaction.Begin();

            // 3. Perform op
            long l_address = address;
            long l_remain = length;                                  // remaining length to write
            long l_offset = 0;

            while (l_remain > 0)
            {
                long l_pageno = l_address / page_size;                  // current page number
                long l_shift = l_address - (l_pageno * page_size);      // address inside the page
                long wsize = (l_remain > (page_size - l_shift)) ? (page_size - l_shift) : l_remain;

                int sg = find_segment(l_pageno);
                int pg = (int)(l_pageno - segment_table[sg].start_page);
                if (imo)
                { 
                    int bufno = (int)pg;

                    if (op == DEFS.PAGE_WRITE)
                    {
                        copy_buffer(ref segment_table[sg].cache[bufno].buf, ref data, l_shift, offset + l_offset, wsize);
                    }
                    else
                    {
                        copy_buffer(ref data, ref segment_table[sg].cache[bufno].buf, offset + l_offset, l_shift, wsize);
                    }
                }
                else
                {
                    //__TIMER.START("FETCH");

                    fetch(l_pageno, sg, pg, op);        //Run algorithm to fetch pagw

                    //__TIMER.END("FETCH");

                    int bufno = segment_table[sg].page_table[pg].bufno;

                    if (op == DEFS.PAGE_WRITE)
                    {
                        copy_buffer(ref segment_table[0].cache[bufno].buf, ref data, l_shift, offset + l_offset, wsize);
                    }
                    else
                    {
                        copy_buffer(ref data, ref segment_table[0].cache[bufno].buf, offset + l_offset, l_shift, wsize);
                    }
                }

                l_remain = l_remain - wsize;            // decrease remain
                l_address = l_address + wsize;          // shift address
                l_offset = l_offset + wsize;
            }
            //__TIMER.END("IO");

        }

        /// <summary>
        /// Move item between queues
        /// </summary>
        /// <param name="to"></param>
        /// <param name="from"></param>
        /// <param name="n"></param>
        private void move_queue(ref int[] to, ref int[] from, int n)
        {
            // Remove from the old queue
            if (segment_table[0].cache[n].prev < 0)
                from[first] = segment_table[0].cache[n].next;                                   // Update first
            else
                segment_table[0].cache[segment_table[0].cache[n].prev].next = segment_table[0].cache[n].next;           // Update prev

            if (segment_table[0].cache[n].next < 0)
                from[last] = segment_table[0].cache[n].prev;                                    // Update last
            else
                segment_table[0].cache[segment_table[0].cache[n].next].prev = segment_table[0].cache[n].prev;           // Update next

            from[number]--;

            // Add to the new queue
            segment_table[0].cache[n].prev = -1;
            segment_table[0].cache[n].next = to[first];

            if (to[first] >= 0)
                segment_table[0].cache[to[first]].prev = n;

            to[first] = n;

            if (to[last] < 0)
                to[last] = n;

            to[number]++;
        }

        /// <summary>
        /// Fetch page by number: n - page number; mode - PAGE_READ(1) or PAGEWRITE(2)
        /// </summary>
        /// <param name="page_no"></param>
        /// <param name="mode"></param>
        public void fetch(long page_no, int sg, int pg, int mode)
        {
            int nbuf = -1;
            if (segment_table[sg].page_table[pg].page_state == mode)                            // Page is already in buffer
                nbuf = segment_table[sg].page_table[pg].bufno;
            else
            {
                //Check if page already loaded
                if (segment_table[sg].page_table[pg].page_state != DEFS.PAGE_FREE)                  // Page is already in buffer
                {
                    nbuf = segment_table[sg].page_table[pg].bufno;
                    if (mode > segment_table[sg].page_table[pg].page_state)
                    {
                        move_queue(ref q_write, ref q_read, nbuf);                      // Move to 'write' queue if in 'read' queue
                        segment_table[sg].page_table[pg].page_state = DEFS.PAGE_WRITE;
                    }
                }
                else
                {
                    if (q_free[first] < 0)
                    {
                        if (q_read[number] < MIN_DISPLACE_PAGES)
                        {
                            int count = MAX_DISPLACE_PAGES - q_read[number];    // Number of pages to flush
                            for (int i = 0; i < count; i++)
                            {
                                flush((int)q_write[last]);
                            }
                        }

                        for (int i = 0; i < MIN_DISPLACE_PAGES; i++)
                        {
                            int cl = 0;

                            long page_n = segment_table[0].cache[q_read[last]].page_no;     // Page# for last in 'read' queue
                            long segm_n = find_segment(page_n);                             // Segment#
                            long page_r = page_n - segment_table[segm_n].start_page;        // Relative page

                            // Make queue shift if last 'read' queue page is locked (one or more)
                            while (segment_table[segm_n].page_table[page_r].page_lock)
                            {
                                move_queue(ref q_read, ref q_read, q_read[last]);       // If last is locked move to the top of the queue
                                cl++;
                                if (cl > segment_table[0].cache.Length)
                                {
                                    cl = -1;
                                    break;
                                }
                                page_n = segment_table[0].cache[q_read[last]].page_no;     // Page# for last in 'read' queue
                                segm_n = find_segment(page_n);                             // Segment#
                                page_r = page_n - segment_table[segm_n].start_page;        // Relative page
                            }

                            if (cl < 0)
                                break;

                            free_page((int)page_n);

                            if (q_read[last] < 0)
                                break;

                            page_n = segment_table[0].cache[q_read[last]].page_no;     // Page# for last in 'read' queue
                            segm_n = find_segment(page_n);                             // Segment#
                            page_r = page_n - segment_table[segm_n].start_page;        // Relative page

                        }
                    }

                    nbuf = q_free[first];
                    page_read(page_no, sg, pg, nbuf);              // Read page into buffer

                    if (mode == DEFS.PAGE_READ)
                        move_queue(ref q_read, ref q_free, nbuf);                       // Move to 'write' queue if in 'read' queue
                    else
                        move_queue(ref q_write, ref q_free, nbuf);                       // Move to 'write' queue if in 'read' queue
                }

                if (mode == DEFS.PAGE_WRITE)
                {
                    segment_table[sg].page_table[pg].page_state = DEFS.PAGE_WRITE;

                    if (segment_table[0].cache[nbuf].tbuf == null)
                    {
                        segment_table[0].cache[nbuf].tbuf = new byte[segment_table[0].cache[nbuf].buf.Length];
                        copy_buffer(ref segment_table[0].cache[nbuf].tbuf, ref segment_table[0].cache[nbuf].buf, 0, 0, page_size);
                    }
                }
            }

            segment_table[0].cache[nbuf].access_count++;
            segment_table[0].cache[nbuf].last_access = DateTime.Now.Ticks;
        }

        /// <summary>
        /// Move page buffer to the free queue
        /// !!! NOT APPLICABLE TO IMO
        /// </summary>
        /// <param name="page_n"></param>
        private void free_page(int page_n)
        {
            long sg = find_segment(page_n);
            long pg = page_n - segment_table[sg].start_page;

            int old_state = segment_table[sg].page_table[pg].page_state;
            int buf_n = segment_table[sg].page_table[pg].bufno;

            segment_table[sg].page_table[pg].page_state = DEFS.PAGE_FREE;
            segment_table[sg].page_table[pg].bufno = -1;

            if (old_state == DEFS.PAGE_READ)
                move_queue(ref q_free, ref q_read, buf_n);
            else
                move_queue(ref q_free, ref q_write, buf_n);

            segment_table[0].cache[buf_n].page_no = -1;
            segment_table[0].cache[buf_n].last_access = 0;
            segment_table[0].cache[buf_n].access_count = 0;
        }

        /// <summary>
        /// Flush all
        /// !!! NOT APPLICABLE TO IMO
        /// </summary>
        public void flush()
        {
            flush(-1);
        }

        /// <summary>
        /// Flush one or all pages to disk: n=-1 - all pages; n>=0 - only buffer #n
        /// !!! NOT APPLICABLE TO IMO
        /// </summary>
        /// <param name="buf_n"></param>
        private void flush(int buf_n)
        {
            int startn = 0;
            int count = (int)cache_size_pages;

            if (buf_n >= 0)
            {
                startn = buf_n;
                count = 1;
            }
            for (long i = startn; i < (startn + count); i++)
            {

                long pn = segment_table[0].cache[i].page_no;                // Persistent cache is always in segment 0. Get page#
                if (pn >= 0)
                {
                    long sg = find_segment(pn);
                    long pg = pn - segment_table[sg].start_page;
                    if (segment_table[sg].page_table[pg].page_state == DEFS.PAGE_WRITE)
                    {
                        // Write transaction data
                        flush_ta((int)i);

                        // Write page
                        page_write(pn, (int)i);
                        segment_table[sg].page_table[pg].page_state = DEFS.PAGE_READ;
                        move_queue(ref q_read, ref q_write, (int)i);
                    }
                }
            }
        }

        /// <summary>
        /// Write transaction record for buffer if transaction is in progress.
        /// Cleanup buffwer after that
        /// !!! NOT APPLICABLE TO IMO
        /// </summary>
        /// <param name="rec"></param>
        private void flush_ta(int bufno)
        {
            if (Transaction != null)
            {
                if (Transaction.Started)
                {
                    if (segment_table[0].cache[bufno].tbuf != null)
                    {
                        long address = segment_table[0].cache[bufno].page_no * page_size;

                        Transaction.WriteRecord(DESCRIPTOR.Id, address, ref segment_table[0].cache[bufno].tbuf);

                        segment_table[0].cache[bufno].tbuf = null;
                    }
                }
            }
        }

        /// <summary>
        /// Read page into buffer: n - page number; nbuf - buffer number
        /// !!! NOT APPLICABLE TO IMO
        /// </summary>
        /// <param name="n"></param>
        /// <param name="nbuf"></param>
        private void page_read(long n, int sg, int pg, int nbuf)
        {
            for (int i = 0; i < pd.Length; i++)
            {
                if ((n >= pd[i].start_page) & (n <= pd[i].end_page))
                {
                    // Absolute address (beginning of system area
                    long addr = (long)((n - pd[i].start_page) * (DESCRIPTOR.PageSize + DEFS.SYSTEM_USED_PAGE_SPACE));

                    uint encr = pd[i].fs.ReadUInt(addr);        // Encryption

                    uint old_crc = pd[i].fs.ReadUInt(addr + 4); // Crc32

                    if (encr == DEFS.DATA_ENCRYPTED)
                    {
                        e_buf = pd[i].fs.ReadBytes(addr + DEFS.SYSTEM_USED_PAGE_SPACE, (int)page_size);

                        VSCrypto.Decrypt(ref e_buf, ref segment_table[0].cache[nbuf].buf, e_key, 0);
                    }
                    else
                        segment_table[0].cache[nbuf].buf = pd[i].fs.ReadBytes(addr + DEFS.SYSTEM_USED_PAGE_SPACE, (int)page_size);

                    if (old_crc != 0)
                    {
                        uint new_crc = VSCRC32.CountCrc(segment_table[0].cache[nbuf].buf);
                        if (old_crc != new_crc)
                            throw new VSException(DEFS.E0033_INVALID_CRC_CODE, "Space: " + DESCRIPTOR.name + ", Page#: " + n.ToString());
                    }

                    PGRead++;

                    // Update page table entry by but# ans sett 'read' state
                    segment_table[sg].page_table[pg].page_state = DEFS.PAGE_READ;
                    segment_table[sg].page_table[pg].bufno = nbuf;

                    // Update cache entry by page#
                    segment_table[0].cache[nbuf].page_no = n;

                }
            }
        }

        /// <summary>
        /// Write page from buffer: n - page number; nbuf - buffer number
        /// !!! NOT APPLICABLE TO IMO
        /// </summary>
        /// <param name="n"></param>
        /// <param name="nbuf"></param>
        private void page_write(long n, int nbuf)
        {
            for (int i = 0; i < pd.Length; i++)
            {
                if ((n >= pd[i].start_page) & (n <= pd[i].end_page))
                {
                    long addr = (long)((n - pd[i].start_page) * (DESCRIPTOR.PageSize + DEFS.SYSTEM_USED_PAGE_SPACE));

                    uint crc32 = VSCRC32.CountCrc(segment_table[0].cache[nbuf].buf);

                    pd[i].fs.Write(addr + 4, crc32);                         // CRC32

                    if (DESCRIPTOR.CATALOG.ENCRYPT)
                    {
                        pd[i].fs.Write(addr, DEFS.DATA_ENCRYPTED);           // Encryption
                        VSCrypto.Encrypt(ref segment_table[0].cache[nbuf].buf, ref e_buf, e_key, 0);
                        pd[i].fs.Write(addr + DEFS.SYSTEM_USED_PAGE_SPACE, ref e_buf);
                    }
                    else
                    {
                        pd[i].fs.Write(addr, DEFS.DATA_NOT_ENCRYPTED);       // Encryption
                        pd[i].fs.Write(addr + DEFS.SYSTEM_USED_PAGE_SPACE, ref segment_table[0].cache[nbuf].buf);
                    }

                    PGWrite++;
                }
            }
        }

        /// <summary>
        /// Lock page: npage - page number
        /// !!! NOT APPLICABLE TO IMO
        /// </summary>
        /// <param name="npage"></param>
        private void lock_page(int npage)
        {
            if (!imo)
                if (this.state > DEFS.SPACE_CLOSED)
                {
                    long sg = find_segment(npage);
                    long pg = npage - segment_table[sg].start_page;

                    if (segment_table[sg].page_table[pg].page_state > DEFS.PAGE_FREE)
                    {
                        segment_table[sg].page_table[pg].page_lock = DEFS.PAGE_LOCKED;
                    }
                }
        }

        /**************************************************************************/
        /**************************************************************************/
        /// <summary>
        /// GetSpace space size (bytes)
        /// </summary>
        /// <returns></returns>
        public long Size
        {
            get { return (DESCRIPTOR.SpaceSize); }
        }

        ///////////////////////////////////////////////////////////
        /////////// READ METHODS       ////////////////////////////
        ///////////////////////////////////////////////////////////

        /// <summary>
        /// Read data (bytes)
        /// </summary>
        /// <param name="address"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public byte[] ReadBytes(long address, long length)
        {
            byte[] data = new byte[length];
            Io(address, ref data, length, DEFS.OP_READ);
            return data;
        }

        /// <summary>
        /// Read byte
        /// </summary>
        /// <param name="address">Relative address</param>
        /// <param name="length"></param>
        /// <returns></returns>
        public byte ReadByte(long address)
        {
            return ReadBytes(address, 1)[0];
        }

        /// <summary>
        /// Read string
        /// </summary>
        /// <param name="address"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public string ReadString(long address, long length)
        {
            return VSLib.ConvertByteToString(ReadBytes(address, length));
        }

        /// <summary>
        /// Read int
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public int ReadInt(long address)
        {
            return VSLib.ConvertByteToInt(ReadBytes(address, 4));
        }

        /// <summary>
        /// Read long
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public long ReadLong(long address)
        {
            return VSLib.ConvertByteToLong(ReadBytes(address, 8));
        }

        /// <summary>
        /// Read short
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public short ReadShort(long address)
        {
            return VSLib.ConvertByteToShort(ReadBytes(address, 2));
        }

        /// <summary>
        /// Read u=short
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public ushort ReadUShort(long address)
        {
            return VSLib.ConvertByteToUShort(ReadBytes(address, 2));
        }

        /// <summary>
        /// Read datetime
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public DateTime ReadDateTime(long address)
        {
            return new DateTime(ReadLong(address));
        }

        /////////////////////////////////////////////////////////
        //////////// WRITE METHODS //////////////////////////////
        /////////////////////////////////////////////////////////
        /// <summary>
        /// Write data (byte)
        /// </summary>
        /// <param name="address"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        internal void Write(long address, byte data)
        {
            byte[] b = new byte[1];
            b[0] = data;
            Write(address, b, 1);
        }

        /// <summary>
        /// Write data (int)
        /// </summary>
        /// <param name="address"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public void Write(long address, int data)
        {
            Write(address, VSLib.ConvertIntToByte(data), 4);
        }

        /// <summary>
        /// Write data (short)
        /// </summary>
        /// <param name="address"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public void Write(long address, short data)
        {
            Write(address, VSLib.ConvertShortToByte(data), 2);
        }

        /// <summary>
        /// Write data (int)
        /// </summary>
        /// <param name="address"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public void Write(long address, ushort data)
        {
            Write(address, VSLib.ConvertUIntToByte(data), 2);
        }

        /// <summary>
        /// Write data (long)
        /// </summary>
        /// <param name="address"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public void Write(long address, long data)
        {
            Write(address, VSLib.ConvertLongToByte(data), 8);
        }

        /// <summary>
        /// Write data (bytes)
        /// </summary>
        /// <param name="address"></param>
        /// <param name="data"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public void Write(long address, byte[] data, long length, long offset = 0)
        {
            Io(address, ref data, length, DEFS.OP_WRITE, offset: offset);
        }

        /// <summary>
        /// Write data (string)
        /// </summary>
        /// <param name="address"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public void Write(long address, string data)
        {
            Write(address, VSLib.ConvertStringToByte(data), data.Length);
        }

        /// <summary>
        /// Write DateTime
        /// </summary>
        /// <param name="address"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public void Write(long address, DateTime data)
        {
            Write(address, VSLib.ConvertLongToByte(data.Ticks), 8);
        }

        ///////////////////////////////////////////////////////////
        ///////////////////////////////////////////////////////////

        private void copy_buffer(ref byte[] target, ref byte[] source, long target_offset = 0, long source_offset = 0, long length = 0)
        {
            Buffer.BlockCopy(source, (int)source_offset, target, (int)target_offset, (int)length);
            //bool rc = false;
            //for (int i = 0; i < length; i++)
                //if (target[target_offset + i] != source[source_offset + i])
                //{
              //      target[target_offset + i] = source[source_offset + i];
                //    rc = true;
                //}
            //return rc;
        }


        /// <summary>
        /// Transaction
        /// </summary>
        public VSTransaction Transaction
        {
            get { return _ta; }
        }


        /// <summary>
        /// Find page segment using golden cut method
        /// </summary>
        private int find_segment(long page_no)
        {
            /*
            int QQ = 0;
            if ((page_no == 80) & (segments_used > 1))
            {
                QQ++;
            }
            QQ = 1;
            */
            double GR = 1.618;
            int n = segments_used;

            int lo = 0;
            int hi = n - 1;
            int ix = 0;

            while (lo <= hi)
            {

                ix = (int)((hi - lo) / GR);

                int k = lo + ix;

                if ((page_no >= segment_table[k].start_page) & (page_no <= segment_table[k].end_page))
                    return k;
                else if (segment_table[k].end_page < page_no)
                    lo = lo + ix + 1;
                else
                    hi = lo + ix - 1;
            }

            return -1;
        }

    }
}
