using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VStorage
{
    public class VSObject:VSAllocation
    {
        private const int ALLOC_RESET = 0;
        private const int ALLOC_INIT =  1;
        private const int ALLOC_RENEW = 2;

        // Structure
        // 1. Reserve - reserved space  +0(2)               //>>>
        // 2. Size - allocated size     +0(4)     
        // 3. N - number of fields      +4(2)
        // 4. Fields (N)
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
        private int FreeSpace = -1;                    // Unallocated space inside object. -1 - no fields (RAW)

        private VSBBTree FCacheTree = null;
        private const string FCT_NAME = "cache_tree";

        /// <summary>
        /// Variable allocated size
        /// </summary>
        private const int FIELDS_SIZE_POS = 0;
        private const int FIELDS_SIZE_LEN = 4;
        private int FIELDS_SIZE
        {
            get { return base.ReadInt(FIELDS_SIZE_POS + base.FIXED); }
            set { base.Write(FIELDS_SIZE_POS + base.FIXED, value); }
        }

        /// <summary>
        /// Number of fields
        /// </summary>
        private const int FIELDS_NUMBER_POS = FIELDS_SIZE_POS + FIELDS_SIZE_LEN;
        private const int FIELDS_NUMBER_LEN = 2;
        private short FIELDS_NUMBER
        {
            get { return base.ReadShort(FIELDS_NUMBER_POS + base.FIXED); }
            set { base.Write(FIELDS_NUMBER_POS + base.FIXED, value); }
        }

        /// <summary>
        /// 1st field offset (abs)
        /// </summary>
        private int FIRST_FIELD_OFFSET_ABS 
        {
            get { return FIELDS_NUMBER_POS + FIELDS_NUMBER_LEN + base.FIXED; }
        }

        private const int FIRST_FIELD_OFFSET_REL = FIELDS_NUMBER_POS + FIELDS_NUMBER_LEN;


        // Field (relative)
        /// <summary>
        /// Field type
        /// </summary>
        private const int FIELD_TYPE_POS = 0;
        private const int FIELD_TYPE_LEN = 1;

        /// <summary>
        /// Field name length
        /// </summary>
        private const int FIELD_NAME_LENGTH_POS = FIELD_TYPE_POS + FIELD_TYPE_LEN;
        private const int FIELD_NAME_LENGTH_LEN = 1;

        /// <summary>
        /// Field name offset
        /// </summary>
        private const int FIELD_NAME_POS = FIELD_NAME_LENGTH_POS + FIELD_NAME_LENGTH_LEN;

        /// <summary>
        /// Length of the fixed part 
        /// </summary>
        private const int FIELD_FIXED_LENGTH = FIELD_NAME_POS;

        /// <summary>
        /// Current allocation version
        /// </summary>
        private ushort CURRENT_ALLOC = 0;

        /// <summary>
        /// Cache for tagged values location
        /// </summary>
        private struct FieldCache
        {
            public string NAME;                      // Field name
            public int STATE;                        // -1 - new; 0 - not changed; 1- changed  
            public long OFFSET;                      // Relative data address
            public byte TYPE;                        // Data type
            public int LENGTH;                       // Data length
            public int OLDLENGTH;                    // Data length before update
            public int FULL_LENGTH;                  // Full length (data + all descriptor length: type [+length] + data)
            public int DATA_OFFSET;                  // Data value relative address (within field)
            public byte[] VALUE;                     // Value
            public bool DELETED;                     // True - record is deleted
        }

        private const byte FIELD_TYPE_BYTE = 1;
        private const byte FIELD_TYPE_SHORT = 2;
        private const byte FIELD_TYPE_INT = 3;
        private const byte FIELD_TYPE_LONG = 4;
        private const byte FIELD_TYPE_DATETIME = 5;
        private const byte FIELD_TYPE_STRING = 6;
        private const byte FIELD_TYPE_BYTES = 7;

        private static int[] FIELD_LENGTHS = { 0, 1, 2, 4, 8, 8, 0, 0 };
        private static bool[] FIELD_COMPRESS = { false, false, false, true, true, false, false, false };
        private static string[] FIELD_TYPES = { "undefined", "byte", "short", "int", "long", "datetime", "string", "bytes", "undefined" };

        private const ushort ALLOC_TYPE_RAW = 0;
        private const ushort ALLOC_TYPE_MIN = 1;
        private const ushort ALLOC_TYPE_MAX = 65535;

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

        private VSpace sp = null;


        /// <summary>
        /// Constructor - get existing object
        /// </summary>
        /// <param name="s"></param>
        /// <param name="addr"></param>
        internal VSObject(VSpace s, long addr)
            : base(s.VM, addr)
        {
            sp = s;
        }

        /// <summary>
        /// Constructor - create new object
        /// </summary>
        /// <param name="s"></param>
        /// <param name="addr"></param>
         /*
        internal VSObject(VSpace s, long addr, long len, int fixed_len, short pool)
            : base(s.VM, addr, len, pool)
        {
            sp = s;
            
            set_alloc(ALLOC_INIT);

            base.FIXED = (ushort)fixed_len;

            FCacheIndex = new int[INDEX_CACHE_SIZE];
            for (int i = 0; i < FCacheIndex.Length; i++)
                FCacheIndex[i] = -1;
        }

        */
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
                    return (o.NEXT == 0)? null : sp.GetObjectByDescriptor(o.NEXT);
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
            byte[] b = get_field(name, FIELD_TYPE_BYTE);
            return (b == null) ? DEFAULT_BYTE : b[0];
        }

        /// <summary>
        /// Get bytes
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public byte[] GetBytes(string name)
        {
            byte[] b = get_field(name, FIELD_TYPE_BYTES);
            return (b == null) ? DEFAULT_BYTES : b;
        }

        /// <summary>
        /// Get short value
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public short GetShort(string name)
        {
            byte[] b = get_field(name, FIELD_TYPE_SHORT);
            return (b == null) ? DEFAULT_SHORT : VSLib.ConvertByteToShort(b);
        }

        /// <summary>
        /// Get int value
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public int GetInt(string name)
        {
            byte[] b = get_field(name, FIELD_TYPE_INT);
            return (b == null) ? DEFAULT_INT : VSLib.ConvertByteToInt(b);
        }

        /// <summary>
        /// Get long value
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public long GetLong(string name)
        {
            byte[] b = get_field(name, FIELD_TYPE_LONG);
            return (b == null) ? DEFAULT_LONG : VSLib.ConvertByteToLong(b);
        }

        /// <summary>
        /// Get string value
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public string GetString(string name)
        {
            byte[] b = get_field(name, FIELD_TYPE_STRING);
            return (b == null) ? DEFAULT_STRING : VSLib.ConvertByteToString(b);
        }

        /// <summary>
        /// Get DateTime value
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public DateTime GetDateTime(string name)
        {
            byte[] b = get_field(name, FIELD_TYPE_DATETIME);
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
            byte[] b = new byte[1];
            b[0] = value;
            set_field(name, FIELD_TYPE_BYTE, b);
        }

        /// <summary>
        /// Set bytes
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public void Set(string name, byte[] value)
        {
            set_field(name, FIELD_TYPE_BYTES, value);
        }

        /// <summary>
        /// Set short value
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public void Set(string name, short value)
        {
            set_field(name, FIELD_TYPE_SHORT, VSLib.ConvertShortToByte(value));
        }

        /// <summary>
        /// Set int value
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public void Set(string name, int value)
        {
            set_field(name, FIELD_TYPE_INT, VSLib.ConvertIntToByte(value));
        }

        /// <summary>
        /// Set long value
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public void Set(string name, long value)
        {
            set_field(name, FIELD_TYPE_LONG, VSLib.ConvertLongToByte(value));
        }

        /// <summary>
        /// Set multiple string values
        /// </summary>
        /// <param name="fields">
        /// Array of fieldsin format: {name}={value}
        /// </param>
        /// <returns></returns>
        public void Set(string[] fields)
        {
            bool upd = false;
            if (fields != null)
            {
                if (fields.Length > 0)
                {
                    for (int i = 0; i < fields.Length; i++)
                    {
                        int j = fields[i].IndexOf("=");
                        if (j >= 0)
                        {
                            string name = fields[i].Substring(0, j);
                            string value = "";
                            if (j < (fields[i].Length - 1))
                                value = fields[i].Substring(j + 1, fields[i].Length - j - 1);
                            set_field(name, FIELD_TYPE_STRING, VSLib.ConvertStringToByte(value), false);
                            upd = true;
                        }
                    }
                }
            }
            if (upd)
                serialize();
        }

        /// <summary>
        /// Set string value
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public void Set(string name, string value)
        {
            set_field(name, FIELD_TYPE_STRING, VSLib.ConvertStringToByte(value));
        }


        /// <summary>
        /// Set DateTime value
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public void Set(string name, DateTime value)
        {
            set_field(name, FIELD_TYPE_DATETIME, VSLib.ConvertLongToByte(value.Ticks));
        }

        //////////////////// OTHER METHODS  ///////////////////////////////
        /// <summary>
        /// Delete field
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool Delete(string name)
        {
            int idx = find_field(name, 0);
            if (idx < 0)
                return false;

            // Replace cache item with empty
            FieldCache f = new FieldCache();
            f.DELETED = true;
            FCache.RemoveAt(idx);
            FCache.Insert(idx, f);

            //Delete from the tree
            FCacheTree.Delete(name);

            this.serialize();

            return true;
        }

        /// <summary>
        /// Get field type
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public string GetType(string name)
        {
            sync_cache();
            for (int i = 0; i < FCache.Count; i++)
            {
                if (FCache[i].NAME == name.Trim())
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
                sync_cache();
                if (FCache == null)
                    return new string[0];

                string[] a = new string[FCache.Count];
                for (int i = 0; i < a.Length; i++)
                    a[i] = FCache[i].NAME;
                return a;
            }
        }

        /// <summary>
        /// Array of the field names by pattern
        /// </summary>
        public string[] GetFields(string pattern = "*")
        {
            sync_cache();
            if (FCache == null)
                return new string[0];

            List<string> ls = new List<string>();

            for (int i = 0; i < FCache.Count; i++)
                if (VSLib.Compare(pattern, FCache[i].NAME))
                    ls.Add(FCache[i].NAME);
            return ls.ToArray();
        }


        ///////////////////////////////////////////////////////////////////////
        //////////////// PRIVATE METHODS //////////////////////////////////////
        ///////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Change allocation state
        /// </summary>
        /// <param name="op">
        /// 0 - reset to raw
        /// 1 - init
        /// 2 - renew(increase)
        /// </param>
        private void set_alloc(int op)
        {
            if (op == ALLOC_RESET)
            {
                base.ALLOC = ALLOC_TYPE_RAW;
                CURRENT_ALLOC = ALLOC_TYPE_RAW;
            }
            else if (op == ALLOC_INIT)
            {
                base.ALLOC = ALLOC_TYPE_MIN;
                CURRENT_ALLOC = ALLOC_TYPE_MIN;
            }
            else if (op == ALLOC_RENEW)
            {
                if (CURRENT_ALLOC == ALLOC_TYPE_MAX)
                    CURRENT_ALLOC = ALLOC_TYPE_MIN;
                else
                    CURRENT_ALLOC++;

                base.ALLOC = CURRENT_ALLOC;
            }
        }

        /// <summary>
        /// Generic get
        /// </summary>
        /// <param name="tag"></param>
        /// <returns></returns>
        private byte[] get_field(string name, byte type)
        {
            int idx = find_field(name, type);

            if (idx < 0)
                return null;

            FieldCache f = FCache[idx];

            if (FIELD_COMPRESS[f.TYPE])
                return decompress(FCache[idx].VALUE, type);
            else
                return FCache[idx].VALUE;
        }

        /// <summary>
        /// Generic Set
        /// </summary>
        /// <param name="serialize">false = do not write (for multiple values at once)</param>
        /// <returns></returns>
        private void set_field(string name, byte type, byte[] data, bool serialize = true)
        {
            string nm = name.Trim();
            if ((nm.Length < 1) | (nm.Length > 255))
                throw new VSException(DEFS.E0027_FIELD_WRITE_ERROR_CODE, "- " + name + " : name length shall be between 1 and 255");

            FieldCache fc;
            int index;

            byte[] compressed_length = null;                           // Compressed length

            // Initialize fields structure
            if (base.ALLOC == ALLOC_TYPE_RAW)
            {
                index = -1;

                set_alloc(ALLOC_INIT);

                FreeSpace = (int)(this.Size - FIRST_FIELD_OFFSET_ABS);
                FIELDS_NUMBER = (short)0;
                FIELDS_SIZE = FIRST_FIELD_OFFSET_REL;
                
                // Create cache
                FCache = new List<FieldCache>(32);
                // Create cache binary tree
                FCacheTree = new VSBBTree(FCT_NAME, 32, true);

            }
            else
                index = find_field(nm, type);


            // Field is not found in cache, add new field
            if (index < 0)
            {
                fc = new FieldCache();
                fc.STATE = STATE_NEW;                                   // New field
                fc.OFFSET = -1;
                fc.NAME = nm;
                fc.VALUE = compress(data, type);
                fc.LENGTH = fc.VALUE.Length;
                fc.OLDLENGTH = fc.VALUE.Length;
                fc.TYPE = type;
                fc.DATA_OFFSET = FIELD_NAME_POS + fc.NAME.Length;

                if ((fc.TYPE == FIELD_TYPE_INT) | (fc.TYPE == FIELD_TYPE_LONG))
                    fc.DATA_OFFSET += 1;
                else if ((fc.TYPE == FIELD_TYPE_BYTES) | (fc.TYPE == FIELD_TYPE_STRING))
                {
                    compressed_length = compress(VSLib.ConvertLongToByte(fc.LENGTH), FIELD_TYPE_LONG);
                    fc.DATA_OFFSET += (compressed_length.Length + 1);
                }

                fc.FULL_LENGTH = fc.DATA_OFFSET + fc.LENGTH;
                
                // Add to the tree
                FCacheTree.Insert(fc.NAME, (long)FCache.Count);

                // Add to the cache
                FCache.Add(fc);
            }
            else
            { // Field found in cache, update
                fc = FCache[index];

                byte[] b = compress(data, type);

                if (b.Length == fc.LENGTH)
                {
                    if (b.Length == 0)
                        return;
                    
                    bool eq = true;
                    for (int i = 0; i < b.Length; i++)
                        if (b[i] != fc.VALUE[i])
                        {
                            eq = false;
                            break;
                        }

                    if (eq)
                        return;             // Value not changed

                    for (int i = 0; i < b.Length; i++)
                        fc.VALUE[i] = b[i];

                    base.Write(base.FIXED + fc.OFFSET + fc.DATA_OFFSET, b, fc.LENGTH);
                    fc.STATE = STATE_LOADED;

                    FCache.RemoveAt(index);
                    FCache.Insert(index, fc);

                    set_alloc(ALLOC_RENEW);

                    return;
                }

                fc.VALUE = b;
                fc.LENGTH = fc.VALUE.Length;
                fc.DATA_OFFSET = FIELD_NAME_POS + fc.NAME.Length;

                if ((fc.TYPE == FIELD_TYPE_INT) | (fc.TYPE == FIELD_TYPE_LONG))
                    fc.DATA_OFFSET += 1;
                else if ((fc.TYPE == FIELD_TYPE_BYTES) | (fc.TYPE == FIELD_TYPE_STRING))
                {
                    compressed_length = compress(VSLib.ConvertLongToByte(fc.LENGTH), FIELD_TYPE_LONG);
                    fc.DATA_OFFSET += (compressed_length.Length + 1);
                }

                fc.FULL_LENGTH = fc.DATA_OFFSET + fc.LENGTH;       // Update full length
                fc.STATE = STATE_UPDATED;

                FCache.RemoveAt(index);
                FCache.Insert(index, fc);
            }
            if (serialize)
                this.serialize();
        }

        /// <summary>
        /// Load cahce fields
        /// </summary>
        private void deserialize()
        {

            CURRENT_ALLOC = base.ALLOC;

            // Create/cleanup fields cache
            if (FCache == null)
                FCache = new List<FieldCache>(32);
            else
                FCache.Clear();
            
            // Create cache binary tree
            FCacheTree = new VSBBTree(FCT_NAME, 32, true);                          

            // Read variable data into the buffer
            byte[] data = base.ReadBytes(base.FIXED, (long)FIELDS_SIZE);          

            int offset = FIRST_FIELD_OFFSET_REL;                                    // Offset inside buffer (relative, doesn't include fixed part)

            int n = (int)FIELDS_NUMBER;                                             // Number of fields

            // Load fields
            for (int i = 0; i < n; i++)
            {
                // Create field
                FieldCache f = new FieldCache();
                f.OFFSET = offset;                                                  // Field offset in object
                f.TYPE = data[offset + FIELD_TYPE_POS];                             // Field type

                byte name_length = data[offset + FIELD_NAME_LENGTH_POS];            // Name length
                f.NAME = VSLib.ConvertByteToString(VSLib.GetByteArray(data, (int)(offset + FIELD_NAME_POS), (int)name_length));    // Name

                int value_offset = FIELD_FIXED_LENGTH + name_length;                // Value offset

                // Get value depending on type
                if ((f.TYPE == FIELD_TYPE_BYTE) | (f.TYPE == FIELD_TYPE_SHORT) | (f.TYPE == FIELD_TYPE_DATETIME))
                {
                    f.DATA_OFFSET = value_offset;                                   // Shift to value offset
                    f.LENGTH = FIELD_LENGTHS[f.TYPE];
                }
                else if ((f.TYPE == FIELD_TYPE_INT) | (f.TYPE == FIELD_TYPE_LONG))
                {
                    f.DATA_OFFSET = value_offset + 1;                               // Shift to value offset (+ 1 byte length)
                    f.LENGTH = (int)data[offset + value_offset];
                }
                else if ((f.TYPE == FIELD_TYPE_BYTES) | (f.TYPE == FIELD_TYPE_STRING))
                {
                    int l = (int)data[offset + value_offset];                       // Read number of length bytes
                    if (l > 0)
                    {
                        byte[] ba = VSLib.GetByteArray(data, (int)(offset + value_offset + 1), (int)l);      // Read length
                        f.LENGTH = VSLib.ConvertByteToInt(decompress(ba, FIELD_TYPE_LONG));
                    }
                    else
                        f.LENGTH = 0;

                    f.DATA_OFFSET = value_offset + 1 + l;
                }
                else
                    throw new VSException(DEFS.E0029_INVALID_FIELD_TYPE_CODE, "- " + f.TYPE.ToString());

                f.OLDLENGTH = f.LENGTH;
                f.STATE = STATE_LOADED;
                f.FULL_LENGTH = f.DATA_OFFSET + f.LENGTH;

                if (f.LENGTH > 0)
                    f.VALUE = VSLib.GetByteArray(data, (int)(offset + f.DATA_OFFSET), (int)f.LENGTH);
                else
                    f.VALUE = new byte[0];

                offset += f.FULL_LENGTH;

                f.DELETED = false;

                // Add to cache
                FCache.Add(f);
                
                // Add to the tree
                FCacheTree.Insert(f.NAME, i);           
            }
            FreeSpace = (int)(this.Size - base.FIXED - offset);
        }
        /// <summary>
        /// Load/renew cache if versions doesnt match
        /// </summary>
        private void sync_cache()
        {
            if ((CURRENT_ALLOC == base.ALLOC) & (FCache != null))
                return;
            else
            {
                if (base.ALLOC == ALLOC_TYPE_RAW)
                {
                    if (FreeSpace >= 0)
                    {
                        FCache.Clear();
                        FCache = null;
                        FCacheTree = null;
                        FreeSpace = -1;
                        set_alloc(ALLOC_RESET);
                    }
                }
                else
                    deserialize();          // Load and cache new version of the fields
            }
        }

        /// <summary>
        /// Serialize all fields to object fields space
        /// Free excessive chunks if needed
        /// </summary>
        private void serialize()
        {
            // If no fields
            if (FCache.Count == 0)
            {
                set_alloc(ALLOC_RESET);
                FIELDS_SIZE = 0;
                FreeSpace = -1;
                FCache = null;
                return;
            }

            byte[] b = null;
            int old_size = (int)(FIELDS_SIZE);                     // Current used size (number of fields + all fields size)

            // 1. Calculate new size
            int new_size = FIRST_FIELD_OFFSET_REL;
            for (int i = 0; i < FCache.Count; i++)
                new_size += FCache[i].FULL_LENGTH;

            // 2. Check if space is availabe, extend if required
            if (new_size > (old_size + FreeSpace))
                sp.Extend(this, (new_size - old_size));

            // 3. Check if only value update is required

            List<FieldCache> afc = FCache;
            FCache = new List<FieldCache>(32);

            int array_size = (new_size > old_size) ? new_size : old_size;
            byte[] data = new byte[array_size];


            int data_pos = FIRST_FIELD_OFFSET_REL;
            VSLib.CopyBytes(data, VSLib.ConvertIntToByte(new_size), FIELDS_SIZE_POS, FIELDS_SIZE_LEN);                             // Size
            VSLib.CopyBytes(data, VSLib.ConvertShortToByte((short)afc.Count), FIELDS_NUMBER_POS, FIELDS_NUMBER_LEN);            // Fields number

            FCache.Clear();
            for (int i = 0; i < afc.Count; i++)
            {
                FieldCache fc_element = afc[i];
                if (!fc_element.DELETED)
                {

                    fc_element.OFFSET = data_pos;

                    // Type
                    data[data_pos] = fc_element.TYPE;
                    data_pos++;

                    // Name length
                    data[data_pos] = (byte)(fc_element.NAME.Length);
                    data_pos++;

                    // Name 
                    b = VSLib.ConvertStringToByte(fc_element.NAME);
                    VSLib.CopyBytes(data, b, data_pos, b.Length);
                    data_pos += b.Length;

                    // Value
                    if (FIELD_COMPRESS[fc_element.TYPE])                                                              // int/long/decimal
                    {
                        data[data_pos] = (byte)(fc_element.LENGTH);
                        data_pos++;
                    }
                    else if ((fc_element.TYPE == FIELD_TYPE_BYTES) | (fc_element.TYPE == FIELD_TYPE_STRING))
                    {
                        b = compress(VSLib.ConvertLongToByte(fc_element.LENGTH), FIELD_TYPE_LONG);
                        data[data_pos] = (byte)b.Length;
                        data_pos++;

                        if (b.Length > 0)
                        {
                            VSLib.CopyBytes(data, b, data_pos, b.Length);
                            data_pos += b.Length;
                        }
                    }

                    // Write value
                    if (fc_element.VALUE.Length > 0)
                    {
                        VSLib.CopyBytes(data, fc_element.VALUE, data_pos, fc_element.VALUE.Length);
                        data_pos += afc[i].LENGTH;
                    }

                    // Shift offset and add to cache
                    fc_element.OLDLENGTH = afc[i].LENGTH;
                    fc_element.STATE = STATE_LOADED;

                    FCacheTree.Insert(fc_element.NAME, FCache.Count);
                    FCache.Add(fc_element);
                }
            }

            base.Write(base.FIXED, data, data.Length);

            set_alloc(ALLOC_RENEW);

            // Fill the rest by zeros if le length < old length
            if (new_size < old_size)
            {
                long full_used_size = base.FIXED + new_size;
                b = new byte[old_size - new_size];
                base.Write(full_used_size, b, b.Length);                              // Fill be zeros unused space
                
                // Multiple chunks, chech if there are exessive
                if (base.Chunk > 0)
                    sp.ShrinkObject(this, full_used_size);
            }
        
            FreeSpace = (int)(this.Size - base.FIXED - new_size);

        }



         
        /// <summary>
        /// Find existing field
        /// </summary>
        /// <param name="name"></param>
        /// <param name="type"></param>
        /// <returns>index in the cache or -1(not found)</returns>
        private int find_field(string name, byte type)
        {
            sync_cache();

            if (base.ALLOC == ALLOC_TYPE_RAW)
                return -1;                           // Raw object - return -1
            VSBBTree.BTResult res = FCacheTree.Find(name, 0);
            int idx = (int)FCacheTree.Find(name, 0).Value;
            /*
            for (int i = 0; i < FCacheIndex.Length; i++)
                if (FCacheIndex[i] >= 0)
                    if (FCache[FCacheIndex[i]].NAME == name)
                        return FCacheIndex[i];

            for (int i = 0; i < FCache.Count; i++)
            {
                if (FCache[i].NAME == name.Trim())
                {
                    idx = i;
                    break;
                }
            }
            */
            if (idx < 0)
                return -1;

            if (type > 0)
                if (FCache[idx].TYPE != type)
                    throw new VSException(DEFS.E0026_FIELD_READ_ERROR_CODE, "- " + name + " : type " + FCache[idx].TYPE.ToString() + " doesnt match requested field type " + type.ToString());

            return idx;
        }

        /// <summary>
        /// Compress array of bytes by eliminating higher zero bytes
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static byte[] compress(byte[] value, byte type)
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

            if (n > 0)
                for (int j = 0; j < n; j++)
                    b[j] = value[j];

            return b;
        }
        /// <summary>
        /// decompress array of bytes
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static byte[] decompress(byte[] value, byte type)
        {
            if (!FIELD_COMPRESS[type])
                return value;

            byte[] b = new byte[FIELD_LENGTHS[type]];
            for (int i = 0; i < value.Length; i++)
                b[i] = value[i];
            return b;
        }

        //////////////////////////////////////////////////////////////////
        /////////////// Overwrite 'Read' methods ////////////////////////
        //////////////////////////////////////////////////////////////////

        /// <summary>
        /// Read bytes
        /// </summary>
        /// <param name="address">Relative address</param>
        /// <param name="length"></param>
        /// <returns></returns>
        public new byte[] ReadBytes(long address, long length)
        {
            if ((base.ALLOC != ALLOC_TYPE_RAW) | (FIXED > 0))
            {
                if ((address + length) > FIXED)
                    throw new VSException(DEFS.E0030_INVALID_WRITE_OP_CODE);
            }

            byte[] b = new byte[length];
            base.io_protected(DEFS.OP_READ, address, ref b, length);
            return b;
        }

        /// <summary>
        /// Read byte
        /// </summary>
        /// <param name="address">Relative address</param>
        /// <param name="length"></param>
        /// <returns></returns>
        public new byte ReadByte(long address)
        {
            return this.ReadBytes(address, 1)[0];
        }

        /// <summary>
        /// Read string
        /// </summary>
        /// <param name="address"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public new string ReadString(long address, long length)
        {
            return VSLib.ConvertByteToString(this.ReadBytes(address, length));
        }

        /// <summary>
        /// Read int
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public new int ReadInt(long address)
        {
            return VSLib.ConvertByteToInt(this.ReadBytes(address, 4));
        }

        /// <summary>
        /// Read long
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public new long ReadLong(long address)
        {
            return VSLib.ConvertByteToLong(this.ReadBytes(address, 8));
        }

        /// <summary>
        /// Read short
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public new short ReadShort(long address)
        {
            return VSLib.ConvertByteToShort(this.ReadBytes(address, 2));
        }

        /// <summary>
        /// Read u-short
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public new ushort ReadUShort(long address)
        {
            return VSLib.ConvertByteToUShort(this.ReadBytes(address, 2));
        }

        /// <summary>
        /// Read datetime
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public new DateTime ReadDateTime(long address)
        {
            return new DateTime(this.ReadLong(address));
        }


        //////////////////////////////////////////////////////////////////
        /////////////// Overwrite 'Write' methods ////////////////////////
        //////////////////////////////////////////////////////////////////
        // bytes[]
        public new void Write(long address, byte[] data, long length)
        {
            if ((base.ALLOC != ALLOC_TYPE_RAW) | (FIXED > 0))
            {
                if ((address + length) > FIXED)
                    throw new VSException(DEFS.E0030_INVALID_WRITE_OP_CODE);
            }

            base.Write(address, data, length);
        }
        
        // byte
        public new void Write(long address, byte data)
        {
            byte[] b = new byte[1];
            b[0] = data;
            this.Write(address, b, 1);
        }

        // string
        public new void Write(long address, string data)
        {
            this.Write(address, VSLib.ConvertStringToByte(data), data.Length);
        }

        // int
        public new void Write(long address, int data)
        {
            this.Write(address, VSLib.ConvertIntToByte(data), 4);
        }

        // long
        public new void Write(long address, long data)
        {
            this.Write(address, VSLib.ConvertLongToByte(data), 8);
        }

        // short
        public new void Write(long address, short data)
        {
            this.Write(address, VSLib.ConvertShortToByte(data), 2);
        }

        // u-short
        public new void Write(long address, ushort data)
        {
            this.Write(address, VSLib.ConvertUShortToByte(data), 2);
        }

        // DateTime
        public new void Write(long address, DateTime data)
        {
            this.Write(address, VSLib.ConvertLongToByte(data.Ticks), 8);
        }
    }
}
