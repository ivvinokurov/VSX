using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VStorage
{
    public interface IVSIO
    {
        /// <summary>
        /// Calculate CRC
        /// </summary>
        /// <returns></returns>
        uint GetCRC32(long position, long length);

        /// <summary>
        /// Read methods
        /// </summary>
        byte[] ReadBytes(long offset, int len);

        short ReadShort(long offset);

        ushort ReadUShort(long offset);

        int ReadInt(long offset);

        uint ReadUInt(long offset);

        long ReadLong(long offset);

        ulong ReadULong(long offset);

        string ReadString(long offset, int len);

        /// <summary>
        /// Write methods
        /// </summary>
        void Write(long offset, ref byte[] data);

        void Write(long offset, short data);

        void Write(long offset, ushort data);

        void Write(long offset, int data);

        void Write(long offset, uint data);

        void Write(long offset, long data);

        void Write(long offset, ulong data);

        void Write(long offset, string data);

        /// <summary>
        /// Get byte array stream
        /// </summary>
        /// <returns></returns>
        byte[] GetBytes();

        /// <summary>
        /// Close stream
        /// </summary>
        void Close();

        /// <summary>
        /// Flush stream
        /// </summary>
        void Flush();

        /// <summary>
        /// Get current position
        /// </summary>
        /// <returns></returns>
        long GetPosition();
        
        /// <summary>
        /// Set current position
        /// </summary>
        void SetPosition(long value);

        /// <summary>
        /// Get steam name
        /// </summary>
        /// <returns></returns>
        string GetName();

        /// <summary>
        /// Get stream length
        /// </summary>
        /// <returns></returns>
        long GetLength();

        /// <summary>
        /// Get encryption status
        /// </summary>
        /// <returns></returns>
        bool GetEncryption();

        /// <summary>
        /// Set encryption statue
        /// </summary>
        /// <param name="encr"></param>
        void SetEncryption(bool value);
    }
}
