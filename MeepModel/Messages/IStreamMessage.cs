using System;
using System.IO;
using System.Threading.Tasks;

namespace MeepLib.Messages
{
    public interface IStreamMessage
    {
        Task<Stream> Stream { get; }
    }
}
