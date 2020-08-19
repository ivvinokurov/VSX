using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace VStorage
{
    public class VSTransaction
    {
        private bool imo = false;

        //private FileStream fs = null;

        private VSIO IO = null;

        private string _path = "";

        public string Error = "";

        private long CURRENT_POS = -1;          // Current position
        
        private bool EOF = false;

        private bool started = false;

        private bool roll_mode = false;

        /// <summary>
        /// Transaction level 1 (physical) record definitions
        /// </summary>
        public struct TA_RECORD
        {
            public short ID;                         // Space ID

            public long ADDRESS;                    // Base address

            public int LENGTH;                      // Data package length

            public byte[] DATA;                     // Data package
        }
        //////////////////////////////////////////////////////
        // +4(length -8) TA_RECORD
        // +length-4)(4) length
        //////////////////////////////////////////////////////

            /// <summary>
            /// Empty _ta_file - In Memory Option
            /// </summary>
            /// <param name="_ta_file"></param>
        public VSTransaction(string _ta_file)
        {
            _path =_ta_file.Trim();
            imo = (_path == "");
            roll_mode = false;
        }

        /// <summary>
        /// Open for Rollback/rollforward transaction (NOT USED for IMO!)
        /// </summary>
        public void Open()
        {
            CURRENT_POS = -1;
            EOF = false;

            IO = new VSIO(_path, VSIO.FILE_MODE_OPEN, "");

            if (IO.GetLength() == 0)
                EOF = true;
            else
                CURRENT_POS = IO.GetLength();
            roll_mode = true;
        }



        /// <summary>
        /// Write record to transaction log
        /// </summary>
        /// <param name="id"></param>
        /// <param name="address"></param>
        /// <param name="_length"></param>
        /// <param name="_old"></param>
        /// <param name="_new"></param>
        public void WriteRecord(short id, long address, ref byte[] data)
        {
            IO.Write(-1, (short)id);
            IO.Write(-1, (long)address);
            IO.Write(-1, (int)data.Length);
            IO.Write(-1, ref data);
            int ln = (int)(data.Length + 14 + 4);
            IO.Write(-1, (int)ln);
            IO.Flush();
        }

        /// <summary>
        /// Read record from log
        /// </summary>
        /// <returns></returns>
        public TA_RECORD ReadRecord()
        {
            TA_RECORD ta_f = new TA_RECORD();

            if (EOF)
            {
                ta_f.ID = -1;
                return ta_f;
            }

            int ln = IO.ReadInt(CURRENT_POS - 4);                  // Length
            CURRENT_POS -= ln;

            IO.SetPosition(CURRENT_POS);

            ta_f.ID = IO.ReadShort();
            ta_f.ADDRESS = IO.ReadLong();
            ta_f.LENGTH = IO.ReadInt();
            ta_f.DATA = IO.ReadBytes(-1, (int)ta_f.LENGTH);

            if (CURRENT_POS == 0)
                EOF = true;

            return ta_f;
        }

        /// <summary>
        /// Begin transaction 
        /// </summary>
        public void Begin()
        {
            if (!Started)
            {
                if (!imo)
                {
                    CURRENT_POS = 0;
                    CloseTAFile();

                    IO = new VSIO(_path, VSIO.FILE_MODE_CREATE, "");

                    if (IO.GetLength() > 0)
                    {
                        CloseTAFile();
                        throw new VSException(DEFS.E0018_TRANSACTION_ERROR_CODE, "- Begin transaction - previous transaction is not completed or rolled back");
                    }
                }
                started = true;
            }
        }

        /// <summary>
        /// Commit - close and truncate all files
        /// </summary>
        public void Commit()
        {
            if (!imo)
            {
                CloseTAFile();
                if (File.Exists(_path))
                {
                    FileStream fs = new FileStream(_path, FileMode.Truncate);
                    fs.Close();
                }
                CloseTAFile();
            }
            started = false;
            roll_mode = false;
        }

        /// <summary>
        /// Close only close all files (for further Rollback)
        /// </summary>
        public void Close()
        {
            if (!imo)
                CloseTAFile();

            started = false;
            roll_mode = false;
        }

        /// <summary>
        /// Close file and set to null (private)
        /// </summary>
        private void CloseTAFile()
        {
            if (IO != null)
            {
                IO.Close();
                IO = null;
            }
        }
        /////////////////////////////////////////////////
        /////////////// PROPERTIES //////////////////////
        /////////////////////////////////////////////////

        /// <summary>
        /// Transaction state: true - opened; false - no
        /// </summary>
        public bool Started
        {
            get { return started; }
        }

        /// <summary>
        /// Transaction roll mode: true - yes; false - no
        /// </summary>
        public bool RollMode
        {
            get { return roll_mode; }
            set { roll_mode = value; }
        }

        /// <summary>
        /// Check is rollback pending 
        /// Return: false - not pending; true - pending
        /// </summary>
        public bool Pending
        {
            get
            {
                long l = 0;
                if (IO == null)
                {
                    if (!System.IO.File.Exists(_path))
                        return false;
                    IO = new VSIO(_path, VSIO.FILE_MODE_OPEN, "");
                    l = IO.GetLength();
                    CloseTAFile();
                }
                return (l > 0);
            }
        }
    }
}
