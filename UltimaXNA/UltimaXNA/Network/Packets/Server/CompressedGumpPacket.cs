﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UltimaXNA.Network.Packets.Server
{
    public class CompressedGumpPacket : RecvPacket
    {
        public readonly Serial Serial;
        public readonly Serial GumpID;
        public readonly int X;
        public readonly int Y;
        public readonly string GumpData;
        public readonly string[] TextLines;

        public CompressedGumpPacket(PacketReader reader)
            : base(0xDD, "Compressed Gump")
        {
            Serial = reader.ReadInt32();
            GumpID = reader.ReadInt32();
            X = reader.ReadInt32();
            Y = reader.ReadInt32();
            int compressedLength = reader.ReadInt32() - 4;
            int decompressedLength = reader.ReadInt32();
            byte[] compressedData = reader.ReadBytes(compressedLength);
            int numTextLines = reader.ReadInt32();
            int compressedTextLength = reader.ReadInt32() - 4;
            int decompressedTextLength = reader.ReadInt32();
            byte[] compressedTextData = reader.ReadBytes(compressedTextLength);

            byte[] decompressedData = new byte[decompressedLength]; ;
            Compression.Unpack(decompressedData, ref decompressedLength, compressedData, compressedLength);
            GumpData = Encoding.ASCII.GetString(decompressedData);
            byte[] decompressedText = new byte[decompressedTextLength];
            Compression.Unpack(decompressedText, ref decompressedTextLength, compressedTextData, compressedTextLength);

            int index = 0;
            List<string> lines = new List<string>();
            for (int i = 0; i < numTextLines; i++)
            {
                int length = decompressedText[++index] + decompressedText[++index] * 256;
                byte[] b = new byte[length * 2];
                Array.Copy(decompressedText, index, b, 0, length * 2);
                index += length * 2;
                lines.Add(Encoding.BigEndianUnicode.GetString(b));
            }

            TextLines = lines.ToArray();
        }
    }
}