using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace VStorage
{
    /// <summary>
    /// 2017-06-29 In Memory Option (begin)
    /// </summary>
    public class VSEngine
    {
        /// <summary>
        /// Engine statae
        /// </summary>
        private int state = DEFS.STATE_UNDEFINED;

        private VSTransaction TA;
        private bool TRANSACTIONS_ON = true;
        private string root_path = DEFS.PATH_UNDEFINED;
        private VSCatalog CATALOG = null;

        private FileStream fslck;
        private int lock_number = -1;
        private const int lock_length = 16;


        private List<VSpace> sl;

        private List<VSVirtualMemoryManager> vl;

        internal string Error = "";

        /// <summary>
        /// Constructor
        /// </summary>
        public VSEngine()
        {
            state = DEFS.STATE_UNDEFINED;
            root_path = DEFS.PATH_UNDEFINED;
        }

        /// <summary>
        /// Constructor with path
        /// </summary>
        public VSEngine(string path)
        {
            state = DEFS.STATE_UNDEFINED;
            root_path = path.Trim();
        }

        ///////////////////////////////////////////////////////////////////////////////
        ///////// GENERAL METHODS /////////////////////////////////////////////////////
        ///////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Set storage path and define catalog without opening 
        /// Constructor - path to the root folder
        /// Empty path - in-memory option
        /// "*" - current directory
        /// 
        /// Not applicable in 'open' state
        /// </summary>
        /// <param name="path"></param>
        public void Set(string path, string encrypt = "")
        {
            if (state == DEFS.STATE_OPENED)
                throw new VSException(DEFS.E0025_STORAGE_UNABLE_TO_COMPLETE_CODE, " - 'Set', storage is opened");

            vs_imo = false;

            string p = (root_path == DEFS.PATH_UNDEFINED) ? path : root_path;

            root_path = p.Trim();

            if (root_path == DEFS.PATH_CURRENT)
                root_path = System.IO.Directory.GetCurrentDirectory();
            else if (root_path == "")
                vs_imo = true;
            else if (root_path == "")
                throw new VSException(DEFS.E0032_STORAGE_PATH_UNDEFINED_CODE);

            if (CATALOG == null)
                CATALOG = new VSCatalog(root_path);

            state = DEFS.STATE_DEFINED;

            if (encrypt.Trim() != "")
            {
                this.encrypt = (encrypt.ToUpper().Substring(0, 1) == DEFS.CT_ENCRYPTED);
            }
        }

        /// <summary>
        /// Open storage (all address spaces)
        /// </summary>
        /// <returns></returns>
        public void Open(string path = "", string encrypt = "")
        {
            if (state == DEFS.STATE_OPENED)
                return;

            string p = (root_path == DEFS.PATH_UNDEFINED) ? path : root_path;

            root_path = p.Trim();

            this.Set(p);

            // Space list
            sl = new List<VSpace>();

            // VMs list
            vl = new List<VSVirtualMemoryManager>();

            // Check if app directory exists. Create if missing 
            if (!Directory.Exists(DEFS.APP_ROOT_DATA))
                Directory.CreateDirectory(DEFS.APP_ROOT_DATA);

            // Check if keys directory exists. Create if missing 
            if (!Directory.Exists(DEFS.KEY_DIRECTORY))
                Directory.CreateDirectory(DEFS.KEY_DIRECTORY);

            ///////////////////////////////////////////////////////

            TA = new VSTransaction(TransactionFileName);

            if (!this.Lock())
            {
                throw new VSException(DEFS.E0001_UNABLE_TO_LOCK_CODE);
            }

            // Attach spaces
            for (int i = 0; i < CATALOG.Count; i++)
            {
                VSpace sp = new VSpace();
                VSVirtualMemoryManager vm = new VSVirtualMemoryManager();
                string err = vm.Open(CATALOG[i], TA);
                if (err == "")
                    err = sp.Attach(CATALOG[i], vm, TA, IMO);

                if (err != "")
                {
                    this.Release();
                    throw new VSException(DEFS.E0017_ATTACH_SPACE_ERROR_CODE, "- " + err);
                }
                else
                {
                    vl.Add(vm);
                    sl.Add(sp);
                }
            }

            // Add index spaces references
            for (int i = 0; i < sl.Count; i++)
            {
                if (CATALOG[i].IndexSpace != "")
                {
                    for (int j = 0; j < sl.Count; j++)
                    {
                        if (sl[j].Name == CATALOG[i].IndexSpace)
                        {
                            sl[i].IndexSpace = sl[j];
                            break;
                        }
                    }
                }
            }

            // Presistent only - check if rollback required
            if (!IMO)
            {
                if (TA.Pending)
                    RollBackChanges();
            }

            //Process new FSAT creation and new allocation descriptors, Initialize KEyManager
            for (int i = 0; i < sl.Count; i++)
            {
                sl[i].VerifySpaceChanges();
            }

            this.Commit();

            state = DEFS.STATE_OPENED;

            if (encrypt.Trim() != "")
            {
                this.encrypt = (encrypt.ToUpper().Substring(0, 1) == DEFS.CT_ENCRYPTED);
            }

            return;
        }

        /// <summary>
        /// Close storage (all address spaces)
        /// </summary>
        /// <returns></returns>
        public void Close()
        {
            if (state == DEFS.STATE_OPENED)
            {
                Commit();
                int j = sl.Count;
                for (int i = 0; i < j; i++)
                {
                    sl[0].Detach();
                    sl[0] = null;
                    sl.RemoveAt(0);
                }
                this.Release();
                state = DEFS.STATE_DEFINED;
            }
        }

        /// <summary>
        /// Lock current directory(all spaces)
        /// </summary>
        /// <returns></returns>
        private bool Lock()
        {
            if (IMO)
                return true;

            byte[] shared_code = { 0x0F, 0, 0, 0, 0, 0, 0, 0 };
            byte[] exclusive_code = { 0xFF, 0, 0, 0, 0, 0, 0, 0 };
            long exclusive_lock_offset = 0;

            string lock_fn = root_path + "\\" + DEFS.LOCK_FILE_NAME;

            if (fslck == null)
            {
                lock_number = -1;
                if (!File.Exists(lock_fn))
                {
                    byte[] b = new byte[8192];
                    fslck = File.Create(lock_fn);
                    fslck.Write(b, 0, b.Length);
                    fslck.Close();
                    fslck = null;
                }

                fslck = File.Open(lock_fn, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
                long number = fslck.Length / lock_length;

                try
                {
                    fslck.Lock(exclusive_lock_offset, fslck.Length);           // Lock for both read/write exclusively
                    lock_number = 0;
                    fslck.Seek(lock_number, SeekOrigin.Begin);
                    fslck.Write(exclusive_code, 0, exclusive_code.Length);
                    fslck.Write(VSLib.ConvertLongToByte(DateTime.Now.ToBinary()), 0, 8);
                }
                catch (IOException e)
                {
                    this.Error = e.Message;
                    fslck.Close();
                    fslck = null;
                }
                return (lock_number >= 0);
            }
            else
                return true;
        }

        /// <summary>
        /// Release storage
        /// </summary>
        /// <returns></returns>
        private void Release()
        {
            if (IMO)
                return;

            if (fslck != null)
            {
                long offs = lock_number * lock_length;
                long l = (lock_number == 0) ? fslck.Length : lock_length;
                fslck.Seek(offs, SeekOrigin.Begin);
                fslck.Write(new byte[lock_length], 0, lock_length);
                fslck.Unlock(offs, l);
                fslck.Close();
            }
            fslck = null;
            lock_number = -1;
        }

        /// <summary>
        /// Begin transaction
        /// </summary>
        /// <returns></returns>
        public void Begin()
        {
            if (TRANSACTIONS_ON & (!TA.Started))             // Begin if transaction is not started yet. Otherwise ignore
                    TA.Begin();
        }

        /// <summary>
        /// Commit transaction
        /// </summary>
        /// <returns></returns>
        public void Commit()
        {
            if (!IMO)
                for (int i = 0; i < sl.Count; i++)
                    vl[i].flush();

            if (TA != null)
                TA.Commit();
        }

        /// <summary>
        /// Manual rollback
        /// </summary>
        public void RollBack()
        {
            if (TA.Started)
            {
                if (!IMO)
                    for (int i = 0; i < vl.Count; i++)
                        vl[i].flush();

                TA.Close();
                RollBackChanges();
            }
        }

        /// <summary>
        /// Rollback
        /// </summary>
        private void RollBackChanges()
        {
            //1. Check if transaction is in progress
            if (TA.Started)
                throw new VSException(DEFS.E0018_TRANSACTION_ERROR_CODE, "- Rollback - transaction is still in progress");

            TA.Close();

            TRANSACTIONS_ON = false;                //Disable transactioning

            if (!IMO)
            {
                TA.Open();                              //Open transaction for rollforward
                VSTransaction.TA_RECORD tr = TA.ReadRecord();
                while (tr.ID >= 0)
                {
                    for (int i = 0; i < sl.Count; i++)
                        if (sl[i].Id == tr.ID)
                        {
                            vl[i].Write(tr.ADDRESS, tr.DATA, (long)tr.LENGTH);
                            break;
                        }
                    tr = TA.ReadRecord();
                }
                for (int i = 0; i < sl.Count; i++)
                {
                    if (!IMO)
                        vl[i].flush();

                    sl[i].FreeSpaceMgr.BuildBTrees();
                }
            }
            else
            { // IMO Rollback
                for (int i = 0; i < vl.Count; i++)
                    vl[i].RollBackIMO();


            }
            TA.Commit();
            TRANSACTIONS_ON = true;
        }

        /// <summary>
        /// Get list of space names
        /// </summary>
        /// <returns></returns>
        public string[] GetSpaceList()
        {
            string[] nm = new string[sl.Count];
            for (int i = 0; i < sl.Count; i++)
                nm[i] = sl[i].Name;
            return nm;
        }

        /// <summary>
        /// Get space by name
        /// </summary>
        /// <param name="name"></param>
        /// <returns>Space index (0..n) or -1 if not found</returns>
        public VSpace GetSpace(string name)
        {
            for (int i = 0; i < sl.Count; i++)
                if (sl[i].Name.ToLower() == name.Trim().ToLower())
                    return sl[i];
            return null;          //Space is not found
        }

        /// <summary>
        /// Get VM by name
        /// </summary>
        /// <param name="name"></param>
        /// <returns>Space index (0..n) or -1 if not found</returns>
        internal VSVirtualMemoryManager GetVM(string name)
        {
            for (int i = 0; i < sl.Count; i++)
                if (sl[i].Name.ToLower() == name.Trim().ToLower())
                    return vl[i];
            return null;          // Not found
        }

        /////////////////////////////////////////////////
        ///////// PROPERTIES ////////////////////////////
        /////////////////////////////////////////////////
        /// <summary>
        /// Catalog file name with path
        /// Empty for IMO
        /// </summary>
        private string CtlgFileName
        {
            get { return (IMO) ? "" : this.root_path + "\\" + DEFS.CTLG_FILE_NAME; }
        }
        
        /// <summary>
        /// Transaction file name with path
        /// Empty for IMO
        /// </summary>
        private string TransactionFileName
        {
            get { return (IMO)? "" : this.root_path + "\\" + DEFS.TA_FILE_NAME; }
        }

        /// <summary>
        /// Get space file name with path
        /// part - number of partition
        /// Empty for IMO
        /// </summary>
        private string GetSpaceFileName(string nm, string path = "", int part = 0)
        {
            if (IMO)
                return "";
            else
                return ((path == "") ? this.root_path : path) + "\\" + DEFS.SPACE_FILE_NAME(nm, part);
        }


        //==========================================================================================================//
        //=========================================== Manager ======================================================//
        //==========================================================================================================//
        /// <summary>
        /// Create new address space. Return 0 - successful; -1 - error
        /// dir - directory
        /// name - space name
        /// page size -  in Kb
        /// size - size of the space in Mb. Default - 5
        /// extension - size for dynamic extension in MB. Default - 5
        /// </summary>
        /// <param name="name"></param>
        /// <param name="pagesize"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public void Create(string name, int pagesize = DEFS.DEFAULT_PAGE_SIZE, int size = DEFS.DEFAULT_SPACE_SIZE, int extension = DEFS.DEFAULT_EXT_SIZE, string path = "")
        {
            if (state != DEFS.STATE_DEFINED)
                throw new VSException(DEFS.E0025_STORAGE_UNABLE_TO_COMPLETE_CODE, "- 'Create' (storage is opened or undefined)");

            VSCatalogDescriptor desc = CATALOG.Get(name);
            if (desc != null)
                throw new VSException(DEFS.E0024_CREATE_SPACE_ERROR_CODE, "- space already exists(" + name + ")");

            string space_path = path.Trim();
            string sname = "";

            if (IMO)
                space_path = "";
            else
            {
                if (space_path == "")
                    space_path = root_path;

                if (!System.IO.Directory.Exists(space_path))
                    throw new VSException(DEFS.E0004_STORAGE_NOT_FOUND_CODE, "(" + space_path + ")");

                // Check if directory exists
                if (!System.IO.Directory.Exists(root_path))
                    throw new VSException(DEFS.E0004_STORAGE_NOT_FOUND_CODE, "(" + root_path + ")");

                if (!this.Lock())
                    throw new VSException(DEFS.E0001_UNABLE_TO_LOCK_CODE, "(Create space)");

                // Check if file exists, if no - create new space.
                sname = GetSpaceFileName(name, space_path);
                if (System.IO.File.Exists(sname))
                    throw new VSException(DEFS.E0005_FILE_ALREADY_EXISTS_CODE, "(" + sname + ")");
            }

            // Add descriptor
            desc = CATALOG.Create(name, (long)size, (long)extension, (long)pagesize, space_path);

            desc.IMO = IMO;

            if (!desc.IMO)
            {
                //Create file
                byte[] dataArray = new byte[desc.PageSize];

                VSIO IO = new VSIO(sname, VSIO.FILE_MODE_CREATE, "");

                byte[] resv = new byte[8];

                for (long i = 0; i < desc.space_size_pg; i++)
                {
                    IO.Write(-1, DEFS.DATA_NOT_ENCRYPTED);                 // + 0 (4) Encryption indicator

                    IO.Write(-1, (uint)0);                                // + 4 (4) CRC32 placeholder

                    IO.Write(-1, ref resv);                               // +8 (8) reserve

                    IO.Write(-1, ref dataArray);
                }
                IO.Write(DEFS.SYSTEM_OWNER_ADDRESS + 16, DEFS.SYSTEM_OWNER_UNDEFINED.PadRight((int)DEFS.SYSTEM_OWNER_LENGTH));

                IO.Close();

                CATALOG.Save();
            }

            Release();
        }

        /// <summary>
        /// Remove space: name: space name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public void Remove(string name)
        {
            if (state != DEFS.STATE_DEFINED)
                throw new VSException(DEFS.E0025_STORAGE_UNABLE_TO_COMPLETE_CODE, "- 'Remove' (storage is opened or undefined)");

            if (!this.Lock())
                throw new VSException(DEFS.E0001_UNABLE_TO_LOCK_CODE, "(Remove space)");

            for (int n = (CATALOG.Count - 1); n >= 0; n--)
            {
                if (VSLib.Compare(name, CATALOG[n].Name))
                {
                    if (!IMO)
                    {
                        for (int i = 0; i < CATALOG[n].Partitions; i++)
                            System.IO.File.Delete(GetSpaceFileName(CATALOG[n].Name, CATALOG[n].Path, i));

                        FileStream fp = new FileStream(TransactionFileName, FileMode.Create);
                        fp.Close();
                        CATALOG.Delete(CATALOG[n].Name);
                        CATALOG.Save();
                    }
                    else
                        CATALOG.Delete(CATALOG[n].Name);
                }
            }
            Release();
        }

        /// <summary>
        /// Extend space size: name - space name; size - extension(Mb)
        /// </summary>
        /// <param name="name"></param>
        /// <param name="size (Mb)"></param>
        /// <returns></returns>
        public void Extend(string name, int size)
        {
            if (state != DEFS.STATE_DEFINED)
                throw new VSException(DEFS.E0025_STORAGE_UNABLE_TO_COMPLETE_CODE, "- 'Extend' (storage is opened or undefined)");

            VSCatalogDescriptor desc = CATALOG.Get(name);
            if (desc == null)
                throw new VSException(DEFS.E0002_SPACE_NOT_FOUND_CODE, "(" + name + ")");

            if (!IMO)
                if (desc.Partitions > 1)        // Check if more than 1 partition exists. If so - add partition instead of extending
                {
                    this.AddPartition(name, size);
                    return;
                }

            if (!this.Lock())
                throw new VSException(DEFS.E0001_UNABLE_TO_LOCK_CODE, "(Extend space)");


            long start_pos = desc.SpaceSize;                                                  //Address of the 1st byte out of space

            //Calculate new allocation size in pages
            long add_pages = (long)(((size < 1) ? 1 : size) * 1048576) / (long)(desc.PageSize);

            // Increase space size in descriptor
            desc.space_size_pg += add_pages;

            if (!IMO)
            {
                //Initialize page buffer
                byte[] dataArray = new byte[desc.PageSize];

                //Append pages
                VSIO IO = new VSIO(GetSpaceFileName(name, desc.Path), VSIO.FILE_MODE_APPEND, "");

                byte[] resv = new byte[8];

                for (long i = 0; i < add_pages; i++)
                {
                    IO.Write(-1, DEFS.DATA_NOT_ENCRYPTED);                 // + 0 (4) Encryption indicator

                    IO.Write(-1, (uint)0);                                // + 4 (4) CRC32 placeholder

                    IO.Write(-1, ref resv);                               // +8 (8) reserve

                    IO.Write(-1, ref dataArray);
                }
                IO.Close();

                AddNewAllocation(desc.Path, name, start_pos, add_pages * desc.PageSize);

                CATALOG.Save();
            }

            Release();
        }

        /// <summary>
        /// Add partition to space: name - space name; size - size(Mb)
        /// </summary>
        /// <param name="name"></param>
        /// <param name="size">Mb</param>
        /// <returns></returns>
        public void AddPartition(string name, int size)
        {
            if (state != DEFS.STATE_DEFINED)
                throw new VSException(DEFS.E0025_STORAGE_UNABLE_TO_COMPLETE_CODE, "- 'AddPartition' (storage is opened or undefined)");

            VSCatalogDescriptor desc = CATALOG.Get(name);
            if (desc == null)
                throw new VSException(DEFS.E0002_SPACE_NOT_FOUND_CODE, "(" + name + ")");

            if (!this.Lock())
                throw new VSException(DEFS.E0001_UNABLE_TO_LOCK_CODE, "(Add partition)");

            long old_size = desc.SpaceSize;

            //Calculate space size (in pages)
            long add_pages = (long)(((size < 1) ? 1 : size) * 1048576) / desc.PageSize;

            desc.space_size_pg += add_pages;

            if (!IMO)
            {
                // Append pages
                byte[] dataArray = new byte[desc.PageSize];

                VSIO IO = new VSIO(GetSpaceFileName(name, desc.Path, (int)desc.Partitions), VSIO.FILE_MODE_CREATE, "");

                byte[] resv = new byte[8];

                for (long i = 0; i < add_pages; i++)
                {
                    IO.Write(-1, DEFS.DATA_NOT_ENCRYPTED);                 // + 0 (4) Encryption indicator

                    IO.Write(-1, (uint)0);                                // + 4 (4) CRC32 placeholder

                    IO.Write(-1, ref resv);                               // +8 (8) reserve

                    IO.Write(-1, ref dataArray);
                }

                IO.Close();

                desc.partitions++;

                AddNewAllocation(desc.Path, name, old_size, add_pages * desc.PageSize);

                CATALOG.Save();
            }
            Release();
        }

        /// <summary>
        /// Display space/storage name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public string[] List(string name = "*")
        {
            List<string> l = new List<string>();
            string[] sp_list = GetSpaceNameList();

            bool nf = false;
            long sz = GetStorageSize();
            if (name == "*")
            {
                l.Add("Storage size:   " + sz.ToString("#,#;(#,#)") + " bytes (" + (sz / 1048576).ToString("#,#;(#,#)") + " Mb)");
            }

            for (int i = 0; i < sp_list.Length; i++)
            {
                if (l.Count > 0)
                    l.Add(" ");
                if (VSLib.Compare(name.Trim().ToLower(), sp_list[i]))
                {
                    nf = true;
                    string[] info = GetSpaceHeaderInfo(sp_list[i]);
                    for (int j = 0; j < info.Length; j++)
                        l.Add(info[j]);
                }
            }

            if (!nf)
                l.Add("No space found matching search criteria '" + name + "'");

            return l.ToArray();
        }

                /// <summary>
        /// Get the list of spaces from the catalog
        /// </summary>
        /// <returns></returns>
        public string[] GetSpaceNameList()
        {
            List<string> ls = new List<string> ();
            for (int i = 0; i < CATALOG.Count; i++)
                ls.Add(CATALOG[i].Name);
            return ls.ToArray();
        }

        /// <summary>
        /// Get free space info
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public string[] GetFreeSpaceInfo(string name)
        {
            VSpace sp = GetSpace(name);
            return sp.GetFreeSpaceInfo();
        }


        /// <summary>
        /// Save storage to byte arrray
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public byte[] DumpToArray(string name, bool encrypt = false)
        {
            long sz = (this.GetStorageSize() / 4);

            if (sz > int.MaxValue)
                throw new VSException(DEFS.E0025_STORAGE_UNABLE_TO_COMPLETE_CODE, "Storage size is too big for in-memory backup)");

            VSIO IO = new VSIO(null, DEFS.ENCRYPT_DUMP);

            IO.SetEncryption(encrypt);

            dump(IO, name);

            return  IO.GetBytes();
        }

        /// <summary>
        /// Save storage to file
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public void Dump(string path, string name, bool encrypt = false)
        {
            string d_path = path;
            if (d_path != "")
            {
                if (!System.IO.Directory.Exists(d_path))
                    throw new VSException(DEFS.E0025_STORAGE_UNABLE_TO_COMPLETE_CODE, "Dump, path is not found: '" + d_path + "'");
            }
            else
                d_path = System.IO.Directory.GetCurrentDirectory();

            string fname = GenerateDumpFileName(d_path);

            if (File.Exists(fname))
                File.Delete(fname);

            VSIO IO = new VSIO(fname, VSIO.FILE_MODE_CREATE, DEFS.ENCRYPT_DUMP);

            IO.SetEncryption(encrypt);

            dump(IO, name);

            IO.Close();
        }

        /// <summary>
        /// Dump storage
        /// Space name - 32
        /// System area - 1024
        /// Pool area - 4096
        /// </summary>
        private void dump(VSIO IO, string name)
        {
            bool was_opened = false;

            uint e_code = IO.GetEncryption() ? e_code = DEFS.DATA_ENCRYPTED : e_code = DEFS.DATA_NOT_ENCRYPTED;

            if (state == DEFS.STATE_OPENED)
            {
                if (TA.Started)
                    throw new VSException(DEFS.E0025_STORAGE_UNABLE_TO_COMPLETE_CODE, "- Dump (transaction is in progress)");
                was_opened = true;
            }
            else
            {
                Open(null);
            }

            string[] spaces = this.GetSpaceList();
            bool encr = IO.GetEncryption();

            IO.SetEncryption(false);

            IO.Write(0, e_code);                                                     // + 0 (4)Encryption indicator

            IO.SetEncryption(encr);
                
            IO.Write(-1, DEFS.DUMP_SIGNATURE_INCOMPLETE);                            // + 4 (4) Signature 'incomplete'
            IO.Write(-1, (int)0);                                                    // + 8 (4) CRC placeholder
            IO.Write(-1, (long)0);                                                   // +12 (8)Total length


            for (int sp_index = 0; sp_index < spaces.Length; sp_index++)
            {
                VSpace sp = GetSpace(spaces[sp_index]);
                VSVirtualMemoryManager vm = GetVM(spaces[sp_index]);

                if ((name == "") | (VSLib.Compare(name, sp.Name)))
                {
                    long sp_hdr_pos = IO.GetPosition();                          // Position for header(number of bytes)

                    IO.Write(-1, (long)0);

                    // Save space header
                    IO.Write(-1, (short)sp.Name.Length);                             // Space neme length (short)
                    IO.Write(-1, sp.Name);                                           // Space name

                    IO.Write(-1, (short)sp.Owner.Length);                            // Space owner length (short)
                    IO.Write(-1, sp.Owner);                                          // Space owner

                    //////////////////////////////////////////////////////////////
                    // Save keys 
                    //////////////////////////////////////////////////////////////
                    IO.Write(-1, DEFS.DUMP_KEYS_SIGNATURE);                                // Start keys
                    VSKeyManager kh = sp.KeyManager;
                    kh.Reset();

                    while (kh.Next())
                    {
                        long k = kh.Current;
                        IO.Write(-1, k);
                    }

                    IO.Write(-1, (long)-1);                                   // End keys

                    // Save pool ares (starting from 2)

                    short[] pools = sp.GetPoolsForDump();                           // Create list of pools

                    for (int i = 0; i < pools.Length; i++)
                    {
                        long a_desc = sp.GetFirstAddress(pools[i]);
                        while (a_desc > 0)
                        {

                            VSAllocation a = sp.GetAllocationByDescriptor(a_desc);

                            //////////// Save ADSC fields ///////////
                            IO.Write(-1, a.Id);
                            IO.Write(-1, a.Pool);

                            IO.Write(-1, (a.Chunk == 0) ? a.Length : a.Size);     // Memory size
                            IO.Write(-1, a.ChunkSize);                           // Chunk sizeMemory size

                            IO.Write(-1, a.ALLOC);                               // Alloc version (object) 
                            IO.Write(-1, a.FIXED);                               // Fixed part (object) 

                            //////////// Save data //////////////////
                            byte[] b = vm.ReadBytes(a.Address, a.Length);
                            IO.Write(-1, ref b);
                            a_desc = a.NEXT;
                            if (a.Chunk != 0)
                            {
                                while (a_desc != 0)
                                {
                                    a = sp.GetAllocationByDescriptor(a_desc);
                                    if ((a.Chunk == 0) | (a.Chunk == 1))
                                        break;
                                    b = vm.ReadBytes(a.Address, a.Length);
                                    IO.Write(-1, ref b);
                                    a_desc = a.NEXT;
                                }
                            }
                        }
                    }
                    long sp_count = IO.GetPosition() - sp_hdr_pos;                   // Calculate count
                    long current_pos = IO.GetPosition();                        // Save current position
                    IO.Write(sp_hdr_pos, sp_count);                             // Write count to the header (incl hdr)
                    IO.SetPosition(current_pos);                                // Restore position
                }
            }

            IO.Write(-1, (long)-1);                                      // Write eof indicator

            IO.Write(12, (long)IO.GetLength());                          // + 8 (8) - Total length

            IO.Flush();

            uint c = IO.GetCRC32(12, -1);                                // calculate CRC32

            IO.Write(8, c);                                              // + 4 (4) - CRC32

            IO.Write(4, DEFS.DUMP_SIGNATURE);                            // Signature 'complete'

            IO.Flush();

            if (!was_opened)
                Close();
        }

        /// <summary>
        /// Restore storage from file
        /// </summary>
        public void Restore(string filename = "", string name = "*")
        {
            if (state != DEFS.STATE_DEFINED)
                throw new VSException(DEFS.E0025_STORAGE_UNABLE_TO_COMPLETE_CODE, "- 'Restore' - storage is opened or undefined");

            if (!System.IO.File.Exists(filename))
                throw new VSException(DEFS.E0025_STORAGE_UNABLE_TO_COMPLETE_CODE, "- 'Restore' - file is not found: '" + filename + "'");

            VSIO IO = new VSIO(filename, VSIO.FILE_MODE_OPEN, DEFS.ENCRYPT_DUMP);

            this.restore(IO, name);

            IO.Close();
        }

        /// <summary>
        /// Restore storage from byte array
        /// </summary>
        public void RestoreFromArray(byte[] array, string name = "*")
        {
            if (state != DEFS.STATE_DEFINED)
                throw new VSException(DEFS.E0025_STORAGE_UNABLE_TO_COMPLETE_CODE, "- 'Restore' - storage is opened or undefined");

            VSIO IO = new VSIO(array, DEFS.ENCRYPT_DUMP);

            this.restore(IO, name);

            IO.Close();
        }


        /// <summary>
        /// Restore storage
        /// </summary>
        public void restore(VSIO IO, string name = "*")
        {
            byte[] buf;                            // Buffer

            // Check database state
            if (state != DEFS.STATE_DEFINED)
                throw new VSException(DEFS.E0025_STORAGE_UNABLE_TO_COMPLETE_CODE, "- 'Restore' (storage is opened or undefined)");

            // Check if length not less than 16
            if (IO.GetLength() < 32)
                throw new VSException(DEFS.E0025_STORAGE_UNABLE_TO_COMPLETE_CODE, "Restore - invalid source stream (wrong source length)");

            // Check encryption
            IO.SetEncryption(false);
            uint encr = IO.ReadUInt(0);
            IO.SetEncryption(encr == DEFS.DATA_ENCRYPTED);

            // Check signature
            string sig = IO.ReadString(-1, DEFS.DUMP_SIGNATURE.Length);

            if (sig != DEFS.DUMP_SIGNATURE)
                throw new VSException(DEFS.E0025_STORAGE_UNABLE_TO_COMPLETE_CODE, "Restore - invalid source stream (wrong signature)");

            // Check CRC
            uint old_crc = IO.ReadUInt(-1);

            uint new_crc = IO.GetCRC32(12, -1);                               // calculate CRC32

            if (old_crc != new_crc)
                throw new VSException(DEFS.E0025_STORAGE_UNABLE_TO_COMPLETE_CODE, "Restore - invalid source stream (wrong CRC)");

            // Check source length vs saved
            long slen = IO.ReadLong(-1);

            if (IO.GetLength() != slen)
                throw new VSException(DEFS.E0025_STORAGE_UNABLE_TO_COMPLETE_CODE, "Restore - invalid source stream (wrong source length)");

            long save_pos = IO.GetPosition();                // +16

            long cpos = save_pos;

            TRANSACTIONS_ON = false;
            Open();
            TA.RollMode = true;

            long splen = IO.ReadLong(cpos);      // Space length

            // Validate list of spaces
            while (splen > 0)
            {
                // Read space name length
                int n_len = IO.ReadShort(cpos + 8);

                // Read space name
                string sname = IO.ReadString(cpos + 10, n_len);

                VSpace sp = GetSpace(sname);

                if (sp == null)
                {
                    this.Close();
                    this.Create(sname);
                    this.Open();
                }
                cpos += splen;
                splen = IO.ReadLong(cpos);
            }

            // Restore position
            IO.SetPosition(save_pos);

            // Proceed with restore
            splen = IO.ReadLong(-1);

            long spcnt = 8;

            while (splen != -1)
            { 

                // Read space name length
                int n_len = IO.ReadShort(-1);

                // Read space name
                string sname = IO.ReadString(-1, n_len);

                spcnt += (n_len + 2);

                // Read owner length
                int o_len = IO.ReadShort(-1);

                // Read Owner
                string sowner = IO.ReadString(-1, o_len);

                spcnt += (o_len + 2);

                VSpace sp = GetSpace(sname);

                VSVirtualMemoryManager vm = GetVM(sname);

                sp.Owner = sowner;
                vm.Write(DEFS.SYSTEM_STATUS_ADDRESS, DateTime.Now.ToBinary());            // Write timestamp - restore started

                if (!IMO)
                    vm.flush();

                // Restore keys
                if (IO.GetPosition() < IO.GetLength())
                {
                    sig = IO.ReadString(-1, DEFS.DUMP_KEYS_SIGNATURE.Length);

                    spcnt += DEFS.DUMP_KEYS_SIGNATURE.Length;

                    if (sig != DEFS.DUMP_KEYS_SIGNATURE)
                        throw new VSException(DEFS.E0006_INVALID_SIGNATURE_CODE, "(Restore missing start key signature)");


                    VSKeyManager kh = sp.KeyManager;

                    if (!kh.IsEmpty)
                        throw new VSException(DEFS.E0025_STORAGE_UNABLE_TO_COMPLETE_CODE, "Space '" + sname + "' is not empty. Restore terminated.");

                    long id = IO.ReadLong(-1);
                    spcnt += 8;

                    while (id >= 0)
                    {
                        //VSDebug.StopPoint(id, 23884);
                        //VSDebug.StopPoint(id, 24033);

                        kh.Add(null, id);
                        id = IO.ReadLong(-1);
                        spcnt += 8;
                    }
                }

                // Restore data pools
                long size = 0;
                int chunk_size = 0;
                ushort alloc = 0;
                ushort fix = 0;
                while (spcnt < splen)
                {
                    VSAllocation new_a;

                    //Read ADSC fields
                    long oldid = IO.ReadLong(-1);

                    short pool = IO.ReadShort(-1);
                    spcnt += 10;

                    size = IO.ReadLong(-1);                     // Total size
                    spcnt += 8;

                    chunk_size = IO.ReadInt(-1);                // Chunk size
                    spcnt += 4;

                    alloc = IO.ReadUShort(-1);                  // Alloc
                    spcnt += 2;

                    fix = IO.ReadUShort(-1);                    // Fixed part length
                    spcnt += 2;

                    buf = IO.ReadBytes(-1, (int)size);
                    spcnt += size;

                    // Allocate space
                    new_a = sp.ALLOCATE_SPACE(size, pool, false, (long)chunk_size, (short)fix);

                    new_a.ALLOC = (ushort)((alloc == 0) ? 0 : 1);                      // Set allocation 0 - RAW; 1 - FIELD 

                    if (oldid > 0)
                    {
                        new_a.Id = oldid;
                        sp.KeyManager.Update(oldid, new_a);
                    }

                    new_a.Write(0, buf, buf.Length);                          //Save data
                }

                vm.Write(DEFS.SYSTEM_STATUS_ADDRESS, (long)0);                          // Write 0 - restore ended for space

                splen = IO.ReadLong(-1);

                spcnt = 8;
            }
            if (!IMO)
                Close();

            TRANSACTIONS_ON = true;
        }

        /// <summary>
        /// Check if space with the specified name exists
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool Exists(string name)
        {
            return (this.CATALOG.Get(name.ToLower().Trim()) != null);
        }

        /// <summary>
        /// Set use dedicated space {index_space_name} for indexes of the {space_name} objects
        /// Empty 'index_space_name' means stop using index space. Indexes will not be removed automatically.
        /// 
        /// </summary>
        /// <param name="space_name"></param>
        /// <param name="index_space_name"></param>
        /// <returns></returns>
        public string UseIndexSpace(string space_name, string index_space_name)
        {
            string s = "";
            VSCatalogDescriptor d_desc =  this.CATALOG.Get(space_name);
            VSCatalogDescriptor x_desc = null;
            if (d_desc == null)
                s = "Space '" + space_name + "' is not found";
            else
            {
                if (index_space_name.Trim() != "")
                {
                    x_desc = this.CATALOG.Get(index_space_name);
                    if (x_desc == null)
                        s = "Space '" + index_space_name + "' is not found";
                    else if (x_desc.IndexSpace != "")
                        s = "The specified index space cannot refer to anothe index space ('" + index_space_name + "' refers to '" + x_desc.IndexSpace + "'";
                }
            }
            
            if (s == "")
            {
                if (x_desc == null)
                {
                    if (d_desc.indexspace == "")
                        return "";
                    else
                        d_desc.indexspace = "";
                }
                else
                {
                    if (d_desc.indexspace == index_space_name.Trim().ToLower())
                        return "";
                    else
                        d_desc.indexspace = index_space_name;
                }

                if (!IMO)
                    this.CATALOG.Save();
            }
            return s;
        }



        /*************************************************************************************/
        /*************************  PRUBLIC PROPERTIES ***************************************/
        /*************************************************************************************/
        /// <summary>
        /// Running storage option (in memory or prsistent)
        /// </summary>
        public bool IMO
        {
            get { return vs_imo; }
        }
        private bool vs_imo = false;

        /// <summary>
        /// Encryption
        /// </summary>
        private bool encrypt
        {
            get { return CATALOG.Encrypt; }
            set { CATALOG.Encrypt = value; }
        }

        /*************************************************************************************/
        /*************************  PRIVATE METHODS ******************************************/
        /*************************************************************************************/
        /// <summary>
        /// Update FSAT by adding new element (Extend, AddPartition) or in Create
        /// </summary>
        /// <param name="name"></param>
        /// <param name="address"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        private void AddNewAllocation(string space_path, string name, long address, long length)
        {
            VSIO IO = new VSIO(GetSpaceFileName(name, space_path), VSIO.FILE_MODE_OPEN, "");

            long h_addr = DEFS.SYSTEM_ALLOCATION_ADDRESS;
            short n = IO.ReadShort(h_addr);       //The number of pending updates

            long e_addr = h_addr + 2 + (n * 16);

            IO.Write(e_addr, address);

            IO.Write(e_addr + 8, address + length);

            n++;

            IO.Write(h_addr, n);

            IO.Close();
        }

        /// <summary>
        /// Get space file name with path
        /// part - number of partition
        /// </summary>
        //private string GetDumpFileName(string nm, int part = 0, string path = "")
        //{
        //    return ((path == "") ? "" : path + "\\") + DEFS.DUMP_FILE_NAME(nm, part);
        //}

        /// <summary>
        /// Get file name with path
        /// </summary>
        private string GenerateDumpFileName(string path = "")
        {
            DateTime dt = DateTime.Now;

            return path + "\\" + DEFS.SYSID + DEFS.SYSMJ + "." + CATALOG.CATALOG_DIR + //"." +
                //dt.Year.ToString("D4") + dt.Month.ToString("D2") + dt.Day.ToString("D2") +
                //dt.Hour.ToString("D2") + dt.Minute.ToString("D2") + dt.Second.ToString("D2") + dt.Millisecond.ToString("D4") +
                ".vdmp";
        }


        /// <summary>
        /// Get summary size of all spaces
        /// </summary>
        /// <returns></returns>
        public long GetStorageSize()
        {
            long sz = 0;
            for (int i = 0; i < CATALOG.Count; i++)
                sz += CATALOG[i].SpaceSize;
            return sz;
        }

        /// <summary>
        /// Get information about space
        /// </summary>
        /// <param name="name"></param>
        private string[] GetSpaceHeaderInfo(string name)
        {
            string[] hdr = new string[8];
            for (int i = 0; i < hdr.Length; i++)
                hdr[i] = "";

            VSCatalogDescriptor desc = CATALOG.Get(name);
            if (desc != null)
            {
                hdr[0] = "Id:             " + desc.Id.ToString();
                hdr[1] = "Name:           " + desc.Name;
                hdr[2] = "Path:           " + desc.Path;
                hdr[3] = "Page size:      " + desc.PageSize.ToString("#,#;(#,#)") + " bytes";
                hdr[4] = "Partitions:     " + desc.Partitions.ToString();
                hdr[5] = "Size:           " + desc.SpaceSize.ToString("#,#;(#,#)") + " bytes (" + desc.SpaceSizeMb.ToString("#,#;(#,#)") + " Mb)";
                hdr[6] = "Pages:          " + desc.space_size_pg.ToString();
                hdr[7] = "Auto extension: " + ((desc.Extension == 0) ? "No" : (desc.extension_pg.ToString("#,0;(#,0)") + " pages (" + desc.ExtensionMb.ToString("#,0;(#,0)") + " Mb)"));
            }
            return hdr;
        }


    }
}
