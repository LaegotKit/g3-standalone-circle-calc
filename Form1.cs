using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Standalone_Circle_Calc
{
    public partial class Form1 : Form
    {
        private GeniePlugin.Interfaces.IHost _host;

        public Form1(ref GeniePlugin.Interfaces.IHost host)
        {
            InitializeComponent();

            _host = host;
        }
        private void btnOk_Click(object sender, EventArgs e)
        {
            if (cbCircle150.Checked == true)
                _host.set_Variable("ExpTracker.ShowPast150", "1");
            else
                _host.set_Variable("ExpTracker.ShowPast150", "0");

            _host.SendText("#var save");

            this.Close();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

    }
}
