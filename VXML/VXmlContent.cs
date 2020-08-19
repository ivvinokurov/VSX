using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VStorage;
using System.IO;

namespace VXML
{
    /////////////////////////////////////////////////////////////
    //////////////////////// VXmlContent ////////////////////////
    /////////////////////////////////////////////////////////////
    public class VXmlContent : VXmlNode
    {
        /// <summary>
        /// Content header
        /// </summary>
        // Length
        private const int HDR_LENGTH_POS = 0;
        private const int HDR_LENGTH_LEN = 8;
        
        // Header length
        private const long CONTENT_HDR_SIZE = HDR_LENGTH_POS + HDR_LENGTH_LEN;

        internal VXmlContent(VSpace ns, VSpace cs)
            : base(ns, cs)
        {
        }

        /////////////////////////////////////////////////////////////////////////
        ///////////// METHODS ///////////////////////////////////////////////////
        /////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Upload content (replace if exists)
        /// </summary>
        /// <param name="filename">Full path to the file</param>
        public void Upload(string filename, bool setattr = true)
        {
            remove_content();
            byte[] b = File.ReadAllBytes(filename);
            save_content_bytes(ref b);

            if (setattr)
            {
                this.filename = System.IO.Path.GetFileName(filename);
                this.path = System.IO.Path.GetDirectoryName(filename);
            }
        }

        /// <summary>
        /// Save content to the specified directory
        /// </summary>
        /// <param name="filename">Path to save file</param>
        /// <param name="filename">File name. If empty - saved name</param>
        public void Download(string filename = "")
        {
            string fname = filename;
            if (fname == "")
                fname = this.filename;
            if (fname == "")
                fname = "NONAME.content";

            File.WriteAllBytes(fname, this.ContentBytes);
        }

        /// <summary>
        /// Used by "ContentBytes property and CheckIn
        /// </summary>
        /// <param name="bytes"></param>
        internal void save_content_bytes(ref byte[] value)
        {
            long ln = value.Length;
            if (ln > 0)
            {
                VSObject a = content_space.Allocate(ln + 12, DEFX.NODE_TYPE_CONTENT, DEFX.CONTENT_BLOCKSIZE);
                CONT_ID = a.Id;
                a.Write(HDR_LENGTH_POS, ln);                         // Save length
                a.Write(CONTENT_HDR_SIZE, value, ln);
            }
        }

        /////////////////////////////////////////////////////////////////////////
        ///////////// PROPERTIES ////////////////////////////////////////////////
        /////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Ref (saved) - full path, used by auto upload/download XML capabilities
        /// </summary>
        public string fileref
        {
            get { return GetAttribute("fileref"); }
            set { SetAttribute("fileref", value); }
        }

        /// <summary>
        /// File name (for user's usage)
        /// </summary>
        public string filename
        {
            get { return GetAttribute("filename"); }
            set { SetAttribute("filename", value); }
        }

        /// <summary>
        /// File path (for user's usage)
        /// </summary>
        public string path
        {
            get { return GetAttribute("path"); }
            set { SetAttribute("path", value); }
        }

        /// <summary>
        /// Content string representation
        /// </summary>
        public string ContentString
        {
            get { return VSLib.ConvertByteToString(ContentBytes); }
            set { ContentBytes = VSLib.ConvertStringToByte(value); }
        }

        /// <summary>
        /// Content byte representation
        /// </summary>
        public byte[] ContentBytes
        {
            get
            {
                if ((type == DEFX.NODE_TYPE_CONTENT) & (CONT_ID > 0))
                {
                    VSObject a = content_space.GetObject(CONT_ID);

                    long l = a.ReadLong(HDR_LENGTH_POS);
                    if (l > 0)
                        return a.ReadBytes(CONTENT_HDR_SIZE, l);
                }
                return new byte[0];
            }
            set
            {
                remove_content();
                save_content_bytes(ref value);
                this.path = "";
                this.filename = "NONAME";
            }
        }

        /// <summary>
        /// Content file size in bytes
        /// </summary>
        public long Length
        {
            get
            {
                if ((type != DEFX.NODE_TYPE_CONTENT) | (CONT_ID == 0))
                    return 0;
                VSObject a = content_space.GetObject(CONT_ID);
                return a.ReadLong(HDR_LENGTH_POS);
            }
        }
    }
}
