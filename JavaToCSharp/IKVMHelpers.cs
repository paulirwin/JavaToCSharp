using System.Collections.Generic;
using java.util;

namespace JavaToCSharp
{
	public static class IKVMHelpers
    {
        public static List<T> ToList<T>(this java.util.List list)
        {
            if (list == null)
                return null;

            var newList = new List<T>();

            for (int i = 0; i < list.size(); i++)
            {
                newList.Add((T)list.get(i));
            }

            return newList;
        }

        public static bool HasFlag(this int value, int flag)
        {
            return (value & flag) != 0;
		}

		public static bool HasFlag(this EnumSet value, int flag)
		{
			return value.contains(flag);
		}
	}
}
