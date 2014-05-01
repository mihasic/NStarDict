using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

using PCLStorage;

namespace NStarDict
{
    public class DictZipFile
    {
        private Stream _dictZip;

        //const int FTEXT = 1;
        const int FHCRC = 2;
        const int FEXTRA = 4;
        const int FNAME = 8;
        const int FCOMMENT = 16;

        //const int READ = 1;
        //const int WRITE = 2;

        private int _currentPosition;
        private int _characterLength;
        private int _firstPosition;

        private readonly List<Chunk> _chunks;
        private readonly string _fileName;
        private readonly IFolder _folder;

        public DictZipFile(IFolder folder, String fileName)
            : this(fileName)
        {
            _folder = folder;
        }

        public DictZipFile(String fileName)
        {
            _fileName = fileName;
            _currentPosition = 0;
            _firstPosition = 0;
            _chunks = new List<Chunk>();
        }

        public async Task Init()
        {
            IFile file = _folder == null
                ? await FileSystem.Current.GetFileFromPathAsync(_fileName)
                : await _folder.GetFileAsync(_fileName);

            _dictZip = await file.OpenAsync(FileAccess.Read);
            ReadGzipHeader();
        }

        public async Task<int> Read(byte[] buff, int size)
        {
            if (size <= 0)
            {
                return 0;
            }
            int firstChunk = _currentPosition / _characterLength;
            int offset = _currentPosition - firstChunk * _characterLength;
            int lastChunk = (_currentPosition + size) / _characterLength;
            /*
         * int finish = 0;
         * int npos = 0;
         * finish = offset+size;
         * npos = this.pos+size;
         */
            var byteStream = new MemoryStream();
            for (int i = firstChunk; i <= lastChunk; i++)
            {
                byte[] chunk = await ReadChunk(i);
                await byteStream.WriteAsync(chunk, 0, chunk.Length);
            }
            byte[] buf = byteStream.ToArray();
            for (int i = 0; i < size; i++)
            {
                buff[i] = buf[offset + i];
            }
            return 0;
        }

        public void Seek(int position)
        {
            _currentPosition = position;
        }

        public int Tell()
        {
            return _currentPosition;
        }

        public void Close()
        {
            if (_dictZip != null)
            {
                _dictZip.Dispose();
            }
        }

        private void ReadGzipHeader()
        {
            var buffer = new byte[2];
            _dictZip.Read(buffer, 0, 2);
            _firstPosition += 2;
            if (buffer[0] != 31 || buffer[1] != 139 /*-117*/)
            {
                throw new Exception("Not a gzipped file");
            }
            var b = (byte)_dictZip.ReadByte();
            _firstPosition += 1;
            if (b != 8)
            {
                throw new Exception("Unknown compression method");
            }
            var flag = (byte)_dictZip.ReadByte();
            //System.out.println("flag = "+flag);
            _firstPosition += 1;
            _dictZip.Seek(6, SeekOrigin.Current);
            //ReadInt(dictzip);
            //dictzip.ReadByte();
            //dictzip.ReadByte();
            _firstPosition += 6;
            if ((flag & FEXTRA) != 0)
            {
                int xlen = _dictZip.ReadByte();
                xlen += 256 * _dictZip.ReadByte();
                var extra = new byte[xlen];
                _dictZip.Read(extra, 0, xlen);
                _firstPosition += 2 + xlen;
                int ext = 0;
                while (true)
                {
                    int l = (extra[ext + 2] & 0xff) + (256 * (extra[ext + 3] & 0xff));
                    if (extra[ext + 0] != 'R' || extra[ext + 1] != 'A')
                    {
                        ext = 4 + l;
                        if (ext > xlen)
                        {
                            throw new Exception("Missing dictzip extension");
                        }
                    }
                    else
                    {
                        break;
                    }
                }
                _characterLength = (extra[ext + 6] & 0xff) + (256 * (extra[ext + 7] & 0xff));
                int chcnt = (extra[ext + 8] & 0xff) + (256 * (extra[ext + 9] & 0xff));
                int p = 10;
                var lens = new List<int>();
                for (int i = 0; i < chcnt; i++)
                {
                    int thischlen = (extra[ext + p] & 0xff) + (256 * (extra[ext + p + 1] & 0xff));
                    p += 2;
                    lens.Add(thischlen);
                }
                int chpos = 0;
                foreach (int i in lens)
                {
                    _chunks.Add(new Chunk(chpos, i));
                    chpos += i;
                }
            }
            else
            {
                throw new Exception("Missing dictzip extension");
            }

            if ((flag & FNAME) != 0)
            {
                //Read and discard a null-terminated string containing the filename
                while (true)
                {
                    var s = (byte)_dictZip.ReadByte();
                    _firstPosition += 1;
                    if (s == 0)
                    {
                        break;
                    }
                }
            }

            if ((flag & FCOMMENT) != 0)
            {
                //Read and discard a null-terminated string containing a comment
                while (true)
                {
                    var s = (byte)_dictZip.ReadByte();
                    _firstPosition += 1;
                    if (s == 0)
                    {
                        break;
                    }
                }
            }

            if ((flag & FHCRC) != 0)
            {
                //Read & discard the 16-bit header CRC
                _dictZip.ReadByte();
                _dictZip.ReadByte();
                _firstPosition += 2;
            }
        }

        private async Task<byte[]> ReadChunk(int n)
        {
            if (n >= _chunks.Count)
            {
                return null;
            }
            _dictZip.Seek(_firstPosition + _chunks[n].Offset, SeekOrigin.Begin);
            int size = _chunks[n].Size;
            var buff = new byte[size];
            await _dictZip.ReadAsync(buff, 0, size);
            /* jzlib */
            var zIn = new GZipStream(new MemoryStream(buff), CompressionLevel.Optimal, true);
            var byteStream = new MemoryStream();
            var buf = new byte[1024];
            int numRead;
            while ((numRead = await zIn.ReadAsync(buf, 0, 1024)) != -1)
            {
                await byteStream.WriteAsync(buf, 0, numRead);
            }
            return byteStream.ToArray();
        }

        //public void runtest()
        //{
        //    Console.WriteLine("chunklen=" + chlen);
        //    Console.WriteLine("_firstpos=" + _firstpos);
        //}

        //public String test()
        //{
        //    return ("chunklen=" + chlen);
        //}

        //public static int ReadInt(FileStream fstream)
        //{
        //    var bytes = new Byte[4];
        //    fstream.Read(bytes, 0, 4);
        //    int result = BitConverter.ToInt32(bytes, 0);
        //    return result;
        //}
    }
}