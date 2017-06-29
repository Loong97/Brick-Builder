using Rhino;
using Rhino.Geometry;
using Rhino.DocObjects;
using Rhino.Collections;

using GH_IO;
using GH_IO.Serialization;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

using System;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Linq;
using System.Data;
using System.Drawing;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace GH_BRICK
{
    /// <summary>
    /// 一些和字符串有关的工具函数
    /// </summary>
    public class TextTools
    {
        /// <summary>
        /// 判断一个字符串存在于一个组中
        /// </summary>
        /// <param name="key"></param>
        /// <param name="pool"></param>
        /// <returns></returns>
        public static bool ExsistIn(string key,string[] pool)
        {
            for(int i = 0; i < pool.Count(); i++)
            {
                if (key == pool[i]) { return true; }
            }
            return false;
        }

        /// <summary>
        /// 判断砖块类型是否正确
        /// </summary>
        /// <param name="types"></param>
        /// <returns></returns>
        public static bool IsBrickTypeValid(List<string>[] types)
        {
            string[] validTypes = { "Horizontal", "Vertical" };
            for(int i = 0; i < types.Count(); i++)
            {
                for(int j = 0; j < types[i].Count; j++)
                {
                    if (!ExsistIn(types[i][j], validTypes)) { return false; }
                }
            }
            return true;
        }
    }
}