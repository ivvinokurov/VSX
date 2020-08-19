using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace VStorage
{
    public class VSConfig
    {
        /// <summary>
        /// Structure
        /// Line 1: [VSTORAGE] - if structure is correct; [PENDING] - if updating
        /// Space name: [name]
        /// parameter=valuse
        /// </summary>

        internal string CONFIG_DIR = "";

        internal bool ENCRYPT = false;
        internal string ste = "";



        private List<VSConfigDescriptor> dl = null;
        private const string sg_ok = "$VSTORAGE$";
        private string catalog_file_name = "";
        private string backup_file_name = "";
        //private FileStream fs;
        
        //Definitions
        private const string DEF_ID = "id";
        private const string DEF_SIZE = "size";
        private const string DEF_EXTENSION = "extension";
        private const string DEF_PARTITIONS = "partitions";
        private const string DEF_PATH = "path";
        private const string DEF_INDEXSPACE = "index_space";
        private const string DEF_PAGESIZE = "page_size";
        private const string DEF_TIMESTAMP = "creation_timestamp";
        private const string DEF_SIGNATURE = "signature";

        private const string DEF_ERROR = "$$error$$";

        private string Error = "";

        private int err_line = -1;
        private int line = 1;

        /// <summary>
        /// Empty path - In Memory Option
        /// </summary>
        /// <param name="path"></param>
        public VSConfig(string path)
        {
            this.dl = new List<VSConfigDescriptor>();

            if (path == "")
            {
                catalog_file_name = "";
                backup_file_name = "";
                CONFIG_DIR = "~IMO~";
            }
            else
            {
                catalog_file_name = path + "\\" + DEFS.CTLG_FILE_NAME;
                backup_file_name = catalog_file_name + ".bak";

                string[] s = VSLib.Parse(path, "\\");
                CONFIG_DIR = s[s.Length - 1];

                Load();
            }
        }

        /// <summary>
        /// Load catalog content
        /// </summary>
        public void Load()
        {
            int pos = 0;
            string s = "";
            

            err_line = -1;

            if (!File.Exists(catalog_file_name))
                File.WriteAllBytes(catalog_file_name, VSLib.ConvertStringToByte(sg_ok + DEFS.VSTORAGE_VERSION + "$" + "U" + "$" + DEFS.DELIM_NEWLINE));         // 'U'  dont encrypt (+19)

            s = VSLib.ConvertByteToString(File.ReadAllBytes(catalog_file_name));
            // Parse file
            pos = s.IndexOf(DEFS.DELIM_NEWLINE);
            if (pos < 0)
                return;
            // Get encryption status
            string sth = s.Substring(0, pos);
            if (sth.Length < 21)
                err_line = 1;
            else
            {
                // Encryption
                ste = sth.Substring(19, 1).ToUpper();
                ENCRYPT = (sth.Substring(19, 1) == "E");

                pos += DEFS.DELIM_NEWLINE.Length;
                line = 1;
                VSConfigDescriptor desc = null;

                while ((pos < s.Length) & (err_line < 0))
                {
                    string par = "";
                    int ln = 0;
                    int new_pos = s.IndexOf(DEFS.DELIM_NEWLINE, pos);
                    if (new_pos < 0)
                        ln = s.Length - pos;
                    else
                        ln = new_pos - pos;

                    if (ln > 0)
                        par = s.Substring(pos, ln).Trim();
                    line++;
                    pos += (ln + DEFS.DELIM_NEWLINE.Length);

                    if (par != "")
                    {
                        if (par.Substring(0, 1) == "[")
                        { // Parse new descriptor
                            desc = null;
                            string sname = "";
                            int eb = par.IndexOf("]", 1);
                            if (eb < 0)
                                err_line = line;
                            else
                            {
                                if (eb > 1)
                                    sname = par.Substring(1, eb - 1);
                                if (sname != "")
                                {
                                    sname = sname.Trim().ToLower();
                                    for (int i = 0; i < dl.Count; i++)
                                        if (dl[i].Name == sname)
                                        {
                                            desc = dl[i];
                                            break;
                                        }
                                    if (desc == null)
                                    {
                                        desc = new VSConfigDescriptor(this);
                                        desc.name = sname;
                                        dl.Add(desc);
                                    }
                                }
                                else
                                    err_line = line;
                            }
                        }
                        else
                        { // Parse parameters
                            string s_val = "";
                            long n_val = 0;
                            if (desc == null)
                                err_line = line;
                            else
                            {
                                par += "                            ";

                                if (parse_long(ref par, DEF_ID, out n_val))
                                {
                                    if (n_val >= 0)
                                        desc.id = (short)n_val;
                                }
                                else if (parse_long(ref par, DEF_SIZE, out n_val))
                                {
                                    if (n_val >= 0)
                                        desc.space_size_pg = n_val;
                                }
                                else if (parse_long(ref par, DEF_EXTENSION, out n_val))
                                {
                                    if (n_val >= 0)
                                        desc.extension_pg = n_val;
                                }
                                else if (parse_long(ref par, DEF_PAGESIZE, out n_val))
                                {
                                    if (n_val >= 0)
                                        desc.page_size_kb = n_val;
                                }
                                else if (parse_long(ref par, DEF_PARTITIONS, out n_val))
                                {
                                    if (n_val >= 0)
                                        desc.partitions = n_val;
                                }
                                else if (parse_string(ref par, DEF_PATH, out s_val))
                                {
                                    if (s_val != DEF_ERROR)
                                        desc.path = s_val;
                                }
                                else if (parse_string(ref par, DEF_INDEXSPACE, out s_val))
                                {
                                    if (s_val != DEF_ERROR)
                                        desc.indexspace = s_val;
                                }
                                else if (parse_string(ref par, DEF_TIMESTAMP, out s_val))
                                {
                                    if (s_val != DEF_ERROR)
                                        desc.creation_timestamp = s_val;
                                }
                                else if (parse_string(ref par, DEF_SIGNATURE, out s_val))
                                {
                                    if (s_val != DEF_ERROR)
                                        desc.signature = s_val;
                                }
                                else
                                    err_line = line;
                            }
                        }
                    }
                }
            }
            if (err_line >= 0)
                throw new VSException(DEFS.E0016_OPEN_STORAGE_ERROR_CODE, "- invalid catalog entry at line " + err_line.ToString());

            for (int i = 0; i < dl.Count; i++)
            {
                string sg = dl[i].Signature;
                dl[i].CalculateSignature();
                if (dl[i].Signature != sg)
                    throw new VSException(DEFS.E0016_OPEN_STORAGE_ERROR_CODE, "- missing or invalid space signature for '" + dl[i].Name);
            }
        }

        /// <summary>
        /// Get string value
        /// </summary>
        /// <param name="sourse"></param>
        /// <returns></returns>
        private string GetStringValue(string p)
        {
            if (p.Substring(0, 1) != "=")
                return DEF_ERROR;
            return p.Remove(0, 1).Trim();
        }

        /// <summary>
        /// Get numeric value
        /// </summary>
        /// <param name="sourse"></param>
        /// <returns></returns>
        private long GetNumericValue(string p)
        {
            long n = -1;
            try
            {
                string ost = "";
                string ist = GetStringValue(p);
                for (int i = 0; i < ist.Length; i++)
                {
                    int res = 0;
                    string sym = ist.Substring(i, 1);
                    if (int.TryParse(sym, out res))
                        ost += sym;
                    else
                        break;
                }
                n = VSLib.ConvertStringToLong(ost);
            }
            catch (Exception e1)
            {
                this.Error = e1.Message;
                return -1;
            }
            return n;
        }


        /// <summary>
        /// Save catalog content
        /// </summary>
        public void Save()
        {
            string ost = sg_ok + DEFS.VSTORAGE_VERSION + "$" + ste + "$" + DEFS.DELIM_NEWLINE;
            for (int i = 0; i < dl.Count; i++)
            {
                ost += "[" + dl[i].Name + "]" + DEFS.DELIM_NEWLINE;

                ost += DEF_ID + "=" + dl[i].Id.ToString() + DEFS.DELIM_NEWLINE;
                ost += DEF_PAGESIZE + "=" + dl[i].page_size_kb.ToString() + "Kb" + DEFS.DELIM_NEWLINE;
                ost += DEF_SIZE + "=" + dl[i].space_size_pg.ToString() + "Pages" + DEFS.DELIM_NEWLINE;
                ost += DEF_EXTENSION + "=" + dl[i].extension_pg.ToString() + "Pages" + DEFS.DELIM_NEWLINE;
                ost += DEF_PARTITIONS + "=" + dl[i].partitions.ToString() + DEFS.DELIM_NEWLINE;

                ost += DEF_TIMESTAMP + "=" + dl[i].creation_timestamp + DEFS.DELIM_NEWLINE;
                ost += DEF_PATH + "=" + dl[i].path + DEFS.DELIM_NEWLINE;
                ost += DEF_INDEXSPACE + "=" + dl[i].indexspace + DEFS.DELIM_NEWLINE;
                
                // Create signature
                ost += DEF_SIGNATURE + "=" + dl[i].CalculateSignature() + DEFS.DELIM_NEWLINE;
            }

            if (File.Exists(catalog_file_name))
            {
                if (File.Exists(backup_file_name))
                    File.Delete(backup_file_name);
                File.Move(catalog_file_name, backup_file_name);
            }
            File.WriteAllBytes(catalog_file_name, VSLib.ConvertStringToByte(ost));
        }

        /// <summary>
        /// Get catalog descriptor by name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public VSConfigDescriptor Get(string name)
        {
            for (int i = 0; i < dl.Count; i++)
                if (dl[i].Name == name.Trim().ToLower())
                    return dl[i];
            return null;
        }

        /// <summary>
        /// Update or create catalog descriptor
        /// Size - Mb
        /// Extension - Mb
        /// pagesize - Kb
        /// </summary>
        /// <param name="desc"></param>
        public VSConfigDescriptor Create(string name, long size, long extension = 0, long pagesize = 0, string path = "")
        {
            for (int i = 0; i < dl.Count; i++)
                if (dl[i].Name == name.Trim().ToLower())
                    return null;
            VSConfigDescriptor d = new VSConfigDescriptor(this);
            // Name
            d.name = name.Trim().ToLower();

            // Page size (Kb)
            long p = pagesize;
            if (p <= 0)
                p = 16;
            else if (p < 4)
                p = 4;
            else if (p > 64)
                p = 64;
            d.page_size_kb = p;

            // Space size (pages)
            p = size;
            if (p <= 0)
                p = 5;
            d.space_size_pg = (p * 1048576) / d.PageSize;

            // Extension (pages)
            p = extension;
            if (p <= 0)
                p = 0;
            d.extension_pg = (p * 1048576) / d.PageSize;

            // Path
            d.path = path.Trim();
            
            // Timestamp
            d.creation_timestamp = DateTime.Now.ToString("u");

            // Calculate new id
            short new_id = 1;
            bool ready = false;
            while (!ready)
            {
                ready = true;
                for (int i = 0; i < dl.Count; i++)
                {
                    if (new_id ==  dl[i].Id)
                    {
                        ready = false;
                        new_id++;
                        break;
                    }
                }
            }
            d.id = new_id;
            dl.Add(d);
            return d;
        }

        /// <summary>
        /// Delete catalog descriptor
        /// </summary>
        /// <param name="name"></param>
        public void Delete(string name)
        {
            for (int i = 0; i < dl.Count; i++)
                if (dl[i].Name == name.Trim().ToLower())
                {
                    dl.RemoveAt(i);
                    return;
                }
        }

        /// <summary>
        /// Get/Set encryption
        /// </summary>
        public bool Encrypt
        {
            get { return ENCRYPT; }
            set
            {
                if (catalog_file_name != "")
                {
                    ENCRYPT = value;
                    ste = value ? "E" : "U";
                    Save();
                }
            }
        }

        /// <summary>
        /// Spaces counter
        /// </summary>
        public int Count
        {
            get { return dl.Count; }
        }

        /// <summary>
        /// Descriptors
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public VSConfigDescriptor this[int i]
        {
            get
            {
                if ((i < 0) | (i > dl.Count))
                    return null;
                return dl[i];
            }
        }

        /// <summary>
        /// Parse string value
        /// </summary>
        /// <returns></returns>
        private bool parse_string(ref string par, string templ, out string val)
        {
            val = "";
            if (par.Substring(0, templ.Length) == templ)
            {
                val = GetStringValue(par.Remove(0, templ.Length).Trim());
                if (val == DEF_ERROR)
                    err_line = line;

                return true;
            }
            else
                return false;
        }

        /// <summary>
        /// Parse numeric value
        /// </summary>
        /// <returns></returns>
        private bool parse_long(ref string par, string templ, out long val)
        {
            val = -1;
            if (par.Substring(0, templ.Length) == templ)
            {
                val = GetNumericValue(par.Remove(0, templ.Length).Trim());
                if (val < 0)
                    err_line = line;

                return true;
            }
            else
                return false;
        }

    }
}
