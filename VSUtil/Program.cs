using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VStorage;
using System.Windows;
using System.Reflection;

namespace VSUtil
{
    class Program
    {
        static void Main(string[] args)
        {
            /*
             * Usage
             * <command> [-<par1> [value]] ... [-<parN> [value]] 
             ********** Commands *********************
             * Command can be shortened but shall be unambiguous
             *  create    - create space
             *      -n[ame] -p[agesize] -s[ize] -e[xtension] -d[ir]
             *  extend    - extend space
             *      -n[ame] -e[xtension]
             *  remove       - delete space
             *      -n[ame]
             *  addpartition - add partition
             *      -n[ame] -s[ize]
             *  dump - dump storage
             *      -n[ame] -d[ir]
             *  restore - restore data to the storage from dump
             *      -n[ame] -d[ir]
             *  list] - display space information (* - all)
             *      -n[ame] 
             ********** Parameters summary ***********      
             ***** Common, mandatory
             * -n[ame]
             ***** Common, optional
             * -r[oot]
             ***** Other
             * -p[agesize]        default=16
             * -s[ize]            default=5
             * -e[xtension]       default=0
             * -d[directory]       default=0
             * 
            */

            const string DEF_CMD_CREATE = "create";
            const string DEF_CMD_EXTEND = "extend";
            const string DEF_CMD_REMOVE = "remove";
            const string DEF_CMD_ADDPARTITION = "addpartition";
            const string DEF_CMD_DUMP = "dump";
            const string DEF_CMD_RESTORE = "restore";
            const string DEF_CMD_LIST = "list";

            string[] cmds = { DEF_CMD_CREATE, DEF_CMD_EXTEND, DEF_CMD_REMOVE, DEF_CMD_ADDPARTITION, DEF_CMD_DUMP, DEF_CMD_RESTORE, DEF_CMD_LIST };

            const string DEF_OP_NAME = "-n";
            const string DEF_OP_ROOT = "-r";
            const string DEF_OP_PAGESIZE = "-p";
            const string DEF_OP_SPACESIZE = "-s";
            const string DEF_OP_EXTEND = "-e";
            const string DEF_OP_DIRECTORY = "-d";



            string errmsg = "Invalid parameter";
            string errexe = "Command execution error";

            string cmd = "";

            string err = "";
            string root = "";
            string dir = "";
            string name = "";
            string size = "";
            string ext = "";
            string page = "";
            VSEngine vs;

            string msg100 = "Started " + DateTime.Now.ToString("s");


            Console.WriteLine("Virtual Storage Util V " + Assembly.GetEntryAssembly().GetName().Version.ToString());
            string s = "";
            for (int i = 0; i < args.Length; i++)
                s += " " + args[i];

            Console.WriteLine("Aguments: " + s.Trim());

            if (args.Length == 0)
                err = errmsg + " - command is not specified";
            else
            {
                // Identify command
                for (int i = 0; i < cmds.Length; i++)
                {
                    if (cmds[i].IndexOf(args[0].ToLower()) == 0)
                    {
                        if (cmd != "")
                        {
                            err = errmsg + " - umbiguous command - '" + args[0] + "'";
                            break;
                        }
                        else
                            cmd = cmds[i];
                    }
                }

                if (cmd == "")
                    err = errmsg + " - command is not recognized";
                else
                {
                    if (err == "")
                    {
                        root = getParameterValue(args, DEF_OP_ROOT);
                        if (root.Substring(0, 1) == ":")
                            err = errmsg + " - rooth path is not specified or incorrect";
                        else
                        {
                            name = getParameterValue(args, DEF_OP_NAME);
                            dir = getParameterValue(args, DEF_OP_DIRECTORY);        // Space directory
                            size = getParameterValue(args, DEF_OP_SPACESIZE);
                            ext = getParameterValue(args, DEF_OP_EXTEND);
                            page = getParameterValue(args, DEF_OP_PAGESIZE);

                            vs = new VSEngine(root);


                            ////////////// CREATE ////////////////
                            if (cmd == DEF_CMD_CREATE)
                            {
                                Console.WriteLine(msg100 + ", command='CREATE'");
                                if (name.Substring(0, 1) == ":")
                                    err = name;
                                else if (size.Substring(0, 1) == ":")
                                    err = size;
                                else if (ext.Substring(0, 1) == ":")
                                    err = ext;
                                else if (page.Substring(0, 1) == ":")
                                    err = page;
                                else if (dir.Length > 0)
                                {
                                    if (dir.Substring(0, 1) == ":")
                                        err = dir;
                                }

                                if (err == "")
                                {
                                    try
                                    {
                                        vs.Create(name, Convert.ToInt32(page), Convert.ToInt32(size), Convert.ToInt32(ext), dir);
                                    }
                                    catch (VSException e)
                                    {
                                        Console.WriteLine(errexe);
                                        err = e.Message;
                                    }
                                }
                                else
                                    err = errmsg + err;
                            }
                            ////////////// EXTEND ////////////////
                            else if (cmd == DEF_CMD_EXTEND)
                            {
                                Console.WriteLine(msg100 + ", command='EXTEND'");
                                ext = getParameterValue(args, "-e");

                                if (name.Substring(0, 1) == ":")
                                    err = name;
                                else if (ext.Substring(0, 1) == ":")
                                    err = ext;

                                if (err == "")
                                {
                                    try
                                    {
                                        vs.Extend(name, Convert.ToInt32(ext));
                                    }
                                    catch (VSException e)
                                    {
                                        Console.WriteLine(errexe);
                                        err = e.Message;
                                    }
                                }
                                else
                                    err = errmsg + err;
                            }
                            ////////////// REMOVE ////////////////
                            else if (cmd == DEF_CMD_REMOVE)
                            {
                                Console.WriteLine(msg100 + ", command='REMOVE'");

                                if (name.Substring(0, 1) == ":")
                                    err = name;

                                if (err == "")
                                {

                                    try
                                    {
                                        vs.Remove(name);
                                    }
                                    catch (VSException e)
                                    {
                                        Console.WriteLine(errexe);
                                        err = e.Message;
                                    }
                                }
                                else
                                    err = errmsg + err;
                            }
                            ////////////// ADDPARTITION ////////////////
                            else if (cmd == DEF_CMD_ADDPARTITION)
                            {
                                Console.WriteLine(msg100 + ", command='ADD PARTITION'");

                                if (name.Substring(0, 1) == ":")
                                    err = name;
                                else if (size.Substring(0, 1) == ":")
                                    err = size;

                                if (err == "")
                                {
                                    try
                                    {
                                        vs.AddPartition(name, Convert.ToInt32(size));
                                    }
                                    catch (VSException e)
                                    {
                                        Console.WriteLine(errexe);
                                        err = e.Message;
                                    }
                                }
                                else
                                    err = errmsg + err;
                            }
                            ////////////// DUMP ////////////////
                            else if (cmd == DEF_CMD_DUMP)
                            {
                                Console.WriteLine(msg100 + ", command='DUMP'");

                                if (name.Substring(0, 1) == ":")
                                    err = name;
                                if (dir.Length > 0)
                                {
                                    if (dir.Substring(0, 1) == ":")
                                        err = dir;
                                }

                                if (err == "")
                                {
                                    string rc = vs.Dump(dir, name);
                                    if (rc != "")
                                        err = "Dump error - " + rc;
                                }
                            }
                            ////////////// RESTORE ////////////////
                            else if (cmd == DEF_CMD_RESTORE)
                            {
                                Console.WriteLine(msg100 + ", command='RESTORE'");

                                if (name.Substring(0, 1) == ":")
                                    err = name;

                                if (dir.Length > 0)
                                {
                                    if (dir.Substring(0, 1) == ":")
                                        err = dir;
                                }

                                if (err.Length == 0)
                                {
                                    string rc = vs.Restore(dir, name);
                                    if (rc != "")
                                        err = "Restore error - " + rc;
                                }
                            }
                            ////////////// LIST ////////////////
                            else if (cmd == DEF_CMD_LIST)
                            {
                                Console.WriteLine(msg100 + ", command='LIST'");

                                if (name.Substring(0, 1) == ":")
                                    err = name;
                                else
                                {
                                    string[] rc = vs.List(name);
                                    for (int i = 0; i < rc.Length; i++)
                                        Console.WriteLine(rc[i]);
                                }

                            }
                            else
                                err = "Invalid command - " + cmd;
                        }
                    }
                }
                int r = 0;
                if (err != "")
                {
                    Console.WriteLine(err);
                    r = 8;
                }
                Console.WriteLine("Ended, Rc = " + r.ToString() + ", " + DateTime.Now.ToString("s"));
            }
        }
        /// <summary>
        /// GetSpace string representation of the value for specified parameter
        /// </summary>
        /// <param name="args"></param>
        /// <param name="s">-r; -n; -p; -s; -e;</param>
        /// <returns></returns>
        static string getParameterValue(string[] args, string s, bool optional = false)
        {
            string val = "";
            for (int i = 1; i < args.Length; i++)
            {
                if (args[i].Length >= 2)
                {
                    if (args[i].Substring(0, 2) == s)
                    {
                        if (args.Length > (i + 1))
                        {
                            if (args[i + 1].Substring(0, 1) != "-")
                                val = args[i + 1];
                        }
                        break;
                    }
                }
            }

            if (s == "-r")
            {
                if ((val == "*") | (val == ""))
                val = System.IO.Directory.GetCurrentDirectory();
                
                if (System.IO.Directory.Exists(val))
                    return val;
                else
                    return ": Root directory doesn't exist - " + val;
            }
            if (s == "-d")
            {
                if (val == "")
                    return "";
                if (System.IO.Directory.Exists(val))
                        return val;
                else
                    return ": directory doesn't exist - " + val;
            }
            else if (s == "-n")
            {
                if (val != "")
                    return val;
                else
                    return ": Name is not defined.";
            }
            else if (s == "-p")
            {
                if (val == "")
                    return "0";
                else
                {
                    try
                    {
                        int ps = Convert.ToInt32(val);
                        return ps.ToString();
                    }
                    catch (Exception e)
                    {
                        return ": Invalid pagesize - " + val + ":" + e.Message;
                    }
                }
            }
            else if (s == "-s")
            {
                if (val == "")
                    return ": Size is not defined";
                else
                {
                    try
                    {
                        int ps = Convert.ToInt32(val);
                        if (ps < 1)
                            ps = 1;
                            return ps.ToString();
                    }
                    catch (Exception e)
                    {
                        return ": Invalid size - " + val + ":" + e.Message;
                    }
                }
            }
            else if (s == "-e")
            {
                if (val == "")
                    return "0";
                else
                {
                    try
                    {
                        int ps = Convert.ToInt32(val);
                        if (ps < 0)
                            ps = 0;
                        return ps.ToString();
                    }
                    catch (Exception e)
                    {
                        return ": Invalid extension - " + val + ":" + e.Message;
                    }
                }

            }
            return ": Parameter is not recognized - " + s;
        }
    }
}
