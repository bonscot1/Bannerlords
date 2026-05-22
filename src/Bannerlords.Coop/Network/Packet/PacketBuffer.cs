using System;
using System.Text;

namespace Bannerlords.Coop.Network.Packet
{
    /// <summary>
    /// Minimal, allocation-light wire buffer. Little-endian, length-prefixed
    /// strings (UTF-8), no framing — framing is the transport's job.
    ///
    /// Not thread-safe. One buffer per send/receive call.
    /// </summary>
    public sealed class PacketBuffer
    {
        private byte[] _buf;
        private int _pos;
        private int _len;

        public int Position => _pos;
        public int Length => _len;
        public int Remaining => _len - _pos;

        public PacketBuffer(int capacity = 256)
        {
            _buf = new byte[capacity];
        }

        public PacketBuffer(byte[] data, int length)
        {
            _buf = data;
            _len = length;
            _pos = 0;
        }

        public byte[] ToArray()
        {
            var r = new byte[_len];
            Buffer.BlockCopy(_buf, 0, r, 0, _len);
            return r;
        }

        // ---------- write ----------

        private void EnsureWrite(int n)
        {
            var needed = _pos + n;
            if (needed <= _buf.Length) return;
            var newCap = _buf.Length;
            while (newCap < needed) newCap *= 2;
            var nb = new byte[newCap];
            Buffer.BlockCopy(_buf, 0, nb, 0, _len);
            _buf = nb;
        }

        public void WriteByte(byte v)
        {
            EnsureWrite(1);
            _buf[_pos++] = v;
            if (_pos > _len) _len = _pos;
        }

        public void WriteBool(bool v) => WriteByte((byte)(v ? 1 : 0));

        public void WriteUShort(ushort v)
        {
            EnsureWrite(2);
            _buf[_pos++] = (byte)(v & 0xFF);
            _buf[_pos++] = (byte)((v >> 8) & 0xFF);
            if (_pos > _len) _len = _pos;
        }

        public void WriteInt(int v) => WriteUInt(unchecked((uint)v));

        public void WriteUInt(uint v)
        {
            EnsureWrite(4);
            _buf[_pos++] = (byte)(v & 0xFF);
            _buf[_pos++] = (byte)((v >> 8) & 0xFF);
            _buf[_pos++] = (byte)((v >> 16) & 0xFF);
            _buf[_pos++] = (byte)((v >> 24) & 0xFF);
            if (_pos > _len) _len = _pos;
        }

        public void WriteLong(long v) => WriteULong(unchecked((ulong)v));

        public void WriteULong(ulong v)
        {
            EnsureWrite(8);
            for (var i = 0; i < 8; i++)
                _buf[_pos++] = (byte)((v >> (i * 8)) & 0xFF);
            if (_pos > _len) _len = _pos;
        }

        public unsafe void WriteFloat(float v)
        {
            var u = *(uint*)&v;
            WriteUInt(u);
        }

        public void WriteString(string s)
        {
            if (s == null) { WriteUShort(0); return; }
            var bytes = Encoding.UTF8.GetBytes(s);
            if (bytes.Length > ushort.MaxValue)
                throw new InvalidOperationException("string too long for wire");
            WriteUShort((ushort)bytes.Length);
            EnsureWrite(bytes.Length);
            Buffer.BlockCopy(bytes, 0, _buf, _pos, bytes.Length);
            _pos += bytes.Length;
            if (_pos > _len) _len = _pos;
        }

        // ---------- read ----------

        private void EnsureRead(int n)
        {
            if (_pos + n > _len)
                throw new InvalidOperationException(
                    $"read past end of packet (pos={_pos} +{n} > len={_len})");
        }

        public byte ReadByte()
        {
            EnsureRead(1);
            return _buf[_pos++];
        }

        public bool ReadBool() => ReadByte() != 0;

        public ushort ReadUShort()
        {
            EnsureRead(2);
            ushort v = (ushort)(_buf[_pos] | (_buf[_pos + 1] << 8));
            _pos += 2;
            return v;
        }

        public int ReadInt() => unchecked((int)ReadUInt());

        public uint ReadUInt()
        {
            EnsureRead(4);
            uint v = (uint)(_buf[_pos]
                       | (_buf[_pos + 1] << 8)
                       | (_buf[_pos + 2] << 16)
                       | (_buf[_pos + 3] << 24));
            _pos += 4;
            return v;
        }

        public long ReadLong() => unchecked((long)ReadULong());

        public ulong ReadULong()
        {
            EnsureRead(8);
            ulong v = 0;
            for (var i = 0; i < 8; i++)
                v |= ((ulong)_buf[_pos++]) << (i * 8);
            return v;
        }

        public unsafe float ReadFloat()
        {
            var u = ReadUInt();
            return *(float*)&u;
        }

        public string ReadString()
        {
            var n = ReadUShort();
            if (n == 0) return string.Empty;
            EnsureRead(n);
            var s = Encoding.UTF8.GetString(_buf, _pos, n);
            _pos += n;
            return s;
        }
    }
}
