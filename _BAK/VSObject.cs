using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VStorage
{
    public class VSObject:VSAllocation
    {
        //private byte[] DEBUG = null;
        // Structure
        // 1. Size - allocated size     +0(4)     
        // 1. N - number of fields      +4(2)
        // 2. Fields (N)
        // Name:
        //      +0(1)       -   type
        //      +1(1)       -   name length (1-255)
        //      +2(n)       -   name
        //
        // Value: + 2 + name_len
        // byte:
        //      +0(1)       - value 
        // short:
        //      +0(2)       - value
        // datetime:
        //      +0(8)       - value  
        // int, long, decimal:
        //      +0(1)       - number of bytes (n)
        //      +1(n)       - value bytes (lowest)
        // string, byte[]
        //      +0(1)       -   number of the length bytes(n)
        //      +1(n)       -   n bytes conaining lenngth(N)
        //      +1+n(N)       - N value bytes
        //
        ///////////////// HEADER ////////////////////

        private List<FieldCache> FCache = null;         // Fields cache
        private long FreeSpace = -1;                    // Unallocated space inside object. -1 - no fields (RAW)
        private bool FCacheRewrite = false;             // True if needs rewrite all fields

        private int[] FCacheIndex = null;


        /// <summary>
        /// Allocated size
        /// </summary>
        private const long FIELDS_SIZE_POS = 0;
        private const long FIELDS_SIZE_LEN = 4;
        private int FIELDS_SIZE
        {
            get { return ReadInt(FIELDS_SIZE_POS); }
            set { base.Write(FIELDS_SIZE_POS, value); }
        }

        /// <summary>
        /// Number of fields
        /// </summary>
        private const long FIELDS_NUMBER_POS = FIELDS_SIZE_POS + FIELDS_SIZE_LEN;
        private const long FIELDS_NUMBER_LEN = 2;
        private short FIELDS_NUMBER
        {
            get { return ReadShort(FIELDS_NUMBER_POS); }
            set { base.Write(FIELDS_NUMBER_POS, value); }
        }

        private const long FIRST_FIELD_OFFSET = FIELDS_NUMBER_POS + FIELDS_NUMBER_LEN;


        // Field (relative)
        /// <summary>
        /// Field type
        /// </summary>
        private const long FIELD_TYPE_POS = 0;
        private const long FIELD_TYPE_LEN = 1;

        /// <summary>
        /// Field name length
        /// </summary>
        private const long FIELD_NAME_LENGTH_POS = FIELD_TYPE_POS + FIELD_TYPE_LEN;
        private const long FIELD_NAME_LENGTH_LEN = 1;

        /// <summary>
        /// Field name offset
        /// </summary>
        private const long FIELD_NAME_POS = FIELD_NAME_LENGTH_POS + FIELD_NAME_LENGTH_LEN;

        /// <summary>
        /// Length of the fixed part 
        /// </summary>
        private const long FIELD_FIXED_LENGTH = FIELD_NAME_POS;

        /// <summary>
        /// Current allocation version
        /// </summary>
        private int CURRENT_ALLOC = 0;

        /// <summary>
        /// Cache for tagged values location
        /// </summary>
        private struct FieldCache
        {
            public string NAME;                       // Field name
            public int STATE;                         // -1 - new; 0 - not changed; 1- changed  
            public long OFFSET;                       // Relative data address
            public byte TYPE;                         // Data type
            public long LENGTH;                       // Data length
            public long OLDLENGTH;                    // Data length before update
            public long FULL_LENGTH;                  // Full length (data + all descriptor length: type [+length] + data)
            public long DATA_OFFSET;                  // Data value relative address (within field)
            public byte[] VALUE;                      // Value
            public byte[] UVALUE;                     // Uncompressed value
        }

        private const byte FIELD_TYPE_BYTE = 1;
        private const byte FIELD_TYPE_SHORT = 2;
        private const byte FIELD_TYPE_INT = 3;
        private const byte FIELD_TYPE_LONG = 4;
        private const byte FIELD_TYPE_DECIMAL = 5;
        private const byte FIELD_TYPE_DATETIME = 6;
        private const byte FIELD_TYPE_STRING = 7;
        private const byte FIELD_TYPE_BYTES = 8;

        private static long[] FIELD_LENGTHS = { 0, 1, 2, 4, 8, 16, 8, 0, 0 };
        private static bool[] FIELD_COMPRESS = { false, false, false, true, true, true, false, false, false };
        private static string[] FIELD_TYPES = { "undefined", "byte", "short", "int", "long", "decimal", "datetime", "string", "bytes", "undefined" };

        private const int ALLOC_TYPE_RAW = 0;
        private const int ALLOC_TYPE_MIN = 1;
        private const int ALLOC_TYPE_MAX = 2147483647;

        // Field state
        private const int STATE_NEW = -1;
        private const int STATE_LOADED = 0;
        private const int STATE_UPDATED = 1;


        ///////////////////////////////////////////
        //////////// DEFAULT VALUES ///////////////
        ///////////////////////////////////////////

        private const byte DEFAULT_BYTE = 0;
        private const short DEFAULT_SHORT = 0;
        private const int DEFAULT_INT = 0;
        private const long DEFAULT_LONG = 0;
        private const decimal DEFAULT_DECIMAL = 0;
        private DateTime DEFAULT_DATETIME = DateTime.MinValue;
        private const string DEFAULT_STRING = "";
        private byte[] DEFAULT_BYTES = new byte[0];


        /// <summary>
        /// Constructor - get existing object
        /// </summary>
        /// <param name="s"></param>
        /// <param name="addr"></param>
        internal VSObject(VSpace s, long addr)
            : base(s, addr)
        {
            //SYNC_CACHE();
        }

        /// <summary>
        /// Constructor - create new object
        /// </summary>
        /// <param name="s"></param>
        /// <param name="addr"></param>
        internal VSObject(VSpace s, long addr, long len, short pool)
            : base(s, addr, len, pool)
        {
            ALLOC = ALLOC_TYPE_RAW;
        }


        ///////////////////////////////////////////////////////////////////////
        ////////////////  PUBLIC METHODS  /////////////////////////////////////
        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Next object in the pool
        /// </summary>
        public new VSObject Next
        {
            get
            {
                if (NEXT == 0)
                    return null;
                if (Chunk == 0)
                    return sp.GetObjectByDescriptor(NEXT);
                else if (Chunk == 1)
                {
                    VSAllocation o = sp.GetAllocationByDescriptor(LAST);
                    return sp.GetObjectByDescriptor(o.NEXT);
                }
                else
                    return null;
            }
        }

        /// <summary>
        /// Previous object in the pool
        /// </summary>
        public new VSObject Previous
        {
            get
            {
                if (PREV == 0)
                    return null;

                VSObject o = sp.GetObjectByDescriptor(PREV);

                if (o.Chunk == 0)
                    return o;

                while (o.Chunk != 1)
                    o = sp.GetObjectByDescriptor(o.PREV);

                return o;
            }
        }


        //////////////////// GET METHODS  ///////////////////////////////
        /// <summary>
        /// Get byte value
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public byte GetByte(string name)
        {
            byte[] b = GET_FIELD(name, FIELD_TYPE_BYTE);
            return (b == null) ? DEFAULT_BYTE : b[0];
        }

        /// <summary>
        /// Get bytes
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public byte[] GetBytes(string name)
        {
            byte[] b = GET_FIELD(name, FIELD_TYPE_BYTES);
            return (b == null) ? DEFAULT_BYTES : b;
        }

        /// <summary>
        /// Get short value
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public short GetShort(string name)
        {
            byte[] b = GET_FIELD(name, FIELD_TYPE_SHORT);
            return (b == null) ? DEFAULT_SHORT : VSLib.ConvertByteToShort(b);
        }

        /// <summary>
        /// Get int value
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public int GetInt(string name)
        {
            byte[] b = GET_FIELD(name, FIELD_TYPE_INT);
            return (b == null) ? DEFAULT_INT : VSLib.ConvertByteToInt(b);
        }

        /// <summary>
        /// Get long value
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public long GetLong(string name)
        {
            byte[] b = GET_FIELD(name, FIELD_TYPE_LONG);
            return (b == null) ? DEFAULT_LONG : VSLib.ConvertByteToLong(b);
        }

        /// <summary>
        /// Get decimal value
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public decimal GetDecimal(string name)
        {
            byte[] b = GET_FIELD(name, FIELD_TYPE_DECIMAL);
            return (b == null) ? DEFAULT_DECIMAL : VSLib.ConvertByteToDecimal(b);
        }

        /// <summary>
        /// Get string value
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public string GetString(string name)
        {
            byte[] b = GET_FIELD(name, FIELD_TYPE_STRING);
            return (b == null) ? DEFAULT_STRING : VSLib.ConvertByteToString(b);
        }

        /// <summary>
        /// Get DateTime value
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public DateTime GetDateTime(string name)
        {
            byte[] b = GET_FIELD(name, FIELD_TYPE_DATETIME);
            return (b == null) ? DEFAULT_DATETIME : new DateTime(VSLib.ConvertByteToLong(b));
        }

        //////////////////// SET METHODS  ///////////////////////////////
        /// <summary>
        /// Set byte value
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public void Set(string name, byte value)
        {
            if (value == DEFAULT_BYTE)
                Delete(name, FIELD_TYPE_BYTE);
            else
            {
                byte[] b = new byte[1];
                b[0] = value;
                SET_FIELD(name, FIELD_TYPE_BYTE, b);
            }
        }

        /// <summary>
        /// Set bytes
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public void Set(string name, byte[] value)
        {
            if (value == DEFAULT_BYTES)
                Delete(name, FIELD_TYPE_BYTES);
            else
                SET_FIELD(name, FIELD_TYPE_BYTES, value);
        }

        /// <summary>
        /// Set short value
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public void Set(string name, short value)
        {
            if (value == DEFAULT_SHORT)
                Delete(name, FIELD_TYPE_SHORT);
            else
                SET_FIELD(name, FIELD_TYPE_SHORT, VSLib.ConvertShortToByte(value));
        }

        /// <summary>
        /// Set int value
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public void Set(string name, int value)
        {
            if (value == DEFAULT_INT)
                Delete(name, FIELD_TYPE_INT);
            else
                SET_FIELD(name, FIELD_TYPE_INT, VSLib.ConvertIntToByte(value));
        }

        /// <summary>
        /// Set long value
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public void Set(string name, long value)
        {
            if (value == DEFAULT_LONG)
                Delete(name, FIELD_TYPE_LONG);
            else
                SET_FIELD(name, FIELD_TYPE_LONG, VSLib.ConvertLongToByte(value));
        }

        /// <summary>
        /// Set decimal value
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public void Set(string name, decimal value)
        {
            if (value == DEFAULT_DECIMAL)
                Delete(name, FIELD_TYPE_DECIMAL);
            else
                SET_FIELD(name, FIELD_TYPE_DECIMAL, VSLib.ConvertDecimalToByte(value));
        }

        /// <summary>
        /// Set string value
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public void Set(string name, string value)
        {
            if (value == DEFAULT_STRING)
                Delete(name, FIELD_TYPE_STRING);
            else
                SET_FIELD(name, FIELD_TYPE_STRING, VSLib.ConvertStringToByte(value));
        }

        /// <summary>
        /// Set DateTime value
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public void Set(string name, DateTime value)
        {
            if (value == DEFAULT_DATETIME)
                Delete(name, FIELD_TYPE_DATETIME);
            else
                SET_FIELD(name, FIELD_TYPE_DATETIME, VSLib.ConvertLongToByte(value.Ticks));
        }

        //////////////////// OTHER METHODS  ///////////////////////////////
        /// <summary>
        /// Delete field
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool Delete(string name, byte type)
        {
            int idx = FIND_FIELD(name, type);
            if (idx < 0)
                return false;

            FCache.RemoveAt(idx);

            if (idx != FCache.Count)
                FCacheRewrite = true;               // Rewrite cache if not last element

            for (int i = 0; i < FCacheIndex.Length; i++)
                if (FCacheIndex[i] >= idx)
                    FCacheIndex[i] = -1;

            SAVE_FIELDS();

            return true;
        }

        /// <summary>
        /// Get field type
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public string GetType(string name)
        {
            for (int i=0; i<FCache.Count; i++)
            {
                if (FCache[i].NAME == name.Trim().ToLower())
                    return FIELD_TYPES[FCache[i].TYPE];
            }
            return FIELD_TYPES[0];          // Field is not found, return "undefined"
        }

        /// <summary>
        /// Check if field exists
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool Exists(string name)
        {
            return (GetType(name) != FIELD_TYPES[0]);          // return false if "undefined"
        }

        /// <summary>
        /// Array of the field names
        /// </summary>
        public string[] Fields
        {
            get
            {
                if (FCache == null)
                    return new string[0];

                string[] a = new string[FCache.Count];
                for (int i = 0; i < a.Length; i++)
                    a[i] = FCache[i].NAME;
                return a;
            }
        }

        ///////////////////////////////////////////////////////////////////////
        //////////////// PRIVATE METHODS //////////////////////////////////////
        ///////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Generic get
        /// </summary>
        /// <param name="tag"></param>
        /// <returns></returns>
        private byte[] GET_FIELD(string name, byte type)
        {
            SYNC_CACHE();
            int idx = FIND_FIELD(name, type);
            if (idx < 0)
                return null;

            FieldCache f = FCache[idx];
            if (FIELD_COMPRESS[f.TYPE])
            {
                if (f.UVALUE == null)
                {
                    f.UVALUE = DECOMPRESS(FCache[idx].VALUE, type);
                    FCache.RemoveAt(idx);
                    FCache.Insert(idx, f);
                }
                return f.UVALUE;
            }
            else
                return FCache[idx].VALUE;
        }


        /// <summary>
        /// Generic Set
        /// </summary>
        /// <param name="tag"></param>
        /// <returns></returns>
        private void SET_FIELD(string name, byte type, byte[] data)
        {
            SYNC_CACHE();
            FCacheRewrite = false;

            FieldCache fc;
            string nm = name.Trim().ToLower();
            if ((nm.Length < 1) | (nm.Length > 255))
                throw new VSException(VSException.E0027_FIELD_WRITE_ERROR_CODE, "- " + name + " : name length shall be between 1 and 255");

            int index = -1;

            if (ALLOC == ALLOC_TYPE_RAW)
            {
                ALLOC = ALLOC_TYPE_MIN;
                FreeSpace = this.Size - FIELDS_NUMBER_LEN;
                FIELDS_NUMBER = (short)0;
                FCache = new List<FieldCache>(32);
            }
            else
                index = FIND_FIELD(nm, type);

            byte[] compressed_length = null;                           // Compressed length for string/bytes
 
            if (index < 0)
            { // Field is not found in cache, add new field
                fc.STATE = STATE_NEW;                                   // New field

                fc = new FieldCache();
                fc.OFFSET = -1;
                fc.NAME = nm;
                fc.VALUE = COMPRESS(data, type);
                fc.UVALUE = data;

                fc.LENGTH = fc.VALUE.Length;
                fc.OLDLENGTH = fc.VALUE.Length;

                fc.TYPE = type;
                fc.DATA_OFFSET = FIELD_NAME_POS + fc.NAME.Length;

                if ((fc.TYPE == FIELD_TYPE_INT) | (fc.TYPE == FIELD_TYPE_LONG) | (fc.TYPE == FIELD_TYPE_DECIMAL))
                    fc.DATA_OFFSET += 1;
                else if ((fc.TYPE == FIELD_TYPE_BYTES) | (fc.TYPE == FIELD_TYPE_STRING))
                {
                    compressed_length = COMPRESS(VSLib.ConvertLongToByte(fc.LENGTH), FIELD_TYPE_LONG);
                    fc.DATA_OFFSET += (compressed_length.Length + 1);
                }

                fc.FULL_LENGTH = fc.DATA_OFFSET + fc.LENGTH;

                FCache.Add(fc);

                FCacheRewrite = true;
            }
            else
            { // Field found in cache, update
                fc = FCache[index];
                
                fc.VALUE = COMPRESS(data, type);
                
                if (FIELD_COMPRESS[type])
                    fc.UVALUE = data;
                
                fc.LENGTH = fc.VALUE.Length;
                fc.FULL_LENGTH = fc.DATA_OFFSET + fc.LENGTH;       // Update full length
                fc.STATE = STATE_UPDATED;

                FCache.RemoveAt(index);
                FCache.Insert(index, fc);

                FCacheRewrite = (fc.LENGTH != fc.OLDLENGTH) | (index != (FCache.Count - 1));
            }
            
            SAVE_FIELDS();
        }


        /// <summary>
        /// Load/renew cache
        /// </summary>
        private void SYNC_CACHE()
        {
            if (FCacheIndex == null)
                FCacheIndex = new int[5];
            
            if (ALLOC == ALLOC_TYPE_RAW)
            {
                if (FreeSpace >= 0)
                {
                    FCache.Clear();
                    FCache = null;
                    FreeSpace = -1;
                    FCacheRewrite = false;
                    CURRENT_ALLOC = 0;
                }
            }
            else
            {
                if (CURRENT_ALLOC != ALLOC)
                {
                    for (int i = 0; i < FCacheIndex.Length; i++)
                        FCacheIndex[i] = -1;
                    
                    ALLOC++;
                    CURRENT_ALLOC = ALLOC;

                    FCacheRewrite = false;

                    if (FreeSpace < 0)
                        FCache = new List<FieldCache>(32);
                    else
                        FCache.Clear();

                    byte[] data = base.ReadBytes(0, (long)FIELDS_SIZE);                     // Read data into the buffer

                    long offset = FIRST_FIELD_OFFSET;                                       // Offset inside buffer
                    
                    int n = (int)FIELDS_NUMBER;                                             // Number of fields

                    for (int i = 0; i < n; i++)
                    {
                        FieldCache f = new FieldCache();
                        f.OFFSET = offset;                                                  // Field offset in object
                        f.TYPE = data[offset + FIELD_TYPE_POS];                             // Field type

                        byte name_length = data[offset + FIELD_NAME_LENGTH_POS];            // Name length
                        f.NAME = VSLib.ConvertByteToString(VSLib.GetByteArray(data, (int)(offset + FIELD_NAME_POS), (int)name_length));    // Name

                        long v_offset = FIELD_FIXED_LENGTH + name_length;                   // Value offset

                        if ((f.TYPE == FIELD_TYPE_BYTE) | (f.TYPE == FIELD_TYPE_SHORT) | (f.TYPE == FIELD_TYPE_DATETIME))
                        {
                            f.DATA_OFFSET = v_offset;                                       // Shift to value offset
                            f.LENGTH = FIELD_LENGTHS[f.TYPE];
                        }
                        else if ((f.TYPE == FIELD_TYPE_INT) | (f.TYPE == FIELD_TYPE_LONG) | (f.TYPE == FIELD_TYPE_DECIMAL))
                        {
                            f.DATA_OFFSET = v_offset + 1;                                   // Shift to value offset
                            f.LENGTH = (long)data[offset + v_offset];
                        }
                        else if ((f.TYPE == FIELD_TYPE_BYTES) | (f.TYPE == FIELD_TYPE_STRING))
                        {
                            long l = (long)data[offset + v_offset];                                          // Read number of length bytes
                            byte[] ba = VSLib.GetByteArray(data, (int)(offset + v_offset + 1), (int)l);      // Read length
                            f.LENGTH = VSLib.ConvertByteToLong(DECOMPRESS(ba, FIELD_TYPE_LONG));
                            f.DATA_OFFSET = v_offset + 1 + l;
                        }
                        else
                            throw new VSException(VSException.E0029_INVALID_FIELD_TYPE_CODE, "- " + f.TYPE.ToString());

                        f.OLDLENGTH = f.LENGTH;
                        f.STATE = STATE_LOADED;
                        f.FULL_LENGTH = f.DATA_OFFSET + f.LENGTH;
                        f.VALUE = VSLib.GetByteArray(data, (int)(offset + f.DATA_OFFSET), (int)f.LENGTH);

                        offset += f.FULL_LENGTH;
                        FCache.Add(f);
                    }
                    FreeSpace = this.Size - offset;
                }
            }
        }

        /// <summary>
        /// Save object fields after Set/Delete
        /// </summary>
        private void SAVE_FIELDS()
        {
            byte[] b = null;
            long old_size = this.Size - FreeSpace;                              // Current used size (number of fields + all fields size)

            // 1. Calculate new size
            long new_size = FIRST_FIELD_OFFSET;

            for (int i = 0; i < FCache.Count; i++)
                new_size += FCache[i].FULL_LENGTH;

            // 2. Check if space is availabe, extend if required
            if (new_size > this.Size)
                sp.Extend(this, (new_size - old_size));

            // 3. Check if only value update is required

            FIELDS_NUMBER = (short)FCache.Count;                                // Update fields count
            if (!FCacheRewrite)
            {
                for (int i = 0; i < FCache.Count; i++)
                    if (FCache[i].STATE != STATE_LOADED)
                    {
                        FieldCache f = FCache[i];
                        base.Write(f.OFFSET + f.DATA_OFFSET, f.VALUE, f.LENGTH);
                        f.STATE = STATE_LOADED;
                        FCache.RemoveAt(i);
                        FCache.Insert(i, f);
                        break;
                    }
            }
            else
            {
                // Set new fields number
                if (FIELDS_NUMBER == 0)
                {
                    ALLOC = ALLOC_TYPE_RAW;
                    FIELDS_SIZE = 0;
                    FreeSpace = -1;
                    FCache = null;
                    return;
                }
                else
                    FIELDS_SIZE = (int)new_size;

                // Find start index to update
                int update_index = (FCache[FCache.Count - 1].STATE == STATE_LOADED) ? -1 : FCache.Count - 1;      // Update last only or all(-1 if not last)

                FieldCache[] afc = FCache.ToArray();

                long offset = FIRST_FIELD_OFFSET;
                FCache.Clear();
                for (int i = 0; i < afc.Length; i++)
                {
                    // Write name
                    afc[i].OFFSET = offset;
                    if ((update_index < 0) | (i == update_index))
                    {
                        byte[] data = new byte[afc[i].FULL_LENGTH];
                        int data_pos = 0;
                        
                        // Type
                        data[data_pos] = afc[i].TYPE;
                        data_pos++;

                        // Name length
                        data[data_pos] = (byte)(afc[i].NAME.Length);
                        data_pos++;

                        // Name 
                        b = VSLib.ConvertStringToByte(afc[i].NAME);
                        VSLib.CopyBytes(data, b, data_pos);
                        data_pos += b.Length;

                        // Value
                        if (FIELD_COMPRESS[afc[i].TYPE])                                                              // int/long/decimal
                        {
                            data[data_pos] = (byte)(afc[i].LENGTH);
                            data_pos++;
                        }
                        else if ((afc[i].TYPE == FIELD_TYPE_BYTES) | (afc[i].TYPE == FIELD_TYPE_STRING))
                        {
                            b = COMPRESS(VSLib.ConvertLongToByte(afc[i].LENGTH), FIELD_TYPE_LONG);
                            data[data_pos] = (byte)b.Length;
                            data_pos++;

                            VSLib.CopyBytes(data, b, data_pos);
                            data_pos += b.Length;
                        }

                        // Write value
                        VSLib.CopyBytes(data, afc[i].VALUE, data_pos);

                        base.Write(afc[i].OFFSET, data, data.Length);           
                    }

                    // Shift offset and add to cache
                    offset += afc[i].FULL_LENGTH;
                    afc[i].OLDLENGTH = afc[i].LENGTH;
                    afc[i].STATE = STATE_LOADED;
                    FCache.Add(afc[i]);
                }

                FreeSpace = this.Size - offset;
            }
            // Update allocation #
            if (CURRENT_ALLOC == ALLOC_TYPE_MAX)
                CURRENT_ALLOC = ALLOC_TYPE_MIN;
            else
                CURRENT_ALLOC++;

            FCacheRewrite = false;
            ALLOC = CURRENT_ALLOC;

            // Fill the rest by zeros if le length < old length
            if (new_size < old_size)
            {
                b = new byte[old_size - new_size];
                base.Write(new_size, b, b.Length);                              // Fill be zeros unused space
            }
        }

        /// <summary>
        /// Find existing field
        /// </summary>
        /// <param name="name"></param>
        /// <param name="type"></param>
        /// <returns>index in the cache or -1(not found)</returns>
        private int FIND_FIELD(string name, byte type)
        {
            SYNC_CACHE();

            if (ALLOC == ALLOC_TYPE_RAW)
                return -1;                           // Raw object - return -1

            int idx = -1;

            for (int i = 0; i < FCacheIndex.Length; i++)
                if (FCacheIndex[i] >= 0)
                    if (FCache[FCacheIndex[i]].NAME == name)
                    {
                        idx = FCacheIndex[i];
                        break;
                    }

            if (idx < 0)
                for (int i = 0; i < FCache.Count; i++)
                {
                    if (FCache[i].NAME == name.Trim())
                    {
                        idx = i;
                        break;
                    }
                }

            if (idx < 0)
                return -1;

            if (FCache[idx].TYPE != type)
                throw new VSException(VSException.E0026_FIELD_READ_ERROR_CODE, "- " + name + " : type " + FCache[idx].TYPE.ToString() + " doesnt match requested field type " + type.ToString());

            // Add to index cache
            for (int i = 0; i < FCacheIndex.Length; i++)
                if (FCacheIndex[i] < 0)
                {
                    FCacheIndex[i] = idx;
                    return idx;
                }

            for (int i = 1; i < FCacheIndex.Length; i++)
                FCacheIndex[i] = FCacheIndex[i - 1];
            
            FCacheIndex[0] = idx;

            return idx;
        }

        /// <summary>
        /// Compress array of bytes by eliminating higher zero bytes
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static byte[] COMPRESS(byte[] value, byte type)
        {
            if (!FIELD_COMPRESS[type])
                return value;

            int n = value.Length;
            for (int i = (value.Length - 1); i >= 0; i--)
            {
                if (value[i] != 0)
                    break;
                n--;
            }

            byte[] b = new byte[n];
            for (int j = 0; j < n; j++)
                b[j] = value[j];
            return b;
        }
        /// <summary>
        /// decompress array of bytes
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static byte[] DECOMPRESS(byte[] value, byte type)
        {
            if (!FIELD_COMPRESS[type])
                return value;

            byte[] b = new byte[FIELD_LENGTHS[type]];
            for (int i = 0; i < value.Length; i++)
                b[i] = value[i];
            return b;
        }

        /// <summary>
        /// Check if allocation is Raw
        /// </summary>
        private void CHKALLOC()
        {
            if (ALLOC != ALLOC_TYPE_RAW)
                throw new VSException(VSException.E0030_INVALID_WRITE_OP_CODE);
        }

        //////////////////////////////////////////////////////////////////
        /////////////// Overwrite 'Write methods /////////////////////////
        //////////////////////////////////////////////////////////////////
        // bytes[]
        public new void Write(long address, byte[] data, long length)
        {
            CHKALLOC();
            base.Write(address, data, length);
        }
        
        // byte
        public new void Write(long address, byte data)
        {
            CHKALLOC();
            base.Write(address, data);
        }

        // string
        public new void Write(long address, string data)
        {
            CHKALLOC();
            base.Write(address, data);
        }

        // int
        public new void Write(long address, int data)
        {
            CHKALLOC();
            base.Write(address, data);
        }

        // long
        public new void Write(long address, long data)
        {
            CHKALLOC();
            base.Write(address, data);
        }

        // short
        public new void Write(long address, short data)
        {
            CHKALLOC();
            base.Write(address, data);
        }

        // decimal
        public new void Write(long address, decimal data)
        {
            Write(address, VSLib.ConvertDecimalToByte(data), 16);
        }

        // DateTime
        public new void Write(long address, DateTime data)
        {
            CHKALLOC();
            base.Write(address, data);
        }
    }
}
