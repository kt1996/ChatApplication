using System.IO;
using System.Security.Cryptography;
using System.Text;
using ChatApplication.Network;

namespace ChatApplication
{
    public static class Encryption
    {
        static internal byte[] EncodeStringUsingAes(byte[] key, string secretMessage, out byte[] iv)
        {
            using (Aes aes = new AesCryptoServiceProvider()) {
                aes.Key = key;
                iv = aes.IV;

                // Encrypt the message
                using (MemoryStream ciphertext = new MemoryStream())
                using (CryptoStream cs = new CryptoStream(ciphertext, aes.CreateEncryptor(), CryptoStreamMode.Write)) {
                    byte[] plaintextMessage = Encoding.UTF8.GetBytes(secretMessage);
                    cs.Write(plaintextMessage, 0, plaintextMessage.Length);
                    cs.Close();
                    byte[] encryptedMessage = ciphertext.ToArray();
                    return encryptedMessage;
                }
            }

        }

        static internal byte[] EncodeByteArrayUsingAes(byte[] key, byte[] plaintextMessage, out byte[] iv)
        {
            using (Aes aes = new AesCryptoServiceProvider()) {
                aes.Key = key;
                iv = aes.IV;

                // Encrypt the message
                using (MemoryStream ciphertext = new MemoryStream())
                using (CryptoStream cs = new CryptoStream(ciphertext, aes.CreateEncryptor(), CryptoStreamMode.Write)) {
                    cs.Write(plaintextMessage, 0, plaintextMessage.Length);
                    cs.Close();
                    byte[] encryptedMessage = ciphertext.ToArray();
                    return encryptedMessage;
                }
            }

        }

        static internal string DecodeToStringUsingAes(byte[] key, byte[] encryptedMessage, byte[] iv)
        {
            using (Aes aes = new AesCryptoServiceProvider()) {
                aes.Key = key;
                aes.IV = iv;
                // Decrypt the message
                using (MemoryStream plaintext = new MemoryStream()) {
                    using (CryptoStream cs = new CryptoStream(plaintext, aes.CreateDecryptor(), CryptoStreamMode.Write)) {
                        cs.Write(encryptedMessage, 0, encryptedMessage.Length);
                        cs.Close();
                        return Encoding.UTF8.GetString(plaintext.ToArray());
                    }
                }
            }
        }

        static internal byte[] DecodeToByteArrayUsingAes(byte[] key, byte[] encryptedMessage, byte[] iv)
        {
            using (Aes aes = new AesCryptoServiceProvider()) {
                aes.Key = key;
                aes.IV = iv;
                // Decrypt the message
                using (MemoryStream plaintext = new MemoryStream()) {
                    using (CryptoStream cs = new CryptoStream(plaintext, aes.CreateDecryptor(), CryptoStreamMode.Write)) {
                        cs.Write(encryptedMessage, 0, encryptedMessage.Length);
                        cs.Close();
                        return plaintext.ToArray();
                    }
                }
            }
        }

        static internal byte[] PerformAssymetricKeyExchangeUsingECDiffieHellmanOnSocket(System.Net.Sockets.Socket socket)
        {
            using (ECDiffieHellmanCng ECDiffieHellmanCngobj = new ECDiffieHellmanCng()) {

                ECDiffieHellmanCngobj.KeyDerivationFunction = ECDiffieHellmanKeyDerivationFunction.Hash;
                ECDiffieHellmanCngobj.HashAlgorithm = CngAlgorithm.Sha256;
                byte[] _myPublicKey = ECDiffieHellmanCngobj.PublicKey.ToByteArray();
                byte[] _otherPublicKey = new byte[140];

                if (NetworkCommunicationManagers.SendByteArrayOverSocket(socket, _myPublicKey) == false) {
                    return null;
                };
                if (NetworkCommunicationManagers.ReceiveByteArrayOverSocket(socket, out _otherPublicKey, 140) == false) {
                    return null;
                };
                
                return ECDiffieHellmanCngobj.DeriveKeyMaterial(CngKey.Import(_otherPublicKey, CngKeyBlobFormat.EccPublicBlob));
            }
        }
    }
}
