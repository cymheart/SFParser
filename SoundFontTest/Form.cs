using Sc;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SoundFontTest
{
    public partial class Form : System.Windows.Forms.Form
    {
        ScMgr scMgr;
        ScLayer root;
        ChartApp chartApp;
        public Form()
        {
            InitializeComponent();
        }

        private void Form_Load(object sender, EventArgs e)
        {
            scMgr = new ScMgr(panel);
            scMgr.BackgroundColor = Color.FromArgb(255, 246, 245, 251);
            chartApp = new ChartApp(scMgr);
            scMgr.ReBulid();
        }

        private void Form_SizeChanged(object sender, EventArgs e)
        {
            if (scMgr != null)
                scMgr.Refresh();
        }
    }
}
