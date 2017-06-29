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
    public class MathTools
    {
        public static double[] Solution3(double[] input)
        {
            double[] result = { 0, 0, 0 };
            if (input.Count() != 12) { return result; }
            double a1 = input[0];
            double b1 = input[1];
            double c1 = input[2];
            double d1 = input[3];
            double a2 = input[4];
            double b2 = input[5];
            double c2 = input[6];
            double d2 = input[7];
            double a3 = input[8];
            double b3 = input[9];
            double c3 = input[10];
            double d3 = input[11];
            double delta0 = a1 * b2 * c3 + a2 * b3 * c1 + a3 * b1 * c2 - a1 * b3 * c2 - a2 * b1 * c3 - a3 * b2 * c1;
            double deltax = d1 * b2 * c3 + d2 * b3 * c1 + d3 * b1 * c2 - d1 * b3 * c2 - d2 * b1 * c3 - d3 * b2 * c1;
            double deltay = a1 * d2 * c3 + a2 * d3 * c1 + a3 * d1 * c2 - a1 * d3 * c2 - a2 * d1 * c3 - a3 * d2 * c1;
            double deltaz = a1 * b2 * d3 + a2 * b3 * d1 + a3 * b1 * d2 - a1 * b3 * d2 - a2 * b1 * d3 - a3 * b2 * d1;
            if (delta0 == 0 ) { return result; }
            double x = deltax / delta0;
            double y = deltay / delta0;
            double z = deltaz / delta0;
            result = new double[] { x, y, z };
            return result;
        }
    }
}