using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;

namespace RaspberryPi_Lib
{
    /// <summary>
    /// Pi 의 HID 데몬과 TCP 로 연결되어 키보드/마우스 HID 리포트를 전송한다.
    ///
    /// 연결 악수: 키보드 소켓에는 0x11111111, 마우스 소켓에는 0x22222222 를 보낸다.
    ///
    /// 연결 안정성 (장시간 사용 대응):
    ///  - TCP keepalive(4초/1초) 로 dead peer 및 중간 경로(NAT/공유기)의 세션 삭제를 빠르게 감지한다.
    ///  - 워치독이 주기적으로 Watchdog_pulse() 를 호출하면 현재 HID 상태를 재전송하여
    ///    연결을 활성 상태로 유지하고, 끊긴 경우 자동으로 재접속한다.
    ///  - 전송 중 예외가 발생해도 훅/UI 스레드가 죽지 않도록 격리하고 재접속 대상표시만 남긴다.
    /// </summary>
    public class Raspberry_LocalCtrl
    {
        #region Constants / State

        private const int State_Disconnected = 0;
        private const int State_Connected = 1;

        private const int Keyboard_Type_Magic = 0x11111111;
        private const int Mouse_Type_Magic = 0x22222222;

        private const int Connect_Timeout_MS = 1500;
        private const int Reconnect_Backoff_MS = 2000;

        private const int Max_Axis_Delta = 127; // HID 리포트 축 값 범위

        private string m_ip_address;
        private int m_port;

        private readonly object m_sync = new object();

        private TcpClient m_keyboard_sock = null;
        private TcpClient m_mouse_sock = null;
        private NetworkStream m_keyboard_stream = null;
        private NetworkStream m_mouse_stream = null;

        private volatile int m_state = State_Disconnected;
        private DateTime m_last_reconnect_try = DateTime.MinValue;

        private byte[] m_keyboard_buffer = new byte[8]; // [modifier][reserved][key x6]
        private byte[] m_mouse_buffer = new byte[3];    // [buttons][dx][dy]

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

        public bool IsConnected
        {
            get { return m_state == State_Connected; }
        }

        public bool Connect()
        {
            lock (m_sync)
            {
                return Try_Establish();
            }
        }

        public bool Disconnect()
        {
            lock (m_sync)
            {
                Close_Sockets();
                m_state = State_Disconnected;
                Initial_Keyboard();
                m_keyboard_buffer = new byte[8];
                m_mouse_buffer = new byte[3];
            }

            return true;
        }

        /// <summary>
        /// 워치독 타이머에서 주기적으로 호출한다.
        /// 연결이 살아있으면 현재 HID 상태를 재전송(하트비트) 하여 NAT/방화벽 세션을 유지하고,
        /// 끊겨 있으면 재접속을 시도한다. (백그라운드 스레드에서 호출할 것)
        /// </summary>
        public bool Watchdog_Pulse()
        {
            lock (m_sync)
            {
                if (m_state == State_Connected)
                {
                    if (Flush_Safe())
                        return true;

                    // 쓰기 실패 = 링크 끊김. 즉시 재접속 대상으로 전환.
                    m_state = State_Disconnected;
                }

                if ((DateTime.UtcNow - m_last_reconnect_try).TotalMilliseconds < Reconnect_Backoff_MS)
                    return false;

                m_last_reconnect_try = DateTime.UtcNow;
                return Try_Establish();
            }
        }

        public void Mouse_Move(int rel_x, int rel_y)
        {
            lock (m_sync)
            {
                if (m_state != State_Connected)
                    return;

                try
                {
                    // ±127 을 초과하는 이동은 축 합산이 보존되게 여러 리포트로 분할 전송한다.
                    // (기존 구현은 분할 후 클램프한 값을 마지막 리포트에 다시 실어 과전송했다)
                    // 버튼(byte[0])은 눌림 상태를 그대로 유지한 채로 전송한다.
                    while (rel_x > Max_Axis_Delta || rel_x < -Max_Axis_Delta ||
                           rel_y > Max_Axis_Delta || rel_y < -Max_Axis_Delta)
                    {
                        int step_x = Clamp_Axis(rel_x);
                        int step_y = Clamp_Axis(rel_y);
                        m_mouse_buffer[1] = (byte)(sbyte)step_x;
                        m_mouse_buffer[2] = (byte)(sbyte)step_y;
                        m_mouse_stream.Write(m_mouse_buffer, 0, m_mouse_buffer.Length);
                        rel_x -= step_x;
                        rel_y -= step_y;
                    }

                    if (rel_x == 0 && rel_y == 0)
                        return;

                    m_mouse_buffer[1] = (byte)(sbyte)rel_x;
                    m_mouse_buffer[2] = (byte)(sbyte)rel_y;
                    m_mouse_stream.Write(m_mouse_buffer, 0, m_mouse_buffer.Length);
                }
                catch (Exception)
                {
                    // 링크 끊김: 워치독이 재접속을 담당한다. 예외를 흡수하여 UI를 보호한다.
                    m_state = State_Disconnected;
                }
            }
        }

        /// <summary>
        /// 현재 눌려 있는 모든 입력(키보드 키/모디파이어/마우스 버튼)을 원격에 Up 으로 해제한다.
        /// 머신전환(Alt+Ctrl+Shift) 토글 시점에 호출하여 원격 쪽 키/버튼 고착(stuck)을 방지한다.
        /// </summary>
        public void Flush_Pressed_State()
        {
            lock (m_sync)
            {
                bool keyboard_dirty = m_press_key_index.Count > 0 || m_keyboard_buffer[0] != 0;
                bool mouse_dirty = m_mouse_buffer[0] != 0;

                foreach (KeyValuePair<Key, int> entry in m_press_key_index)
                    m_keyboard_buffer[entry.Value] = 0;
                m_press_key_index.Clear();
                Initial_Keyboard();
                m_keyboard_buffer[0] = 0;

                m_mouse_buffer[0] = 0;
                m_mouse_buffer[1] = 0;
                m_mouse_buffer[2] = 0;

                if (m_state != State_Connected)
                    return;

                try
                {
                    if (keyboard_dirty)
                        m_keyboard_stream.Write(m_keyboard_buffer, 0, m_keyboard_buffer.Length);
                    if (mouse_dirty)
                        m_mouse_stream.Write(m_mouse_buffer, 0, m_mouse_buffer.Length);
                }
                catch (Exception)
                {
                    m_state = State_Disconnected;
                }
            }
        }

        public void Mouse_Down(MouseButton button)
        {
            byte button_bit = Mouse_Button_Bit(button);

            lock (m_sync)
            {
                // 버튼 상태는 누적(|=) 관리 — 기존 코드는 덮어써서 다중 버튼 입력이 깨졌다.
                m_mouse_buffer[0] |= button_bit;
                m_mouse_buffer[1] = 0;
                m_mouse_buffer[2] = 0;
                Mouse_Send_Guarded();
            }
        }

        public void Mouse_Up(MouseButton button)
        {
            byte button_bit = Mouse_Button_Bit(button);

            lock (m_sync)
            {
                m_mouse_buffer[0] &= (byte)~button_bit;
                m_mouse_buffer[1] = 0;
                m_mouse_buffer[2] = 0;
                Mouse_Send_Guarded();
            }
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

            lock (m_sync)
            {
                if (!Get_Keyboard_IsPressed(key, ref pressed_index) &&
                    Get_Keyboard_Free_Slot(ref pressed_index))
                {
                    m_keyboard_buffer[pressed_index] = Convert.ToByte(key);
                    Keyboard_Send_Guarded();
                    m_press_key_index.Add(key, pressed_index);
                    ret = true;
                }
            }

            return ret;
        }

        public bool Keyboard_Down(Control_Key key)
        {
            lock (m_sync)
            {
                if (!Get_Keyboard_IsPressed(key))
                {
                    m_keyboard_buffer[0] |= Convert.ToByte(key);
                    Keyboard_Send_Guarded();
                    return true;
                }
            }

            return false;
        }

        public bool Keyboard_Up(Key key)
        {
            int pressed_index = -1;
            bool ret = false;

            lock (m_sync)
            {
                if (Get_Keyboard_IsPressed(key, ref pressed_index))
                {
                    m_keyboard_buffer[pressed_index] = 0;
                    Keyboard_Send_Guarded();
                    m_press_key_index.Remove(key);
                    Insert_Keyboard_Free_Slot(pressed_index);
                    ret = true;
                }
            }

            return ret;
        }

        public bool Keyboard_Up(Control_Key key)
        {
            lock (m_sync)
            {
                if (Get_Keyboard_IsPressed(key))
                {
                    // ~(byte)key : Control_Key(enum)에 ~ 연산 시 int로 승격되어 CS0034 방지
                    m_keyboard_buffer[0] &= (byte)~(byte)key;
                    Keyboard_Send_Guarded();
                    return true;
                }
            }

            return false;
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

        private static byte Mouse_Button_Bit(MouseButton button)
        {
            switch (button)
            {
                case MouseButton.Left: return 0x01;
                case MouseButton.Right: return 0x02;
                case MouseButton.Middle: return 0x04;
                default: return 0x00;
            }
        }

        private static int Clamp_Axis(int value)
        {
            if (value > Max_Axis_Delta) return Max_Axis_Delta;
            if (value < -Max_Axis_Delta) return -Max_Axis_Delta;
            return value;
        }

        private void Mouse_Send_Guarded()
        {
            if (m_state != State_Connected)
                return;

            try
            {
                m_mouse_stream.Write(m_mouse_buffer, 0, m_mouse_buffer.Length);
            }
            catch (Exception)
            {
                m_state = State_Disconnected;
            }
        }

        private void Keyboard_Send_Guarded()
        {
            if (m_state != State_Connected)
                return;

            try
            {
                m_keyboard_stream.Write(m_keyboard_buffer, 0, m_keyboard_buffer.Length);
            }
            catch (Exception)
            {
                m_state = State_Disconnected;
            }
        }

        private bool Flush_Safe()
        {
            try
            {
                m_keyboard_stream.Write(m_keyboard_buffer, 0, m_keyboard_buffer.Length);
                m_mouse_buffer[1] = 0;
                m_mouse_buffer[2] = 0;
                m_mouse_stream.Write(m_mouse_buffer, 0, m_mouse_buffer.Length);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// 두 소켓을 새로 연결하고 악수 매직 + 현재 HID 상태를 재전송한다. (m_sync 유지 상태에서 호출)
        /// </summary>
        private bool Try_Establish()
        {
            Close_Sockets();

            TcpClient keyboard = null;
            TcpClient mouse = null;

            try
            {
                keyboard = Create_Connected_Socket();
                mouse = Create_Connected_Socket();

                NetworkStream keyboard_stream = keyboard.GetStream();
                NetworkStream mouse_stream = mouse.GetStream();

                byte[] keyboard_magic = BitConverter.GetBytes(Keyboard_Type_Magic);
                byte[] mouse_magic = BitConverter.GetBytes(Mouse_Type_Magic);

                keyboard_stream.Write(keyboard_magic, 0, keyboard_magic.Length);
                mouse_stream.Write(mouse_magic, 0, mouse_magic.Length);

                // 재접속 직후 현재 눌린 키/버튼 상태를 재전송하여 데몬과 동기화한다.
                keyboard_stream.Write(m_keyboard_buffer, 0, m_keyboard_buffer.Length);
                mouse_stream.Write(m_mouse_buffer, 0, m_mouse_buffer.Length);

                m_keyboard_sock = keyboard;
                m_mouse_sock = mouse;
                m_keyboard_stream = keyboard_stream;
                m_mouse_stream = mouse_stream;

                m_state = State_Connected;
            }
            catch (Exception)
            {
                Safe_Close(keyboard);
                Safe_Close(mouse);

                m_keyboard_stream = null;
                m_mouse_stream = null;

                m_state = State_Disconnected;
                return false;
            }

            return true;
        }

        private TcpClient Create_Connected_Socket()
        {
            TcpClient client = new TcpClient();
            client.NoDelay = true;

            // TCP keepalive: 4초 유휴 후 1초 간격 프robe -> 공유기 Wi-Fi 절전/NAT 세션 유지에 필수.
            try
            {
                byte[] keepalive_values = new byte[12];
                BitConverter.GetBytes((uint)1).CopyTo(keepalive_values, 0);      // on/off
                BitConverter.GetBytes((uint)4000).CopyTo(keepalive_values, 4);   // 시작 전 대기
                BitConverter.GetBytes((uint)1000).CopyTo(keepalive_values, 8);   // probe 주기
                client.Client.IOControl(IOControlCode.KeepAliveValues, keepalive_values, null);
            }
            catch (Exception)
            {
                // 구버전/플랫폼에 따라 실패해도 기본 keepalive 는 SocketOptionName.KeepAlive 로 유지.
                try { client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true); }
                catch (Exception) { }
            }

            IAsyncResult async = client.BeginConnect(m_ip_address, m_port, null, null);
            if (!async.AsyncWaitHandle.WaitOne(TimeSpan.FromMilliseconds(Connect_Timeout_MS)))
            {
                client.Close();
                throw new TimeoutException("HID daemon connect timeout");
            }
            client.EndConnect(async);

            return client;
        }

        private void Close_Sockets()
        {
            Safe_Close(m_keyboard_sock);
            Safe_Close(m_mouse_sock);

            m_keyboard_sock = null;
            m_mouse_sock = null;
            m_keyboard_stream = null;
            m_mouse_stream = null;
        }

        private static void Safe_Close(TcpClient client)
        {
            try { if (client != null) client.Close(); }
            catch (Exception) { }
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

        private bool Get_Keyboard_IsPressed(Control_Key key)
        {
            byte key_byte = (byte)key;
            return (m_keyboard_buffer[0] & key_byte) == key_byte;
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
