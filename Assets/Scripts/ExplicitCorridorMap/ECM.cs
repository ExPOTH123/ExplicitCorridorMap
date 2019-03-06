﻿using ExplicitCorridorMap.Maths;
using ExplicitCorridorMap.Voronoi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using KdTree;
using KdTree.Math;
using RBush;

namespace ExplicitCorridorMap
{
    public class ECM : ECMCore
    {
        private KdTree<float, Vertex> KdTree { get; }

        public ECM(List<Obstacle> obstacles) : base(obstacles)
        {
            KdTree = new KdTree<float, Vertex>(2, new FloatMath());
        }
        public override void ConstructTree()
        {
            base.ConstructTree();
            //contruct kdtree
            foreach (var v in Vertices.Values)
            {
                KdTree.Add(v.KDKey, v);
            }
        }
        public Edge GetNearestEdge(Vector2 point)
        {
            var edges = RTree.Search(new Envelope(point.x, point.y, point.x, point.y));
            foreach (var edge in edges)
            {
                if (Geometry.PolygonContainsPoint(edge.Cell, point)) return edge;
            }
            return GetNearestVertex(point).Edges[0];
        }
        public Vertex GetNearestVertex(Vector2 point)
        {
            var pointArray = new float[] { point.x, point.y };
            var nodes = KdTree.GetNearestNeighbours(pointArray, 1);
            return nodes[0].Value;
        }

        public override void AddSegment(Segment s)
        {
            s.ID = InputSegments.Count;
            InputSegments[InputSegments.Count] = s;
        }
        public void AddBorder(RectInt rect)
        {
            AddSegment(new Segment(rect.x, rect.y, rect.x, rect.yMax));
            AddSegment(new Segment(rect.x, rect.yMax, rect.xMax, rect.yMax));
            AddSegment(new Segment(rect.xMax, rect.yMax, rect.xMax, rect.y));
            AddSegment(new Segment(rect.xMax, rect.y, rect.x, rect.y));
        }
        public void DeleteEdge(Edge e)
        {
            e.Start.Edges.Remove(e);
            if (e.Start.Edges.Count == 0) DeleteVertex(e.Start);
            RTree.Delete(e);
            Edges.Remove(e.ID);
        }
        public void DeleteVertex(Vertex v)
        {
            Vertices.Remove(v.ID);
            KdTree.RemoveAt(v.KDKey);
        }
        public void AddEdge(Edge e)
        {
            e.ID = CountEdges++;
            Edges[e.ID] = e;
            RTree.Insert(e);
        }
        public void AddVertex(Vertex v)
        {
            v.ID = CountVertices++;
            Vertices[v.ID] = v;
            KdTree.Add(v.KDKey, v);
        }
        public void AddObstacle(Obstacle obs)
        {
            AddSegment(obs.Segments);
            Obstacles.Add(obs);
            RTreeObstacle.Insert(obs);
        }
        public void CheckOldVertex(Vertex v)
        {
            var node = KdTree.GetNearestNeighbours(v.KDKey, 1);
            if (node[0].Value.Position == v.Position)
            {
                //update this vertex
                v.OldVertex = node[0].Value;
            }
        }
        public void AddPolygonDynamic(Obstacle newObstacle)
        {

            var selectedEdges = RTree.Search(newObstacle.Envelope);
            //enlarge obstacle
            float maxSquareClearance = 0;
            foreach (var e in selectedEdges)
            {
                var d1 = PathFinding.HeuristicCost(e.Start.Position, e.LeftObstacleOfStart);
                var d2 = PathFinding.HeuristicCost(e.End.Position, e.LeftObstacleOfEnd);
                if (d1 > maxSquareClearance) maxSquareClearance = d1;
                if (d2 > maxSquareClearance) maxSquareClearance = d2;
            }
            var maxClearance = Mathf.Sqrt(maxSquareClearance);
            var extendedEnvelope = Geometry.ExtendEnvelope(newObstacle.Envelope, maxClearance);

            //search again with extended envelope, find all obstacle and segment involves
            selectedEdges = RTree.Search(extendedEnvelope);
            var obstacleSet = new HashSet<Obstacle>();
            var segmentSet = new HashSet<Segment>();
            foreach (var e in selectedEdges)
            {
                var seg = RetrieveInputSegment(e);
                if (seg.Parent != null) obstacleSet.Add(seg.Parent);
                else segmentSet.Add(seg);
                seg = RetrieveInputSegment(e.Twin);
                if (seg.Parent != null) obstacleSet.Add(seg.Parent);
                else segmentSet.Add(seg);
                DeleteEdge(e);
                DeleteEdge(e.Twin);
            }
            var obstacleList = obstacleSet.ToList();
            obstacleList.Add(newObstacle);
            this.AddObstacle(newObstacle);
            var segmentList = segmentSet.ToList();

            //contruct new ECM
            var newECM = new ECMCore(obstacleList);
            foreach (var s in segmentList) newECM.AddSegment(s);
            newECM.Construct();
            var newEdges = newECM.RTree.Search(extendedEnvelope);
            foreach(var v in newECM.Vertices.Values)
            {
                CheckOldVertex(v);
            }
            var newVertices = new HashSet<Vertex>();
            foreach (var e in newEdges)
            {
                UpdateEdge(e, newVertices);
                UpdateEdge(e.Twin, newVertices);
                e.SiteID = newECM.RetrieveInputSegment(e).ID;
                e.Twin.SiteID = newECM.RetrieveInputSegment(e.Twin).ID;
            }
            foreach (var v in newVertices)
            {
                AddVertex(v);
            }
            foreach (var e in newEdges)
            {
                AddEdge(e);
                AddEdge(e.Twin);
            }

            foreach (var v in Vertices.Values)
            {
                Debug.Log(v.ID + "   " + string.Join(",", v.Edges));
            }
            //Debug.Log("selected edge");
            //foreach (var e in selectedEdges)
            //{
            //    Debug.Log(e);
            //}
        }
        public void UpdateEdge(Edge e, HashSet<Vertex> newVertices)
        {
            if (e.Start.OldVertex != null)
            {
                e.Start = e.Start.OldVertex;
                e.Start.Edges.Add(e);
            }
            else newVertices.Add(e.Start);
            if (e.End.OldVertex != null)
            {
                e.End = e.End.OldVertex;
                e.End.Edges.Add(e);
            }
            else newVertices.Add(e.End);
        } 

        /// <summary>
        /// Generate a polyline representing a curved edge.
        /// </summary>
        /// <param name="edge">The curvy edge.</param>
        /// <param name="max_distance">The maximum distance between two vertex on the output polyline.</param>
        /// <returns></returns>
        public List<Vector2> SampleCurvedEdge(Edge edge, float max_distance)
        {
            Edge pointCell = null;
            Edge lineCell = null;

            //Max distance to be refined
            if (max_distance <= 0)
                throw new Exception("Max distance must be greater than 0");

            Vector2Int pointSite;
            Segment segmentSite;

            Edge twin = edge.Twin;

            if (edge.ContainsSegment == true && twin.ContainsSegment == true)
                return new List<Vector2>() { edge.Start.Position, edge.End.Position };

            if (edge.ContainsPoint)
            {
                pointCell = edge;
                lineCell = twin;
            }
            else
            {
                lineCell = edge;
                pointCell = twin;
            }

            pointSite = RetrieveInputPoint(pointCell);
            segmentSite = RetrieveInputSegment(lineCell);

            List<Vector2> discretization = new List<Vector2>(){
                edge.Start.Position,
                edge.End.Position
            };

            if (edge.IsLinear)
                return discretization;


            return ParabolaComputation.Densify(
                new Vector2(pointSite.x, pointSite.y),
                new Vector2(segmentSite.Start.x, segmentSite.Start.y),
                new Vector2(segmentSite.End.x, segmentSite.End.y),
                discretization[0],
                discretization[1],
                max_distance,
                0
            );
        }
    }
}
