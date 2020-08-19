using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VStorage;

namespace VXML
{
    /////////////////////////////////////////////////////////////
    ////////////////////// VXmlException ////////////////////////
    /////////////////////////////////////////////////////////////
    public class VXmlException : Exception
    {
        public int ErrorCode = 0;
        public new string Message = "";
        protected static string prefix = "VXML Error ";
        protected static string[] ERROR_MESSAGES =
        {
            "UNDEFINED",                                                                // 00
            "Content file not found",                                                   // 01
            "Node is up in the tree",                                                   // 02
            "Node is up from different document",                                       // 03
            "Invalid node type",                                                        // 04     
            "Not a child node",                                                         // 05
            "Invalid operation, applicable for root catalog element only",              // 06
            "Operands are the same node",                                               // 07
            "Invalid character in the node name",                                       // 08
            "Invalid node type",                                                        // 09
            "CreateNode: root node already exists",                                     // 10
            "Create from template - node type is not element",                          // 11
            "Path is not found",                                                        // 12
            "Current node, parent or child node is charged out",                        // 13
            "Unsupported ChargeOut format version",                                     // 14
            "ChargeIn error: ",                                                          // 15
            "XQL error: ",                                                              // 16
            "Node with the specified name already exists",                              // 17       
            "Document name is missing",                                                 // 18
            "VXML Node space is not found",                                             // 19
            "VXml Parser - parse error",                                                // 20
            "Invalid node field (internal error)",                                      // 21
            "Space owner is not VSXML or undefined",                                    // 22
            "Space owner is undefined but space is not empty",                          // 23
            "One node must be selected",                                                // 24
            "VXml Parser - file read error",                                            // 25
            "Set Tag/Attribute - no DocumentElement node",                              // 26
            "Bulk node create - empty value",                                           // 27
            "Storage is undefined",                                                     // 28
            "UNDEFINED",                                                                // 29
            "UNDEFINED",                                                                // 30
            "UNDEFINED",                                                                // 31
            "UNDEFINED",                                                                // 32
            "UNDEFINED",                                                                // 33
            "UNDEFINED",                                                                // 34
            "UNDEFINED",                                                                // 35
            "UNDEFINED",                                                                // 36
            "UNDEFINED",                                                                // 37
            "UNDEFINED",                                                                // 38
            "UNDEFINED",                                                                // 39
            "UNDEFINED"                                                                 // 40
        };

        /// <summary>
        /// ERROR CODES
        /// </summary>

        public const int E0001_CONTENT_FILE_NOT_FOUND_CODE = 1;
        public const int E0002_NODE_IS_UP_TREE_CODE = 2;
        public const int E0003_NODE_IS_FROM_DIFFERENT_DOC_CODE = 3;
        public const int E0004_INVALID_NODE_TYPE_CODE = 4;
        public const int E0005_NOT_A_CHILD_NODE_CODE = 5;
        public const int E0006_CATALOG_INVALID_OP_CODE = 6;
        public const int E0007_OLD_EQUAL_NEW_CODE = 7;
        public const int E0008_INVALID_CHAR_CODE = 8;
        public const int E0009_INVALID_TYPE_CODE = 9;
        public const int E0010_ROOT_EXISTS_CODE = 10;
        public const int E0011_XML_CREATE_INVALID_TYPE_CODE = 11;
        public const int E0012_PATH_NOT_FOUND_CODE = 12;
        public const int E0013_ALREADY_CHARGED_OUT_CODE = 13;
        public const int E0014_UNSUPPORTED_CHARGEOUT_VERSION_CODE = 14;
        public const int E0015_CHARGE_ERROR_CODE = 15;
        public const int E0016_XQL_ERROR_CODE = 16;
        public const int E0017_DOC_EXISTS_CODE = 17;
        public const int E0018_DOC_NAME_MISSING_CODE = 18;
        public const int E0019_SPACE_MISSING_CODE = 19;
        public const int E0020_XML_PARSE_ERROR_CODE = 20;
        public const int E0021_INVALID_NODE_FIELD_CODE = 21;
        public const int E0022_NOT_VSXML_NODE_SPACE_CODE = 22;
        public const int E0023_NOT_EMPTY_UNDEFINED_SPACE_CODE = 23;
        public const int E0024_ONE_NODE_MUST_BE_SELECTED_CODE = 24;
        public const int E0025_XML_FILE_ERROR_CODE = 25;
        public const int E0026_SET_NO_DOCUMENTELEMENT_ERROR_CODE = 26;
        public const int E0027_BULK_CREATE_EMPTY_VALUE_ERROR_CODE = 27;
        public const int E0028_STORAGE_UNDEFINED_CODE = 28;

        /// <summary>
        /// Thow exception
        /// </summary>
        /// <param name="code"></param>
        /// <param name="message_ext">Message to append</param>
        public VXmlException(int code, string message_ext = "")
            : base(prefix + code.ToString("D4") + " " + (GetMessage(code) + " " + message_ext).Trim())
        {
            ErrorCode = code;
            Message = prefix + code.ToString("D4") + " " + (GetMessage(code) + " " + message_ext).Trim();
        }

        /// <summary>
        /// Get message by code
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        public static string GetMessage(int code)
        {
            return ERROR_MESSAGES[code];
        }

    }
}
