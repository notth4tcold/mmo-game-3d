using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;

public class Packet {
    private List<byte> buffer;
    private byte[] readableBuffer;
    private int readPos;

    public Packet() {
        buffer = new List<byte>();
        readPos = 0;
    }

    public Packet(byte[] data) {
        buffer = new List<byte>();
        readPos = 0;
        SetBytes(data);
    }

    public void SetBytes(byte[] data) {
        Write(data);
        readableBuffer = buffer.ToArray();
    }

    public void WriteLength() {
        buffer.InsertRange(0, BitConverter.GetBytes(buffer.Count));
    }

    public void InsertInt(int value) {
        buffer.InsertRange(0, BitConverter.GetBytes(value));
    }

    public byte[] ToArray() {
        readableBuffer = buffer.ToArray();
        return readableBuffer;
    }

    public int Length() {
        return buffer.Count;
    }

    public int UnreadLength() {
        return Length() - readPos;
    }

    public void Reset(bool shouldReset = true) {
        if (shouldReset) {
            buffer.Clear();
            readableBuffer = null;
            readPos = 0;
        } else {
            readPos -= 4;
        }
    }

    public void Write(byte value) {
        buffer.Add(value);
    }

    public void Write(byte[] value) {
        buffer.AddRange(value);
    }

    public void Write(short value) {
        buffer.AddRange(BitConverter.GetBytes(value));
    }

    public void Write(int value) {
        buffer.AddRange(BitConverter.GetBytes(value));
    }

    public void Write(long value) {
        buffer.AddRange(BitConverter.GetBytes(value));
    }

    public void Write(float value) {
        buffer.AddRange(BitConverter.GetBytes(value));
    }

    public void Write(bool value) {
        buffer.AddRange(BitConverter.GetBytes(value));
    }

    public void Write(string value) {
        Write(value.Length);
        buffer.AddRange(Encoding.ASCII.GetBytes(value));
    }

    public void Write(uint value) {
        buffer.AddRange(BitConverter.GetBytes(value));
    }

    public void Write(Vector3 value) {
        Write(value.x);
        Write(value.y);
        Write(value.z);
    }

    public void Write(Quaternion value) {
        Write(value.x);
        Write(value.y);
        Write(value.z);
        Write(value.w);
    }

    public void Write(Inputs inputs) {
        foreach (var prop in inputs.GetType().GetProperties()) {
            Type type = prop.PropertyType;
            object value = prop.GetValue(inputs, null);
            if (type == typeof(bool)) Write((bool)value);
        }
    }

    public void Write(InputPayload inputPayload) {
        foreach (var prop in inputPayload.GetType().GetProperties()) {
            Type type = prop.PropertyType;
            object value = prop.GetValue(inputPayload, null);
            if (type == typeof(float)) Write((float)value);
            if (type == typeof(uint)) Write((uint)value);
            if (type == typeof(Inputs)) Write((Inputs)value);
        }
    }

    public void Write(StatePayload statePayload) {
        foreach (var prop in statePayload.GetType().GetProperties()) {
            Type type = prop.PropertyType;
            object value = prop.GetValue(statePayload, null);
            if (type == typeof(float)) Write((float)value);
            if (type == typeof(uint)) Write((uint)value);
            if (type == typeof(Vector3)) Write((Vector3)value);
            if (type == typeof(Quaternion)) Write((Quaternion)value);
        }
    }

    public byte ReadByte(bool moveReadPos = true) {
        if (buffer.Count > readPos) {
            byte value = readableBuffer[readPos];
            if (moveReadPos) readPos += 1;
            return value;
        } else {
            throw new Exception("Could not read value of type 'byte'!");
        }
    }

    public byte[] ReadBytes(int length, bool moveReadPos = true) {
        if (buffer.Count > readPos) {
            byte[] value = buffer.GetRange(readPos, length).ToArray();
            if (moveReadPos) readPos += length;
            return value;
        } else {
            throw new Exception("Could not read value of type 'byte[]'!");
        }
    }

    public short ReadShort(bool moveReadPos = true) {
        if (buffer.Count > readPos) {
            short value = BitConverter.ToInt16(readableBuffer, readPos);
            if (moveReadPos) readPos += 2;
            return value;
        } else {
            throw new Exception("Could not read value of type 'short'!");
        }
    }

    public int ReadInt(bool moveReadPos = true) {
        if (buffer.Count > readPos) {
            int value = BitConverter.ToInt32(readableBuffer, readPos);
            if (moveReadPos) readPos += 4;
            return value;
        } else {
            throw new Exception("Could not read value of type 'int'!");
        }
    }

    public long ReadLong(bool moveReadPos = true) {
        if (buffer.Count > readPos) {
            long value = BitConverter.ToInt64(readableBuffer, readPos);
            if (moveReadPos) readPos += 8;
            return value;
        } else {
            throw new Exception("Could not read value of type 'long'!");
        }
    }

    public float ReadFloat(bool moveReadPos = true) {
        if (buffer.Count > readPos) {
            float value = BitConverter.ToSingle(readableBuffer, readPos);
            if (moveReadPos) readPos += 4;
            return value;
        } else {
            throw new Exception("Could not read value of type 'float'!");
        }
    }

    public bool ReadBool(bool moveReadPos = true) {
        if (buffer.Count > readPos) {
            bool value = BitConverter.ToBoolean(readableBuffer, readPos);
            if (moveReadPos) readPos += 1;
            return value;
        } else {
            throw new Exception("Could not read value of type 'bool'!");
        }
    }

    public string ReadString(bool moveReadPos = true) {
        try {
            int length = ReadInt();
            string value = Encoding.ASCII.GetString(readableBuffer, readPos, length);
            if (moveReadPos && value.Length > 0) readPos += length;
            return value;
        } catch {
            throw new Exception("Could not read value of type 'string'!");
        }
    }

    public uint ReadUint(bool moveReadPos = true) {
        if (buffer.Count > readPos) {
            uint value = BitConverter.ToUInt32(readableBuffer, readPos);
            if (moveReadPos) readPos += 4;
            return value;
        } else {
            throw new Exception("Could not read value of type 'uint'!");
        }
    }

    public Vector3 ReadVector3(bool moveReadPos = true) {
        return new Vector3(ReadFloat(moveReadPos), ReadFloat(moveReadPos), ReadFloat(moveReadPos));
    }

    public Quaternion ReadQuaternion(bool moveReadPos = true) {
        return new Quaternion(ReadFloat(moveReadPos), ReadFloat(moveReadPos), ReadFloat(moveReadPos), ReadFloat(moveReadPos));
    }

    public Inputs ReadInputs(bool moveReadPos = true) {
        Inputs inputs = new Inputs();

        foreach (var prop in inputs.GetType().GetProperties()) {
            Type type = prop.PropertyType;
            if (type == typeof(bool)) prop.SetValue(inputs, ReadBool(moveReadPos));
        }

        return inputs;
    }

    public InputPayload ReadInputPayload(bool moveReadPos = true) {
        InputPayload inputPayload = new InputPayload();

        foreach (var prop in inputPayload.GetType().GetProperties()) {
            Type type = prop.PropertyType;
            if (type == typeof(float)) prop.SetValue(inputPayload, ReadFloat(moveReadPos));
            if (type == typeof(uint)) prop.SetValue(inputPayload, ReadUint(moveReadPos));
            if (type == typeof(Inputs)) prop.SetValue(inputPayload, ReadInputs(moveReadPos));
        }

        return inputPayload;
    }

    public StatePayload ReadStatePayload(bool moveReadPos = true) {
        StatePayload statePayload = new StatePayload();

        foreach (var prop in statePayload.GetType().GetProperties()) {
            Type type = prop.PropertyType;
            if (type == typeof(float)) prop.SetValue(statePayload, ReadFloat(moveReadPos));
            if (type == typeof(uint)) prop.SetValue(statePayload, ReadUint(moveReadPos));
            if (type == typeof(Vector3)) prop.SetValue(statePayload, ReadVector3(moveReadPos));
            if (type == typeof(Quaternion)) prop.SetValue(statePayload, ReadQuaternion(moveReadPos));
        }

        return statePayload;
    }
}