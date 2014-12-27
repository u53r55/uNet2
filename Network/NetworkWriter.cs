﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using uNet2.Channel;
using uNet2.Extensions;
using uNet2.Packet;
using uNet2.Peer;
using uNet2.SocketOperation;

namespace uNet2.Network
{
    internal class NetworkWriter
    {

        public void WritePacketToSocket(IDataPacket data, IChannel senderChannel, Guid guid, BufferObject buffObj,
            Socket sock, AsyncCallback sendCallback, SocketOperationContext operationCtx)
        {
            var ms = new MemoryStream();
            data.SerializeTo(ms);

            var headerSize = 1 + (operationCtx != null ? 32 : 0);
            var sendBuff2 = new byte[ms.Length + headerSize +4];
            sendBuff2.FastMoveMem(0, BitConverter.GetBytes(headerSize + ms.Length), 4); 
            sendBuff2[4] = operationCtx == null ? ((byte) 0x0) : ((byte) 0x1);

            if (operationCtx != null)
            {
                sendBuff2.FastMoveMem(5, operationCtx.OperationGuid.ToByteArray(), 16);
                sendBuff2.FastMoveMem(21, guid.ToByteArray(), 16);
            }

            var tmpBuff = new byte[ms.Length];
            ms.Seek(0, SeekOrigin.Begin);
            ms.Read(tmpBuff,0,tmpBuff.Length);

            sendBuff2.FastMoveMem(operationCtx != null ? 38 : 5, tmpBuff, tmpBuff.Length);

            var sendObj = new Peer.Peer.SendObject {Channel = senderChannel, Packet = data};
            if (sock.Connected)
                sock.BeginSend(sendBuff2, 0, sendBuff2.Length, 0, sendCallback, sendObj);
        }

        public void WriteSequenceToSocket(SequenceContext seqCtx, IChannel senderChannel, Guid guid,
            BufferObject buffObj,
            Socket sock, AsyncCallback sendCallback)
        {
            WritePacketToSocket(seqCtx.InitPacket, senderChannel, guid, buffObj, sock, sendCallback, null);
            for (var i = 0; i < seqCtx.SequencePackets.Length; i++)
                WritePacketToSocket(seqCtx.SequencePackets[i], senderChannel, guid, buffObj, sock, sendCallback, null);
        }

        internal static void PrependStreamSize(MemoryStream stream)
        {
            var size = stream.Length;
            InsertData(stream, 0, BitConverter.GetBytes((int) size));
        }

        private static void InsertData(MemoryStream stream, int idx, byte[] data)
        {
            var curBuff = stream.ToArray();
            stream.Position = data.Length +idx;
            stream.Write(curBuff, 0, curBuff.Length);
            stream.Position = idx;
            stream.Write(data, 0, data.Length);
            stream.Position += curBuff.Length;
        }
    }
}
