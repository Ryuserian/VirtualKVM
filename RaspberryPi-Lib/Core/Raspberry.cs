using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Renci.SshNet;

namespace RaspberryPi_Lib
{
    public class Raspberry
    {
        #region Private Variable

        private string m_ip_address;
        private int m_ssh_port;
        private string m_user_id;
        private string m_password;

        #endregion

        #region Constructor

        public Raspberry(string ip_address, int ssh_port, string user_id, string password)
        {
            m_ip_address = ip_address;
            m_ssh_port = ssh_port;
            m_user_id = user_id;
            m_password = password;
        }

        /// <summary>SSH 포트 기본값(22) 생성자.</summary>
        public Raspberry(string ip_address, string user_id, string password)
            : this(ip_address, 22, user_id, password)
        {
        }

        #endregion

        #region Public Method

        public bool Install()
        {
            try
            {
                SftpClient client = new SftpClient(m_ip_address, m_user_id, m_password);
                client.Connect();
                if (!client.IsConnected)
                    throw null;

                System.IO.MemoryStream ms = new System.IO.MemoryStream();
                byte[] data = Properties.Resources.RaspberryPiZeroW_HID;
                ms.Write(data, 0, data.Length);
                ms.Position = 0;
                client.UploadFile(ms, "Desktop/RaspberryPiZeroW-HID");

                client.ChangePermissions("Desktop/RaspberryPiZeroW-HID", 0777);
                client.Disconnect();
            }
            catch
            {
                return false;
            }

            return true;
        }

        public Raspberry_RemoteCtrl GetRemoteController()
        {
            return new Raspberry_RemoteCtrl(m_ip_address, m_ssh_port, m_user_id, m_password);
        }

        /// <summary>Pi HID 데몬의 TCP 포트로 컨트롤러를 생성한다.</summary>
        public Raspberry_LocalCtrl GetLocalController(int hid_port)
        {
            return new Raspberry_LocalCtrl(m_ip_address, hid_port);
        }

        /// <summary>HID 포트 기본값(5000) 컨트롤러.</summary>
        public Raspberry_LocalCtrl GetLocalController()
        {
            return new Raspberry_LocalCtrl(m_ip_address, 5000);
        }

        #endregion
    }
}
