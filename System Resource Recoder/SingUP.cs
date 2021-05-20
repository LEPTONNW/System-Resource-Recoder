using Microsoft.Win32;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace System_Resource_Recoder
{
    public partial class SingUP : Form
    {
        public SingUP()
        {
            InitializeComponent();
        }

        private void SingUP_Load(object sender, EventArgs e)
        {
            textBox2.Hide();
            label2.Hide();

            comboBox1.Text = "직접입력";
        }

        public string FilePath = "C:\\Windows\\SRR.ini";
        public string Email = "";
        
        [DllImport("kernel32")]
        private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);
        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(textBox1.Text))
                {
                    MessageBox.Show("정보가 입력되지 않았습니다.");
                    return;
                }
                else if(string.IsNullOrEmpty(textBox2.Text))
                {
                    textBox2.Text = "";
                }



                Email = textBox1.Text + comboBox1.Text;
                CreateINI();

                if (!File.Exists("C:\\Windows\\SRR_Key.ocx"))
                {
                    RegistryKey rkey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                    rkey.SetValue("ProgramName", Application.ExecutablePath.ToString());
                    MessageBox.Show("시작프로그램에 자동등록되었습니다.");
                    File.Create("C:\\Windows\\SRR_Key.ocx");
                }
                else
                {
                    MessageBox.Show("메일은 최대 1개 까지만 등록이 가능합니다." + Environment.NewLine + "메일을 새로 등록하려면 삭제후 다시 만들어 주세요");
                }

            }
            catch
            {
                textBox1.Text = "";
                textBox2.Text = "";
                comboBox1.Text = "직접입력";
                File.Delete("C:\\Windows\\SRR.ini");

                if (File.Exists("C:\\Windows\\SRR_Key.ocx"))
                {
                    File.Delete("C:\\Windows\\SRR_Key.ocx");
                }
                else

                    MessageBox.Show("정상적으로 입력되지 않았습니다.");
            }

            MessageBox.Show("메일 등록이 완료되었습니다.");
            this.Close();
        }

        void CreateINI() //유저 정보파일 생성
        {
            WritePrivateProfileString("섹션1", "ID", Email, FilePath);
            WritePrivateProfileString("섹션1", "PASS", textBox2.Text, FilePath);
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
