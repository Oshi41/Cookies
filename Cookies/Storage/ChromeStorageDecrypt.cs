using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Cookies.Storage
{
    /**
     * Выцепляет все сохранённые куки из Google Chrome
     * Работает, опираясь на файл БД куков браузера
     * Расшифровывает и передает уже готовые к использованию
     */
    public class ChromeStorageDecrypt : IStorage
    {
        private readonly byte[] _decryptedKey;
        private readonly string _chromeCookiePath;

        public ChromeStorageDecrypt()
        {
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Google", "Chrome", "User Data", "Local State");

            if (!File.Exists(path))
            {
                throw new FileNotFoundException("Cannot find file with encrypted key", path);
            }

            var json = File.ReadAllText(path);
            var encKey = Newtonsoft.Json.Linq.JObject.Parse(json)["os_crypt"]["encrypted_key"].ToString();
            _decryptedKey = ProtectedData.Unprotect(Convert.FromBase64String(encKey).Skip(5).ToArray(),
                null,
                DataProtectionScope.LocalMachine);

            _chromeCookiePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Google", "Chrome", "User Data", "Default", "cookies");

            if (!File.Exists(_chromeCookiePath))
            {
                throw new FileNotFoundException("Can't find Google Chrome cookie file", _chromeCookiePath);
            }
        }

        public async Task<List<Cookie>> GetCookies()
        {
            using (var conn = new SQLiteConnection($"Data Source={_chromeCookiePath}"))
            {
                await conn.OpenAsync();

                // Формат описан тут
                // https://metacpan.org/pod/HTTP::Cookies::Chrome
                using (var command =
                    new SQLiteCommand(
                        "select encrypted_value, name, path, host_key, expires_utc, is_secure, has_expires, is_httponly  from cookies",
                        conn))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        var list = new List<Cookie>();

                        while (await reader.ReadAsync())
                        {
                            try
                            {
                                var expires = DateTime.MaxValue;

                                if (Equals(1, reader["has_expires"]))
                                {
                                    // https://stackoverflow.com/questions/43518199/cookies-expiration-time-format 
                                    expires = DateTime.UnixEpoch.AddSeconds(((long) reader["expires_utc"] / 1000000) -
                                                                            11644473600);
                                }


                                var cookie = new Cookie(
                                    (string) reader["name"],
                                    Decrypt((byte[]) reader["encrypted_value"]),
                                    (string) reader["path"],
                                    (string) reader["host_key"])
                                {
                                    Expires = expires,
                                    Secure = Equals(1, reader["is_secure"]),
                                    HttpOnly = Equals(1, reader["is_httponly"]),
                                };

                                list.Add(cookie);
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e);
                            }
                        }

                        if (list.Any())
                        {
                            return list;
                        }
                    }
                }
            }

            return null;
        }

        private string Decrypt(byte[] encrypted)
        {
            if (_decryptedKey != null && "v10".Equals(Encoding.UTF8.GetString(encrypted.Take(3).ToArray())))
            {
                // Первые 12 байт
                var nonce = encrypted.Skip(3).Take(12).ToArray();

                // Последние 16 байт
                var tag = encrypted.TakeLast(16).ToArray();
                
                // все оставшееся - текст
                var cipher = encrypted.Skip(3 + 12).Take(encrypted.Length - 3 - 12 - 16).ToArray();

                var aesGcm = new AesGcm(_decryptedKey);
                var plaintext = new byte[cipher.Length];
                
                aesGcm.Decrypt(nonce, cipher, tag, plaintext);
                encrypted = plaintext;
            }
            else
            {
                // Использую DPI
                encrypted = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
            }

            return Encoding.UTF8.GetString(encrypted);
        }
    }
}