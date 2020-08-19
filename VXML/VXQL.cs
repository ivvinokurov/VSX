using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VStorage;

namespace VXML
{
    /////////////////////////////////////////////////////////////
    ///////////////////////// VXQL //////////////////////////////
    /////////////////////////////////////////////////////////////
    class VXQL
    {
        /////////////////////////////////////////////////////////////
        /////////////////////// Structures //////////////////////////
        /////////////////////////////////////////////////////////////

        /// <summary>
        /// XQL record
        /// </summary>
        public struct XPATH_CMD
        {
            public string command;
            public string operand;
            public string value;
        }

        /////////////////////////////////////////////////////////////
        /////////////////////// CONSTANTS ///////////////////////////
        /////////////////////////////////////////////////////////////

        /// <summary>
        /// CONTEXT DEFINITION
        /// </summary>
        public const string CONTEXT_RECURSIVE_CURRENT = ".//";
        public const string CONTEXT_RECURSIVE_ROOT = "///";
        public const string CONTEXT_PARENT = "..";
        public const string CONTEXT_RECURSIVE_DOWN = "//";
        public const string CONTEXT_CURRENT = "./";
        public const string CONTEXT_CURRENT_DOT = ".";
        public const string CONTEXT_ROOT = "/";
        public const string CONTEXT_PREDICATE = "[";
        public const string CONTEXT_PREDICATE2 = "{";
        public const string CONTEXT_PREDICATE_END = "]";
        public const string CONTEXT_PREDICATE_END2 = "}";
        public const string CONTEXT_PREDICATE_TAG = "#";
        public const string CONTEXT_PREDICATE_FIRST = "first()";
        public const string CONTEXT_PREDICATE_LAST = "last()";
        public const string CONTEXT_CONTENT = "content()";
        public const string CONTEXT_ATTRIBUTE = "@";

        // COMMANDS

        // SEARCH
        public const string CMD_SET_SEARCH = "##set_search##";                      
        // Values:
        public const string SEARCH_SUBTREE = "##subtree##";                     // All nodea in subtree
        public const string SEARCH_NODE = "##node##";                           // Only last child level

        // SELECT
        public const string CMD_SELECT = "##select##";                              
        // Operands:
        public const string SELECT_NODE = "##node##";                               
        // Values:
        public const string SELECT_NODE_CURRENT = "##current##";                    // Current node
        public const string SELECT_NODE_ROOT = "##root##";                          // Root node
        public const string SELECT_NODE_PARENT = "##parent##";                      // Parent node
        // Or: node name pattern


        // ERROR
        public const string CMD_ERROR = "##error##";                                // return error
        // Value:
        public const string ERROR_PARSE = "##parse##";                              // parser (operand)

        // RETURN REFERENCE OR NODE 
        public const string CMD_SET_REFERENCE = "##set_reference##";                     // result type (node/reference) for catalog/document
        // Operand:
        public const string REFERENCE_OFF = "##reference_off##";
        public const string REFERENCE_ON = "##reference_on##";
        // Value: empty
        private const string REFERENCE_PREFIX = "~";                 // Prefix to include reference '~'
        //private const bool RESULT_REFERENCE = true;                  // true  - include reference
        //private const bool RESULT_NODE = false;                      // false - include referencing node



        // WHERE 
        public const string CMD_WHERE_BEGIN = "##where_begin##";                    // Begin
        public const string CMD_WHERE_END = "##where_end##";                        // End
        public const string CMD_WHERE_CONDITION = "##where_condition##";            // Condition


        // WHERE NODE - node identification condition
        public const string CMD_WHERE_NODE = "##where_node##";
        // Operand:
        public const string NODE_INDEX = "##node_index##";                          // Node index (1..n) 
        public const string NODE_FIRST = "##node_first##";                          // First node
        public const string NODE_LAST = "##node_last##";                            // Last node
        // Or operand = node name
        // Value:
        //      NODE_INDEX  - index value
        //      NODE_FIRST  - empty
        //      NODE_LAST   - empty
        //      Node name   - empty value - child node exists, otherwise - child node value

        // WHERE ATTRIBUTE
        public const string CMD_WHERE_ATTRIBUTE = "##where_attribute##";
        // Operand:
        //      attribute name
        // Value:
        //      empty - attribute exists
        //      otherwise - attribute value

        // WHERE TAG
        public const string CMD_WHERE_TAG = "##where_tag##";
        // Operand:
        //      tag values separated by '#"

        /// <summary>
        /// SELECT TYPE COMMAND
        /// </summary>
        private const string CMD_SELECT_TYPE = "##select_type##";                       // Type to select

        private const string SELECT_TYPE_ELEMENT = "##select_element##";                // Root node is element
        private const string SELECT_TYPE_CATALOG = "##select_catalog##";                // [PREFIX='$'] Root node is catalog. Select catalog nodes
        private const string SELECT_TYPE_DOCUMENT = "##select_document##";              // [PREFIX='#'] Root node is catalog. Select document nodes (full search) 

        /// <summary>
        /// Comparison operations for CMD_WHERE_NODE/CMD_WHERE_ATTRIBUTE/CMD_WHERE_TAG
        /// </summary>
        public const string PREDICATE_OP_NE = "NE:";                // Not equal, case-sensitive for text
        public const string PREDICATE_OP_NE_CHECK = "!=";

        public const string PREDICATE_OP_GE = "GE:";                // More or equal (numeric)
        public const string PREDICATE_OP_GE_CHECK = ">=";

        public const string PREDICATE_OP_LE = "LE:";                // Less or equal (numeric)
        public const string PREDICATE_OP_LE_CHECK = "<=";

        public const string PREDICATE_OP_EQ = "EQ:";                // Equal, case-sensitive for text
        public const string PREDICATE_OP_EQ_CHECK = "=";

        public const string PREDICATE_OP_GT = "GT:";                // Greater (numeric)
        public const string PREDICATE_OP_GT_CHECK = ">";

        public const string PREDICATE_OP_LT = "LT:";                // Less (numeric)
        public const string PREDICATE_OP_LT_CHECK = "<";

        public const string PREDICATE_OP_EX = "EX:";                // Exists    

        public const string PREDICATE_OP_ES = "ES:";                // Equal, NOT case-sensitive (text only)
        public const string PREDICATE_OP_ES_CHECK = "=$";

        public const string PREDICATE_OP_NS = "NS:";                // Not equal, NOT case-sensitive (text only)
        public const string PREDICATE_OP_NS_CHECK = "!=$";

        /// <summary>
        /// Comparison data types for CMD_WHERE_NODE/CMD_WHERE_ATTRIBUTE/CMD_WHERE_TAG
        /// </summary>
        public const string PREDICATE_TYPE_STRING = "S:";           // String
        public const string PREDICATE_TYPE_NUMERIC = "N:";          // Numeric

        public const int PREDICATE_TYPE_LENGTH = 2;                 // Type length
        public const int PREDICATE_OP_LENGTH = 3;                   // Operation length

        // VALUE STRUCTURE: PREDICATE_OP+PREDICATE_TYPE+<value>

        /// <summary>
        /// Logical predicates for condition levels
        /// </summary>
        public const string PREDICATE_COND_NA = "#NA#";             // Default value (Not Available). 'AND' will be assigned further if N/A.

        public const string PREDICATE_COND_AND = "#AND#";           // 'AND' value
        public const string PREDICATE_COND_AND_CHECK = "&";         // 'AND' check

        public const string PREDICATE_COND_OR = "#OR#";             // 'OR' value
        public const string PREDICATE_COND_OR_CHECK = "|";          // 'OR' check



        /////////////////////////////////////////////////////////////
        ///////////////////////// FIELDS ////////////////////////////
        /////////////////////////////////////////////////////////////
        /// <summary>
        /// Root node definition
        /// </summary>
        private VXmlNode NODE;                                      // Root node for search
        private short NODE_TYPE;                                    // Catalog or Element
        
        private string NODE_SELECT_TYPE;                             // For catalog: select documents (no prefix) or catalog nodes (if prefix '$')

        public string SEARCH_MODE = SEARCH_NODE;                     // Only current level by default

        
        public string ERROR = "";                                   // Error message

        private string RESULT_REFERENCE = REFERENCE_OFF;            // Result type for reference nodes

        private List<XPATH_CMD> XQL = new List<XPATH_CMD>();        // XQL command list

        private List<long> BLACK_LIST;                              // Black list to prevent recursive search of the REFERENCE nodes

//#if DEBUG
//        public VSTimer TIMER = new VSTimer();
//#endif
    
        /// <summary>
        /// Empty constructor
        /// </summary>
        /// <param name="node"></param>
        /// <param name="xpath"></param>
        /// <param name="all"></param>
        public VXQL(VXmlNode node)
        {
//#if DEBUG
//            TIMER = new VSTimer();
///#endif
            if (node.NodeTypeCode == DEFX.NODE_TYPE_DOCUMENT)
                NODE = ((VXmlDocument)node).DocumentElement;
            else if ((node.NodeTypeCode == DEFX.NODE_TYPE_ELEMENT) | (node.NodeTypeCode == DEFX.NODE_TYPE_CATALOG))
            {
                NODE = node;
                NODE_TYPE = NODE.NodeTypeCode;
            }
            else
                throw new VXmlException(VXmlException.E0004_INVALID_NODE_TYPE_CODE, ": " + node.NodeType + " VXQL");
        }

        /// <summary>
        /// Parse XPATH expression and return array of internal subcomands
        /// </summary>
        /// <param name="xpath"></param>
        /// <returns></returns>
        public string ParseXQL(string xpath)
        {
//#if DEBUG
//            TIMER.START("pars");
//#endif

            XQL.Clear();

            string error = "";                                              // Error message

            // Context definition fields
            string context_operation = "";
            bool context_defined = false;                                   
            
            bool name_defined = false;                                      // If search name is defined (for predicates)

            string inpath = xpath.Trim();
            if (NODE_TYPE == DEFX.NODE_TYPE_CATALOG)
            {
                add_cmd(CMD_SELECT_TYPE, SELECT_TYPE_DOCUMENT);

                // Handle prefix if Catalog node
                for (int i = 0; i < inpath.Length; i++)
                {
                    if (inpath.Substring(0, 1) == "$")              // Select catalog nodes rather than documents if NODE is catalog
                    {
                        add_cmd(CMD_SELECT_TYPE, SELECT_TYPE_CATALOG);
                        inpath = inpath.Remove(0, 1);
                    }
                    else if (inpath.Substring(0, 1) == "#")         // Select catalog nodes rather than documents if NODE is catalog
                    {
                        add_cmd(CMD_SELECT_TYPE, SELECT_TYPE_DOCUMENT);
                        inpath = inpath.Remove(0, 1);
                    }
                    else if (inpath.Substring(0, 1) == "~")         // Select references rather than referenced object (catalog or document)
                    {
                        add_cmd(CMD_SET_REFERENCE, REFERENCE_ON);
                        inpath = inpath.Remove(0, 1);
                    }
                    else
                        break;
                }
            }
            else
                add_cmd(CMD_SELECT_TYPE, SELECT_TYPE_ELEMENT);

            int pos = 0;                                            // Current parse position
            int pos_shift = 0;                                      // if './' is inserted to provide correct shift in 'parse error' message

            if (inpath.Length == 0)
                inpath = "*";                                       // 7/20/16 - if empty then select all ('*')

            inpath += "                     ";

            while ((pos < inpath.Length) & (error == ""))
            {
                // ' ' ignore spaces
                if (inpath.Substring(pos, 1) == " ")
                    pos++;
                // .// recursive current
                else if (inpath.Substring(pos, CONTEXT_RECURSIVE_CURRENT.Length) == CONTEXT_RECURSIVE_CURRENT)
                {
                    if (pos != 0)
                        error = "Error: expression './/' is not at the beginning";
                    else
                    {
                        context_defined = true;
                        context_operation = CONTEXT_RECURSIVE_CURRENT;
                        add_cmd(CMD_SET_SEARCH, SEARCH_SUBTREE);
                        add_cmd(CMD_SELECT, SELECT_NODE, SELECT_NODE_CURRENT);
                        pos += CONTEXT_RECURSIVE_CURRENT.Length;
                    }
                }
                // /// recursive all tree
                else if (inpath.Substring(pos, CONTEXT_RECURSIVE_ROOT.Length) == CONTEXT_RECURSIVE_ROOT)
                {
                    if (pos != 0)
                        error = "Error: expression '///' is not at the beginning";
                    else
                    {
                        context_defined = true;
                        context_operation = CONTEXT_RECURSIVE_DOWN;
                        add_cmd(CMD_SET_SEARCH, SEARCH_SUBTREE);
                        add_cmd(CMD_SELECT, SELECT_NODE, SELECT_NODE_ROOT);
                        pos += CONTEXT_RECURSIVE_ROOT.Length;
                    }
                }
                // // - recursive search all subtree starting from childs
                else if (inpath.Substring(pos, CONTEXT_RECURSIVE_DOWN.Length) == CONTEXT_RECURSIVE_DOWN)
                {
                    context_defined = false;
                    context_operation = CONTEXT_RECURSIVE_DOWN;
                    add_cmd(CMD_SET_SEARCH, SEARCH_SUBTREE);
                    //if (pos == 0)
                    //{
                    //    add_cmd(CMD_SELECT, SELECT_NODE, SELECT_NODE_ROOT);
                    //    context_defined = true;
                    //}
                    pos += CONTEXT_RECURSIVE_DOWN.Length;
                }
                // .. - shall be in the beginning, reference to the parent node
                else if (inpath.Substring(pos, CONTEXT_PARENT.Length) == CONTEXT_PARENT)
                {
                    if (pos > 0)
                        error = "Error: invalid location of '..' path locator at position " + (pos + pos_shift).ToString();
                    else
                    {
                        context_defined = true;
                        context_operation = CONTEXT_PARENT;
                        add_cmd(CMD_SELECT, SELECT_NODE, SELECT_NODE_PARENT);
                        pos += CONTEXT_PARENT.Length;
                    }
                }
                // './' - current node
                else if ((inpath.Substring(pos, CONTEXT_CURRENT.Length) == CONTEXT_CURRENT) | (inpath.Substring(pos, CONTEXT_CURRENT_DOT.Length) == CONTEXT_CURRENT_DOT))
                {
                    if (pos > 0)
                        error = "Error: invalid location of '.' path locator at position " + (pos + pos_shift).ToString();
                    else
                    {
                        context_defined = true;
                        context_operation = CONTEXT_CURRENT;
                        add_cmd(CMD_SET_SEARCH, SEARCH_NODE);
                        add_cmd(CMD_SELECT, SELECT_NODE, SELECT_NODE_CURRENT);
                        pos += (inpath.Substring(pos, CONTEXT_CURRENT.Length) == CONTEXT_CURRENT) ? CONTEXT_CURRENT.Length : CONTEXT_CURRENT_DOT.Length;
                    }
                }
                // '/' - root node or context level separator
                else if (inpath.Substring(pos, CONTEXT_ROOT.Length) == CONTEXT_ROOT)
                {
                    if (context_operation == CONTEXT_ROOT)
                        error = "Error: invalid expression at position " + (pos + pos_shift).ToString();
                    else
                    {
                        name_defined = false;
                        context_defined = false;
                        add_cmd(CMD_SET_SEARCH, SEARCH_NODE);
                        if (pos == 0)
                        {
                            add_cmd(CMD_SELECT, SELECT_NODE, SELECT_NODE_ROOT);
                            if ((pos + CONTEXT_ROOT.Length) == inpath.Length)
                                context_defined = true;
                        }
                        context_operation = CONTEXT_ROOT;
                        pos += CONTEXT_ROOT.Length;
                    }
                }
                //////////////////////////////////////////////////////////////////////////
                ///////////   [ or { - start condition definition /////////////////////////////
                //////////////////////////////////////////////////////////////////////////
                else if ((inpath.Substring(pos, CONTEXT_PREDICATE.Length) == CONTEXT_PREDICATE) | (inpath.Substring(pos, CONTEXT_PREDICATE2.Length) == CONTEXT_PREDICATE2))
                {
                    add_cmd(CMD_WHERE_BEGIN);
                    if (!context_defined)
                        error = "Error: missing context operation at position " + (pos + pos_shift).ToString();
                    else if (!name_defined)
                        error = "Error: missing name for predicate at position " + (pos + pos_shift).ToString();
                    else
                    {
                        int LEVEL = 0;
                        bool PRED = false;
                        bool COND = true;               // Will be set to false in '[', otherwise error
                        while (pos < inpath.Length)     // & (error == ""))
                        {
                            string fsm = inpath.Substring(pos, 1);
                            // ' ' (space) - just ignore
                            if (fsm == " ")
                                pos += 1;
                            else
                            {
                                // '['
                                if ((fsm == CONTEXT_PREDICATE) | (fsm == CONTEXT_PREDICATE2))
                                {
                                    LEVEL += 1;
                                    PRED = true;
                                    COND = false;
                                    add_cmd(CMD_WHERE_BEGIN);
                                    pos += CONTEXT_PREDICATE.Length;
                                }
                                // ']' or '}'
                                else if ((fsm == CONTEXT_PREDICATE_END) | (fsm == CONTEXT_PREDICATE_END2))
                                {
                                    if (LEVEL == 0)
                                        error = "Error: invalid condition level at position " + (pos + pos_shift).ToString();
                                    else
                                    {
                                        LEVEL -= 1;
                                        PRED = false;
                                        add_cmd(CMD_WHERE_END);
                                        pos += CONTEXT_PREDICATE_END.Length;
                                    }
                                }
                                // '/'
                                else if (fsm == CONTEXT_ROOT)
                                {
                                    break;          // End of predicates processing
                                }
                                // '&'
                                else if (fsm == PREDICATE_COND_AND_CHECK)
                                {
                                    if (PRED | COND)
                                        error = "Error: invalid condition at position " + (pos + pos_shift).ToString();
                                    else
                                    {
                                        add_cmd(CMD_WHERE_CONDITION, PREDICATE_COND_AND);
                                        pos += PREDICATE_COND_AND_CHECK.Length;
                                    }
                                }
                                // '|'
                                else if (fsm == PREDICATE_COND_OR_CHECK)
                                {
                                    if (PRED | COND)
                                        error = "Error: invalid condition at position " + (pos + pos_shift).ToString();
                                    else
                                    {
                                        add_cmd(CMD_WHERE_CONDITION, PREDICATE_COND_OR);
                                        pos += PREDICATE_COND_OR_CHECK.Length;
                                    }
                                }
                                // Any other
                                else
                                {
                                    if (!PRED)
                                    {
                                        error = "Error: missing predicate at position " + (pos + pos_shift).ToString();
                                        break;
                                    }
                                    int end_predicate = inpath.IndexOf(CONTEXT_PREDICATE_END, pos);
                                    int end_predicate2 = inpath.IndexOf(CONTEXT_PREDICATE_END2, pos);

                                    if (end_predicate < 0)
                                        end_predicate = end_predicate2;
                                    else if ((end_predicate2 >= 0) & (end_predicate2 < end_predicate))
                                        end_predicate = end_predicate2;

                                    if (end_predicate == 0)
                                    {
                                        error = "Error: empty predicate at position " + (pos + pos_shift).ToString();
                                        break;
                                    }
                                    else if (end_predicate < 0)
                                    {
                                        error = "Error: missing end predicate (']') at position " + (pos + pos_shift).ToString();
                                        break;
                                    }

                                    string predicate = (inpath.Substring(pos, end_predicate - pos)).Trim();


                                    
                                    long idx = 0;
                                    if (predicate.Substring(0,1) == CONTEXT_PREDICATE_TAG)
                                    {
                                        // op: 4 - number of tags
                                        // value: for each tag: 4 - tag length + <var>tag
                                        string t_pre = predicate.Trim() + "#";
                                        string res_pred = "";
                                        List<string> tags = new List<string>();
                                        int t_pos = 0;
                                        int t_len = 0;
                                        int t_i = t_pre.IndexOf(CONTEXT_PREDICATE_TAG, t_pos + 1);
                                        while (t_i > 0)
                                        {
                                            t_len = t_i - t_pos - 1;
                                            if (t_len > 0)
                                                tags.Add(t_pre.Substring(t_pos + 1, t_len));
                                            t_pos = t_i;
                                            t_i = t_pre.IndexOf(CONTEXT_PREDICATE_TAG, t_pos + 1);
                                        }

                                        if (tags.Count > 0)
                                        {
                                            for (int i = 0; i < tags.Count; i++)
                                                res_pred += tags[i].Length.ToString("D4") + tags[i].ToLower();

                                            add_cmd(CMD_WHERE_TAG, tags.Count.ToString("D4"), res_pred);     
                                        }
                                        pos += predicate.Length;
                                    }
                                    else if (get_number(predicate, ref idx)) // Check if numeric
                                    { // Number (index)
                                        if (idx < 1)
                                            error = "Error: invalid index at position " + (pos + pos_shift).ToString() + " (shall be >= 1)";
                                        else
                                        {
                                            add_cmd(CMD_WHERE_NODE, NODE_INDEX, (idx - 1).ToString());    
                                            pos += predicate.Length;
                                        }
                                    }
                                    else if (predicate == CONTEXT_PREDICATE_FIRST)
                                    { // first()
                                        add_cmd(CMD_WHERE_NODE, NODE_FIRST);
                                        pos += predicate.Length;
                                    }
                                    else if (predicate == CONTEXT_PREDICATE_LAST)
                                    { // last()
                                        add_cmd(CMD_WHERE_NODE, NODE_LAST);
                                        pos += predicate.Length;
                                    }
                                    else
                                    { // Get predicate name
                                        //int predicate_attribute = 0;
                                        int predicate_attribute = (predicate.Substring(0, 1) == CONTEXT_ATTRIBUTE) ? 1 : 0;
                                        string predicate_name = get_name(predicate, predicate_attribute, "*?");
                                        if (predicate_name == "")
                                        {
                                            error = "Error: invalid name at position " + (pos + pos_shift).ToString();
                                            break;
                                        }

                                        if (predicate.Length == (predicate_name.Length + predicate_attribute))
                                        {
                                            if (predicate_attribute == 1)
                                                add_cmd(CMD_WHERE_ATTRIBUTE, predicate_name, PREDICATE_OP_EX);   // Exists
                                            else
                                                add_cmd(CMD_WHERE_NODE, predicate_name, PREDICATE_OP_EX);        // Exists
                                            pos += predicate.Length;
                                        }
                                        else
                                        {   // Parse operation
                                            int cmp_pos = predicate_name.Length + predicate_attribute;                       // Where sign should be
                                            
                                            // 
                                            while ((predicate.Substring(cmp_pos, 1) == " ") & (cmp_pos < predicate.Length))
                                                cmp_pos++;

                                            string op = "";
                                            string prl = predicate + "     ";       // To avoid length errors in case of invalid syntax and brackets in the expression
                                            if (prl.Substring(cmp_pos, PREDICATE_OP_NS_CHECK.Length) == PREDICATE_OP_NS_CHECK)
                                            {
                                                op = PREDICATE_OP_NS;
                                                cmp_pos += PREDICATE_OP_NS_CHECK.Length;
                                            }
                                            else if (prl.Substring(cmp_pos, PREDICATE_OP_ES_CHECK.Length) == PREDICATE_OP_ES_CHECK)
                                            {
                                                op = PREDICATE_OP_ES;
                                                cmp_pos += PREDICATE_OP_ES_CHECK.Length;
                                            }
                                            else if (prl.Substring(cmp_pos, PREDICATE_OP_NE_CHECK.Length) == PREDICATE_OP_NE_CHECK)
                                            {
                                                op = PREDICATE_OP_NE;
                                                cmp_pos += PREDICATE_OP_NE_CHECK.Length;
                                            }
                                            else if (prl.Substring(cmp_pos, PREDICATE_OP_LE_CHECK.Length) == PREDICATE_OP_LE_CHECK)
                                            {
                                                op = PREDICATE_OP_LE;
                                                cmp_pos += PREDICATE_OP_LE_CHECK.Length;
                                            }
                                            else if (prl.Substring(cmp_pos, PREDICATE_OP_GE_CHECK.Length) == PREDICATE_OP_GE_CHECK)
                                            {
                                                op = PREDICATE_OP_GE;
                                                cmp_pos += PREDICATE_OP_GE_CHECK.Length;
                                            }
                                            else if (prl.Substring(cmp_pos, PREDICATE_OP_EQ_CHECK.Length) == PREDICATE_OP_EQ_CHECK)
                                            {
                                                op = PREDICATE_OP_EQ;
                                                cmp_pos += PREDICATE_OP_EQ_CHECK.Length;
                                            }
                                            else if (prl.Substring(cmp_pos, PREDICATE_OP_GT_CHECK.Length) == PREDICATE_OP_GT_CHECK)
                                            {
                                                op = PREDICATE_OP_GT;
                                                cmp_pos += PREDICATE_OP_GT_CHECK.Length;
                                            }
                                            else if (prl.Substring(cmp_pos, PREDICATE_OP_LT_CHECK.Length) == PREDICATE_OP_LT_CHECK)
                                            {
                                                op = PREDICATE_OP_LT;
                                                cmp_pos += PREDICATE_OP_LT_CHECK.Length;
                                            }
                                            else
                                                error = "Error: invalid comparison type at position " + (pos + pos_shift + predicate_name.Length + 2).ToString();

                                            if (op != "")
                                            { // Parse value
                                                if (cmp_pos == predicate.Length)
                                                {
                                                    error = "Error: missing operand at position " + (pos + pos_shift + cmp_pos).ToString();
                                                    break;
                                                }

                                                // Shift spaces
                                                while ((predicate.Substring(cmp_pos, 1) == " ") & (cmp_pos < predicate.Length))
                                                    cmp_pos++;

                                                string sym = predicate.Substring(cmp_pos, 1);
                                                if ((sym == "'") | (sym == @""""))
                                                { // String value
                                                    if ((op != PREDICATE_OP_EQ) & (op != PREDICATE_OP_NE) & (op != PREDICATE_OP_NS) & (op != PREDICATE_OP_ES))
                                                    {
                                                        error = "Error: invalid operation for string value at position " + (pos + pos_shift).ToString();
                                                        break;
                                                    }
                                                    string s = "";
                                                    int rc = get_string(inpath.Substring(pos + cmp_pos, inpath.Length - pos - cmp_pos), ref s);

                                                    if (rc < 0)
                                                    {
                                                        error = "Invalid value at position " + (pos + pos_shift + cmp_pos).ToString();
                                                        break;
                                                    }
                                                    add_cmd((predicate_attribute == 1) ? CMD_WHERE_ATTRIBUTE : CMD_WHERE_NODE, predicate_name, op + PREDICATE_TYPE_STRING + s);
                                                    pos += (cmp_pos + rc + 2);
                                                }
                                                else
                                                { // Numeric value
                                                    if ((op == PREDICATE_OP_NS) | (op == PREDICATE_OP_ES))
                                                    {
                                                        error = "Error: invalid operation for numeric value at position " + (pos + pos_shift).ToString();
                                                        break;
                                                    }
                                                    
                                                    string s = predicate.Substring(cmp_pos, (predicate.Length - cmp_pos));
                                                    decimal d = 0;
                                                    if (!get_decimal(s, ref d))
                                                    {
                                                        error = "Error: invalid numeric value at position " + (pos + pos_shift + cmp_pos).ToString();
                                                        break;
                                                    }
                                                    add_cmd((predicate_attribute == 1) ? CMD_WHERE_ATTRIBUTE : CMD_WHERE_NODE, predicate_name, op + PREDICATE_TYPE_NUMERIC + d.ToString());
                                                    pos += predicate.Length;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        if ((LEVEL != 0) & (error == ""))
                            error = "Error: missing closing ']' at position " + (pos + pos_shift).ToString();
                        else if ((error == "") & (COND | PRED))
                            error = "Error: invalid condition level at position " + (pos + pos_shift).ToString();
                    }
                    if (error == "")
                        add_cmd(CMD_WHERE_END);
                }
                // If not predicate then shall be qname
                else 
                {
                    string qn = get_name(inpath, pos, "*?");

                    if (qn == "")
                        error = "Error: invalid expression at position " + (pos + pos_shift).ToString();
                    else
                    {
                        if (pos == 0)
                        {
                            context_defined = true;
                            context_operation = CONTEXT_CURRENT;
                            add_cmd(CMD_SET_SEARCH, SEARCH_NODE);


                            //inpath = CONTEXT_CURRENT + inpath;                  // Insert "current" and repeat if xpath is started by name
                            //pos_shift -= CONTEXT_CURRENT.Length;
                        }
                        //else
                        //{
                        if (context_operation == "")
                        {
                            error = "Error: missing context operation at position " + (pos + pos_shift).ToString();
                        }
                        else
                        {
                            name_defined = true;
                            context_operation = CMD_SELECT;
                            add_cmd(context_operation, SELECT_NODE, qn);
                        }
                        context_defined = true;
                        context_operation = "";
                        pos += qn.Length;
                        //}
                    }
                }

                //////////////////////////////////////////////////////////////////////////
                ///////////   End condition definition ///////////////////////////////////
                //////////////////////////////////////////////////////////////////////////
            }
            //////////////////////////////////////////////////////////////////////////
            ///////////   Post-processing    /////////////////////////////////////////
            //////////////////////////////////////////////////////////////////////////
            if ((error == "") & (!context_defined))
                error = "Error: search context is not defined";

            if (error != "")
            {
                XQL.Clear();
                add_cmd(CMD_ERROR, ERROR_PARSE, error);
            }

//#if DEBUG
//            TIMER.END("pars");
//#endif

            return error;
        }

        /// <summary>
        /// Execute previously generated XQL and return one or set of nodes
        /// </summary>
        /// <param name="node"></param>
        /// <param name="single_node"></param>
        /// <returns></returns>
        public VXmlNodeCollection ExecuteXQL(bool single_node)
        {
            VXmlNodeCollection ret = new VXmlNodeCollection(NODE, null);              // Node collection to return

            BLACK_LIST = new List<long>(); 

            VXmlNodeCollection old_node_list = new VXmlNodeCollection(NODE, null);
            VXmlNodeCollection new_node_list = new VXmlNodeCollection(NODE, null);
            new_node_list.Add(NODE.Id);

            //if (NODE_TYPE == DEFX.NODE_TYPE_CATALOG)
            //    BLACK_LIST.Add(NODE.Id);


            int select_cmd = -1;

            int n_item = 0;
            while (n_item < XQL.Count)
            {
                int where_begin = -1;                                           // BEGIN WHERE
                int where_end = -1;                                             // END_WHERE

                if (XQL[n_item].command == CMD_SET_SEARCH)
                    SEARCH_MODE = XQL[n_item].operand;                          // SET SEARCH NODE/TREE
                else if (XQL[n_item].command == CMD_SET_REFERENCE)
                    RESULT_REFERENCE = XQL[n_item].operand;                     // SET REFERENCE ON/OF
                else if (XQL[n_item].command == CMD_SELECT_TYPE)
                    NODE_SELECT_TYPE = XQL[n_item].operand;                     // SET SELECT TYPE
                else if (XQL[n_item].command == CMD_SELECT)
                { // SELECT
                    select_cmd = n_item;
                    old_node_list = new_node_list;                              // Revert old list to new
                    new_node_list = new VXmlNodeCollection(NODE, null);               // Create new empty list
                    if (XQL[select_cmd].operand == SELECT_NODE)
                    { // SELECT NODE
                        // 1. Check if some predefined values
                        if (XQL[select_cmd].value == SELECT_NODE_CURRENT)
                        {
                            new_node_list.Add(NODE.Id);                         // CURRENT - add current node (that requested query)
                            ret.Add(NODE.Id);
                        }
                        else if (XQL[select_cmd].value == SELECT_NODE_PARENT)
                        {                                                       // ADD PARENT: (Parent=null)or(Root element) - add current; Otherwise - add Parent
                            long id = ((NODE.ParentNode == null) | ((NODE_TYPE == DEFX.NODE_TYPE_ELEMENT) & (NODE.Id == NODE.OwnerDocument.DocumentElement.Id))) ? NODE.Id : NODE.ParentNode.Id;
                            new_node_list.Add(id);
                        }
                        else if (XQL[select_cmd].value == SELECT_NODE_ROOT)
                        { // SELECT ROOT
                            long id = (NODE_TYPE == DEFX.NODE_TYPE_CATALOG) ? NODE.Owner.Id : NODE.OwnerDocument.DocumentElement.Id;
                            new_node_list.Add(id);
                        }
                        else
                        { // Filtering nodes
                            // Find where range
                            where_begin = -1;
                            where_end = -1;
                            if (XQL.Count > (n_item + 1))
                            {
                                if (XQL[n_item + 1].command == CMD_WHERE_BEGIN)
                                {
                                    where_begin = n_item + 1;
                                    for (int i = where_begin; i < XQL.Count; i++)
                                    {
                                        if (XQL[i].command == CMD_WHERE_END)
                                            where_end = i;
                                        else if (!is_predicate_command(XQL[i].command))
                                            break;
                                    }
                                }
                            }
                            for (int icn = 0; icn < old_node_list.Count; icn++)
                            {
                                BLACK_LIST.Clear();
                                VXmlNodeCollection tmpc = get_node_list(0, old_node_list[icn], single_node, SEARCH_MODE, XQL[select_cmd].value, where_begin, where_end);           // Search nodes

                                for (int nc = 0; nc < tmpc.Count; nc++)
                                    new_node_list.Add(tmpc.GetNodeId(nc));
                            }
                        }
                    }
                    else
                        throw new VXmlException(VXmlException.E0016_XQL_ERROR_CODE, " unknown operand: " + XQL[select_cmd].operand);
                }
                else
                    throw new VXmlException(VXmlException.E0016_XQL_ERROR_CODE, " unknown command: " + XQL[select_cmd].command);

                n_item = (where_end > 0) ? where_end + 1 : n_item + 1;
            }

            // Compile final result set

            if (NODE_SELECT_TYPE == SELECT_TYPE_ELEMENT)
                ret = new_node_list;
            else
            {
                int cnt = single_node ? Math.Min(1, new_node_list.Count) : new_node_list.Count;

                for (int i = 0; i < cnt; i++)
                {
                    VXmlNode nc = new_node_list[i];
                    short node_type = nc.NodeTypeCode;
                    long add_id = 0;

                    if (NODE_SELECT_TYPE == SELECT_TYPE_ELEMENT)                    // Root node is element , just add node 
                    {
                        if (node_type == DEFX.NODE_TYPE_ELEMENT)
                            add_id = nc.Id;
                    }
                    else if (nc.NodeTypeCode == DEFX.NODE_TYPE_REFERENCE)           // Root node is reference, add depending on NODE_SELECT_TYPE
                    {
                        short ref_type = ((VXmlReference)nc).ReferenceNode.NodeTypeCode;
                        if (((NODE_SELECT_TYPE == SELECT_TYPE_CATALOG) & (ref_type == DEFX.NODE_TYPE_CATALOG)) |
                           ((NODE_SELECT_TYPE == SELECT_TYPE_DOCUMENT) & (ref_type == DEFX.NODE_TYPE_DOCUMENT)))
                            ret.Add(nc.Id);
                    }
                    else
                    {
                        if (node_type == DEFX.NODE_TYPE_CATALOG)           // Select catalog
                        {
                            if (NODE_SELECT_TYPE == SELECT_TYPE_CATALOG)
                                add_id = nc.Id;
                        }
                        else if (node_type == DEFX.NODE_TYPE_DOCUMENT)     // Select document
                        {
                            if (NODE_SELECT_TYPE == SELECT_TYPE_DOCUMENT)
                                add_id = nc.Id;
                        }
                        else if (node_type == DEFX.NODE_TYPE_ELEMENT)
                        {
                            if (NODE_SELECT_TYPE == SELECT_TYPE_DOCUMENT)
                                add_id = nc.OwnerDocument.Id;
                        }
                    }

                    if (add_id > 0)
                    {
                        // Chceck if id is already in the list
                        for (int j = 0; j < ret.Count; j++)
                        {
                            if (ret.GetNodeId(j) == add_id)
                            {
                                add_id = 0;
                                break;
                            }
                        }
                        if (add_id > 0)
                            ret.Add(add_id);
                    }
                }
            }
//#if DEBUG
//            TIMER.END("exec");
//            TIMER.PRINT();
//#endif
            return ret;
        }

        /// <summary>
        /// Recursively search nodes
        /// </summary>
        /// <param name="n"></param>
        /// <param name="where_begin"></param>
        /// <param name="where_end"></param>
        /// <returns></returns>
        private VXmlNodeCollection get_node_list(int level, VXmlNode node, bool single_node, string mode, string name, int where_begin, int where_end)
        {
            //#if DEBUG
            //            TIMER.START("getn");
            //#endif

            VXmlNodeCollection ret_nodes = new VXmlNodeCollection(NODE, null);            // Temp array for nodes found on this level

            if (name == CONTEXT_CONTENT)          // If content
            {
                VXmlNodeCollection vcont = node.ContentNodes;
                for (int i = 0; i < vcont.Count; i++)
                    ret_nodes.Add(vcont[i].Id);
            }
            else
            {
                VXmlNodeCollection search_nodes = node.get_child_nodes_of_type(DEFX.NODE_TYPES_XQL);            // Child nodes for XQL
                VXmlNode n = null;
                VXmlReference refn = null;
                int node_index = 0;
                ///////////////// Begin nodes cycle /////////////////
                for (int ix = 0; ix < search_nodes.Count; ix++)
                {
                    //#if DEBUG
                    //                    TIMER.START("get0");
                    //#endif
                    //n = search_nodes[inode];
                    n = search_nodes[ix];
                    if (n.NodeTypeCode == DEFX.NODE_TYPE_REFERENCE)
                    {
                        refn = (VXmlReference)n;
                        n = refn.ReferenceNode;
                    }
                    else
                        refn = null;

                    short ntype = n.NodeTypeCode;

                    // Define preliminary condition
                    // Ignore node if(OR):
                    // 1. SELECT_TYPE_CATALOG & (NodeType != DEFX.NODE_TYPE_CATALOG)
                    // 2. SELECT_TYPE_DOCUMENT & SEARCH_SUBTREE & (NodeType == DEFX.NODE_TYPE_ELEMENT)

                    bool cond = (((NODE_SELECT_TYPE == SELECT_TYPE_CATALOG) & (ntype != DEFX.NODE_TYPE_CATALOG)) |
                                ((NODE_SELECT_TYPE == SELECT_TYPE_DOCUMENT) & (SEARCH_MODE == SEARCH_SUBTREE) & (ntype == DEFX.NODE_TYPE_ELEMENT)));
                    if (!cond)
                    {
                        bool inBlackList = (ntype == DEFX.NODE_TYPE_ELEMENT) ? false : node_is_in_black_list(n.Id);
                        if (!inBlackList)
                        {

                            VXmlAttributeCollection attrs = null;
                            VXmlNodeCollection nodes = null;
                            VXmlTagCollection tags = null;

                            bool add_node = false;

                            if (VSLib.Compare(name, n.Name))
                            {
                                if (where_begin < 0)
                                {
                                    long add_id = n.Id;
                                    if ((RESULT_REFERENCE == REFERENCE_ON) & (refn != null))
                                        add_id = refn.Id;

                                    ret_nodes.Add(add_id);

                                    if (SEARCH_MODE == SEARCH_SUBTREE)
                                        if ((ntype == DEFX.NODE_TYPE_CATALOG)) // | (ntype == DEFX.NODE_TYPE_DOCUMENT))
                                            BLACK_LIST.Add(n.Id);

                                    if ((NODE_SELECT_TYPE == SELECT_TYPE_CATALOG) & (ntype != DEFX.NODE_TYPE_CATALOG))
                                        break;
                                }
                                else
                                {
                                    for (int i = where_begin; i < where_end; i++)
                                    {
                                        if ((XQL[i].command == CMD_WHERE_ATTRIBUTE) & (attrs == null))
                                        {
                                            attrs = (n.NodeTypeCode == DEFX.NODE_TYPE_DOCUMENT) ? ((VXmlDocument)n).Attributes : n.Attributes;
                                            //attrs = n.Attributes;
                                        }

                                        if ((XQL[i].command == CMD_WHERE_TAG) & (tags == null))
                                            tags = n.TagNodes;

                                        if ((XQL[i].command == CMD_WHERE_NODE) & (nodes == null))
                                            nodes = n.ChildNodes;

                                        if ((nodes != null) & (attrs != null) & (tags != null))
                                            break;
                                    }
                                    ////////////////////////////////////////////////////////////////////////
                                    ///// Check WHERE predicates. If at least 1 is false - ignore node /////
                                    ////////////////////////////////////////////////////////////////////////
                                    List<bool> result = new List<bool>(32);
                                    List<string> condition = new List<string>(32);
                                    int where_level = -1;
                                    add_node = false;
                                    for (int i = where_begin; i <= where_end; i++)
                                    {
                                        ///////////////// Begin conditions cycle /////////////////
                                        if (XQL[i].command == CMD_WHERE_BEGIN)
                                        { //////////// WHERE_BEGIN
                                            result.Add(false);
                                            condition.Add(PREDICATE_COND_NA);
                                            where_level++;
                                        }
                                        else if (XQL[i].command == CMD_WHERE_END)
                                        { //////////// WHERE_END
                                            if (where_level == 0)
                                            {
                                                add_node = result[0];
                                                break;
                                            }

                                            if (condition[where_level - 1] == PREDICATE_COND_NA)
                                            {
                                                result[where_level - 1] = result[where_level];
                                                condition[where_level - 1] = PREDICATE_COND_AND;        // Set '&' by default
                                            }
                                            else if (condition[where_level - 1] == PREDICATE_COND_AND)
                                                result[where_level - 1] = result[where_level - 1] & result[where_level];
                                            else
                                                result[where_level - 1] = result[where_level - 1] | result[where_level];
                                            where_level--;
                                            result.RemoveAt(result.Count - 1);
                                            condition.RemoveAt(condition.Count - 1);
                                        }
                                        else if (XQL[i].command == CMD_WHERE_CONDITION)
                                        { //////////// WHERE_CONDITION
                                            condition[where_level] = XQL[i].operand;
                                        }
                                        else if (XQL[i].command == CMD_WHERE_ATTRIBUTE)
                                        { //////////// WHERE_ATTRIBUTE
                                            if (attrs != null)
                                            {
                                                for (int j = 0; j < attrs.Count; j++)
                                                {
                                                    result[where_level] = compare_node_value(i, attrs[j].Name, attrs[j].Value);
                                                    if (result[where_level])
                                                        break;
                                                }
                                            }
                                        }
                                        else if (XQL[i].command == CMD_WHERE_TAG)
                                        { //////////// WHERE_TAG. At leas 1 tag exists
                                            result[where_level] = false;
                                            if (tags != null)
                                            {
                                                // Decode tag values to compare
                                                int n_tags = VSLib.ConvertStringToInt(XQL[i].operand);      // The number of tags in condition
                                                string[] c_tags = new string[n_tags];
                                                int tag_pos = 0;
                                                for (int it = 0; it < n_tags; it++)
                                                {
                                                    int tag_len = VSLib.ConvertStringToInt(XQL[i].value.Substring(tag_pos, 4));
                                                    c_tags[it] = XQL[i].value.Substring(tag_pos + 4, tag_len);
                                                    tag_pos += (tag_len + 4);
                                                }

                                                for (int j = 0; j < tags.Count; j++)
                                                {
                                                    string st = tags[j].Value.ToLower();
                                                    for (int jt = 0; jt < n_tags; jt++)
                                                    {
                                                        if (VSLib.Compare(c_tags[jt], st))
                                                        {
                                                            result[where_level] = true;
                                                            break;
                                                        }
                                                    }

                                                    if (result[where_level])
                                                        break;
                                                }
                                            }
                                        }
                                        else if (XQL[i].command == CMD_WHERE_NODE)
                                        { //////////// WHERE_NODE
                                            if (XQL[i].operand == NODE_FIRST)
                                            {
                                                if (node_index == 0)
                                                    result[where_level] = true;
                                            }
                                            else if (XQL[i].operand == NODE_LAST)
                                            {
                                                if (node_index == (search_nodes.Count - 1))
                                                    result[where_level] = true;
                                            }
                                            else if (XQL[i].operand == NODE_INDEX)
                                            {
                                                int idx = Convert.ToInt32(XQL[i].value);
                                                if (node_index == idx)
                                                    result[where_level] = true;
                                            }
                                            else
                                            { // Else name is specified
                                                if (nodes != null)
                                                {
                                                    for (int icn = 0; icn < nodes.Count; icn++)
                                                    {
                                                        VXmlNode c_node = nodes[icn];
                                                        result[where_level] = compare_node_value(i, c_node.Name, c_node.Value);
                                                        if (result[where_level])
                                                            break;
                                                    }
                                                }
                                            }
                                        }
                                        else
                                            throw new VXmlException(VXmlException.E0016_XQL_ERROR_CODE, VXmlException.GetMessage(VXmlException.E0016_XQL_ERROR_CODE) + " unidentified WHERE command: " + XQL[i].command);
                                        ///////////////// End conditions cycle /////////////////
                                    }
                                    if (add_node)
                                    {
                                        long add_id = ((RESULT_REFERENCE == REFERENCE_ON) & (refn != null)) ? refn.Id : n.Id;

                                        ret_nodes.Add(add_id);

                                        if (SEARCH_MODE == SEARCH_SUBTREE)
                                            if (ntype == DEFX.NODE_TYPE_CATALOG)  // | (ntype == DEFX.NODE_TYPE_DOCUMENT))
                                                BLACK_LIST.Add(n.Id);

                                        if ((NODE_SELECT_TYPE == SELECT_TYPE_CATALOG) & (ntype != DEFX.NODE_TYPE_CATALOG))
                                            break;
                                    }
                                }
                            }

                            if (mode == SEARCH_SUBTREE)
                            {
                                VXmlNodeCollection tmpc = get_node_list(level + 1, n, single_node, mode, name, where_begin, where_end);
                                for (int nc = 0; nc < tmpc.Count; nc++)
                                {
                                    if (!((NODE_SELECT_TYPE == SELECT_TYPE_DOCUMENT) & (tmpc[nc].NodeTypeCode == DEFX.NODE_TYPE_ELEMENT) & SEARCH_MODE == SEARCH_SUBTREE))
                                        ret_nodes.Add(tmpc.GetNodeId(nc));
                                }
                            }
                            ///////////////// End nodes cycle /////////////////
                        }
                    }
                    node_index++;
                    //#if DEBUG
                    //                    TIMER.END("get0");
                    //#endif
                }
            }
            //#if DEBUG
            //            TIMER.END("getn");
            //#endif
            return ret_nodes;
        }

        /// <summary>
        /// Returns true if it is predicate command (WHERE)
        /// </summary>
        /// <param name="cmd"></param>
        /// <returns></returns>
        private bool is_predicate_command(string cmd)
        {
            return (cmd.Substring(0, 7) == CMD_WHERE_BEGIN.Substring(0, 7));
        }

        /// <summary>
        /// Check if node is un the black list
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private bool node_is_in_black_list(long id)
        {
            for (int i = 0; i < BLACK_LIST.Count; i++)
                if (id == BLACK_LIST[i])
                    return true;
            return false;
        }


        /// <summary>
        /// Compare node/attribute value
        /// </summary>
        /// <param name="x_index">XQL index</param>
        /// <param name="node">Node</param>
        /// <returns></returns>
        private bool compare_node_value(int x_index, string name, string value)
        {
//#if DEBUG
//            TIMER.START("comp");
//#endif
            bool ret = true;
            if (VSLib.Compare(XQL[x_index].operand, name))
            {
                string o = (XQL[x_index].value.Substring(0, PREDICATE_OP_LENGTH));
                if (o == PREDICATE_OP_EX)       // If 'exists' condition
                    ret = true;
                else
                {
                    string t = (XQL[x_index].value.Substring(PREDICATE_OP_LENGTH, PREDICATE_TYPE_LENGTH));        // Type
                    string v = XQL[x_index].value.Remove(0, PREDICATE_OP_LENGTH + PREDICATE_TYPE_LENGTH);         // Value
                    if (t == PREDICATE_TYPE_STRING)
                    {
                        bool eq = false;
                        if ((o == PREDICATE_OP_ES) | (o == PREDICATE_OP_NS))
                            eq = VSLib.Compare(v.ToLower(), value.ToLower());
                        else
                            eq = VSLib.Compare(v, value);

                        if ((eq & ((o == PREDICATE_OP_EQ) | (o == PREDICATE_OP_ES))) | ((!eq) & ((o == PREDICATE_OP_NE) | (o == PREDICATE_OP_NS))))
                            ret = true;
                        else
                            ret = false;
                    }
                    else if (t == PREDICATE_TYPE_NUMERIC)
                    {
                        decimal vd = Convert.ToDecimal(v);
                        decimal va;
                        try
                        {
                            va = Convert.ToDecimal(value);
                        }
                        catch (Exception e)
                        {
//#if DEBUG
//                            TIMER.END("comp");
//#endif
                            ERROR = e.Message;
                            return false;
                        }


                        if ((o == PREDICATE_OP_EQ) & (va == vd))
                            ret = true;
                        else if ((o == PREDICATE_OP_NE) & (va != vd))
                            ret = true;
                        else if ((o == PREDICATE_OP_GE) & (va >= vd))
                            ret = true;
                        else if ((o == PREDICATE_OP_GT) & (va > vd))
                            ret = true;
                        else if ((o == PREDICATE_OP_LT) & (va < vd))
                            ret = true;
                        else if ((o == PREDICATE_OP_LE) & (va <= vd))
                            ret = true;
                        else
                            ret = false;
                    }
                    else
                        throw new VXmlException(VXmlException.E0016_XQL_ERROR_CODE, "unsupported type: " + t);
                }
            }
            else
                ret = false;
//#if DEBUG
//            TIMER.END("comp");
//#endif
            return ret;
        }


        /// <summary>
        /// Add commang to array
        /// </summary>
        /// <param name="command"></param>
        /// <param name="operand"></param>
        /// <returns></returns>
        private XPATH_CMD add_cmd(string command, string operand = "", string value = "")
        {
            XPATH_CMD c = new XPATH_CMD();
            c.command = command;
            c.operand = operand;
            c.value = value;
            XQL.Add(c);
            return c;
        }

        /// <summary>
        /// Get node/attribute name
        /// </summary>
        /// <param name="str"></param>
        /// <param name="ps"></param>
        /// <returns></returns>
        private string get_name(string str, int ps, string supp_pattern = "")
        {
            string nm = "";
            int p = ps;
            string start_pattern = "0123456789qwertyuiopasdfghjklzxcvbnmQWERTYUIOPASDFGHJKLZXCVBNM_" + supp_pattern;
            string non_start_pattern = start_pattern + "-.:";

            while (p < str.Length)
            {
                string s = str.Substring(p, 1);
                string pattern = (p == ps) ? start_pattern : non_start_pattern;
                if (pattern.IndexOf(s) < 0)
                    break;
                nm += s;
                p++;
            }
            return nm;
        }

        /// <summary>
        /// Return numeric value or -1
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        private bool get_number(string s, ref long d)
        {
            bool rc = true;
            try
            {
                d = Convert.ToInt64(s);
            }
            catch (Exception e)
            {
                ERROR = e.Message;
                rc = false;
            }
            return rc;
        }

        /// <summary>
        /// Return decimal value
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        private bool get_decimal(string s, ref decimal d)
        {
            bool rc = true;
            try
            {
                d = Convert.ToDecimal(s);
            }
            catch (Exception e)
            {
                ERROR = e.Message;
                rc = false;
            }
            return rc;
        }


        /// <summary>
        /// Return string value
        /// Could be embraced by ' or "
        /// Embracing symbol inside shall be doubled
        /// </summary>
        /// <param name="st">Value string including starting and ending ' or " symbols</param>
        /// <param name="v"></param>
        /// <returns>"" - length of string including '/", or -1 if error</returns>
        private static int get_string(string st, ref string v)
        {
            int l = 0;
            v = "";
            if (st.Length == 0)
                return -1;
            string sym = st.Substring(0, 1);

            for (int pos = 1; pos < st.Length; pos++)
            {
                string ch = st.Substring(pos, 1);
                if (ch == sym)
                {
                    if ((pos + 1) == st.Length)
                    {
                        return l;
                    }
                    else
                    {
                        if (st.Substring(pos + 1, 1) == sym)
                        {
                            l += 2;
                            pos += 1;
                            v += sym;
                        }
                        else
                            return l;
                    }
                }
                else
                {
                    v += ch;
                    l++;
                }
            }
            return -1;
        }
    }
}
