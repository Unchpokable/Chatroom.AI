using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Chatroom.AI.Core;

[StructLayout(LayoutKind.Sequential)]
public struct HwidSha256
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
    public byte[] data;
}

[InlineArray(512)]
public struct EncryptedKey
{
    private byte _buf;

    public static EncryptedKey Create()
    {
        return default;
    }

    public readonly void CopyTo(Span<byte> destination)
    {
        ReadOnlySpan<byte> source = this;
        source.CopyTo(destination);
    }
}

/*
 * Binary format of file:
 * [key_name: 32 bytes, UTF-8 string][key_value: 512 bytes, binary encrypted data]
 */
public sealed class TokenLocker
{
    public IReadOnlyDictionary<string, EncryptedKey> Keys => _keys;

    private Dictionary<string, EncryptedKey> _keys = new();

    private const int KeyNameLength = 32;
    private const int KeyValueLength = 512;

    private string _extension = ".bink";

    public void AddKey(string name, string value)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentException("Key name cannot be null or empty", nameof(name));

        var encryptedBuffer = new byte[KeyValueLength];
        hwprotect_encrypt_string(value ?? "", encryptedBuffer, KeyValueLength, false);

        var encryptedKey = EncryptedKey.Create();
        encryptedBuffer.CopyTo(encryptedKey);

        _keys[name] = encryptedKey;
    }

    public string GetKey(string name)
    {
        if (!_keys.TryGetValue(name, out var encryptedKey))
            return "";

        var encryptedBuffer = new byte[KeyValueLength];
        encryptedKey.CopyTo(encryptedBuffer);

        var decryptedBuffer = new byte[KeyValueLength];
        var encryptedNullIndex = Array.IndexOf(encryptedBuffer, (byte)0);

        hwprotect_decrypt_string(encryptedBuffer, encryptedNullIndex, decryptedBuffer, KeyValueLength, false);

        var nullIndex = Array.IndexOf(decryptedBuffer, (byte)0);
        var length = nullIndex >= 0 ? nullIndex : KeyValueLength;

        return Encoding.UTF8.GetString(decryptedBuffer, 0, length);
    }

    public void LoadFromFile(string path)
    {
        var fullPath = Path.ChangeExtension(path, _extension);

        if (!File.Exists(fullPath))
            throw new FileNotFoundException("Token file not found", fullPath);

        _keys.Clear();

        using var stream = File.OpenRead(fullPath);
        using var reader = new BinaryReader(stream);

        var recordSize = KeyNameLength + KeyValueLength;

        while (stream.Position + recordSize <= stream.Length)
        {
            var nameBytes = reader.ReadBytes(KeyNameLength);
            var valueBytes = reader.ReadBytes(KeyValueLength);

            var nullIndex = Array.IndexOf(nameBytes, (byte)0);
            var nameLength = nullIndex >= 0 ? nullIndex : KeyNameLength;
            var keyName = Encoding.UTF8.GetString(nameBytes, 0, nameLength);

            if (string.IsNullOrEmpty(keyName))
                continue;

            var encryptedKey = EncryptedKey.Create();
            valueBytes.CopyTo(encryptedKey);

            _keys[keyName] = encryptedKey;
        }
    }

    public void SaveToFile(string path)
    {
        var fullPath = Path.ChangeExtension(path, _extension);

        using var stream = File.Create(fullPath);
        using var writer = new BinaryWriter(stream);

        foreach (var kvp in _keys)
        {
            var nameBytes = new byte[KeyNameLength];
            var nameUtf8 = Encoding.UTF8.GetBytes(kvp.Key);
            var copyLength = Math.Min(nameUtf8.Length, KeyNameLength);
            Array.Copy(nameUtf8, nameBytes, copyLength);

            writer.Write(nameBytes);

            var valueBytes = new byte[KeyValueLength];
            kvp.Value.CopyTo(valueBytes);

            writer.Write(valueBytes);
        }
    }

    #region Dll Imports
    [DllImport("hwprotect.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern void hwprotect_encrypt_string(
        [MarshalAs(UnmanagedType.LPStr)] string source,
        [Out] byte[] buffer,
        int length,
        [MarshalAs(UnmanagedType.I1)] bool snap_drives);

    [DllImport("hwprotect.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern void hwprotect_decrypt_string(
        [In] byte[] encrypted,
        int encryptedLength,
        [Out] byte[] outBuffer,
        int outBufferLength,
        [MarshalAs(UnmanagedType.I1)] bool snap_drives);

    [DllImport("hwprotect.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern HwidSha256 hwprotect_get_hwid(
        [MarshalAs(UnmanagedType.I1)] bool snap_drives);
    #endregion
}
