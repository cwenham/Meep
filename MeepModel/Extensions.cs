using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Serialization;
using System.Text.RegularExpressions;

using Newtonsoft.Json;
using Newtonsoft.Json.Bson;

namespace MeepLib.Messages
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

        public static void ToBSONStream(this object obj, Stream output)
        {
            using (BsonWriter writer = new BsonWriter(output))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(writer, obj);
            }
        }

        /// <summary>
        /// Convert a message to enumerable form, exposing a batch's inner
        /// messages if applicable
        /// </summary>
        /// <returns>The enumerable.</returns>
        /// <param name="msg">Message.</param>
        public static IEnumerable<Message> AsEnumerable(this Message msg)
        {
            if (msg is Batch)
                return ((Batch)msg).Messages;
            else
                return new List<Message> { msg };
        }

        /// <summary>
        /// Split into tokens (words)
        /// </summary>
        /// <returns>The tokens.</returns>
        /// <param name="text">Text.</param>
        public static IEnumerable<String> ExtractTokens(this String text)
        {
            foreach (var token in Regex.Replace(text, "\\p{P}+", "").Split(' '))
                yield return token.ToLowerInvariant();
        }
    }
}
