
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VStorage;

namespace VXML
{
    /////////////////////////////////////////////////////////////
    /////////////////////// VXmlCatalog /////////////////////////
    /////////////////////////////////////////////////////////////
    public class VXmlCatalog : VXmlNode
    {
        /// <summary>
        /// Root storage state
        /// </summary>
        private int state = DEFS.STATE_UNDEFINED;

        // Root Catalog semaphor: true - Checkin/Checkout is in progress (set by VXmlNode)
        public bool ChargeOutProgress = false;

        internal VSIndex index_check_out = null;             // Checkout index
        internal VSIndex index_reference = null;             // Reference index

        internal VSIndex index_catalog = null;               // Catalog index
        internal VSIndex index_document = null;              // Document index

        private string root_path = DEFS.PATH_UNDEFINED;
        public VSEngine Storage = null;

        ///////////////////////////////////////////////////////
        ///////////// CONSTRUCTORS ////////////////////////////
        ///////////////////////////////////////////////////////

        /// <summary>
        /// Empty constructor for create storage
        /// </summary>
        public VXmlCatalog()
        {
            state = DEFS.STATE_UNDEFINED;
            is_root_catalog = true;
            root_path = DEFS.PATH_UNDEFINED;
        }

        /// <summary>
        /// Constructor with path
        /// Open/Set parameter is ignored if defined
        /// </summary>
        public VXmlCatalog(string path)
        {
            state = DEFS.STATE_UNDEFINED;
            is_root_catalog = true;
            root_path = path.Trim();
        }

        /// <summary>
        /// Create object for Sub-Catalog
        /// </summary>
        internal VXmlCatalog(VSpace ns, VSpace cs)
        {
            state = DEFS.STATE_UNDEFINED;           // Not applicable to non-root node

            is_root_catalog = false;

            node_space = ns;

            content_space = cs;
        }

        /// <summary>
        /// Open storage
        /// </summary>
        public void Open(string path = "", string encrypt = "")
        {
            if (!IsRootCatalog)
                throw new VXmlException(VXmlException.E0006_CATALOG_INVALID_OP_CODE, "(Open)");

            if (state == DEFS.STATE_OPENED)
                return;

            string p = (root_path == DEFS.PATH_UNDEFINED) ? path : root_path;

            // Set storage
            Set(p, encrypt);

            // Open storage
            Storage.Open();

            // Initialize Catalog
            initialize_storage();
        }


        /// <summary>
        /// Initialize all objects and indexes (re-open, storage is on)
        /// </summary>
        private void initialize_storage()
        {
            // Initialize Catalog
            node_space = Storage.GetSpace(DEFX.XML_SPACE_NAME);
            content_space = Storage.GetSpace(DEFX.XML_CONTENT_SPACE_NAME);
            if (content_space == null)
                content_space = node_space;

            // Check for space ownership, set if undefined
            if (NodeSpace.Owner != DEFX.SYSTEM_OWNER_VSXML)
            {
                if (NodeSpace.Owner == DEFS.SYSTEM_OWNER_UNDEFINED)
                {
                    if (NodeSpace.GetRootID(DEFX.NODE_TYPE_CATALOG) > 0)
                    {
                        Storage.Close();
                        throw new VXmlException(VXmlException.E0023_NOT_EMPTY_UNDEFINED_SPACE_CODE, ": '" + NodeSpace.Name + "'");
                    }
                    else
                        NodeSpace.Owner = DEFX.SYSTEM_OWNER_VSXML;
                }
                else
                {
                    Storage.Close();
                    throw new VXmlException(VXmlException.E0022_NOT_VSXML_NODE_SPACE_CODE, ": '" + NodeSpace.Owner + "'");
                }
            }
            if (content_space.Name != DEFX.SYSTEM_OWNER_VSXML)
            {
                if (content_space.Owner != DEFX.SYSTEM_OWNER_VSXML)
                {
                    if (content_space.Owner == DEFS.SYSTEM_OWNER_UNDEFINED)
                    {
                        if (content_space.GetRootID(DEFX.NODE_TYPE_CONTENT) > 0)
                        {
                            Storage.Close();
                            throw new VXmlException(VXmlException.E0023_NOT_EMPTY_UNDEFINED_SPACE_CODE, ": '" + content_space.Name + "'");
                        }
                        else
                            content_space.Owner = DEFX.SYSTEM_OWNER_VSXML;
                    }
                    else
                    {
                        Storage.Close();
                        throw new VXmlException(VXmlException.E0022_NOT_VSXML_NODE_SPACE_CODE, ": '" + content_space.Owner + "'");
                    }
                }
            }

            // 1.Lookup root catalog node
            this.OBJ = NodeSpace.GetRootObject(DEFX.NODE_TYPE_CATALOG);

            if (this.OBJ == null)
            {   // Create root catalog object
                long sz = DEFX.ROOT_CATALOG_NODE_NAME.Length + 64 + NODE_FIXED_LENGTH;

                this.OBJ = NodeSpace.Allocate(sz, DEFX.NODE_TYPE_CATALOG, 0, NODE_FIXED_LENGTH);                        // Create default chunk
                this.type = DEFX.NODE_TYPE_CATALOG;
                this.OWNER_ID = this.OBJ.Id;
                this.FGEN = 0;
                OBJ.Set(DEFX.F_NAME, DEFX.ROOT_CATALOG_NODE_NAME);
            }

            this.type = DEFX.NODE_TYPE_CATALOG;

            this.root_catalog = this;

            // INDEXES

            // Create/Open Checkout index
            if (node_space.IndexExists(DEFX.INDEX_NAME_CHARGEOUT))
                index_check_out = node_space.GetIndex(DEFX.INDEX_NAME_CHARGEOUT);
            else
                index_check_out = node_space.CreateIndex(DEFX.INDEX_NAME_CHARGEOUT, true);

            // Create/Open Reference index
            if (node_space.IndexExists(DEFX.INDEX_NAME_REFERENCE))
                index_reference = node_space.GetIndex(DEFX.INDEX_NAME_REFERENCE);
            else
                index_reference = node_space.CreateIndex(DEFX.INDEX_NAME_REFERENCE, false);



            // Create/Open CATALOG index
            if (node_space.IndexExists(DEFX.INDEX_NAME_CATALOG))
                index_catalog = node_space.GetIndex(DEFX.INDEX_NAME_CATALOG);
            else
                index_catalog = node_space.CreateIndex(DEFX.INDEX_NAME_CATALOG, false);

            // Create/Open DOCUMENT index
            if (node_space.IndexExists(DEFX.INDEX_NAME_DOCUMENT))
                index_document = node_space.GetIndex(DEFX.INDEX_NAME_DOCUMENT);
            else
                index_document = node_space.CreateIndex(DEFX.INDEX_NAME_DOCUMENT, false);

            state = DEFS.STATE_OPENED;

        }


        ///////////////////////////////////////////////////////
        ///////////// METHODS /////////////////////////////////
        ///////////////////////////////////////////////////////

        /// <summary>
        /// Crerate new storage
        /// </summary>
        /// <param name="root_path"></param>
        /// <param name="size"></param>
        /// <param name="ext"></param>
        /// <param name="content_size"></param>
        /// <param name="content_ext"></param>
        /// <returns></returns>
        public void Set(string path = "", string encrypt = "", int size = 5, int ext = 5, int content_size = 5, int content_ext = 15)
        {

            if (state == DEFS.STATE_OPENED)
                return;

            string p = (root_path == DEFS.PATH_UNDEFINED) ? path : root_path;

            root_path = p.Trim();

            Storage = new VSEngine();
            Storage.Set(root_path, encrypt);

            /////////// Create spaces if missing ///////////
            if (!Storage.Exists(DEFX.XML_SPACE_NAME))
            {
                if (Storage.Exists(DEFX.XML_CONTENT_SPACE_NAME))
                    Storage.Remove(DEFX.XML_CONTENT_SPACE_NAME);

                if (Storage.Exists(DEFX.XML_INDEX_SPACE_NAME))
                    Storage.Remove(DEFX.XML_INDEX_SPACE_NAME);

                Storage.Create(DEFX.XML_SPACE_NAME, 16, size, ext);

                if (content_size > 0)
                    Storage.Create(DEFX.XML_CONTENT_SPACE_NAME, 64, content_size, content_ext);

                Storage.Create(DEFX.XML_INDEX_SPACE_NAME, 16, 1, 5);

                Storage.UseIndexSpace(DEFX.XML_SPACE_NAME, DEFX.XML_INDEX_SPACE_NAME);

            }
            state = DEFS.STATE_DEFINED;
        }

        /// <summary>
        /// Create new document
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public VXmlDocument CreateDocument(string name, string value= "")
        {
            return (VXmlDocument)create_node(DEFX.NODE_TYPE_DOCUMENT, name, value);
        }

        /// <summary>
        /// Delete document
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public void RemoveDocument(VXmlDocument doc)
        {
            remove_child_node(doc, true);
        }

        /// <summary>
        /// Create new reference
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public VXmlReference CreateReference(VXmlNode n)
        {
            if (!DEFX.BR_NODE_REFERENCE(n.NodeTypeCode))
                throw new VXmlException(VXmlException.E0004_INVALID_NODE_TYPE_CODE, ": " + n.NodeType + " (Create Reference)");

            // Check if this object is already in child node or reference

            VXmlNodeCollection refs = this.get_child_nodes_of_type(DEFX.NODE_TYPE_REFERENCE);
            
            //Check if ref already exists
            foreach (VXmlReference rf in refs)
                if (rf.ReferenceId == n.Id)
                    return rf;

            VXmlReference r = (VXmlReference)create_node(DEFX.NODE_TYPE_REFERENCE, n.Name);
            r.set_reference_node(n);

            root_catalog.index_reference.Insert(n.ID, r.Id);          // Add index
            
            return r;
        }

        /// <summary>
        /// Delete reference
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public void RemoveReference(VXmlReference r)
        {
            remove_child_node(r, true);
        }

        /// <summary>
        /// Remove all references
        /// </summary>
        public void RemoveAllReferences()
        {
            remove_nodes(DEFX.NODE_TYPE_REFERENCE, "", true);
        }

        /// <summary>
        /// Create Sub-Catalog
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public VXmlCatalog CreateCatalog(string name, string value = "")
        {
            return (VXmlCatalog)create_node(DEFX.NODE_TYPE_CATALOG, name, value);
        }

        /// <summary>
        /// Delete Sub-Catalog
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public void RemoveCatalog(VXmlCatalog cat)
        {
            remove_child_node(cat, true);
        }

        ///////////////////////////////////////////////////////////////////
        ///////////// TRANSACTION MANAGEMENT //////////////////////////////
        ///////////////////////////////////////////////////////////////////

        /// <summary>
        /// Start transaction (only root catalog node)
        /// </summary>
        public void Begin()
        {
            if (IsRootCatalog)
                Storage.Begin();
            else
                throw new VXmlException(VXmlException.E0006_CATALOG_INVALID_OP_CODE, "- 'Begin'");
        }

        /// <summary>
        /// Commit transaction (only root catalog node)
        /// </summary>
        public void Commit()
        {
            if (IsRootCatalog)
                Storage.Commit();
            else
                throw new VXmlException(VXmlException.E0006_CATALOG_INVALID_OP_CODE, "- 'Commit'");
            
            Storage.Begin();            // Automatically start new transaction
        }

        /// <summary>
        /// Rollback transaction
        /// </summary>
        public void RollBack()
        {
            if (this.PARENT_ID == 0)
                Storage.RollBack();
            else
                throw new VXmlException(VXmlException.E0006_CATALOG_INVALID_OP_CODE, "- 'Rollback'");
        }

        /// <summary>
        /// Close this session
        /// </summary>
        public void Close()
        {
            if (IsRootCatalog)
            {
                index_check_out = null;
                index_reference = null;
                Storage.Close();
                state = DEFS.STATE_DEFINED;
            }
            else
                throw new VXmlException(VXmlException.E0006_CATALOG_INVALID_OP_CODE, "- 'Close'");
        }

        /// <summary>
        /// Dump database
        /// </summary>
        /// <param name="path"></param>
        public void Dump(string path, bool encrypt = false)
        {
            if (IsRootCatalog)
            {
                if (state != DEFS.STATE_UNDEFINED)
                {
                    Storage.Commit();
                    Storage.Dump(path, "*", encrypt);
                }
                else
                    throw new VXmlException(VXmlException.E0028_STORAGE_UNDEFINED_CODE, "- 'Dump'");
            }
            else
                throw new VXmlException(VXmlException.E0006_CATALOG_INVALID_OP_CODE, "- 'Dump'");
        }

        /// <summary>
        /// Dump database to byte array
        /// </summary>
        /// <param name="path"></param>
        public byte[] DumpToArray(bool encrypt = false)
        {
            if (IsRootCatalog)
            {
                if (state != DEFS.STATE_UNDEFINED)
                {
                    Storage.Commit();
                    return Storage.DumpToArray("*", encrypt);
                }
                else
                    throw new VXmlException(VXmlException.E0028_STORAGE_UNDEFINED_CODE, "- 'Dump'");
            }
            else
                throw new VXmlException(VXmlException.E0006_CATALOG_INVALID_OP_CODE, "- 'Dump'");
        }


        /// <summary>
        /// Restore database
        /// </summary>
        /// <param name="filename"></param>
        public void Restore(string filename)
        {
            if (IsRootCatalog)
            {
                if (state != DEFS.STATE_UNDEFINED)
                {
                    if (root_path == "")
                    {
                        Storage.Restore(filename);
                        this.initialize_storage();
                    }
                    else
                    {
                        int old_state = state;

                        if (state == DEFS.STATE_OPENED)             // Close if opened
                            this.Close();

                        Storage.Restore(filename);

                        if (old_state == DEFS.STATE_OPENED)         // Open if was closed
                            this.Open();
                    }
                }
                else
                    throw new VXmlException(VXmlException.E0028_STORAGE_UNDEFINED_CODE, "- 'Restore'");
            }
            else
                throw new VXmlException(VXmlException.E0006_CATALOG_INVALID_OP_CODE, "- 'Restore'");
        }

        /// <summary>
        /// Restore database from array
        /// </summary>
        public void RestoreFromArray(byte[] data)
        {
            if (IsRootCatalog)
            {
                if (state != DEFS.STATE_UNDEFINED)
                {
                    if (root_path == "")
                    {
                        Storage.RestoreFromArray(data, "*");
                        this.initialize_storage();
                    }
                    else
                    {
                        int old_state = state;

                        if (state == DEFS.STATE_OPENED)             // Close if opened
                            this.Close();

                        Storage.RestoreFromArray(data, "*");

                        if (old_state == DEFS.STATE_OPENED)         // Open if was closed
                            this.Open();
                    }
                }
                else
                    throw new VXmlException(VXmlException.E0028_STORAGE_UNDEFINED_CODE, "- 'Restore'");
            }
            else
                throw new VXmlException(VXmlException.E0006_CATALOG_INVALID_OP_CODE, "- 'Restore'");
        }


        ///////////////////////////////////////////////////////////////////
        ///////////// PROPERTIES //////////////////////////////////////////
        ///////////////////////////////////////////////////////////////////
        /// <summary>
        /// Title
        /// </summary>
        public string Title
        {
            get { return this.GetAttribute("title"); }
            set { SetAttribute("title", value); }
        }


        /// <summary>
        /// Get child document by name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public VXmlDocument GetChildDocument(string name)
        {
            return (VXmlDocument)this.GetNode(get_child_node_id(name, DEFX.NODE_TYPE_DOCUMENT));
        }

        /// <summary>
        /// Get child document by name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public VXmlCatalog GetChildCatalog(string name)
        {
            return (VXmlCatalog)this.GetNode(get_child_node_id(name, DEFX.NODE_TYPE_CATALOG));
        }

        /// <summary>
        /// Collection of documents
        /// </summary>
        public VXmlNodeCollection Documents
        {
            get
            {
                return get_child_nodes_of_type(DEFX.NODE_TYPE_DOCUMENT);
            }
        }

        /// <summary>
        /// Catalog nodes property
        /// </summary>
        public new VXmlNodeCollection ChildNodes
        {
            get
            {
                return get_child_nodes_of_type(DEFX.NODE_TYPE_CATALOG);
            }
        }

        /// <summary>
        /// Reference nodes property
        /// </summary>
        public VXmlNodeCollection References
        {
            get
            {
                return get_child_nodes_of_type(DEFX.NODE_TYPE_REFERENCE);
            }
        }

        /// <summary>
        /// OwnerCatalog property
        /// </summary>
        public VXmlCatalog OwnerCatalog
        {
            get
            {
                return (OWNER_ID == 0) ? null : (VXmlCatalog)GetNode(OWNER_ID);
            }
        }

        /// <summary>
        /// Has child nodes property (catalog nodes)
        /// </summary>
        public new bool HasChildNodes
        {
            get 
            {
                return (get_child_nodes_of_type_ids(DEFX.NODE_TYPE_CATALOG, true).Length > 0);
            }
        }

        public bool IsRootCatalog
        {
            get { return is_root_catalog; }
        }
        private bool is_root_catalog = false;
        ///////////////////////////////////////////////////////////////////
        ///////////// INDEX WRAPPER ///////////////////////////////////////
        ///////////////////////////////////////////////////////////////////
        /// <summary>
        /// Check if index already exists
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool IndexExists(string name)
        {
            return node_space.IndexExists(name);
        }

        /// <summary>
        /// Create index
        /// </summary>
        /// <param name="name"></param>
        public void CreateIndex(string name, bool unique)
        {
            node_space.CreateIndex(name, unique);
        }

        /// <summary>
        /// Delete existing index
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public void DeleteIndex(string name)
        {
            node_space.DeleteIndex(name);
        }

        /// <summary>
        /// Add new index (string key)
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool Insert(string index, string key, long value)
        {
            VSIndex x = node_space.GetIndex(index);
            return x.Insert(key, value);
        }

        /// <summary>
        /// Get ID by full or partial string key (first if if non-unique index)
        /// </summary>
        /// <param name="k"></param>
        /// <returns></returns>
        public long Find(string index, string key, bool partial = false)
        {
            VSIndex x = node_space.GetIndex(index);
            return x.Find(key, partial);
        }

        /// <summary>
        /// Get all IDs by full key (single ID if unique index)
        /// </summary>
        /// <param name="k"></param>
        /// <returns></returns>
        public long[] FindAll(string index, string key, bool partial = false)
        {
            VSIndex x = node_space.GetIndex(index);
            return x.FindAll(key, partial);
        }

        /// <summary>
        /// Check if node exists (string key)
        /// </summary>
        /// <param name="key"></param>
        /// <param name="partial"></param>
        /// <returns>true/false</returns>
        public bool Exists(string index, string key, bool partial = false)
        {
            VSIndex x = node_space.GetIndex(index);
            return x.Exists(key, partial);
        }

        /// <summary>
        /// Delete index (string key)
        /// </summary>
        /// <param name="key"></param>
        /// <param name="id">For non-unique index. 0 - delete a </param>
        /// <returns></returns>
        public bool Delete(string index, string key, long id = 0)
        {
            VSIndex x = node_space.GetIndex(index);
            return x.Delete(key, id);
        }


    }
}
