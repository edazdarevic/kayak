﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kayak.Http
{
    interface IOutputStream
    {
        bool Write(ArraySegment<byte> data, Action continuation);
        void End();
    }

    interface IBufferedOutputStreamDelegate
    {
        void OnDrained(IBufferedOutputStream message);
    }

    interface IBufferedOutputStream : IOutputStream
    {
        void Attach(ISocket socket);
    }

    class BufferedOutputStream : IBufferedOutputStream
    {
        ISocket socket;
        LinkedList<byte[]> buffer;
        IBufferedOutputStreamDelegate del;
        Action continuation;
        bool ended;

        public BufferedOutputStream(IBufferedOutputStreamDelegate del)
        {
            this.del = del;
        }

        public void Attach(ISocket socket)
        {
            if (socket == null)
                throw new ArgumentNullException("socket");

            if (this.socket != null)
                throw new InvalidOperationException("Already attached to a socket.");

            this.socket = socket;

            Flush();
        }

        public bool Write(ArraySegment<byte> data, Action continuation)
        {
            // XXX
            if (this.continuation != null)
                throw new InvalidOperationException("the continuation was not null.");

            if (socket == null)
            {
                if (buffer == null)
                    buffer = new LinkedList<byte[]>();

                var b = new byte[data.Count];
                Buffer.BlockCopy(data.Array, data.Offset, b, 0, data.Count);

                buffer.AddLast(b);

                if (continuation != null)
                {
                    Console.WriteLine("weird.");
                    this.continuation = continuation;
                    return true;
                }

                return false;
            }
            else
            {
                // XXX possible that buffer is not empty?
                if (buffer != null && buffer.Count > 0) throw new Exception("Buffer was not empty.");

                return socket.Write(data, continuation);
            }
        }

        public void End()
        {
            ended = true;

            if (socket != null)
                Flush();
        }

        bool BufferIsEmpty()
        {
            return buffer == null || buffer.Count == 0;
        }

        void Flush()
        {
            if (continuation != null)
            {
                var c = continuation;
                continuation = null;
                c();
            }
            while (!BufferIsEmpty())
            {
                var first = buffer.First;
                buffer.RemoveFirst();

                var c = buffer.Count == 0 && this.continuation != null ?
                    this.continuation : null;

                var data = first.Value;
                if (socket.Write(new ArraySegment<byte>(data), c))
                {
                    this.continuation = null;
                    return;
                }
            }

            if (ended)
                del.OnDrained(this);
        }
    }
}