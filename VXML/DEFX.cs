using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VXML
{
    /////////////////////////////////////////////////////////////
    ///////////////// VSXML static definitions //////////////////
    /////////////////////////////////////////////////////////////
    public static class DEFX
    {

        ///////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////// PUBLIC /////////////////////////////////////////
        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Keys
        /// </summary>
        public const string KEY_SNAP = "snap-path.vs.default";
        public const string KEY_LOAD_XML = "loadxml-path.vs.default";

        /// <summary>
        /// Node name patterns
        /// </summary>
        public const string START_PATTERN = "0123456789qwertyuiopasdfghjklzxcvbnmQWERTYUIOPASDFGHJKLZXCVBNM_";
        public const string NON_START_PATTERN = "-.:";

        /// <summary>
        /// Predefined space names
        /// </summary>
        public const string XML_SPACE_NAME =            "vxmlbase";         // Base vxml space
        public const string XML_CONTENT_SPACE_NAME =    "vxmlcont";         // Optional content space
        public const string XML_INDEX_SPACE_NAME =      "vxmlindx";         // Index vxml space

        /// <summary>
        /// Export file type (extension)
        /// </summary>
        public const string XML_EXPORT_FILE_TYPE = "vexp";                    // file type for snap files (chargeout)

        /// <summary>
        /// Root catalog node name
        /// </summary>
        public const string ROOT_CATALOG_NODE_NAME = "root";                 


        ///////////////////////////////////////////////////////////////
        //////////////////// METHODS //////////////////////////////////
        ///////////////////////////////////////////////////////////////

        /// <summary>
        /// Get node type (text)
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string GET_NODETYPE(short type)
        {
            return (type < 100) ? NODE_TYPE[type] : NODE_TYPE_INTERNAL[type - 100];
        }

        /// <summary>
        /// The list of types(codes) valid as child node for 'type' (Reference is not included!)
        /// </summary>
        /// <param name="type"></param>
        public static short[] BR_CHILD_VALID_TYPE_CODES(short type)
        {
            short[] TYPE_UNDEFINED_CH = { };
            short[] TYPE_CATALOG_CH = { NODE_TYPE_CATALOG, NODE_TYPE_DOCUMENT, NODE_TYPE_ATTRIBUTE, NODE_TYPE_COMMENT, NODE_TYPE_TAG, NODE_TYPE_REFERENCE};
            short[] TYPE_DOCUMENT_CH = { NODE_TYPE_ELEMENT, NODE_TYPE_ATTRIBUTE, NODE_TYPE_COMMENT, NODE_TYPE_TAG };
            short[] TYPE_ELEMENT_CH = { NODE_TYPE_ELEMENT, NODE_TYPE_TEXT, NODE_TYPE_ATTRIBUTE, NODE_TYPE_COMMENT, NODE_TYPE_CONTENT, NODE_TYPE_TAG };
            short[] TYPE_CONTENT_CH = { NODE_TYPE_ATTRIBUTE, NODE_TYPE_COMMENT };

            switch (type)
            {
                case NODE_TYPE_CATALOG:
                    return TYPE_CATALOG_CH;
                case NODE_TYPE_DOCUMENT:
                    return TYPE_DOCUMENT_CH;
                case NODE_TYPE_ELEMENT:
                    return TYPE_ELEMENT_CH;
                case NODE_TYPE_CONTENT:
                    return TYPE_CONTENT_CH;
                default:
                    return TYPE_UNDEFINED_CH;
            }
        }
        
        /// <summary>
        /// The list of types(namess) valid as child node for 'type'
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string[] BR_CREATE_VALID_TYPES(short type)
        {
            short[] l = BR_CHILD_VALID_TYPE_CODES(type);
            string[] s = new string[l.Length];
            for (int i = 0; i < l.Length; i++)
                s[i] = GET_NODETYPE(l[i]);
            return s;
        }


        /// <summary>
        /// Check if type is valid to create child
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool BR_CHILD_IS_VALID_TYPE(short parent_type, short type)
        {
            short[] l = BR_CHILD_VALID_TYPE_CODES(parent_type);
            for (int i = 0; i < l.Length; i++)
                if (l[i] == type)
                    return true;
            return false;
        }

        /// <summary>
        /// Check if type is valid for save/load xml
        /// </summary>
        /// <param name="parent_type"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool BR_XML_IS_VALID_TYPE(short parent_type)
        {
            return ((parent_type == DEFX.NODE_TYPE_ELEMENT) | (parent_type == DEFX.NODE_TYPE_DOCUMENT));
        }

        /// <summary>
        /// Check if type is valid for insert before
        /// </summary>
        /// <param name="parent_type"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool BR_INSERT_IS_VALID_TYPE(short target_type, short type)
        {
            return (target_type == type);
        }

        /// <summary>
        /// Check if type is valid for chargein/chargeout
        /// </summary>
        /// <param name="parent_type"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool BR_CHARGEIN_IS_VALID_TYPE(short parent_type)
        {
            return ((parent_type == DEFX.NODE_TYPE_ELEMENT) | (parent_type == DEFX.NODE_TYPE_DOCUMENT) | (parent_type == DEFX.NODE_TYPE_CATALOG));
        }
        /// <summary>
        /// Check if type is valid for chargeout/snap
        /// </summary>
        /// <param name="parent_type"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool BR_CHARGEOUT_IS_VALID_TYPE(short parent_type)
        {
            return ((parent_type == DEFX.NODE_TYPE_ELEMENT) | (parent_type == DEFX.NODE_TYPE_DOCUMENT));
        }

        /// <summary>
        /// Check if node name is valid
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static bool BR_NODE_NAME_VALID(string name)
        {
            if ((DEFX.START_PATTERN.IndexOf(name.Substring(0, 1)) < 0))
               return false;
            else
            {
                for (int i = 1; i < name.Length; i++)
                    if (((DEFX.START_PATTERN + DEFX.NON_START_PATTERN).IndexOf(name.Substring(i, 1)) < 0))
                        return false;
                return true;
            }
        }

        public static bool BR_NODE_RENAME(short type)
        {
            return ((type == NODE_TYPE_CATALOG) | (type == NODE_TYPE_DOCUMENT) | (type == NODE_TYPE_ELEMENT));
        }

        /// <summary>
        /// If type is valid for reference
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool BR_NODE_REFERENCE(short type)
        {
            return ((type == NODE_TYPE_CATALOG) | (type == NODE_TYPE_DOCUMENT));
        }

        /// <summary>
        /// If type can have reference
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool BR_CAN_HAVE_REFERENCE(short type)
        {
            return (type == NODE_TYPE_CATALOG);
        }

        /// <summary>
        /// Check if node need user-defined name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static bool BR_NODE_NEED_NAME(short type)
        {
            if ((type == NODE_TYPE_ATTRIBUTE) | (type == NODE_TYPE_CATALOG) | (type == NODE_TYPE_ELEMENT) | (type == NODE_TYPE_DOCUMENT))
                return true;
            else 
                return false;
        }

        /// <summary>
        /// Check if index shall be created for node type
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        internal static bool BR_INDEX_REQUIRED(short type)
        {
            return ((type == NODE_TYPE_CATALOG) | (type == NODE_TYPE_DOCUMENT));
        }

        /// <summary>
        /// Check if XQL type
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        internal static bool BR_NODE_TYPE_XQL(short type)
        {
            return ((type < 100) & (type != NODE_TYPE_CONTENT));
        }


        ///////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////// INTERNAL /////////////////////////////////////////
        ///////////////////////////////////////////////////////////////////////////////////////////

        internal const string ENCRYPT_CHARGEOUT = "vschargeoutfile";

        /// <summary>
        /// Node states
        /// </summary>
        internal const short NODE_STATE_UNCHARGED = 0;
        internal const short NODE_STATE_CHARGED = 1;

        /// <summary>
        /// Lisof types: all and for XQL
        /// </summary>
        internal const short NODE_TYPES_XQL = -2;             // Types applicable to XQL (document, Catalog, Element, Reference)
        internal const short NODE_TYPES_ALL = -1;             // All possible types

        /// <summary>
        /// Index names
        /// </summary>
        internal const string INDEX_NAME_CHARGEOUT =    "$vxmlchg$";
        internal const string INDEX_NAME_REFERENCE =    "$vxmlref$";

        internal const string INDEX_NAME_ELEMENT =      "$vxmlelm$";
        internal const string INDEX_NAME_CATALOG =      "$vxmlcat$";
        internal const string INDEX_NAME_DOCUMENT =     "$vxmldoc$";
        internal const string INDEX_NAME_TAG =          "$vxmltag$";

        // Create or no index for type


        /// <summary>
        /// Space System Owner
        /// </summary>
        internal const string SYSTEM_OWNER_VSXML = "$VSXML$";

        /// <summary>
        /// Chargeout file signature
        /// </summary>
        internal const string CHARGEOUT_SIGNATURE = "$chg";


        /// <summary>
        /// Node type codes 
        /// </summary>
        public const short NODE_TYPE_UNDEFINED =        0;
        public const short NODE_TYPE_CATALOG =          1;
        public const short NODE_TYPE_DOCUMENT =         2;
        public const short NODE_TYPE_ELEMENT =          3;
        public const short NODE_TYPE_CONTENT =          4;
        public const short NODE_TYPE_REFERENCE =        5;
        public const short NODE_TYPE_ATTRIBUTE =        100;
        public const short NODE_TYPE_COMMENT =          101;
        public const short NODE_TYPE_TEXT =             102;
        public const short NODE_TYPE_TAG =              103;
        internal const short NODE_TYPE_INSTRUCTION =    104;

        /// <summary>
        /// Node space allocation pools for types
        /// Types 1-4 can have child nodes/attributes
        /// </summary>

        internal static short[] NODE_TYPE_CODE = {
                                                 0,    // NODE_TYPE_UNDEFINED
                                                 1,    // NODE_TYPE_CATALOG
                                                 2,    // NODE_TYPE_DOCUMENT
                                                 3,    // NODE_TYPE_ELEMENT
                                                 4,    // NODE_TYPE_CONTENT
                                                 5     // NODE_TYPE_REFERENCE
                                             };

        internal static short[] NODE_XQL_TYPES = { DEFX.NODE_TYPE_CATALOG, DEFX.NODE_TYPE_DOCUMENT, DEFX.NODE_TYPE_ELEMENT, DEFX.NODE_TYPE_REFERENCE };
        internal static short[] NODE_NON_XQL_TYPES = { DEFX.NODE_TYPE_CONTENT };

        /// <summary>
        /// Node type (text)
        /// Only undefined (0) and types that are created as separate node objects
        /// </summary>
        internal static string[] NODE_TYPE = 
        { 
                   "undefined",     // 0
                   "catalog",       // 1
                   "document",      // 2
                   "element",       // 3
                   "content",       // 4
                   "reference"      // 5
        };

        /// <summary>
        /// Internal node types (text)
        /// Only internal types started at 100
        /// </summary>
        internal static string[] NODE_TYPE_INTERNAL = 
        { 
                   "attribute",     // 100
                   "comment",       // 101
                   "text",          // 102
                   "tag",           // 103
                   "instruction",   // 104
        };

        /// <summary>
        /// Internal node types field prefixes
        /// Only internal types started at 100
        /// </summary>
        internal static string[] NODE_TYPE_INTERNAL_FIELD_PREFIX = 
        { 
                   PREFIX_ATTRIBUTE,     // 100
                   PREFIX_COMMENT,       // 101
                   PREFIX_TEXT,          // 102
                   PREFIX_TAG,           // 103
                   "instruction",   // 104
        };


        /// <summary>
        /// 'All' node type mask
        /// </summary>
        internal static ushort NODE_MASK_ALL = 0xffff;
 
        /// <summary>
        /// Bit masks for node types
        /// </summary>
        internal static ushort[] NODE_MASK =
        {
            0x0000,     // undefined
            0x0001,     // catalog
            0x0002,     // document
            0x0004,     // element
            0x0008,     // content
            0x0010      // reference
        };

        /// <summary>
        /// Chargeout node type 'element'
        /// </summary>
        internal const string CHARGEOUT_TYPE_ELEMENT = "$elm";

        /// <summary>
        /// Chargeout node type 'document'
        /// </summary>
        internal const string CHARGEOUT_TYPE_DOCUMENT = "$doc";

        /// <summary>
        /// Chargeout format version
        /// </summary>
        internal const long CHARGEOUT_FORMAT_VERSION = 1;

        /// <summary>
        /// Chargeout Header reserved length
        /// </summary>
        internal const int CHARGEOUT_HEADER_RESERVE = 64;

        /// <summary>
        /// Chargeout node reserved length
        /// </summary>
        internal const int CHARGEOUT_NODE_RESERVE = 32;

        /// <summary>
        /// Block size for content
        /// </summary>
        internal const long CONTENT_BLOCKSIZE = 65536;

        /// <summary>
        /// First-Last by node type
        /// </summary>
        internal static string F_FIRST(short nodetype)
        {
            return "f" + nodetype.ToString();
        }

        internal static string F_LAST(short nodetype)
        {
            return "l" + nodetype.ToString();
        }

        /// <summary>
        /// Fields keys
        /// </summary>
        internal const string F_REF_ID = "#rf";

        internal const string F_CONT_ID = "#ct";

        internal const string F_CHARGEOUT_DATE = "#ch";

        internal const string F_NAME = "#nm";

        internal const string F_VALUE = "#va";

        internal const string F_GUID = "#gu";

        internal const string F_CREFS = "#cr";          // Child nodes refs (type + id)

        /// <summary>
        /// Prefix for VSObject attribute name
        /// </summary>
        public const string PREFIX_ATTRIBUTE =    "%";
        public const string PREFIX_COMMENT =      "^";
        public const string PREFIX_TEXT =         "&";
        public const string PREFIX_TAG =          "~";
    }
}
