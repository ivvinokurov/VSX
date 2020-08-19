using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using VStorage;

namespace VSUILib
{

    public class VSUIControl
    {
        public VSUIControl(string name)
        {
            this.v_name = name.ToLower().Trim();
        }
        public const string ALIGN_LEFT = "left";
        public const string ALIGN_RIGHT = "right";
        public const string ALIGN_CENTER = "center";

        public const string CONTROL_TYPE_TEXT = "text";             // Text box
        public const string CONTROL_TYPE_LIST = "list";             // Lixt box
        public const string CONTROL_TYPE_COMBO = "combo";           // Combo box
        public const string CONTROL_TYPE_COMBOLIST = "combolist";   // Combo box (fixed list)
        public const string CONTROL_TYPE_LABEL = "label";           // Label
        public const string CONTROL_TYPE_CHECK = "check";           // Check box
        public const string CONTROL_TYPE_ROW = "row";               // New row
        public const string CONTROL_TYPE_EXT = "ext";               // External control
        public const string CONTROL_TYPE_BUTTON = "button";         // Button 
        public const string CONTROL_TYPE_IMAGE = "image";           // Button 

        public static string[] ControlTypes = { 
                                                   CONTROL_TYPE_TEXT, CONTROL_TYPE_LIST, 
                                                   CONTROL_TYPE_COMBO, CONTROL_TYPE_COMBOLIST,
                                                   CONTROL_TYPE_LABEL, CONTROL_TYPE_CHECK, 
                                                   CONTROL_TYPE_ROW, CONTROL_TYPE_EXT,
                                                   CONTROL_TYPE_BUTTON, CONTROL_TYPE_IMAGE
                                               };

        public const string NOVALUE = "$$novalue$$";

        public const string TAGVALUE = "$$vsui$$";
        
        /// <summary>
        /// FIELDS
        /// </summary>

        // PUBLICL FIELDS
        public string ControlType = NOVALUE;
        //public string Value = NOVALUE;                         // value
        public string ListItems = NOVALUE;                     // list/combo - ','/';' separated
        public int SelectedIndex = -1;                         // ListBox/ComboBox index
        public string Caption = NOVALUE;
        public string Tag = NOVALUE;                           // User-defined tag field


        
        // INTERNAL FIELDS
        internal int Row = 0;
        internal int Column = 0;
        public System.Windows.Forms.Control Control = null;
        internal System.Windows.Forms.Control CaptionControl = null;

        internal string Font = "Calibri";
        internal float FontSize = 11;
        internal int Space = 0;                                // Additional space before the row (row) 
        public int Heigh = 0;
        public int Width = 0;

        internal string GroupId = "";                           // Group id


        internal bool Bold
        {
            get { return (v_bold == "true"); }
            set { v_bold = value ? "true" : "false"; }
        }
        private string v_bold = NOVALUE;

        internal bool Italic
        {
            get { return (v_italic == "true"); }
            set { v_italic = value ? "true" : "false"; }
        }
        private string v_italic = NOVALUE;



        internal string Align = NOVALUE;                    // left/right/center

        internal bool MultiLine                                // Textbox   
        {
            get { return (v_multiline == "true"); }
            set { v_multiline = value ? "true" : "false"; }
        }
        private string v_multiline = NOVALUE;

        /// <summary>
        /// Readonly property
        /// </summary>
        public bool ReadOnly
        {
            get { return v_readonly == "true"; }
            set { v_readonly = value ? "true" : "false"; }
        }
        internal string v_readonly = NOVALUE;

        /// <summary>
        /// Visible property
        /// </summary>
        public bool Visible
        {
            get { return !(v_visible == "false"); }
            set { v_visible = value ? "true" : "false"; }
        }
        internal string v_visible = NOVALUE;

        /// <summary>
        /// Enabled property
        /// </summary>
        public bool Enabled
        {
            get { return !(v_enabled == "false"); }
            set { v_enabled = value ? "true" : "false"; }
        }
        internal string v_enabled = NOVALUE;

        /// <summary>
        /// Resize property (ext control only)
        /// </summary>
        public bool Resize
        {
            get { return !(v_resize == "false"); }
            set { v_resize = value ? "true" : "false"; }
        }
        internal string v_resize = NOVALUE;

        /// <summary>
        /// Locked property (ext control only)
        /// </summary>
        public bool Locked
        {
            get { return (v_locked == "true"); }
            set { v_locked = value ? "true" : "false"; }
        }
        internal string v_locked = NOVALUE;

        /// <summary>
        /// 'Checked' propery (check box)
        /// </summary>
        public bool Checked                                   
        {
            get { return (v_checked == "true"); }
            set { v_checked = value ? "true" : "false"; }
        }
        internal string v_checked = NOVALUE;                       // Internal representation for 'Checked'

        /// <summary>
        /// Image property
        /// </summary>
        public Image Image
        {
            get { return (v_image); }
            //set { v_image = value; }
        }
        internal Image v_image = null;                             // Internal representation for 'Image'

        /// <summary>
        /// Image data property. If (value.length == 0) - delete image
        /// </summary>
        public byte[] ImageData
        {
            get { return (v_image_data); }
            set 
            {
                if (value != null)
                {
                    if (value.Length == 0)
                    {
                        v_image_data = null;
                        v_image = null;
                    }
                    else
                    {
                        v_image_data = value;
                        Stream ms = new MemoryStream(v_image_data);
                        v_image = Image.FromStream(ms);
                    }
                }
            }
        }
        internal byte[] v_image_data = null;                       // Internal representation for 'Image'

        /// <summary>
        /// Display current image
        /// </summary>
        public void DisplayImage(int width)
        {
            if ((this.ControlType != CONTROL_TYPE_IMAGE) | (this.Image == null))
                return;
            ((PictureBox)(this.Control)).Image = this.Image;
            if (width > 0)
            {
                this.Width = width;
                this.Heigh = (int)((double)this.Width / ((double)this.Image.Width / (double)this.Image.Height));
            }
            else
            {
                this.Width = this.Heigh = 1;
            }
            this.Visible = (width > 0);
        }

        /// <summary>
        /// Image scale - % of the panel width
        /// </summary>
        public int Scale
        {
            get { return v_scale; }
            set
            {
                if ((value > 0) & (value <= 100))
                    v_scale = value;
            }
        }
        internal int v_scale = 10;



        // Public name 
        public string Name
        {
            get { return v_name; }
        }
        internal string v_name = NOVALUE;

        /// <summary>
        /// Value property
        /// </summary>
        public string Value
        {
            get { return v_value; }
            set 
            {
                v_old_value = (v_old_value == NOVALUE) ? value : v_value;

                v_value = value; 
            }
        }
        internal string v_value = NOVALUE;                       // Internal representation for 'Value'

        /// <summary>
        /// OldValue property
        /// </summary>
        public string OldValue
        {
            get { return v_old_value; }
        }
        internal string v_old_value = NOVALUE;                   // Internal representation for 'OldValue'

        /// <summary>
        /// Revert value
        /// </summary>
        public void Revert()
        {
            v_value = v_old_value;
        }
    }
}
