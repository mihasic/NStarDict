using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using PCLStorage;

namespace NStarDict
{
    public class StarDict : IDisposable
    {
        private Stream index;
        private readonly DictZipFile dz;
        private readonly string dictname;
        private readonly IFolder _folder;

        public StarDict()
            : this("//sdcard/dict")
        {
        }

        public StarDict(IFolder folder, String dictname)
        {
            _folder = folder;
            this.dictname = dictname;
            dz = new DictZipFile(folder, dictname + ".dict.dz");
        }

        public StarDict(String dictname)
        {
            _folder = null;
            this.dictname = dictname;
            dz = new DictZipFile(dictname + ".dict.dz");
        }

        public async Task Init()
        {
            var indexFile = _folder == null
                ? await FileSystem.Current.GetFileFromPathAsync(dictname + ".idx")
                : await _folder.GetFileAsync(dictname + ".idx");

            index = await indexFile.OpenAsync(FileAccess.Read);
            await dz.Init();
        }

        public async Task<string> GetWord(int p, Location l)
        {
            if (l == null)
            {
                l = new Location();
            }
            String word = null;
            var buffer = new byte[1024];
            int dataoffset = 0;
            int datasize = 0;
            int offset = 0; // the offset of the p-th word in this.index

            for (int i = 0; i < 4; i++)
            {
                offset <<= 8;
                offset |= buffer[i] & 0xff;
            }

            index.Seek(offset, SeekOrigin.Begin);

            int size = await index.ReadAsync(buffer, 0, 1024);

            for (int i = 0; i < size; i++)
            {
                if (buffer[i] == 0)
                {
                    word = Encoding.UTF8.GetString(buffer, 0, i);
                    dataoffset = 0;
                    datasize = 0;
                    for (int j = i + 1; j < i + 5; j++)
                    {
                        dataoffset <<= 8;
                        dataoffset |= buffer[j] & 0xff;
                    }
                    for (int j = i + 5; j < i + 9; j++)
                    {
                        datasize <<= 8;
                        datasize |= buffer[j] & 0xff;
                    }
                    break;
                }
            }
            //System.out.println(datasize);
            //buffer = new byte[datasize];
            //this.dz.seek(dataoffset);
            //this.dz.read(buffer, datasize);
            l.Offset = dataoffset;
            l.Size = datasize;
            return word;
        }

        public async Task<string> GetExplanation(String word)
        {
            int i = 0;
            int max = await GetWordNum();
            String w = "";
            var l = new Location();
            //return this.dz.test()+this.dz.last_error;
            //*
            while (i <= max)
            {
                int mid = (i + max) / 2;
                w = await GetWord(mid, l);
                if (string.CompareOrdinal(w, word) > 0)
                {
                    max = mid - 1;
                }
                else if (string.CompareOrdinal(w, word) < 0)
                {
                    i = mid + 1;
                }
                else
                {
                    break;
                }
            }

            //get explanation
            var buffer = new byte[l.Size];
            dz.Seek(l.Offset);
            int size = await dz.Read(buffer, l.Size);

            string exp = Encoding.UTF8.GetString(buffer, 0, size);
            return w + "\n" + exp;
            //return mid+"\n"+l.offset+exp+l.size;
            //*/
        }

        public async Task<string> GetVersion()
        {
            var file = _folder == null
                            ? await FileSystem.Current.GetFileFromPathAsync(dictname + ".ifo")
                            : await _folder.GetFileAsync(dictname + ".ifo");
            using (var stream = await file.OpenAsync(FileAccess.Read))
            {
                var br = new StreamReader(stream);
                string line = await br.ReadLineAsync();

                while (line != null)
                {
                    String[] version = line.Split('=');
                    if (version.Length == 2 && version[0].Equals("version"))
                    {
                        return version[1];
                    }
                    line = br.ReadLine();
                }
                return "UNKNOWN VERSION";
            }
        }

        public async Task<int> GetWordNum()
        {
            var file = _folder == null
                            ? await FileSystem.Current.GetFileFromPathAsync(dictname + ".ifo")
                            : await _folder.GetFileAsync(dictname + ".ifo");
            using (var stream = await file.OpenAsync(FileAccess.Read))
            {
                var br = new StreamReader(stream);
                var line = await br.ReadLineAsync();
                while (line != null)
                {
                    string[] version = line.Split('=');
                    if (version.Length == 2 && version[0].Equals("wordcount"))
                    {
                        return int.Parse(version[1]);
                    }
                    line = await br.ReadLineAsync();
                }
                return 0;
            }
        }

        //public static void main(String[] args)
        //{
        //    var dict = new StarDict();
        //    //System.out.println(dict.getVersion());
        //    var l = new Location();
        //    String w = dict.getWord(400000, l);
        //    Console.WriteLine(w);
        //    //System.out.println(dict.getExplanation(w));
        //    Console.WriteLine(dict.getExplanation("this"));
        //}
        public void Dispose()
        {
            if (index != null)
            {
                index.Dispose();
            }

            dz.Close();
        }
    }
}