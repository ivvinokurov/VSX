using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VStorage
{
    public static class DEFS
    {

        ///////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////// PUBLIC /////////////////////////////////////////
        ///////////////////////////////////////////////////////////////////////////////////////////


        public static string POOL_MNEM(short n)
        {
            string[] m = { "FSP", "KEY", "IDX", "UDF", "DYN" };
            if (n > 0)
                return "";

            int n1 = n * (-1);

            if (n1 >= m.Length)
                return "";

            return m[n1];
        }

        public const string SYSTEM_OWNER_UNDEFINED = "$UNDEFINED$"; // Undefined space owner

        // New line constant
        public const string DELIM_NEWLINE = "\r" + "\n";

        /// <summary>
        /// Root directory in the default user's dir
        /// </summary>
        public static string APP_ROOT_DATA
        {
            get { return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\IVVS"; }
        }

        /// <summary>
        /// Keys directory
        /// </summary>
        public static string KEY_DIRECTORY
        {
            get { return APP_ROOT_DATA + "\\vs.default"; }
        }


        public const string KEY_STORAGE_ROOT = "root-path.vs.default";
        public const string KEY_DUMP_RESTORE = "dump-path.vs.default";

        /// <summary>
        /// Storage run option
        /// </summary>
        public const int OPTION_PERSISTENT = 0;
        public const int OPTION_IN_MEMORY = 1;

        /// <summary>
        /// Pool area address
        /// </summary>
        //public static long POOL_AREA_ADDRESS
        //{
        //    get { return SYSTEM_POOL_AREA_ADDRESS; }
        //}

        ///////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////// INTERNAL  ///////////////////////////////////////
        ///////////////////////////////////////////////////////////////////////////////////////////

        internal const int BaseDescriptorLength = 64;        //Descriptor length (for chunk = 0/1) 

        internal const int ExpansionDescriptorLength = 44;             //Header length (for chunks > 1)


        internal const string INDEX_CROSS_REFERENCES =  "$$crossrefsindex$$";
        internal const string INDEX_NAME_DELIMITER =    "::";
        public const string _DEBUG_INDEX_CROSS_REFERENCES = INDEX_CROSS_REFERENCES;

        internal const string VSTORAGE_VERSION = "00010001";
        internal const string VM_CACHE_SIZE_KEY = "vm-cache-size.vs.default";
        internal const string VM_CACHE_SIZE_DEFAULT = "32";                            // 32 M 


        /// <summary>
        /// Consts for file names generation
        /// </summary>
        internal const string SYSID = "vsto";
        internal const string SYSMJ = "0001";
        internal const string SYSMN = "0000";

        internal const int SPACE_NAME_LEN = 32;

        /// <summary>
        /// Lock file name
        /// </summary>
        internal static string LOCK_FILE_NAME
        {
            get { return SYSID + SYSMJ + "." + SYSMN + ".vlck"; }
        }

        /// <summary>
        /// Catalog file name
        /// </summary>
        internal static string CTLG_FILE_NAME
        {
            get { return SYSID + SYSMJ + "." + SYSMN + ".vctl"; }
        }

        /// <summary>
        /// Transaction file name
        /// </summary>
        internal static string TA_FILE_NAME
        {
            get { return SYSID + SYSMJ + "." + SYSMN + ".vsta"; }
        }

        /// <summary>
        /// Log base file name
        /// </summary>
        /// <param name="n">File seq number</param>
        /// <returns></returns>
        internal static string LOG_FILE_NAME(string path, string name, string prefix = "")
        {
            return path + "\\" + ((prefix == "") ? "" : prefix + "_") + "VSXLOG_" + name;
        }

        /// <summary>
        /// Log data file name
        /// </summary>
        /// <param name="n">File seq number</param>
        /// <returns></returns>
        internal static string LOG_DATA_FILE_NAME(string path, string name, string prefix = "")
        {
            return LOG_FILE_NAME(path, name, prefix) + ".vdat";
        }

        /// <summary>
        /// Log index file name
        /// </summary>
        /// <param name="n">File seq number</param>
        /// <returns></returns>
        internal static string LOG_INDEX_FILE_NAME(string path, string name, string prefix = "")
        {
            return LOG_FILE_NAME(path, name, prefix) + ".vidx";
        }

        /// <summary>
        /// Dump file name
        /// </summary>
        /// <param name="n">File seq number</param>
        /// <returns></returns>
        internal static string DUMP_FILE_NAME(string nm, int part)
        {
            return SYSID + SYSMJ + "." + nm.ToLower() + "." + ((part < 0) ? "*" : part.ToString("D4")) + ".vdmp";
        }

        /// <summary>
        /// Get space file name
        /// </summary>
        /// <param name="nm">Name</param>
        /// <param name="part">Partition number</param>
        /// <returns></returns>
        internal static string SPACE_FILE_NAME(string nm, int part)
        {
            return SYSID + SYSMJ + "." + nm.ToLower() + "." + part.ToString("D4") + ".vspc";
        }

        /// <summary>
        /// Get index name w/o space prefix
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string ParseIndexName(string name)
        {
            string n = name.Trim().ToLower();
            int pos = n.IndexOf(DEFS.INDEX_NAME_DELIMITER);
            return (pos < 0) ? n : n.Remove(0, pos + 2);
        }

        /// <summary>
        /// Get index space name w/o index name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string ParseIndexSpace(string name)
        {
            string n = name.Trim().ToLower();
            int pos = n.IndexOf(DEFS.INDEX_NAME_DELIMITER);
            return (pos < 0) ? "" : n.Substring(0, pos);
        }

        public static string PrepareFullIndexName(string space, string name)
        {
            string s = (space.Trim() == "") ? name.Trim() : space.Trim() + DEFS.INDEX_NAME_DELIMITER + name.Trim();
            return s.ToLower();
        }

        /// <summary>
        /// Maximum backup segment file size
        /// </summary>
        internal const long MAX_BACKUP_FILE_SIZE = 1073741824;

        ////////////////////////////////////////////////////////////////
        /////////////////// SPACE SIGNATURES ///////////////////////////
        ////////////////////////////////////////////////////////////////
        /// <summary>
        /// FBQE Header signature
        /// </summary>
        internal const string FBQE_HEADER_SIGNATURE = "$fqh";

        /// <summary>
        /// FBQE signature
        /// </summary>
        internal const string FBQE_SIGNATURE = "$fqe";

        /// <summary>
        /// KeyHelper V-header signature
        /// </summary>
        internal const string V_HEADER_SIGNATURE = "$vhd";

        /// <summary>
        /// KeyHelper H-header signature
        /// </summary>
        internal const string H_HEADER_SIGNATURE = "$hhd";

        /// <summary>
        /// Dump signature
        /// </summary>
        internal const string DUMP_SIGNATURE = "$dmp";
        internal const string DUMP_SIGNATURE_INCOMPLETE = "$wrk";

        /// <summary>
        /// Dump: start keys signature
        /// </summary>
        internal const string DUMP_KEYS_SIGNATURE = "$key";

        /// <summary>
        /// Dump: end keys signature
        /// </summary>
        //internal const string DUMP_KEND_SIGNATURE = "$key$end";

        ////////////////////////////////////////////////////////////////
        /////////////////// INDEXER SIGNATURES /////////////////////////
        ////////////////////////////////////////////////////////////////

        /// <summary>
        /// AVLNODE signature
        /// </summary>
        internal const string AVL_SIGNATURE = "$avn";

        /// <summary>
        /// Index signature
        /// </summary>
        internal const string INDEX_SIGNATURE = "$idx";

        ////////////////////////////////////////////////
        ////////// Space predefined pools //////////////
        ////////////////////////////////////////////////
        internal const short POOL_FREE_SPACE = 0;                            // Free space descriptors
        internal const short POOL_KEY = -1;                                  // Key management blocks
        internal const short POOL_INDEX = -2;                                // Index descriptors (nodes are in dynamic pools)
        internal const short POOL_USER_DEFINED = -3;                         // User pools allocation
        internal const short POOL_DYNAMIC = -4;                              // Dynamic pools allocation
        // -5 - -31 System reserved

        /// <summary>
        /// User defined and dynamic pools starting numbers
        /// </summary>

        internal const short POOL_MIN_USER_DEFINED = 1;                      // Min user defined pool number
        internal const short POOL_MIN_DYNAMIC = 4096;                        // Min dynamic pool number

        /// <summary>
        /// Allocation size
        /// </summary>
        internal const long ALLOCATION_USER_DEFINED = 4096;                 // Block size for user's pool allocation descriptors (initial/extension)
        internal const long ALLOCATION_DYNAMIC = 4096;                      // Block size for dynamic pool allocation descriptors (initial/extension)

        /// <summary>
        /// Each pool descriptor is 16-bytes:
        /// 1st  descriptor address (8)
        /// last descriptor address (8)
        /// If no descriptors in pool - 0/0
        /// </summary>
        internal const int SYSTEM_POOL_DESCRIPTOR_LENGTH = 16;
        internal const int SYSTEM_POOL_DESCRIPTOR_NUMBER = 64;                // Reserved number of the system descriptors

        ////////////////////////////////////////////////
        //////// System reserved area: 0-16384 /////////
        ////////////////////////////////////////////////

        /// <summary>
        /// SYSTEM STATUS
        /// +0(8) System restore status
        /// If not 0 - Load is not completed successfully (unconsistent). 
        /// In this case contains restore binary timestamp.
        /// </summary>
        internal const long SYSTEM_STATUS_ADDRESS = 0;
        internal const long SYSTEM_STATUS_LENGTH = 8;

        /// <summary>
        /// SYSTEM OWNER
        /// +8(32)Space owner info
        /// Component/system name owning this space
        /// </summary>
        internal const long SYSTEM_OWNER_ADDRESS = SYSTEM_STATUS_ADDRESS + SYSTEM_STATUS_LENGTH;
        internal const long SYSTEM_OWNER_LENGTH = 32;

        /// <summary>
        /// NEW ALLOCATION AREA
        /// +40(984)New allocation address
        /// Use by Extend/AddPartition - temporary place to pick up by Open 
        /// </summary>
        internal const long SYSTEM_ALLOCATION_ADDRESS = SYSTEM_OWNER_ADDRESS + SYSTEM_OWNER_LENGTH;
        internal const long SYSTEM_ALLOCATION_LENGTH = 984;

        /// <summary>
        /// SYSTEM POOL AREA
        /// +1024(512) System memory pool descriptors area
        /// </summary>
        internal const long SYSTEM_POOL_AREA_ADDRESS = SYSTEM_ALLOCATION_ADDRESS + SYSTEM_ALLOCATION_LENGTH;              // 1024
        internal const long SYSTEM_POOL_AREA_LENGTH = SYSTEM_POOL_DESCRIPTOR_NUMBER * SYSTEM_POOL_DESCRIPTOR_LENGTH;      // 1024

        /// <summary>
        /// FBQE HEADER
        /// +2048(50 + 64 = 114) Free space descriptors initial allocation address
        /// Address of the allocation Descriptor for the FBQE HEADER + 1st block
        /// </summary>
        internal const long FBQE_HEADER_ADDRESS = SYSTEM_POOL_AREA_ADDRESS + SYSTEM_POOL_AREA_LENGTH;       // Header's VSAllocation address
        internal const long FBQE_HEADER_LENGTH_FULL = VSFreeSpaceManager.FBQE_HEADER_LENGTH + DEFS.BaseDescriptorLength;   // Header length + VSAllocation descriptor length

        /// <summary>
        /// Firts FBQE F-block allocation
        /// +2162(9216)
        /// </summary>
        internal const long FBQE_FIRST_BLOCK_ADDRESS = FBQE_HEADER_ADDRESS + FBQE_HEADER_LENGTH_FULL;

        /// <summary>
        /// Lowest addess available for allocation
        /// +11378(5006)
        /// </summary>
        internal const long SYSTEM_RESERVED = FBQE_FIRST_BLOCK_ADDRESS + FBQE_ALLOCATION_LENGTH;
        internal const long SYSTEM_RESERVED_LENGTH = 5006;

        /// <summary>
        /// Lowest addess available for allocation
        /// +16384
        /// </summary>
        internal const long SYSTEM_ALLOCATION_SPACE = SYSTEM_RESERVED + SYSTEM_RESERVED_LENGTH;

        /// <summary>
        /// Free space descriptors initial allocation length
        /// </summary>
        internal const int FBQE_ALLOCATION_NUMBER = 256;                                                            // Number of FBQE for allocation
        internal const int FBQE_MIN_FREE_NUMBER = 32;                                                               // Minimal free elements number threshold for expanding
        internal const int FBQE_ALLOCATION_LENGTH = VSFreeSpaceManager.FBQE_LENGTH * FBQE_ALLOCATION_NUMBER;   // Initial size of the block (8192bytes)

        /// <summary>
        /// Minimal allocation size (bytes)
        /// </summary>
        internal const long MIN_SPACE_ALLOCATION_CHUNK = 64;
        internal const long MAX_SPACE_ALLOCATION_CHUNKS = 32767;


        /// <summary>
        /// Cache size for FBQE elements
        /// </summary>
        internal const int FBQE_CACHE_SIZE = 8;




        /// <summary>
        /// Operations
        /// </summary>
        internal const int OP_READ = 1;
        internal const int OP_WRITE = 2;

        /// <summary>
        /// VMHelper Constants
        /// </summary>

        internal const int SPACE_CLOSED = 0;                                          //space is closed  
        internal const int SPACE_OPENED = 1;                                          //space is opened  
        internal const int SPACE_LOCKED = 2;                                          //space is locked

        internal const int PAGE_FREE = 0;                                             //page is NOT loaded ib buffer   
        internal const int PAGE_READ = 1;                                             //page is loaded ib buffer   
        internal const int PAGE_WRITE = 2;                                            //write is pending for page   

        internal const bool PAGE_UNLOCKED = false;                                    //page is NOT locked in the buffer
        internal const bool PAGE_LOCKED = true;                                       //page is locked in the buffer

        internal const int DEFAULT_PAGE_SIZE = 16;                                    // Default page size = 16Kb
        internal const int DEFAULT_SPACE_SIZE = 5;                                    // Default space size = 5Mb
        internal const int DEFAULT_EXT_SIZE = 5;                                      // Default space ext size = 5Mb

        internal const int SYSTEM_USED_PAGE_SPACE = 16;

        /// <summary>
        /// VSEngine constructor path constants
        /// </summary>
        public const string PATH_CURRENT = "*";                                       // Default (current) directory
        public const string PATH_UNDEFINED = "$$UNDEFINED$$";                         // Undefined path string

        public const int PAGE_SEGMENTS_NUMBER = 8192;                                 // Max number of space segments for allocation

        /// <summary>
        /// Storage states
        /// </summary>
        public const int STATE_UNDEFINED = -1;
        public const int STATE_DEFINED = 0;
        public const int STATE_OPENED = 1;

        /// <summary>
        /// Encryption keys
        /// Space pages
        /// </summary>
        public const string ENCRYPT_SPACE = "$vsspacepage$00001.00001$vstorage.security.0$";
        
        /// <summary>
        /// Dump file
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public const string ENCRYPT_DUMP = "$vsdumpfile$00001.00001$vstorage.security.0$";
        
        /// <summary>
        /// User-defined
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string ENCRYPT_UDF(string value)
        {
            return "$vsudfdata$00001.00001$" + value.Trim().ToLower() + "$vstorage.security.0$";
        }

        /// <summary>
        /// Encrypt staus codes
        /// </summary>
        public const uint DATA_ENCRYPTED = 0x000000ff;
        public const uint DATA_NOT_ENCRYPTED = 0x000000fe;

        /// <summary>
        /// Encrypt symbolic code values 
        /// </summary>
        public const string CT_ENCRYPTED = "E";
        public const string CT_UNENCRYPED = "U";

        ////////////////////////////////////////////////////////////////////////////
        //////////////// ERROR HANDLING MESSAGES AND CODES /////////////////////////
        ////////////////////////////////////////////////////////////////////////////
        internal static string[] ERROR_MESSAGES =
            {
            "UNDEFINED",                                                                // 00
            "Unable to lock storage",                                                   // 01
            "Space is not found",                                                       // 02
            "Cannot extend multipartition space",                                       // 03
            "Storage path is not found",                                                // 04
            "Space file already exists",                                                // 05
            "Invalid block signature",                                                  // 06
            "Address is out of space boundaries",                                       // 07
            "Invalid descriptor address",                                               // 08
            "Restore has not been completed for space. Re-creation is required.",       // 09
            "Write attempt in Read-Only mode",                                          // 10
            "Transaction is pending in read-only mode",                                 // 11
            "Invalid pool number",                                       // 12
            "Space is not available for allocation",                                    // 13
            "Free space pool: invalid pool number",                                     // 14
            "Free space: invalid address",                                              // 15
            "Open storage error",                                                       // 16
            "Attach space error",                                                       // 17
            "Transaction error",                                                        // 18
            "Invalid relative read/write address error",                                // 19
            "Invalid extension parameters: " +
            "pool and generateId cannot be specified for extension",                    // 20
            "Maximum space allocation chunks number is reached",                        // 21
            "Key is not found (probably DB structure error)",                           // 22
            "Invalid predefined key sequence",                                          // 23
            "Create space error",                                                       // 24
            "Storage unable to complete operation",                                     // 25
            "Object field read error",                                                  // 26
            "Object field write error",                                                 // 27
            "Invalid read/write length",                                                // 28
            "Invalid field type (object structure error)",                              // 29
            "Invalid read/write operation, allocation is not raw or out of fixed adderess",// 30
            "Remove space error, space is used as index space",                         // 31
            "Storage path is undefined",                                                // 32
            "Invalic CRC32 for page",                                                   // 33
            "I/O Error",                                                                // 34
            "UNDEFINED",                                                                // 35
            "UNDEFINED",                                                                // 36
            "UNDEFINED",                                                                // 37
            "UNDEFINED",                                                                // 38
            "UNDEFINED",                                                                // 39
            "UNDEFINED",                                                                // 40
            "UNDEFINED",                                                                // 41
            "UNDEFINED",                                                                // 42
            "UNDEFINED",                                                                // 43
            "UNDEFINED",                                                                // 44
            "UNDEFINED",                                                                // 45
            "UNDEFINED",                                                                // 46
            "UNDEFINED",                                                                // 47
            "UNDEFINED",                                                                // 48
            "UNDEFINED",                                                                // 49
            // INDEXER ERROR MESSAGES     
            "Create index error",                                                       // 50
            "Open index error",                                                         // 51
            "Delete index error",                                                       // 52
            "Index is not opened",                                                      // 53
            "ID is not specified for non-unique index",                                 // 54
            "Invalid operation",                                                        // 55
            "UNDEFINED",                                                                // 56
            "UNDEFINED",                                                                // 57
            "UNDEFINED",                                                                // 58
            "UNDEFINED",                                                                // 59
            "UNDEFINED"                                                                 // 60
        };

        internal static string prefix = "VStorage Error ";

        /// <summary>
        /// ERROR CODES
        /// </summary>
        public const int E0001_UNABLE_TO_LOCK_CODE = 1;
        public const int E0002_SPACE_NOT_FOUND_CODE = 2;
        public const int E0003_EXTEND_ERROR_CODE = 3;
        public const int E0004_STORAGE_NOT_FOUND_CODE = 4;
        public const int E0005_FILE_ALREADY_EXISTS_CODE = 5;
        public const int E0006_INVALID_SIGNATURE_CODE = 6;
        public const int E0007_INVALID_ADDRESS_CODE = 7;
        public const int E0008_INVALID_DESCRIPTOR_CODE = 8;
        public const int E0009_RESTORE_NOT_COMPLETED_CODE = 9;
        public const int E0010_READ_ONLY_CODE = 10;
        public const int E0011_TRANSACTION_PENDING_CODE = 11;
        public const int E0012_INVALID_POOL_NUMBER_CODE = 12;
        public const int E0013_SPACE_NOT_AVAILABLE_CODE = 13;
        public const int E0014_INVALID_POOL_NUMBER_CODE = 14;
        public const int E0015_INVALID_ADDRESS_CODE = 15;
        public const int E0016_OPEN_STORAGE_ERROR_CODE = 16;
        public const int E0017_ATTACH_SPACE_ERROR_CODE = 17;
        public const int E0018_TRANSACTION_ERROR_CODE = 18;
        public const int E0019_INVALID_OP_ADDRESS_ERROR_CODE = 19;
        public const int E0020_INVALID_EXTENSION_PARAMETERS_ERROR_CODE = 20;
        public const int E0021_MAX_ALLOCATION_CHUNKS_REACHED_CODE = 21;
        public const int E0022_KEY_NOT_FOUND_CODE = 22;
        public const int E0023_INVALID_KEY_SEQUENCE_CODE = 23;
        public const int E0024_CREATE_SPACE_ERROR_CODE = 24;
        public const int E0025_STORAGE_UNABLE_TO_COMPLETE_CODE = 25;
        public const int E0026_FIELD_READ_ERROR_CODE = 26;
        public const int E0027_FIELD_WRITE_ERROR_CODE = 27;
        public const int E0028_INVALID_LENGTH_ERROR_CODE = 28;
        public const int E0029_INVALID_FIELD_TYPE_CODE = 29;
        public const int E0030_INVALID_WRITE_OP_CODE = 30;
        public const int E0031_ERROR_REMOVING_INDEX_SPACE_CODE = 31;
        public const int E0032_STORAGE_PATH_UNDEFINED_CODE = 32;
        public const int E0033_INVALID_CRC_CODE = 33;
        public const int E0034_IO_ERROR_CODE = 34;

        /////////////////////////////////////////////////////////////////////////////
        ///////////// INDEXER EXCEPTION  ////////////////////////////////////////////
        /////////////////////////////////////////////////////////////////////////////

        public const int E0050_CREATE_INDEX_ERROR_CODE = 50;
        public const int E0051_OPEN_INDEX_ERROR_CODE = 51;
        public const int E0052_DELETE_INDEX_ERROR_CODE = 52;
        public const int E0053_INDEX_NOT_OPENED_CODE = 53;
        public const int E0054_ID_NOT_SPECIFIED_CODE = 54;
        public const int E0055_INDEX_INVALID_OP_CODE = 55;


    }
}
