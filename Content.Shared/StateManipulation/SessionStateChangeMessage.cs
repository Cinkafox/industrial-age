using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.StateManipulation;

public sealed class SessionStateChangeMessage : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;

    public string type = "";
    
    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        type = buffer.ReadString();
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(type);
    }
}