using System.IO;

namespace CenturyGame.Framework.Network
{
    public interface IMsgProcesser
    {
        void WriteSendBytes(byte[] data, Stream stream);

        bool ReadRecvBytes(Stream stream, out byte[] msgData);

        void DispatchMsg(byte[] data);
    }
}