﻿using System.Diagnostics;
using System.IO;
using System.Linq;
using SWSniff.Core.Interfaces;

namespace SWSniff.SoulWorker.Packets
{
    public abstract class SWPacket
    {
        public byte ID0;
        public byte ID1;
        public byte ID2;
        public ushort ID => (ushort)(ID2 + (ID1 << 8));
        public byte[] Data;

        public static SWPacket Parse(byte[] allData)
        {
#if DEBUG
            var beforeDecrypt = allData.Clone();
#endif
            GeneralHelper.XorPacket(allData);
            using (var ms = new MemoryStream(allData))
            using (var br = new BinaryReader(ms)) {
                br.ReadInt16();
                var len = br.ReadInt16();
                var id0 = br.ReadByte();
                var id1 = br.ReadByte();
                var id2 = br.ReadByte();
                byte[] packetData = br.ReadBytes(len - 7);

                //TODO: if not at end of stream, continue reading the next packet
                Debug.Assert(ms.Position == ms.Length, "Not at end of stream after reading packet");

                SWPacket ret = GetCorrectPacket((PacketType)(id2 + (id1 << 8)));
                ret.ID0 = id0;
                ret.ID1 = id1;
                ret.ID2 = id2;
                ret.Data = packetData;
                ret.Deserialize(ret.Data);
                return ret;

            }
        }

        private static SWPacket GetCorrectPacket(PacketType t)
        {
            switch (t) {
                case PacketType.BothSystemKeepAlive: return new PacketSystemKeepAlive();

                case PacketType.ClientMovementMove: return new PacketMovementMove();
                case PacketType.ClientMovementStop: return new PacketMovementStop();
                case PacketType.ClientMovementJump: return new PacketMovementJump();

                case PacketType.ClientChatNormal: return new PacketChatSend();

                case PacketType.ClientDropPickUp: return new PacketDropPickUp();

                case PacketType.ClientItemMove: return new PacketItemMove();
                case PacketType.ClientItemCombine: return new PacketItemJoinStack();
                case PacketType.ClientItemDivide: return new PacketItemSplitStack();
                case PacketType.ClientItemMoveMoney: return new PacketItemMoveMoney();
                case PacketType.ClientItemLineUp: return new PacketItemSort();

                case PacketType.ClientShopBuy:  return new PacketShopBuy();
                case PacketType.ClientShopSell: return new PacketShopSell();

                case PacketType.ClientTradeUpdateItem: return new PacketTradeUpdateItem();
                case PacketType.ClientTradeUpdateMoney: return new PacketTradeUpdateMoney();

                default: return new GenericSWPacket();
            }
        }

        protected abstract void Deserialize(byte[] data);

        public override string ToString()
        {
            string dataString = Data.Length == 0 ? string.Empty : ": " + string.Join("-", Data.Select(x => x.ToString("X2")));
            return $"{this.IDString()}{dataString}";
        }
    }
    
    public class GenericSWPacket : SWPacket, ICanSerialize
    {
        protected override void Deserialize(byte[] data) { }
        public byte[] Serialize() => new byte[0];
    }
}
