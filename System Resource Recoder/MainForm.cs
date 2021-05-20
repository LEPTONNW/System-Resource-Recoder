using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;
using System.Net.NetworkInformation;
using Microsoft.Win32;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Net.Mail;
using System.IO.Compression;
using System.Net;
/* 실행 시 관리자 권한 상승을 위해 추가*/
using System.Security.Principal;
/* 추가 끝 */

namespace System_Resource_Recoder
{
    public partial class MainForm : Form
    {

        private PerformanceCounter cpu = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        private PerformanceCounter ram = new PerformanceCounter("Memory", "Available MBytes");
        string process_name = Process.GetCurrentProcess().ProcessName;
        private PerformanceCounter prcess_cpu = new PerformanceCounter("Process", "% Processor Time", Process.GetCurrentProcess().ProcessName);
        //private PerformanceCounter diskRead = new Performance

        private PerformanceCounter diskReadsPerSec = new PerformanceCounter("PhysicalDisk", "Disk Read Bytes/sec", "_Total");
        private PerformanceCounter diskWritesPerSec = new PerformanceCounter("PhysicalDisk", "Disk Write Bytes/sec", "_Total");
        private PerformanceCounter DiskTime = new PerformanceCounter("PhysicalDisk", "AVG. Disk sec/Read", "_Total");

        public static NetworkInterface[] nicArr;
        PerformanceCounterCategory performanceCounterCategory = new PerformanceCounterCategory("Network Interface");
        public static string instance = "";
        public PerformanceCounter performanceCounterSent = new PerformanceCounter("Network Interface", "Bytes Sent/sec", instance);
        public PerformanceCounter performanceCounterReceived = new PerformanceCounter("Network Interface", "Bytes Received/sec", instance);

        private bool loop_state = true;

        public string RA = Convert.ToString(new Microsoft.VisualBasic.Devices.ComputerInfo().TotalPhysicalMemory);
        public string GPS;
        public int temp1 = 0;
        public int filetimer = 0, sec1 = 0, tm1 = 0;
        public int mailtimer = 0, sec2 = 0, tm2 = 0;

        [DllImport("kernel32")]
        private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);
        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);


        public MainForm()
        {
            InitializeComponent();

            this.ShowInTaskbar = false;
            this.Visible = false;
            this.notifyIcon1.Visible = true;
            notifyIcon1.ContextMenuStrip = contextMenuStrip1;
        }

        private void Form1_Load(object sender, EventArgs e)
        {      

            /* 실행 시 관리자 권한 상승을 위한 코드 시작 */
            if (/* Main 아래에 정의된 함수 */IsAdministrator() == false)
            {
                try
                {
                    ProcessStartInfo procInfo = new ProcessStartInfo();
                    procInfo.UseShellExecute = true;
                    procInfo.FileName = Application.ExecutablePath;
                    procInfo.WorkingDirectory = Environment.CurrentDirectory;
                    procInfo.Verb = "runas";
                    Process.Start(procInfo);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message.ToString());
                }

                return;
            }

            NeStart();

            Thread system = new Thread(check_system);
            system.Start();

            Thread DISK_TH = new Thread(diskcheck);
            DISK_TH.Start();

            Thread DISK_W = new Thread(diskcheck_W);
            DISK_W.Start();

            // 디폴트값 사용 (Maximum=100, Minimum=0, Step=10)
            progressBar1.Style = ProgressBarStyle.Blocks;

            this.button17.BackColor = System.Drawing.Color.Orange;
            this.button18.BackColor = System.Drawing.Color.Orange;

            string procName = Process.GetCurrentProcess().ProcessName;

            Process[] procArray = Process.GetProcessesByName(procName);
            if (procArray.Length > 1)
            {
                MessageBox.Show("Error : 관리자 권한으로 실행되지 않았거나, 이미 실행중입니다.");
                loop_state = false;  // for worker thread exit....
                Application.ExitThread();
                Application.Exit();
            }
            else
            {

            }
        }

        public static bool IsAdministrator()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();

            if (null != identity)
            {
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }

            return false;
        }
        /* 실행 시 관리자 권한 상승을 위한 함수 끝 */

        private void check_system() // CPU, RAM
        {
            float RA_V = Convert.ToInt64(RA) / 1000000000;

            string[] RAM1 = Convert.ToString(RA_V).Split('.'); // 기가량만 뽑아냄
            int RA_T = Convert.ToInt32(RAM1[0]) * 1024; //전체용량
            //전체용량 - 남은용량 = 사용용량

            do
            {
                if (this.InvokeRequired)
                {
                    this.label1.BeginInvoke(new Action(() =>
                    {
                        string CPU = cpu.NextValue().ToString(); //CPU
                        string[] CPU1 = CPU.Split('.');
                        this.label1.Text = CPU1[0] + " %";

                        string RAM2 = ram.NextValue().ToString(); //남은용량 //RAM
                        temp1 = RA_T - Convert.ToInt32(RAM2); //사용용량
                        this.label2.Text = temp1 + "MB";

                        //this.label3.Text = diskReadsPerSec.NextValue().ToString();
                    }));
                }
                else
                {
                    string CPU = cpu.NextValue().ToString();
                    string[] CPU1 = CPU.Split('.');
                    this.label1.Text = CPU[0] + " %";

                    string RAM2 = ram.NextValue().ToString(); //남은용량
                    temp1 = RA_T - Convert.ToInt32(RAM2); //temp1 = 사용용량
                    this.label2.Text = temp1 + " MB";
                    //this.label3.Text = diskReadsPerSec.NextValue().ToString();
                }





                //프로그래스바 부분
                //////////////////////////////////
                try
                {
                    Application.EnableVisualStyles();

                    string[] CPU = label1.Text.Split(' ');
                    progressBar1.Value = Convert.ToInt32(CPU[0]);

                    float RA_O = RA_T / 100; //1%값
                    // 전체용량 / 100 = 1%값

                    progressBar2.Value = Convert.ToInt32(temp1 / RA_O);

                    //if(progressBar1.Value < 10 || progressBar2.Value < 10)
                    //{
                    //    progressBar1.ForeColor = Color.Orange;
                    //    progressBar2.ForeColor = Color.Orange;
                    //}
                    //else if(progressBar1.Value < 85 || progressBar2.Value < 85)
                    //{
                    //    progressBar1.ForeColor = Color.Red;
                    //    progressBar2.ForeColor = Color.Red;
                    //}


                }
                catch
                {


                }

                Thread.Sleep(700);
            } while (loop_state);
        }

        private void diskcheck() //DISK 읽기
        {
            do
            {
                if (this.InvokeRequired)
                {
                    try
                    {
                        string DISK_R = string.Format("{0:f2}", diskReadsPerSec.NextValue() / 1048576);
                        this.label3.Text = Convert.ToString(DISK_R) + "MB/s";

                    }
                    catch
                    {
                        this.label3.Text = string.Format("{0:f2}", diskReadsPerSec.NextValue() / 1048576) + "MB/s";
                    }
                }
                else
                {
                    try
                    {
                        string DISK_R = string.Format("{0:f2}", diskReadsPerSec.NextValue() / 1048576);
                        this.label3.Text = Convert.ToString(DISK_R) + "MB/s";
                    }
                    catch
                    {
                        this.label3.Text = string.Format("{0:f2}", diskReadsPerSec.NextValue() / 1048576) + "MB/s";
                    }
                }
                Thread.Sleep(500);
            } while (loop_state);
        }

        private void diskcheck_W() //DISK 쓰기
        {
            do
            {
                if (this.InvokeRequired)
                {
                    try
                    {
                        string DISK_W = string.Format("{0:f2}" , diskWritesPerSec.NextValue() / 1048576);
                        this.label4.Text = Convert.ToString(DISK_W) + "MB/s";
                    }
                    catch
                    {
                        this.label4.Text = string.Format("{0:f2}" , diskWritesPerSec.NextValue() / 1048576) + "MB/s";
                    }
                }
                else
                {
                    try
                    {
                        string DISK_W = string.Format("{0:f2}", diskWritesPerSec.NextValue() / 1048576);
                        this.label4.Text = Convert.ToString(DISK_W) + "MB/s";
                    }
                    catch
                    {
                        this.label4.Text = string.Format("{0:f2}", diskWritesPerSec.NextValue() / 1048576) + "MB/s";
                    }
                }
                Thread.Sleep(500);
            } while (loop_state);
        }

        void NeStart() //네트워크 카드 감지
        {
            nicArr = NetworkInterface.GetAllNetworkInterfaces();

            for (int i = 0; i < nicArr.Length - 1; i++)
            {
                try
                {
                    string ins = performanceCounterCategory.GetInstanceNames()[i];
                    comboBox1.Items.Add(ins);
                }
                catch
                {
                    comboBox1.Items.Add("선택해주세요");
                }
            }

        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            notifyIcon1.ShowBalloonTip(1000);
        }



        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            instance = comboBox1.Text;
            performanceCounterSent = new PerformanceCounter("Network Interface", "Bytes Sent/sec", instance);
            performanceCounterReceived = new PerformanceCounter("Network Interface", "Bytes Received/sec", instance);

            timer1.Interval = 390; //스케쥴 간격을 1초로 준 것이다.
            timer1.Start();
        }



        private void timer1_Tick(object sender, EventArgs e)
        {
            string up = string.Format("{0:f2}", performanceCounterSent.NextValue() / 1024);
            string down = string.Format("{0:f2}", performanceCounterReceived.NextValue() / 1024);

            try
            {
                label7.Text = "업로드 : " + up + "kb/s  " + "다운로드 : " + down + "kb/s";

            }
            catch
            {

            }
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

        private void label7_Click(object sender, EventArgs e)
        {

        }

        private void groupBox1_Enter(object sender, EventArgs e)
        {

        }

        private void progressBar1_Click(object sender, EventArgs e)
        {

        }

        private void button4_Click(object sender, EventArgs e)
        {
            loop_state = false;  // for worker thread exit....
            Application.ExitThread();
            Application.Exit();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if(File.Exists("C:\\Windows\\SRR.ini"))
            {
                MessageBox.Show("이미 등록되어있습니다.");
                return;
            }

            var myForm = new SingUP();
            myForm.Show();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            File.Delete("C:\\Windows\\SRR.ini");

            RegistryKey rkey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            if (File.Exists("C:\\Windows\\SRR_Key.ocx"))
            {
                rkey.DeleteValue("ProgramName", false);
                MessageBox.Show("자동실행이 등록해제되었습니다.");
                File.Delete("C:\\Windows\\SRR_Key.ocx");
            }
        }

        private void timer2_Tick(object sender, EventArgs e) //파일저장
        {
            sec1--;

            label8.Text = sec1.ToString();

            if(!Directory.Exists("C:\\SRR\\"))
            {
                Directory.CreateDirectory("C:\\SRR\\");
            }

            if (sec1 == 0)
            {
                File.AppendAllText("C:\\SRR\\" + "CPU.txt", "["+label1.Text+"]" + " " + DateTime.Now.ToString("MM월-dd일-HH시-mm분-ss초") + Environment.NewLine);
                File.AppendAllText("C:\\SRR\\" + "RAM.txt", "["+label2.Text+"]" + " " + DateTime.Now.ToString("MM월-dd일-HH시-mm분-ss초") + Environment.NewLine);
                File.AppendAllText("C:\\SRR\\" + "DISK읽기.txt", "[" + label3.Text + "]" + " " + DateTime.Now.ToString("MM월-dd일-HH시-mm분-ss초") + Environment.NewLine);
                File.AppendAllText("C:\\SRR\\" + "DISK쓰기.txt", "[" + label4.Text + "]" + " " + DateTime.Now.ToString("MM월-dd일-HH시-mm분-ss초") + Environment.NewLine);
                File.AppendAllText("C:\\SRR\\" + "Network.txt", "[" + label7.Text + "]" + " " + DateTime.Now.ToString("MM월-dd일-HH시-mm분-ss초") + Environment.NewLine);

                sec1 = tm1;

            }



        }

        private void timer3_Tick(object sender, EventArgs e) //메일발송
        {
            sec2--;

            label9.Text = sec2.ToString();

            if (!Directory.Exists("C:\\SRR\\"))
            {
                Directory.CreateDirectory("C:\\SRR\\");
            }

            if (sec2 == 0)
            {
                MailWAR();
                sec2 = tm2;
            }
        }

        private void button19_Click(object sender, EventArgs e)
        {
            this.button19.BackColor = System.Drawing.Color.Orange;
            this.button17.BackColor = System.Drawing.Color.White;

            if (sec1 == 0)
            {
                this.button19.BackColor = System.Drawing.Color.White;
                this.button17.BackColor = System.Drawing.Color.Orange;
                MessageBox.Show("먼저 파일 저장 시간을 정해주세요");
                return;
            }

            timer2.Interval = 1000; //스케쥴 간격을 1초로 준 것이다.
            timer2.Start();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Process.Start("explorer.exe", "C:\\SRR");
        }

        private void button5_Click(object sender, EventArgs e)
        {
            tm1 = 600;
            sec1 = tm1;
            this.button5.BackColor = System.Drawing.Color.Orange;
            this.button6.BackColor = System.Drawing.Color.White;
            this.button7.BackColor = System.Drawing.Color.White;
            this.button8.BackColor = System.Drawing.Color.White;
            this.button9.BackColor = System.Drawing.Color.White;
            this.button10.BackColor = System.Drawing.Color.White;
        }

        private void button6_Click(object sender, EventArgs e)
        {
            tm1 = 1200;
            sec1 = tm1;
            this.button5.BackColor = System.Drawing.Color.White;
            this.button6.BackColor = System.Drawing.Color.Orange;
            this.button7.BackColor = System.Drawing.Color.White;
            this.button8.BackColor = System.Drawing.Color.White;
            this.button9.BackColor = System.Drawing.Color.White;
            this.button10.BackColor = System.Drawing.Color.White;
        }

        private void button7_Click(object sender, EventArgs e)
        {
            tm1 = 1800;
            sec1 = tm1;
            this.button5.BackColor = System.Drawing.Color.White;
            this.button6.BackColor = System.Drawing.Color.White;
            this.button7.BackColor = System.Drawing.Color.Orange;
            this.button8.BackColor = System.Drawing.Color.White;
            this.button9.BackColor = System.Drawing.Color.White;
            this.button10.BackColor = System.Drawing.Color.White;
        }

        private void button8_Click(object sender, EventArgs e)
        {
            tm1 = 3600;
            sec1 = tm1;
            this.button5.BackColor = System.Drawing.Color.White;
            this.button6.BackColor = System.Drawing.Color.White;
            this.button7.BackColor = System.Drawing.Color.White;
            this.button8.BackColor = System.Drawing.Color.Orange;
            this.button9.BackColor = System.Drawing.Color.White;
            this.button10.BackColor = System.Drawing.Color.White;
        }

        private void button9_Click(object sender, EventArgs e)
        {
            tm1 = 7200;
            sec1 = tm1;
            this.button5.BackColor = System.Drawing.Color.White;
            this.button6.BackColor = System.Drawing.Color.White;
            this.button7.BackColor = System.Drawing.Color.White;
            this.button8.BackColor = System.Drawing.Color.White;
            this.button9.BackColor = System.Drawing.Color.Orange;
            this.button10.BackColor = System.Drawing.Color.White;
        }

        private void button10_Click(object sender, EventArgs e)
        {
            tm1 = 10800;
            sec1 = tm1;
            this.button5.BackColor = System.Drawing.Color.White;
            this.button6.BackColor = System.Drawing.Color.White;
            this.button7.BackColor = System.Drawing.Color.White;
            this.button8.BackColor = System.Drawing.Color.White;
            this.button9.BackColor = System.Drawing.Color.White;
            this.button10.BackColor = System.Drawing.Color.Orange;
        }

        private void button16_Click(object sender, EventArgs e)
        {
            tm2 = 600;
            sec2 = tm2;

            this.button16.BackColor = System.Drawing.Color.Orange;
            this.button15.BackColor = System.Drawing.Color.White;
            this.button14.BackColor = System.Drawing.Color.White;
            this.button13.BackColor = System.Drawing.Color.White;
            this.button12.BackColor = System.Drawing.Color.White;
            this.button11.BackColor = System.Drawing.Color.White;
        }

        private void button15_Click(object sender, EventArgs e)
        {
            tm2 = 1200;
            sec2 = tm2;

            this.button16.BackColor = System.Drawing.Color.White;
            this.button15.BackColor = System.Drawing.Color.Orange;
            this.button14.BackColor = System.Drawing.Color.White;
            this.button13.BackColor = System.Drawing.Color.White;
            this.button12.BackColor = System.Drawing.Color.White;
            this.button11.BackColor = System.Drawing.Color.White;
        }

        private void button14_Click(object sender, EventArgs e)
        {
            tm2 = 1800;
            sec2 = tm2;

            this.button16.BackColor = System.Drawing.Color.White;
            this.button15.BackColor = System.Drawing.Color.White;
            this.button14.BackColor = System.Drawing.Color.Orange;
            this.button13.BackColor = System.Drawing.Color.White;
            this.button12.BackColor = System.Drawing.Color.White;
            this.button11.BackColor = System.Drawing.Color.White;
        }

        private void button13_Click(object sender, EventArgs e)
        {
            tm2 = 3600;
            sec2 = tm2;

            this.button16.BackColor = System.Drawing.Color.White;
            this.button15.BackColor = System.Drawing.Color.White;
            this.button14.BackColor = System.Drawing.Color.White;
            this.button13.BackColor = System.Drawing.Color.Orange;
            this.button12.BackColor = System.Drawing.Color.White;
            this.button11.BackColor = System.Drawing.Color.White;
        }

        private void button12_Click(object sender, EventArgs e)
        {
            tm2 = 7200;
            sec2 = tm2;

            this.button16.BackColor = System.Drawing.Color.White;
            this.button15.BackColor = System.Drawing.Color.White;
            this.button14.BackColor = System.Drawing.Color.White;
            this.button13.BackColor = System.Drawing.Color.White;
            this.button12.BackColor = System.Drawing.Color.Orange;
            this.button11.BackColor = System.Drawing.Color.White;
        }

        private void button11_Click(object sender, EventArgs e)
        {
            tm2 = 10800;
            sec2 = tm2;

            this.button16.BackColor = System.Drawing.Color.White;
            this.button15.BackColor = System.Drawing.Color.White;
            this.button14.BackColor = System.Drawing.Color.White;
            this.button13.BackColor = System.Drawing.Color.White;
            this.button12.BackColor = System.Drawing.Color.White;
            this.button11.BackColor = System.Drawing.Color.Orange;
        }

        private void button21_Click(object sender, EventArgs e)
        {
            this.Visible = false;//어플리케이션을 숨긴다. 
            notifyIcon1.ShowBalloonTip(5000);
        }

    private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            this.Visible = true; // 폼의 표시
            if (this.WindowState == FormWindowState.Minimized)
                this.WindowState = FormWindowState.Normal; // 최소화를 멈춘다 
            this.Activate(); // 폼을 활성화 시킨다
        }

        private void 종료ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            loop_state = false;  // for worker thread exit....
            Application.ExitThread();
            Application.Exit();
        }

        private void button22_Click(object sender, EventArgs e)
        {
            MessageBox.Show("1.파일저장 옵션을 통해 [C:\\SRR] 폴더 안에 txt 파일로 측정된 값이 저장됩니다." + Environment.NewLine + "2.이메일 발송 옵션을 통해 메일로 측정된 파일이 압축되어 발송됩니다." + Environment.NewLine + "3.메일 등록은 시작프로그램 등록시에 설정 가능합니다.");
        }

        private void comboBox1_SelectedIndexChanged_1(object sender, EventArgs e)
        {

        }

        private void groupBox3_Enter(object sender, EventArgs e)
        {

        }

        private void button20_Click(object sender, EventArgs e)
        {
            if(!File.Exists("C:\\Windows\\SRR.ini"))
            {
                MessageBox.Show("메일이 등록되지않았습니다." + Environment.NewLine + "먼저 메일을 등록해주세요");
                return;
            }

            this.button20.BackColor = System.Drawing.Color.Orange;
            this.button18.BackColor = System.Drawing.Color.White;

            if (sec2 == 0)
            {
                this.button20.BackColor = System.Drawing.Color.White;
                this.button18.BackColor = System.Drawing.Color.Orange;

                MessageBox.Show("먼저 메일 저장 시간을 정해주세요");
                return;
            }

            timer3.Interval = 1000; //스케쥴 간격을 1초로 준 것이다.
            timer3.Start();
        }

        private void button17_Click(object sender, EventArgs e)
        {
            timer2.Stop();
            sec1 = 0;
            tm1 = 0;
            label8.Text = sec1.ToString();

            this.button17.BackColor = System.Drawing.Color.Orange;
            this.button19.BackColor = System.Drawing.Color.White;
            this.button5.BackColor = System.Drawing.Color.White;
            this.button6.BackColor = System.Drawing.Color.White;
            this.button7.BackColor = System.Drawing.Color.White;
            this.button8.BackColor = System.Drawing.Color.White;
            this.button9.BackColor = System.Drawing.Color.White;
            this.button10.BackColor = System.Drawing.Color.White;
        }

        private void button18_Click(object sender, EventArgs e)
        {
            timer3.Stop();
            sec2 = 0;
            tm2 = 0;
            label9.Text = sec2.ToString();

            this.button20.BackColor = System.Drawing.Color.White;
            this.button18.BackColor = System.Drawing.Color.Orange;
            this.button16.BackColor = System.Drawing.Color.White;
            this.button15.BackColor = System.Drawing.Color.White;
            this.button14.BackColor = System.Drawing.Color.White;
            this.button13.BackColor = System.Drawing.Color.White;
            this.button12.BackColor = System.Drawing.Color.White;
            this.button11.BackColor = System.Drawing.Color.White;
        }


        void MailWAR() //메일발송
        {
            try
            {
                if (File.Exists("C:\\Info.zip"))
                {
                    File.Delete("C:\\Info.zip");
                }

                StringBuilder E = new StringBuilder(255);

                int ret = GetPrivateProfileString("섹션1", "ID", "Error:" + Environment.NewLine + "계정 정보를 불러올 수 없습니다.", E, 255, "C:\\Windows\\SRR.ini");
                string EM = E.ToString();
                string Email = "kac4484@gmail.com";
                //E.ToString();

                //StringBuilder P = new StringBuilder(255);

                //int ret1 = GetPrivateProfileString("섹션1", "PASS", "Error:" + Environment.NewLine + "계정 정보를 불러올 수 없습니다.", P, 255, "C:\\Windows\\SRR.ini");
                string PASS = "wkdtjsgh2tlf@";
                //P.ToString();
                MessageBox.Show(PASS);
                string infoPath = "C:\\Info.zip";

                ZipFile.CreateFromDirectory("C:\\SRR", infoPath);

                
                

                SmtpClient client = new SmtpClient("smtp.gmail.com", 587);
                client.UseDefaultCredentials = false; // 시스템에 설정된 인증 정보를 사용하지 않는다.
                client.EnableSsl = true;  // SSL을 사용한다.
                client.DeliveryMethod = SmtpDeliveryMethod.Network; // 이걸 하지 않으면 Gmail에 인증을 받지 못한다.
                client.Credentials = new System.Net.NetworkCredential(Email, PASS);

                MailAddress from = new MailAddress(Email, "[시스템] 리소스 모니터링 파일입니다.", System.Text.Encoding.UTF8);
                MailAddress to = new MailAddress(EM);

                MailMessage message = new MailMessage(from, to);

                //String WanIP = new WebClient().DownloadString("http://ip.mc-blacklist.kr/");
                //GPS = "http://whatismyipaddress.com/ip/" + WanIP;

                message.Body = "[시스템] 리소스 모니터링 파일입니다." + Environment.NewLine;

                message.BodyEncoding = System.Text.Encoding.UTF8;
                message.Subject = "[시스템] 리소스 모니터링 파일입니다.";
                message.SubjectEncoding = System.Text.Encoding.UTF8;

                Attachment attachment;
                attachment = new System.Net.Mail.Attachment("C:\\Info.zip");
                message.Attachments.Add(attachment);

                try
                {
                    // 동기로 메일을 보낸다.
                    client.Send(message);

                    // Clean up.
                    message.Dispose();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("잘못된 메일이거나, 메일 등록이 되지않았습니다.");
                MessageBox.Show(e.ToString());
            }
        }
    }
}
