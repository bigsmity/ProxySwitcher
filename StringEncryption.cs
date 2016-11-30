using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ProxySwitcher
{
    public static class StringEncryption
    {
        static byte[] s_aditionalEntropy = { 2, 2, 0, 9, 8 };

        public static string Protect(string data)
        {
            try
            {
                if (!string.IsNullOrEmpty(data))
                {
                    byte[] dataBytes = Encoding.Unicode.GetBytes(data);
                    byte[] encryptedBytes = ProtectedData.Protect(dataBytes, s_aditionalEntropy, DataProtectionScope.LocalMachine);
                    return Convert.ToBase64String(encryptedBytes);
                }
                return string.Empty;
            }
            catch (Exception e)
            {
                Console.WriteLine("Data was not encrypted. An error occurred.");
                Console.WriteLine(e.ToString());
                return string.Empty;
            }
        }

        public static string Unprotect(string data)
        {
            try
            {
                if (!string.IsNullOrEmpty(data))
                {
                    byte[] dataBytes = Convert.FromBase64String(data);
                    byte[] decryptedBytes = ProtectedData.Unprotect(dataBytes, s_aditionalEntropy, DataProtectionScope.LocalMachine);
                    return Encoding.Unicode.GetString(decryptedBytes);
                }
                return string.Empty;
            }
            catch (Exception e)
            {
                Console.WriteLine("Data was not decrypted. An error occurred.");
                Console.WriteLine(e.ToString());
                return string.Empty;
            }
        }
    }
}
