namespace NStarDict
{
    public class Chunk
    {
        public int Offset;
        public int Size;

        public Chunk(int o, int s)
        {
            Offset = o;
            Size = s;
        }
    }
}