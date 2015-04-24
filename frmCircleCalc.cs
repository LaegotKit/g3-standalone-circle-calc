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
    public partial class frmCicleCalc : Form
    {
        private GeniePlugin.Interfaces.IHost _host;

        public frmCicleCalc(ref GeniePlugin.Interfaces.IHost host)
        {
            InitializeComponent();

            _host = host;
        }
        private void btnOk_Click(object sender, EventArgs e)
        {
            if (cboSort.Text == "Bottom")
                _host.SendText("#var CircleCalc.Sort 1");
            else
                _host.SendText("#var CircleCalc.Sort 0");

            if (Post200Circle.Checked == true)
                _host.SendText("#var CircleCalc.Display 1");
            else if (NextCircle.Checked == true)
                _host.SendText("#var CircleCalc.Display 2");
            else
                _host.SendText("#var CircleCalc.Display 0");

            if (chkEcho.Checked == true)
                _host.SendText("#var CircleCalc.Echo 1");
            else
                _host.SendText("#var CircleCalc.Echo 0");
            if (chkLog.Checked == true)
                _host.SendText("#var CircleCalc.Log 1");
            else
                _host.SendText("#var CircleCalc.Log 0");
            if (chkParse.Checked == true)
                _host.SendText("#var CircleCalc.Parse 1");
            else
                _host.SendText("#var CircleCalc.Parse 0");

            if (chkGag.Checked)
                _host.SendText("#var CircleCalc.GagFunny 1");
            else
                _host.SendText("#var CircleCalc.GagFunny 0");

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
