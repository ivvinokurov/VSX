using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace VSUILib
{
    public partial class VSViewText : Form
    {
        public VSViewText()
        {
            InitializeComponent();
        }

        public VSViewText(string caption, string text)
        {
            InitializeComponent();
            this.Text = caption;
            txtBox.Text = text;
        }
    }
}
