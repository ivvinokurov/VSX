using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace VStorage
{
    public class VSIO : IVSIO
    {
        public const string FILE_MODE_CREATE = "c";
        public const string FILE_MODE_OPEN = "o";
        public const string FILE_MODE_APPEND = "a";

        private bool IMO = false;
        private Stream fs = null;
        private byte[] xkey_b = null;

        // Reader/Writer with encryption

        /// <summary>
        /// File Stream
        /// </summary>
        /// <param name="filename">File name for Read/Write</param>
        /// <param name="mode">'c' - create; 'o' - open, 'a' - append</param>
        /// <param name="key">key for encrypt/decrypt ("" - no encryption)</param>
        public VSIO(string filename, string mode, string key)
        {
            string md = mode.Trim().ToLower();

            //fs = stream;
            xkey_b = VSLib.ConvertStringToByte(key);
            _encrypt = (key != "");
            IMO = false;

            if (md == FILE_MODE_CREATE)
                fs = new FileStream(filename, FileMode.Create);
            else if (md == FILE_MODE_APPEND)
                fs = new FileStream(filename, FileMode.Append);
            else if (md == FILE_MODE_OPEN)
                fs = new FileStream(filename, FileMode.OpenOrCreate);
            else
                throw new VSException(DEFS.E0034_IO_ERROR_CODE, "Invalid open mode = '" + md + "'");
        }

        /// <summary>
        /// Memory Stream
        /// </summary>
        /// <param name="data">byte array - existing stream (read-only); null or empty - new stream</param>
        /// <param name="key">key for encrypt/decrypt ("" - no encryption)</param>
        public VSIO(byte[] data, string key)
        {
            //fs = stream;
            xkey_b = VSLib.ConvertStringToByte(key);
            _encrypt = (key != "");
            IMO = true;
            if (data == null)
                fs = new MemoryStream();
            else
                fs = new MemoryStream(data);

        }

        /// <summary>
        /// Calculate CRC32 
        /// </summary>
        /// <param name="position">Start position, -1 - from the current position</param>
        /// <param name="length">Length, -1 to end of stream</param>
        /// <returns></returns>
        public uint GetCRC32(long position, long length)
        {
            const int chunk = 1024;

            // Save position
            long save_pos = fs.Position;

            // Calculate pos
            long pos = (position < 0) ? fs.Position : position;
            if (pos >= fs.Length)
                pos = 0;

            // Calculate len
            long len = (length < 0) ? (fs.Length - pos) : length;
            if ((pos + len) > fs.Length)
                len = fs.Length - pos;

            if (len == 0)
                return 0;

            // Calculate CRC32
            uint c = VSCRC32.BeginCRC();

            byte[] b = new byte[chunk];

            fs.Seek(pos, SeekOrigin.Begin);

            while (len > 0)
            {
                int l = (int)((len <= chunk) ? len : chunk);

                this.ReadBytes(0, l);

                c = VSCRC32.AddCRC(c, b);

                len -= l;
            }

            // Restore position
            fs.Seek(save_pos, SeekOrigin.Begin);

            return VSCRC32.EndCRC(c);
        }

        ////////////////////////////////////////////////////////////////////
        //////////////// READ METHODS //////////////////////////////////////
        ////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Read bytes
        /// </summary>
        /// <returns></returns>
        public byte[] ReadBytes(long offset, int len)
        {
            if (offset >= 0)
                fs.Seek(offset, SeekOrigin.Begin);

            byte[] b = new byte[len];

            long pos = fs.Position;

            fs.Read(b, 0, len);

            if (!_encrypt)
                return b;

            byte[] b_ret = new byte[len];

            VSCrypto.Decrypt(ref b, ref b_ret, xkey_b, pos);

            return b_ret;
        }

        /// <summary>
        /// Read short
        /// </summary>
        /// <returns></returns>
        public short ReadShort(long offset = -1)
        {
            return VSLib.ConvertByteToShort(this.ReadBytes(offset, 2));
        }

        /// <summary>
        /// Read u-short
        /// </summary>
        /// <returns></returns>
        public ushort ReadUShort(long offset = -1)
        {
            return VSLib.ConvertByteToUShort(this.ReadBytes(offset, 2));
        }

        /// <summary>
        /// Read int
        /// </summary>
        /// <returns></returns>
        public int ReadInt(long offset = -1)
        {
            return VSLib.ConvertByteToInt(this.ReadBytes(offset, 4));
        }

        /// <summary>
        /// Read uint
        /// </summary>
        /// <returns></returns>
        public uint ReadUInt(long offset = -1)
        {
            return VSLib.ConvertByteToUInt(this.ReadBytes(offset, 4));
        }

        /// <summary>
        /// Read long
        /// </summary>
        /// <returns></returns>
        public long ReadLong(long offset = -1)
        {
            return VSLib.ConvertByteToLong(this.ReadBytes(offset, 8));
        }

        /// <summary>
        /// Read ulong
        /// </summary>
        /// <returns></returns>
        public ulong ReadULong(long offset = -1)
        {
            return VSLib.ConvertByteToULong(this.ReadBytes(offset, 8));
        }

        /// <summary>
        /// Read string
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="len"></param>
        /// <returns></returns>
        public string ReadString(long offset, int len)
        {
            return VSLib.ConvertByteToString(this.ReadBytes(offset, len));
        }

        ////////////////////////////////////////////////////////////////////
        //////////////// WRITE METHODS /////////////////////////////////////
        ////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Write bytes
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="data"></param>
        public void Write(long offset, ref byte[] data)
        {
            if (offset >= 0)
                fs.Seek(offset, SeekOrigin.Begin);

            if (_encrypt)
            {
                byte[] e_data = new byte[data.Length];

                VSCrypto.Encrypt(ref data, ref e_data, xkey_b, fs.Position);

                fs.Write(e_data, 0, data.Length);
            }
            else
                fs.Write(data, 0, data.Length);
        }

        /// <summary>
        /// Write short
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="data"></param>
        public void Write(long offset, short data)
        {
            byte[] b = VSLib.ConvertShortToByte(data);
            this.Write(offset, ref b);
        }

        /// <summary>
        /// Write u-short
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="data"></param>
        public void Write(long offset, ushort data)
        {
            byte[] b = VSLib.ConvertUShortToByte(data);
            this.Write(offset, ref b);
        }

        /// <summary>
        /// Write int
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="data"></param>
        public void Write(long offset, int data)
        {
            byte[] b = VSLib.ConvertIntToByte(data);
            this.Write(offset, ref b);
        }

        /// <summary>
        /// Write u-int
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="data"></param>
        public void Write(long offset, uint data)
        {
            byte[] b = VSLib.ConvertUIntToByte(data);
            this.Write(offset, ref b);
        }

        /// <summary>
        /// Write long
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="data"></param>
        public void Write(long offset, long data)
        {
            byte[] b = VSLib.ConvertLongToByte(data);
            this.Write(offset, ref b);
        }

        /// <summary>
        /// Write ulong
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="data"></param>
        public void Write(long offset, ulong data)
        {
            byte[] b = VSLib.ConvertULongToByte(data);
            this.Write(offset, ref b);
        }

        /// <summary>
        /// Write string
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="data"></param>
        public void Write(long offset, string data)
        {
            byte[] b = VSLib.ConvertStringToByte(data);
            this.Write(offset, ref b);
        }

        /// <summary>
        /// Get stream bytes
        /// </summary>
        /// <returns></returns>
        public byte[] GetBytes()
        {
            byte[] b = new byte[fs.Length];

            fs.Seek(0, SeekOrigin.Begin);

            fs.Read(b, 0, (int)fs.Length);

            return b;
        }

        /// <summary>
        /// Flush stream
        /// </summary>
        public void Flush()
        {
            fs.Flush();
        }

        /// <summary>
        /// Close stream
        /// </summary>
        public void Close()
        {
            fs.Close();
        }

        ////////////////////////// PROPERTIES //////////////////////////////

        /// <summary>
        /// Excrypt or not content
        /// </summary>
        public bool GetEncryption()
        {
            return _encrypt; 
        }

        public void SetEncryption(bool value)
        {
            if (xkey_b != null)
                _encrypt = value;

        }
        private bool _encrypt = false;

        /// <summary>
        /// File name
        /// </summary>
        public string GetName()
        {
            return IMO? "" : ((FileStream)fs).Name;
        }

        /// <summary>
        /// Position
        /// </summary>
        public long GetPosition()
        {
            return fs.Position;
        }

        public void SetPosition(long value)
        { 
            fs.Position = value;
        }

        /// <summary>
        /// Length
        /// </summary>
        public long GetLength()
        {
            return fs.Length;
        }
    }
}
