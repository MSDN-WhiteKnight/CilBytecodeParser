﻿/* CIL Tools 
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;

namespace CilView.Common
{
    public static class Utils
    {
        public static bool StringEquals(string left, string right)
        {
            return String.Equals(left, right, StringComparison.InvariantCulture);
        }

        public static bool PathEquals(string left, string right)
        {
            if (left == null)
            {
                if (right == null) return true;
                else return false;
            }

            if (right == null) return false;

            if (left.EndsWith("\\") || left.EndsWith("/"))
            {
                left = left.Substring(0, left.Length - 1);
            }

            if (right.EndsWith("\\") || right.EndsWith("/"))
            {
                right = right.Substring(0, right.Length - 1);
            }

            left = left.ToLower();
            right = right.ToLower();
            return StringEquals(left,right);
        }

        public static int Search<T>(T[] array, Func<T, string, bool> func, string text, int start_index)
        {
            if (start_index < 0) start_index = 0;
            if (start_index >= array.Length) return -1;

            for (int i = start_index; i < array.Length; i++)
            {
                if (func(array[i], text))
                {
                    return i; //found item
                }
            }

            return -1; //not found
        }
    }
}
