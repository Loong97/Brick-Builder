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
    /// 一些与数据结构有关的工具函数
    /// </summary>
    public class DataTools
    {
        /// <summary>
        /// 返回一个数组中一个元素的数量
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="array"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        public static int CountItem<T>(T[] array,T item)
        {
            int count = 0;
            for(int i = 0; i < array.Count(); i++)
            {
                if (array[i].Equals(item)) { count++; }
            }
            return count;
        }

        /// <summary>
        /// 返回一个路径中第n个数，从1开始
        /// </summary>
        /// <param name="path"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static int GetPathNumber(GH_Path path,int index)
        {
            string str = path.ToString();
            char[] chr = str.ToArray();
            int count = CountItem(chr, ';') + 1;
            int idx = index % count;
            if (idx == 0) { idx = count; }
            List<char> seperator_l = new List<char> { '{' };
            for (int i = 0; i < count-1; i++)
            {
                seperator_l.Add(';');
            }
            seperator_l.Add('}');
            char[] seperator = seperator_l.ToArray();
            int number = int.Parse(str.Split(seperator)[idx]);
            return number;
        }
        
        /// <summary>
        /// 检查树的结构是否为二维
        /// </summary>
        /// <param name="tree"></param>
        /// <returns></returns>
        public static bool IsTreeDimension2(GH_Structure<GH_Plane> tree)
        {
            IList<GH_Path> paths = tree.Paths;
            string this_path = string.Empty;
            for (int i = 0; i < paths.Count; i++)
            {
                this_path = paths[i].ToString();
                char[] seperator = { '{', '}' };
                this_path = this_path.Split(seperator)[1];
                if (!int.TryParse(this_path, out int this_number)) { return false; }
            }
            return true;
        }

        public static bool IsTreeDimension2(GH_Structure<GH_String> tree)
        {
            IList<GH_Path> paths = tree.Paths;
            string this_path = string.Empty;
            for (int i = 0; i < paths.Count; i++)
            {
                this_path = paths[i].ToString();
                char[] seperator = { '{', '}' };
                this_path = this_path.Split(seperator)[1];
                if (!int.TryParse(this_path, out int this_number)) { return false; }
            }
            return true;
        }

        /// <summary>
        /// 检查树的结构是否为三维
        /// </summary>
        /// <param name="tree"></param>
        /// <returns></returns>
        public static bool IsTreeDimension3(GH_Structure<GH_Point> tree)
        {
            IList<GH_Path> paths = tree.Paths;
            string this_path = string.Empty;
            string this_path1 = string.Empty;
            string this_path2 = string.Empty;
            for (int i = 0; i < paths.Count; i++)
            {
                this_path = paths[i].ToString();
                char[] seperator = { '{', ';', '}' };
                this_path1 = this_path.Split(seperator)[1];
                this_path2 = this_path.Split(seperator)[2];
                if (!int.TryParse(this_path1, out int this_number1)) { return false; }
                if (!int.TryParse(this_path2, out int this_number2)) { return false; }
            }
            return true;
        }

        /// <summary>
        /// 检查一个二维树和一个三维树路径是否匹配
        /// </summary>
        /// <param name="tree2"></param>
        /// <param name="tree3"></param>
        /// <returns></returns>
        public static bool IsTreeMatch23(GH_Structure<GH_Plane> tree2, GH_Structure<GH_Point> tree3)
        {
            IList<GH_Path> t2_paths = tree2.Paths;
            IList<GH_Path> t3_paths = tree3.Paths;
            List<int> t2_path1 = new List<int>();
            List<int> t3_path1 = new List<int>();
            for (int i = 0; i < t2_paths.Count; i++)
            {
                string this_path = string.Empty;
                this_path = t2_paths[i].ToString();
                char[] seperator = { '{', '}' };
                this_path = this_path.Split(seperator)[1];
                if (!int.TryParse(this_path, out int this_number)) { return false; }
                t2_path1.Add(this_number);
            }
            PurifyInts(ref t2_path1);
            for (int i = 0; i < t3_paths.Count; i++)
            {
                string this_path = string.Empty;
                this_path = t3_paths[i].ToString();
                char[] seperator = { '{', ';', '}' };
                this_path = this_path.Split(seperator)[1];
                if (!int.TryParse(this_path, out int this_number)) { return false; }
                t3_path1.Add(this_number);
            }
            PurifyInts(ref t3_path1);
            if (t2_path1.All(t3_path1.Contains) && t2_path1.Count == t3_path1.Count) { return true; }
            else return false;
        }

        public static bool IsTreeMatch23(GH_Structure<GH_String> tree2, GH_Structure<GH_Point> tree3)
        {
            IList<GH_Path> t2_paths = tree2.Paths;
            IList<GH_Path> t3_paths = tree3.Paths;
            List<int> t2_path1 = new List<int>();
            List<int> t3_path1 = new List<int>();
            for (int i = 0; i < t2_paths.Count; i++)
            {
                string this_path = string.Empty;
                this_path = t2_paths[i].ToString();
                char[] seperator = { '{', '}' };
                this_path = this_path.Split(seperator)[1];
                if (!int.TryParse(this_path, out int this_number)) { return false; }
                t2_path1.Add(this_number);
            }
            PurifyInts(ref t2_path1);
            for (int i = 0; i < t3_paths.Count; i++)
            {
                string this_path = string.Empty;
                this_path = t3_paths[i].ToString();
                char[] seperator = { '{', ';', '}' };
                this_path = this_path.Split(seperator)[1];
                if (!int.TryParse(this_path, out int this_number)) { return false; }
                t3_path1.Add(this_number);
            }
            PurifyInts(ref t3_path1);
            if (t2_path1.All(t3_path1.Contains) && t2_path1.Count == t3_path1.Count) { return true; }
            else return false;
        }

        /// <summary>
        /// 将二维列表转化为树
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        public static DataTree<Plane> ListToTree2(List<List<Plane>> list)
        {
            GH_Path now_path = new GH_Path();
            DataTree<Plane> tree = new DataTree<Plane>();
            for(int i = 0; i < list.Count; i++)
            {
                now_path = new GH_Path();
                now_path.FromString("{" + i.ToString() + "}");
                tree.AddRange(list[i], now_path);
            }
            return tree;
        }

        public static DataTree<string> ListToTree2(List<List<string>> list)
        {
            GH_Path now_path = new GH_Path();
            DataTree<string> tree = new DataTree<string>();
            for (int i = 0; i < list.Count; i++)
            {
                now_path = new GH_Path();
                now_path.FromString("{" + i.ToString() + "}");
                tree.AddRange(list[i], now_path);
            }
            return tree;
        }

        /// <summary>
        /// 清理重复的整数
        /// </summary>
        /// <param name="ints"></param>
        public static void PurifyInts(ref List<int> ints)
        {
            List<int> new_ints = new List<int>();
            bool exsist_flag = false;
            for (int i = 0; i < ints.Count; i++)
            {
                if (i == 0)
                {
                    new_ints.Add(ints[i]);
                    continue;
                }
                for (int j = 0; j < new_ints.Count; j++)
                {
                    if (ints[i] == new_ints[j]) { exsist_flag = true; }
                }
                if (exsist_flag == false) { new_ints.Add(ints[i]); }
                else { exsist_flag = false; }
            }
            ints.Clear();
            ints = new_ints;
        }

        /// <summary>
        /// 根据一组浮点数从小到大，对一组元素排序，键值顺序改变
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="keys"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        public static bool Sort<T>(ref List<float> keys, ref List<T> values)
        {
            if (keys.Count != values.Count) { return false; }
            int pin = 0;
            while (pin < keys.Count - 1)
            {
                if (keys[pin] > keys[pin + 1])
                {
                    keys.Reverse(pin, 2);
                    values.Reverse(pin, 2);
                    pin = 0;
                }
                else { pin++; }
            }
            return true;
        }

        /// <summary>
        /// 将二维树转化为列表
        /// </summary>
        /// <param name="tree"></param>
        /// <returns></returns>
        public static List<List<Plane>> TreeToList2(GH_Structure<GH_Plane> tree)
        {
            IList<GH_Path> paths = tree.Paths;
            List<List<Plane>> list = new List<List<Plane>>();
            IEnumerable<GH_Plane> this_branch = new List<GH_Plane>();
            for (int i = 0; i < paths.Count; i++)
            {
                list.Add(new List<Plane>());
                this_branch = tree.get_Branch(paths[i]).Cast<GH_Plane>();
                List<GH_Plane> this_list = this_branch.ToList();
                for(int j = 0; j < this_list.Count(); j++)
                {
                    GH_Plane raw_item = this_list[j];
                    Plane item = new Plane();
                    raw_item.CastTo(out item);
                    list[i].Add(item);
                }
            }
            return list;
        }

        public static List<List<string>> TreeToList2(GH_Structure<GH_String> tree)
        {
            IList<GH_Path> paths = tree.Paths;
            List<List<string>> list = new List<List<string>>();
            IEnumerable<GH_String> this_branch = new List<GH_String>();
            for (int i = 0; i < paths.Count; i++)
            {
                list.Add(new List<string>());
                this_branch = tree.get_Branch(paths[i]).Cast<GH_String>();
                List<GH_String> this_list = this_branch.ToList();
                for (int j = 0; j < this_list.Count(); j++)
                {
                    GH_String raw_item = this_list[j];
                    raw_item.CastTo(out string item);
                    list[i].Add(item);
                }
            }
            return list;
        }

        /// <summary>
        /// 将三维树转化为列表
        /// </summary>
        /// <param name="tree"></param>
        /// <returns></returns>
        public static List<List<List<Point3d>>> TreeToList3(GH_Structure<GH_Point> tree)
        {
            IList<GH_Path> paths = tree.Paths;
            List<List<List<Point3d>>> list = new List<List<List<Point3d>>>();
            IEnumerable<GH_Point> this_branch = new List<GH_Point>();
            int pre_num1 = -1;
            int pre_num2 = -1;
            for(int i = 0; i < paths.Count; i++)
            {
                this_branch = tree.get_Branch(paths[i]).Cast<GH_Point>();
                List<GH_Point> this_list = this_branch.ToList();
                int now_num1 = GetPathNumber(paths[i], 1);
                int now_num2 = GetPathNumber(paths[i], 2);
                if (now_num1 != pre_num1)
                {
                    list.Add(new List<List<Point3d>>());
                    list[now_num1].Add(new List<Point3d>());
                    pre_num1 = now_num1;
                }
                else if (now_num2 != pre_num2)
                {
                    list[now_num1].Add(new List<Point3d>());
                    pre_num2 = now_num2;
                }
                for(int j = 0; j < this_list.Count; j++)
                {
                    GH_Point raw_item = this_list[j];
                    Point3d item = new Point3d();
                    raw_item.CastTo(out item);
                    list[now_num1][now_num2].Add(item);
                }
            }
            return list;
        }
    }
}