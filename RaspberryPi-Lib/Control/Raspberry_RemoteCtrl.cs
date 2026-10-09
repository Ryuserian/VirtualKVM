using System;
using System.Text;
using Renci.SshNet;

namespace RaspberryPi_Lib
{
    /// <summary>
    /// SSH 로 Pi 에 접속하여 USB HID 데몬(RaspberryPiZeroW-HID)을 구동/제어한다.
    ///
    /// 연결 안정성 (장시간 사용 대응):
    ///  - KeepAliveInterval = 10초: 유휴 상태에서도 keepalive@openssh.com 을 전송하여
    ///    NAT/공유기/Wi-Fi AP 의 세션 타임아웃으로 연결이 끊기는 것을 방지한다.
    ///  - Timeout/OperationTimeout: 죽은 피아(死皮亞)에 명령이 무한 대기하는 것을 방지한다.
    ///  - 데몬은 setsid nohup 으로 세션과 독립적으로 구동하여 SSH 채널 수명도에 흔들리지 않는다.
    ///  - IsAlive()/Restart() 로 데몬/연결 헬스체크 후 자가 복구를 지원한다.
    /// </summary>
    public class Raspberry_RemoteCtrl
    {
        #region Constants / State

        private const string Daemon_Name = "RaspberryPiZeroW-HID";
        private const string Usb_Setup_Script_Path = "/tmp/virtualkvm_usb_setup.sh";

        private readonly string m_ip_address;
        private readonly string m_user_id;
        private readonly string m_password;

        private readonly object m_sync = new object();

        private SshClient m_ssh_client = null;
        private bool m_isStart = false;

        #endregion

        #region Constructor

        internal Raspberry_RemoteCtrl(string ip_address, string user, string password)
        {
            m_ip_address = ip_address;
            m_user_id = user;
            m_password = password;
        }

        #endregion

        #region Public Method

        public bool Start()
        {
            lock (m_sync)
            {
                if (m_isStart)
                    return false;

                try
                {
                    Connect_Ssh();

                    Ensure_Usb_Configuration();
                    Launch_Daemon();

                    m_isStart = true;
                }
                catch (Exception)
                {
                    Disconnect_Ssh_Safe();
                    m_isStart = false;
                    return false;
                }

                return true;
            }
        }

        public bool Stop()
        {
            lock (m_sync)
            {
                if (!m_isStart)
                    return false;

                try
                {
                    if (m_ssh_client != null && m_ssh_client.IsConnected)
                    {
                        // 프로세스 이름 기반 종료로 패턴 매칭을 견고화한다.
                        Execute("sudo pkill -f /" + Daemon_Name);
                        Execute("sudo pkill -f " + Daemon_Name);
                    }

                    Disconnect_Ssh_Safe();
                }
                catch (Exception)
                {
                    // 종료 실패와 무관하게 상태는 종료로 간주한다.
                }

                m_isStart = false;
                return true;
            }
        }

        /// <summary>
        /// SSH 연결과 HID 데몬이 모두 살아있는지 점검한다. (워치독에서 주기 호출)
        /// </summary>
        public bool IsAlive()
        {
            lock (m_sync)
            {
                if (!m_isStart)
                    return false;

                try
                {
                    if (m_ssh_client == null || !m_ssh_client.IsConnected)
                        return false;

                    var result = Execute("pgrep -f /" + Daemon_Name);
                    return !string.IsNullOrWhiteSpace(result);
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// 끊긴 SSH 연결/데몬을 복구한다. (워치독에서 IsAlive() 실패 시 호출)
        /// </summary>
        public bool Restart()
        {
            lock (m_sync)
            {
                Disconnect_Ssh_Safe();
                m_isStart = false;

                try
                {
                    Connect_Ssh();
                    Ensure_Usb_Configuration();
                    Launch_Daemon();

                    m_isStart = true;
                }
                catch (Exception)
                {
                    Disconnect_Ssh_Safe();
                    return false;
                }

                return true;
            }
        }

        /// <summary>
        /// 최후의 수단: Pi 를 재부팅한다. (연속 복구 실패 시 워치독에서 호출)
        /// </summary>
        public bool Reboot_Host()
        {
            try
            {
                if (m_ssh_client == null || !m_ssh_client.IsConnected)
                    Connect_Ssh();

                Execute("sudo reboot &");
                System.Threading.Thread.Sleep(2000);
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        #endregion

        #region Private Method

        private void Connect_Ssh()
        {
            var connection_info = new ConnectionInfo(m_ip_address, 22, m_user_id,
                new PasswordAuthenticationMethod(m_user_id, m_password))
            {
                Timeout = TimeSpan.FromSeconds(10),            // 접속 타임아웃
            };

            m_ssh_client = new SshClient(connection_info);
            m_ssh_client.KeepAliveInterval = TimeSpan.FromSeconds(10);
            m_ssh_client.Connect();
        }

        private void Disconnect_Ssh_Safe()
        {
            try
            {
                if (m_ssh_client != null)
                {
                    if (m_ssh_client.IsConnected)
                        m_ssh_client.Disconnect();
                    m_ssh_client.Dispose();
                }
            }
            catch (Exception)
            {
            }

            m_ssh_client = null;
        }

        private string Execute(string command)
        {
            using (var cmd = m_ssh_client.CreateCommand(command))
            {
                return cmd.Execute();
            }
        }

        /// <summary>
        /// 데몬을 데몬화(setsid+nohup)하여 실행한다.
        /// SSH 대화형 채널에 묶지 않으므로 채널 수명과 무관하게 장시간 안정적으로 살아남는다.
        /// </summary>
        private void Launch_Daemon()
        {
            Execute("sudo setsid nohup Desktop/" + Daemon_Name + " </dev/null >/tmp/virtualkvm_hid.log 2>&1 &");
        }

        /// <summary>
        /// USB gadget(키보드+마우스 HID) 설정이 되어있지 않으면 설정한다.
        /// 스크립트를 업로드 후 sudo sh 로 실행 — Expect 패턴 무한 대기가 없는 멱등 설정.
        /// </summary>
        private void Ensure_Usb_Configuration()
        {
            string list_gadget = Execute("ls /sys/kernel/config/");
            if (list_gadget.Contains("usb_gadget"))
            {
                string gadget_exists = Execute("test -e /sys/kernel/config/usb_gadget/g1/UDC && cat /sys/kernel/config/usb_gadget/g1/UDC");
                if (!string.IsNullOrWhiteSpace(gadget_exists))
                    return; // 이미 활성화됨
            }

            Upload_Usb_Setup_Script();
            Execute("sudo sh " + Usb_Setup_Script_Path);
        }

        private void Upload_Usb_Setup_Script()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("#!/bin/sh");
            sb.AppendLine("set -e");
            sb.AppendLine("modprobe usb_f_hid || true");
            sb.AppendLine("modprobe libcomposite || true");
            sb.AppendLine("cd /sys/kernel/config/usb_gadget/");
            sb.AppendLine("mkdir -p g1");
            sb.AppendLine("cd g1");
            sb.AppendLine("echo 0 > UDC || true");
            sb.AppendLine("rm -f configs/c.1/hid.usb0 configs/c.1/hid.usb1 || true");
            sb.AppendLine("echo 0x1d6b > idVendor");
            sb.AppendLine("echo 0x0104 > idProduct");
            sb.AppendLine("echo 0x0100 > bcdDevice");
            sb.AppendLine("echo 0x0200 > bcdUSB");
            sb.AppendLine("mkdir -p strings/0x409");
            sb.AppendLine("echo \"fedcba9876543210\" > strings/0x409/serialnumber");
            sb.AppendLine("echo \"todostudio\" > strings/0x409/manufacturer");
            sb.AppendLine("echo \"HID KeyMouse\" > strings/0x409/product");
            sb.AppendLine("mkdir -p configs/c.1/strings/0x409");
            sb.AppendLine("echo \"Config 1: Keyboard And Mouse\" > configs/c.1/strings/0x409/configuration");
            sb.AppendLine("echo 10 > configs/c.1/MaxPower");
            sb.AppendLine("mkdir -p functions/hid.usb0");
            sb.AppendLine("echo 1 > functions/hid.usb0/protocol");
            sb.AppendLine("echo 1 > functions/hid.usb0/subclass");
            sb.AppendLine("echo 8 > functions/hid.usb0/report_length");
            sb.AppendLine(@"echo -ne \\x05\\x01\\x09\\x06\\xa1\\x01\\x05\\x07\\x19\\xe0\\x29\\xe7\\x15\\x00\\x25\\x01\\x75\\x01\\x95\\x08\\x81\\x02\\x95\\x01\\x75\\x08\\x81\\x03\\x95\\x05\\x75\\x01\\x05\\x08\\x19\\x01\\x29\\x05\\x91\\x02\\x95\\x01\\x75\\x03\\x91\\x03\\x95\\x06\\x75\\x08\\x15\\x00\\x25\\x65\\x05\\x07\\x19\\x00\\x29\\x65\\x81\\x00\\xc0 > functions/hid.usb0/report_desc");
            sb.AppendLine("mkdir -p functions/hid.usb1");
            sb.AppendLine("echo 2 > functions/hid.usb1/protocol");
            sb.AppendLine("echo 1 > functions/hid.usb1/subclass");
            sb.AppendLine("echo 8 > functions/hid.usb1/report_length");
            sb.AppendLine(@"echo -ne \\x05\\x01\\x09\\x02\\xa1\\x01\\x09\\x01\\xa1\\x00\\x05\\x09\\x19\\x01\\x29\\x03\\x15\\x00\\x25\\x01\\x95\\x03\\x75\\x01\\x81\\x02\\x95\\x01\\x75\\x05\\x81\\x03\\x05\\x01\\x09\\x30\\x09\\x31\\x15\\x81\\x25\\x7f\\x75\\x08\\x95\\x02\\x81\\x06\\xc0\\xc0 > functions/hid.usb1/report_desc");
            sb.AppendLine("ln -sf functions/hid.usb0 configs/c.1/ || true");
            sb.AppendLine("ln -sf functions/hid.usb1 configs/c.1/ || true");
            sb.AppendLine("ls /sys/class/udc > UDC");

            byte[] script_bytes = Encoding.ASCII.GetBytes(sb.ToString());
            using (var sftp = new SftpClient(m_ip_address, m_user_id, m_password))
            {
                sftp.Connect();
                using (var ms = new System.IO.MemoryStream(script_bytes))
                {
                    sftp.UploadFile(ms, Usb_Setup_Script_Path, true);
                }
                sftp.Disconnect();
            }
        }

        #endregion
    }
}
