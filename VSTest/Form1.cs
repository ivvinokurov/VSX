using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using VStorage;
using VXML;
using System.Xml;
using System.Reflection;
using VSUILib;
using System.IO;

namespace VSTest
{
    public partial class Form1 : Form
    {
        class StrList : List<string>
        { }

        private VSEngine vmms;
        public const string ROOT = "C:\\DATA\\TT\\TEST_DATA\\ROOT";
        public const string BACKUP = "C:\\DATA\\TT\\TEST_DATA\\BACKUP";
        public const string NEW_ROOT = "C:\\DATA\\TT\\TEST_DATA\\ROOT_COPY";

        private const long TEST_N = 10000;

        public struct ATTR
        {
            public ATTR(string n, string t, string v)
            {
                name = n.Trim();
                type = t.Trim().ToLower();
                value = v.Trim();
            }
            public string name;
            public string type;
            public string value;
        }
        public List<ATTR> ATTRIBUTES;

        public Form1()
        {
            InitializeComponent();
        }

        private void btnATTR_Click(object sender, EventArgs e)
        {
            RegressionTest.Test002A(txtBox);
        }



        public static void LOG(TextBox log, string msg)
        {
            log.AppendText(msg + "\r" + "\n");
        }


        private void btnViewAlloc_Click(object sender, EventArgs e)
        {
        }
        private void PrintTree(VSpace s, long root, string pref)
        {
            /*
            VKeyHelper.KEY_HEADER h = s.GetKeyHelper().readHeader(root);
            if (h.SG == "VKEY")
            {
                LOG(txtBox, pref + "SG:" + h.SG +
                " ADDRESS:'" + h.ADDRESS.ToString("X") + "'X" +
                " PARENT :" + h.PARENT.ToString("X") + "'X" +
                " MAX_N:  " + h.MAX_N.ToString() +
                " FREE_N: " + h.FREE_N.ToString());
                LOG(txtBox, pref + "FIRST:  " + h.FIRST.ToString() +
                " LAST:   " + h.LAST.ToString() +
                " FIRST_Q:" + h.FIRST_Q.ToString() +
                " LAST_Q: " + h.LAST_Q.ToString());
            }
            if (h.SG == "VKEY")
            {
                int ix = h.FIRST;
                while (ix >= 0)
                {
                    VKeyHelper.KEY k = s.GetKeyHelper().readKEY(h, ix);
                    LOG(txtBox, " " + pref + "KEY: " + k.INDEX.ToString("D3") + " " + k.KEY1.ToString("X8") + " " + k.KEY2.ToString("X8") + " " + k.REF.ToString("X8"));
                    PrintTree(s, k.REF, pref + "  ");
                    ix = k.NEXT;
                } 
            }
             * */
        }



        private void btnGet_Click(object sender, EventArgs e)
        {
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
        }

        private void btnTest_Click(object sender, EventArgs e)
        {
            
            Random r = new Random(5);
            byte[] b1 = new byte[4096];
            byte[] b2 = new byte[256];
            r.NextBytes(b1);
            int k = 0;
            for (int i = 0; i < b1.Length; i++)
            {
                bool f = false;
                for (int j = 0; j < k; j++)
                {
                    if (b1[i] == b2[j])
                    {
                        f = true;
                        break;
                    }
                }
                if (!f)
                {
                    b2[k] = b1[i];
                    k++;
                }
            }
            //LOG(txtBox, k.ToString());
            string s = "";
            for (int i = 0; i < b2.Length; i++)
            {
                s += b2[i].ToString() + ", ";
                LOG(txtBox, b2[i].ToString("D3"));
                //LOG(txtBox, i.ToString("D3"));
            }
            LOG(txtBox, s);


            /*
            string s = "vs_space_key";
            byte[] b = VSLib.ConvertStringToByte(s);
            for (int i = 0; i < b.Length; i++)
                LOG(txtBox, b[i].ToString());
            
             */
            /*
           LOG(txtBox, s);
           VSCrypto cry = new VSCrypto();
           byte[] b = VSLib.ConvertStringToByte(s);
           byte[] b1 = VSCrypto.Encrypt(b, "5Q462");
           byte[] b2 = VSCrypto.Decrypt(b1, "5Q462");

           string s2 = VSLib.ConvertByteToString(b2);
           LOG(txtBox, s2);

           for (int i = 0; i < VSCrypto.byte_e.Length; i++)
           {
               int n = VSCrypto.byte_e[i];
               int m = VSCrypto.byte_d[n];
               LOG(txtBox, i.ToString ("D3") + "   " + m.ToString("D3") + "   " +  n.ToString("D3"));
           }
           */
        }

        private void ttx(string[] parm)
        {
            for (int i = 0; i < parm.Length; i++)
                LOG(txtBox, parm[i]);

        }

        private void btnXML0_Click(object sender, EventArgs e)
        {
            VXmlCatalog cont;


            cont = new VXmlCatalog();
            cont.Set(ROOT);
            VXmlNode n = cont.GetNode(35);
            
            VXmlAttributeCollection ac = n.Attributes;
            for (int i = 0; i < ac.Count; i++)
                LOG(txtBox, ac[i].Name + " " + ac[i].Value);

            LOG(txtBox, " ");

            VXmlCommentCollection cc = n.CommentNodes;
            for (int i = 0; i < cc.Count; i++)
                LOG(txtBox, cc[i].Name + " " + cc[i].Value);
            
            
            cont.Close();
        }

        private void prntElement(VXmlNode n, string space)
        {
            //LOG(txtBox, space + "Id:" + n.ID + " Name:" + n.Name + " Type:" + n.NodeType + " State:" + n.CheckedOut.ToString() + " Value:" + n.Value + " GUID:" + n.GUID);
            //VXmlNode c = n.First;
            //while (c != null)
           // {
           //     prntElement(c, space + "...");
           //     c = c.Next;
            //}
        }

        private void prntNode(VXmlNode n, string space)
        {
            //LOG(txtBox, space + "Id:" + n.ID + " Name:" + n.Name + " Type:" + n.NodeType + " Value:" + n.Value + " State:" + n.CheckedOut.ToString());
            //VXmlNode c = n.First;
            //while (c != null)
           // {
           //     prntNode(c, space + "...");
           //     c = c.Next;
           // }
        }

        private void btnXMLDoc_Click(object sender, EventArgs e)
        {
            VXmlCatalog cont;

            
            /** Create **/
            LOG(txtBox, "***** Create ***** ");
            cont = new VXmlCatalog();
            cont.Set(ROOT);
            VXmlDocument doc1 = cont.CreateDocument("Doc1a");
            VXmlDocument doc2 = cont.CreateDocument("Doc2b");
            VXmlDocument doc3 = cont.CreateDocument("Doc2c");
            VXmlDocument doc4 = cont.CreateDocument("Doc4d");
            VXmlDocument doc5 = cont.CreateDocument("Doc5e");

            VXmlElement e1 = doc5.CreateElement("e1");
            VXmlElement e1a = e1.CreateElement("e1a");
            VXmlElement e1b =e1.CreateElement("e1b");

            for (int i = 0; i < cont.Documents.Count; i++ )
                LOG(txtBox, "**** " + cont.Documents[i].Name);

            prntElement(doc5.DocumentElement, "");
            cont.Commit();
            cont.Close();

            LOG(txtBox, "******************");
            /**/

            cont = new VXmlCatalog();
            cont.Open(ROOT);

            VXmlDocument doc = cont.GetChildDocument("doc2a");
            LOG(txtBox, "doc2a: " + ((doc == null)? "null" : doc.Name));

            doc = cont.GetChildDocument("doc2b");
            LOG(txtBox, "doc2b: " + ((doc == null) ? "null" : doc.Name));

            doc = cont.GetChildDocument("doc2*");
            LOG(txtBox, "doc2*: " + ((doc == null) ? "null" : doc.Name));

            doc = cont.GetChildDocument("doc*");
            LOG(txtBox, "doc*: " + ((doc == null) ? "null" : doc.Name));
            /*
            VXmlDocumentCollection dc = cont.SearchDocuments("doc2a");
            LOG(txtBox, "Search doc2a (non-partial): " + dc.Count.ToString());
            for (int i=0; i<dc.Count; i++)
                LOG(txtBox, "Found: " + dc[i].Name);

            dc = cont.SearchDocuments("doc2b");
            LOG(txtBox, "Search doc2b (non-partial): " + dc.Count.ToString());
            for (int i = 0; i < dc.Count; i++)
                LOG(txtBox, "Found: " + dc[i].Name);


            dc = cont.SearchDocuments("doc2", true);
            LOG(txtBox, "Search doc2 (partial): " + dc.Count.ToString());
            for (int i = 0; i < dc.Count; i++)
                LOG(txtBox, "Found: " + dc[i].Name);
            */

            cont.Close();
            LOG(txtBox, "***** Ended  ***** ");
        }

        private void btnVXQL_Click(object sender, EventArgs e)
        {
            //VXQL q = new VXQL(null);
            //VXQL.XPATH_CMD[] l;
            //string s = "";

 //           l = q.ParseXPath("");
 //           prntXQ(l, "");
 //           l = q.ParseXPath(".");
 //           prntXQ(l, ".");
 //           l = q.ParseXPath("./");
 //           prntXQ(l, "./");
 //           l = q.ParseXPath(".//");
 //           prntXQ(l, ".//");
 /*           l = q.ParseXPath("/aa/bb/cc/dd");
            prntXQ(l, "/aa/bb/cc/dd");
            l = q.ParseXPath("/a1/bb/cc/dd?");
            prntXQ(l, "/a1/bb/cc/dd?");
            l = q.ParseXPath("..");
            prntXQ(l, "..");
            l = q.ParseXPath("../");
            prntXQ(l, "../");
            l = q.ParseXPath("..//");
            prntXQ(l, "..//");
            l = q.ParseXPath("/");
            prntXQ(l, "/");
            l = q.ParseXPath("../a1/bb/cc/*");
            prntXQ(l, "../a1/bb/cc/*");*/

            //s = "/aa/bb/@*";
            //s = @"/[@a!='abc'][b='5'][ last()][@c=55.77]/bb//cc/*[7]";
            //l = q.ParseXQL(s);
            //prntXQ(l, s);
        }

        /*
        private void prntXQ(VXQL.XPATH_CMD[] l, string x)
        {
            LOG(txtBox, "XQL:" + x);
            for (int i = 0; i < l.Length; i++)
                LOG(txtBox, l[i].command + " | " + l[i].operand + " | " + l[i].value);
        }
         */

        private void btnXmlTree_Click(object sender, EventArgs e)
        {
            VXmlDocument doc;
            VXmlCatalog cont;


            cont = new VXmlCatalog();
            cont.Open(ROOT);
            doc = cont.CreateDocument("my_document");

            LOG(txtBox, "Document name:" + doc.Name + " type:" + doc.NodeType);

            VXmlNode root = null;
            if (doc.DocumentElement == null)
            {
                LOG(txtBox, "DocumentElement = null");
                root = doc.CreateElement("Root");
                VXmlElement e1 = root.CreateElement("Node1");
                VXmlElement e2 = root.CreateElement("Node2");
                VXmlElement e3 = root.CreateElement("Node3");

                VXmlElement e21 = e2.CreateElement("Node21");
                VXmlElement e22 = e2.CreateElement("Node22");

                //////////////////////////////////////////////
                e1.SetAttribute("atr1", "AT2TR#E1");
                e1.SetAttribute("atr2", "ATTR#E2");
                e1.SetAttribute("atr3", "ATTRE#E3");
                e22.SetAttribute("atr1", "ATTR#E22-1");
                e22.SetAttribute("atr2", "ATTR#E22-2");
                e3.SetAttribute("atr1", "ATTR#E231");
                e3.SetAttribute("atr2", "ATTR#E32");
                LOG(txtBox, "-------------------------------------------------------");

            }
            prntElement(root, "");
            cont.Close();
        }



        private void btnPERFTEST_Click(object sender, EventArgs e)
        {
            /*
            LOG(txtBox, "Encryption - Start " + DateTime.Now.ToString());
            
            FileStream fsi = new FileStream("C:\\DATA\\TT\\TEMP\\a.vdmp", FileMode.Open);
            FileStream fso = new FileStream("C:\\DATA\\TT\\TEMP\\a.vdmp.encrypted", FileMode.Create);

            VSIo io1 = new VSIo(fsi, "");
            VSIo io2 = new VSIo(fso, DEFS.ENCRYPT_DUMP);

            long cnt = fsi.Length;

            while (cnt > 0)
            {
                long l = (cnt < 1000) ? cnt : 1000;

                byte[] b = io1.ReadBytes(-1, (int)l);

                io2.Write(-1, ref b);

                cnt -= l;
            }

            fsi.Close();
            fso.Close();
            LOG(txtBox, "Encryption - Done " + DateTime.Now.ToString());


            LOG(txtBox, "Decryption - Start " + DateTime.Now.ToString());

            fsi = new FileStream("C:\\DATA\\TT\\TEMP\\a.vdmp.encrypted", FileMode.Open);
            fso = new FileStream("C:\\DATA\\TT\\TEMP\\a.vdmp.decrypted", FileMode.Create);

            io1 = new VSIo(fsi, DEFS.ENCRYPT_DUMP);
            io2 = new VSIo(fso, "");

            cnt = fsi.Length;

            while (cnt > 0)
            {
                long l = (cnt < 500) ? cnt : 500;

                byte[] b= io1.ReadBytes(-1, (int)l);

                io2.Write(-1, ref b);

                cnt -= l;
            }

            fsi.Close();
            fso.Close();
             
            LOG(txtBox, "Decryption - Done " + DateTime.Now.ToString());
            */
        }

        private void btnVXQLGEN_Click(object sender, EventArgs e)
        {
            VXmlDocument doc;
            VXmlCatalog cont;

            cont = new VXmlCatalog();
            cont.Open(ROOT);

            doc = cont.CreateDocument("test_document");

            LOG(txtBox, "Document name:" + doc.Name + " type:" + doc.NodeType);
            DateTime dstarted = DateTime.Now;
            LOG(txtBox, "Gen Started: " + dstarted.ToString("o"));

            VXmlNode root = doc.CreateElement("root");
            root.SetAttribute("root_attr1", "atr0123");
            int cnt = 0;
            ///////////////////////////////////////////
            for (int i = 1; i < 50; i++)
            {

                //vmms.Commit();
                //vmms.Begin();

                VXmlElement n = root.CreateElement("level01_" + i.ToString("D4"));
                cnt++;
                for (int x = 0; x < 10; x++)
                {
                    n.SetAttribute("atr01_" + x.ToString("D4"), "A01_" + x.ToString("D4"));
                    cnt++;
                }

                for (int j = 0; j < 10; j++)
                {
                    VXmlElement n1 = n.CreateElement("level02_" + j.ToString("D4"));
                    cnt++;
                    if (j == 3)
                    {
                        n1.SetAttribute("a1n", "567");
                        n1.SetAttribute("a1s", "QWE");
                    }
                    for (int x = 0; x < 5; x++)
                    {
                        n1.SetAttribute("atr02_" + x.ToString("D4"), "A02_" + x.ToString("D4"));
                        cnt++;
                    }

                    for (int k = 0; k < 10; k++)
                    {
                        VXmlElement n2 = n1.CreateElement("level03_" + k.ToString("D4"));
                        cnt++;
                        for (int x = 0; x < 5; x++)
                        {
                            n2.SetAttribute("atr03_" + x.ToString("D4"), "A02_" + x.ToString("D4"));
                            cnt++;
                            n2.CreateTextNode("Text node " + x.ToString("D4"));
                            cnt++;
                        }
                        if (k == 5)
                        {
                            n2.SetAttribute("a2n", "99.9");
                            n2.SetAttribute("a2s", "abc");
                        }
                    }
                    ////////////////////////////////////
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
                    }
                    ////////////////////////////////////
                    LOG(txtBox, i.ToString() + " " + j.ToString());

                }
            }
            LOG(txtBox, "---------------------------------");
            LOG(txtBox, "Gen Started: " + dstarted.ToString("o"));
            LOG(txtBox, "Gen Ended  : " + DateTime.Now.ToString("o") + " Cnt=" + cnt.ToString());
            cont.Close();
        }

        private void btnVXQLRun_Click(object sender, EventArgs e)
        {
            VXmlDocument doc;
            VXmlCatalog cont;

            cont = new VXmlCatalog();
            cont.Open(ROOT);

            doc = cont.GetChildDocument("test_document");

            VXmlNode n= doc.SelectSingleNode("//AAA[@XA1='VAL5']");
            LOG (txtBox, n.ID);
            VXmlAttributeCollection ac = n.Attributes;
            for (int i = 0; i < ac.Count; i++)
            {
                VXmlAttribute atr = ac[i];
                LOG(txtBox, atr.Name + "   " + atr.Value);
            }

            LOG(txtBox, "-------------------------");
            VXmlAttribute a2 = ac["XA2"];
            LOG(txtBox, a2.Name + "   " + a2.Value);



            //            VXmlNodeList l = doc.DocumentElement.SelectNodes("//*[@atr1='*2*'][8]");
            //VXmlNodeList l = doc.DocumentElement.SelectNodes("//*");
            //VXmlNodeList l = doc.DocumentElement.SelectNodes("//*/@*");
            //VXmlNodeList l = doc.DocumentElement.SelectNodes("//*[@atr03_0003][last()]");
            //VXmlNodeList l = doc.DocumentElement.SelectNodes("//*[@a1n=567][a1s='QWaE']");
            //VXmlNodeList l = doc.DocumentElement.SelectNodes("//*[@a1n=567][@a1s='QWEa']");
            //VXmlNodeList l = doc.DocumentElement.SelectNodes("//*[@a2n<=99.91][@a2s='AS']");
            //VXmlNodeList l = doc.DocumentElement.SelectNodes("//*[@a1n=567][@a1s!=$'QWA']");
            //VXmlNodeList l = doc.DocumentElement.SelectNodes("//level03_0000");
            //VXmlNodeList l = doc.DocumentElement.SelectNodes("/sp2/a4/@asp:*");

            

            //for (int i = 0; i < l.Count; i++)
            //    LOG(txtBox, "i=" + i.ToString() + " Name=" + l[i].Name + " Id=" + l[i].ID);

            //prntNode(doc.DocumentElement, "");

            //LOG (txtBox, "Undo rc = " + doc.DocumentElement.UndoCheckOut());

            //prntNode(doc.DocumentElement, "");

            cont.Close();
            LOG(txtBox, "Ended");
        }

        private void btnStorage_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < 256; i++)
            {
                string s = i.ToString("D3");
                s += "  " + VSCrypto.byte_e2[i].ToString("D3");
                s += "  " + VSCrypto.byte_d2[VSCrypto.byte_e[i]].ToString("D3");
                LOG(txtBox, s);
                int QQ = 0;
                if (VSCrypto.byte_d[VSCrypto.byte_e[i]] != i)
                {
                    QQ++;
                }
                QQ = 1;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            RegressionTest.Test001s(txtBox);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            VXmlDocument doc;
            VXmlCatalog cont;

            cont = new VXmlCatalog();
            cont.Open(ROOT);

            doc = cont.GetChildDocument("test_document");
            if (doc == null)
                doc = cont.CreateDocument("test_document");

            LOG(txtBox, "Started: " + DateTime.Now.ToString("o"));

            VXmlNode root = doc.GetChildElement("root");
            if (root == null)
                root = doc.CreateElement("root");

/**/
            VXmlContent ct = root.CreateContent("C:\\DATA\\TT\\test.pdf");
            LOG(txtBox, "Created: id=" + ct.ID + " length=" + ct.Length.ToString());

            VXmlContent new_ct = (VXmlContent)ct.Clone();
            root.AppendChild(new_ct);

            //vmms.Commit();
            prntElement(root, "");

            ct.Download("C:\\DATA\\TT\\1.pdf");
            new_ct.Download("C:\\DATA\\TT\\1a.pdf");

            prntElement(root, "");

            LOG(txtBox, "------------ Outer ----------------------------");
            LOG(txtBox, root.Xml);

            /***** CheckOut *****/
            string rc = "";// cont.CheckOut("C:\\DATA\\TT");
            LOG(txtBox, "CheckOut rc=" + rc);
            /********************/
            cont.Close();

        }

        private void btnBackup_Click(object sender, EventArgs e)
        {
        }

        private void btnRestore_Click(object sender, EventArgs e)
        {
        }

        private void btnTree_Click(object sender, EventArgs e)
        {
            VXmlDocument doc;
            VXmlCatalog cont;

            LOG(txtBox, "Started: " + DateTime.Now.ToString("o"));

            cont = new VXmlCatalog();
            cont.Open(ROOT);

            doc = cont.GetChildDocument("test_document");
            if (doc != null)
            {
                VXmlNode root = doc.GetChildElement("root");
                if (root != null)
                    prntElement(root, "");

            }
            cont.Close();
            LOG(txtBox, "Ended: " + DateTime.Now.ToString("o"));

        }

        private void btnIndex_Click(object sender, EventArgs e)
        {
        }

        
        //private void PRINT_AVL(VSIndexer x, long maxid)
        //{
            /*
            LOG(txtBox, "--------------------------------------------------------------");
            for (long i = 1; i <= maxid; i++)
            {
                VSAVL a = x._GetAVL(i);
                LOG(txtBox, "ID=" + a.ID.ToString() + "  KEY=" + VSLib.ConvertByteToString(a.KEY) + "  PARENT=" + a.PARENT.ToString() + "  LEFT=" + a.LEFT.ToString() + "  RIGHT=" + a.RIGHT.ToString());
            }
             */
        //}

        private void btnCheckOut_Click(object sender, EventArgs e)
        {
            VXmlCatalog cont;
            VXmlDocument doc;

            cont = new VXmlCatalog();
            cont.Open(ROOT);

            doc = cont.CreateDocument("test_document");

            VXmlElement root = doc.CreateElement("Root_node");

            VXmlNode e1 = root.CreateElement("Element_1");
            VXmlNode e2 = root.CreateElement("Element_2");
            VXmlNode e3 = root.CreateElement("Element_3");

            VXmlElement e21 = e2.CreateElement("Element_21");
            e21.SetAttribute("Attribute1", "123");
            e21.SetAttribute("Attribute2", "ABC");
            
            VXmlElement e22 = e2.CreateElement("Element_22");
            e22.CreateTextNode("This is text node");
            e22.CreateContent("C:\\DATA\\TT\\test.pdf");
            

            VXmlNode clone = e2.CloneNode(true);
            root.AppendChild(clone);

            LOG(txtBox, "Before Checkout:");
            prntElement(doc, "");

            /*
            core.Begin();

            //string rc = e2.CheckOut("C:\\DATA\\TT\\CHECKOUT");

            string rc = doc.CheckOut("C:\\DATA\\TT\\CHECKOUT");

            LOG(txtBox, "CheckOut rc=" + rc + " GUID='" + e2.GUID + "'");

            LOG(txtBox, "After Checkout:");
            prntElement(doc, "");
            */
            cont.Close();

        }

        private void btnCheckIn_Click(object sender, EventArgs e)
        {
            VXmlCatalog cont;
            VXmlDocument doc;
            cont = new VXmlCatalog();
            cont.Open(ROOT);

            doc = cont.CreateDocument("test_document 2");

            LOG(txtBox, "Before CheckIn:");
            prntElement(doc, "");

            //string rc = doc.CheckIn("C:\\DATA\\TT\\CHECKOUT\\ELEMENT.vsco");
            //string rc = doc.CheckIn("C:\\DATA\\TT\\CHECKOUT\\DOCUMENT.vsco");
            VXmlNode rc = cont.ChargeIn("C:\\DATA\\TT\\CHECKOUT\\DOCUMENT.vsco");
            LOG(txtBox, "CheckIn rc=" + rc.ID);

            LOG(txtBox, "After CheckIn:");
            //prntElement(doc, "");
            prntElement(cont, "");

            cont.Close();
        }

        private void btnCloneDoc_Click(object sender, EventArgs e)
        {
            VXmlCatalog cont;
            VXmlDocument doc;
            cont = new VXmlCatalog();
            cont.Open(ROOT);

            doc = cont.CreateDocument("My new document");

            VXmlNode root;
            if (doc.DocumentElement == null)
            {
                root = doc.CreateElement("Root");
            }
            else
            {
                root = doc.DocumentElement;
            }
            VXmlNode e1 = root.CreateElement("EE1");
            VXmlElement e2 = root.CreateElement("EE2");
            VXmlElement e3 = root.CreateElement("EE3");

            VXmlElement e21 = e2.CreateElement("EE21");
            VXmlElement e22 = e2.CreateElement("EE22");

            LOG(txtBox, "Before clone:");
            prntElement(root, "");

            VXmlNode clone = e2.CloneNode(true);
            root.AppendChild(clone);
            LOG(txtBox, "After clone:");
            prntElement(root, "");

            LOG(txtBox, "======= Before DOC clone =======");

            prntElement((VXmlNode)cont, "");

            //cont.Clone("Document CLONE");
            //doc.Clone("Document CLONE");

            LOG(txtBox, "======= After DOC clone =======");
            prntElement((VXmlNode)cont, "");

            cont.Close();

        }

        private void btnCheckIn2_Click(object sender, EventArgs e)
        {
            VXmlCatalog cont;
            VXmlDocument doc;
            cont = new VXmlCatalog();
            cont.Open(ROOT);


            ////////////////// CREATE //////////////////
            doc = cont.CreateDocument("test_document");

            VXmlElement root = doc.CreateElement("Root_node");

            VXmlNode e1 = root.CreateElement("Element_1");
            VXmlNode e2 = root.CreateElement("Element_2");
            VXmlNode e3 = root.CreateElement("Element_3");

            VXmlElement e21 = e2.CreateElement("Element_21");
            e21.SetAttribute("Attribute1", "123");
            e21.SetAttribute("Attribute2", "ABC");

            VXmlElement e22 = e2.CreateElement("Element_22");
            e22.CreateTextNode("This is text node");
            e22.CreateContent("C:\\DATA\\TT\\test.pdf");


            VXmlNode clone = e2.CloneNode(true);
            root.AppendChild(clone);

            LOG(txtBox, "Before Checkout:");
            prntElement(cont, "");


            ////////////// CHECKOUT ////////////////

            //string rc = e2.CheckOut("C:\\DATA\\TT\\CHECKOUT");

            doc.ChargeOut("C:\\DATA\\TT\\CHECKOUT");
            string GUID = doc.GUID;

            LOG(txtBox, "CheckOut done");

            LOG(txtBox, "After Checkout:");
            prntElement(cont, "");

            ////////////// CHECKIN ////////////////

            cont.ChargeIn("C:\\DATA\\TT\\CHECKOUT\\" + GUID + ".vsco");

            LOG(txtBox, "After CheckIn:");
            prntElement(cont, "");

            cont.Close();

        }

        private void btnVSCATALOG_Click(object sender, EventArgs e)
        {
            VSTest.RegressionTest.Test001s(txtBox);
        }

        private void btnFULL_Click(object sender, EventArgs e)
        {
            VSTest.RegressionTest.Test001(txtBox);
        }

        private void btnKEY_Click(object sender, EventArgs e)
        {
            VSTest.RegressionTest.Test002(txtBox);
        }

        private void btnKEYP_Click(object sender, EventArgs e)
        {
            VSTest.RegressionTest.Test002A(txtBox);
        }

        private void btnPARSER_Click(object sender, EventArgs e)
        {
            /*
            LOG(txtBox, "Started: " + DateTime.Now.ToString("o"));
            VXMLTemplate t = VXmlParser.ParseFromFile("C:\\DATA\\TT\\2\\root.xml", "test1");
            /*string shift = "";
            for (int i = 0; i < t.Count; i++)
            {
                if (t[i].def == VXmlParser.DEF_END)
                    shift = shift.Remove(0, 4);
                LOG(txtBox, shift + t[i].def + " " + DEFX.NODE_TYPE[t[i].type] + " " + t[i].name + " " + t[i].value);
                if (t[i].def == VXmlParser.DEF_START)
                    shift += "    ";
            }
            LOG(txtBox, "Ended:   " + DateTime.Now.ToString("o"));
            */
        }

        private void pictureBox1_DoubleClick(object sender, EventArgs e)
        {
            pictureBox1.Cursor = Cursors.Hand;
        }

        private void btnFULL_IMO_Click(object sender, EventArgs e)
        {
            VSTest.RegressionTest.Test001_IMO(txtBox);
        }
    }
}
