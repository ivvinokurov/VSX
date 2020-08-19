using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using VStorage;
using VXML;
using System.IO;
using System.Drawing;

namespace VSUILib
{
    public class VSUIPanel
    {
        private VSUIControl[] data = null;
        public Panel PN = null;
        private string vsui_template = "";

        // For display method
        private int top_position = 0;
        private int row_h = 0;
        private int capt_h = 0;
        private int n_cols = 0;
        private int col_width = 0;
        // For left/Right align
        private int col_position = 0; // Current position to allocate control


        private bool DISPLAYED = false;


        public Object Tag = null;                   // User tag for panel

        public string Name = "";                    // User-defined name tag for panel

        public string Id = "";                      // Panel id
        
        public string DefaultFont = "Calibri";
        
        public float DefaultFontSize = 11;


        public VSUIPanel(Panel panel, string id)
        {
            this.PN = panel;
            this.Id = id;
            PN.AutoScroll = true;
            visible = true; // panel.Visible;
        }

        /// <summary>
        /// Load panel template from file
        /// </summary>
        public string LoadFromFile(string template)
        {
            string error = "";
            string s = "";
            VSIO IO = null;
            try
            {
                IO = new VSIO(template, VSIO.FILE_MODE_OPEN, "");
                s = IO.ReadString((long)0, (int)IO.GetLength());
            }
            catch (Exception e)
            {
                error = "Error: " + e.Message;
            }

            if (IO != null)
                IO.Close();

            if (error != "")
                return "Error: " + error;

            return Load(s);
        }


        /// <summary>
        /// Display panel (file template)
        /// </summary>
        public void DisplayFromFile(string template)
        {
            string s = LoadFromFile(template);

            if (s != "")
                MessageBox.Show(s);
            else
                Display();
        }

        /// <summary>
        /// Load panel template (string)
        /// </summary>
        public string Load(string template)
        {
            vsui_template = template;
            string s = this.parse();
            if (s != "")
            {
                data = null;
                s = "Error: " + s;
            }
            DISPLAYED = false;
            return s;
        }


        /// <summary>
        /// Display panel (string template)
        /// </summary>
        public void Display(string template)
        {
            string s = this.Load(template);

            if (s != "")
            {
                MessageBox.Show(s);
                    return;
            }
            this.Display();
        }

        /// <summary>
        /// Display parsed template
        /// </summary>
        public void Display()
        {
            if (data == null)
                MessageBox.Show("Template is not loaded!");
            else
                this.display(false);
        }

        /// <summary>
        /// Add external control
        /// </summary>
        /// <param name="c"></param>
        public void AddControl(string name, Control c)
        {
            int i = get_control_index_by_name(name);
            if (i < 0)
                return;
            data[i].Control = c;

            data[i].Control.TabIndex = i; 

            if (c.Parent != PN)
                c.Parent = PN;
            if (!data[i].Locked)
                resize_control(data[i]);
        }

        /// <summary>
        /// Get Control object
        /// </summary>
        /// <param name="name"></param>
        public VSUIControl GetControl(string name)
        {
            int i = get_control_index_by_name(name);

            return (i < 0) ? null : data[i];
        }

        /// <summary>
        /// Display constants for panel filling
        /// </summary>
        private const int PS_ROW_SPACE = 0;            //  Default space between rows (if not specified in the "row: attribute
        private const int PS_COL_SPACE = 2;            //  Default space between columns
        private const int PS_BND_SPACE = 2;            //  Space on the left and right of the row

        /// <summary>
        /// Display panel when template is parsed
        /// </summary>
        private void display(bool resize)
        {
            int max_ctrl_heigh = 0;         // Maximum control heigh in the row
            System.Drawing.Color[] COLORS = new System.Drawing.Color[2];

            System.Drawing.Color BUTTON_COLOR = System.Drawing.Color.LightGray;//.YellowGreen;
            System.Drawing.Color BUTTON_FONT_COLOR = System.Drawing.Color.Blue;//.Red;//.YellowGreen;

            System.Drawing.Color PANEL_COLOR = System.Drawing.Color.LightGray;

            //PN.BackColor = PANEL_COLOR;

            COLORS[0] = System.Drawing.Color.White;
            COLORS[1] = System.Drawing.Color.White;

            string current_group = "";              // Current group
            int current_color = 0;                  // Current color
            VSUIControl ROW = null;

            if (!resize)
            {
                for (int i = 0; i < PN.Controls.Count; i++)
                {
                    if (PN.Controls[i].Tag != null)
                    {
                        string s = (string)PN.Controls[i].Tag;
                        if (s.Length >= VSUIControl.TAGVALUE.Length)
                        {
                            if (s.Substring(0, VSUIControl.TAGVALUE.Length) == VSUIControl.TAGVALUE)
                                PN.Controls.RemoveAt(i);
                        }
                    }
                }

                DISPLAYED = true;
            }
            else
            {
                if (!DISPLAYED)
                    return;
            }

            int row = 0;
            int row_start = -1;
            int row_end = 1;
            int idx = 0;
            
            this.top_position = PS_ROW_SPACE;


            while (idx < data.Length)
            {
                row_start = idx;
                if (data[idx].ControlType == VSUIControl.CONTROL_TYPE_ROW)
                {
                    top_position += (data[idx].Heigh > 0) ? data[idx].Heigh : PS_ROW_SPACE;    // + space before row or delim_size if not defined

                    ROW = data[idx];

                    if (data[idx].GroupId != current_group)
                    {
                        current_color = (current_color == 0) ? 1 : 0;
                        current_group = data[idx].GroupId;
                    }
                    idx++;
                }
                else
                {
                    while (idx < data.Length)
                    {
                        if (data[idx].Row == row)
                            row_end = idx;
                        else
                            break;
                        idx++;
                    }

                    // Display row
                    this.row_h = 0;
                    this.capt_h = 0;
                    this.n_cols = row_end - row_start + 1;

                    if (n_cols > 0)
                    {
                        this.col_width = (this.Width - (PS_BND_SPACE * 2) - ((n_cols - 1) * PS_COL_SPACE)) / n_cols;
                        max_ctrl_heigh = 0;      

                        for (int i = 0; i < n_cols; i++)
                        {
                            int item_number = i + row_start;
                            FontStyle style = new FontStyle();
                            if (data[item_number].Bold)
                                style = style | FontStyle.Bold;
                            if (data[item_number].Italic)
                                style = style | FontStyle.Italic;

                            // Display caption if required
                            if ((data[item_number].Caption.Trim() != "") & (data[item_number].Caption != VSUIControl.NOVALUE) &
                                (data[item_number].ControlType != VSUIControl.CONTROL_TYPE_BUTTON) & (data[item_number].ControlType != VSUIControl.CONTROL_TYPE_LABEL))
                            {
                                Label l = null;
                                if (resize)
                                    l = (Label)data[item_number].CaptionControl;
                                else
                                {
                                    data[item_number].CaptionControl = new Label();
                                    l = (Label)data[item_number].CaptionControl;

                                    l.AutoSize = false;
                                    l.Text = data[item_number].Caption;

                                    l.Font = new System.Drawing.Font(data[item_number].Font, DefaultFontSize, style);

                                    //l.Font = new System.Drawing.Font(data[item_number].Font, data[item_number].FontSize, style);

                                    l.Parent = PN;

                                    l.Tag = "~" + data[item_number].Name;


                                    if (data[item_number].ControlType == VSUIControl.CONTROL_TYPE_LABEL)
                                        l.ForeColor = System.Drawing.Color.DarkRed;
                                    else
                                        l.ForeColor = System.Drawing.Color.Navy;

                                    if (data[item_number].Align == VSUIControl.ALIGN_RIGHT)
                                        l.TextAlign = System.Drawing.ContentAlignment.BottomRight;
                                    else if (data[item_number].Align == VSUIControl.ALIGN_LEFT)
                                        l.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
                                    else if (data[item_number].Align == VSUIControl.ALIGN_CENTER)
                                        l.TextAlign = System.Drawing.ContentAlignment.BottomCenter;

                                }
                                if (l != null)
                                {
                                    l.Top = top_position;

                                    if (l.Height > capt_h)
                                        capt_h = l.Height;
                                }
                                data[item_number].CaptionControl.Visible = data[item_number].Visible;
                            }

                            // LABEL - display only caption
                            if (!resize)
                            {
                                if (data[item_number].ControlType == VSUIControl.CONTROL_TYPE_LABEL)
                                {
                                    Label l = new Label();
                                    data[item_number].Control = l;
                                    l.Parent = PN;
                                    l.AutoSize = true;

                                    l.Text = data[item_number].Caption;

                                    l.Font = new System.Drawing.Font(data[item_number].Font, data[item_number].FontSize, style);

                                    l.Tag = "~" + data[item_number].Name;

                                    l.ForeColor = System.Drawing.Color.DarkRed;

                                    if (data[item_number].Align == VSUIControl.ALIGN_RIGHT)
                                        l.TextAlign = System.Drawing.ContentAlignment.BottomRight;
                                    else if (data[item_number].Align == VSUIControl.ALIGN_LEFT)
                                        l.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
                                    else if (data[item_number].Align == VSUIControl.ALIGN_CENTER)
                                        l.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
                                }

                                // EXT - External Control
                                else if (data[item_number].ControlType == VSUIControl.CONTROL_TYPE_EXT)
                                {
                                }
                                // TEXT Control
                                else if (data[item_number].ControlType == VSUIControl.CONTROL_TYPE_TEXT)
                                {
                                    TextBox t = new TextBox();
                                    data[item_number].Control = t;
                                    t.Parent = PN;

                                    //t.AutoSize = true;
                                    t.Multiline = data[item_number].MultiLine;

                                    t.Text = (data[item_number].Value == VSUIControl.NOVALUE) ? "" : data[item_number].Value;
                                    //if (data[item_number].Value != VSUIControl.NOVALUE)
                                    //    t.Text = data[item_number].Value;


                                    if (data[item_number].Align == VSUIControl.ALIGN_RIGHT)
                                        t.TextAlign = HorizontalAlignment.Right;
                                    else if (data[item_number].Align == VSUIControl.ALIGN_LEFT)
                                        t.TextAlign = HorizontalAlignment.Left;
                                    else if (data[item_number].Align == VSUIControl.ALIGN_CENTER)
                                        t.TextAlign = HorizontalAlignment.Center;

                                    t.Font = new System.Drawing.Font(data[item_number].Font, data[item_number].FontSize, style);

                                    if (t.Multiline)
                                    {
                                        t.Height *= 2;
                                        t.ScrollBars = ScrollBars.Both;
                                    }

                                    t.ReadOnly = data[item_number].ReadOnly;

                                    if (t.ReadOnly)
                                        t.ForeColor = System.Drawing.Color.Maroon;
                                }
                                //LISTBOX / COMBOBOX
                                else if (is_list_control(data[item_number].ControlType))
                                {
                                    // Create box
                                    ListControl box;
                                    string[] items = VSLib.Parse(data[item_number].ListItems.Trim(), "/,;");
                                    if (data[item_number].ControlType == VSUIControl.CONTROL_TYPE_LIST)
                                    {
                                        data[item_number].Control = new ListBox();
                                        ListBox b = (ListBox)data[item_number].Control;
                                        b.Items.AddRange(items);
                                        if (data[item_number].SelectedIndex < b.Items.Count)
                                            b.SelectedIndex = data[item_number].SelectedIndex;
                                        
                                        box = b;
                                    }
                                    else
                                    {
                                        data[item_number].Control = new ComboBox();
                                        ComboBox b = (ComboBox)data[item_number].Control;
                                        b.Items.AddRange(items);
                                        if (data[item_number].SelectedIndex < b.Items.Count)
                                            b.SelectedIndex = data[item_number].SelectedIndex;


                                        //b.Enabled = !data[item_number].ReadOnly;

                                        if (data[item_number].ControlType == VSUIControl.CONTROL_TYPE_COMBOLIST)
                                            b.DropDownStyle = ComboBoxStyle.DropDownList;
                                        else
                                            b.DropDownStyle = ComboBoxStyle.DropDown;

                                        box = b;

                                    }

                                    box.Parent = PN;
                                    box.AutoSize = false;

                                    if (data[item_number].Value != VSUIControl.NOVALUE)
                                        box.Text = data[item_number].Value;

                                    box.Font = new System.Drawing.Font(data[item_number].Font, data[item_number].FontSize, style);
                                }
                                // CHECKBOX
                                else if (data[item_number].ControlType == VSUIControl.CONTROL_TYPE_CHECK)
                                {
                                    CheckBox cb = new CheckBox();
                                    data[item_number].Control = cb;
                                    cb.Parent = PN;
                                    cb.AutoSize = true;
                                    cb.Text = (data[item_number].Value == VSUIControl.NOVALUE) ? "" : data[item_number].Value;
                                    cb.Checked = data[item_number].Checked;
                                    cb.Font = new System.Drawing.Font(data[item_number].Font, data[item_number].FontSize, style);
                                }

                                else if (data[item_number].ControlType == VSUIControl.CONTROL_TYPE_BUTTON)
                                {
                                    Button btn = new Button();
                                    data[item_number].Control = btn;
                                    btn.Parent = PN;
                                    btn.AutoSize = true;

                                    btn.BackColor = BUTTON_COLOR;
                                    btn.ForeColor = BUTTON_FONT_COLOR;

                                    btn.FlatAppearance.BorderSize = 5;

                                    btn.Text = data[item_number].Caption;

                                    btn.Font = new System.Drawing.Font(data[item_number].Font, data[item_number].FontSize, style);
                                }
                                    // IMAGE
                                else if (data[item_number].ControlType == VSUIControl.CONTROL_TYPE_IMAGE)
                                {
                                    PictureBox pcb = new PictureBox();
                                    data[item_number].Control = pcb;
                                    pcb.Parent = PN;
                                    pcb.AutoSize = false;
                                    pcb.BackColor = PN.BackColor;
                                    pcb.SizeMode = PictureBoxSizeMode.Zoom;
                                    //pcb.Image = data[item_number].Image;
                                }

                            }
                            resize_control(data[item_number]);

                            if (data[item_number].Control != null)
                            {
                                data[item_number].Control.Visible = data[item_number].Visible;
                                data[item_number].Control.Enabled = data[item_number].Enabled;

                                if ((!resize) & (data[item_number].ControlType != VSUIControl.CONTROL_TYPE_EXT))
                                    data[item_number].Control.Tag = VSUIControl.TAGVALUE + this.Id;
                                //if (data[item_number].Control.Visible)
                                    if (data[item_number].Control.Height > max_ctrl_heigh)
                                        max_ctrl_heigh = data[item_number].Control.Height;
                            }

                            if ((!resize) & (data[item_number].Control != null))
                            {
                                if ((data[item_number].ControlType != VSUIControl.CONTROL_TYPE_ROW) & (data[item_number].ControlType != VSUIControl.CONTROL_TYPE_EXT))
                                    data[item_number].Control.Name = data[item_number].Name;
                            }
                            if (data[item_number].Control != null)
                                if ((data[item_number].ControlType != VSUIControl.CONTROL_TYPE_LABEL) & (data[item_number].ControlType != VSUIControl.CONTROL_TYPE_BUTTON))
                                    data[item_number].Control.BackColor = COLORS[current_color];
                        }
                    }
                    if (ROW != null)
                    {
                        if (ROW.Align == VSUIControl.ALIGN_LEFT)
                        {
                            col_position = PS_BND_SPACE;
                            for (int i = 0; i < n_cols; i++)
                            {
                                int j = row_start + i;
                                if (data[j].Visible)
                                {
                                    if (data[j].CaptionControl != null)
                                    {
                                        data[j].CaptionControl.Left = col_position;
                                    }
                                    if (data[j].Control != null)
                                    {
                                        data[j].Control.Left = col_position;
                                        col_position += (data[j].Control.Width + PS_COL_SPACE);
                                    }
                                }
                            }
                        }
                        else if (ROW.Align == VSUIControl.ALIGN_RIGHT)
                        {
                            col_position = PN.Width - PS_BND_SPACE - data[row_start + n_cols - 1].Control.Width;

                            for (int i = n_cols; i > 0; i--)
                            {
                                int j = row_start + i - 1;
                                if (data[j].Visible)
                                {

                                    if (data[j].CaptionControl != null)
                                    {
                                        data[j].CaptionControl.Left = col_position;
                                    }
                                    if (data[j].Control != null)
                                    {
                                        data[j].Control.Left = col_position;
                                        if (i > 1)
                                            col_position -= (data[j - 1].Control.Width + PS_COL_SPACE);
                                    }
                                }
                            }
                            if ((data[row_start].Visible) & (data[row_start].Control != null))
                            {
                                int sp = data[row_start].Control.Left;
                                data[row_start].Control.Left = PS_BND_SPACE;
                                data[row_start].Control.Width += sp - PS_BND_SPACE;
                            }
                            if ((data[row_start].Visible) & (data[row_start].CaptionControl != null))
                            {
                                int sp = data[row_start].CaptionControl.Left;
                                data[row_start].CaptionControl.Left = PS_BND_SPACE;
                                data[row_start].CaptionControl.Width += sp - PS_BND_SPACE;
                            }
                        }
                    }

                    // Adjust row heigh
                    top_position += (capt_h + row_h + PS_ROW_SPACE);

                    row++;
                }
            }
        }


        /// <summary>
        /// Get control by name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public VSUIControl Get(string name)
        {
            return Get(get_control_index_by_name(name));
        }

        /// <summary>
        /// Get control by index
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public VSUIControl Get(int index)
        {
            if (!DISPLAYED)
                return null;
            if ((index < 0) | (index >= data.Length))
                return null;

            Read(index);

            VSUIControl c = new VSUIControl(data[index].Name);
            c.ControlType = data[index].ControlType;
            c.Caption = data[index].Caption;
            c.ListItems = data[index].ListItems;
            c.SelectedIndex = data[index].SelectedIndex;
            c.Value = data[index].Value;
            return c;
        }

        /// <summary>
        /// Bulk get
        /// </summary>
        /// <returns></returns>
        public VSUIControl[] Get()
        {
            Read();
            List<VSUIControl> l = new List<VSUIControl>();
            if (DISPLAYED)
            {
                for (int i = 0; i < data.Length; i++)
                {
                    if (data[i].ControlType != VSUIControl.CONTROL_TYPE_ROW)
                    {
                        VSUIControl c = Get(i);
                        if (c != null)
                            l.Add(c);
                    }
                }
            }
            return l.ToArray();
        }

        /// <summary>
        /// Read data from controls
        /// </summary>
        public void Read()
        {
            if (!DISPLAYED)
                return;

            for (int i = 0; i < data.Length; i++)
            {
                if (data[i].Control != null)
                {
                    data[i].Value = data[i].Control.Text;
                    if (data[i].ControlType == VSUIControl.CONTROL_TYPE_LIST) 
                        data[i].SelectedIndex = ((ListControl)data[i].Control).SelectedIndex;
                }
            }
        }

        /// <summary>
        /// Read data from controls
        /// </summary>
        public void Read(int index)
        {
            if (!DISPLAYED)
                return;
            if ((index < 0) | (index >= data.Length))
                return;
            if (data[index].Control != null)
            {
                data[index].Value = data[index].Control.Text;
                if (data[index].ControlType == VSUIControl.CONTROL_TYPE_LIST)
                    data[index].SelectedIndex = ((ListControl)data[index].Control).SelectedIndex;
            }
        }


        /// <summary>
        /// Refresh all controls data
        /// </summary>
        public void Refresh()
        {
            if (!DISPLAYED)
                return;

            for (int i = 0; i < data.Length; i++)
                    refresh_control(i);
        }

        /// <summary>
        /// Refresh controls data by index
        /// </summary>
        public void Refresh(string name)
        {
            int i = get_control_index_by_name(name);
            if (i < 0)
                return;

            refresh_control(i);
        }

        /// <summary>
        /// Set focus by name
        /// </summary>
        public void SetFocus(string name)
        {
            if (!DISPLAYED)
                return;

            int i = get_control_index_by_name(name);
            if (i < 0)
                return;

            if (data[i].Control != null)
                data[i].Control.Focus();
        }

        /// <summary>
        /// Load panel XML definition
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private string parse()
        {
            VXMLTemplate t = VXmlParser.Parse(vsui_template, "nm1");
            if (t[0].def == VXmlParser.DEF_ERROR)
                return t[0].value;

            this.data = null;

            List<VSUIControl> dlist = new List<VSUIControl>();

            if (t.Count == 0)
                return "Empty content";

            VSUIControl ctrl = null;
            bool default_bold = false;
            bool default_italic = false;

            bool root = true;
            string error = "";

            if (t[0].name != "pn")
                return "Invalid root tag " + t[0].name;

            int row_count = -1;
            int col_count = -1;
            for (int i = 0; i < t.Count; i++)
            {
                error = "";
                if (t[i].def == VXmlParser.DEF_START)
                {
                    if (t[i].name == "pn")
                        root = true;
                    else
                    {
                        if (!is_valid_control_type(t[i].name))
                            error = "Unrecognized tag name '" + t[i].name + "'";
                        else
                        {
                            ctrl = new VSUIControl(VSUIControl.NOVALUE);
                            ctrl.Font = DefaultFont;
                            ctrl.FontSize = DefaultFontSize;
                            ctrl.Bold = default_bold;
                            ctrl.Italic = default_italic;
                            dlist.Add(ctrl);
                            ctrl.ControlType = t[i].name;
                            if (t[i].name == "row")
                            {
                                ctrl.Align = VSUIControl.ALIGN_CENTER;
                                row_count++;
                                col_count = -1;
                            }
                            else
                            {
                                ctrl.Align = VSUIControl.ALIGN_LEFT;
                                col_count++;
                                ctrl.Column = col_count;
                            }
                            if (row_count < 0)
                                row_count++;

                            ctrl.Row = row_count;

                            root = false;
                        }
                    }
                }
                else if (t[i].def == VXmlParser.DEF_END)
                {
                    if (t[i].name == "pn")
                        root = false;
                    else
                    {
                        if (ctrl.Name == VSUIControl.NOVALUE)
                        {
                            if ((ctrl.ControlType == VSUIControl.CONTROL_TYPE_TEXT) | (ctrl.ControlType == VSUIControl.CONTROL_TYPE_EXT) | is_list_control(ctrl.ControlType))
                                error = "'name' is missing for text/list/combo/combolist/ext";
                        }
                    }
                }
                else if (t[i].def == VXmlParser.DEF_ATTRIBUTE)
                {
                    if (t[i].name == "font")
                    {
                        if (root)
                            DefaultFont = t[i].value;
                        else
                            ctrl.Font = t[i].value; ;
                    }
                    else if (t[i].name == "fontsize")
                    {
                        double d = 0;
                        if (get_double(t[i].value, ref d))
                        {
                            if (root)
                                DefaultFontSize = (float)d;
                            else
                                ctrl.FontSize = (float)d;
                        }
                        else
                            error = "Invalid font size: " + t[i].value;
                    }
                    else if (t[i].name == "space")
                    {
                        if ((ctrl == null) | ((ctrl != null) & (ctrl.ControlType != VSUIControl.CONTROL_TYPE_ROW)))
                            error = "'Space' is applicable for 'row' tag only";
                        else
                        {
                            double d = 0;
                            if (get_double(t[i].value, ref d) & (d >= 0))
                                ctrl.Space = (int)d;
                            else
                                error = "Invalid space value: " + t[i].value;
                        }
                    }
                    else if (t[i].name == "name")
                    {
                        if (ctrl != null)
                        {
                            bool fnd = false;
                            for (int j = 0; j < dlist.Count; j++)
                            { 
                                if (dlist[j].v_name==t[i].value.Trim().ToLower())
                                {
                                    fnd = true;
                                    break;
                                }
                            }
                            if (fnd)
                                error = "Duplicate control name - '" + t[i].value + "'";
                            else
                                ctrl.v_name = t[i].value.Trim().ToLower();
                        }
                    }
                    else if (t[i].name == "caption")
                    {
                        if (ctrl != null)
                            ctrl.Caption = t[i].value;
                    }
                    else if (t[i].name == "bold")
                    {
                        if (root)
                            default_bold = (t[i].value == "true");
                        else
                            ctrl.Bold = get_bool(t[i].value);
                    }
                    else if (t[i].name == "italic")
                    {
                        if (root)
                            default_italic = (t[i].value == "true");
                        else
                            ctrl.Italic = get_bool(t[i].value);
                    }
                    else if (t[i].name == "multiline")
                    {
                        if (!root)
                        {
                            if (ctrl.ControlType == VSUIControl.CONTROL_TYPE_TEXT)
                                ctrl.MultiLine = get_bool(t[i].value);
                            else
                                error = "'multiline' is applicable to text control only.";
                        }
                    }
                    else if (t[i].name == "checked")
                    {
                        if (!root)
                        {
                            if (ctrl.ControlType == VSUIControl.CONTROL_TYPE_CHECK)
                                ctrl.Checked = get_bool(t[i].value);
                            else
                                error = "'checked' is applicable to check box control only.";
                        }
                    }
                    else if (t[i].name == "align")
                    {
                        if (!root)
                        {
                            ctrl.Align = t[i].value.ToLower();
                            if ((ctrl.Align != VSUIControl.ALIGN_LEFT) & (ctrl.Align != VSUIControl.ALIGN_RIGHT) & (ctrl.Align != VSUIControl.ALIGN_CENTER))
                                error = "Invalid 'align' value: " + t[i].value;
                        }
                    }
                    else if (t[i].name == "value")
                    {
                        if (!root)
                            ctrl.Value = t[i].value;
                    }
                    else if (t[i].name == "heigh")
                    {
                        double d = 0;
                        if (get_double(t[i].value, ref d) & (d >= 0))
                            ctrl.Heigh = (int)d;
                        else
                            error = "Invalid heigh value: " + t[i].value;
                    }
                    else if (t[i].name == "width")
                    {
                        double d = 0;
                        if (get_double(t[i].value, ref d) & (d >= 0))
                            ctrl.Width = (int)d;
                        else
                            error = "Invalid heigh value: " + t[i].value;
                    }
                    else if (t[i].name == "lines")
                    {
                        if (ctrl.ControlType == VSUIControl.CONTROL_TYPE_TEXT)
                        {
                            double d = 0;
                            if (get_double(t[i].value, ref d) & (d >= 0))
                                ctrl.Heigh = (int)d;
                            else
                                error = "Invalid lines value: " + t[i].value;
                        }
                        else
                            error = "'lines' is applicable only for text";
                    }
                    else if (t[i].name == "list")
                    {
                        if (is_list_control(ctrl.ControlType))
                            ctrl.ListItems = t[i].value;
                        else
                            error = "'list' is applicable only for list or combo/combolist";
                    }
                    else if (t[i].name == "index")
                    {
                        if (is_list_control(ctrl.ControlType))
                        {
                            double d = 0;
                            if (get_double(t[i].value, ref d) & (d >= 0))
                                ctrl.SelectedIndex = (int)d;
                            else
                                error = "Invalid index value: " + t[i].value;
                        }
                        else
                            error = "'index' is applicable only for list or combo";
                    }
                    else if (t[i].name == "readonly")
                    {
                        if (!root)
                            ctrl.ReadOnly = get_bool(t[i].value);
                    }
                    else if (t[i].name == "enabled")
                    {
                        if (!root)
                            ctrl.Enabled = get_bool(t[i].value);
                    }
                    else if (t[i].name == "resize")
                    {
                        if (!root)
                            ctrl.Resize = get_bool(t[i].value);
                    }
                    else if (t[i].name == "visible")
                    {
                        if (!root)
                            ctrl.Visible = get_bool(t[i].value);
                    }
                    else if (t[i].name == "locked")
                    {
                        if (!root)
                            ctrl.Locked = get_bool(t[i].value);
                    }
                    else if (t[i].name == "tag")
                    {
                        if (!root)
                            ctrl.Tag = t[i].value;
                    }
                    else if (t[i].name == "groupid")
                    {
                        if (ctrl.ControlType != VSUIControl.CONTROL_TYPE_ROW)
                            error = "'groupid' is applicable only for row";
                        else
                            ctrl.GroupId = t[i].value;
                    }
                    else if (t[i].name == "scale")
                    {
                        if (ctrl.ControlType != VSUIControl.CONTROL_TYPE_IMAGE)
                            error = "'scale' is applicable only for image";
                        else
                        {
                            if (VSLib.IsNumeric(t[i].value))
                                ctrl.Scale = VSLib.ConvertStringToInt(t[i].value);
                            else
                                ctrl.Scale = 100;
                        }
                    }
                    else if (t[i].name == "imagefile")
                    {
                        if (ctrl.ControlType != VSUIControl.CONTROL_TYPE_IMAGE)
                            error = "'imagefile' is applicable only for 'image' control only";
                        else
                        {
                            byte[] b = null;
                            try
                            {
                                b = File.ReadAllBytes(t[i].value);
                            }
                            catch (Exception e)
                            {
                                error = "'imagefile' - load error: " + e.Message;
                            }

                            if (error == "")
                                ctrl.ImageData = b;
                        }
                    }

                }
                if (error != "")
                    break;
            }
            if (error != "")
                return error;
            this.data = dlist.ToArray();

            return "";
        }

        /// <summary>
        /// Resize controls
        /// </summary>
        public void Resize()
        {
            if (DISPLAYED)
                display(true);
        }

        /// <summary>
        /// Remove controls
        /// </summary>
        public void Remove()
        {
            this.PN.Parent.Controls.Remove(PN);
            PN = null;
        }


        /// <summary>
        /// Get value by name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public string GetValue(string name)
        {
            VSUIControl c = Get(name);
            return c.Value;
        }

        /// <summary>
        /// Get old value by name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public string GetOldValue(string name)
        {
            VSUIControl c = Get(name);
            return c.OldValue;
        }

        /// <summary>
        /// Set value by name
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public void SetValue(string name, string value)
        {
            VSUIControl c = new VSUIControl(name);
            c.Value = value;
            Set(c);
        }

        /// <summary>
        /// Set image by name
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public void SetImageData(string name, byte[] value)
        {
            VSUIControl c = new VSUIControl(name);
            c.ImageData = value;
            Set(c);
        }


        /// <summary>
        /// Get tag by name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public string GetTag(string name)
        {
            VSUIControl c = Get(name);
            return c.Tag;
        }

        /// <summary>
        /// Set tag by name
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public void SetTag(string name, string value)
        {
            VSUIControl c = new VSUIControl(name);
            c.Tag = value;
            Set(c);
        }

        /// <summary>
        /// Set enabled property by name
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public void SetEnabled(string name, bool value)
        {
            VSUIControl c = new VSUIControl(name);
            c.Enabled = value;
            Set(c);
        }

        /// <summary>
        /// Set readonly property by name
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public void SetReadOnly(string name, bool value)
        {
            VSUIControl c = new VSUIControl(name);
            c.ReadOnly = value;
            Set(c);
        }

        /// <summary>
        /// Set visible property by name
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public void SetVisible(string name, bool value)
        {
            VSUIControl c = new VSUIControl(name);
            c.Visible = value;
            Set(c);
        }

        /// <summary>
        /// Set Width property by name
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public void SetWidth(string name, int value)
        {
            VSUIControl c = new VSUIControl(name);
            c.Width = value;
            Set(c);
        }


        /// <summary>
        /// Set checked
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public void SetChecked(string name, bool value)
        {
            VSUIControl c = new VSUIControl(name);
            c.Checked = value;
            Set(c);
        }

        /// <summary>
        /// Set caption
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public void SetCaption(string name, string value)
        {
            VSUIControl c = new VSUIControl(name);
            c.Caption = value;
            Set(c);
        }

        /// <summary>
        /// Set box list
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public void SetList(string name, string value)
        {
            VSUIControl c = new VSUIControl(name);
            c.ListItems = value;
            Set(c);
        }

        /// <summary>
        /// Get box index
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public int GetSelectedIndex(string name)
        {
            if (!DISPLAYED)
                return -1;

            int i = get_control_index_by_name(name);
            if (i >= 0)
                Read(i);

            return data[i].SelectedIndex;
        }

        /// <summary>
        /// Set box index
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public void SetSelectedIndex(string name, int value)
        {
            if (data != null)
            {
                int i = get_control_index_by_name(name);
                if (i >= 0)
                {
                    data[i].SelectedIndex = value;
                    if (DISPLAYED)
                    {
                        if (is_list_control(data[i].ControlType))
                        {
                            if (data[i].Control != null)
                            {
                                int cnt = (data[i].ControlType == VSUIControl.CONTROL_TYPE_LIST) ? ((ListBox)data[i].Control).Items.Count : ((ComboBox)data[i].Control).Items.Count;
                                if ((value >= 0) & (value < cnt))
                                    ((ListControl)data[i].Control).SelectedIndex = value;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Single set
        /// </summary>
        /// <param name="data">1st - name; 2nd - value</param>
        /// <param name="value"></param>
        /// <returns></returns>
        public void Set(VSUIControl c)
        {
            int idx = get_control_index_by_name(c.Name);

            if (idx < 0)
                return;

            //////// STEP 1 - update metadata //////////
            if (c.Caption != VSUIControl.NOVALUE)
                data[idx].Caption = c.Caption;

            if (data[idx].Caption == "")
                data[idx].Caption = VSUIControl.NOVALUE;

            if (c.ListItems != VSUIControl.NOVALUE)
                data[idx].ListItems = c.ListItems;

            if (c.SelectedIndex >= 0)
                data[idx].SelectedIndex = c.SelectedIndex;

            if (c.Value != VSUIControl.NOVALUE)
                data[idx].Value = c.Value;

            if (c.v_checked != VSUIControl.NOVALUE)
                data[idx].Checked = c.Checked;

            if (c.v_readonly != VSUIControl.NOVALUE)
                data[idx].ReadOnly = c.ReadOnly;

            if (c.v_enabled != VSUIControl.NOVALUE)
                data[idx].Enabled = c.Enabled;

            if (c.v_visible != VSUIControl.NOVALUE)
                data[idx].Visible = c.Visible;

            if (c.Tag != VSUIControl.NOVALUE)
                data[idx].Tag = c.Tag;

            if (c.ImageData != null)
                data[idx].ImageData = c.ImageData;

            if (c.Width > 0)
                data[idx].Width = c.Width;


            //////// STEP 2 - update control //////////
            if (DISPLAYED)
                refresh_control(idx);
        }

        /// <summary>
        /// Bulk set
        /// </summary>
        /// <returns></returns>
        public void Set(VSUIControl[] d)
        {
            for (int i = 0; i < d.Length; i++)
                Set(d[i]);
        }

        /// <summary>
        /// Return double value
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        private bool get_double(string s, ref double d)
        {
            bool rc = true;
            try
            {
                d = Convert.ToDouble(s);
            }
            catch (Exception e)
            {
                rc = false;
            }
            return rc;
        }

        /// <summary>
        /// Return bool value
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        private bool get_bool(string s)
        {
            bool rc = false;

            try
            {
                rc = Convert.ToBoolean(s);
            }
            catch (Exception e)
            {
                rc = false;
            } 
            return rc;
        }

        /// <summary>
        /// Get control index by name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public int get_control_index_by_name(string name)
        {
            if (data == null)
                return -1;

            string s = name.Trim().ToLower();

            for (int i = 0; i < data.Length; i++)
                if (s == data[i].Name)
                    return i;

            return -1;
        }

        /// <summary>
        /// Refresh control by index
        /// </summary>
        /// <param name="i"></param>
        private void refresh_control(int i)
        {
            if (!DISPLAYED)
                return;
            
            // CAPTION
            if (data[i].CaptionControl == null)
            {
                if ((data[i].Caption != VSUIControl.NOVALUE) & (data[i].ControlType != VSUIControl.CONTROL_TYPE_BUTTON))
                {
                    data[i].CaptionControl = new Label();
                    Label l = (Label)data[i].CaptionControl;

                    l.AutoSize = false;
                    l.Text = data[i].Caption;
                    l.Font = new System.Drawing.Font(data[i].Font, data[i].FontSize - 1);

                    if (data[i].ControlType == VSUIControl.CONTROL_TYPE_LABEL)
                        l.ForeColor = System.Drawing.Color.DarkRed;
                    else
                        l.ForeColor = System.Drawing.Color.Navy;

                    l.Parent = PN;
                }
            }
            else
            {
                if (data[i].Caption == VSUIControl.NOVALUE)
                {
                    PN.Controls.Remove(data[i].CaptionControl);
                    data[i].CaptionControl = null;
                }
                else
                    data[i].CaptionControl.Text = data[i].Caption;
            }


            // CONTROL
            if (data[i].Control != null)
            {
                if (data[i].Value != VSUIControl.NOVALUE)
                    data[i].Control.Text = data[i].Value;

                if (data[i].v_visible != VSUIControl.NOVALUE)
                    data[i].Control.Visible = data[i].Visible;

                if (data[i].v_enabled != VSUIControl.NOVALUE)
                    data[i].Control.Enabled = data[i].Enabled;

                if (data[i].Width > 0)
                    data[i].Control.Width = data[i].Width;

                // Control-specific actions
                if (data[i].ControlType == VSUIControl.CONTROL_TYPE_LIST)
                {
                    string[] items = VSLib.Parse(data[i].ListItems.Trim(), "/,;");
                    ((ListBox)data[i].Control).Items.Clear();
                    ((ListBox)data[i].Control).Items.AddRange(items);

                    if (data[i].SelectedIndex >= 0)
                    {
                        if (((ListBox)data[i].Control).Items.Count > data[i].SelectedIndex)
                            ((ListBox)data[i].Control).SelectedIndex = data[i].SelectedIndex;
                    }
                }
                else if ((data[i].ControlType == VSUIControl.CONTROL_TYPE_COMBO) | (data[i].ControlType == VSUIControl.CONTROL_TYPE_COMBOLIST))
                {
                    string[] items = VSLib.Parse(data[i].ListItems.Trim(), "/,;");
                    ((ComboBox)data[i].Control).Items.Clear();
                    ((ComboBox)data[i].Control).Items.AddRange(items);

                    if (data[i].SelectedIndex >= 0)
                    {
                        if (((ComboBox)data[i].Control).Items.Count > data[i].SelectedIndex)
                            ((ComboBox)data[i].Control).SelectedIndex = data[i].SelectedIndex;
                    }
                }
                else if (data[i].ControlType == VSUIControl.CONTROL_TYPE_CHECK)
                {
                    ((CheckBox)data[i].Control).Checked = data[i].Checked;
                }
                else if (data[i].ControlType == VSUIControl.CONTROL_TYPE_BUTTON)
                {
                    ((Button)data[i].Control).Text = data[i].Caption;
                }
                else if (data[i].ControlType == VSUIControl.CONTROL_TYPE_IMAGE)
                {
                    ((PictureBox)data[i].Control).Image = data[i].Image;
                }

                // Update visibility
                if (data[i].CaptionControl != null)
                    data[i].CaptionControl.Visible = data[i].Control.Visible;

            }
        }

        /// <summary>
        /// Resize control
        /// </summary>
        /// <param name="c"></param>
        /// <param name="col"></param>
        private void resize_control(VSUIControl ctrl)
        {
            Control c = ctrl.Control;

            //Scale image
            if ((ctrl.ControlType == VSUIControl.CONTROL_TYPE_IMAGE) & ctrl.Image != null)
            {
                Size s = ctrl.Image.Size;
                double img_scale = (double)s.Width / (double)s.Height;

                double isc = (double)ctrl.Scale / (double)100;

                ctrl.Width = (int)((double)PN.Width * isc);

                ctrl.Heigh = (int)((double)ctrl.Width / img_scale);

            }
            else
            {
                Control cpt = ctrl.CaptionControl;

                c.Left = PS_BND_SPACE + (ctrl.Column * col_width) + (ctrl.Column * PS_COL_SPACE);
                c.Top = top_position + capt_h;

                if (ctrl.Resize & (ctrl.Width <= 0))
                {
                    c.Width = col_width;
                }

                if ((this.Width - (c.Left + c.Width)) < PS_BND_SPACE)
                    c.Width = this.Width - c.Left - PS_BND_SPACE;

                if (cpt != null)
                {
                    ((Label)cpt).AutoSize = false;
                    cpt.Left = c.Left;
                    cpt.Width = c.Width;
                }

            }

            if (ctrl.Heigh > 0)
                c.Height = ctrl.Heigh;

            if (ctrl.Width > 0)
                c.Width = ctrl.Width;


            if (c.Height > row_h)
                row_h = c.Height;
        }

        /// <summary>
        /// Check if the control type is in the list family
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private bool is_list_control(string type)
        {
            return
                (
                (type == VSUIControl.CONTROL_TYPE_LIST) |
                (type == VSUIControl.CONTROL_TYPE_COMBOLIST) |
                (type == VSUIControl.CONTROL_TYPE_COMBO)
                );
        }

        /// <summary>
        /// Check if the control type is in the list family
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private bool is_valid_control_type(string type)
        {
            for (int i = 0; i < VSUIControl.ControlTypes.Length; i++)
                if (VSUIControl.ControlTypes[i] == type)
                    return true;

            return false;
        }


        /// <summary>
        /// Actual panel heigh
        /// </summary>
        public int PanelHeigh
        {
            get
            {
                int sz = 0;
                if (data != null)
                    for (int i = 0; i < data.Length; i++)
                    {
                        if (data[i].Control != null)
                        {
                            if ((data[i].Control.Bottom > sz) & (data[i].Control.Visible))
                                sz = data[i].Control.Bottom;
                        }
                    }
                return sz;
            }
        }

        /// <summary>
        /// Visible property
        /// </summary>
        public bool Visible
        {
            get { return visible; }
            set
            {
                visible = value;
                if (PN != null)
                    PN.Visible = value;
            }

        }
        private bool visible = true;

        /// <summary>
        /// Width property
        /// </summary>
        public int Width
        {
            get { return PN.Width; }
        }

        /// <summary>
        /// Heigh property
        /// </summary>
        public int Heigh
        {
            get { return PN.Height; }
        }

        /// <summary>
        /// Get array of modified controls
        /// </summary>
        public VSUIControl[] ModifiedControls
        {
            get
            {
                Read();
                List<VSUIControl> l = new List<VSUIControl>();
                for (int i = 0; i < data.Length; i++)
                    if (data[i].Value != data[i].OldValue)
                        l.Add(data[i]);

                    return l.ToArray();
            }
        }

    }
}
