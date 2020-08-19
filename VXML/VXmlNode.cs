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
    ///////////////////////// VXmlNode //////////////////////////
    /////////////////////////////////////////////////////////////


    public class VXmlNode :Object
    {
        /// <summary>
        /// Manage  child node references
        /// </summary>
        public struct CRef
        {
            public short Type;
            public long Id;
        }

        /******** BASE PROPERTIES      *********/
        protected VSpace node_space = null;                           // Space for xml node data
        protected VSpace content_space = null;                        // Space for content data
        //protected VSpace index_space = null;                          // Space for xml index
        protected VXmlCatalog root_catalog = null;
        internal VSObject OBJ = null;

        ////////////////////////////////////////////////////////////////////////////////////////////////////////
        ///////////////////////// FIELDS ///////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Node type
        /// </summary>
        protected short type = 0;

        ////////////////////////////////////////////////////////////////////////////////////////////////////////
        ///////////////////////// FIXED PART ///////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Checkout state 1-checked out; 0 - no
        /// </summary>
        private const int STATE_POS = 0;
        private const int STATE_LEN = 2;
        private short STATE
        {
            get { return OBJ.ReadShort(STATE_POS); }
            set { OBJ.Write(STATE_POS, value); }
        }

        /// <summary>
        /// Internal nodes count (used to generate names for text, comment and tag noes)
        /// 2017-06-27
        /// </summary>
        private const int FGEN_POS = STATE_POS + STATE_LEN;
        private const int FGEN_LEN = 4;
        protected int FGEN
        {
            get { return OBJ.ReadInt(FGEN_POS); }
            set { OBJ.Write(FGEN_POS, value); }
        }

        /// <summary>
        /// Parent
        /// </summary>
        private const int PARENT_ID_POS = FGEN_POS + FGEN_LEN;
        private const int PARENT_ID_LEN = 8;
        protected long PARENT_ID
        {
            get { return OBJ.ReadLong(PARENT_ID_POS); }
            set { OBJ.Write(PARENT_ID_POS, value); }
        }

        /// <summary>
        /// Owner
        /// </summary>
        private const int OWNER_ID_POS = PARENT_ID_POS + PARENT_ID_LEN;
        private const int OWNER_ID_LEN = 8;
        protected long OWNER_ID
        {
            get { return OBJ.ReadLong(OWNER_ID_POS); }
            set { OBJ.Write(OWNER_ID_POS, value); }
        }

        protected const short NODE_FIXED_LENGTH = OWNER_ID_LEN + OWNER_ID_POS;

        /// <summary>
        /// NODE_TYPE_REFERENCE:    Reference node ID
        /// </summary>
        protected long REF_ID
        {
            get { return OBJ.GetLong(DEFX.F_REF_ID); }
            set
            {
                if (value == 0)
                    OBJ.Delete(DEFX.F_REF_ID);
                else
                    OBJ.Set(DEFX.F_REF_ID, value);
            }
        }

        /// <summary>
        /// NODE_TYPE_CONTENT:      Content ID
        /// </summary>
        protected long CONT_ID
        {
            get { return OBJ.GetLong(DEFX.F_CONT_ID); }
            set
            {
                if (value == 0)
                    OBJ.Delete(DEFX.F_CONT_ID);
                else
                    OBJ.Set(DEFX.F_CONT_ID, value);
            }
        }


        /************ CONSTRUCTORS ***************/
        internal VXmlNode()
        {
        }

        /// <summary>
        /// Initialize node
        /// </summary>
        /// <param name="_s"></param>
        internal VXmlNode(VSpace ns, VSpace cs)
        {
            node_space = ns;
            content_space = cs;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////////
        ///////////////////////// PUBLIC METHODS ///////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Append child node
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public VXmlNode AppendChild(VXmlNode node)
        {
            if (!DEFX.BR_CHILD_IS_VALID_TYPE(this.type, node.type))
                throw new VXmlException(VXmlException.E0004_INVALID_NODE_TYPE_CODE, "(AppendChild): parent '" + this.NodeType + "', child '" + node.NodeType + "'");

            if (IsChargedOut)
                throw new VXmlException(VXmlException.E0013_ALREADY_CHARGED_OUT_CODE);

            if (node.PARENT_ID > 0)
            {
                if (is_parent_node(node.Id))
                    throw new VXmlException(VXmlException.E0002_NODE_IS_UP_TREE_CODE, "ID=" + node.ID + " NAME='" + node.Name + "'");
            }

            if (this.type != DEFX.NODE_TYPE_CATALOG)
            {
                long docid = (type == DEFX.NODE_TYPE_DOCUMENT) ? Id : OWNER_ID;
                if (node.OWNER_ID != docid)
                    throw new VXmlException(VXmlException.E0003_NODE_IS_FROM_DIFFERENT_DOC_CODE, "ID=" + node.ID + " NAME='" + node.Name + "'");
            }

            node.remove_child_reference();
            append_child_node(node);
            return node;
        }

        /// <summary>
        /// Remove child node and subtree
        /// </summary>
        /// <param name="node"></param>
        public void RemoveChild(VXmlNode node)
        {
            if (node.PARENT_ID != this.Id)
                throw new VXmlException(VXmlException.E0005_NOT_A_CHILD_NODE_CODE, "(RemoveChild) ID=" + node.ID + " NAME='" + node.Name + "'");

            if (node.NodeTypeCode != DEFX.NODE_TYPE_REFERENCE)
            {
                if ((this.IsChargedOut) | (node.IsChargedOutTree))
                    throw new VXmlException(VXmlException.E0013_ALREADY_CHARGED_OUT_CODE);
            }
            remove_child_node(node, true);
        }

        /// <summary>
        /// Remove this node
        /// </summary>
        public void Remove()
        {
            remove_child_node(this, true);
        }

        /// <summary>
        /// Remove all child nodes
        /// </summary>
        public void RemoveAll()
        {
            remove_nodes(-1, "", true);
        }

        /// <summary>
        /// Clone node
        /// </summary>
        /// <param name="deep"></param>
        /// <returns></returns>
        public VXmlNode CloneNode(bool deep = false)
        {
            if (type == DEFX.NODE_TYPE_DOCUMENT)
                throw new VXmlException(VXmlException.E0018_DOC_NAME_MISSING_CODE, "(Clone)");

            return clone_xml_node(this, null, deep);
        }

        /// <summary>
        /// Clone all subtree
        /// </summary>
        /// <returns></returns>
        public VXmlNode Clone()
        {
            return CloneNode(true);
        }


        /// <summary>
        /// Select first node matching xpath criteria
        /// </summary>
        /// <param name="xpath"></param>
        /// <returns></returns>
        public VXmlNode SelectSingleNode(string xpath)
        {
            VXmlNodeCollection l = get_nodes(xpath, true);
            return (l.Count > 0) ? l[0] : null;
        }

        /// <summary>
        /// Select the list of nodes matching xpath criteria
        /// </summary>
        /// <param name="xpath"></param>
        /// <returns></returns>
        public VXmlNodeCollection SelectNodes(string xpath)
        {
            return get_nodes(xpath, false);
        }

        /// <summary>
        /// Select attributes of the node node matching xpath criteria
        /// </summary>
        /// <param name="xpath"></param>
        /// <returns></returns>
        public VXmlAttributeCollection SelectAttributes(string xpath, string name = "*")
        {
            VXmlNodeCollection l = get_nodes(xpath, true);

            if (l.Count == 0) 
                return new VXmlAttributeCollection(null);

            return new VXmlAttributeCollection(l[0], name);

        }

        /// <summary>
        /// Create attribute
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public void SetAttribute(string name, string value= "")
        {
            if (!DEFX.BR_NODE_NAME_VALID(name))
                throw new VXmlException(VXmlException.E0008_INVALID_CHAR_CODE);
            create_internal_node(DEFX.NODE_TYPE_ATTRIBUTE, name, value);
        }

        /// <summary>
        /// Create/set group of attributes
        /// </summary>
        /// <param name="attrs">
        /// Array of fields in format: {name}={value}
        /// </param>
        /// <returns></returns>
        public void SetAttributes(string[] attrs)
        {
            create_internal_nodes_bulk(DEFX.NODE_TYPE_ATTRIBUTE, attrs);
        }


        /// <summary>
        /// Create text node
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public VXmlText CreateTextNode(string text)
        {
            string nm = create_internal_node(DEFX.NODE_TYPE_TEXT, "", text);

            return new VXmlText(this, nm);
        }

        /// <summary>
        /// Bulk create text nodes
        /// </summary>
        /// <param name="nodes"></param>
        public void CreateTextNodes(string[] nodes)
        {
            create_internal_nodes_bulk(DEFX.NODE_TYPE_TEXT, nodes);
        }

        /// <summary>
        /// Create tag node
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public void SetTag(string text)
        {
            create_internal_node(DEFX.NODE_TYPE_TAG, text, text);
        }

        /// <summary>
        /// Bulk create tag nodes
        /// </summary>
        /// <param name="nodes"></param>
        public void SetTags(string[] nodes)
        {
            create_internal_nodes_bulk(DEFX.NODE_TYPE_TAG, nodes);
        }


        /// <summary>
        /// Create content node
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public VXmlContent CreateContent(string filename)
        {
            if (filename != "")
                if (!System.IO.File.Exists(filename))
                     throw new VXmlException(VXmlException.E0001_CONTENT_FILE_NOT_FOUND_CODE, "- '" + filename + "'") ;
            VXmlContent c = (VXmlContent)create_node(DEFX.NODE_TYPE_CONTENT, DEFX.GET_NODETYPE(DEFX.NODE_TYPE_CONTENT));
            if (filename != "")
                c.Upload(filename);
            return c;
        }

        /// <summary>
        /// Create content node from byte array
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public VXmlContent CreateContent(byte[] data)
        {
            VXmlContent c = (VXmlContent)create_node(DEFX.NODE_TYPE_CONTENT, DEFX.GET_NODETYPE(DEFX.NODE_TYPE_CONTENT));
            if (data != null)
                if (data.Length > 0)
                    c.ContentBytes = data;
            return c;
        }

        /// <summary>
        /// Create comment node
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public VXmlComment CreateComment(string text)
        {
            string nm = create_internal_node(DEFX.NODE_TYPE_COMMENT, "", text);

            return new VXmlComment(this, nm);
        }


        /// <summary>
        /// Bulk create comment nodes
        /// </summary>
        /// <param name="nodes"></param>
        public void CreateCommentNodes(string[] nodes)
        {
            create_internal_nodes_bulk(DEFX.NODE_TYPE_COMMENT, nodes);
        }

        /// <summary>
        /// Create Element
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public VXmlElement CreateElement(string name, string value = "")
        {
            return (VXmlElement)create_node(DEFX.NODE_TYPE_ELEMENT, name, value);
        }

        /// <summary>
        /// Load child node from file
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public string Load(string file, VXmlNode parent = null)
        {
            VXmlNode n = (parent == null) ? this : parent;

            if (n.NodeTypeCode != DEFX.NODE_TYPE_ELEMENT)
                throw new VXmlException(VXmlException.E0011_XML_CREATE_INVALID_TYPE_CODE);
            return OwnerDocument.Load(file, n);
        }

        /// <summary>
        /// Load child node from xml string
        /// </summary>
        /// <param name="xmlstring"></param>
        /// <returns></returns>
        public string LoadXml(string xmlstring, string name, VXmlNode parent = null)
        {
            VXmlNode n = (parent == null) ? this : parent;

            if (n.NodeTypeCode != DEFX.NODE_TYPE_ELEMENT)
                throw new VXmlException(VXmlException.E0011_XML_CREATE_INVALID_TYPE_CODE);
            return OwnerDocument.LoadXml(xmlstring, name, n);
        }

        /// <summary>
        /// Get existing node by Id 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public VXmlNode GetNode(long id)
        {
            if (id <= 0)
                return null;
            
            short t = get_node_type(id);

            if (t == 0)
                return null;

            VXmlNode node = this.cast_node(t);

            node.OBJ = NodeSpace.GetObject(id);
            node.type = node.OBJ.Pool;

            //node.read(id);

            node.root_catalog = this.root_catalog;

            return node;
        }


        /// <summary>
        /// Export node tree to file. Like CheckOut, but do not lock node
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public void Export(string path, bool encrypt = false)
        {
            chagre_out_to_file(path, false, encrypt);
        }

        /// <summary>
        /// Export node tree to byte array. Like CheckOut, but do not lock node
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public byte[] ExportToArray(bool encrypt = false)
        {
            return charge_out_to_array(false, encrypt);
        }


        /// <summary>
        /// Charge out node to file
        /// </summary>
        /// <param name="path">Directory to save file</param>
        /// <returns>GUID</returns>
        public void ChargeOut(string path, bool encrypt = false)
        {
            chagre_out_to_file(path, true, encrypt);
        }

        /// <summary>
        /// Checkout node to byte array
        /// </summary>
        /// <param name="lck">If true - change state to locked, otherwise - no</param>
        /// <returns>GUID</returns>
        public byte[] ChargeOutToArray(bool encrypt = false)
        {
            return charge_out_to_array(true, encrypt);
        }

        /// <summary>
        /// Chargeout node to file
        /// </summary>
        /// <param name="path">Directory to save file</param>
        /// <param name="lck">If true - change state to locked, otherwise - no</param>
        /// <returns>GUID</returns>
        public void chagre_out_to_file(string path, bool lck, bool encrypt)
        {
            if ((type != DEFX.NODE_TYPE_ELEMENT) & (type != DEFX.NODE_TYPE_DOCUMENT))
                throw new VXmlException(VXmlException.E0009_INVALID_TYPE_CODE, "- '" + NodeType + "' (ChargeOut)");

            if (path != "")
                if (!System.IO.Directory.Exists(path))
                    throw new VXmlException(VXmlException.E0012_PATH_NOT_FOUND_CODE, "- '" + path + "' (ChargeOut)");

            if (lck)
                if (IsChargedOutTree)
                    throw new VXmlException(VXmlException.E0013_ALREADY_CHARGED_OUT_CODE);

            if (GUID == "")
                assign_GUID();

            string fullpath = path + ((path == "") ? "" : "\\") + GUID + "." + DEFX.XML_EXPORT_FILE_TYPE;


            VSIO IO = new VSIO(fullpath, VSIO.FILE_MODE_CREATE, DEFS.ENCRYPT_UDF(DEFX.ENCRYPT_CHARGEOUT));

            IO.SetEncryption(encrypt);

            charge_out_node(IO, lck);

            IO.Close();
        }


        /// <summary>
        /// Chargeout node to byte array
        /// </summary>
        /// <param name="lck">If true - change state to locked, otherwise - no</param>
        /// <returns>GUID</returns>
        public byte[] charge_out_to_array(bool lck, bool encrypt)
        {
            if ((type != DEFX.NODE_TYPE_ELEMENT) & (type != DEFX.NODE_TYPE_DOCUMENT))
                throw new VXmlException(VXmlException.E0009_INVALID_TYPE_CODE, "- '" + NodeType + "' (CheckOut)");

            if (lck)
                if (IsChargedOutTree)
                    throw new VXmlException(VXmlException.E0013_ALREADY_CHARGED_OUT_CODE);

            if (GUID == "")
                assign_GUID();

            VSIO IO = new VSIO(null, DEFS.ENCRYPT_UDF(DEFX.ENCRYPT_CHARGEOUT));

            IO.SetEncryption(encrypt);

            charge_out_node(IO, lck);

            byte[] b = IO.GetBytes();

            IO.Close();

            return b;
        }


        /// <summary>
        /// Checkout node
        /// </summary>
        /// <param name="fs">Sream (file/memory to write</param>
        /// <param name="lck">If true - change state to locked, otherwise - no</param>
        /// <returns>GUID</returns>
        public void charge_out_node(VSIO IO, bool lck)
        {
            uint e_code = IO.GetEncryption() ? DEFS.DATA_ENCRYPTED : DEFS.DATA_NOT_ENCRYPTED;

            // Write encryption indicator
            bool encrypt = IO.GetEncryption();

            IO.SetEncryption(false);
            IO.Write(0, e_code);                                                     // + 0 (4)Encryption indicator
            IO.SetEncryption(encrypt);

            // 1. +4 (4) Signature placeholder
            IO.Write(-1, (int)0);                            

            // 2. +8 (4) CRC; 0 - not finished;
            IO.Write(-1, (int)0);

            // 3. +12 (4) Node type "elem" | "docm"
            IO.Write(-1, (type == DEFX.NODE_TYPE_ELEMENT) ? DEFX.CHARGEOUT_TYPE_ELEMENT : DEFX.CHARGEOUT_TYPE_DOCUMENT);

            // 4. +16(8) DateTime bin
            IO.Write(-1, DateTime.Now.ToBinary());

            // 5. +24(8) VXML Version
            IO.Write(-1, DEFX.CHARGEOUT_FORMAT_VERSION);

            // 6. +32(36) Guid
            IO.Write(-1, GUID);                            // GUID

            // 7. +68(64) Reserve
            byte[] br = new byte[DEFX.CHARGEOUT_HEADER_RESERVE];

            IO.Write(-1, ref br);

            VXmlSerializer s = new VXmlSerializer(IO);          // Serializer

            write_charge_out_data(s, (short)0, lck);

            IO.Write(8, (int)0);           // -0 - CheckOut successfully finished

            IO.Flush();

            // Calculate CRC and update signature

            uint c = IO.GetCRC32(12, -1);                             // CRC32 value

            // +8 (4) CRC
            IO.Write(8, c);

            // +4 (4) Signature 
            IO.Write(4, DEFX.CHARGEOUT_SIGNATURE);

            IO.Flush();
        }


        /// <summary>
        /// Recusive - write node to the file
        /// Save variable length fields:
        /// 2 - name length
        /// name
        /// 8 - value length
        /// value
        /// </summary>
        /// <param name="node"></param>
        internal void write_charge_out_data(VXmlSerializer s, short level, bool lck)
        {

            s.Serialize(this, level);

            if (lck)
                this.STATE = DEFX.NODE_STATE_CHARGED;

            if ((this.NodeTypeCode == DEFX.NODE_TYPE_ELEMENT) | (this.NodeTypeCode == DEFX.NODE_TYPE_CONTENT) | (this.NodeTypeCode == DEFX.NODE_TYPE_DOCUMENT))
            {
                VXmlNodeCollection nodes = this.AllChildNodes;

                for (int i = 0; i < nodes.Count; i++)
                        nodes[i].write_charge_out_data(s, (short)(level + 1), lck);
            }
        }

        /// <summary>
        /// Undo chargeout
        /// </summary>
        /// <returns></returns>
        public bool UndoChargeOut()
        {
            if (!IsChargedOut)
                return false;
            if (this.ParentNode.IsChargedOut)
                throw new VXmlException(VXmlException.E0013_ALREADY_CHARGED_OUT_CODE);

            undo_check_out_node(this);
            return true;
        }

        /// <summary>
        /// Check if node is checked out
        /// </summary>
        public bool IsChargedOut
        {
            get { return (STATE == DEFX.NODE_STATE_CHARGED); }
        }


        /// <summary>
        /// Check node in
        /// </summary>
        /// <param name="path"></param>
        /// <returns>Error message or empty is succeeded</returns>
        public VXmlNode ChargeIn(string path)
        {
            return import_from_file(path, true);
        }

        /// <summary>
        /// Charge node in
        /// </summary>
        public VXmlNode ChargeInFromArray(byte[] data)
        {
            return import_from_array(data, true);
        }


        /// <summary>
        /// Import node from file
        /// </summary>
        /// <param name="path"></param>
        /// <returns>Error message or empty is succeeded</returns>
        public VXmlNode Import(string path)
        {
            return import_from_file(path, false);
        }

        /// <summary>
        /// Import node from array
        /// </summary>
        public VXmlNode ImportFromArray(byte[] data)
        {
            return import_from_array(data, false);
        }

        /// <summary>
        /// Import node from file
        /// </summary>
        /// <param name="path"></param>
        /// <returns>Error message or empty is succeeded</returns>
        public VXmlNode import_from_file(string path, bool replace)
        {
            FileStream fs = null;

            if (!System.IO.File.Exists(path))
                throw new VXmlException(VXmlException.E0015_CHARGE_ERROR_CODE, "file '" + path + "' is not found");

            VSIO IO = new VSIO(path, VSIO.FILE_MODE_OPEN, DEFS.ENCRYPT_UDF(DEFX.ENCRYPT_CHARGEOUT));

            VXmlNode target_node = charge_in_node(IO, replace);

            IO.Close();

            return target_node;
        }

        /// <summary>
        /// Import node from array
        /// </summary>
        public VXmlNode import_from_array(byte[] data, bool replace)
        {
            if (data == null)
                throw new VXmlException(VXmlException.E0015_CHARGE_ERROR_CODE, "data array is null");

            VSIO IO = new VSIO(data, DEFS.ENCRYPT_UDF(DEFX.ENCRYPT_CHARGEOUT));

            VXmlNode target_node = charge_in_node(IO, replace);

            IO.Close();

            return target_node;
        }


        /// <summary>
        /// Charge node in
        /// </summary>
        /// <param name="fs">File/memory stream</param>
        /// <param name="replace">true - find and replace nod using GUID from file; false - create as current node child with empty GUID</param>
        /// <returns>Error message or empty is succeeded</returns>
        public VXmlNode charge_in_node(VSIO IO, bool replace)
        {
            // Check encryption
            IO.SetEncryption(false);
            uint encr = IO.ReadUInt(0);
            IO.SetEncryption(encr == DEFS.DATA_ENCRYPTED);

            VXmlNode target_node = null;
            long fl = IO.GetLength();
            if (fl < 64)
            {
                throw new VXmlException(VXmlException.E0015_CHARGE_ERROR_CODE, "Invalid source length - " + fl.ToString());
            }
            else
            {
                string s = IO.ReadString(-1, 4);                // +4(4)
                if (s != DEFX.CHARGEOUT_SIGNATURE)
                {
                    throw new VXmlException(VXmlException.E0015_CHARGE_ERROR_CODE, "Invalid file signature");
                }
                else
                {
                    uint old_crc = IO.ReadUInt(-1);                     // +8 (4)
                    uint new_crc = IO.GetCRC32(8, -1);                  // CRC32 value

                    if (old_crc != new_crc)
                    {
                        throw new VXmlException(VXmlException.E0015_CHARGE_ERROR_CODE, "Invalid source CRC");
                    }
                    else
                    {
                        //fs.Position = 8;                                    // Restore position

                        // Read header
                        string n0_node_type = IO.ReadString(-1, 4);
                        long n0_date_time = IO.ReadLong(-1);
                        long n0_version = IO.ReadLong(-1);

                        if (n0_version > DEFX.CHARGEOUT_FORMAT_VERSION)
                        {
                            throw new VXmlException(VXmlException.E0014_UNSUPPORTED_CHARGEOUT_VERSION_CODE, "- " + n0_version.ToString() + " (current - " + DEFX.CHARGEOUT_FORMAT_VERSION.ToString() + ")");
                        }
                        string n0_guid = IO.ReadString(-1, 36);
                        IO.SetPosition(IO.GetPosition() + DEFX.CHARGEOUT_HEADER_RESERVE);

                        //////////////////////// Read 1st node /////////////////////////////

                        // Set semaphore on
                        root_catalog.ChargeOutProgress = true;


                        VXmlSerializer ser = new VXmlSerializer(IO);

                        ser.Deserialize();

                        long target_id = (replace)? root_catalog.index_check_out.Find(n0_guid) : 0;

                        if (target_id > 0)
                        {
                            ///// Use existing node
                            target_node = GetNode(target_id);
                            target_node.STATE = DEFX.NODE_STATE_UNCHARGED;
                            target_node = GetNode(target_id);
                            target_node.Name = ser.Name;
                            target_node.Value = ser.Value;

                            target_node.RemoveAll();         // Remove all child nodes of all types

                        }
                        else
                        {
                            ///// Create new node
                            if (n0_node_type == DEFX.CHARGEOUT_TYPE_ELEMENT)
                            {
                                target_node = this.CreateElement(ser.Name, ser.Value);
                                target_node.STATE = DEFX.NODE_STATE_UNCHARGED;
                            }
                            else
                            {
                                // Create document
                                target_node = create_node(DEFX.NODE_TYPE_DOCUMENT, ser.Name, ser.Value);
                                AppendChild(target_node);
                            }

                            if (replace)
                            {
                                target_node.assign_GUID(n0_guid);
                                root_catalog.index_check_out.Insert(n0_guid, target_node.Id);
                            }
                        }

                        for (int i = 0; i < ser.Attr_Names.Count; i++)
                            target_node.SetAttribute(ser.Attr_Names[i], ser.Attr_Values[i]);

                        for (int i = 0; i < ser.Comment_Values.Count; i++)
                            target_node.CreateComment(ser.Comment_Values[i]);

                        for (int i = 0; i < ser.Text_Values.Count; i++)
                            target_node.CreateTextNode(ser.Text_Values[i]);

                        for (int i = 0; i < ser.Tag_Values.Count; i++)
                            target_node.SetTag(ser.Tag_Values[i]);

                        // Create subtree
                        charge_in_child_nodes(ser, 0, target_node);

                        // Set semaphore off
                        root_catalog.ChargeOutProgress = false;

                    }
                }
            }
            return target_node;
        }


        /// <summary>
        /// Get child element by name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public VXmlElement GetChildElement(string name)
        {
            if ((this.type == DEFX.NODE_TYPE_ELEMENT) | (this.type == DEFX.NODE_TYPE_DOCUMENT))
                return (VXmlElement)this.GetNode(get_child_node_id(name, DEFX.NODE_TYPE_ELEMENT));
            else
                return null;
        }

        /// <summary>
        /// Save XML representation to file
        /// </summary>
        /// <param name="fname"></param>
        public string SaveXml(string fname, bool content = true)
        {
            VSIO IO = null;

            if (!DEFX.BR_XML_IS_VALID_TYPE(this.type))
                return "";
            VXmlNode n = (this.type == DEFX.NODE_TYPE_DOCUMENT) ? ((VXmlDocument)this).DocumentElement : this;
            if (n == null)
                return "";

            string sx = @"<?xml version=""1.0""?>";
            if (fname != "")
            {
                IO = new VSIO(fname, VSIO.FILE_MODE_CREATE, "");
                IO.Write(-1, sx);
                sx = "";
            }
            string s = generate_xml(n, sx, "", IO, content);
            if (IO != null)
                IO.Close();

            return (fname == "") ? s : "";
        }

        /// <summary>
        /// Get attribute value by name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public string GetAttribute(string name)
        {
            VXmlAttribute n = GetAttributeNode(name);
            return (n == null) ? "" : n.Value;
        }

        /// <summary>
        /// Get attribute node by name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public VXmlAttribute GetAttributeNode(string name)
        {
            if (field_exists(DEFX.PREFIX_ATTRIBUTE, name))
                return new VXmlAttribute(this, name);
            else
                return null;
        }

        /// <summary>
        /// Get comment node by name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public VXmlComment GetCommentNode(string name)
        {
            if (field_exists(DEFX.PREFIX_COMMENT, name))
                return new VXmlComment(this, name);
            else
                return null;
        }

        /// <summary>
        /// Get text node by name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public VXmlText GetTextNode(string name)
        {
            if (field_exists(DEFX.PREFIX_TEXT, name))
                return new VXmlText(this, name);
            else
                return null;
        }

        /// <summary>
        /// Get tag node by name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public VXmlTag GetTagNode(string name)
        {
            if (field_exists(DEFX.PREFIX_TAG, name))
                return
                    new VXmlTag(this, name);
            else
                return null;
        }

        /// <summary>
        /// Remove all attributes
        /// </summary>
        public void RemoveAllAttributes()
        {
            string[] al = OBJ.GetFields(DEFX.PREFIX_ATTRIBUTE + "*");
         
            for (int i = 0; i < al.Length; i++)
                RemoveAttribute(al[i]);
        }

        /// <summary>
        /// Remove all comments
        /// </summary>
        public void RemoveAllComments()
        {
            string[] al = OBJ.GetFields(DEFX.PREFIX_COMMENT + "*");

            for (int i = 0; i < al.Length; i++)
                RemoveComment(al[i]);
        }

        /// <summary>
        /// Remove all text
        /// </summary>
        public void RemoveAllText()
        {
            string[] al = OBJ.GetFields(DEFX.PREFIX_TEXT + "*");

            for (int i = 0; i < al.Length; i++)
                RemoveText(al[i]);
        }

        /// <summary>
        /// Remove all tags
        /// </summary>
        public void RemoveAllTags()
        {
            string[] al = OBJ.GetFields(DEFX.PREFIX_TAG + "*");

            for (int i = 0; i < al.Length; i++)
                RemoveTag(al[i]);
        }


        /// <summary>
        /// Remove attribute by name
        /// </summary>
        /// <param name="name"></param>
        public void RemoveAttribute(string name)
        {
            OBJ.Delete(DEFX.PREFIX_ATTRIBUTE +  name);
        }

        /// <summary>
        /// Remove comment by name
        /// </summary>
        /// <param name="name"></param>
        public void RemoveComment(string name)
        {
            OBJ.Delete(DEFX.PREFIX_COMMENT + name);
        }

        /// <summary>
        /// Remove text by name
        /// </summary>
        /// <param name="name"></param>
        public void RemoveText(string name)
        {
            OBJ.Delete(DEFX.PREFIX_TEXT + name);
        }

        /// <summary>
        /// Remove tag by value
        /// </summary>
        /// <param name="name"></param>
        public void RemoveTag(string tag)
        {
            string tagname = DEFX.PREFIX_TAG + tag.ToLower();
            OBJ.Delete(tagname);                                    // Delete object tag
        }

 

        ////////////////////////////////////////////////////////////////////////////////////////////////////////
        ///////////////////////// PROPERTIES ///////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Parent_node property
        /// </summary>
        public VXmlNode ParentNode
        {
            get { return GetNode(PARENT_ID); }
        }

        /// <summary>
        /// Node type property
        /// </summary>
        public string NodeType
        {
            get { return DEFX.GET_NODETYPE(type); }
        }

        /// <summary>
        /// Node type code property
        /// </summary>
        public short NodeTypeCode
        {
            get { return type; }
        }

        /// <summary>
        /// Has child nodes property
        /// </summary>
        public bool HasChildNodes
        {
            get 
            {
                List<CRef> c = this.CREFS;
                for (int i = 0; i < c.Count; i++)
                    if (DEFX.BR_NODE_TYPE_XQL(c[i].Type))
                        return true;

                return false; 
            }
        }

        /// <summary>
        /// Name property
        /// </summary>
        public string Name
        {
            get { return OBJ.GetString(DEFX.F_NAME); }
            set
            {
                if (!DEFX.BR_NODE_RENAME(type))
                    throw new VXmlException(VXmlException.E0004_INVALID_NODE_TYPE_CODE, ": node cannot be renamed");

                if (IsChargedOut)
                    throw new VXmlException(VXmlException.E0013_ALREADY_CHARGED_OUT_CODE);

                if ((type != DEFX.NODE_TYPE_CATALOG) & (type != DEFX.NODE_TYPE_DOCUMENT) & (type != DEFX.NODE_TYPE_REFERENCE))
                    if (!DEFX.BR_NODE_NAME_VALID(value))
                        throw new VXmlException(VXmlException.E0008_INVALID_CHAR_CODE);

                if (type == DEFX.NODE_TYPE_ATTRIBUTE)
                {
                    VXmlAttribute a = this.ParentNode.GetAttributeNode(value);
                    if (a != null)
                        throw new VXmlException(VXmlException.E0017_DOC_EXISTS_CODE, "(RenameNode)");
                }

                set_name(value);
            }
        }

        /// <summary>
        /// Value property
        /// </summary>
        public string Value
        {
            get { return OBJ.GetString(DEFX.F_VALUE); }
            set
            {
                if (IsChargedOut)
                    throw new VXmlException(VXmlException.E0013_ALREADY_CHARGED_OUT_CODE);
                if (value == "")
                    OBJ.Delete(DEFX.F_VALUE);
                else
                    OBJ.Set(DEFX.F_VALUE, value);
            }
        }

        /// <summary>
        /// GUID property
        /// </summary>
        public string GUID
        {
            get { return OBJ.GetString(DEFX.F_GUID); }
        }

        /// <summary>
        /// Child refs property
        /// </summary>
        private List<CRef> CREFS
        {
            get
            {
                byte[] b = OBJ.GetBytes(DEFX.F_CREFS);
                int n = b.Length / 10;
                List<CRef> l = new List<CRef>(n);
                for (int i = 0; i < n; i++)
                {
                    CRef r = new CRef();
                    r.Type =   VSLib.ConvertByteToShort(VSLib.GetByteArray(b, (i * 10), 2));
                    r.Id = VSLib.ConvertByteToLong(VSLib.GetByteArray(b, (i * 10) + 2, 8));
                    l.Add(r);
                }
                return l;
            }
            set
            {
                byte[] b = new byte[value.Count * 10];
                for (int i = 0; i < value.Count; i++)
                {
                    VSLib.CopyBytes(b, VSLib.ConvertShortToByte(value[i].Type), (i * 10), 2);
                    VSLib.CopyBytes(b, VSLib.ConvertLongToByte(value[i].Id), (i * 10) + 2, 8);
                }
                OBJ.Set(DEFX.F_CREFS, b);
            }
        }


        /// <summary>
        /// All Child nodes property
        /// </summary>
        public VXmlNodeCollection AllChildNodes
        {
            get { return get_child_nodes_of_type(DEFX.NODE_TYPES_ALL); }
        }


        /// <summary>
        /// Child nodes property
        /// </summary>
        public VXmlNodeCollection ChildNodes
        {
            get { return get_child_nodes_of_type(DEFX.NODE_TYPE_ELEMENT); }
        }

        /// <summary>
        /// Text nodes property
        /// </summary>
        public VXmlTextCollection TextNodes
        {
            get { return new VXmlTextCollection(this); }
        }

        /// <summary>
        /// Tag nodes property
        /// </summary>
        public VXmlTagCollection TagNodes
        {
            get { return new VXmlTagCollection(this); }
        }

        /// <summary>
        /// Tag values property
        /// </summary>
        public string Tags
        {
            get 
            {
                string t = "";
                VXmlTagCollection tc = this.TagNodes;
                for (int i = 0; i < tc.Count; i++)
                    t += tc[i].Value + ";";
                return t;
            }
            set
            {
                string[] new_tags = VSLib.Parse(value.Trim().ToLower(), " /;,#");
                string[] old_tags = VSLib.Parse(this.Tags, " /;,#");
                bool[] old_keep = new bool[old_tags.Length];
                for (int i = 0; i < new_tags.Length; i++)
                {
                    //bool found = false;
                    for (int j = 0; j < old_tags.Length; j++)
                    {
                        if (!old_keep[j])
                            if (new_tags[i] == old_tags[j])
                            {
                                new_tags[i] = "";
                                old_keep[j] = true;
                                break;
                            }
                    }
                }

                // Add new tags
                for (int i = 0; i < new_tags.Length; i++)
                {
                    if (new_tags[i] != "")
                        this.SetTag(new_tags[i]);
                }

                // Remove obsolete tags
                for (int i = 0; i < old_tags.Length; i++)
                {
                    if (!old_keep[i])
                        this.RemoveTag(old_tags[i]);
                }
                
            }
        }

        /// <summary>
        /// Comment nodes property
        /// </summary>
        public VXmlCommentCollection CommentNodes
        {
            get { return new VXmlCommentCollection(this); }
        }

        /// <summary>
        /// Content nodes property
        /// </summary>
        public VXmlNodeCollection ContentNodes
        {
            get
            {
                return get_child_nodes_of_type(DEFX.NODE_TYPE_CONTENT);
            }
        }

        /// <summary>
        /// Id property
        /// </summary>
        public long Id
        {
            get { return this.OBJ.Id; }
        }

        /// <summary>
        /// Id property (string)
        /// </summary>
        public string ID
        {
            get { return Id.ToString(); }
        }

        public virtual string Xml
        {
            get { return SaveXml(""); }
        }


        /// <summary>
        /// Text property
        /// </summary>
        public virtual string Text
        {
            get
            {
                if (!DEFX.BR_CHILD_IS_VALID_TYPE(this.type, DEFX.NODE_TYPE_TEXT))
                    return (Value == null)? "" : Value;
                else
                {
                    string s = "";
                    VXmlTextCollection l = TextNodes;
                    if (l != null)
                        for (int i = 0; i < l.Count; i++)
                            s += l[i].Value;
                    return s;
                }
            }
        }

        /// <summary>
        /// Attributes collection
        /// </summary>
        public VXmlAttributeCollection Attributes
        {
            get { return new VXmlAttributeCollection(this); }
        }

        /// <summary>
        /// True if at least 1 attribute exists
        /// </summary>
        public bool HasAttributes
        {

            get { return (OBJ.GetFields(DEFX.PREFIX_ATTRIBUTE + "*").Length > 0); }
        }

        /// <summary>
        /// OwnerDocument property
        /// </summary>
        public VXmlDocument OwnerDocument
        {
            get
            {
                return (OWNER_ID == 0) ? null : (VXmlDocument)GetNode(OWNER_ID);
            }
        }

        /// <summary>
        /// Owner property
        /// </summary>
        public VXmlNode Owner
        {
            get
            {
                return (OWNER_ID == 0) ? null : GetNode(OWNER_ID);
            }
        }


        /// <summary>
        /// Node space
        /// </summary>
        public VSpace NodeSpace
        {
            get { return node_space; }
        }

        /// <summary>
        /// Content space
        /// </summary>
        public VSpace ContentSpace
        {
            get { return content_space; }
        }

        /// <summary>
        /// Check recursively if at least 1 child node is checked out
        /// </summary>
        public bool IsChargedOutTree
        {
            get
            {
                if (IsChargedOut)
                    return true;

                VXmlNodeCollection l = ChildNodes;
                for (int i = 0; i < l.Count; i++)
                {
                    if (l[i].IsChargedOutTree)
                        return true;
                }
                return false;
            }
        }


        ////////////////////////////////////////////////////////////////////////////////////////////////////////
        ///////////////////////// PRIVATE/PROTECTED/INTERNAL METHODS ///////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Set GUID value
        /// </summary>
        /// <param name="g"></param>
        private string assign_GUID(string oldg = "")
        {
            if (IsChargedOut)
                throw new VXmlException(VXmlException.E0013_ALREADY_CHARGED_OUT_CODE);

            string g = (oldg == "") ? Guid.NewGuid().ToString() : oldg;


            if (GUID != "")
                root_catalog.index_check_out.Delete(GUID, this.Id);

            OBJ.Set(DEFX.F_GUID, g);

            root_catalog.index_check_out.Insert(g, this.Id);

            return g;
        }

        /// <summary>
        /// Change name (used by CheckIn)
        /// </summary>
        /// <param name="name"></param>
        private void set_name(string name)
        {
            OBJ.Set(DEFX.F_NAME, name);
        }

        /// <summary>
        /// Generate xml
        /// </summary>
        /// <param name="node"></param>
        /// <param name="in_str"></param>
        /// <param name="outer"></param>
        /// <returns></returns>
        private string generate_xml(VXmlNode node, string in_str, string in_shift, VSIO IO, bool content)
        {
            string out_str = in_str;
            string out_shift = in_shift + "    ";

            out_str += add_xml_line(IO, DEFS.DELIM_NEWLINE + in_shift + "<" + node.Name);
            // Find all child ids
            VXmlAttributeCollection l_attr = node.Attributes;
            VXmlCommentCollection l_comm = node.CommentNodes;

            VXmlNodeCollection l_elem = node.ChildNodes;
            VXmlTextCollection l_text = node.TextNodes;
            VXmlNodeCollection l_cont = node.ContentNodes;

            // Add attributes
            for (int i = 0; i < l_attr.Count; i++)
            {
                VXmlAttribute nd = l_attr[i];
                out_str += add_xml_line(IO, " " + nd.Name + @"=""" + nd.Value + @"""");
            }

            // Content handling
            if ((node.NodeTypeCode == DEFX.NODE_TYPE_CONTENT) & (IO != null) & content)
            {
                VXmlContent c = (VXmlContent)node.GetNode(node.Id);
                string pth = IO.GetName();
                string dnm = Path.GetDirectoryName(pth);
                if (dnm == "")
                    dnm = "files_" + node.ID;
                else
                    dnm += "\\files_" + node.ID;
                if (!Directory.Exists(dnm))
                {
                    Directory.CreateDirectory(dnm);
                }
                string fnm = c.filename;
                if (fnm == "")
                    fnm = "NONAME.content";
                fnm = dnm + "\\" + c.CONT_ID.ToString("d8") + "_" + fnm;
                c.Download(fnm);

                out_str += add_xml_line(IO, @" fileref=""" + fnm + @"""");
            }

            bool has_childs = (l_elem.Count > 0) | (l_comm.Count > 0) | (l_cont.Count > 0) | (l_text.Count > 0);

            if ((node.Value == "") & !has_childs)
                out_str += add_xml_line(IO, "/>");
            else
            {
                out_str += add_xml_line(IO, ">" + node.Value);

                // Comments
                for (int i = 0; i < l_comm.Count; i++)
                    out_str += add_xml_line(IO, DEFS.DELIM_NEWLINE + in_shift + "<!-- " + l_comm[i].Value + " -->");
                // Text
                for (int i = 0; i < l_text.Count; i++)
                    out_str += add_xml_line(IO, DEFS.DELIM_NEWLINE + in_shift + "<text>" + l_text[i].Value + "</text>");
                // Content
                foreach (VXmlContent ct_node in l_cont)
                    out_str += generate_xml( ct_node, "", out_shift, IO, content);
                // Elements
                foreach (VXmlElement el_node in l_elem)
                    out_str += generate_xml(el_node, "", out_shift, IO, content);

                out_str += add_xml_line(IO, ((has_childs ? DEFS.DELIM_NEWLINE + in_shift : "")) + "</" + node.Name + ">");
            }

            return (IO == null) ? out_str : "";
        }

        /// <summary>
        /// Append string value or send to the stream
        /// </summary>
        /// <param name="str"></param>
        /// <param name="s"></param>
        /// <param name="fs"></param>
        private string add_xml_line(VSIO IO, string s)
        {
            if (IO == null)
                return s;
            else
            {
                IO.Write(-1, s);
                return "";
            }
        }

        /// <summary>
        /// Remove nodes or by criteria (private)
        /// </summary>
        /// <param name="type"></param>
        /// <param name="name">"" - all nodes 9no filter by name)</param>
        /// <param name="remove_ref">true - remove reference in the parent node (top level only); false - for subtree (means parent node will be deleted too</param>
        protected void remove_nodes(short type = -1, string name = "", bool remove_ref = true)
        {
            if (type >= 100)
                return;

            List<long> del_list = new List<long>();
            short t = (type < 0) ? DEFX.NODE_TYPES_ALL : type;
            VXmlNodeCollection node_list = get_child_nodes_of_type(t);

            for (int i = 0; i < node_list.Count; i++)
            {
                if (name == "")
                    del_list.Add(node_list[i].Id);
                else if (VSLib.Compare(name, node_list[i].Name))
                    del_list.Add(node_list[i].Id);
            }

            for (int i = 0; i < del_list.Count; i++)
            {
                GetNode(del_list[i]).Remove();
            }
        }

        /// <summary>
        /// Recursive clone nodes
        /// </summary>
        /// <param name="sourceNode"></param>
        /// <param name="parentNode"></param>
        /// <param name="deep"></param>
        /// <returns></returns>
        protected VXmlNode clone_xml_node(VXmlNode sourceNode, VXmlNode parentNode, bool deep = false)
        {
            VXmlNode pnode = (parentNode == null) ? this : parentNode;
            VXmlNode newNode = pnode.create_node(sourceNode.type, sourceNode.Name, sourceNode.Value);

            // Clone attributes
            VXmlAttributeCollection attrs = sourceNode.Attributes;
            for (int i = 0; i < attrs.Count; i++)
                newNode.SetAttribute(attrs[i].Name, attrs[i].Value);

            // Clone comments
            VXmlCommentCollection comms = sourceNode.CommentNodes;
            for (int i = 0; i < comms.Count; i++)
                newNode.CreateComment(comms[i].Value);

            // Clone content
            if (sourceNode.CONT_ID != 0)
            {
                VXmlContent source_c = (VXmlContent)GetNode(sourceNode.Id);
                VXmlContent new_c = (VXmlContent)GetNode(newNode.Id);
                new_c.ContentBytes = source_c.ContentBytes;
            }

            if (parentNode != null)
                parentNode.AppendChild(newNode);
            else
                sourceNode.ParentNode.AppendChild(newNode);

            // Get all child nodes
            VXmlNodeCollection child_nodes = sourceNode.get_child_nodes_of_type(DEFX.NODE_TYPES_ALL);

            // Clone other nodes
            foreach (VXmlNode c_node in child_nodes)
            {
                VXmlNode node_to_clone = c_node;
                if ((node_to_clone.type != DEFX.NODE_TYPE_ELEMENT) | (deep))
                    if (c_node.Id != newNode.Id)
                        clone_xml_node(c_node, newNode, deep);
            }

            return newNode;
        }

        /// <summary>
        /// Remove all refrenced for node (Parent, Prev, Next), update parent's FirstChild and LastChils if needed
        /// </summary>
        /// <param name="nodeId"></param>
        protected void remove_child_reference()
        {
            VXmlNode parent = this.ParentNode;
            if (parent == null)
                return;

            List<CRef> c = parent.CREFS;
            for (int i = 0; i < c.Count; i++)
                if (c[i].Id == this.Id)
                {
                    c.RemoveAt(i);
                    parent.CREFS = c;
                    break;
                }
        }

        /// <summary>
        /// Create subtree
        /// </summary>
        /// <param name="fs"></param>
        /// <param name="pos"></param>
        /// <param name="node"></param>
        private void charge_in_child_nodes(VXmlSerializer ser, int level, VXmlNode node)
        {
            List<long> stack_n = new List<long>();
            stack_n.Add(node.Id);

            while (ser.Deserialize())
            {
                short current = (short)(stack_n.Count - 1);

                while (current >= ser.Level)
                {
                    stack_n.RemoveAt(current);  // Go up
                    current -= 1;
                }

                VXmlNode pn = node.GetNode(stack_n[current]);

                VXmlNode n = pn.create_node(ser.Type, ser.Name, ser.Value);

                int na = ser.Attr_Names.Count;
                if (na > 0)
                {
                    string[] nds = new string[na];
                    for (int i = 0; i < na; i++)
                        nds[i] = ser.Attr_Names[i] + "=" + ser.Attr_Values[i];
                    n.SetAttributes(nds);
                }

                na = ser.Comment_Values.Count;
                if (na > 0)
                {
                    string[] nds = new string[na];
                    for (int i = 0; i < na; i++)
                        nds[i] = ser.Comment_Values[i];
                    n.CreateCommentNodes(nds);
                }

                na = ser.Text_Values.Count;
                if (na > 0)
                {
                    string[] nds = new string[na];
                    for (int i = 0; i < na; i++)
                        nds[i] = ser.Text_Values[i];
                    n.CreateTextNodes(nds);
                }

                na = ser.Tag_Values.Count;
                if (na > 0)
                {
                    string[] nds = new string[na];
                    for (int i = 0; i < na; i++)
                        nds[i] = ser.Tag_Values[i];
                    n.SetTags(nds);
                }
                stack_n.Add(n.Id);

                if (ser.Type == DEFX.NODE_TYPE_CONTENT)
                {
                    VXmlContent cntn = (VXmlContent)GetNode(n.Id);
                    cntn.save_content_bytes(ref ser.Content);
                }
            }

        }

        protected short get_node_type(long id)
        {
            short pool = NodeSpace.GetObjectPool(id);

            return pool;
        }

        /// <summary>
        /// Execute XQL - used by 'SelectSingleNode' and 'SelectNodes'
        /// </summary>
        /// <param name="xpath"></param>
        /// <param name="all"></param>
        /// <returns></returns>
        private VXmlNodeCollection get_nodes(string xpath, bool single_node = true)
        {
            VXQL q = new VXQL(this);

            string rc = q.ParseXQL(xpath);

            if (rc != "")
                throw new VXmlException(VXmlException.E0016_XQL_ERROR_CODE, rc);

            return q.ExecuteXQL(single_node);
        }

        /// <summary>
        /// Search child node with specified type and name
        /// </summary>
        /// <param name="type"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        protected long get_child_node_id(string name, short type)
        {
            long[] ids = get_child_nodes_of_type_ids(type);
            List<long> o_list = new List<long>();
            for (int i = 0; i < ids.Length; i++)
            {
                VSObject a = this.NodeSpace.GetObject(ids[i]);
                if (a != null)
                    if (a.Pool == type)
                    {
                        VXmlNode node = GetNode(a.Id);
                        if (name == "")
                            return node.Id;
                        else if (VSLib.Compare(name, node.Name))
                            return node.Id;
                    }
            }
            return 0;
        }

        /// <summary>
        /// Check if node (id) is up tree of the current node
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private bool is_parent_node(long id)
        {
            long n = this.PARENT_ID;
            while (n > 0)
            {
                VSObject a = this.NodeSpace.GetObject(n);
                short type = a.Pool;
                if ((type == DEFX.NODE_TYPE_CATALOG) | (type == DEFX.NODE_TYPE_DOCUMENT))
                    return false;
                if (id == a.Id)
                    return true;
                n = a.ReadLong(VXmlNode.PARENT_ID_POS);
            }
            return false;
        }

        /// <summary>
        /// Undo checkout - recursive
        /// </summary>
        /// <param name="node"></param>
        private void undo_check_out_node(VXmlNode node)
        {
            node.STATE = DEFX.NODE_STATE_UNCHARGED;

            VXmlNodeCollection nc = node.get_child_nodes_of_type(DEFX.NODE_TYPES_ALL);

            foreach (VXmlNode n in nc)
                undo_check_out_node(n);
        }


        /// <summary>
        /// Append child node (protected, used by AppendChild and CreateNode)
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private void append_child_node(VXmlNode node)
        {
            if (node.PARENT_ID == this.Id)
                return;
            /*
            long last_id = this.get_last_id(node.type);

            if (last_id != 0)
            {
                node.PREV_ID = last_id;
                this.GetLastNode(node.type).NEXT_ID = node.Id;
                this.set_last_id(node.type, node.Id);
            }
            else
            {
                this.set_first_id(node.type, node.Id);
                this.set_last_id(node.type, node.Id);
            }
            */
            node.PARENT_ID = this.Id;
            List<CRef> c = this.CREFS;
            CRef r = new CRef();

            r.Type = node.NodeTypeCode;
            r.Id = node.Id;
            c.Add(r);
            this.CREFS = c;

            //node.InsertChildNodeIndex();
        }

        /// <summary>
        /// Remove child node and subtree
        /// </summary>
        /// <param name="node"></param>
        /// <param name="remove_ref">true - remove reference in the parent node (top level only); false - for subtree (means parent node will be deleted too</param>
        protected void remove_child_node(VXmlNode node, bool remove_ref = true)
        {
            // 1. Remove specific information
            if (node.type == DEFX.NODE_TYPE_CONTENT)
                node.remove_content();

            // 2. Remove all 'reference' nodes if necessary
            
            if ((node.type == DEFX.NODE_TYPE_DOCUMENT) | (node.type == DEFX.NODE_TYPE_CATALOG))
            {
                long[] refs = root_catalog.index_reference.FindAll(node.ID);
                for (int i = 0; i < refs.Length; i++)
                {
                    VXmlNode n = GetNode(refs[i]);
                    n.Remove();
                }
            }

            // 3. Delete Childs
            node.remove_nodes(-1, "", false);

            // 4. Remove all indexes - OBSOLETE, do that in FreeSpaceSegment for internal/external indexes

            // 5. Remove all references
            if (remove_ref)
                node.remove_child_reference();

            // 6. Free space
            NodeSpace.ReleaseID(node.Id);
        }

        /// <summary>
        /// Create new node
        /// </summary>
        /// <param name="type"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        protected VXmlNode create_node(short type, string name, string value = "")
        {
            ////////////////// Rules for Owner /////////////////////////////
            // Catalog      - root catalog node
            // Reference    - root catalog node
            // Document     - this
            // All others   - document 
            ////////////////////////////////////////////////////////////////
#if (DEBUG)
            root_catalog.__TIMER.START("create_node");
#endif
            if (!root_catalog.ChargeOutProgress)
            {
                // Check if node type is proper
                if (!DEFX.BR_CHILD_IS_VALID_TYPE(this.type, type))
                    throw new VXmlException(VXmlException.E0004_INVALID_NODE_TYPE_CODE, "- '" + DEFX.GET_NODETYPE(type) + "'");

                if (IsChargedOut)
                    throw new VXmlException(VXmlException.E0013_ALREADY_CHARGED_OUT_CODE);

                if ((type != DEFX.NODE_TYPE_CATALOG) & (type != DEFX.NODE_TYPE_DOCUMENT) & (type != DEFX.NODE_TYPE_REFERENCE))
                    if (!DEFX.BR_NODE_NAME_VALID(name))
                        throw new VXmlException(VXmlException.E0008_INVALID_CHAR_CODE);

                if ((this.type == DEFX.NODE_TYPE_DOCUMENT) & (type == DEFX.NODE_TYPE_ELEMENT) & (this.HasChildNodes))
                    throw new VXmlException(VXmlException.E0010_ROOT_EXISTS_CODE);
            }

            VXmlNode node = this.cast_node(type);

            node.OBJ = NodeSpace.Allocate(name.Length + value.Length + 64 + NODE_FIXED_LENGTH, type, 0, NODE_FIXED_LENGTH);                        // Create default chunk

            node.type = type;

            node.OWNER_ID = (this.type == DEFX.NODE_TYPE_DOCUMENT) ? this.Id : this.OWNER_ID;

            node.OBJ.Set(DEFX.F_NAME, name);

            this.FGEN = 0;

            if (value != "")
                node.OBJ.Set(DEFX.F_VALUE, value);

            node.root_catalog = this.root_catalog;

            append_child_node(node);

            ///////////// Create index if necessary /////////
            if (type == DEFX.NODE_TYPE_CATALOG)
            {
                if (DEFX.BR_INDEX_REQUIRED(DEFX.NODE_TYPE_CATALOG))
                    root_catalog.index_catalog.Insert(name, node.Id);
            }
            else if (type == DEFX.NODE_TYPE_DOCUMENT)
            {
                if (DEFX.BR_INDEX_REQUIRED(DEFX.NODE_TYPE_DOCUMENT))
                    root_catalog.index_document.Insert(name, node.Id); 
            }
            /*
            else if (type == DEFX.NODE_TYPE_ELEMENT)
            {
                if (DEFX.BR_INDEX_REQUIRED(DEFX.NODE_TYPE_ELEMENT))
                    root_catalog.index_element.Insert(name, node.Id); 
            }
            */
#if (DEBUG)
            root_catalog.__TIMER.END("create_node");
#endif

            return node;
        }

        /// <summary>
        /// Create attribute/comment/text/tag
        /// </summary>
        /// <param name="type"></param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private string create_internal_node(short type, string name, string value = "")
        {
#if (DEBUG)
            root_catalog.__TIMER.START("create_internal_node");
#endif

            string v = value;

            string nm = "";
            if (type == DEFX.NODE_TYPE_ATTRIBUTE)
                nm = DEFX.PREFIX_ATTRIBUTE + name;
            else if (type == DEFX.NODE_TYPE_TAG)
                nm = DEFX.PREFIX_TAG + name.ToLower();
            else
                nm = generate_unique_name(DEFX.NODE_TYPE_INTERNAL_FIELD_PREFIX[type - 100]);

            OBJ.Set(nm, v);

#if (DEBUG)
            root_catalog.__TIMER.END("create_internal_node");
#endif

            return nm;
        }

        /// <summary>
        /// Bulk create attributes/tags/comments, texts
        /// </summary>
        /// <param name="type"></param>
        /// <param name="nodes"></param>
        private void create_internal_nodes_bulk(short type, string[] nodes)
        {
#if (DEBUG)
            root_catalog.__TIMER.START("create_internal_nodes_bulk");
#endif

            if (nodes == null)
                return;

            if (nodes.Length == 0)
                return;

            string[] flds = new string[nodes.Length];

            for (int i = 0; i < nodes.Length; i++)
            {
                if (type == DEFX.NODE_TYPE_ATTRIBUTE)
                {
                    int j = nodes[i].IndexOf("=");
                    if (j >= 0)
                    {
                        string name = nodes[i].Substring(0, j);
                        if (!DEFX.BR_NODE_NAME_VALID(name))
                            throw new VXmlException(VXmlException.E0008_INVALID_CHAR_CODE, nodes[i]);

                        flds[i] = DEFX.PREFIX_ATTRIBUTE + name + "=" + nodes[i].Remove(0, j + 1);

                    }
                    else
                        throw new VXmlException(VXmlException.E0008_INVALID_CHAR_CODE, nodes[i] + " (missing '=')");
                }
                else
                {
                    if (nodes[i].Trim() != "")
                    {
                        if (type == DEFX.NODE_TYPE_TAG)
                        {
                            flds[i] = DEFX.PREFIX_TAG + nodes[i].ToLower() + "=" + nodes[i].ToLower();
                        }
                        else
                            flds[i] = generate_unique_name(DEFX.NODE_TYPE_INTERNAL_FIELD_PREFIX[type - 100]) + "=" + nodes[i];
                    }
                    else
                        throw new VXmlException(VXmlException.E0027_BULK_CREATE_EMPTY_VALUE_ERROR_CODE);
                }
            }
#if (DEBUG)
            root_catalog.__TIMER.END("create_internal_nodes_bulk");
#endif

            OBJ.Set(flds);         // Group set attrs
        }

        /// <summary>
        /// Create node class for type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private VXmlNode cast_node(short type)
        {
            if (type == DEFX.NODE_TYPE_CATALOG)
                return new VXmlCatalog(NodeSpace, content_space);
            else if (type == DEFX.NODE_TYPE_REFERENCE)
                return new VXmlReference(NodeSpace, content_space);
            else if (type == DEFX.NODE_TYPE_DOCUMENT)
                return new VXmlDocument(NodeSpace, content_space);
            else if (type == DEFX.NODE_TYPE_CONTENT)
                return new VXmlContent(NodeSpace, content_space);
            else if (type == DEFX.NODE_TYPE_ELEMENT)
                return new VXmlElement(NodeSpace, content_space);
            else
                throw new VXmlException(VXmlException.E0009_INVALID_TYPE_CODE, "- '" + DEFX.GET_NODETYPE(type) + "'");
        }

        /// <summary>
        /// Remove content if exists
        /// </summary>
        /// <param name="node"></param>
        protected void remove_content()
        {
            if (CONT_ID > 0)
            {
                VSObject a = content_space.GetObject(CONT_ID);
                content_space.Release(a);
                CONT_ID = 0;
            }
        }

        /// <summary>
        /// Get child node ids of the specific type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        internal long[] get_child_nodes_of_type_ids(short type, bool single = false)
        {
            List<CRef> c = this.CREFS;

            List<long> ids = new List<long>();

            for (int i = 0; i < c.Count; i++)
            {
                if (type < 0)
                {
                    if ((type == DEFX.NODE_TYPES_ALL) | (c[i].Type != DEFX.NODE_TYPE_CONTENT))
                        ids.Add(c[i].Id);
                }
                else if (c[i].Type == type)
                    ids.Add(c[i].Id);

                if ((ids.Count > 0) & single)
                    break;
            }

            return ids.ToArray();
        }

        /// <summary>
        /// Get child nodes of the specific type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        internal VXmlNodeCollection get_child_nodes_of_type(short type)
        {
            return new VXmlNodeCollection(this, this.get_child_nodes_of_type_ids(type));
        }

        /// <summary>
        /// Chek if node of type exist
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        internal bool child_node_of_type_exist(short type)
        {
            return (get_child_nodes_of_type_ids(type, true).Length > 0);
        }

        /// <summary>
        /// Check if field already exists
        /// </summary>
        /// <param name="prefix"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        private bool field_exists(string prefix, string name)
        {
            return OBJ.Exists(prefix + name);
        }

        /// <summary>
        /// Generate unique name for internal node
        /// </summary>
        /// <param name="prefix"></param>
        /// <returns></returns>
        private string generate_unique_name(string prefix)
        {
            this.FGEN++;
            return prefix.Trim() + "fld$" + this.FGEN.ToString();
        }
    }
}
