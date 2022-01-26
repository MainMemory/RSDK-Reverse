﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using RSDKv3_4;

namespace RSDKv4
{
    public class DataFile : IDataFile
    {
        public class NameIdentifier
        {
            /// <summary>
            /// the MD5 hash of the name in bytes
            /// </summary>
            public byte[] hash;
            /// <summary>
            /// the name in plain text
            /// </summary>
            public string name = null;

            public bool usingHash = true;

            public NameIdentifier(string name)
            {
                using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
                {
                    hash = md5.ComputeHash(new System.Text.ASCIIEncoding().GetBytes(name));
                }
                this.name = name;
                usingHash = false;
            }

            public NameIdentifier(byte[] hash)
            {
                this.hash = hash;
            }

            public NameIdentifier(Reader reader)
            {
                read(reader);
            }

            public void read(Reader reader)
            {
                hash = reader.readBytes(16);
            }

            public void write(Writer writer)
            {
                writer.Write(hash);
            }

            public string hashString()
            {
                return BitConverter.ToString(hash).Replace("-", string.Empty).ToLower();
            }

            public override string ToString()
            {
                if (name != null) return name;
                return hashString();
            }
        }

        public class FileInfo
        {
            /// <summary>
            /// A list of extension types (used if the filename is unknown)
            /// </summary>
            public enum ExtensionTypes
            {
                UNKNOWN,
                OGG,
                WAV,
                MDL,
                PNG,
                GIF,
            }

            /// <summary>
            /// filename of the file
            /// </summary>
            public NameIdentifier fileName = new NameIdentifier("File.bin");

            /// <summary>
            /// whether the file is encrypted or not
            /// </summary>
            public bool encrypted = false;

            /// <summary>
            /// an array of the bytes in the file, decrypted
            /// </summary>
            public byte[] fileData;

            /// <summary>
            /// the extension of the file
            /// </summary>
            public ExtensionTypes extension = ExtensionTypes.UNKNOWN;

            /// <summary>
            /// a string of bytes used for decryption/encryption
            /// </summary>
            private static byte[] encryptionStringA = new byte[16];
            /// <summary>
            /// another string of bytes used for decryption/encryption
            /// </summary>
            private static byte[] encryptionStringB = new byte[16];

            private static int eStringNo;
            private static int eStringPosA;
            private static int eStringPosB;
            private static int eNybbleSwap;

            public uint offset = 0;

            public FileInfo() { }

            public FileInfo(Reader reader, List<string> fileNames = null, int fileID = 0)
            {
                read(reader, fileNames, fileID);
            }

            public void read(Reader reader, List<string> fileNames = null, int fileID = 0)
            {
                for (int y = 0; y < 16; y += 4)
                {
                    fileName.hash[y + 3] = reader.ReadByte();
                    fileName.hash[y + 2] = reader.ReadByte();
                    fileName.hash[y + 1] = reader.ReadByte();
                    fileName.hash[y + 0] = reader.ReadByte();
                }
                fileName.usingHash = true;

                var md5 = MD5.Create();

                fileName.name = (fileID + 1) + ".bin"; //Make a base name

                for (int i = 0; fileNames != null && i < fileNames.Count; i++)
                {
                    // RSDKv4 Hashes all Strings at Lower Case
                    string fp = fileNames[i].ToLower();

                    bool match = true;

                    for (int z = 0; z < 16; z++)
                    {
                        if (calculateMD5Hash(fp)[z] != fileName.hash[z])
                        {
                            match = false;
                            break;
                        }
                    }

                    if (match)
                    {
                        fileName = new NameIdentifier(fileNames[i]);
                        break;
                    }
                }

                uint fileOffset = reader.ReadUInt32();
                uint tmp = reader.ReadUInt32();

                encrypted = (tmp & 0x80000000) != 0;
                uint fileSize = (tmp & 0x7FFFFFFF);

                long tmp2 = reader.BaseStream.Position;
                reader.BaseStream.Position = fileOffset;

                // Decrypt File if Encrypted
                if (encrypted)
                    fileData = decrypt(reader.readBytes(fileSize), false);
                else
                    fileData = reader.readBytes(fileSize);


                reader.BaseStream.Position = tmp2;

                extension = getExtensionFromData();

                if (fileName.usingHash)
                {
                    switch (extension)
                    {
                        case ExtensionTypes.GIF:
                            fileName.name = "Sprite" + (fileID + 1) + ".gif";
                            break;
                        case ExtensionTypes.MDL:
                            fileName.name = "Model" + (fileID + 1) + ".bin";
                            break;
                        case ExtensionTypes.OGG:
                            fileName.name = "Music" + (fileID + 1) + ".ogg";
                            break;
                        case ExtensionTypes.PNG:
                            fileName.name = "Image" + (fileID + 1) + ".png";
                            break;
                        case ExtensionTypes.WAV:
                            fileName.name = "SoundEffect" + (fileID + 1) + ".wav";
                            break;
                        case ExtensionTypes.UNKNOWN:
                            fileName.name = "UnknownFileType" + (fileID + 1) + ".bin";
                            break;
                    }
                }
                md5.Dispose();
            }

            public void writeFileHeader(Writer writer, uint offset = 0)
            {
                NameIdentifier name = fileName;
                if (!fileName.usingHash)
                    name = new NameIdentifier(fileName.name.Replace('\\', '/').ToLower());
                
                for (int y = 0; y < 16; y += 4)
                {
                    writer.Write(name.hash[y + 3]);
                    writer.Write(name.hash[y + 2]);
                    writer.Write(name.hash[y + 1]);
                    writer.Write(name.hash[y + 0]);
                }
                writer.Write(offset);
                writer.Write((uint)(fileData.Length) | (encrypted ? 0x80000000 : 0));
            }

            public void writeFileData(Writer writer)
            {
                if (encrypted)
                    writer.Write(decrypt(fileData, true));
                else
                    writer.Write(fileData);
            }

            private byte[] calculateMD5Hash(string input)
            {
                MD5 md5 = MD5.Create();
                byte[] hash = md5.ComputeHash(Encoding.ASCII.GetBytes(input));

                md5.Dispose();
                return hash;
            }

            private void generateKey(out byte[] keyBuffer, uint Value)
            {
                string strbuf = Value.ToString();

                byte[] md5Buf = calculateMD5Hash(strbuf);
                keyBuffer = new byte[16];

                for (int y = 0; y < 16; y += 4)
                {
                    // convert every 32-bit word to Little Endian
                    keyBuffer[y + 3] = md5Buf[y + 0];
                    keyBuffer[y + 2] = md5Buf[y + 1];
                    keyBuffer[y + 1] = md5Buf[y + 2];
                    keyBuffer[y + 0] = md5Buf[y + 3];
                }
            }

            private void generateELoadKeys(uint key1, uint key2, uint size)
            {
                generateKey(out encryptionStringA, key1);
                generateKey(out encryptionStringB, key2);

                eStringNo = (int)(size / 4) & 0x7F;
                eStringPosA = 0;
                eStringPosB = 8;
                eNybbleSwap = 0;
            }
            private byte[] decrypt(byte[] data, bool encrypting)
            {
                uint fileSize = (uint)(data.Length);
                generateELoadKeys(fileSize, (fileSize >> 1) + 1, fileSize);

                const uint ENC_KEY_2 = 0x24924925;
                const uint ENC_KEY_1 = 0xAAAAAAAB;

                byte[] outputData = new byte[data.Length];

                for (int i = 0; i < data.Length; i++)
                {
                    int encByte = data[i];
                    if (encrypting)
                    {
                        encByte ^= encryptionStringA[eStringPosA++];

                        if (eNybbleSwap == 1)   // swap nibbles: 0xAB <-> 0xBA
                            encByte = ((encByte << 4) + (encByte >> 4)) & 0xFF;

                        encByte ^= eStringNo ^ encryptionStringB[eStringPosB++];
                    }
                    else
                    {
                        encByte ^= eStringNo ^ encryptionStringB[eStringPosB++];

                        if (eNybbleSwap == 1)   // swap nibbles: 0xAB <-> 0xBA
                            encByte = ((encByte << 4) + (encByte >> 4)) & 0xFF;

                        encByte ^= encryptionStringA[eStringPosA++];
                    }
                    outputData[i] = (byte)encByte;

                    if (eStringPosA <= 0x0F)
                    {
                        if (eStringPosB > 0x0C)
                        {
                            eStringPosB = 0;
                            eNybbleSwap ^= 0x01;
                        }
                    }
                    else if (eStringPosB <= 0x08)
                    {
                        eStringPosA = 0;
                        eNybbleSwap ^= 0x01;
                    }
                    else
                    {
                        eStringNo += 2;
                        eStringNo &= 0x7F;

                        if (eNybbleSwap != 0)
                        {
                            int key1 = mulUnsignedHigh(ENC_KEY_1, eStringNo);
                            int key2 = mulUnsignedHigh(ENC_KEY_2, eStringNo);
                            eNybbleSwap = 0;

                            int temp1 = key2 + (eStringNo - key2) / 2;
                            int temp2 = key1 / 8 * 3;

                            eStringPosA = eStringNo - temp1 / 4 * 7;
                            eStringPosB = eStringNo - temp2 * 4 + 2;
                        }
                        else
                        {
                            int key1 = mulUnsignedHigh(ENC_KEY_1, eStringNo);
                            int key2 = mulUnsignedHigh(ENC_KEY_2, eStringNo);
                            eNybbleSwap = 1;

                            int temp1 = key2 + (eStringNo - key2) / 2;
                            int temp2 = key1 / 8 * 3;

                            eStringPosB = eStringNo - temp1 / 4 * 7;
                            eStringPosA = eStringNo - temp2 * 4 + 3;
                        }
                    }
                }
                return outputData;
            }

            private int mulUnsignedHigh(uint arg1, int arg2)
            {
                return (int)(((ulong)arg1 * (ulong)arg2) >> 32);
            }

            private ExtensionTypes getExtensionFromData()
            {
                byte[] header = new byte[5];

                for (int i = 0; i < header.Length; i++)
                    header[i] = fileData[i];

                byte[] signature_ogg = new byte[] { (byte)'O', (byte)'g', (byte)'g', (byte)'s' };
                byte[] signature_gif = new byte[] { (byte)'G', (byte)'I', (byte)'F' };
                byte[] signature_mdl = new byte[] { (byte)'R', (byte)'3', (byte)'D', 0 };
                byte[] signature_png = new byte[] { (byte)'P', (byte)'N', (byte)'G' };
                byte[] signature_wav = new byte[] { (byte)'R', (byte)'I', (byte)'F', (byte)'F' };

                if (header.Take(4).SequenceEqual(signature_ogg))
                    return ExtensionTypes.OGG;

                if (header.Take(3).SequenceEqual(signature_gif))
                    return ExtensionTypes.GIF;

                if (header.Take(4).SequenceEqual(signature_mdl))
                    return ExtensionTypes.MDL;

                if (header.Take(3).SequenceEqual(signature_png))
                    return ExtensionTypes.PNG;

                if (header.Take(4).SequenceEqual(signature_wav))
                    return ExtensionTypes.WAV;

                return ExtensionTypes.UNKNOWN;
            }
        }

        public static readonly byte[] signature = new byte[] { (byte)'R', (byte)'S', (byte)'D', (byte)'K', (byte)'v', (byte)'B' };

        public List<FileInfo> files = new List<FileInfo>();

        public DataFile() { }

        public DataFile(string filepath, List<string> fileNames = null) : this(new Reader(filepath), fileNames) { }
        public DataFile(Stream stream, List<string> fileNames = null) : this(new Reader(stream), fileNames) { }

        public DataFile(Reader reader, List<string> fileNames = null)
        {
            read(reader, fileNames);
        }

        public void read(Reader reader, List<string> fileNames = null)
        {
            if (!reader.readBytes(6).SequenceEqual(signature))
            {
                reader.Close();
                throw new Exception("Invalid DataFile v4 signature");
            }

            ushort fileCount = reader.ReadUInt16();
            files.Clear();
            for (int i = 0; i < fileCount; i++)
                files.Add(new FileInfo(reader, fileNames, i));

            reader.Close();
        }

        public void write(string filename)
        {
            using (Writer writer = new Writer(filename))
                write(writer);
        }

        public void write(Stream stream)
        {
            using (Writer writer = new Writer(stream))
                write(writer);
        }

        public void write(Writer writer)
        {
            // firstly we set out the file
            // write a bunch of blanks

            writer.Write(signature);
            writer.Write((ushort)files.Count); // write the header

            foreach (FileInfo f in files)  // write each file's header
                f.writeFileHeader(writer); // write our file header data

            foreach (FileInfo f in files) // write "Filler Data"
            {
                f.offset = (uint)writer.BaseStream.Position;    // get our file data offset
                byte[] b = new byte[f.fileData.Length];         // load up a set of blanks with the same size as the original set
                writer.Write(b);                                // fill the file up with blank data
            }

            // now we really write our data

            writer.seek(0, SeekOrigin.Begin); // jump back to the start of the file

            writer.Write(signature);
            writer.Write((ushort)files.Count); // re-write our header

            foreach (FileInfo f in files) // for each file
            {
                f.writeFileHeader(writer, f.offset);        // write our header
                long pos = writer.BaseStream.Position;      // get our writer pos for later
                writer.BaseStream.Position = f.offset;      // jump to our saved offset
                f.writeFileData(writer);                    // write our file data
                writer.BaseStream.Position = pos;           // jump back ready to write the next file!
            }

        }

        private byte[] GetHash(string str)
        {
            byte[] hash;
            using (MD5 md5 = MD5.Create())
            {
                hash = md5.ComputeHash(new ASCIIEncoding().GetBytes(str.ToLowerInvariant()));
            }
            return hash;
        }

        public bool FileExists(string fileName)
        {
            byte[] hash = GetHash(fileName);
            return files.Any(a => a.fileName.hash.SequenceEqual(hash));
        }

        public byte[] GetFileData(string fileName)
        {
            byte[] hash = GetHash(fileName);
            return files.First(a => a.fileName.hash.SequenceEqual(hash)).fileData;
        }

        public bool TryGetFileData(string fileName, out byte[] fileData)
        {
            byte[] hash = GetHash(fileName);
            var f = files.FirstOrDefault(a => a.fileName.hash.SequenceEqual(hash));
            fileData = f?.fileData;
            return f != null;
        }
    }
}
