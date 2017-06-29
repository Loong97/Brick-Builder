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
    public class PickBrick : GH_Component
    {
        public PickBrick() : base("PickBrick", "PB", "Generate pick planes", "Brick", "Basic")
        {
        }

        protected override Bitmap Internal_Icon_24x24
        {
            get { return new Bitmap(Resources.PickBrick_icon); }
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter("Brick", "B", "Brick to pick", GH_ParamAccess.item);
            pManager.AddPointParameter("Center", "C", "Center of robot", GH_ParamAccess.item);
            pManager.AddTextParameter("Types", "T", "Types of bricks", GH_ParamAccess.tree);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddPlaneParameter("Planes", "P", "Planes for the tool to pick", GH_ParamAccess.tree);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Mesh brick = new Mesh();
            Point3d center_d = new Point3f();
            GH_Structure<GH_String> raw_types = new GH_Structure<GH_String>();

            if (!DA.GetData("Brick", ref brick)) { return; }
            if (!DA.GetData("Center", ref center_d)) { return; }
            if (!DA.GetDataTree("Types", out raw_types)) { return; }

            if (raw_types.DataCount == 0) { return; }

            if (!raw_types.get_DataItem(0).IsValid) { return; }
            if (!center_d.IsValid) { return; }

            if (!DataTools.IsTreeDimension2(raw_types))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Wrong tree structure of types, please use {0}{1} type");
                return;
            }

            Point3f center = GeometryTools.D2F(center_d);
            List<List<string>> types = DataTools.TreeToList2(raw_types);

            List<Point3f> vertices = new List<Point3f>();
            List<Line> lines = new List<Line>();
            List<float> distances = new List<float>();
            List<Point3f> corners = new List<Point3f>();
            Point3f corner = new Point3f();
            Point3f average = new Point3f();

            //读取所有顶点，检测是否为8个
            vertices = brick.Vertices.ToList();
            GeometryTools.PurifyPoints(ref vertices, 0.1f);
            if (vertices.Count != 8)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "For a brick, there must be 8 vertices, now here is" + vertices.Count.ToString());
                return;
            }

            //取出各边，按边长排序
            for(int i = 0; i < vertices.Count-1; i++)
            {
                for(int j = i + 1; j < vertices.Count; j++)
                {
                    lines.Add(new Line(vertices[i], vertices[j]));
                    distances.Add(GeometryTools.Distance(vertices[i], vertices[j]));
                }
            }
            DataTools.Sort(ref distances, ref lines);

            //取出最短四条边的上顶点
            for (int i = 0; i < 4; i++)
            {
                Point3f from = GeometryTools.D2F(lines[i].From);
                Point3f to = GeometryTools.D2F(lines[i].To);
                float z1 = from.Z;
                float z2 = to.Z;
                if (z1 > z2) { corners.Add(from); }
                else { corners.Add(to); }
            }

            //按据角点距离排序顶点
            average = GeometryTools.Average(corners);
            distances.Clear();
            for(int i = 0; i < corners.Count; i++)
            {
                distances.Add(GeometryTools.Distance(corners[i], center));
            }
            DataTools.Sort(ref distances, ref corners);
            corner = corners[0];
            distances.Clear();
            for (int i = 0; i < corners.Count; i++)
            {
                distances.Add(GeometryTools.Distance(corners[i], corner));
            }
            DataTools.Sort(ref distances, ref corners);

            List<List<Plane>> planes_list = new List<List<Plane>>();

            //生成抓取坐标系
            for(int i = 0; i < types.Count; i++)
            {
                planes_list.Add(new List<Plane>());
                for (int j = 0; j < types[i].Count; j++)
                {
                    if(types[i][j]== "Horizontal")
                    {
                        planes_list[i].Add(new Plane(average, GeometryTools.P2P(corners[0], corners[2]), GeometryTools.P2P(corners[0], corners[1])));
                    }
                    else if(types[i][j]== "Vertical")
                    {
                        planes_list[i].Add(new Plane(average, GeometryTools.P2P(corners[0], corners[1]), GeometryTools.P2P(corners[0], corners[2])));
                    }
                    else
                    {
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Wrong type name");
                        return;
                    }
                }
            }

            DataTree<Plane> plane = DataTools.ListToTree2(planes_list);

            DA.SetDataTree(0, plane);
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("084B1000-FAB9-45AE-8AFB-FBF77AAA8CC4"); }
        }
    }
}