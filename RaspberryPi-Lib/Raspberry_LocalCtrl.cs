using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaspberryPi_Lib
{
    public class Raspberry_LocalCtrl
    {
        #region Private Variable

        private string m_ip_address;
        private int m_port;

        private System.Net.Sockets.TcpClient m_keyboard_sock = new System.Net.Sockets.TcpClient();
        private System.Net.Sockets.TcpClient m_mouse_sock = new System.Net.Sockets.TcpClient();
        private System.Net.Sockets.NetworkStream m_keyboard_stream = null;
        private System.Net.Sockets.NetworkStream m_mouse_stream = null;

        private System.Threading.SemaphoreSlim m_keyboard_lock = new System.Threading.SemaphoreSlim(1);
        private System.Threading.SemaphoreSlim m_mouse_lock = new System.Threading.SemaphoreSlim(1);

        private byte[] m_keyboard_buffer = new byte[8];
        private byte[] m_mouse_buffer = new byte[3];

        private Dictionary<Key, int> m_press_key_index = new Dictionary<Key, int>();
        private Stack<int> m_free_key_index = new Stack<int>();
        #endregion

        #region Constructor

        internal Raspberry_LocalCtrl(string ip_address, int port)
        {
            m_ip_address = ip_address;
            m_port = port;

            Initial_Keyboard();
        }

        #endregion

        #region Public Method

        public bool Connect()
        {
            try
            {
                m_keyboard_lock.Wait();
                m_keyboard_sock.Connect(m_ip_address, m_port);
                m_keyboard_stream = m_keyboard_sock.GetStream();
                byte[] keyboard_buffer = BitConverter.GetBytes((int)0x11111111);
                m_keyboard_stream.Write(keyboard_buffer, 0, keyboard_buffer.Length);
                m_keyboard_lock.Release();

                m_mouse_lock.Wait();
                m_mouse_sock.Connect(m_ip_address, m_port);
                m_mouse_stream = m_mouse_sock.GetStream();
                byte[] mouse_buffer = BitConverter.GetBytes((int)0x22222222);
                m_mouse_stream.Write(mouse_buffer, 0, mouse_buffer.Length);
                m_mouse_lock.Release();
            }
            catch
            {
                if (m_mouse_sock.Connected)
                    m_mouse_sock.Close();

                if (m_keyboard_sock.Connected)
                    m_keyboard_sock.Close();

                return false;
            }

            return true;
        }

        public bool Disconnect()
        {
            m_keyboard_sock.Close();
            m_mouse_sock.Close();

            return true;
        }

        public void Mouse_Move(int rel_x, int rel_y)
        {
            m_mouse_lock.Wait();
            m_mouse_buffer[1] = (byte)rel_x;
            m_mouse_buffer[2] = (byte)rel_y;
            m_mouse_stream.Write(m_mouse_buffer, 0, m_mouse_buffer.Length);
            m_mouse_lock.Release();
        }

        public void Mouse_Down(MouseButton button)
        {
            byte button_byte = 0;
            switch(button)
            {
                case MouseButton.Left:
                    button_byte |= 0x1;
                    break;
                case MouseButton.Middle:
                    button_byte |= 0x4;
                    break;
                case MouseButton.Right:
                    button_byte |= 0x2;
                    break;
            }

            m_mouse_lock.Wait();
            m_mouse_buffer[0] = button_byte;
            m_mouse_buffer[1] = 0;
            m_mouse_buffer[2] = 0;
            Mouse_Send();
            m_mouse_lock.Release();
        }

        public void Mouse_Up(MouseButton button)
        {
            m_mouse_lock.Wait();
            byte clear_button = 0;
            switch(button)
            {
                case MouseButton.Left:
                    clear_button = 0xfe;
                    break;
                case MouseButton.Middle:
                    clear_button = 0xfb;
                    break;
                case MouseButton.Right:
                    clear_button = 0xfd;
                    break;
            }
            m_mouse_buffer[0] &= clear_button;
            m_mouse_buffer[1] = 0;
            m_mouse_buffer[2] = 0;
            Mouse_Send();
            m_mouse_lock.Release();
        }

        public void Mouse_Click(MouseButton button, int ms_period)
        {
            Mouse_Down(button);
            System.Threading.Thread.Sleep(ms_period);
            Mouse_Up(button);
        }

        public bool Keyboard_Down(Key key)
        {
            int pressed_index = -1;
            bool ret = false;

            m_keyboard_lock.Wait();
            if (!Get_Keyboard_IsPressed(key, ref pressed_index) &&
                Get_Keyboard_Free_Slot(ref pressed_index))
            {
                m_keyboard_buffer[pressed_index] = Convert.ToByte(key);
                Keyboard_Send();
                m_press_key_index.Add(key, pressed_index);
                ret = true;
            }
            m_keyboard_lock.Release();

            return ret;
        }

        public bool Keyboard_Down(Control_Key key)
        {
            int pressed_index = -1;
            bool ret = false;

            m_keyboard_lock.Wait();
            if (!Get_Keyboard_IsPressed(key, ref pressed_index))
            {
                m_keyboard_buffer[0] |= Convert.ToByte(key);
                Keyboard_Send();
                ret = true;
            }
            m_keyboard_lock.Release();

            return ret;
        }

        public bool Keyboard_Up(Key key)
        {
            int pressed_index = -1;
            bool ret = false;

            m_keyboard_lock.Wait();
            if (Get_Keyboard_IsPressed(key, ref pressed_index))
            {
                m_keyboard_buffer[pressed_index] = 0;
                Keyboard_Send();
                m_press_key_index.Remove(key);
                Insert_Keyboard_Free_Slot(pressed_index);
                ret = true;
            }
            m_keyboard_lock.Release();

            return ret;
        }

        public bool Keyboard_Up(Control_Key key)
        {
            int pressed_index = -1;
            bool ret = false;

            m_keyboard_lock.Wait();
            if (Get_Keyboard_IsPressed(key, ref pressed_index))
            {
                m_keyboard_buffer[0] &= Convert.ToByte(~key);
                Keyboard_Send();
                ret = true;
            }
            m_keyboard_lock.Release();

            return ret;
        }

        public void Keyboard_Press(Key key, int ms_period)
        {
            Keyboard_Down(key);
            System.Threading.Thread.Sleep(ms_period);
            Keyboard_Up(key);
        }

        public void Keyboard_Press(Control_Key key, int ms_period)
        {
            Keyboard_Down(key);
            System.Threading.Thread.Sleep(ms_period);
            Keyboard_Up(key);
        }

        #endregion

        #region Private Method

        private void Mouse_Send()
        {
            m_mouse_stream.Write(m_mouse_buffer, 0, m_mouse_buffer.Length);
        }

        private void Keyboard_Send()
        {
            m_keyboard_stream.Write(m_keyboard_buffer, 0, m_keyboard_buffer.Length);
        }

        private void Initial_Keyboard()
        {
            m_free_key_index.Clear();
            for (int i = 7; i >= 2; --i)
                m_free_key_index.Push(i);
        }

        private bool Get_Keyboard_IsPressed(Key key, ref int index)
        {
            if (m_press_key_index.ContainsKey(key))
            {
                index = m_press_key_index[key];
                return true;
            }

            return false;
        }

        private bool Get_Keyboard_IsPressed(Control_Key key, ref int index)
        {
            byte key_byte = (byte)key;
            if ((m_keyboard_buffer[0] & key_byte) == key_byte)
                return true;

            return false;
        }

        private bool Get_Keyboard_Free_Slot(ref int index)
        {
            if (m_free_key_index.Count == 0)
                return false;

            index = m_free_key_index.Pop();
            return true;
        }

        private void Insert_Keyboard_Free_Slot(int index)
        {
            if (m_free_key_index.Count == 6)
                return;

            m_free_key_index.Push(index);
        }

        #endregion
    }
}
