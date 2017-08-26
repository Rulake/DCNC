﻿using System.IO;
using Shared.Util;

namespace Shared.Network.GameServer
{
    public class JoinChannelAnswer : OutPacket
    {
        public string ChannelName;
        public string CharacterName;
        public ushort Serial;
        public ushort SessionAge;

        public override Packet CreatePacket()
        {
            return base.CreatePacket(Packets.JoinChannelAck);
        }

        public override byte[] GetBytes()
        {
            using (var ms = new MemoryStream())
            {
                using (var bs = new BinaryWriterExt(ms))
                {
                    bs.WriteUnicodeStatic(ChannelName, 10);
                    bs.WriteUnicodeStatic(CharacterName, 16);
                    bs.Write(Serial);
                    bs.Write(SessionAge);
                }
                return ms.GetBuffer();
            }
            /*
            ack.Writer.WriteUnicodeStatic("speeding", 10); // ChannelName
            ack.Writer.WriteUnicodeStatic("charName", 16); // CharName

            ack.Writer.Write((ushort) 123); // Serial
            ack.Writer.Write((ushort) 123); // Session Age
            */
        }
    }
}