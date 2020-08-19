using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using VStorage;

namespace VSUILib
{

    public static class VSUICommonFunctions
    {

        public const string CANCELLED = "\t$$\t";

        /// <summary>
        /// Get path
        /// </summary>
        /// <param name="key"></param>
        /// <param name="title"></param>
        /// <returns></returns>
        public static string SelectPath(string key, string title)
        {

            FolderBrowserDialog d = new FolderBrowserDialog();

            d.SelectedPath = VSLib.VSGetKey(key);

            d.Description = title + ((d.SelectedPath == "") ? "" : " (default: '" + d.SelectedPath + "')");

            DialogResult result = d.ShowDialog();
            if ((result != DialogResult.Cancel) & (d.SelectedPath != ""))
            {
                VSLib.VSSetKey(key, d.SelectedPath);
                return d.SelectedPath;
            }
            else
                return "";
        }

        /// <summary>
        /// Get file
        /// </summary>
        /// <param name="key"></param>
        /// <param name="title"></param>
        /// <returns></returns>
        public static string SelectFile(string key, string title, string filter)
        {

            OpenFileDialog d = new OpenFileDialog();
            d.Filter = filter;

            d.Title = "Select file";

            d.InitialDirectory = VSLib.VSGetKey(key);

            DialogResult result = d.ShowDialog();

            if ((result != DialogResult.Cancel) & (d.FileName != ""))
            {
                
                //File.WriteAllText(key, Path.GetDirectoryName(d.FileName));
                VSLib.VSSetKey(key, Path.GetDirectoryName(d.FileName));
                return d.FileName;
            }
            else
                return "";
        }

        /// <summary>
        /// Input value (numeric or string)
        /// </summary>
        /// <param name="title"></param>
        /// <param name="caption"></param>
        /// <param name="default_value"></param>
        /// <param name="numeric"></param>
        /// <returns></returns>
        public static string InputBox(string title, string caption, string value = "", bool numeric = false)
        {
            VSInputBox d = new VSInputBox(title, caption, value, numeric);
            DialogResult res = d.ShowDialog();

            if (res != DialogResult.OK)
                return CANCELLED;
            else
            {
                if (numeric)
                {
                    int ret_int = d.VALUE_INT;
                    return ret_int.ToString();
                }
                else
                    return d.VALUE_STRING;
            }
        }

        public static void DisplayText(string title, string text, int X = -1, int Y = -1)
        {
            VSViewText d = new VSViewText(title, text);
            d.Show();
            if ((X >= 0) & (Y >= 0))
            {
                d.Top = Cursor.Position.Y;
                d.Left = Cursor.Position.X;
            }
        }
    }
}
