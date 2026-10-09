using System;
using System.Drawing;
using System.Net;
using System.Windows.Forms;

namespace VirtualKVM
{
    /// <summary>
    /// 연결 설정(IP / HID 포트 / SSH 포트 / 계정) 입력 다이얼로그.
    /// 확인 시 AppConfig 파일(%APPDATA%\VirtualKVM\config.json)에 저장한다.
    /// </summary>
    public class WindowConfig : Form
    {
        private Label label_ip;
        private TextBox text_ip;
        private Label label_hid_port;
        private NumericUpDown numeric_hid_port;
        private Label label_ssh_port;
        private NumericUpDown numeric_ssh_port;
        private Label label_user;
        private TextBox text_user;
        private Label label_password;
        private TextBox text_password;
        private CheckBox check_show_password;
        private Button button_ok;
        private Button button_cancel;

        private readonly AppConfig m_config;

        public WindowConfig(AppConfig config)
        {
            m_config = config;
            InitializeComponent();
            Load_Values();
        }

        private void InitializeComponent()
        {
            this.label_ip = new Label();
            this.text_ip = new TextBox();
            this.label_hid_port = new Label();
            this.numeric_hid_port = new NumericUpDown();
            this.label_ssh_port = new Label();
            this.numeric_ssh_port = new NumericUpDown();
            this.label_user = new Label();
            this.text_user = new TextBox();
            this.label_password = new Label();
            this.text_password = new TextBox();
            this.check_show_password = new CheckBox();
            this.button_ok = new Button();
            this.button_cancel = new Button();

            ((System.ComponentModel.ISupportInitialize)this.numeric_hid_port).BeginInit();
            ((System.ComponentModel.ISupportInitialize)this.numeric_ssh_port).BeginInit();
            this.SuspendLayout();

            // label_ip
            this.label_ip.AutoSize = true;
            this.label_ip.Location = new Point(14, 18);
            this.label_ip.Text = "IP 주소";

            // text_ip
            this.text_ip.Location = new Point(110, 15);
            this.text_ip.Size = new Size(200, 23);

            // label_hid_port
            this.label_hid_port.AutoSize = true;
            this.label_hid_port.Location = new Point(14, 50);
            this.label_hid_port.Text = "HID 포트";

            // numeric_hid_port
            this.numeric_hid_port.Location = new Point(110, 48);
            this.numeric_hid_port.Maximum = 65535;
            this.numeric_hid_port.Minimum = 1;
            this.numeric_hid_port.Size = new Size(80, 23);

            // label_ssh_port
            this.label_ssh_port.AutoSize = true;
            this.label_ssh_port.Location = new Point(210, 50);
            this.label_ssh_port.Text = "SSH 포트";

            // numeric_ssh_port
            this.numeric_ssh_port.Location = new Point(280, 48);
            this.numeric_ssh_port.Maximum = 65535;
            this.numeric_ssh_port.Minimum = 1;
            this.numeric_ssh_port.Size = new Size(80, 23);

            // label_user
            this.label_user.AutoSize = true;
            this.label_user.Location = new Point(14, 82);
            this.label_user.Text = "사용자 ID";

            // text_user
            this.text_user.Location = new Point(110, 79);
            this.text_user.Size = new Size(100, 23);

            // label_password
            this.label_password.AutoSize = true;
            this.label_password.Location = new Point(220, 82);
            this.label_password.Text = "비밀번호";

            // text_password
            this.text_password.Location = new Point(280, 79);
            this.text_password.Size = new Size(80, 23);
            this.text_password.UseSystemPasswordChar = true;

            // check_show_password
            this.check_show_password.AutoSize = true;
            this.check_show_password.Location = new Point(110, 110);
            this.check_show_password.Text = "비밀번호 표시";
            this.check_show_password.CheckedChanged += Check_show_password_CheckedChanged;

            // button_ok
            this.button_ok.Location = new Point(122, 140);
            this.button_ok.Size = new Size(80, 28);
            this.button_ok.Text = "확인";
            this.button_ok.Click += Button_ok_Click;

            // button_cancel
            this.button_cancel.DialogResult = DialogResult.Cancel;
            this.button_cancel.Location = new Point(210, 140);
            this.button_cancel.Size = new Size(80, 28);
            this.button_cancel.Text = "취소";

            // WindowConfig
            this.AcceptButton = this.button_ok;
            this.CancelButton = this.button_cancel;
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(374, 181);
            this.Controls.Add(this.label_ip);
            this.Controls.Add(this.text_ip);
            this.Controls.Add(this.label_hid_port);
            this.Controls.Add(this.numeric_hid_port);
            this.Controls.Add(this.label_ssh_port);
            this.Controls.Add(this.numeric_ssh_port);
            this.Controls.Add(this.label_user);
            this.Controls.Add(this.text_user);
            this.Controls.Add(this.label_password);
            this.Controls.Add(this.text_password);
            this.Controls.Add(this.check_show_password);
            this.Controls.Add(this.button_ok);
            this.Controls.Add(this.button_cancel);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "WindowConfig";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "연결 설정";

            ((System.ComponentModel.ISupportInitialize)this.numeric_hid_port).EndInit();
            ((System.ComponentModel.ISupportInitialize)this.numeric_ssh_port).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private void Load_Values()
        {
            text_ip.Text = m_config.IpAddress;
            numeric_hid_port.Value = m_config.HidPort;
            numeric_ssh_port.Value = m_config.SshPort;
            text_user.Text = m_config.UserId;
            text_password.Text = m_config.PlainPassword;
        }

        private void Check_show_password_CheckedChanged(object sender, EventArgs e)
        {
            text_password.UseSystemPasswordChar = !check_show_password.Checked;
        }

        private void Button_ok_Click(object sender, EventArgs e)
        {
            string ip = text_ip.Text.Trim();

            if (ip.Length == 0)
            {
                MessageBox.Show("IP 주소를 입력하세요.", "설정", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                text_ip.Focus();
                return;
            }

            // IPv4 주소 또는 호스트명 허용 (IPv4 형식이 아니면 DNS 이름으로 간주)
            IPAddress parsed;
            if (!IPAddress.TryParse(ip, out parsed))
            {
                bool valid_hostname = true;
                foreach (char c in ip)
                {
                    if (!(char.IsLetterOrDigit(c) || c == '.' || c == '-'))
                    {
                        valid_hostname = false;
                        break;
                    }
                }

                if (!valid_hostname)
                {
                    MessageBox.Show("올바른 IP 주소 또는 호스트명을 입력하세요.", "설정", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    text_ip.Focus();
                    return;
                }
            }

            if (text_user.Text.Trim().Length == 0)
            {
                MessageBox.Show("사용자 ID를 입력하세요.", "설정", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                text_user.Focus();
                return;
            }

            m_config.IpAddress = ip;
            m_config.HidPort = (int)numeric_hid_port.Value;
            m_config.SshPort = (int)numeric_ssh_port.Value;
            m_config.UserId = text_user.Text.Trim();
            m_config.PlainPassword = text_password.Text;
            m_config.Save();

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
