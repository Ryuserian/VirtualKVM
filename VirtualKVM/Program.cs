using System;
using System.IO;
using System.Windows.Forms;

namespace VirtualKVM
{
    internal static class Program
    {
        /// <summary>
        /// 해당 애플리케이션의 주 진입점입니다.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // 트레이 앱: 예외가 발생해도 창이 없어 증적 없이 종료되므로
            // 실행 파일 옆 crash.log 에 기록하여 원인 파악이 가능하도록 한다.
            AppDomain.CurrentDomain.UnhandledException += (s, e) => Write_Crash_Log("UnhandledException", e.ExceptionObject as Exception);
            Application.ThreadException += (s, e) => Write_Crash_Log("ThreadException", e.Exception);

            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new WindowMain());
            }
            catch (Exception ex)
            {
                Write_Crash_Log("Main", ex);
                throw;
            }
        }

        private static void Write_Crash_Log(string source, Exception ex)
        {
            try
            {
                string path = Path.Combine(AppContext.BaseDirectory, "crash.log");
                string line = string.Format("[{0:yyyy-MM-dd HH:mm:ss}] {1}: {2}\r\n{3}\r\n\r\n",
                    DateTime.Now, source, ex == null ? "(null)" : ex.ToString(),
                    Environment.StackTrace);
                File.AppendAllText(path, line);
            }
            catch
            {
                // 로깅 실패로는 크래시를 가리지 않는다.
            }
        }
    }
}
