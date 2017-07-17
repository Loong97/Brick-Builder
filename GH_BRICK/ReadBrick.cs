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
    public class ReadBrick : GH_Component
    {
        public ReadBrick() : base("ReadBrick", "RB", "Read brick infomations from meshs", "Brick", "Basic")
        {
        }

        protected override Bitmap Internal_Icon_24x24
        {
            get { return new Bitmap(Resources.ReadBrick_icon); }
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter("Bricks", "B", "Input brick meshs", GH_ParamAccess.list);
            pManager.AddPointParameter("Center", "C", "Center of robot", GH_ParamAccess.item);
            pManager.AddNumberParameter("MortarThickness", "MT", "Thickness of mortar", GH_ParamAccess.item, 8);
            pManager.AddNumberParameter("zTolerence", "T", "Tolerence of a floor of bricks", GH_ParamAccess.item, 0.1);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddPlaneParameter("Planes", "P", "Planes for the tool to pick", GH_ParamAccess.tree);
            pManager.AddTextParameter("Types", "T", " types of the bricks", GH_ParamAccess.tree);
            pManager.AddPointParameter("Corners", "C", "Corners of each brick", GH_ParamAccess.tree);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Mesh> bricks = new List<Mesh>();
            Point3d center_d = new Point3f();
            double mortarth_d = 8;
            double tolerence_d = 0.1;

            if (!DA.GetDataList("Bricks", bricks)) { return; }
            if (!DA.GetData("Center", ref center_d)) { return; }
            if (!DA.GetData("MortarThickness", ref mortarth_d)) { return; }
            if (!DA.GetData("zTolerence", ref tolerence_d)) { return; }

            if (bricks.Count == 0) { return; }

            if (!bricks[0].IsValid) { return; }
            if (!center_d.IsValid) { return; }

            if (tolerence_d <= 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "zTolerence must be positive");
                return;
            }
            if (mortarth_d <= 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "MortarThickness must be positive");
                return;
            }

            Point3f center = GeometryTools.D2F(center_d);
            float mortarth = (float)mortarth_d;
            float tolerence = (float)tolerence_d;

            List<float> z_list = new List<float>();
            List<Plane> planes_list = new List<Plane>();
            List<string> types_list = new List<string>();
            List<Point3f[]> corners_list = new List<Point3f[]>();

            for (int i = 0; i < bricks.Count; i++)
            {
                Mesh this_brick = bricks[i];
                List<Point3f> vertices = new List<Point3f>();
                List<float> zs = new List<float>();
                List<Point3f> this_corner = new List<Point3f>();
                Plane this_plane = new Plane();

                //读取所有顶点，检测是否为8个
                vertices = this_brick.Vertices.ToList();
                GeometryTools.PurifyPoints(ref vertices, 0.1f);
                if (vertices.Count != 8)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "For a brick, there must be 8 vertices, now here is" + vertices.Count.ToString());
                    return;
                }

                //将顶点根据高度排序
                for (int j = 0; j < vertices.Count; j++) { zs.Add(vertices[j].Z); }
                if (!DataTools.Sort(ref zs, ref vertices)) { return; }
                
                //提取出上顶点并按3241象限顺序排序
                for (int j = 4; j < vertices.Count; j++) { this_corner.Add(vertices[j]); }
                if (!GeometryTools.Sortby3241Q(ref this_corner, center))
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Has slanted bricks");
                    GeometryTools.Sortby3241D(ref this_corner, center);
                    if (GeometryTools.Distance(this_corner[0], this_corner[2]) > GeometryTools.Distance(this_corner[0], this_corner[3]))
                    {
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Geometric error");
                        return;
                    }
                }

                //检验砖块是否存在几何错误
                if (GeometryTools.Distance(this_corner[0], this_corner[2]) > GeometryTools.Distance(this_corner[0], this_corner[3]))
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Geometrical Error.");
                    return;
                }

                //将砖块类型及放砖坐标系加入列表
                if (GeometryTools.Distance(this_corner[0], this_corner[1]) - GeometryTools.Distance(this_corner[0], this_corner[2]) > 50)
                {
                    types_list.Add("Vertical");
                    this_plane = new Plane(GeometryTools.Average(this_corner), GeometryTools.P2P(this_corner[0], this_corner[2]), GeometryTools.P2P(this_corner[0], this_corner[1]));
                }
                else if (GeometryTools.Distance(this_corner[0], this_corner[2]) - GeometryTools.Distance(this_corner[0], this_corner[1]) > 50)
                {
                    types_list.Add("Horizontal");
                    this_plane = new Plane(GeometryTools.Average(this_corner), GeometryTools.P2P(this_corner[0], this_corner[2]), GeometryTools.P2P(this_corner[0], this_corner[1]));
                }
                else
                {
                    types_list.Add("Half");
                    this_plane = new Plane(GeometryTools.Average(new Point3f[] { this_corner[0], this_corner[1] }), GeometryTools.P2P(this_corner[0], this_corner[2]), GeometryTools.P2P(this_corner[0], this_corner[1]));
                }
                planes_list.Add(this_plane);

                //将坐标系原点高度加入列表
                z_list.Add((float)this_plane.Origin.Z);

                //将上顶点加入列表
                corners_list.Add(this_corner.ToArray());
            }

            //将各元素按高度排序
            List<float> z_list_copy1 = new List<float>(z_list);
            List<float> z_list_copy2 = new List<float>(z_list);
            if (!DataTools.Sort(ref z_list_copy1, ref planes_list)) { return; }
            if (!DataTools.Sort(ref z_list_copy2, ref types_list)) { return; }
            if (!DataTools.Sort(ref z_list,ref corners_list)) { return; }

            DataTree<Plane> planes = new DataTree<Plane>();
            DataTree<string> types = new DataTree<string>();
            DataTree<Point3f> corners = new DataTree<Point3f>();

            int now_floor = 0;
            float now_z = 0;
            List<Plane> now_planes = new List<Plane>();
            List<string> now_types = new List<string>();
            List<Point3f[]> now_corners = new List<Point3f[]>();
            GH_Path now_path1 = new GH_Path();
            GH_Path now_path2 = new GH_Path();

            for (int i = 0; i < z_list.Count; i++)
            {
                //常执行
                planes_list[i] = GeometryTools.Move(planes_list[i], 0, 0, now_floor * mortarth);
                for(int j = 0; j < 4; j++)
                {
                    corners_list[i][j] = GeometryTools.Move(corners_list[i][j], 0, 0, now_floor * mortarth);
                }

                //第一个元素
                if (i == 0)
                {
                    now_floor = 0;
                    now_z = z_list[i];

                    now_planes.Clear();
                    now_types.Clear();
                    now_corners.Clear();

                    now_planes.Add(planes_list[i]);
                    now_types.Add(types_list[i]);
                    now_corners.Add(corners_list[i]);
                }

                //最后一个元素
                else if (i == z_list.Count-1)
                {
                    now_planes.Add(planes_list[i]);
                    now_types.Add(types_list[i]);
                    now_corners.Add(corners_list[i]);

                    List<Plane> now_planes_copy = new List<Plane>(now_planes);
                    GeometryTools.SortbyClock(ref now_planes_copy, ref now_types, center);
                    GeometryTools.SortbyClock(ref now_planes, ref now_corners, center);

                    now_path1 = new GH_Path();
                    now_path1.FromString("{" + now_floor.ToString() + "}");
                    planes.AddRange(now_planes, now_path1);
                    types.AddRange(now_types, now_path1);

                    for (int j = 0; j < now_corners.Count; j++)
                    {
                        now_path2 = new GH_Path();
                        now_path2.FromString("{" + now_floor.ToString() + ";" + j.ToString() + "}");
                        corners.AddRange(now_corners[j].ToList(), now_path2);
                    }
                }

                //没有高度突变
                else if (Math.Abs(z_list[i] - now_z) < tolerence)
                {
                    now_planes.Add(planes_list[i]);
                    now_types.Add(types_list[i]);
                    now_corners.Add(corners_list[i]);
                }

                //高度突变
                else
                {
                    List<Plane> now_planes_copy = new List<Plane>(now_planes);
                    GeometryTools.SortbyClock(ref now_planes_copy, ref now_types, center);
                    GeometryTools.SortbyClock(ref now_planes, ref now_corners, center);

                    now_path1 = new GH_Path();
                    now_path1.FromString("{" + now_floor.ToString() + "}");
                    planes.AddRange(now_planes, now_path1);
                    types.AddRange(now_types, now_path1);

                    for(int j = 0; j < now_corners.Count; j++)
                    {
                        now_path2 = new GH_Path();
                        now_path2.FromString("{" + now_floor.ToString() + ";" + j.ToString() + "}");
                        corners.AddRange(now_corners[j].ToList(), now_path2);
                    }

                    now_planes.Clear();
                    now_types.Clear();
                    now_corners.Clear();

                    planes_list[i] = GeometryTools.Move(planes_list[i], 0, 0, mortarth);
                    for (int j = 0; j < 4; j++)
                    {
                        corners_list[i][j] = GeometryTools.Move(corners_list[i][j], 0, 0, mortarth);
                    }

                    now_planes.Add(planes_list[i]);
                    now_types.Add(types_list[i]);
                    now_corners.Add(corners_list[i]);

                    now_floor++;
                    now_z = z_list[i];
                }
            }

            DA.SetDataTree(0, planes);
            DA.SetDataTree(1, types);
            DA.SetDataTree(2, corners);
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("AD1E4E6C-A5D5-4966-BE00-CF4781A623EE"); }
        }
    }
}
