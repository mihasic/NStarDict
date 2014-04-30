using System;
using System.IO;
using System.Text;

namespace NStarDict
{
    public class StarDict
    {
        readonly FileStream index;
        readonly FileStream yaindex;
        readonly DictZipFile dz;
        readonly String dictname;

        public StarDict()
            : this("//sdcard/dict")
        {
        }

        public StarDict(String dictname)
        {
            this.dictname = dictname;
            index = new FileStream(dictname + ".idx", FileMode.Open, FileAccess.Read);
            dz = new DictZipFile(dictname + ".dict.dz");
            yaindex = new FileStream(dictname + ".yaidx", FileMode.Open, FileAccess.Read);
            //this.dz.runtest();
        }

        public String GetWord(int p, Location l)
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

            yaindex.Seek(p * 4, SeekOrigin.Begin);
            int size = yaindex.Read(buffer, 0, 4);
            if (size != 4)
            {
                throw new Exception("Read Index Error");
            }
            for (int i = 0; i < 4; i++)
            {
                offset <<= 8;
                offset |= buffer[i] & 0xff;
            }
            index.Seek(offset, SeekOrigin.Begin);
            size = index.Read(buffer, 0, 1024);
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

        public String GetExplanation(String word)
        {
            int i = 0;
            int max = GetWordNum();
            String w = "";
            var l = new Location();
            //return this.dz.test()+this.dz.last_error;
            //*
            while (i <= max)
            {
                int mid = (i + max) / 2;
                w = GetWord(mid, l);
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
            dz.Read(buffer, l.Size);

            string exp = Encoding.UTF8.GetString(buffer);
            return w + "\n" + exp;
            //return mid+"\n"+l.offset+exp+l.size;
            //*/
        }

        public String GetVersion()
        {
            var br = new StreamReader(dictname + ".ifo");
            String line = br.ReadLine();
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

        public int GetWordNum()
        {
            var br = new StreamReader(dictname + ".ifo");
            String line = br.ReadLine();
            while (line != null)
            {
                String[] version = line.Split('=');
                if (version.Length == 2 && version[0].Equals("wordcount"))
                {
                    return int.Parse(version[1]);
                }
                line = br.ReadLine();
            }
            return 0;
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
    }
}