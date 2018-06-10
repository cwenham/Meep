using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Serialization;

using Newtonsoft.Json;

namespace MeepModel
{
    /// <summary>
    /// Extension methods for common
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Return the SHA 256 hash of a string
        /// </summary>
        /// <returns>The SHA 256.</returns>
        /// <param name="text">Text.</param>
        public static string ToSHA256(this string text)
        {
            SHA256 sha = System.Security.Cryptography.SHA256.Create();
            return HashToHexString(text, sha);
        }

        /// <summary>
        /// Return the SHA 512 hash of a string
        /// </summary>
        /// <returns>The SHA 512.</returns>
        /// <param name="text">Text.</param>
        public static string ToSHA512(this string text)
        {
            SHA512 sha = System.Security.Cryptography.SHA512.Create();
            return HashToHexString(text, sha);
        }

        /// <summary>
        /// Return the MD5 hash of a string
        /// </summary>
        /// <returns>The MD.</returns>
        /// <param name="text">Text.</param>
        public static string ToMD5(this string text)
        {
            MD5 md5 = System.Security.Cryptography.MD5.Create();
            return HashToHexString(text, md5);
        }

        public static string HashToHexString(String input, HashAlgorithm alg)
        {
            byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
            byte[] hashBytes = alg.ComputeHash(inputBytes);

            // Convert the byte array to hexadecimal string
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hashBytes.Length; i++)
            {
                sb.Append(hashBytes[i].ToString("x2"));
            }
            return sb.ToString();
        }

        /// <summary>
        /// Serialise object to a string of XML
        /// </summary>
        /// <returns>The xml.</returns>
        /// <param name="obj">Object.</param>
        public static string ToXML(this object obj)
        {
            XmlSerializer serialiser = new XmlSerializer(obj.GetType());

            using (TextWriter writer = new StringWriter())
            {
                serialiser.Serialize(writer, obj);
                return writer.ToString();
            }
        }

        /// <summary>
        /// Serialise object to a string of JSON
        /// </summary>
        /// <returns>The json.</returns>
        /// <param name="obj">Object.</param>
        public static string ToJSON(this object obj)
        {
            return JsonConvert.SerializeObject(obj, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Objects
            });
        }
    }
}
