using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace VirtualKVM
{
    /// <summary>
    /// 앱 설정(IP/HID 포트/SSH 포트/계정).
    /// %APPDATA%\VirtualKVM\config.json 에 저장하며, 비밀번호는 DPAPI(현 사용자 범위)로
    /// 암호화하여 PasswordEnc(Base64)로만 보관한다.
    /// </summary>
    public class AppConfig
    {
        // 최초 실행(설정 파일 없음) 시의 기본값 — 기존 하드코딩 값과 동일.
        public const string DEFAULT_IP = "192.168.0.101";
        public const string DEFAULT_USER = "pi";
        public const string DEFAULT_PASSWORD = "raspberry";
        public const int DEFAULT_HID_PORT = 5000;
        public const int DEFAULT_SSH_PORT = 22;

        public string IpAddress { get; set; } = DEFAULT_IP;
        public int HidPort { get; set; } = DEFAULT_HID_PORT;
        public int SshPort { get; set; } = DEFAULT_SSH_PORT;
        public string UserId { get; set; } = DEFAULT_USER;

        /// <summary>런타임 전용. JSON에는 PlainPassword를 넣지 않고 PasswordEnc만 남긴다.</summary>
        [JsonIgnore]
        public string PlainPassword { get; set; } = DEFAULT_PASSWORD;

        public string PasswordEnc { get; set; } = "";

        [JsonIgnore]
        public bool HasStoredPassword
        {
            get { return !string.IsNullOrEmpty(PasswordEnc); }
        }

        private static string Config_Dir
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VirtualKVM"); }
        }

        private static string Config_Path
        {
            get { return Path.Combine(Config_Dir, "config.json"); }
        }

        public static AppConfig Load()
        {
            AppConfig config = new AppConfig();

            try
            {
                if (File.Exists(Config_Path))
                {
                    AppConfig loaded = JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(Config_Path));
                    if (loaded != null)
                        config = loaded;
                }
            }
            catch (Exception)
            {
                // 손상된 설정은 기본값으로 복귀(.log 하지 않고 무시)
                config = new AppConfig();
            }

            if (string.IsNullOrWhiteSpace(config.IpAddress)) config.IpAddress = DEFAULT_IP;
            if (config.HidPort <= 0 || config.HidPort > 65535) config.HidPort = DEFAULT_HID_PORT;
            if (config.SshPort <= 0 || config.SshPort > 65535) config.SshPort = DEFAULT_SSH_PORT;
            if (string.IsNullOrWhiteSpace(config.UserId)) config.UserId = DEFAULT_USER;

            config.PlainPassword = config.Decrypt_Password();

            return config;
        }

        public void Save()
        {
            PasswordEnc = Encrypt_Password(PlainPassword ?? string.Empty);

            string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });

            Directory.CreateDirectory(Config_Dir);
            File.WriteAllText(Config_Path, json);
        }

        private string Encrypt_Password(string plain)
        {
            try
            {
                byte[] enc = ProtectedData.Protect(Encoding.UTF8.GetBytes(plain), null, DataProtectionScope.CurrentUser);
                return Convert.ToBase64String(enc);
            }
            catch (Exception)
            {
                // DPAPI 사용 불가 환경(드물음)에서는 평문 저장으로 축소 보존한다.
                return "plain:" + plain;
            }
        }

        private string Decrypt_Password()
        {
            if (string.IsNullOrEmpty(PasswordEnc))
                return DEFAULT_PASSWORD;

            if (PasswordEnc.StartsWith("plain:", StringComparison.Ordinal))
                return PasswordEnc.Substring("plain:".Length);

            try
            {
                byte[] dec = ProtectedData.Unprotect(Convert.FromBase64String(PasswordEnc), null, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(dec);
            }
            catch (Exception)
            {
                // 다른 사용자/머신에서 가져온 파일 등 복호화 불가: 기본값으로 복귀
                return DEFAULT_PASSWORD;
            }
        }
    }
}
