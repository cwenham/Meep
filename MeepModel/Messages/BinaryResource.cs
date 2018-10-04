using System;
using System.Runtime.Serialization;

namespace MeepLib.Messages
{
    /// <summary>
    /// Contents of a file read into RAM and stored as a byte array
    /// </summary>
    public class BinaryResource : LocalisedResource
    {
        public byte[] Bytes { get; set; }
    }
}
