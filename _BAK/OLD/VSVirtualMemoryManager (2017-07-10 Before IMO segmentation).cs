using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace VStorage
{
    class VSVirtualMemoryManager
    {
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
        private int space_mode;                     // Open mode   
        private long page_size = 0;                 // Page size
        private bool imo = false;                   // In Memory Option

        private partition_descriptor[] pd;          // Partitions 
        private page_descriptor[] page_table;
        private page_buffer[] cache;
        private VSTransaction _ta;

        public string error = "";

        public long PGRead = 0;
        public long PGWrite = 0;

        /// <summary>
        /// Space Header
        /// </summary>
        private VSCatalogDescriptor DESCRIPTOR = null;
        /// <summary>
        /// Partition Descriptor
        /// </summary>
        public struct partition_descriptor
        {
            public FileStream fs;
            public string file_name;
            public long start_page;
            public long end_page;
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

        private const int number = 0;
        private const int first = 1;
        private const int last = 2;

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
        public string Open(VSCatalogDescriptor desc, int mode, VSTransaction ta)
        {
            this.space_mode = mode;
            this.DESCRIPTOR = desc;
            this.page_size = DESCRIPTOR.PageSize;
            this.imo = DESCRIPTOR.IMO;

            _ta = ta;

            //////////////////////////////////////////////
            ///////////// Initiate partitions ////////////
            //////////////////////////////////////////////
            this.pd = new partition_descriptor[DESCRIPTOR.Partitions];

            //Check if all partitions exists
            if (imo)
            {
                pd[0].start_page = 0;                                           //Start Page
                pd[0].end_page = DESCRIPTOR.SpaceSizePages - 1;                 //End Page
            }
            else
            {
                for (int i = 0; i < DESCRIPTOR.Partitions; i++)
                {
                    short j = (short)(i + 1);
                    pd[i].file_name = DESCRIPTOR.Path + "\\" + DEFS.SPACE_FILE_NAME(DESCRIPTOR.Name, i);

                    if (!System.IO.File.Exists(pd[i].file_name))
                        return "Error: space file is not found - " + pd[i].file_name;

                    if (space_mode == DEFS.MODE_OPEN_READ)
                        pd[i].fs = File.Open(pd[i].file_name, FileMode.Open, FileAccess.Read, FileShare.Read);
                    else
                        pd[i].fs = File.Open(pd[i].file_name, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

                    pd[i].start_page = (i == 0) ? 0 : pd[i - 1].end_page + 1;                              //Start Page
                    pd[i].end_page = pd[i].start_page + ((pd[i].fs.Length) / page_size) - 1;     //End Page
                }
            }

            //////////////////////////////////////////////
            ///////////// Initiate cache /////////////////
            //////////////////////////////////////////////

            // Calculate cache size in pages
            long cache_size_bytes = 0;
            if (imo)
            {
                cache_size_bytes = page_size * DESCRIPTOR.SpaceSizePages;
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
            }

            if (cache_size_bytes < (page_size * 10))
                cache_size_bytes = page_size * 10;

            long CACHE_SIZE = (cache_size_bytes / page_size) + 1;

            this.cache = new page_buffer[CACHE_SIZE];
            for (int i = 0; i < CACHE_SIZE; i++)
            {
                this.cache[i].buf = new byte[page_size];
                this.cache[i].tbuf = null;
                this.cache[i].last_access = 0;
                this.cache[i].access_count = 0;
                if (imo)
                {
                    this.cache[i].page_no = i;                  // Page no = Buf no
                    this.cache[i].prev = -1;
                    this.cache[i].next = -1;
                }
                else
                {
                    this.cache[i].page_no = -1;
                    this.cache[i].prev = i - 1;
                    this.cache[i].next = ((i + 1) == CACHE_SIZE) ? -1 : i + 1;
                }
            }


            q_read = new int[3];
            q_read[number] = 0;
            q_read[first] = -1;
            q_read[last] = -1;

            q_write = new int[3];
            q_write[number] = 0;
            q_write[first] = -1;
            q_write[last] = -1;

            q_free = new int[3];
            if (imo)
            {
                q_free[number] = 0;
                q_free[first] = -1;
                q_free[last] = -1;

            }
            else
            {
                q_free[number] = (int)CACHE_SIZE;
                q_free[first] = 0;
                q_free[last] = (int)CACHE_SIZE - 1;
            }
            //////////////////////////////////////////////
            ///////////// Initiate page table ////////////
            //////////////////////////////////////////////
            this.page_table = new page_descriptor[DESCRIPTOR.SpaceSizePages];
            for (int i = 0; i < this.page_table.Length; i++)
            {
                if (imo)
                {
                    this.page_table[i].page_state = DEFS.PAGE_READ;
                    this.page_table[i].page_lock = DEFS.PAGE_LOCKED;
                    this.page_table[i].bufno = i;                       // For IMO bufno = page table element
                }
                else
                {
                    this.page_table[i].page_state = DEFS.PAGE_FREE;
                    this.page_table[i].page_lock = DEFS.PAGE_UNLOCKED;
                    this.page_table[i].bufno = -1;
                }
            }

            this.state = DEFS.SPACE_OPENED;
            if (imo)
                this.Write(DEFS.SYSTEM_OWNER_ADDRESS, DEFS.SYSTEM_OWNER_UNDEFINED.PadRight((int)DEFS.SYSTEM_OWNER_LENGTH)); // Write owner 'undefined'

            fetch(0, DEFS.PAGE_READ);
            this.lock_page(0);                                                            // lock 1st page  
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
            this.cache = null;
            return 0;
        }

        /// <summary>
        /// Roll back all changes in IMO mode
        /// </summary>
        public void RollBackIMO()
        {
            for (int i = 0; i < cache.Length; i++)
            {
                if (cache[i].tbuf != null)
                {
                    copy_buffer(ref this.cache[i].buf, ref this.cache[i].tbuf, 0, 0, page_size);
                    this.cache[i].tbuf = null;
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
            int rc = 0;
            if (DESCRIPTOR.Partitions > 1)
            {
                this.error = "Cannot extend multi-partition space";
                rc = -1;
            }
            //Check if extension is defined while creating space
            else if (DESCRIPTOR.Extension == 0)
            {
                this.error = "Dynamic extension is not defined for this space";
                rc = -1;
            }
            else
            {
                if (imo)    // In Memory Option extension
                {
                    // Save old cache
                    page_descriptor[] old_page_table = page_table;
                    page_buffer[] old_cache = cache;

                    DESCRIPTOR.SpaceSize += DESCRIPTOR.Extension;

                    Close();
                    Open(DESCRIPTOR, space_mode, _ta);

                    // Restore old page table
                    for (int i = 0; i < old_page_table.Length; i++)
                        page_table[i] = old_page_table[i];

                    old_page_table = null;

                    // Restore old cache
                    for (int i = 0; i < old_cache.Length; i++)
                        cache[i] = old_cache[i];

                    old_cache = null;
                }
                else
                {
                    //Append pages
                    byte[] dataArray = new byte[page_size];
                    try
                    {
                        for (int i = 0; i < DESCRIPTOR.ExtensionPages; i++)
                        {
                            long p_addr = DESCRIPTOR.SpaceSize + (i * page_size);
                            pd[0].fs.Seek(p_addr, SeekOrigin.Begin);
                            pd[0].fs.Write(dataArray, 0, (int)page_size);
                        }
                    }
                    catch (Exception e)
                    {
                        this.error = "Error while extending file: " + e.Message;
                        rc = -1;
                    }
                    if (rc == 0)
                    {
                        //Add extension information
                        short n = ReadShort(DEFS.SYSTEM_ALLOCATION_ADDRESS);       //The number of pending updates
                        long e_addr = DEFS.SYSTEM_ALLOCATION_ADDRESS + 2 + (n * 16);
                        Write(e_addr, DESCRIPTOR.SpaceSize);
                        long end_address = DESCRIPTOR.SpaceSize + DESCRIPTOR.Extension;
                        Write(e_addr + 8, end_address);

                        //Update space size in catalog
                        n++;
                        Write(DEFS.SYSTEM_ALLOCATION_ADDRESS, (short)n);

                        DESCRIPTOR.SpaceSize += DESCRIPTOR.Extension;
                        DESCRIPTOR.CATALOG.Save();

                        Close();
                        Open(DESCRIPTOR, space_mode, _ta);
                        //>>>>>
                        if (DESCRIPTOR.SpaceSize != pd[0].fs.Length)
                            throw new Exception("Descriptor and file size doesnt match: " + DESCRIPTOR.SpaceSize.ToString() + " - " + pd[0].fs.Length.ToString());
                    }
                }
            }
            return rc;
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
        internal void io(long address, ref byte[] data, long length, int op, long offset = 0)
        {
            // 1. Check if read-only mode and op is write
            if ((op == DEFS.OP_WRITE) & (space_mode == DEFS.MODE_OPEN_READ))
                throw new VSException(VSException.E0010_READ_ONLY_CODE);

            // 2. Check if address is in the space
            if (((address + length) > this.GetSpaceSize()) | (address < 0))
                throw new VSException(VSException.E0007_INVALID_ADDRESS_CODE, "- " + address.ToString());


            // 3. Start transaction if op=write and not started yet
            if ((!Transaction.Started) & (!Transaction.RollMode) & (op == DEFS.PAGE_WRITE))
                Transaction.Begin();

            // 4. Perform op
            long l_address = address;
            long l_remain = length;                                  // remaining length to write
            long l_offset = 0;

            while (l_remain > 0)
            {
                long l_pageno = l_address / page_size;                  // current page number
                long l_shift = l_address - (l_pageno * page_size);      // address inside the page
                long wsize = (l_remain > (page_size - l_shift)) ? (page_size - l_shift) : l_remain;

                fetch(l_pageno, op);        //Run algorithm to fetch pagw

                int bufno = this.page_table[l_pageno].bufno;

                if (op == DEFS.PAGE_WRITE)
                {
                    copy_buffer(ref this.cache[bufno].buf, ref data, l_shift, offset + l_offset, wsize);
                }
                else
                {
                    copy_buffer(ref data, ref this.cache[bufno].buf, offset + l_offset, l_shift, wsize);
                }

                l_remain = l_remain - wsize;            // decrease remain
                l_address = l_address + wsize;          // shift address
                l_offset = l_offset + wsize;
            }
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
            if (this.cache[n].prev < 0)
                from[first] = this.cache[n].next;                                   // Update first
            else
                this.cache[this.cache[n].prev].next = this.cache[n].next;           // Update prev

            if (this.cache[n].next < 0)
                from[last] = this.cache[n].prev;                                    // Update last
            else
                this.cache[this.cache[n].next].prev = this.cache[n].prev;           // Update next

            from[number]--;

            // Add to the new queue
            this.cache[n].prev = -1;
            this.cache[n].next = to[first];

            if (to[first] >= 0)
                this.cache[to[first]].prev = n;

            to[first] = n;

            if (to[last] < 0)
                to[last] = n;

            to[number]++;
        }

        /// <summary>
        /// Fetch page by number: n - page number; mode - PAGE_READ(1) or PAGEWRITE(2)
        /// </summary>
        /// <param name="n"></param>
        /// <param name="mode"></param>
        public void fetch(long n, int mode)
        {
            //Check if page already loaded
            int nbuf = -1;
            if (this.page_table[n].page_state != DEFS.PAGE_FREE)                  // Page is already in buffer
            {
                nbuf = this.page_table[n].bufno;
                if (mode > this.page_table[n].page_state)
                {
                    if (imo)                // In Memory Option: if transaction buffer is empty - create it and backup initial page content
                    {
                        if (this.cache[nbuf].tbuf == null)
                        {
                            this.cache[nbuf].tbuf = new byte[this.cache[nbuf].buf.Length];
                            copy_buffer(ref this.cache[nbuf].tbuf, ref this.cache[nbuf].buf, 0, 0, page_size);
                        }

                    }
                    else
                    {
                        move_queue(ref q_write, ref q_read, nbuf);                      // Move to 'write' queue if in 'read' queue
                        this.page_table[this.cache[nbuf].page_no].page_state = DEFS.PAGE_WRITE;
                    }
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
                        while (this.page_table[this.cache[q_read[last]].page_no].page_lock)
                        {
                            move_queue(ref q_read, ref q_read, q_read[last]);       // If last is locked move to the top of the queue
                            cl++;
                            if (cl > this.cache.Length)
                            {
                                cl = -1;
                                break;
                            }
                        }

                        if (cl < 0)
                            break;
                        free_page((int)this.cache[q_read[last]].page_no);
                    }
                }

                nbuf = q_free[first];
                PageRead(n, nbuf);              // Read page into buffer

                if (mode == DEFS.PAGE_READ)
                    move_queue(ref q_read, ref q_free, nbuf);                       // Move to 'write' queue if in 'read' queue
                else
                    move_queue(ref q_write, ref q_free, nbuf);                       // Move to 'write' queue if in 'read' queue

            }

            if (mode == DEFS.PAGE_WRITE)
            {
                if (this.page_table[this.cache[nbuf].page_no].page_state == DEFS.PAGE_READ)
                    this.page_table[this.cache[nbuf].page_no].page_state = DEFS.PAGE_WRITE;

                if (this.cache[nbuf].tbuf == null)
                {
                    this.cache[nbuf].tbuf = new byte[this.cache[nbuf].buf.Length];
                    copy_buffer(ref this.cache[nbuf].tbuf, ref this.cache[nbuf].buf, 0, 0, page_size);
                }
            }

            this.cache[nbuf].access_count++;
            this.cache[nbuf].last_access = DateTime.Now.Ticks;
        }

        /// <summary>
        /// Move page buffer to the free queue
        /// </summary>
        /// <param name="page_n"></param>
        private void free_page(int page_n)
        {
            int old_state = this.page_table[page_n].page_state;
            int buf_n = this.page_table[page_n].bufno;

            this.page_table[page_n].page_state = DEFS.PAGE_FREE;
            this.page_table[page_n].bufno = -1;

            if (old_state == DEFS.PAGE_READ)
                move_queue(ref q_free, ref q_read, buf_n);
            else
                move_queue(ref q_free, ref q_write, buf_n);

            this.cache[buf_n].page_no = -1;
            this.cache[buf_n].last_access = 0;
            this.cache[buf_n].access_count = 0;
        }
        /// <summary>
        /// Flush all
        /// </summary>
        public void flush()
        {
            flush(-1);
        }

        /// <summary>
        /// Flush one or all pages to disk: n=-1 - all pages; n>=0 - only buffer #n
        /// </summary>
        /// <param name="buf_n"></param>
        private void flush(int buf_n)
        {
            int startn = 0;
            int count = this.cache.Length;

            if (buf_n >= 0)
            {
                startn = buf_n;
                count = 1;
            }
            for (int i = startn; i < (startn + count); i++)
            {
                long pn = this.cache[i].page_no;
                if (pn >= 0)
                {
                    if (this.page_table[pn].page_state == DEFS.PAGE_WRITE)
                    {
                        // Write transaction data
                        flushTA(i, true);

                        // Write page
                        PageWrite(pn, i);
                        this.page_table[this.cache[i].page_no].page_state = DEFS.PAGE_READ;
                        move_queue(ref q_read, ref q_write, i);
                    }
                }
            }
        }

        /// <summary>
        /// Write transaction records if threshold reached
        /// </summary>
        /// <param name="rec"></param>
        private void flushTA(int bufno, bool force)
        {
            if (Transaction != null)
            {
                if (Transaction.Started)
                {
                    if (this.cache[bufno].tbuf != null)
                    {
                        long address = cache[bufno].page_no * page_size;

                        Transaction.WriteRecord(DESCRIPTOR.Id, address, ref this.cache[bufno].tbuf);

                        this.cache[bufno].tbuf = null;
                    }
                }
            }
        }

        /// <summary>
        /// Read page into buffer: n - page number; nbuf - buffer number
        /// </summary>
        /// <param name="n"></param>
        /// <param name="nbuf"></param>
        private void PageRead(long n, int nbuf)
        {
            for (int i = 0; i < pd.Length; i++)
            {
                if ((n >= pd[i].start_page) & (n <= pd[i].end_page))
                {
                    pd[i].fs.Seek((n - pd[i].start_page) * page_size, SeekOrigin.Begin);
                    pd[i].fs.Read(this.cache[nbuf].buf, 0, (int)page_size);
                    PGRead++;
                    this.page_table[n].page_state = DEFS.PAGE_READ;
                    this.page_table[n].bufno = nbuf;
                    this.cache[nbuf].page_no = n;

                }
            }
        }

        /// <summary>
        /// Write page from buffer: n - page number; nbuf - buffer number
        /// </summary>
        /// <param name="n"></param>
        /// <param name="nbuf"></param>
        private void PageWrite(long n, int nbuf)
        {
            for (int i = 0; i < pd.Length; i++)
            {
                if ((n >= pd[i].start_page) & (n <= pd[i].end_page))
                {
                    pd[i].fs.Seek((n - pd[i].start_page) * page_size, SeekOrigin.Begin);
                    pd[i].fs.Write(this.cache[nbuf].buf, 0, (int)page_size);
                    PGWrite++;
                }
            }
        }

        /// <summary>
        /// Lock page: npage - page number
        /// </summary>
        /// <param name="npage"></param>
        private void lock_page(int npage)
        {
            if (!imo)
                if (this.state > DEFS.SPACE_CLOSED)
                {
                    if (this.page_table[npage].page_state > DEFS.PAGE_FREE)
                    {
                        this.page_table[npage].page_lock = DEFS.PAGE_LOCKED;
                    }
                }
        }

        /**************************************************************************/
        /**************************************************************************/
        /// <summary>
        /// GetSpace space size (bytes)
        /// </summary>
        /// <returns></returns>
        public long GetSpaceSize()
        {
            return (DESCRIPTOR.SpaceSize);
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
            io(address, ref data, length, DEFS.OP_READ);
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
        /// Read decimal
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public decimal ReadDecimal(long address)
        {
            return VSLib.ConvertByteToDecimal(ReadBytes(address, 16));
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
            Write(address, VSLib.ConvertUintToByte(data), 2);
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
        /// Write decimal
        /// </summary>
        /// <param name="address"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public void Write(long address, decimal data)
        {
            Write(address, VSLib.ConvertDecimalToByte(data), 16);
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
            io(address, ref data, length, DEFS.OP_WRITE, offset: offset);
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

        private static bool copy_buffer(ref byte[] target, ref byte[] source, long target_offset = 0, long source_offset = 0, long length = 0)
        {
            bool rc = false;
            for (int i = 0; i < length; i++)
                if (target[target_offset + i] != source[source_offset + i])
                {
                    target[target_offset + i] = source[source_offset + i];
                    rc = true;
                }
            return rc;
        }


        /// <summary>
        /// Transaction
        /// </summary>
        public VSTransaction Transaction
        {
            get { return _ta; }
        }

    }
}
