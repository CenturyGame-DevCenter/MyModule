using System;

namespace CenturyGame.Framework.Network
{
    public enum EOptionMsgType
    {
        Connect,
        Disconnect,
    }

    public abstract class OptionMsg
    {
        public EOptionMsgType Type { get; protected set; }
    }

    public class ConnectMsg : OptionMsg
    {
        public bool Success;

        public ConnectMsg(bool _success)
        {
            Type = EOptionMsgType.Connect;
            Success = _success;
        }
    }

    public enum DisconnectReason
    {
        Active,
        Passive,
        Exception,
    }

    public class DisconnectMsg : OptionMsg
    {
        public DisconnectReason Reason;

        public DisconnectMsg(DisconnectReason _reason)
        {
            Type = EOptionMsgType.Disconnect;
            Reason = _reason;
        }
    }
}