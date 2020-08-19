using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VStorage;
using System.IO;

namespace VXML
{
    /////////////////////////////////////////////////////////////
    ////////////////////// VXmlDocument /////////////////////////
    /////////////////////////////////////////////////////////////
    public class VXmlDocument : VXmlNode
    {
        //internal VSpace document_space = null;
        //internal VSpace document_content_space = null;

        private List<VXMLTemplate> template_cache = null;

        /// <summary>
        /// Constructor
        /// </summary>
        public VXmlDocument()
        {
        }

        /// <summary>
        /// Constructor - load existing document
        /// </summary>
        public VXmlDocument(VSpace space, VSpace cont)
        {
            //document_space = space;
            //document_content_space = cont;
            node_space = space;
            content_space = cont;
        }

        /// <summary>
        /// Clone document
        /// </summary>
        /// <returns></returns>
        public VXmlDocument Clone(string new_name)
        {
            string nm = new_name.Trim().ToLower();
            if (nm == "")
                throw new VXmlException(VXmlException.E0018_DOC_NAME_MISSING_CODE, "(Clone)");

            VXmlDocument d = (VXmlDocument) clone_xml_node(this, ParentNode, true);
            d.Name = new_name;
            return d;
        }



        /// <summary>
        /// Load child node(s) from file
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public new string Load(string file, VXmlNode parent = null)
        {
            string s = "";
            string error = "";
            try
            {
                s = File.ReadAllText(file, Encoding.Default);
            }
            catch (Exception e)
            {
                error = e.Message;
            }
            if (error != "")
                return VXmlException.GetMessage(VXmlException.E0025_XML_FILE_ERROR_CODE) + ": " + error;

            //return LoadXml(s, Path.GetFileName(file), parent);
            return load_xml_data(s, Path.GetFileNameWithoutExtension(file), file, parent);
        }

        /// <summary>
        /// Load child node from xml string
        /// </summary>
        /// <param name="xmlstring"></param>
        /// <returns></returns>
        public new string LoadXml(string xmlstring, string name, VXmlNode parent = null)
        {
            return load_xml_data(xmlstring, name, "", parent);
        }


        /// <summary>
        /// Load child node from xml string
        /// </summary>
        /// <param name="xmlstring">XML string to load</param>
        /// <param name="name">Node name to create</param>
        /// <param name="fileref">Reference to file</param>
        /// <param name="parent">Parent VXML node</param>
        /// <returns></returns>
        private string load_xml_data(string xmlstring, string name, string xml_fileref, VXmlNode parent)
        {
            if (parent == null)
                if (this.DocumentElement != null)
                    this.DocumentElement.Remove();

            VXmlNode n = (parent == null) ? this : parent;
            if (!DEFX.BR_XML_IS_VALID_TYPE(n.NodeTypeCode))
                return VXmlException.GetMessage(VXmlException.E0004_INVALID_NODE_TYPE_CODE) + ": " + n.NodeType;

            VXMLTemplate t = null;
            if (template_cache == null)
                template_cache = new List<VXMLTemplate>(32);
            for (int i = 0; i < template_cache.Count; i++)
            {
                if (template_cache[i].template_name == name)
                    t = template_cache[i];
            }
            if (t == null)                                          // Not found in cache
            {
                t = VXmlParser.Parse(xmlstring, name);
                if (t.Count == 1)
                {
                    if (t[0].def == VXmlParser.DEF_ERROR)
                        return VXmlException.GetMessage(VXmlException.E0020_XML_PARSE_ERROR_CODE) + ": " + t[0].value;                          // Return error
                }
            }

            if (t.Count == 0)
                return "";

            int j = 0;
            List<VXmlNode> stack = new List<VXmlNode>();
            stack.Add(n);
            while (j < t.Count)
            {
                int stack_n = stack.Count - 1;

                if (t[j].def == VXmlParser.DEF_INSTRUCTION)
                {
                    // Just ignore
                }
                else if (t[j].def == VXmlParser.DEF_COMMENT)
                {
                    stack[stack_n].CreateComment(t[j].value);
                }
                else if (t[j].def == VXmlParser.DEF_ATTRIBUTE)
                {
                    stack[stack_n].SetAttribute(t[j].name, t[j].value);
                }
                else if (t[j].def == VXmlParser.DEF_START)
                {
                    if (t[j].type == DEFX.NODE_TYPE_TEXT)
                        stack[stack_n].CreateTextNode(t[j].value);
                    else
                    {
                        VXmlNode new_node = null;
                        switch (t[j].type)
                        {
                            case DEFX.NODE_TYPE_CONTENT:
                                new_node = stack[stack_n].CreateContent("");
                                break;
                            case DEFX.NODE_TYPE_ELEMENT:
                                new_node = stack[stack_n].CreateElement(t[j].name, t[j].value);
                                break;
                            default:
                                break;
                        }

                        stack.Add(new_node);
                    }

                }
                else if (t[j].def == VXmlParser.DEF_END)
                {
                    if (stack[stack_n].NodeTypeCode == DEFX.NODE_TYPE_CONTENT)
                    {
                        VXmlContent c = ((VXmlContent)stack[stack_n]);

                        if (c.fileref != "")
                        {
                            string xp = Path.GetDirectoryName(xml_fileref);
                            string cp = Path.GetDirectoryName(c.fileref);
                            if (cp == "")
                                cp = xp;                // If path is not specified in the 'fileref' - use xml file path
                            string nm = Path.GetFileName(c.fileref);
                            if (cp != "")
                                nm = cp + @"\" + nm;

                            if (File.Exists(nm))
                            {
                                c.Upload(nm, true);
                                c.fileref = nm;
                            }
                            else
                                return "Content file '" + nm + " is not found";
                        }
                    }
                    if (t[j].type != DEFX.NODE_TYPE_TEXT)
                        stack.RemoveAt(stack_n);
                }
                else
                    throw new VXmlException(VXmlException.E0020_XML_PARSE_ERROR_CODE, "- invalid parsed sequence: " + t[j].def);

                j++;
            }

            return "";
        }

        /////////////////////////////////////////////////////////////////////////
        ////////////// ATTRIBUTE METHODS AND PROPERTIES WRAPPERS ////////////////
        /////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Get attribute value by name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public new string GetAttribute(string name)
        {
            VXmlElement de = this.DocumentElement;
            if (de == null)
                return "";
            else
                return de.GetAttribute(name);
        }

        /// <summary>
        /// Get attribute node by name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public new VXmlAttribute GetAttributeNode(string name)
        {
            VXmlElement de = this.DocumentElement;
            if (de == null)
                return null;
            else
                return de.GetAttributeNode(name);
        }

        /*
        /// <summary>
        /// Create attribute
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public new VXmlAttribute CreateAttribute(string name, string value = "")
        {
            VXmlElement de = this.DocumentElement;
            if (de == null)
                throw new VXmlException(VXmlException.E0026_SET_NO_DOCUMENTELEMENT_ERROR_CODE, name);
            else
                return de.SetAttribute(name, value);
        }
        */
        /// <summary>
        /// Set value/add attribute
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public new void SetAttribute(string name, string value)
        {
            VXmlElement de = this.DocumentElement;
            if (de == null)
                throw new VXmlException(VXmlException.E0026_SET_NO_DOCUMENTELEMENT_ERROR_CODE, name);
            else
                de.SetAttribute(name, value);
        }


        /// <summary>
        /// Remove all attributes
        /// </summary>
        public new void RemoveAllAttributes()
        {
            VXmlElement de = this.DocumentElement;
            if (de == null)
                return;
            else
                de.RemoveAllAttributes();
        }

        /// <summary>
        /// Attributes collection
        /// </summary>
        public new VXmlAttributeCollection Attributes
        {
            get
            {
                VXmlElement de = this.DocumentElement;
                if (de == null)
                    return new VXmlAttributeCollection();
                else
                    return de.Attributes;
            }
        }

        /// <summary>
        /// Select attributes of the node node matching xpath criteria
        /// </summary>
        /// <param name="xpath"></param>
        /// <returns></returns>
        public new VXmlAttributeCollection SelectAttributes(string xpath, string name = "*")
        {
            VXmlElement de = this.DocumentElement;
            if (de == null)
                return new VXmlAttributeCollection();
            else
                return de.SelectAttributes(xpath, name);
        }


        /// <summary>
        /// True if at least 1 attribute exists
        /// </summary>
        public new bool HasAttributes
        {

            get
            {
                VXmlElement de = this.DocumentElement;
                if (de == null)
                    return false;
                else
                    return de.HasAttributes;
            }
        }

        ////////////////////////////////////////////////////////////////
        ////////////// TAG METHODS AND PROPERTIES WRAPPERS /////////////
        ////////////////////////////////////////////////////////////////
        /// <summary>
        /// Tag nodes property
        /// </summary>
        public new VXmlTagCollection Tags
        {
            get 
            {
                VXmlElement de = this.DocumentElement;
                if (de == null)
                    return new VXmlTagCollection();
                else
                    return de.TagNodes;
            }
        }

        /// <summary>
        /// Get tag node by name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public new VXmlTag GetTagNode(string name)
        {
            VXmlElement de = this.DocumentElement;
            if (de == null)
                return null;
            else
                return de.GetTagNode(name);
        }

        /// <summary>
        /// Create tag node
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public new void SetTag(string text)
        {
            VXmlElement de = this.DocumentElement;
            if (de == null)
                throw new VXmlException(VXmlException.E0026_SET_NO_DOCUMENTELEMENT_ERROR_CODE, "Tag - " + text);
            else
                de.SetTag(text);
        }

        /// <summary>
        /// Remove tag by value
        /// </summary>
        /// <param name="name"></param>
        public new void RemoveTag(string tag)
        {
            VXmlElement de = this.DocumentElement;
            if (de == null)
                return;
            else
                de.RemoveTag(tag);
        }

        /// <summary>
        /// Remove all tags
        /// </summary>
        public new void RemoveAllTags()
        {
            VXmlElement de = this.DocumentElement;
            if (de == null)
                return;
            else
                de.RemoveAllTags();
        }


        /////////////////////////////////////////////////
        ////////////// PROPERTIES ///////////////////////
        /////////////////////////////////////////////////
        

        /// <summary>
        /// Root node 
        /// </summary>
        public VXmlElement DocumentElement
        {
            get
            {
                long id = get_child_node_id("", DEFX.NODE_TYPE_ELEMENT);
                if (id > 0)
                    return (VXmlElement)GetNode(id);
                else
                    return null;
            }
        }
    }

}
