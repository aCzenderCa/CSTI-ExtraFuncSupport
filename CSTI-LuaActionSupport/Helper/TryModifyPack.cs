namespace CSTI_LuaActionSupport.Helper
{
    public static class TryModifyPack
    {
        public static void TryMod(this ref float self, object? o)
        {
            if (o is double or long or float)
            {
                self = (float) o;
            }
        }

        public static void TryMod(this ref int self, object? o)
        {
            if (o is double or long or float or int)
            {
                self = (int) o;
            }
        }

        public static float TryFloat(this object? o)
        {
            if (o is double or long or float)
            {
                return (float) o;
            }

            return 0;
        }

        public static double TryDouble(this object? o)
        {
            if (o is double or long)
            {
                return (double) o;
            }

            return 0;
        }

        public static int TryInt(this object? o)
        {
            if (o is double or long or float)
            {
                return (int) o;
            }

            return 0;
        }

        public static long TryLong(this object? o)
        {
            if (o is double or long or float)
            {
                return (long) o;
            }

            return 0;
        }
    }
}