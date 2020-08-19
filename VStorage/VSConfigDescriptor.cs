using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VStorage
{
    public class VSConfigDescriptor
    {
        public VSConfig CONFIG = null;
        private const long Mb = 1048576;
        private const long K = 1024;

        /// <summary>
        /// Paremeters used by Create/Load/Save
        /// </summary>
        internal long page_size_kb = 0;                      // Logical Page size in Kbytes (used space)

        internal long space_size_pg = 0;                     // Logical space size (pages)

        internal long extension_pg = 0;                      // Logical sextension size (pages)

        internal string name = "";                           // Space name

        internal string path = "";                           // Space file(s) path

        internal string creation_timestamp = "";             // Creation timestamp

        internal short id = 0;                               // Space ID

        internal long partitions = 1;                        // Number of partitions

        internal string signature = "";                      // Space signature (to prevent chane of key attrs

        internal string indexspace = "";                     // Index space name (optional)
        


        /// <summary>
        /// IMO indicator
        /// </summary>
        internal bool IMO = false;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="c"></param>
        public VSConfigDescriptor(VSConfig c)
        {
            CONFIG = c;
        }

        /// <summary>
        /// Space name
        /// </summary>
        public string Name
        {
            get { return name; }
        }

        /// <summary>
        /// Page size (bytes)
        /// </summary>
        public long PageSize
        {
            get { return page_size_kb * 1024; }
        }

        /// <summary>
        /// Space size (bytes)
        /// </summary>
        public long SpaceSize
        {
            get { return space_size_pg * PageSize; }
        }

        /// <summary>
        /// Space size (Mb)
        /// </summary>
        public long SpaceSizeMb
        {
            get { return (space_size_pg * PageSize) / Mb; }
        }

        /// <summary>
        /// Physical space size (bytes)
        /// </summary>
        internal long SysSpaceSize
        {
            get { return space_size_pg * (PageSize + DEFS.SYSTEM_USED_PAGE_SPACE); }
        }


        /// <summary>
        /// Extension (bytes)
        /// </summary>
        public long Extension
        {
            get { return extension_pg * PageSize; }
        }

        /// <summary>
        /// Extension (Mb)
        /// </summary>
        public long ExtensionMb
        {
            get { return (Extension / Mb); }
        }

        /// <summary>
        /// Extension (pages)
        /// </summary>
        public long Partitions
        {
            get { return partitions; }
        }


        /// <summary>
        /// Space path
        /// </summary>
        public string Path
        {
            get { return path; }
        }

        /// <summary>
        /// Space id
        /// </summary>
        public short Id
        {
            get { return id; }
        }

        /// <summary>
        /// Creation timestamp
        /// </summary>
        public string CreationTimestamp
        {
            get { return creation_timestamp; }
        }

        /// <summary>
        /// Signature value
        /// </summary>
        public string Signature
        {
            get { return signature; }
        }

        /// <summary>
        /// Calculate new signature
        /// </summary>
        /// <returns></returns>
        public string CalculateSignature()
        {
            ulong chs = (ulong)((Id * 123456789) - space_size_pg * 3 + extension_pg * 5 - page_size_kb * 7 + partitions * 9 - VSLib.ConvertStringToByte(CONFIG.ste)[0]);
            
            byte[] x = VSLib.ConvertStringToByte(indexspace.Trim().ToLower());
            for (int i = 0; i < x.Length; i++)
                chs += (ulong)(x[i] * 3);
                
           x = VSLib.ConvertStringToByte(name.Trim().ToLower());
            for (int i = 0; i < x.Length; i++)
                chs += (ulong)(x[i] * 5);
                
            signature = VSLib.ConvertULongToHexString(chs);
            return signature;
        }

        /// <summary>
        /// Index space name
        /// </summary>
        public string IndexSpace
        {
            get { return indexspace; }
        }

    }
}
