using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VXML;
using VStorage;
using System.Windows.Forms;
using System.IO;


namespace VSTest
{
    class UniTest
    {
        private const string ROOT = "C:\\DATA\\TT\\TEST_DATA\\ROOT";
        private const string ROOT_COPY = "C:\\DATA\\TT\\TEST_DATA\\ROOT_COPY";
        private const string DUMP = "C:\\DATA\\TT\\TEST_DATA\\DUMP";
        private const string TEMP = "C:\\DATA\\TT\\TEST_DATA\\TEMP";
        private const string SNAP = "C:\\DATA\\TT\\TEST_DATA\\SNAP";
        private const string SAVE = "C:\\DATA\\TT\\TEST_DATA\\SAVE";

        private const string ROOT_IMO = "";
        private const string ROOT_COPY_IMO = "";

        private static DateTime D1;

        /// <summary>
        /// FULL UNI
        /// </summary>
        /// <param name="tx"></param>
        public static void Test001(TextBox tx, bool IMO)
        {
            DateTime DStart, DEnd;
            VXmlCatalog cont = null;
            VSEngine mgr = null;

            string R1 = IMO ? ROOT_IMO : ROOT;
            string R2 = IMO ? ROOT_COPY_IMO : ROOT_COPY;

            DStart = DateTime.Now;
            LOG(tx, "Started");

            int nn = 0;
            int na = 0;
            int nt = 0;
            int nc = 0;

            // 1. Remove previously created data
            START(tx, "REMOVING files ...");
            foreach (string f in Directory.GetFiles(TEMP))
                File.Delete(f);
            foreach (string f in Directory.GetFiles(SNAP))
                File.Delete(f);
            foreach (string f in Directory.GetFiles(DUMP))
                File.Delete(f);
            foreach (string f in Directory.GetFiles(SAVE))
                File.Delete(f);
            if (Directory.Exists(SAVE + "\\files"))
                Directory.Delete(SAVE + "\\files", true);
            END(tx);

            if (!IMO)
            {
                START(tx, "REMOVING storage copy ...");

                mgr = new VSEngine();
                mgr.Set(ROOT_COPY);
                mgr.Remove("*");
                END(tx);

                START(tx, "REMOVING storage...");
                mgr = new VSEngine();
                mgr.Set(ROOT);
                mgr.Remove("*");
                END(tx);
            }

            // 3. Generate mass nodes
            START(tx, "Starting mass NODE GEN ...");
            cont = new VXmlCatalog(R1);
            cont.Open();


            VXmlCatalog cat0 = cont.CreateCatalog("CAT0");
            VXmlCatalog cat = cont.CreateCatalog("CAT1");
            cat0.CreateReference(cat);
            cat = cont.CreateCatalog("CAT2");
            cat0.CreateReference(cat);
            cat = cont.CreateCatalog("CAT3");
            cat0.CreateReference(cat);
            cat = cont.CreateCatalog("CAT4");
            cat0.CreateReference(cat);
            cat = cont.CreateCatalog("CAT5");
            cat0.CreateReference(cat);
            cat = cont.CreateCatalog("CAT6");
            cat0.CreateReference(cat);
            cat = cont.CreateCatalog("CAT7");
            cat0.CreateReference(cat);
            cat = cont.CreateCatalog("CAT8");
            cat0.CreateReference(cat);
            cat = cont.CreateCatalog("CAT9");
            cat0.CreateReference(cat);
            cat = cont.CreateCatalog("CAT10");

            nn += 22;


            cat = cont.GetChildCatalog("CAT5");

            VXmlDocument doc = cat.CreateDocument("test_document");
            cat0.CreateReference(doc);

            VXmlNodeCollection refs = cat0.References;


            VXmlNodeCollection xcats = cont.SelectNodes("$//*");
            LOG(tx, "Total catalog nodes: " + xcats.Count.ToString());
            int nref = 0;
            foreach (VXmlCatalog xct in xcats)
            {
                ((VXmlCatalog)xct).CreateReference(doc);
                nref++;
            }

            LOG(tx, "Total refs created: " + nref.ToString());



            VXmlNode root = doc.CreateElement("root");
            nn += 2;
            string[] bulk_at;
            string[] bulk_co;
            string[] bulk_te;
            string[] bulk_ta;
            root.SetAttribute("root_attr1", "atr0123");
            na++;
            for (int i = 1; i < 100; i++)
            {
                //VSDebug.StopPoint(i, 94);
                VXmlElement n = root.CreateElement("level01_" + i.ToString("D4"));
                nn++;
                bulk_at = new string[10];
                bulk_ta = new string[10];
                for (int x = 0; x < 10; x++)
                {
                    //n.SetAttribute("atr01_" + x.ToString("D4"), "A01_" + x.ToString("D4"));
                    bulk_at[x] = "atr01_" + x.ToString("D4") + "=" + "A01_" + x.ToString("D4");
                    //n.CreateTag("T1_" + x.ToString());
                    bulk_ta[x] = "T1_" + x.ToString();
                    na++;
                }
                n.SetAttributes(bulk_at);
                n.SetTags(bulk_ta);

                for (int j = 0; j < 10; j++)
                {
                    VXmlElement n1 = n.CreateElement("level02_" + j.ToString("D4"));
                    nn++;

                    if (j == 3)
                    {
                        n1.SetAttribute("a1n", "567");
                        n1.SetAttribute("a1s", "QWE");
                        na += 2;
                    }

                    bulk_at = new string[5];
                    bulk_ta = new string[5];
                    bulk_te = new string[5];
                    bulk_co = new string[5];
                    for (int x = 0; x < 5; x++)
                    {
                        //n1.SetAttribute("atr02_" + x.ToString("D4"), "A02_" + x.ToString("D4"));
                        bulk_at[x] = "atr02_" + x.ToString("D4") + "=" + "A02_" + x.ToString("D4");
                        na++;

                        //n1.CreateTextNode("Text node " + x.ToString("D4"));
                        bulk_te[x] = "Text node " + x.ToString("D4");
                        nt++;

                        //n1.CreateComment("Comment node " + x.ToString("D4"));
                        bulk_co[x] = "Comment node " + x.ToString("D4");
                        nc++;

                        //n1.CreateTag("T2_" + x.ToString() + "AA");
                        bulk_ta[x] = "T2_" + x.ToString() + "AA";
                    }
                    n1.SetAttributes(bulk_at);
                    //n1.CreateTags(bulk_ta);
                    n1.CreateTextNodes(bulk_te);
                    n1.CreateCommentNodes(bulk_co);

                    for (int k = 0; k < 10; k++)
                    {
                        VXmlElement n2 = n1.CreateElement("level03_" + k.ToString("D4"));
                        nn++;
                        for (int x = 0; x < 5; x++)
                        {
                            //n2.SetAttribute("atr03_" + x.ToString("D4"), "A03_" + x.ToString("D4"));
                            bulk_at[x] = "atr03_" + x.ToString("D4") + "=" + "A03_" + x.ToString("D4");
                            na++;
                        }
                        n2.SetAttributes(bulk_at);

                        if (k == 5)
                        {
                            n2.SetAttribute("a2n", "99.9");
                            n2.SetAttribute("a2s", "abc");
                            n2.SetTag("T5");

                            na += 2;
                        }

                    }

                    if ((i == 49) & (j == 9))
                    {
                        VXmlNode a = n1.CreateElement("AAA", "AAA1");
                        a.SetAttribute("XA1", "VAL1");
                        a = n1.CreateElement("AAA", "AAA2");
                        a.SetAttribute("XA1", "VAL2");
                        a = n1.CreateElement("AAA", "AAA3");
                        a.SetAttribute("XA1", "VAL3");
                        a = n1.CreateElement("AAA", "AAA4");
                        a.SetAttribute("XA1", "VAL4");
                        a = n1.CreateElement("AAA", "AAA5");
                        a.SetAttribute("XA1", "VAL5");
                        a.SetAttribute("XA2", "VAL5a");
                        a.SetAttribute("XA3", "VAL5b");
                        a = n1.CreateElement("AAA", "AAA6");
                        a.SetAttribute("XA1", "VAL6");
                        a = n1.CreateElement("AAA", "AAA7");
                        a.SetAttribute("XA1", "VAL7");
                        a = n1.CreateElement("AAA", "AAA8");
                        a.SetAttribute("XA1", "VAL8");
                        a = n1.CreateElement("AAA", "AAA9");
                        a.SetAttribute("XA1", "VAL9");
                        na += 11;
                        nn += 9;
                    }

                }
            }
            cont.Commit();
            LOG(tx, "Created: elements - " + nn.ToString() + ", attributes - " + na.ToString() + ", text nodes - " + nt.ToString() + ", comment nodes - " + nc.ToString());
            LOG(tx, "Total nodes - " + (na + nn + nt).ToString());
            END(tx);


            // 4. Generate content test nodes
            START(tx, "Starting CONTENT generation ...");

            cat = cont.GetChildCatalog("CAT5");
            doc = cat.GetChildDocument("test_document");
            root = doc.DocumentElement;

            VXmlNodeCollection l = doc.SelectNodes("//level02*");
            LOG(tx, "Selected " + l.Count.ToString() + " nodes");
            foreach (VXmlNode ndx in l)
            {
                //VSDebug.StopPoint(ndx.Id, 912);
                VXmlContent ct = ndx.CreateContent("C:\\DATA\\TT\\test.pdf");
            }
            cont.Commit();
            END(tx);

            // 5. Clone node
            START(tx, "Starting CLONE ...");

            cat = cont.GetChildCatalog("CAT5");
            doc = cat.GetChildDocument("test_document");
            root = doc.DocumentElement;
            VXmlNode nx = doc.SelectSingleNode("//level02_0005");
            VXmlNode ny = nx.Clone();
            root.AppendChild(ny);
            cont.Commit();
            END(tx);


            // 6. Save content files
            START(tx, "Downloading CONTENT files ...");

            cat = cont.GetChildCatalog("CAT5");
            doc = cat.GetChildDocument("test_document");
            root = doc.DocumentElement;
            VXmlNodeCollection l1 = doc.SelectNodes("//level02*");
            LOG(tx, "Selected " + l1.Count.ToString() + " nodes");
            foreach (VXmlNode l_node in l1)
            {
                VXmlNodeCollection c = l_node.ContentNodes;
                foreach (VXmlContent ct_node in c)
                {
                    ct_node.Download(TEMP + "\\" + "_" + ((VXmlContent)ct_node).filename);
                }
            }
            cont.Commit();
            END(tx);



            // 7a. Charge out to file
            cat = cont.GetChildCatalog("CAT5");
            doc = cat.GetChildDocument("test_document");
            root = doc.DocumentElement;
            START(tx, "Starting CHARGE OUT 1 to file ...");
            root.ChargeOut(SNAP);
            LOG(tx, "GUID=" + root.GUID);
            cont.Commit();
            END(tx);

            // 7a. Charge out to array
            cat = cont.GetChildCatalog("CAT5");
            doc = cat.GetChildDocument("test_document");
            root = doc.DocumentElement;
            root.UndoChargeOut();
            START(tx, "Starting CHARGE OUT 1 to array ...");
            byte[] checkout_array = root.ChargeOutToArray();
            LOG(tx, "GUID=" + root.GUID);
            cont.Commit();
            END(tx);

            // 8a. Charge in from file
            START(tx, "Starting CHARGE IN 1 (from file) ...");
            cat = cont.GetChildCatalog("CAT5");
            doc = cat.GetChildDocument("test_document");
            root = doc.DocumentElement;
            VXmlNode r2 = root.GetNode(root.Id);
            r2.ChargeIn(SNAP + "\\" + r2.GUID + "." + DEFX.XML_EXPORT_FILE_TYPE);

            cont.Commit();
            END(tx);

            // 8b. Charge in from array
            START(tx, "Starting CHARGE IN 1 (from array) ...");
            cat = cont.GetChildCatalog("CAT5");
            doc = cat.GetChildDocument("test_document");
            root = doc.DocumentElement;
            r2 = root.GetNode(root.Id);
            r2.ChargeInFromArray(checkout_array);
            checkout_array = null;
            cont.Commit();
            END(tx);


            // 9. Export
            START(tx, "Starting EXPORT ...");
            cat = cont.GetChildCatalog("CAT5");
            doc = cat.GetChildDocument("test_document");
            root = doc.DocumentElement;
            //g = root.CheckOut(SNAP);
            root.Export(SNAP);
            LOG(tx, "GUID=" + root.GUID);
            cont.Commit();
            END(tx);

            // 10. Charge in 2
            START(tx, "Starting CHARGE IN 2 ...");
            cat = cont.GetChildCatalog("CAT5");
            doc = cat.GetChildDocument("test_document");
            root = doc.DocumentElement;
            root.ChargeIn(SNAP + "\\" + root.GUID + "." + DEFX.XML_EXPORT_FILE_TYPE);

            LOG(tx, "Rolling back transaction...");
            cont.RollBack();
            END(tx);


            //>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
            // 11. Dump storage (file)
            START(tx, "Starting DUMP to file...");
            cont.Dump(DUMP);
            cont.Close();
            END(tx);

            // 12. Create storage COPY
            START(tx, "CREATING new storage COPY ...");
            cont = new VXmlCatalog();
            cont.Set(ROOT_COPY, "E", 5, 5, 5, 15);
            END(tx);

            // 13. Restore to new COPY
            START(tx, "RESTORING new storage COPY from file ...");
            cont = new VXmlCatalog(ROOT_COPY);
            cont.Set();
            cont.Restore(DUMP + "\\vsto0001.ROOT.vdmp");
            END(tx);

            // 14. Dump storage (memory)
            START(tx, "Starting DUMP to memoryfile...");
            cont = new VXmlCatalog(ROOT);
            cont.Open();
            byte[] bytes_dump = cont.DumpToArray();
            cont.Close();
            END(tx);

            START(tx, "REMOVING storage copy ...");
            mgr = new VSEngine();
            mgr.Set(ROOT_COPY);
            mgr.Remove("*");
            END(tx);

            // 16. Create storage COPY
            START(tx, "CREATING new storage COPY ...");
            cont = new VXmlCatalog();
            cont.Set(ROOT_COPY, "", 5, 5, 5, 15);
            END(tx);

            // 17. Restore to new COPY
            START(tx, "RESTORING new storage COPY from memory array ...");
            cont = new VXmlCatalog(ROOT_COPY);
            cont.Set();
            cont.RestoreFromArray(bytes_dump);
            //bytes_dump = null;
            END(tx);

            // 18. Charge in new COPY
            START(tx, "Starting CHARGE INN new COPY...");
            cont = new VXmlCatalog(ROOT_COPY);
            cont.Open();

            cat = cont.GetChildCatalog("CAT5");
            doc = cat.GetChildDocument("test_document");
            root = doc.DocumentElement;
            root.ChargeIn(SNAP + "\\" + root.GUID + "." + DEFX.XML_EXPORT_FILE_TYPE);
            cont.Close();
            END(tx);
            M15:
            // 19. Delete all REFERENCES  in the new COPY
            START(tx, "Delete all references in the new COPY...");
            cont = new VXmlCatalog(ROOT_COPY);
            cont.Open();

            xcats = cont.SelectNodes("$//*");
            LOG(tx, "Total catalog nodes: " + xcats.Count.ToString());
            nref = 0;
            foreach (VXmlCatalog ccx in xcats)
                ccx.RemoveAllReferences();
            cont.Close();
            END(tx);

            // 20. Delete all NODES in the new COPY
            START(tx, "Delete all nodes in the new COPY...");
            cont = new VXmlCatalog(ROOT_COPY);
            cont.Open();

            cat = cont.GetChildCatalog("CAT5");
            doc = cat.GetChildDocument("test_document");
            root = doc.DocumentElement;
            doc.RemoveChild(root);
            cont.Close();
            END(tx);

            // 21. Save XML

            START(tx, "Starting Save XML ...");
            cont = new VXmlCatalog(ROOT);
            cont.Open();

            cat = cont.GetChildCatalog("CAT5");
            doc = cat.GetChildDocument("test_document");
            root = doc.DocumentElement;
            LOG(tx, "RC=" + root.SaveXml(SAVE + "\\root.xml"));
            cont.Close();
            END(tx);

            // 22. Load XML into the new COPY
            START(tx, "Starting Load XML into the new COPY ...");
            cont = new VXmlCatalog(ROOT_COPY);
            cont.Open();

            cat = cont.GetChildCatalog("CAT5");
            doc = cat.GetChildDocument("test_document");
            LOG(tx, "RC=" + doc.Load(SAVE + "\\root.xml"));
            cont.Close();
            END(tx);


            // 23. Extend 1
            START(tx, "Extend values in the new COPY 1...");
            cont = new VXmlCatalog(ROOT_COPY);
            cont.Open();

            cat = cont.GetChildCatalog("CAT5");
            doc = cat.GetChildDocument("test_document");
            root = doc.DocumentElement;
            VXmlNodeCollection ncol = root.SelectNodes("//level03*");
            foreach (VXmlNode n1 in ncol)
                n1.Value = "0123456789" + "0123456789" + "0123456789" + "0123456789" + "0123456789";
            LOG(tx, "Updated " + ncol.Count.ToString());
            cont.Close();
            END(tx);

            // 24. Extend 2
            START(tx, "Extend values in the new COPY 2...");
            cont = new VXmlCatalog(ROOT_COPY);
            cont.Open();

            cat = cont.GetChildCatalog("CAT5");
            doc = cat.GetChildDocument("test_document");
            root = doc.DocumentElement;
            ncol = root.SelectNodes("//level03*");
            string v = "";
            for (int j = 0; j < 10; j++)
                v += "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";
            foreach (VXmlNode n1 in ncol)
                n1.Value = n1.Value + v;
            LOG(tx, "Updated " + ncol.Count.ToString());
            cont.Close();
            END(tx);
            M17:
            // 25. Shrink
            START(tx, "Shrink values in the new COPY ...");
            cont = new VXmlCatalog(ROOT_COPY);
            cont.Open();

            cat = cont.GetChildCatalog("CAT5");
            doc = cat.GetChildDocument("test_document");
            root = doc.DocumentElement;
            ncol = root.SelectNodes("//level03*");
            foreach (VXmlNode n1 in ncol)
                n1.Value = "sh";
            LOG(tx, "Updated " + ncol.Count.ToString());

            cont.Close();
            END(tx);

            // 26. Add dump as a blob
            START(tx, "Add dump as a blob ...");
            cont = new VXmlCatalog(ROOT_COPY);
            cont.Open();

            cat = cont.GetChildCatalog("CAT5");
            doc = cat.GetChildDocument("test_document");
            root = doc.DocumentElement;
            VXmlNode n01 = root.SelectSingleNode("//level01*");
            n01.CreateContent(bytes_dump);
            cont.Close();
            END(tx);


            DEnd = DateTime.Now;
            TimeSpan ts = DEnd - DStart;
            LOG(tx, "TOTAL: " + ts.Hours.ToString() + " hr " + ts.Minutes.ToString() + " min " + ts.Seconds.ToString() + " secs");
        }


        private static void START(TextBox tx, string msg)
        {
            D1 = DateTime.Now;
            LOG(tx, msg);
        }

        private static void END(TextBox tx)
        {
            TimeSpan ts = DateTime.Now - D1;
            LOG(tx, "Done " + ts.Hours.ToString("d2") + ":" + ts.Minutes.ToString("d2") + ":" + ts.Seconds.ToString("d2"));
        }

        /// <summary>
        /// Logger
        /// </summary>
        /// <param name="log"></param>
        /// <param name="msg"></param>
        public static void LOG(TextBox log, string msg)
        {
            log.AppendText(DateTime.Now.ToString("T") + "  " + msg + "\r" + "\n");
        }
    }
}
