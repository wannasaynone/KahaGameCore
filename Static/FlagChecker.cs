using System;
using System.Collections.Generic;

namespace KahaGameCore.Static
{
    public static class FlagChecker
    {
        public static long GetBitFlag(Enum value)
        {
            return 1L << Convert.ToInt32(value);
        }

        public static int ConvertToInt(long bitFlag)
        {
            int _maxSize = sizeof(long);
            for (int _value = 0; _value <= _maxSize; _value++)
            {
                if(1L << _value == bitFlag)
                {
                    return _value;
                }
            }

            UnityEngine.Debug.LogErrorFormat("Invalid BitFlag:{0}, will return -1", bitFlag);
            return -1;
        }

        public static bool Contains(long bitFlag, long value)
        {
           return (bitFlag & value) > 0;
        }

        public static bool Contains(long bitFlag, Enum value)
        {
            return (bitFlag & GetBitFlag(value)) > 0;
        }
    }
}
