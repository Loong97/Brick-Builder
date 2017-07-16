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
    public class SensorTarget : GH_Component
    {
        public SensorTarget():base("SensorTarget", "ST","Generate mortar targets from corners","Brick","Basic")
        {
        }

        protected override Bitmap Internal_Icon_24x24
        {
            get { return new Bitmap(Resources.SensorTarget_icon); }
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddPlaneParameter("Bricks", "B", "Center planes of bricks", GH_ParamAccess.tree);
            pManager.AddTextParameter("Types", "T", "Types of bricks", GH_ParamAccess.tree);
            pManager.AddPointParameter("Corners", "C", "Corners of bricks", GH_ParamAccess.tree);
            pManager.AddPlaneParameter("SuckerCenter", "SC", "Center of sucker", GH_ParamAccess.item);
            pManager.AddPlaneParameter("SensorCenter", "SE", "Center of sensor", GH_ParamAccess.item);
            pManager.AddPlaneParameter("HomePlane", "HP", "Home planes of bricks", GH_ParamAccess.item);
            pManager.AddNumberParameter("Retreat", "R", "Retreat of sensor", GH_ParamAccess.item, 160);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddPlaneParameter("Targets", "T", "Mortar targets", GH_ParamAccess.tree);
            pManager.AddTextParameter("Names", "N", "Target names", GH_ParamAccess.tree);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            GH_Structure<GH_Plane> raw_bricks = new GH_Structure<GH_Plane>();
            GH_Structure<GH_String> raw_types = new GH_Structure<GH_String>();
            GH_Structure<GH_Point> raw_corners = new GH_Structure<GH_Point>();
            Plane suckerCenter = new Plane();
            Plane sensorCenter = new Plane();
            Plane homePlane = new Plane();
            double retreat = 160;

            if (!DA.GetDataTree("Bricks", out raw_bricks)) { return; }
            if (!DA.GetDataTree("Types", out raw_types)) { return; }
            if (!DA.GetDataTree("Corners", out raw_corners)) { return; }
            if (!DA.GetData("SuckerCenter", ref suckerCenter)) { return; }
            if (!DA.GetData("SensorCenter", ref sensorCenter)) { return; }
            if (!DA.GetData("HomePlane", ref homePlane)) { return; }
            if (!DA.GetData("Retreat", ref retreat)) { return; }

            if (raw_bricks.DataCount == 0) { return; }
            if (raw_types.DataCount == 0) { return; }
            if (raw_corners.DataCount == 0) { return; }

            if (!raw_bricks.get_DataItem(0).IsValid) { return; }
            if (!raw_types.get_DataItem(0).IsValid) { return; }
            if (!raw_corners.get_DataItem(0).IsValid) { return; }

            if (!DataTools.IsTreeDimension2(raw_bricks))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Wrong tree structure of bricks, please use {0}{1} type");
                return;
            }
            if (!DataTools.IsTreeDimension2(raw_types))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Wrong tree structure of types, please use {0}{1} type");
                return;
            }
            if (!DataTools.IsTreeDimension3(raw_corners))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Wrong tree structure of corners, please use {0;0}{0;1} type");
                return;
            }
            if (!DataTools.IsTreeMatch23(raw_bricks, raw_corners))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Bricks and corners does not match");
                return;
            }
            if (!DataTools.IsTreeMatch23(raw_types, raw_corners))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Types and corners does not match");
                return;
            }
            if (retreat < 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Retreat must not be negative");
                return;
            }

            List<List<Plane>> bricks = DataTools.TreeToList2(raw_bricks);
            List<List<string>> types = DataTools.TreeToList2(raw_types);
            List<List<List<Point3d>>> corners = DataTools.TreeToList3(raw_corners);

            List<List<Plane>> targets_list = new List<List<Plane>>();
            List<List<string>> names_list = new List<List<string>>();
            string[] title = { "Floor", "Middle", "Brick", "Sensor" };

            for(int i = 0; i < bricks.Count - 1; i++)
            {
                targets_list.Add(new List<Plane>());
                names_list.Add(new List<string>());

                targets_list[i].Add(homePlane);
                names_list[i].Add(title[0] + i.ToString() + title[1] + "0");

                for (int j = 0; j < bricks[i].Count; j++)
                {
                    Point3d[] now_points = new Point3d[4];
                    Point3d[] this_corners = corners[i][j].ToArray();

                    Point3d this_center = GeometryTools.Average(this_corners);

                    if (types[i][j] == "Horizontal" || types[i][j] == "Half")
                    {
                        now_points[0] = GeometryTools.Average(this_center, this_corners[0]);
                        now_points[1] = GeometryTools.Average(this_center, this_corners[1]);
                        now_points[2] = GeometryTools.Average(this_center, this_corners[3]);
                        now_points[3] = GeometryTools.Average(this_center, this_corners[2]);
                    }
                    else if (types[i][j] == "Vertical")
                    {
                        now_points[0] = GeometryTools.Average(this_center, this_corners[1]);
                        now_points[1] = GeometryTools.Average(this_center, this_corners[3]);
                        now_points[2] = GeometryTools.Average(this_center, this_corners[2]);
                        now_points[3] = GeometryTools.Average(this_center, this_corners[0]);
                    }

                    for (int k = 0; k < now_points.Count(); k++)
                    {
                        Plane now_plane = GeometryTools.Move(bricks[i][j], GeometryTools.P2P(bricks[i][j].Origin, now_points[k]));
                        now_plane = GeometryTools.Move(now_plane, 0, 0, retreat - 50);
                        now_plane = GeometryTools.Transform(sensorCenter, suckerCenter, now_plane);

                        targets_list[i].Add(now_plane);
                        names_list[i].Add(title[0] + i.ToString() + title[2] + j.ToString() + title[3] + k.ToString());
                    }
                }

                targets_list[i].Add(homePlane);
                names_list[i].Add(title[0] + i.ToString() + title[1] + "1");
            }

            DataTree<Plane> targets = DataTools.ListToTree2(targets_list);
            DataTree<string> names = DataTools.ListToTree2(names_list);

            DA.SetDataTree(0, targets);
            DA.SetDataTree(1, names);
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("B8FA6954-0036-4ABF-9FB2-82F7971A652C"); }
        }
    }
}