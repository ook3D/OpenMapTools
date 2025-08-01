﻿using CodeWalker.Core.GameFiles.FileTypes.Builders;
using CodeWalker.GameFiles;
using Ookii.Dialogs.WinForms;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace VONV
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            this.Load += Form1_Load;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Load settings
            InputFolderTextBox.Text = Properties.Settings.Default.LastInputFolder;
            OutputFolderTextBox.Text = Properties.Settings.Default.LastOutputFolder;

            OffsetXTextBox.Text = Properties.Settings.Default.OffsetX.ToString(CultureInfo.InvariantCulture);
            OffsetYTextBox.Text = Properties.Settings.Default.OffsetY.ToString(CultureInfo.InvariantCulture);
            OffsetZTextBox.Text = Properties.Settings.Default.OffsetZ.ToString(CultureInfo.InvariantCulture);
        }
        private void InputFolderBrowseButton_Click(object sender, EventArgs e)
        {
            using (var dialog = new VistaFolderBrowserDialog())
            {
                dialog.Description = "Select a folder";
                dialog.UseDescriptionForTitle = true;
                dialog.SelectedPath = Properties.Settings.Default.LastInputFolder;

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    InputFolderTextBox.Text = dialog.SelectedPath;
                    Properties.Settings.Default.LastInputFolder = dialog.SelectedPath;
                    Properties.Settings.Default.Save();
                }
            }
        }

        private void OutputFolderBrowseButton_Click(object sender, EventArgs e)
        {
            using (var dialog = new VistaFolderBrowserDialog())
            {
                dialog.Description = "Select a folder";
                dialog.UseDescriptionForTitle = true;
                dialog.SelectedPath = Properties.Settings.Default.LastOutputFolder;

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    OutputFolderTextBox.Text = dialog.SelectedPath;
                    Properties.Settings.Default.LastOutputFolder = dialog.SelectedPath;
                    Properties.Settings.Default.Save();
                }
            }
        }

        [Flags]
        public enum PolyFlags1 : byte
        {
            None = 0,
            SmallPoly = 1 << 0,
            LargePoly = 1 << 1,
            IsPavement = 1 << 2,
            IsUnderground = 1 << 3,
            Unused1 = 1 << 4,
            Unused2 = 1 << 5,
            IsTooSteepToWalk = 1 << 6,
            IsWater = 1 << 7
        }

        [Flags]
        public enum PolyFlags2 : uint
        {
            None = 0,
            ZeroAreaStitchPoly = 1 << 0,
            NetworkSpawnCandidate = 1 << 1,
            IsRoad = 1 << 2,
            LiesAlongEdgeOfMesh = 1 << 3,
            IsTrainTrack = 1 << 4,
            IsShallowWater = 1 << 5,
            PedDensity1 = 1 << 6,
            PedDensity2 = 1 << 7,
            PedDensity3 = 1 << 8,
        }

        [Flags]
        public enum PolyFlags3 : uint
        {
            None = 0,
            AudioFlag1 = 1 << 0,        // AudioFlag1
            AudioFlag2 = 1 << 1,        // AudioFlag2
            AudioFlag3 = 1 << 2,        // AudioFlag3
            AudioFlag4 = 1 << 3,        // AudioFlag4
            DebugUnused = 1 << 4,       // Debug
            NearCarNode = 1 << 5,       // NearCarNode
            IsInterior = 1 << 6,        // Interior
            IsIsolated = 1 << 7,        // IsIsolated
            CoverSouth = 1 << 8,        // CoverSouth
            CoverSouthEast = 1 << 9,    // CoverSouthEast
            CoverEast = 1 << 10,        // CoverEast
            CoverNorthEast = 1 << 11,   // CoverNorthEast
            CoverNorth = 1 << 12,       // CoverNorth
            CoverNorthWest = 1 << 13,   // CoverNorthWest
            CoverWest = 1 << 14,        // CoverWest
            CoverSouthWest = 1 << 15,   // CoverSouthWest
        }


        private void ProcessButton_Click(object sender, EventArgs e)
        {
            if (!Directory.Exists(InputFolderTextBox.Text))
            {
                MessageBox.Show("Input folder doesn't exist: " + InputFolderTextBox.Text);
                return;
            }
            if (!Directory.Exists(OutputFolderTextBox.Text))
            {
                MessageBox.Show("Output folder doesn't exist: " + OutputFolderTextBox.Text);
                return;
            }

            Cursor = Cursors.WaitCursor;


            string inputpath = InputFolderTextBox.Text;
            string outputpath = OutputFolderTextBox.Text;
            string[] onvfiles = Directory.GetFiles(inputpath, "*.onv", SearchOption.TopDirectoryOnly);
            float offsetx = float.Parse(OffsetXTextBox.Text, CultureInfo.InvariantCulture);
            float offsety = float.Parse(OffsetYTextBox.Text, CultureInfo.InvariantCulture);
            float offsetz = float.Parse(OffsetZTextBox.Text, CultureInfo.InvariantCulture);

            Properties.Settings.Default.OffsetX = offsetx;
            Properties.Settings.Default.OffsetY = offsety;
            Properties.Settings.Default.OffsetZ = offsetz;
            Properties.Settings.Default.Save();

            Vector3 offset = new Vector3(offsetx, offsety, offsetz);
            var vehc = VehicleCheckBox.Checked;

            if (!outputpath.EndsWith("\\"))
            {
                outputpath = outputpath + "\\";
            }

            StringBuilder errorlog = new StringBuilder();


            List<OnvFile> onvlist = new List<OnvFile>();
            Dictionary<int, OnvFile> onvdict = new Dictionary<int, OnvFile>();
            YnvBuilder builder = new YnvBuilder();

            foreach (var onvfile in onvfiles) //load onv files...
            {
                try
                {
                    OnvFile onv = new OnvFile();
                    onv.Load(onvfile);
                    onv.InitPolys();
                    onvlist.Add(onv);
                    onvdict[onv.SectorID] = onv;
                }
                catch (Exception ex)
                {
                    string err = "Error loading " + onvfile + ":\n" + ex.ToString();
                    errorlog.AppendLine(err);
                }
            }


            foreach (var onv in onvlist) //join up all the edges
            {
                onv.InitEdges(onvdict);
            }

            foreach (var onv in onvlist) //offset all the vertices
            {
                for (int i = 0; i < onv.VerticesWS.Count; i++)
                {
                    onv.VerticesWS[i] = onv.VerticesWS[i] + offset;
                }
                foreach (var poly in onv.Polys)
                {
                    if (poly.Vertices != null)
                    {
                        for (int i = 0; i < poly.Vertices.Length; i++)
                        {
                            poly.Vertices[i] = poly.Vertices[i] + offset;
                        }
                    }
                }
                foreach (var portal in onv.Portals)
                {
                    //TODO: offset portals
                }
                //TODO: points ("Bounds")
            }

            foreach (var onv in onvlist)
            {
                foreach (var poly in onv.Polys)
                {
                    var ypoly = builder.AddPoly(poly.Vertices);
                    poly.NewPoly = ypoly;

                    var flags1 = (PolyFlags1)poly.Flags1;
                    var flags2 = (PolyFlags2)poly.Flags2;
                    var flags3 = (PolyFlags3)poly.Flags3;

                    // flags1
                    ypoly.SmallPoly =               flags1.HasFlag(PolyFlags1.SmallPoly);
                    ypoly.LargePoly =               flags1.HasFlag(PolyFlags1.LargePoly);
                    ypoly.IsPavement =              flags1.HasFlag(PolyFlags1.IsPavement);
                    ypoly.IsUnderground =           flags1.HasFlag(PolyFlags1.IsUnderground);
                    ypoly.Unused1 =                 flags1.HasFlag(PolyFlags1.Unused1);
                    ypoly.Unused2 =                 flags1.HasFlag(PolyFlags1.Unused2);
                    ypoly.IsTooSteepToWalk =        flags1.HasFlag(PolyFlags1.IsTooSteepToWalk);
                    ypoly.IsWater =                 flags1.HasFlag(PolyFlags1.IsWater);

                    // flags3
                    ypoly.AudioProperties1 =        flags3.HasFlag(PolyFlags3.AudioFlag1);
                    ypoly.AudioProperties2 =        flags3.HasFlag(PolyFlags3.AudioFlag2);
                    ypoly.AudioProperties3 =        flags3.HasFlag(PolyFlags3.AudioFlag3);
                    ypoly.AudioProperties4 =        flags3.HasFlag(PolyFlags3.AudioFlag4);
                    ypoly.Unused3 =                 flags3.HasFlag(PolyFlags3.DebugUnused);
                    ypoly.NearCarNode =             flags3.HasFlag(PolyFlags3.NearCarNode);
                    ypoly.IsInterior =              flags3.HasFlag(PolyFlags3.IsInterior);
                    ypoly.IsIsolated =              flags3.HasFlag(PolyFlags3.IsIsolated);
                    ypoly.CoverSouth =              flags3.HasFlag(PolyFlags3.CoverSouth);
                    ypoly.CoverSouthEast =          flags3.HasFlag(PolyFlags3.CoverSouthEast);
                    ypoly.CoverEast =               flags3.HasFlag(PolyFlags3.CoverEast);
                    ypoly.CoverNorthEast =          flags3.HasFlag(PolyFlags3.CoverNorthEast);
                    ypoly.CoverNorth =              flags3.HasFlag(PolyFlags3.CoverNorth);
                    ypoly.CoverNorthWest =          flags3.HasFlag(PolyFlags3.CoverNorthWest);
                    ypoly.CoverWest =               flags3.HasFlag(PolyFlags3.CoverWest);
                    ypoly.CoverSouthWest =          flags3.HasFlag(PolyFlags3.CoverSouthWest);

                    // flags2
                    ypoly.ZeroAreaStitchPoly =      flags2.HasFlag(PolyFlags2.ZeroAreaStitchPoly);
                    ypoly.NetworkSpawnCandidate =   flags2.HasFlag(PolyFlags2.NetworkSpawnCandidate);
                    ypoly.IsRoad =                  false;
                    ypoly.LiesAlongEdgeOfMesh =     flags2.HasFlag(PolyFlags2.LiesAlongEdgeOfMesh);
                    ypoly.IsTrainTrack =            flags2.HasFlag(PolyFlags2.IsTrainTrack);
                    ypoly.IsShallowWater =          flags2.HasFlag(PolyFlags2.IsShallowWater);
                    ypoly.PedDensity1 =             flags2.HasFlag(PolyFlags2.IsRoad);
                    ypoly.PedDensity2 =             flags2.HasFlag(PolyFlags2.IsRoad);
                    ypoly.PedDensity3 =             flags2.HasFlag(PolyFlags2.IsRoad);


                    ypoly.CentroidX = 127;
                    ypoly.CentroidY = 127;
                }
            }

            foreach (var onv in onvlist) //create the new edges
            {
                foreach (var poly in onv.Polys)
                {
                    var ypoly = poly.NewPoly;
                    int ec = poly.Edges?.Length ?? 0;

                    if (ec == 0)
                    { continue; }//no edges? shouldn't happen
                    if (ec != ypoly.Vertices?.Length)
                    { continue; }//shouldn't happen! error log this?

                    var newedges = new YnvEdge[ec];
                    for (int i = 0; i < ec; i++)
                    {
                        var edge = poly.Edges[i];
                        var yedge = new YnvEdge();
                        bool haspoly = (edge.Poly1?.NewPoly != null);
                        bool f1 = (edge.Flags1 & 1) > 0;//nonav?
                        bool f2 = (edge.Flags1 & 2) > 0;//nonav?
                        bool f3 = (edge.Flags1 & 4) > 0;//drop?
                        bool f4 = (edge.Flags1 & 8) > 0;//suicide?
                        bool f5 = (edge.Flags1 & 1) > 0;
                        bool nonav = f1 || f2;

                        uint edgeval = 62; //62

                        yedge.Poly1 = edge.Poly1?.NewPoly;
                        yedge.Poly2 = edge.Poly2?.NewPoly;
                        yedge.AreaID1 = 0x3FFF;
                        yedge.AreaID2 = 0x3FFF;
                        yedge.PolyID1 = 0x3FFF;
                        yedge.PolyID2 = 0x3FFF;
                        yedge._RawData._Poly1.Unk2 = haspoly ? nonav ? 2 : 0u : 1u;//2 if nonnavigable?
                        yedge._RawData._Poly2.Unk2 = haspoly ? 0 : 1u;
                        yedge._RawData._Poly1.Unk3 = haspoly ? nonav ? 1u : edgeval : 0u;//odd if nonnavigable?   haspoly ? 42 : 0u; //TEST
                        yedge._RawData._Poly2.Unk3 = haspoly ? nonav ? 2 : 0u : 0u;//2 if nonnavigable? 4 if cell edge! - needs to be set later in ynv's
                        newedges[i] = yedge;
                        
                    }
                    ypoly.Edges = newedges;

                }
            }

            List<YnvFile> ynvs = null;
            if (vehc)
            {
                ynvs = new List<YnvFile>();
                foreach (var onv in onvlist)
                {
                    builder.PolyList.Clear();
                    foreach (var poly in onv.Polys)
                    {
                        var ypoly = poly.NewPoly;
                        builder.PolyList.Add(ypoly);
                    }
                    builder.VehicleName = onv.Name;
                    var ynv = builder.Build(true);
                    ynvs.AddRange(ynv);
                }
            }
            else
            {
                ynvs = builder.Build(false);
            }

            foreach (var ynv in ynvs)
            {
                byte[] data = ynv.Save();
                string filename = outputpath + ynv.Name + ".ynv";
                File.WriteAllBytes(filename, data);
            }


            Cursor = Cursors.Default;


            if (errorlog.Length > 0)
            {
                File.WriteAllText(outputpath + "errorlog.txt", errorlog.ToString());
                MessageBox.Show("Process complete with errors, see errorlog.txt.");
            }
            else
            {
                MessageBox.Show("Process complete.");
            }
        }


    }



    public class OnvFile
    {
        public string Name { get; set; } = string.Empty;
        public int VersionMaj { get; set; }
        public int VersionMin { get; set; }
        public Vector3 Sizes { get; set; }
        public int Flags { get; set; }
        public int VerticesCount { get; set; }
        public int IndicesCount { get; set; }
        public int EdgesCount { get; set; }
        public int PolysCount { get; set; }
        public int PortalsCount { get; set; }
        public int SectorID { get; set; }


        public List<NavMeshVertex> Vertices { get; set; } = new List<NavMeshVertex>();
        public List<int> Indices { get; set; } = new List<int>();
        public List<OnvEdge> Edges { get; set; } = new List<OnvEdge>();
        public List<OnvPoly> Polys { get; set; } = new List<OnvPoly>();
        public List<OnvPortal> Portals { get; set; } = new List<OnvPortal>();
        public OnvSector SectorTree { get; set; } = new OnvSector();

        public List<Vector3> VerticesWS { get; set; } = new List<Vector3>();

        public void Load(string onvfile)
        {
            Name = Path.GetFileNameWithoutExtension(onvfile).ToLowerInvariant();

            var lines = File.ReadAllLines(onvfile);
            bool inverts = false;
            bool ininds = false;
            bool inedges = false;
            bool inpolys = false;
            bool insectors = false;
            bool insectordata = false;
            bool insectorpolyinds = false;
            bool insectorbounds = false;
            bool inportals = false;
            int depth = 0;
            int cdepth = 0;
            OnvSector csector = SectorTree;

            var spacedelim = new[] { ' ' };
            var cult = CultureInfo.InvariantCulture;

            foreach (var line in lines)
            {
                string tline = line.Trim();
                if (string.IsNullOrEmpty(tline)) continue; //blank line
                //if (tline.StartsWith("#")) continue; //commented out

                string[] parts = tline.Split(spacedelim, StringSplitOptions.RemoveEmptyEntries);

                if (tline.StartsWith("{")) { depth++; continue; }
                if (tline.StartsWith("}")) { depth--; } //need to handle the closing cases

                if (inverts)
                {
                    if (depth <= 0) inverts = false;
                    else if (parts.Length == 3)
                    {
                        NavMeshVertex v = new NavMeshVertex();
                        v.X = ushort.Parse(parts[0].Trim(), cult);
                        v.Y = ushort.Parse(parts[1].Trim(), cult);
                        v.Z = ushort.Parse(parts[2].Trim(), cult);
                        Vertices.Add(v);
                    }
                }
                else if (ininds)
                {
                    if (depth <= 0) ininds = false;
                    else
                    {
                        for (int i = 0; i < parts.Length; i++)
                        {
                            int ind = int.Parse(parts[i]);
                            Indices.Add(ind);
                        }
                    }
                }
                else if (inedges)
                {
                    if (depth <= 0) inedges = false;
                    else
                    {
                        OnvEdge edge = new OnvEdge(parts);
                        Edges.Add(edge);
                    }
                }
                else if (inpolys)
                {
                    if (depth <= 0) inpolys = false;
                    else
                    {
                        OnvPoly poly = new OnvPoly(parts);
                        Polys.Add(poly);
                    }
                }
                else if (insectors)
                {
                    if (depth <= 0)
                    {
                        insectors = false;
                        insectordata = false;
                        insectorpolyinds = false;
                        insectorbounds = false;
                    }
                    else if (insectordata)
                    {
                        if (depth <= cdepth)
                        {
                            insectordata = false;
                            insectorpolyinds = false;
                            insectorbounds = false;
                        }
                        else if (insectorpolyinds)
                        {
                            if (depth <= (cdepth+1)) insectorpolyinds = false;
                            else
                            {
                                for (int i = 0; i < parts.Length; i++)
                                {
                                    int ind = int.Parse(parts[i]);
                                    csector.SectorData.PolyIndices.Add(ind);
                                }
                            }
                        }
                        else if (insectorbounds)
                        {
                            if (depth <= (cdepth + 1)) insectorbounds = false;
                            else
                            {
                                OnvBounds bounds = new OnvBounds(parts);
                                csector.SectorData.Bounds.Add(bounds);
                            }
                        }
                        else
                        {
                            string idstr = parts[0].Trim();
                            if (idstr == "PolyIndices")
                            {
                                csector.SectorData.PolyIndicesCount = int.Parse(parts[1].Trim(), cult);
                                insectorpolyinds = csector.SectorData.PolyIndicesCount > 0;
                                if (insectorpolyinds) csector.SectorData.PolyIndices = new List<int>();
                            }
                            else if (idstr == "Bounds")
                            {
                                csector.SectorData.BoundsCount = int.Parse(parts[1].Trim(), cult);
                                insectorbounds = csector.SectorData.BoundsCount > 0;
                                if (insectorbounds) csector.SectorData.Bounds = new List<OnvBounds>();
                            }
                        }
                    }
                    else
                    {
                        if (depth < cdepth)
                        {
                            csector = csector.Parent;
                        }
                        cdepth = depth;
                        string idstr = parts[0].Trim();
                        if (idstr == "AABBMin")
                        {
                            csector.AABBMin = Util.GetVector3(parts, 1);
                        }
                        else if (idstr == "AABBMax")
                        {
                            csector.AABBMax = Util.GetVector3(parts, 1);
                        }
                        else if ((parts.Length < 2) || (parts[1].Trim() != "null"))
                        {
                            if (idstr == "SectorData")
                            {
                                csector.SectorData = new OnvSectorData();
                                insectordata = true;
                            }
                            else if (idstr == "SubTree0")
                            {
                                csector.SubTree0 = new OnvSector();
                                csector.SubTree0.Parent = csector;
                                csector = csector.SubTree0;
                            }
                            else if (idstr == "SubTree1")
                            {
                                csector.SubTree1 = new OnvSector();
                                csector.SubTree1.Parent = csector;
                                csector = csector.SubTree1;
                            }
                            else if (idstr == "SubTree2")
                            {
                                csector.SubTree2 = new OnvSector();
                                csector.SubTree2.Parent = csector;
                                csector = csector.SubTree2;
                            }
                            else if (idstr == "SubTree3")
                            {
                                csector.SubTree3 = new OnvSector();
                                csector.SubTree3.Parent = csector;
                                csector = csector.SubTree3;
                            }
                        }
                    }
                }
                else if (inportals)
                {
                    if (depth <= 0) inportals = false;
                    else
                    {
                        OnvPortal portal = new OnvPortal(parts);
                        Portals.Add(portal);
                    }
                }
                else
                {
                    //at root level, look for identifier
                    depth = 0; //reset just in case
                    string idstr = parts[0].Trim();
                    if (idstr == "Version")
                    {
                        VersionMaj = int.Parse(parts[1].Trim(), cult);
                        VersionMin = int.Parse(parts[2].Trim(), cult);
                    }
                    else if (idstr == "Sizes")
                    {
                        Sizes = Util.GetVector3(parts, 1);
                    }
                    else if (idstr == "Flags")
                    {
                        Flags = int.Parse(parts[1].Trim(), cult);
                    }
                    else if (idstr == "Vertices")
                    {
                        VerticesCount = int.Parse(parts[1].Trim(), cult);
                        inverts = VerticesCount > 0;
                    }
                    else if (idstr == "Indices")
                    {
                        IndicesCount = int.Parse(parts[1].Trim(), cult);
                        ininds = IndicesCount > 0;
                    }
                    else if (idstr == "Edges")
                    {
                        EdgesCount = int.Parse(parts[1].Trim(), cult);
                        inedges = EdgesCount > 0;
                    }
                    else if (idstr == "Polys")
                    {
                        PolysCount = int.Parse(parts[1].Trim(), cult);
                        inpolys = PolysCount > 0;
                    }
                    else if (idstr == "SectorTree")
                    {
                        insectors = true;
                    }
                    else if (idstr == "Portals")
                    {
                        PortalsCount = int.Parse(parts[1].Trim(), cult);
                        inportals = PortalsCount > 0;
                    }
                    else if (idstr == "SectorID")
                    {
                        SectorID = int.Parse(parts[1].Trim(), cult);
                    }
                }


            }

        }


        public void InitPolys()
        {
            Vector3 posoffset = SectorTree?.AABBMin ?? Vector3.Zero;
            Vector3 aabbsize = Sizes;

            var verts = Vertices;
            for (int i = 0; i < verts.Count; i++)
            {
                var ov = verts[i].ToVector3();
                VerticesWS.Add(posoffset + ov * aabbsize);
            }

            var polys = Polys;
            for (int i = 0; i < polys.Count; i++)
            {
                var poly = polys[i];
                poly.Index = i;
                poly.Init(this);
            }


        }

        public void InitEdges(Dictionary<int, OnvFile> onvdict)
        {
            OnvFile onv;
            for (int i = 0; i < Edges.Count; i++)
            {
                var edge = Edges[i];
                if ((edge.PolyID1 >= 0) && (edge.PolyID1 < 16383) && (onvdict.TryGetValue(edge.SectorID1, out onv)))
                {
                    if (edge.PolyID1 < onv.Polys.Count)
                    {
                        edge.Poly1 = onv.Polys[edge.PolyID1];
                    }
                    else
                    { }
                }
                if ((edge.PolyID2 >= 0) && (edge.PolyID2 < 16383) && (onvdict.TryGetValue(edge.SectorID2, out onv)))
                {
                    if (edge.PolyID2 < onv.Polys.Count)
                    {
                        edge.Poly2 = onv.Polys[edge.PolyID2];
                    }
                    else
                    { }
                }
                if (edge.Poly1 != edge.Poly2)
                { }
            }

        }

    }



    public class OnvEdge
    {
        public int SectorID1 { get; set; }
        public int Flags1 { get; set; }
        public int PolyID1 { get; set; }
        public int SectorID2 { get; set; }
        public int PolyID2 { get; set; }
        public int Flags2 { get; set; }

        public OnvPoly Poly1 { get; set; }
        public OnvPoly Poly2 { get; set; }

        public OnvEdge(string[] parts)
        {
            SectorID1 = int.Parse(parts[0].Trim(), CultureInfo.InvariantCulture);
            Flags1 = int.Parse(parts[1].Trim(), CultureInfo.InvariantCulture);
            PolyID1 = int.Parse(parts[2].Trim(), CultureInfo.InvariantCulture);
            SectorID2 = int.Parse(parts[3].Trim(), CultureInfo.InvariantCulture);
            PolyID2 = int.Parse(parts[4].Trim(), CultureInfo.InvariantCulture);
            Flags2 = int.Parse(parts[5].Trim(), CultureInfo.InvariantCulture);
        }

        public override string ToString()
        {
            return SectorID1.ToString() + ", " + SectorID2.ToString() + ", " + PolyID1.ToString() + ", " + PolyID2.ToString() + ", " + Flags1.ToString() + ", " + Flags2.ToString();
        }
    }

    public class OnvPoly
    {
        public int IndexID { get; set; }
        public int IndexCount { get; set; }
        public int Flags1 { get; set; }
        public int Flags2 { get; set; }
        public int Flags3 { get; set; }

        public OnvFile Onv { get; set; }
        public int Index { get; set; }
        public int[] Indices { get; set; }
        public Vector3[] Vertices { get; set; }
        public OnvEdge[] Edges { get; set; }
        public int[] PortalLinks { get; set; }

        public YnvPoly NewPoly { get; set; }

        public OnvPoly(string[] parts)
        {
            IndexID = int.Parse(parts[0].Trim(), CultureInfo.InvariantCulture);
            IndexCount = int.Parse(parts[1].Trim(), CultureInfo.InvariantCulture);
            Flags1 = int.Parse(parts[2].Trim(), CultureInfo.InvariantCulture);
            Flags2 = int.Parse(parts[3].Trim(), CultureInfo.InvariantCulture);
            Flags3 = int.Parse(parts[4].Trim(), CultureInfo.InvariantCulture);
        }


        public void Init(OnvFile onv)
        {
            Onv = onv;
            LoadIndices();
        }


        public void LoadIndices()
        {
            //load indices, vertices and edges
            var indices = Onv.Indices;
            var vertices = Onv.VerticesWS;
            var edges = Onv.Edges;
            if ((indices == null) || (vertices == null) || (edges == null))
            { return; }
            var vc = vertices.Count;
            var ic = IndexCount;
            var startid = IndexID;
            var endid = startid + ic;
            if (startid >= indices.Count)
            { return; }
            if (endid > indices.Count)
            { return; }
            if (endid > edges.Count)
            { return; }

            Indices = new int[ic];
            Vertices = new Vector3[ic];
            Edges = new OnvEdge[ic];

            int i = 0;
            for (int id = startid; id < endid; id++)
            {
                var ind = indices[id];

                Indices[i] = ind;
                Vertices[i] = (ind < vc) ? vertices[ind] : Vector3.Zero;
                Edges[i] = edges[id];

                i++;
            }
        }



        public override string ToString()
        {
            return IndexID.ToString() + ", " + IndexCount.ToString() + ", " + Flags1.ToString() + ", " + Flags2.ToString() + ", " + Flags3.ToString();
        }
    }

    public class OnvPortal
    {
        public int Unk01 { get; set; }
        public int Unk02 { get; set; }
        public int Unk03 { get; set; }
        public int Unk04 { get; set; }
        public int Unk05 { get; set; }
        public int Unk06 { get; set; }
        public int Unk07 { get; set; }
        public int Unk08 { get; set; }
        public int Unk09 { get; set; }
        public int Unk10 { get; set; }
        public int Unk11 { get; set; }
        public int Unk12 { get; set; }

        public OnvPortal(string[] parts)
        {
            Unk01 = int.Parse(parts[0].Trim(), CultureInfo.InvariantCulture);
            Unk02 = int.Parse(parts[1].Trim(), CultureInfo.InvariantCulture);
            Unk03 = int.Parse(parts[2].Trim(), CultureInfo.InvariantCulture);
            Unk04 = int.Parse(parts[3].Trim(), CultureInfo.InvariantCulture);
            Unk05 = int.Parse(parts[4].Trim(), CultureInfo.InvariantCulture);
            Unk06 = int.Parse(parts[5].Trim(), CultureInfo.InvariantCulture);
            Unk07 = int.Parse(parts[6].Trim(), CultureInfo.InvariantCulture);
            Unk08 = int.Parse(parts[7].Trim(), CultureInfo.InvariantCulture);
            Unk09 = int.Parse(parts[8].Trim(), CultureInfo.InvariantCulture);
            Unk10 = int.Parse(parts[9].Trim(), CultureInfo.InvariantCulture);
            Unk11 = int.Parse(parts[10].Trim(), CultureInfo.InvariantCulture);
            Unk12 = int.Parse(parts[11].Trim(), CultureInfo.InvariantCulture);
        }

        public override string ToString()
        {
            return 
                Unk01.ToString() + ", " +
                Unk02.ToString() + ", " +
                Unk03.ToString() + ", " +
                Unk04.ToString() + ", " +
                Unk05.ToString() + ", " +
                Unk06.ToString() + ", " +
                Unk07.ToString() + ", " +
                Unk08.ToString() + ", " +
                Unk09.ToString() + ", " +
                Unk10.ToString() + ", " +
                Unk11.ToString() + ", " +
                Unk12.ToString();
        }
    }


    public class OnvSector
    {
        public Vector3 AABBMin { get; set; }
        public Vector3 AABBMax { get; set; }
        public OnvSectorData SectorData { get; set; }
        public OnvSector SubTree0 { get; set; }
        public OnvSector SubTree1 { get; set; }
        public OnvSector SubTree2 { get; set; }
        public OnvSector SubTree3 { get; set; }

        public OnvSector Parent { get; set; }
    }

    public class OnvSectorData
    {
        public int PolyIndicesCount { get; set; }
        public List<int> PolyIndices { get; set; } = new List<int>();
        public int BoundsCount { get; set; }
        public List<OnvBounds> Bounds { get; set; } = new List<OnvBounds>();
    }

    public class OnvBounds
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }
        public int W { get; set; }

        public OnvBounds(string[] parts)
        {
            X = int.Parse(parts[0].Trim(), CultureInfo.InvariantCulture);
            Y = int.Parse(parts[1].Trim(), CultureInfo.InvariantCulture);
            Z = int.Parse(parts[2].Trim(), CultureInfo.InvariantCulture);
            W = int.Parse(parts[3].Trim(), CultureInfo.InvariantCulture);
        }

        public override string ToString()
        {
            return X.ToString() + ", " + Y.ToString() + ", " + Z.ToString() + ", " + W.ToString();
        }
    }



    public static class Util
    {
        public static Vector3 GetVector3(string[] parts, int i)
        {
            float x = float.Parse(parts[i+0].Trim(), CultureInfo.InvariantCulture);
            float y = float.Parse(parts[i+1].Trim(), CultureInfo.InvariantCulture);
            float z = float.Parse(parts[i+2].Trim(), CultureInfo.InvariantCulture);
            return new Vector3(x, y, z);
        }
    }

}
