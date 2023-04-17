namespace DehxServerLib
{
    public static class IDNumbers
    {
        public static int currentId = 1;
        public static int NextId()
        {
            return currentId++;
        }
    }
}
