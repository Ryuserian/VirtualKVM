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
        private string m_user_id;
        private string m_password;

        #endregion

        #region Constructor

        public Raspberry(string ip_address, string user_id, string password)
        {
            m_ip_address = ip_address;
            m_user_id = user_id;
            m_password = password;
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
            return new Raspberry_RemoteCtrl(m_ip_address, m_user_id, m_password);
        }

        public Raspberry_LocalCtrl GetLocalController()
        {
            return new Raspberry_LocalCtrl(m_ip_address, 5000);
        }

        #endregion
    }
}
