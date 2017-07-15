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
    public class MortarTarget : GH_Component
    {
        public MortarTarget() : base("MortarTarget", "MT", "Generate mortar targets from corners", "Brick", "Basic")
        {
        }

        protected override Bitmap Internal_Icon_24x24
        {
            get { return new Bitmap(Resources.MortarTarget_icon); }
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddPlaneParameter("Bricks", "B", "Center planes of bricks", GH_ParamAccess.tree);
            pManager.AddTextParameter("Types", "T", "Types of bricks", GH_ParamAccess.tree);
            pManager.AddPointParameter("Corners", "C", "Corners of bricks", GH_ParamAccess.tree);
            pManager.AddPlaneParameter("SuckerCenter", "SC", "Center of sucker", GH_ParamAccess.item);
            pManager.AddPlaneParameter("NozzleCenter", "NC", "Center of nozzle", GH_ParamAccess.item);
            pManager.AddPlaneParameter("HomePlane", "HP", "Home planes of bricks", GH_ParamAccess.item);
            pManager.AddNumberParameter("HorizontalBorder", "HB", "Horizontal border of mortar", GH_ParamAccess.item, 25);
            pManager.AddNumberParameter("VerticalBorder", "VB", "Vertical border of mortar", GH_ParamAccess.item, 25);
            pManager.AddNumberParameter("Gap", "G", "Gap of border", GH_ParamAccess.item, 25);
            pManager.AddNumberParameter("Retreat", "R", "Retreat of nozzle", GH_ParamAccess.item, 10);
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
            Plane nozzleCenter = new Plane();
            Plane homePlane = new Plane();
            double horizontalBorder = 25;
            double verticalBorder = 25;
            double gap = 25;
            double retreat = 10;

            if (!DA.GetDataTree("Bricks", out raw_bricks)) { return; }
            if (!DA.GetDataTree("Types", out raw_types)) { return; }
            if (!DA.GetDataTree("Corners", out raw_corners)) { return; }
            if (!DA.GetData("SuckerCenter", ref suckerCenter)) { return; }
            if (!DA.GetData("NozzleCenter", ref nozzleCenter)) { return; }
            if (!DA.GetData("HomePlane", ref homePlane)) { return; }
            if (!DA.GetData("HorizontalBorder", ref horizontalBorder)) { return; }
            if (!DA.GetData("VerticalBorder", ref verticalBorder)) { return; }
            if (!DA.GetData("Gap", ref gap)) { return; }
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
            if (horizontalBorder < 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Horizontal border must not be negative");
                return;
            }
            if (verticalBorder < 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Vertical border must not be negative");
                return;
            }
            if (gap <= 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Gap must be positive");
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
            string[] title = { "Floor", "Target" };

            for (int i = 0; i < bricks.Count-1; i++)
            {
                targets_list.Add(new List<Plane>());
                names_list.Add(new List<string>());

                int count = 0;
                targets_list[i].Add(homePlane);
                names_list[i].Add(title[0] + i.ToString() + title[1] + count.ToString());

                for (int j = 0; j < bricks[i].Count; j++)
                {
                    List<Plane> now_planes = new List<Plane>();
                    Point3d[] sorted_corners = new Point3d[4];
                    Point3d[] this_corners = corners[i][j].ToArray();

                    if (types[i][j] == "Horizontal")
                    {
                        sorted_corners = this_corners;
                        now_planes = GeometryTools.Hatch(bricks[i][j], sorted_corners, horizontalBorder, verticalBorder, gap);
                    }
                    else if (types[i][j] == "Vertical")
                    {
                        sorted_corners[0] = this_corners[1];
                        sorted_corners[1] = this_corners[3];
                        sorted_corners[2] = this_corners[0];
                        sorted_corners[3] = this_corners[2];
                        now_planes = GeometryTools.Hatch(bricks[i][j], sorted_corners, verticalBorder, horizontalBorder, gap);
                    }
                    else
                    {
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Wrong type name");
                        return;
                    }

                    for (int k = 0; k < now_planes.Count; k++)
                    {
                        now_planes[k] = GeometryTools.Move(now_planes[k], 0, 0, retreat);
                        now_planes[k] = GeometryTools.Transform(nozzleCenter, suckerCenter, now_planes[k]);
                        if (j == 0 && k == 0)
                        {
                            count++;
                            targets_list[i].Add(GeometryTools.Move(now_planes[k], -bricks[i][j].XAxis * 150));
                            names_list[i].Add(title[0] + i.ToString() + title[1] + count.ToString());
                        }
                        count++;
                        targets_list[i].Add(now_planes[k]);
                        names_list[i].Add(title[0] + i.ToString() + title[1] + count.ToString());
                        if (j == bricks[i].Count - 1 && k == now_planes.Count - 1)
                        {
                            count++;
                            targets_list[i].Add(GeometryTools.Move(now_planes[k], bricks[i][j].XAxis * 150));
                            names_list[i].Add(title[0] + i.ToString() + title[1] + count.ToString());
                        }
                    }
                }

                count++;
                targets_list[i].Add(homePlane);
                names_list[i].Add(title[0] + i.ToString() + title[1] + count.ToString());
            }

            DataTree<Plane> targets = DataTools.ListToTree2(targets_list);
            DataTree<string> names = DataTools.ListToTree2(names_list);

            DA.SetDataTree(0, targets);
            DA.SetDataTree(1, names);
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("4BD51E68-0B3F-4CCA-A2A1-AEE79A95BA92"); }
        }
    }
}