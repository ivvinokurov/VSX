using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VStorage
{
    public class VSAllocation
    {
        /// <summary>
        /// Space allocation
        /// Descriptor - in front of allocated space
        /// Fields:
        ///     Core:
        /// 1. +00(4)    -   address signature
        /// 2. +04(2)    -   chunk: 0-single descriptor; 1-first descriptor; -1-last descriptor; otherwise - sequence, max=32767
        /// 3. +06(8)    -   length of allocated space (including descriptor length)
        /// 4. +14(8)    -   ID or 0 (if ID is not assigned)   
        /// 5. +22(8)    -   previous object descriptor address/0
        /// 6. +30(8)    -   next object descriptor address/0
        /// 7. +38(2)    -   pool #
        ///     Extension: only if chunk = 0/1
        ///     Total: 40
        /// 8. +40(8)    -   Size total size
        /// 9. +48(8)    -   last object descriptor address in chain/0
        /// 10.+56(4)    -   user-defined state
        /// 11.+60(8)    -   user-defined field 1
        /// 12.+68(8)    -   user-defined field 2
        /// 13.+76(8)    -   user-defined field 3
        /// 14.+84(8)    -   user-defined field 4
        /// 15.+92(2)    -   allocation type: 0 - raw; 1-65535 - value to sync (increasing each time to sysnc)
        /// 16.+92(2)    -   fixed object space: 0 - 65535
        /// 
        /// Total: 96
        /// </summary>

        //internal const int BaseDescriptorLength = END_OF_DESCRIPTOR;        //Descriptor length (for chunk = 0/1) 

        //internal const int ExpansionDescriptorLength = END_OF_HEADER;             //Header length (for chunks > 1)

        /////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Cache to speed up search of the allocation chains
        /// </summary>
        private struct SegmentCache
        {
            public long VIRTUAL_ADDRESS;              // Relative data address
            public long LENGTH;                       // Length
            public long DESCRIPTOR_ADDRESS;           // Abs descriptor address
        }

        private List<SegmentCache> SCache = new List<SegmentCache>(cache_size);
        private const int cache_size = 32;
        private VSVirtualMemoryManager vm = null;

        /// <summary>
        /// Read existing descriptor
        /// </summary>
        /// <param name="s"></param>
        /// <param name="addr"></param>
        internal VSAllocation(VSVirtualMemoryManager vmem, long addr)
        {
            DescriptorAddress = addr;
            vm = vmem;
            // Check if 2 last addr bytes == signature
            string sg = vm.ReadString(DescriptorAddress + SG_POS, SG_LEN);
            if (sg != GetAddressSignature(addr))
                throw new VSException(DEFS.E0006_INVALID_SIGNATURE_CODE, "- Object, address: " + addr.ToString("X"));
        }

        /// <summary>
        /// Create new descriptor
        /// </summary>
        /// <param name="s"></param>
        /// <param name="addr">Descriptor address</param>
        /// <param name="len"></param>
        /// <param name="pool"></param>
        internal VSAllocation(VSVirtualMemoryManager vmem, long addr, long len, short pool)
        {
            vm = vmem;

            DescriptorAddress = addr;

            SG = GetAddressSignature(addr);

            this.FullLength = len;

            Pool = pool;
        }

        /// <summary>
        /// Signature
        /// </summary>
        private const int SG_POS = 0;
        private const int SG_LEN = 4;
        private string SG
        {
            get { return vm.ReadString(DescriptorAddress + SG_POS, SG_LEN); }
            set { vm.Write(DescriptorAddress + SG_POS, value); }
        }

        /// <summary>
        /// Chunk #: 0 - single chunk; negative - last chunk; otherwise chunk# 1..n  
        /// </summary>
        private const int CHUNK_POS = SG_POS + SG_LEN;
        private const int CHUNK_LEN = 2;
        internal short Chunk                                   
        {
            get { return vm.ReadShort(DescriptorAddress + CHUNK_POS); }
            set { vm.Write(DescriptorAddress + CHUNK_POS, value); }
        }

        /// <summary>
        /// Chunk size, 0 
        /// </summary>
        private const int CHUNK_SIZE_POS = CHUNK_POS + CHUNK_LEN;
        private const int CHUNK_SIZE_LEN = 4;
        internal int ChunkSize
        {
            get { return vm.ReadShort(DescriptorAddress + CHUNK_SIZE_POS); }
            set { vm.Write(DescriptorAddress + CHUNK_SIZE_POS, value); }
        }

        /// <summary>
        /// Full LENGTH
        /// </summary>
        private const int FULL_LENGTH_POS = CHUNK_SIZE_POS + CHUNK_SIZE_LEN;
        private const int FULL_LENGTH_LEN = 8;
        public long FullLength
        {
            get { return vm.ReadLong(DescriptorAddress + FULL_LENGTH_POS); }
            set { vm.Write(DescriptorAddress + FULL_LENGTH_POS, value); }
        }

        /// <summary>
        /// ID of allocated space or 0
        /// </summary>
        private const int ID_POS = FULL_LENGTH_POS + FULL_LENGTH_LEN;
        private const int ID_LEN = 8;
        public long Id                                      
        {
            get { return vm.ReadLong(DescriptorAddress + ID_POS); }
            set { vm.Write(DescriptorAddress + ID_POS, value); }
        }

        /// <summary>
        /// Address of the prev object descriptor or 0 (if 1st)
        /// </summary>
        private const int PREV_POS = ID_POS + ID_LEN;
        private const int PREV_LEN = 8;
        internal long PREV                                    
        {
            get { return vm.ReadLong(DescriptorAddress + PREV_POS); }
            set 
            {
                vm.Write(DescriptorAddress + PREV_POS, value); 
            }
        }

        /// <summary>
        /// Address of the next object descriptor (0-last)
        /// </summary>
        private const int NEXT_POS = PREV_POS + PREV_LEN;
        private const int NEXT_LEN = 8;
        internal long NEXT                                    
        {
            get { return vm.ReadLong(DescriptorAddress + NEXT_POS); }
            set { vm.Write(DescriptorAddress + NEXT_POS, value); }
        }


        /// <summary>
        /// Pool #
        /// </summary>
        internal const int POOL_POS = NEXT_POS + NEXT_LEN;
        private const int POOL_LEN = 2;
        public short Pool
        {
            get { return vm.ReadShort(DescriptorAddress + POOL_POS); }
            set { vm.Write(DescriptorAddress + POOL_POS, value); }
        }

        private const int END_OF_HEADER = POOL_POS + POOL_LEN;

        ///////////////////// Extension ////////////////////////////

        /// <summary>
        /// Full size of all segments available for user
        /// </summary>
        private const int SIZE_POS = POOL_POS + POOL_LEN;
        private const int SIZE_LEN = 8;
        public long Size
        {
            get { return vm.ReadLong(DescriptorAddress + SIZE_POS); }
        }

        /// <summary>
        /// Address of the last object descriptor in chain (if CHUNK = 1)
        /// </summary>
        private const int LAST_POS = SIZE_POS + SIZE_LEN;
        private const int LAST_LEN = 8;
        internal long LAST
        {
            get { return vm.ReadLong(DescriptorAddress + LAST_POS); }
            set { vm.Write(DescriptorAddress + LAST_POS, value); }
        }

        /// <summary>
        /// Allocation: 0 - RAW; 1..n - Version
        /// </summary>
        private const int ALLOC_POS = LAST_POS + LAST_LEN;
        private const int ALLOC_LEN = 2;
        internal ushort ALLOC
        {
            get { return vm.ReadUShort(DescriptorAddress + ALLOC_POS); }
            set { vm.Write(DescriptorAddress + ALLOC_POS, value); }
        }

        /// <summary>
        /// Fixed allocation for VSObject
        /// </summary>
        private const int FIXED_POS = ALLOC_POS + ALLOC_LEN;
        private const int FIXED_LEN = 2;
        internal ushort FIXED
        {
            get { return vm.ReadUShort(DescriptorAddress + FIXED_POS); }
            set { vm.Write(DescriptorAddress + FIXED_POS, value); }
        }


        private const int END_OF_DESCRIPTOR = FIXED_POS + FIXED_LEN;

        ////////////////////////////////////////
        /////////// NON-persistent /////////////
        ////////////////////////////////////////

        /// <summary>
        /// Next object chunk
        /// </summary>
        internal VSAllocation NextChunk
        {
            get
            {
                if ((NEXT == 0) | (Chunk <= 0))
                    return null;
                return new VSAllocation(vm, NEXT);
            }
        }

        /// <summary>
        /// Previous object chunk
        /// </summary>
        internal VSAllocation PrevChunk
        {
            get
            {
                if ((PREV == 0) | (Chunk == 0) | (Chunk == 1))
                    return null;
                return new VSAllocation(vm, PREV);
            }
        }

        /// <summary>
        /// Last object chunk
        /// </summary>
        internal VSAllocation LastChunk
        {
            get
            {
                if ((LAST == 0) | (Chunk != 1))
                    return null;
                return new VSAllocation(vm, LAST);
            }
        }

        /// <summary>
        /// Next object in the pool
        /// </summary>
        public VSAllocation Next
        {
            get
            {
                if (NEXT == 0)
                    return null;
                if (Chunk == 0)
                    return new VSAllocation(vm, NEXT);
                else if (Chunk == 1)
                {
                    VSAllocation o = new VSAllocation(vm, LAST);
                    if (o.NEXT == 0)
                        return null;
                    else
                        return new VSAllocation(vm, o.NEXT);
                }
                else 
                    return null;
            }
        }

        /// <summary>
        /// Previous object in the pool
        /// </summary>
        public VSAllocation Previous
        {
            get
            {
                if (PREV == 0)
                    return null;

                VSAllocation o = new VSAllocation(vm, PREV);

                if (o.Chunk == 0)
                    return o;

                while (o.Chunk != 1)
                    o = new VSAllocation(vm, o.PREV);
 
                return o;
            }
        }

        /// <summary>
        /// This descriptor address
        /// </summary>
        internal long DescriptorAddress = 0;                 

        /// <summary>
        /// Allocated length (W/O DESCRIPTOR)
        /// </summary>
        internal long Length                             
        {
            get { return (FullLength - ((Math.Abs(this.Chunk) > 1)? DEFS.ExpansionDescriptorLength : DEFS.BaseDescriptorLength)); }
        }

        /// <summary>
        /// Allocated address (object descriptor address + object descriptor length)
        /// </summary>
        internal long Address                                 
        {
            get { return (DescriptorAddress + ((Math.Abs(this.Chunk) > 1) ? DEFS.ExpansionDescriptorLength : DEFS.BaseDescriptorLength)); }
        }



        ////////////////////////////////////////////////////////////////////////////////
        //////////////////// I/O METHODS ///////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Read/Write bytes
        /// </summary>
        /// <param name="op">DEFS.OP_READ or DEFS.OP_WRITE</param>
        /// <param name="address"></param>
        /// <param name="data"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        protected void io_protected(int op, long address, ref byte[] data, long length)
        {
            if (length <= 0)
                throw new VSException(DEFS.E0028_INVALID_LENGTH_ERROR_CODE, "- " + length.ToString());

            long r_length = length;             // Remaining length
            long s_address = 0;                 // Shifted address

            VSAllocation obj = this;
            bool eo_chunk = false;

            if (address < this.Length)
                s_address = 0;             // Address is in the current chunk
            else
            { // Search cache
                obj = null;
                for (int i = (SCache.Count - 1); i >= 0; i--)
                {
                    if ((address >= SCache[i].VIRTUAL_ADDRESS) & (address < (SCache[i].VIRTUAL_ADDRESS + SCache[i].LENGTH)))
                    {
                        s_address = SCache[i].VIRTUAL_ADDRESS;
                        obj = new VSAllocation(vm, SCache[i].DESCRIPTOR_ADDRESS);
                        break;
                    }
                }
                if (obj == null)
                { // NOT found in cache
                    if ((this.Chunk == 0) | (this.NEXT == 0))
                        eo_chunk = true;
                    else
                    {
                        obj = this.NextChunk;
                        s_address = this.Length;
                        while (!eo_chunk)
                        {
                            if (address < (s_address + obj.Length))
                            { // Address is in chunk, add to cache 
                                if (SCache.Count == cache_size)
                                    SCache.RemoveAt(0);
                                SegmentCache ac = new SegmentCache();
                                ac.VIRTUAL_ADDRESS = s_address;
                                ac.LENGTH = obj.Length;
                                ac.DESCRIPTOR_ADDRESS = obj.DescriptorAddress;
                                SCache.Add(ac);
                                break;
                            }
                            else
                            { //start address not in this chunk
                                if ((obj.Chunk > 0) & (obj.NEXT > 0))
                                {
                                    s_address += obj.Length;
                                    obj = obj.NextChunk;
                                }
                                else
                                    eo_chunk = true;
                            }
                        }
                    }
                }
            }

            if (obj != null)
            {
                long l_address = address - s_address;           // Local address in chunk
                long r_address = 0;                             // Relative address in the input array
                while ((obj != null) & (r_length > 0) & !eo_chunk)
                {
                    long l_length = (r_length > (obj.Length - l_address)) ? (obj.Length - l_address) : r_length;
                    vm.Io(obj.Address + l_address, ref data, l_length, op, r_address);
                    l_address = 0;
                    r_length -= l_length;
                    r_address += l_length;
                    obj = obj.NextChunk;
                }
            }
            if ((r_length > 0) | (length <= 0) | (address < 0))
                throw new VSException(DEFS.E0019_INVALID_OP_ADDRESS_ERROR_CODE, "- address " + address.ToString() + "; lenght " + length.ToString());
        }

        /////////////////////////////////////////////////////////////////
        //////////////////// READ METHODS ///////////////////////////////
        /////////////////////////////////////////////////////////////////
        /// <summary>
        /// Read bytes
        /// </summary>
        /// <param name="address">Relative address</param>
        /// <param name="length"></param>
        /// <returns></returns>
        public byte[] ReadBytes(long address, long length)
        {
            byte[] b = new byte[length];
            io_protected(DEFS.OP_READ, address, ref b, length);
            return b;
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
        /// Read u-short
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public ushort ReadUShort(long address)
        {
            return VSLib.ConvertByteToUShort(ReadBytes(address, 2));
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

        /////////////////////////////////////////////////////////////////
        //////////////////// SET METHODS  ///////////////////////////////
        /////////////////////////////////////////////////////////////////
        /// <summary>
        /// Write bytes
        /// </summary>
        /// <param name="address"></param>
        /// <param name="data"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public void Write(long address, byte[] data, long length)
        {
            io_protected(DEFS.OP_WRITE, address, ref data, length);
        }

        /// <summary>
        /// Write bytes
        /// </summary>
        /// <param name="address"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public void Write(long address, byte data)
        {
            byte[] b = new byte[1];
            b[0] = data;
            Write(address, b, 1);
        }

        /// <summary>
        /// Write string
        /// </summary>
        /// <param name="address"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public void Write(long address, string data)
        {
            Write(address, VSLib.ConvertStringToByte(data), data.Length);
        }

        /// <summary>
        /// Write int
        /// </summary>
        /// <param name="address"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public void Write(long address, int data)
        {
            Write(address, VSLib.ConvertIntToByte(data), 4);
        }

        /// <summary>
        /// Write long
        /// </summary>
        /// <param name="address"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public void Write(long address, long data)
        {
            Write(address, VSLib.ConvertLongToByte(data), 8);
        }

        /// <summary>
        /// Write short
        /// </summary>
        /// <param name="address"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public void Write(long address, short data)
        {
            Write(address, VSLib.ConvertShortToByte(data), 2);
        }

        /// <summary>
        /// Write u-short
        /// </summary>
        /// <param name="address"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public void Write(long address, ushort data)
        {
            Write(address, VSLib.ConvertUShortToByte(data), 2);
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

        /// <summary>
        /// Convert long to byte array
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static string GetAddressSignature(long value)
        {
            //string s = VSLib.ConvertLongToHexString(value);

            byte[] b = VSLib.ConvertLongToByte(value);
            int n = 0;
            for (int i = 0; i < b.Length; i++)
            {
                n += b[i] + (i * 8);
                if (n > 9999)
                    n -= 9999;
            }

            return n.ToString("D4");
        }

        /// <summary>
        /// Set object full size - all chunks (descriptor length is NOT included))
        /// </summary>
        /// <param name="s"></param>
        internal void SetSize(long s)
        {
            vm.Write(DescriptorAddress + SIZE_POS, s); 

        }
        
        /// <summary>
        /// Get current object version
        /// </summary>
        /// <returns></returns>
        public int GetVersion()
        {
            return (int)this.ALLOC; 
        }
    }
}
