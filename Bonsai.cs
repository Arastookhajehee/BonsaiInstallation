﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Rhino.Geometry;
using Newtonsoft.Json;
using System.Drawing;

namespace BonsaiInstallation
{
    public class TimberBranch
    {

        public Guid ID { get; set; }
        public Plane placementPlane { get; set; }
        public List<Plane> orientablePlanes { get; set; }
        public Mesh meshBox { get; set; }

        public double width = 18;
        public double length = 300;
        public double thickness = 18;
        public string user { get; set; }
        public Color color { get; set; }
        public string state { get; set; }
        public List<Guid> parentIDs { get; set; }
        public List<Guid> childIDs { get; set; }
        public Brep brep { get; set; }
        public bool selected { get; set; }
        public Plane buildOnPlane { get; set; }


        // constructor
        public TimberBranch(Plane placementPlane, string user, Color color, string state)
        {
            try
            {
                double tolerance = Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;

                this.ID = Guid.NewGuid();
                this.placementPlane = placementPlane;
                this.buildOnPlane = new Plane(placementPlane);
                this.orientablePlanes = new List<Plane>();
                this.meshBox = new Mesh();
                this.user = user;
                this.color = color;
                this.state = state;
                this.parentIDs = new List<Guid>();
                this.childIDs = new List<Guid>();

                var xInterval = new Interval(-width / 2, width / 2);
                var yInterval = new Interval(-length / 2, length / 2);
                var zInterval = new Interval(-thickness / 2, thickness / 2);
                this.brep = new Box(placementPlane, xInterval, yInterval, zInterval).ToBrep();

                List<Plane> planes = new List<Plane>();
                for (int i = 0; i < 4; i++)
                {
                    Plane plane = new Plane(placementPlane);
                    plane.Rotate(i * Math.PI / 2, placementPlane.YAxis, placementPlane.Origin);
                    planes.Add(plane);

                    Rectangle3d faceBorder = TimberBranch.GetBorderFace(plane, this.width, this.length, this.thickness);

                    MeshingParameters meshingParameters = new MeshingParameters();
                    meshingParameters.SimplePlanes = true;
                    meshingParameters.GridMaxCount = 1;

                    var border = faceBorder.ToNurbsCurve();

                    this.meshBox.Append(Mesh.CreateFromPlanarBoundary(border, meshingParameters, tolerance));

                }

                this.orientablePlanes = planes;
            }
            catch
            {
                this.ID = Guid.Empty;
            }

        }

        public void Select()
        {
            this.selected = true;
            this.color = Color.FromArgb(255, this.color.R, this.color.G, this.color.B);
        }

        public void UnSelect()
        {
            this.selected = false;
            this.color = Color.FromArgb(70, this.color.R, this.color.G, this.color.B);
        }

        public void AddParentID(Guid parentID)
        {
            this.parentIDs.Add(parentID);
        }

        public void AddChildID(Guid childID)
        {
            this.childIDs.Add(childID);
        }

        public void RemoveParentID(Guid parentID)
        {
            this.parentIDs.Remove(parentID);
        }

        public void RemoveChildID(Guid childID)
        {
            this.childIDs.Remove(childID);
        }


        // serialize to JsonObject using Newtonsoft.Json
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this);
        }

        public static TimberBranch FromJson(string json)
        {
            return JsonConvert.DeserializeObject<TimberBranch>(json);
        }

        // build on on a side of another one
        public TimberBranch BuildOn(string user, Color color, string state)
        {
            Plane plane = this.buildOnPlane;
            TimberBranch newBranch = new TimberBranch(plane, user, color, state);
            newBranch.AddParentID(this.ID);
            this.AddChildID(newBranch.ID);
            return newBranch;
        }

        public Curve GetBorderFace(int side)
        {
            TimberBranch branch = this;
            Plane orientationPlane = branch.orientablePlanes[side];
            Interval xInterval = new Interval(-branch.width / 2, branch.width / 2);
            Interval yInterval = new Interval(-branch.length / 2, branch.length / 2);
            Rectangle3d rect = new Rectangle3d(orientationPlane, xInterval, yInterval);
            rect.Transform(Transform.Translation(orientationPlane.ZAxis * -branch.thickness / 2));
            return rect.ToNurbsCurve();
        }

        public static Rectangle3d GetBorderFace(Plane orientationPlane, double width, double length, double thickness)
        {
            Interval xInterval = new Interval(-width / 2, width / 2);
            Interval yInterval = new Interval(-length / 2, length / 2);
            Rectangle3d rect = new Rectangle3d(orientationPlane, xInterval, yInterval);
            rect.Transform(Transform.Translation(orientationPlane.ZAxis * -thickness / 2));
            return rect;
        }

        public static double PlanePointZValue(Plane plane, Point3d pnt)
        {
            plane.RemapToPlaneSpace(pnt, out Point3d p);
            return p.Z;
        }

        public static string ListToJson(List<TimberBranch> list)
        {
            return JsonConvert.SerializeObject(list);
        }
        public static List<TimberBranch> FromJsonToList(string json)
        {
            return JsonConvert.DeserializeObject<List<TimberBranch>>(json);
        }

    }

}
