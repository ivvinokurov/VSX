using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace VStorage
{
    public class VSLogger
    {
        private string log_name;
        private string log_path;
        private string datafile;
        private string indexfile;

        private bool _ready = false;
        private long length = -1;
        private FileStream fs;          //Data file stream
        private FileStream fx;          //Index file stream

        private long current = 0;           //Current file position

        /// <summary>
        /// Constructor
        /// </summary>
        public VSLogger()
        {
        }

        /// <summary>
        /// Open Log
        /// </summary>
        /// <param name="path"></param>
        /// <param name="name"></param>
        public void Open(string path, string name)
        {
            log_path = path;
            log_name = name;
            datafile = DEFS.LOG_DATA_FILE_NAME(log_path, log_name);
            indexfile = DEFS.LOG_INDEX_FILE_NAME(log_path, log_name);

            //Check if directory exists
            if (System.IO.Directory.Exists(log_path))
            {
                if (System.IO.File.Exists(indexfile))
                {
                    fx = System.IO.File.OpenRead(indexfile);
                    length = fx.Length / 16;
                    fx.Close();
                }
                else
                {
                    length = 0;
                    fx = System.IO.File.Open(indexfile, FileMode.Create);
                    fx.Close();
                    fs = System.IO.File.Open(datafile, FileMode.Create);
                    fs.Close();
                }
                _ready = true;
            }
            else
                throw new Exception("Error: Directory is not found - " + log_path);
        }

        /// <summary>
        /// Close log
        /// </summary>
        public void Close()
        {
            log_path = "";
            log_name = "";
            datafile = "";
            indexfile = "";
            length = -1;
        }


        /// <summary>
        /// Write (append) record
        /// </summary>
        /// <param name="data"></param>
        public void Write(string data)
        {
            if (_ready)
            {
                long pos = 0;
                long len = 0;

                len = data.Length;

                fs = System.IO.File.Open(datafile, FileMode.Append);
                pos = fs.Length;

                byte[] b = System.Text.Encoding.Default.GetBytes(data);
                fs.Write(b, 0, b.Length);

                fx = System.IO.File.Open(indexfile, FileMode.Append);
                byte[] bi = BitConverter.GetBytes(pos);
                fx.Write(bi, 0, bi.Length);

                bi = BitConverter.GetBytes(len);
                fx.Write(bi, 0, bi.Length);

                length = fx.Length / 16;

                fs.Close();
                fx.Close();
            }
        }

        public string Read()
        {
            if (_ready)
            {
                string s = "";
                fs = System.IO.File.Open(datafile, FileMode.Open);
                fx = System.IO.File.Open(indexfile, FileMode.Open);
                
                if (length > current)
                    s = getRecord();

                fs.Close();
                fx.Close();

                return s;

            }
            else
                return "";
        }
        
        /// <summary>
        /// read next record from the file
        /// </summary>
        /// <returns></returns>
        private string getRecord()
        {
            if (current >= 0)
            {
                fx.Seek(current * 16, SeekOrigin.Begin);
                byte[] b = new byte[16];
                fx.Read(b, 0, 16);

                long pos = BitConverter.ToInt64(b, 0);
                long len = BitConverter.ToInt64(b, 8);

                b = new byte[len];
                fs.Seek(pos, SeekOrigin.Begin);
                fs.Read(b, 0, (int)len);
                current++;

                return System.Text.Encoding.Default.GetString(b, 0, (int)len - 2);
            }
            else
                return "";
        }

        public string ReadAt(long n)
        {
            current = n;
            return Read();
        }

        /// <summary>
        /// Read set of records
        /// </summary>
        /// <param name="f">First number, default = 0</param>
        /// <param name="n">Number of records, 0 - all(default)</param>
        /// <returns></returns>
        public string[] ReadRecords(long f = 0, long n = 0)
        {
            if (_ready)
            {
                long first = 0;
                long last = 0;
                
                if (length == 0)
                    return new string[0];

                first = (f > (length - 1)) ? length - 1 : f;

                if (n == 0)
                    last = length - 1;
                else
                    last = first + n;

                if (first > last)
                    first = last;

                fs = System.IO.File.Open(datafile, FileMode.Open);
                fx = System.IO.File.Open(indexfile, FileMode.Open);

                long cnt = 0;
                string[] ra = new string[last - first + 1];     //Return array

                current = first;
                while (current <= last)
                {
                    ra[cnt] = getRecord();
                    cnt++;
                }
                fx.Close();
                fs.Close();

                return ra;

            }
            else
                return new string[0];

        }

        /// <summary>
        /// Delete all records from the log file
        /// </summary>
        public void Purge()
        {
            fx = System.IO.File.Open(indexfile, FileMode.Truncate);
            fx.Close();
            fs = System.IO.File.Open(datafile, FileMode.Truncate);
            fs.Close();

        }

        /// <summary>
        /// Delete log files
        /// </summary>
        public void Delete()
        {
            System.IO.File.Delete(datafile);
            System.IO.File.Delete(indexfile);
            Close();
        }


        /// <summary>
        /// Archive log
        /// </summary>
        /// <returns></returns>
        public void Archive()
        {
            DateTime d = DateTime.Now;
            string s = d.Year.ToString("d4") + "-" + d.Month.ToString("d2") + "-" + d.Day.ToString("d2") + "_" + d.Hour.ToString("d2") + "-" + d.Minute.ToString("d2") + "-" + d.Second.ToString("d2");
            string dfile = DEFS.LOG_DATA_FILE_NAME(log_path + "\\arc", log_name, s) + ".bak";
            string ifile = DEFS.LOG_INDEX_FILE_NAME(log_path + "\\arc", log_name, s) + ".bak";

            System.IO.File.Copy(datafile, dfile, true);
            System.IO.File.Copy(indexfile, ifile, true);

            Purge();
        }

        private string getFullPath(string _path, string _name, string _prefix = "")
        {
            string s = _path + "\\";
            if (_prefix != "")
                s += _prefix + "_";
            s += "VSXLOG_" + log_name;
            return s;
        }

        /// <summary>
        /// Number of records
        /// </summary>
        public long Length
        {
            get {return length;}
        }
    }
}
