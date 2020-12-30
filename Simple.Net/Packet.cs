using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Simple.Net {
    public class Packet : IDisposable
    {
        List<byte> buffer = new List<byte>();
        public int Length { get => buffer.Count; }
        int readPos = 0;

        public Packet() {}

        #region For parse
        public static Packet Parse(byte[] buffer) {
            try {
                return new Packet(buffer);
            }
            catch { throw new ArgumentException("Buffer cannot be null"); }
        }

        Packet(byte[] bytes) => buffer.AddRange(bytes);
        #endregion

        #region read
        public byte readByte() {
            try { return buffer[readPos++];} 
            catch { throw new Exception("Cannot read 'byte', either the packet is not initilized or the packet reached the end of the buffer"); }
        }
        public char readChar() => (char)readByte();
        public byte[] readBytes(int length) {
            byte[] _result = new byte[length];
            for (int i = 0; i < _result.Length; i++) _result[i] = readByte();
            return _result;
        }
        public short readShort() {
            try {
                short _result = BitConverter.ToInt16(buffer.ToArray(), readPos);
                readPos += 2;
                return _result;
            }
            catch { throw new Exception("Cannot read 'short', either the packet is not initilized or the packet reached the end of the buffer"); }
        }
        public int readInt() {
            try {
                int _result = BitConverter.ToInt32(buffer.ToArray(), readPos);
                readPos += 4;
                return _result;
            }
            catch { throw new Exception("Cannot read 'int', either the packet is not initilized or the packet reached the end of the buffer"); }
        }
        public long readLong() {
            try {
                long _result = BitConverter.ToInt64(buffer.ToArray(), readPos);
                readPos += 8;
                return _result;
            }
            catch { throw new Exception("Cannot read 'long', either the packet is not initilized or the packet reached the end of the buffer"); }
        }
        public float readFloat() {
            try {
                float _result = BitConverter.ToSingle(buffer.ToArray(), readPos);
                readPos += 4;
                return _result;
            }
            catch { throw new Exception("Cannot read 'float', either the packet is not initilized or the packet reached the end of the buffer"); }
        }
        public bool readBool() {
            try {
                return BitConverter.ToBoolean(buffer.ToArray(), readPos++);
            }
            catch { throw new Exception("Cannot read 'bool', either the packet is not initilized or the packet reached the end of the buffer"); }
        }
        public string readString() {
            try {
                int _length = readInt();
                string _result = Encoding.ASCII.GetString(buffer.ToArray(), readPos, _length);
                readPos += _length;
                return _result;
            }
            catch { throw new Exception("Cannot read 'string', either the packet is not initilized or the packet reached the end of the buffer"); }
        }
        public Vector2 readVector2() => new Vector2(readFloat(), readFloat());
        public Vector3 readVector3() => new Vector3(readFloat(), readFloat(), readFloat());
        public Quaternion readQuaternion() => new Quaternion(readFloat(), readFloat(), readFloat(), readFloat());
        string header;
        public string readHeader() {
            if (header != null) return header;
            header = readString();
            buffer.RemoveRange(0, readPos);
            readPos = 0;
            return header;
        }
        #endregion

        #region write
        public void Write(byte value) => buffer.Add(value);

        public void Write(char value) => Write((byte) value);
        public void Write(byte[] values) {
            foreach (byte value in values)
                Write(value);
        }
        public void Write(short value) => buffer.AddRange(BitConverter.GetBytes(value));
        public void Write(int value) => buffer.AddRange(BitConverter.GetBytes(value));
        public void Write(long value) => buffer.AddRange(BitConverter.GetBytes(value));
        public void Write(float value) => buffer.AddRange(BitConverter.GetBytes(value));
        public void Write(bool value) => buffer.AddRange(BitConverter.GetBytes(value));
        public void Write(string value) {
            byte[] _value = Encoding.ASCII.GetBytes(value);
            Write(_value.Length);
            buffer.AddRange(_value);
        }
        public void Write(Vector2 value) {
            Write(value.X);
            Write(value.Y);
        }
        public void Write(Vector3 value) {
            Write(value.X);
            Write(value.Y);
            Write(value.Z);
        }
        public void Write(Quaternion value) {
            Write(value.X);
            Write(value.Y);
            Write(value.Z);
            Write(value.W);
        }
        public void Insert(int value) => buffer.InsertRange(0, BitConverter.GetBytes(value));
        public void Insert(string value) {
            buffer.InsertRange(0, Encoding.ASCII.GetBytes(value));
            Insert(value.Length);
        }
        #endregion

        public byte[] ToByteArray() => buffer.ToArray();

        public void Dispose() => GC.SuppressFinalize(this);
    }
}