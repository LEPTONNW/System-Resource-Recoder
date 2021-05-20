using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace System_Resource_Recoder
{
    public partial class HWMn : Form
    {
        public HWMn()
        {
            InitializeComponent();
        }

        protected override void WndProc(ref Message m) //FormboardStyle = None 일 경우 마우스 제어 함수
        {
            switch (m.Msg)
            {
                case 0x84:
                    base.WndProc(ref m);
                    if ((int)m.Result == 0x1)
                        m.Result = (IntPtr)0x2;
                    return;
            }

            base.WndProc(ref m);
        }

        private void HWMn_Load(object sender, EventArgs e)
        {

        }
    }
}
