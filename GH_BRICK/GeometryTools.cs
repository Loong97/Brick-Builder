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
    /// 一些与几何操作有关的工具函数
    /// </summary>
    public class GeometryTools
    {
        /// <summary>
        /// 求一组点的平均点
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        public static Point3f Average(List<Point3f> list)
        {
            Point3f[] _list = list.ToArray();
            Point3f average = Average(_list);
            return average;
        }

        public static Point3f Average(Point3f[] list)
        {
            float x_sum = 0;
            float y_sum = 0;
            float z_sum = 0;
            int count = list.Count();
            for (int i = 0; i < count; i++)
            {
                x_sum += list[i].X;
                y_sum += list[i].Y;
                z_sum += list[i].Z;
            }
            x_sum /= count;
            y_sum /= count;
            z_sum /= count;
            return new Point3f(x_sum, y_sum, z_sum);
        }

        public static Point3f Average(Point3f a, Point3f b)
        {
            Point3f[] list = new Point3f[] { a, b };
            return Average(list);
        }

        public static Point3d Average(List<Point3d> list)
        {
            Point3d[] _list = list.ToArray();
            Point3d average = Average(_list);
            return average;
        }

        public static Point3d Average(Point3d[] list)
        {
            double x_sum = 0;
            double y_sum = 0;
            double z_sum = 0;
            int count = list.Count();
            for (int i = 0; i < count; i++)
            {
                x_sum += list[i].X;
                y_sum += list[i].Y;
                z_sum += list[i].Z;
            }
            x_sum /= count;
            y_sum /= count;
            z_sum /= count;
            return new Point3d(x_sum, y_sum, z_sum);
        }

        public static Point3d Average(Point3d a, Point3d b)
        {
            Point3d[] list = new Point3d[]{ a, b };
            return Average(list);
        }

        /// <summary>
        /// 将点（3d）转化为点（3f）
        /// </summary>
        /// <param name="point_d"></param>
        /// <returns></returns>
        public static Point3f D2F(Point3d point_d)
        {
            return new Point3f((float)point_d.X, (float)point_d.Y, (float)point_d.Z);
        }

        /// <summary>
        /// 计算两点之间距离
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static float Distance(Point3f a, Point3f b)
        {
            double x2 = Math.Pow(a.X - b.X, 2);
            double y2 = Math.Pow(a.Y - b.Y, 2);
            double z2 = Math.Pow(a.Z - b.Z, 2);
            return (float)Math.Sqrt(x2 + y2 + z2);
        }

        public static double Distance(Point3d a, Point3d b)
        {
            double x2 = Math.Pow(a.X - b.X, 2);
            double y2 = Math.Pow(a.Y - b.Y, 2);
            double z2 = Math.Pow(a.Z - b.Z, 2);
            return Math.Sqrt(x2 + y2 + z2);
        }

        /// <summary>
        /// 生成满铺路径
        /// </summary>
        /// <param name="plane"></param>
        /// <param name="corners"></param>
        /// <param name="hb"></param>
        /// <param name="vb"></param>
        /// <param name="gap"></param>
        /// <returns></returns>
        public static List<Plane> Hatch(Plane plane, Point3d[] corners, double hb, double vb, double gap)
        {
            Vector3d hd = P2P(corners[0], corners[2]);
            Vector3d vd = P2P(corners[0], corners[1]);
            hd.Unitize();
            vd.Unitize();
            double hl = Distance(corners[0], corners[2]);
            double vl = Distance(corners[0], corners[1]);
            double thl = hl - 2 * hb;
            double tvl = vl - 2 * vb;
            if (thl < 0) { thl = 0; }
            if (tvl < 0) { tvl = 0; }
            int n = (int)Math.Floor((thl / gap) + 1);
            List<Plane> hatch = new List<Plane>();
            Plane plane0 = Move(plane, P2P(plane.Origin, corners[0]));
            for (int i = 0; i < n; i++)
            {
                Plane this_hatch1 = new Plane();
                Plane this_hatch2 = new Plane();
                this_hatch1 = Move(plane0, vd * vb);
                this_hatch1 = Move(this_hatch1, hd * (i * gap + hb));
                this_hatch2 = Move(this_hatch1, vd * tvl);
                if (i % 2 == 0)
                {
                    hatch.Add(this_hatch1);
                    hatch.Add(this_hatch2);
                }
                else
                {
                    hatch.Add(this_hatch2);
                    hatch.Add(this_hatch1);
                }
            }
            return hatch;
        }

        /// <summary>
        /// 若向量（3f）b在向量（3f）a左，返回-1，共线返回0，右返回1
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static int LoR(Vector3f a, Vector3f b)
        {
            float ax = a.X;
            float ay = a.Y;
            float bx = b.X;
            float by = b.Y;
            if (ax * by > ay * bx) { return -1; }
            else if (ax * by == ay * bx) { return 0; }
            else { return 1; }
        }

        /// <summary>
        /// 移动一个坐标系
        /// </summary>
        /// <param name="plane"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        public static Plane Move(Plane plane, float x, float y, float z)
        {
            Vector3d xa = plane.XAxis;
            Vector3d ya = plane.YAxis;
            float _x = (float)plane.OriginX;
            float _y = (float)plane.OriginY;
            float _z = (float)plane.OriginZ;
            _x += x;
            _y += y;
            _z += z;
            Point3d origin = new Point3d(_x, _y, _z);
            return new Plane(origin, xa, ya);
        }

        public static Plane Move(Plane plane, double x, double y, double z)
        {
            Vector3d xa = plane.XAxis;
            Vector3d ya = plane.YAxis;
            double _x = plane.OriginX;
            double _y = plane.OriginY;
            double _z = plane.OriginZ;
            _x += x;
            _y += y;
            _z += z;
            Point3d origin = new Point3d(_x, _y, _z);
            return new Plane(origin, xa, ya);
        }

        public static Plane Move(Plane plane, Vector3d direction)
        {
            Vector3d xa = plane.XAxis;
            Vector3d ya = plane.YAxis;
            double _x = plane.OriginX;
            double _y = plane.OriginY;
            double _z = plane.OriginZ;
            _x += direction.X;
            _y += direction.Y;
            _z += direction.Z;
            Point3d origin = new Point3d(_x, _y, _z);
            return new Plane(origin, xa, ya);
        }

        /// <summary>
        /// 移动一个点
        /// </summary>
        /// <param name="point"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        public static Point3f Move(Point3f point, float x, float y, float z)
        {
            float _x = point.X;
            float _y = point.Y;
            float _z = point.Z;
            _x += x;
            _y += y;
            _z += z;
            return new Point3f(_x, _y, _z);
        }

        public static Point3d Move(Point3d point, Vector3d direction)
        {
            double _x = point.X;
            double _y = point.Y;
            double _z = point.Z;
            _x += direction.X;
            _y += direction.Y;
            _z += direction.Z;
            return new Point3d(_x, _y, _z);
        }

        /// <summary>
        /// 返回两点（3f）之间向量（3f）
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public static Vector3f P2P(Point3f origin, Point3f target)
        {
            float x = target.X - origin.X;
            float y = target.Y - origin.Y;
            float z = target.Z - origin.Z;
            return new Vector3f(x, y, z);
        }

        public static Vector3d P2P(Point3d origin, Point3d target)
        {
            double x = target.X - origin.X;
            double y = target.Y - origin.Y;
            double z = target.Z - origin.Z;
            return new Vector3d(x, y, z);
        }

        /// <summary>
        /// 找到世界坐标系中一个点/向量在局部坐标系中坐标
        /// </summary>
        /// <param name="plane"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        public static Point3d PlaneIn(Plane plane, Point3d point)
        {
            double x = point.X - plane.OriginX;
            double y = point.Y - plane.OriginY;
            double z = point.Z - plane.OriginZ;
            double xx = plane.XAxis.X;
            double xy = plane.XAxis.Y;
            double xz = plane.XAxis.Z;
            double yx = plane.YAxis.X;
            double yy = plane.YAxis.Y;
            double yz = plane.YAxis.Z;
            double zx = plane.ZAxis.X;
            double zy = plane.ZAxis.Y;
            double zz = plane.ZAxis.Z;
            double[] input = { xx, yx, zx, x, xy, yy, zy, y, xz, yz, zz, z };
            double[] output = MathTools.Solution3(input);
            double xn = output[0];
            double yn = output[1];
            double zn = output[2];
            Point3d result = new Point3d(xn, yn, zn);
            return result;
        }

        public static Vector3d PlaneIn(Plane plane, Vector3d vector)
        {
            Point3d point = new Point3d(vector);
            point = Move(point, new Vector3d(plane.Origin));
            point = PlaneIn(plane, point);
            Vector3d result = new Vector3d(point);
            return result;
        }

        /// <summary>
        /// 找到局部坐标系中一个点/向量在世界坐标系中坐标
        /// </summary>
        /// <param name="plane"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        public static Point3d PlaneOut(Plane plane, Point3d point)
        {
            double x = point.X;
            double y = point.Y;
            double z = point.Z;
            Point3d origin = plane.Origin;
            Vector3d xa = plane.XAxis;
            Vector3d ya = plane.YAxis;
            Vector3d za = plane.ZAxis;
            xa.Unitize();
            ya.Unitize();
            za.Unitize();
            Point3d result = Move(origin, xa * x);
            result = Move(result, ya * y);
            result = Move(result, za * z);
            return result;
        }

        public static Vector3d PlaneOut(Plane plane, Vector3d vector)
        {
            Point3d point = new Point3d(vector);
            point = PlaneOut(plane, point);
            point = Move(point, -(new Vector3d(plane.Origin)));
            Vector3d result = new Vector3d(point);
            return result;
        }

        /// <summary>
        /// 清理重复的点（3f）
        /// </summary>
        /// <param name="points"></param>
        /// <param name="tolerence"></param>
        public static void PurifyPoints(ref List<Point3f> points, float tolerence)
        {
            List<Point3f> new_points = new List<Point3f>();
            bool exsist_flag = false;
            for (int i = 0; i < points.Count; i++)
            {
                if (i == 0)
                {
                    new_points.Add(points[i]);
                    continue;
                }
                for (int j = 0; j < new_points.Count; j++)
                {
                    if (Distance(points[i], new_points[j]) < tolerence) { exsist_flag = true; }
                }
                if (exsist_flag == false) { new_points.Add(points[i]); }
                else { exsist_flag = false; }
            }
            points.Clear();
            points = new_points;
        }

        /// <summary>
        /// 返回向量b（3f）相对于向量a（3f）的象限（a作y轴），在轴上返回0
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static int Quadrant(Vector3f a, Vector3f b)
        {
            float ax = a.X;
            float ay = a.Y;
            float bx = b.X;
            float by = b.Y;
            if (ax * by - ay * bx == 0) { return 0; }
            else if (ax * by - ay * bx < 0)
            {
                if (ay * by + ax * bx == 0) { return 0; }
                else if (ay * by + ax * bx > 0) { return 1; }
                else { return 4; }
            }
            else
            {
                if (ay * by + ax * bx == 0) { return 0; }
                else if (ay * by + ax * bx > 0) { return 2; }
                else { return 3; }
            }
        }

        /// <summary>
        /// 根据一个中心点（3f）顺时针对一组坐标系和一组对应值进行排序，键值顺序改变
        /// </summary>
        /// <param name="planes"></param>
        /// <param name="center"></param>
        /// <returns></returns>
        public static void SortbyClock<T>(ref List<Plane> planes, ref List<T> values, Point3f center)
        {
            List<Point3f> origins = new List<Point3f>();
            for (int i = 0; i < planes.Count; i++)
            {
                origins.Add(D2F(planes[i].Origin));
            }
            Point3f average = Average(origins);
            Vector3f vector1 = P2P(center, average);
            List<float>[] distances = new List<float>[5];
            List<Plane>[] part_planes = new List<Plane>[5];
            List<T>[] part_values = new List<T>[5];
            for (int i = 0; i < 5; i++)
            {
                distances[i] = new List<float>();
                part_planes[i] = new List<Plane>();
                part_values[i] = new List<T>();
            }
            for (int i = 0; i < origins.Count; i++)
            {
                Vector3f vector2 = P2P(center, origins[i]);
                int quadrant = Quadrant(vector1, vector2);
                distances[quadrant].Add(Distance(origins[i], average));
                part_planes[quadrant].Add(planes[i]);
                part_values[quadrant].Add(values[i]);
            }
            for (int i = 0; i < 5; i++)
            {
                if (distances[i].Count != 0)
                {
                    List<float> _distance = new List<float>(distances[i]);
                    DataTools.Sort(ref _distance, ref part_planes[i]);
                    DataTools.Sort(ref distances[i], ref part_values[i]);
                }
            }
            part_planes[4].Reverse();
            part_values[4].Reverse();
            part_planes[2].Reverse();
            part_values[2].Reverse();
            planes.Clear();
            values.Clear();
            if (part_planes[3].Count != 0) { planes.AddRange(part_planes[3]); }
            if (part_planes[2].Count != 0) { planes.AddRange(part_planes[2]); }
            if (part_planes[1].Count != 0) { planes.AddRange(part_planes[1]); }
            if (part_planes[4].Count != 0) { planes.AddRange(part_planes[4]); }
            if (part_values[3].Count != 0) { values.AddRange(part_values[3]); }
            if (part_values[2].Count != 0) { values.AddRange(part_values[2]); }
            if (part_values[1].Count != 0) { values.AddRange(part_values[1]); }
            if (part_values[4].Count != 0) { values.AddRange(part_values[4]); }
        }

        /// <summary>
        /// 根据一个中心点（3f）对四个点（3f）按照3241象限顺序排序，根据象限
        /// </summary>
        /// <param name="points"></param>
        /// <param name="center"></param>
        /// <returns></returns>
        public static bool Sortby3241Q(ref List<Point3f> points, Point3f center)
        {
            if (points.Count != 4) { return false; }
            Point3f average = Average(points);
            Vector3f vector1 = P2P(center, average);
            Point3f[] new_points = new Point3f[4];
            bool[] flags = { false, false, false, false };
            int[] map = { 3, 2, 4, 1 };
            for (int i = 0; i < 4; i++)
            {
                Vector3f vector2 = P2P(average, points[i]);
                int quadrant = Quadrant(vector1, vector2);
                if (quadrant == 0) { return false; }
                for (int j = 0; j < 4; j++)
                {
                    if (quadrant == map[j])
                    {
                        if (flags[j]) { return false; }
                        new_points[j] = points[i];
                        flags[j] = true;
                    }
                }
            }
            points.Clear();
            points = new_points.ToList();
            return true;
        }

        /// <summary>
        /// 根据一个中心点（3f）对四个点（3f）按照3241象限顺序排序，根据距离
        /// </summary>
        /// <param name="points"></param>
        /// <param name="center"></param>
        public static void Sortby3241D(ref List<Point3f> points, Point3f center)
        {
            List<float> distances = new List<float>();
            for(int i = 0; i < points.Count; i++)
            {
                distances[i] = Distance(points[i], center);
            }
            DataTools.Sort(ref distances, ref points);
            Point3f[] new_points = new Point3f[4];
            new_points[0] = points[0];
            new_points[1] = points[2];
            new_points[2] = points[1];
            new_points[3] = points[3];
            points.Clear();
            points = new_points.ToList();
        }

        /// <summary>
        /// 根据两个坐标系变换一个坐标系
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="target"></param>
        /// <param name="geo"></param>
        /// <returns></returns>
        public static Plane Transform(Plane origin, Plane target, Plane geo)
        {
            Point3d ori = target.Origin;
            Vector3d xa = target.XAxis;
            Vector3d ya = target.YAxis;
            Point3d inOri = PlaneIn(origin, ori);
            Vector3d inXa = PlaneIn(origin, xa);
            Vector3d inYa = PlaneIn(origin, ya);
            Point3d ouOri = PlaneOut(geo, inOri);
            Vector3d ouXa = PlaneOut(geo, inXa);
            Vector3d ouYa = PlaneOut(geo, inYa);
            Plane result = new Plane(ouOri, ouXa, ouYa);
            return result;
        }
    }
}