using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using VStorage;

namespace VXML
{
    /////////////////////////////////////////////////////////////
    /////////////////////// VXmlParser //////////////////////////
    /////////////////////////////////////////////////////////////
    public static class VXmlParser
    {
        public struct VXMLINT
        {
            public string def;              // Definition start/end 
            public short type;                // Node type: DEFX.NODE_TYPE_XXXXXXX
            public string name;             // Definition name
            public string value;            // Definition value
            public int index;
        }

        // Definition
        public const string DEF_START       = "##start##";
        public const string DEF_END         = "##end##";
        public const string DEF_ATTRIBUTE   = "##attribute##";
        public const string DEF_COMMENT     = "##comment##";
        public const string DEF_TEXT        = "##text##";
        public const string DEF_INSTRUCTION = "##instruction##";
        public const string DEF_ERROR       = "##error##";
        public const string DEF_DUMMY       = "##dummy##";
        // Operands

        /// <summary>
        /// Context definition
        /// </summary>
        public const string CONTEXT_NODE_START      = "<";
        public const string CONTEXT_NODE_END        = ">";
        public const string CONTEXT_NODE_END_SHORT  = "/>";
        public const string CONTEXT_NODE_END_LONG = "/";

        public const string CONTEXT_INSTRUCTION_START = "?";
        public const string CONTEXT_INSTRUCTION_END = "?>";

        public const string CONTEXT_COMMENT_START = "!--";
        public const string CONTEXT_COMMENT_END = "-->";

        /// <summary>
        /// Parse from file 
        /// </summary>
        /// <param name="file"></param>
        public static VXMLTemplate ParseFromFile(string file, string name)
        {
            string s = "";
            try
            {
                VSIO IO = new VSIO(file, VSIO.FILE_MODE_OPEN, "");
                s = IO.ReadString((long)0, (int)IO.GetLength());
                IO.Close();
            }
            catch (Exception e)
            {
                VXMLTemplate v = new VXMLTemplate();
                v.Add(DEF_ERROR, DEFX.NODE_TYPE_UNDEFINED, e.Message, e.Message);
                return v;
            }
            return Parse(s, name);
        }

        /// <summary>
        /// Parse from string
        /// </summary>
        /// <param name="xmlstring"></param>
        public static VXMLTemplate Parse(string xp, string name)
        {
            VXMLTemplate t = new VXMLTemplate(name);
            VXMLTemplate stack = new VXMLTemplate("stack");
            List<int> pages = new List<int>(1024);

            stack.Add(DEF_DUMMY, DEFX.NODE_TYPE_UNDEFINED); // Add 1st (dummy) recor to the stack

            pages.Add(0);
            int sf = 0;
            while (sf < (xp.Length - 3))
            {
                if (xp.Substring(sf, DEFS.DELIM_NEWLINE.Length) == DEFS.DELIM_NEWLINE)
                {
                    sf += 2;
                    pages.Add(sf);
                }
                else
                    sf++;
            }

            string error = "";
            int pos = 0;

            if (xp.Length == 0)
                return t;
            string s = "";
            string ns = "";
            string x = xp + "     ";

            while ((pos < x.Length) & (error == ""))
            {
                /////////////////////////// Node tag ///////////////////////////
                if (x.Substring(pos, 1) == "<")
                {
                    pos++;
                    // <!--
                    if (x.Substring(pos, CONTEXT_COMMENT_START.Length) == CONTEXT_COMMENT_START)
                    {
                        pos += CONTEXT_COMMENT_START.Length;
                        ns = get_value(ref x, pos, CONTEXT_COMMENT_END);
                        if (ns != DEF_ERROR)
                        {
                            t.Add(DEF_COMMENT, DEFX.NODE_TYPE_COMMENT, DEFX.GET_NODETYPE(DEFX.NODE_TYPE_COMMENT), ns.Trim());
                            pos += ns.Length + CONTEXT_COMMENT_END.Length;
                            ns = "";
                        }
                        else
                            error = "Missing/invalid comment closing tag" + get_pos(ref pages, ref pos);
                    }
                    // <?
                    else if (x.Substring(pos, CONTEXT_INSTRUCTION_START.Length) == CONTEXT_INSTRUCTION_START)
                    {
                        pos += CONTEXT_INSTRUCTION_START.Length;
                        ns = get_value(ref x, pos, CONTEXT_INSTRUCTION_END);
                        if (ns != DEF_ERROR)
                        {
                            t.Add(DEF_INSTRUCTION, DEFX.NODE_TYPE_INSTRUCTION, DEFX.GET_NODETYPE(DEFX.NODE_TYPE_INSTRUCTION), ns.Trim());
                            pos += ns.Length + CONTEXT_INSTRUCTION_END.Length;
                            ns = "";
                        }
                        else
                            error = "Missing/invalid instruction closing tag" + get_pos(ref pages, ref pos);
                    }
                    // '</' - closing node tag
                    else if (x.Substring(pos, CONTEXT_NODE_END_LONG.Length) == CONTEXT_NODE_END_LONG)
                    {
                        pos += CONTEXT_NODE_END_LONG.Length;
                        int i = x.IndexOf(CONTEXT_NODE_END, pos);           // Find '>'
                        if (i <= pos)
                            error = "Missing/invalid node closing tag" + get_pos(ref pages, ref pos);
                        else
                        {
                            string n_name = x.Substring(pos, i - pos);
                            string val = s;
                            s = "";
                            if (stack[stack.Count - 1].name == n_name)
                            {
                                // Set value
                                while (val.Length > 0)
                                {
                                    string ch = val.Substring(0, 1);
                                    if ((ch == " ") | (ch == "\t") | (ch == "\r") | (ch == "\n"))
                                        val = val.Remove(0, 1);
                                    else
                                        break;
                                }

                                while (val.Length > 0)
                                {
                                    string ch = val.Substring(val.Length - 1, 1);
                                    if ((ch == " ") | (ch == "\t") | (ch == "\r") | (ch == "\n"))
                                        val = val.Remove(val.Length - 1, 1);
                                    else
                                        break;
                                }

                                t.Add(DEF_END, stack[stack.Count - 1].type, n_name);            // Add End node
                                VXMLINT vi = t[stack[stack.Count - 1].index];
                                vi.value = val;
                                t.RemoveAt(stack[stack.Count - 1].index);
                                t.Insert(stack[stack.Count - 1].index, vi);
                                stack.RemoveAt(stack.Count - 1);                                    // Remove node from stack
                                pos = i + 1;
                            }
                            else
                                error = "Closing tag name doesnt match" + get_pos(ref pages, ref pos);
                        }
                    }

                    // <
                    else 
                    {
                        string st_name = get_name(ref x, ref pos, ">", "/>", " ");

                        if (st_name.Length == 0)
                            error = "Missing node name" + get_pos(ref pages, ref pos);
                        else
                        {
                            short node_type = DEFX.NODE_TYPE_ELEMENT;
                            if (st_name == DEFX.GET_NODETYPE(DEFX.NODE_TYPE_TEXT))
                                node_type = DEFX.NODE_TYPE_TEXT;
                            else if (st_name == DEFX.GET_NODETYPE(DEFX.NODE_TYPE_COMMENT))
                                node_type = DEFX.NODE_TYPE_COMMENT;
                            else if (st_name == DEFX.GET_NODETYPE(DEFX.NODE_TYPE_CONTENT))
                                node_type = DEFX.NODE_TYPE_CONTENT;

                            t.Add(DEF_START, node_type, st_name);
                            stack.Add(DEF_START, node_type, st_name, "", (t.Count - 1));

                            // Handle attributes
                            bool eon = false;
                            while ((pos < x.Length) & (error == ""))
                            {
                                if ((x.Substring(pos, 1) == " ") | (x.Substring(pos, 1) == "\t"))
                                    pos++;
                                else if (x.Substring(pos, 2) == DEFS.DELIM_NEWLINE)
                                    pos += 2;
                                // '>'
                                else if (x.Substring(pos, CONTEXT_NODE_END.Length) == CONTEXT_NODE_END)
                                {
                                    pos++;
                                    eon = true; 
                                    break;
                                }
                                // '/>'
                                else if (x.Substring(pos, CONTEXT_NODE_END_SHORT.Length) == CONTEXT_NODE_END_SHORT)
                                {
                                    pos += 2;
                                    eon = true;

                                    t.Add(DEF_END, node_type, st_name);
                                    stack.RemoveAt(stack.Count - 1);             // Remove node from stack
                                    break;
                                }
                                // Process attribute
                                else
                                {
                                    string a_name = get_name(ref x, ref pos, "=");
                                    if (name.Length == 0)
                                        error = "Missing attribute name" + get_pos(ref pages, ref pos);
                                    else
                                    {
                                        if (x.Substring(pos, 2) != @"=""")
                                            error = "Error in attribute definition" + get_pos(ref pages, ref pos);
                                        else
                                        {
                                            pos += 2;
                                            int i = x.IndexOf(@"""", pos);
                                            if (i < 0)
                                                error = @"Missing closing '""' in attribute definition" + get_pos(ref pages, ref pos);
                                            else
                                            {
                                                string v = (i == pos) ? "" : x.Substring(pos, i - pos);
                                                pos = i + 1;
                                                t.Add(DEF_ATTRIBUTE, DEFX.NODE_TYPE_ATTRIBUTE, a_name, v);
                                            }
                                        }
                                    }
                                }
                            }
                            if ((error == "") & (!eon))
                                error = "Missing closing '>'" + get_pos(ref pages, ref pos);
                        }
                    }
                }
                /////////////////////////// Node value ///////////////////////////
                else
                {
                    if (stack.Count == 1)       // NOT node value
                    {
                        if ((x.Substring(pos, 1) == " ") | (x.Substring(pos, 1) == "\t"))
                            pos++;
                        else if (x.Substring(pos, 2) == DEFS.DELIM_NEWLINE)
                            pos += 2;
                        else
                            error = "Unrecognized term" + get_pos(ref pages, ref pos);
                    }
                    else if (stack[stack.Count - 1].def == DEF_START)
                    {
                        string s1 = get_value(ref x, pos, CONTEXT_NODE_START);
                        s += s1;
                        if (s == DEF_ERROR)
                            error = "Missing/invalid node closing tag" + get_pos(ref pages, ref pos);
                        else
                            pos += s1.Length;
                    }
                    else
                        error = "Unrecognized term" + get_pos(ref pages, ref pos);
                }
            }
            // Post-processing
            if (error != "")
            {
                t.Clear();
                
                t.Add(DEF_ERROR, 0, "Parse error", error);
            }
            else if (stack.Count > 1)
            {
                t.Clear();
                t.Add(DEF_ERROR, 0, "Parse error", "No closing tag" + get_pos(ref pages, ref pos));
            }
            return t;
        }

        /// <summary>
        /// Get node name
        /// </summary>
        /// <param name="x"></param>
        /// <param name="pos"></param>
        /// <returns></returns>
        private static string get_name(ref string x, ref int pos, string term1 = "", string term2 = "", string term3 = "", string term4 = "")
        {
            string nm = "";
            int p = pos;
            int xl = x.Length;
            bool tf = false;
            while ((p < x.Length) & (!tf))
            {
                if (term1 != "")
                {
                    if ((pos + term1.Length) < xl)
                        if (x.Substring(pos, term1.Length) == term1)
                            tf = true;
                }
                if ((term2 != "") & (!tf))
                {
                    if ((pos + term2.Length) < xl)
                        if (x.Substring(pos, term2.Length) == term2)
                            tf = true;
                }
                if ((term3 != "") & (!tf))
                {
                    if ((pos + term3.Length) < xl)
                        if (x.Substring(pos, term3.Length) == term3)
                            tf = true;
                }
                if ((term4 != "") & (!tf))
                {
                    if ((pos + term4.Length) < xl)
                        if (x.Substring(pos, term4.Length) == term4)
                            tf = true;
                }
                
                if (!tf)
                {
                    nm += x.Substring(pos, 1);
                    pos++;
                }
            }
            return nm.Trim();
        }

        /// <summary>
        /// Get value up to 'term' string
        /// </summary>
        /// <param name="x"></param>
        /// <param name="pos"></param>
        /// <param name="term"></param>
        /// <returns></returns>
        private static string get_value(ref string x, int pos, string term)
        {
            int cnt = 0;
            int lterm = term.Length;
            while ((pos + cnt) < (x.Length - lterm + 1))
            {
                if (x.Substring(pos + cnt, lterm) == term)
                {
                    if (cnt > 0)
                        return x.Substring(pos, cnt);
                    else
                        return "";
                }
                cnt++;
            }
            return DEF_ERROR;
        }

        /// <summary>
        /// Get position description (line, pos)
        /// </summary>
        /// <param name="pages"></param>
        /// <param name="pos"></param>
        /// <returns></returns>
        private static string get_pos(ref List<int> pages, ref int pos)
        {
            for (int i = 0; i < pages.Count; i++)
                if (pages[i] >= pos)
                {
                    int j = i - 1;
                    if (j < 0)
                        j = 0;
                    return " at line " + (i + 1).ToString() + ", pos " + (pos - pages[j] + 1).ToString();
                }
            return " at line " + pages.Count.ToString() + ", pos " + (pos - pages[pages.Count - 1] + 1).ToString();
        }
    }
}
