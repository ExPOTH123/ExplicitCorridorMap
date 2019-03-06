﻿using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ExplicitCorridorMap
{
    public class PathFinding
    {
        //Debug Purpose, Draw portal
        public static List<Vector2> FindPathDebug(ECM ecm, Vector2 startPosition, Vector2 endPosition, out List<Vector2> portalsLeft, out List<Vector2> portalsRight)
        {
            var startEdge = ecm.GetNearestEdge(startPosition);
            var endEdge = ecm.GetNearestEdge(endPosition);

            var edgeList = FindEdgePathFromVertexToVertex(ecm, startEdge.Start, startEdge.End, endEdge.Start, endEdge.End, startPosition, endPosition);

            //foreach (var edge in edgeList)
            //{
            //    Debug.Log(edge);
            //}
            ComputePortals(edgeList, startPosition, endPosition, out portalsLeft, out portalsRight);
            return GetShortestPath(portalsLeft, portalsRight);
        }
        public static List<Vector2> FindPath(ECM ecm,Vector2 startPosition, Vector2 endPosition)
        {
            var startEdge = ecm.GetNearestEdge(startPosition);
            var endEdge = ecm.GetNearestEdge(endPosition);

            var edgeList = FindEdgePathFromVertexToVertex(ecm, startEdge.Start,startEdge.End, endEdge.Start,endEdge.End,startPosition,endPosition);
            ComputePortals(edgeList, startPosition, endPosition, out List<Vector2> portalsLeft, out List<Vector2> portalsRight);
            return GetShortestPath(portalsLeft, portalsRight);
        }
        
        private static List<Edge> FindEdgePathFromVertexToVertex(ECM graph, Vertex start1, Vertex start2, Vertex end1, Vertex end2, Vector2 startPosition, Vector2 endPosition)
        {
            var path = PathFinding.FindPathFromVertexToVertex(graph, start1, start2, end1,end2,startPosition,endPosition);
            var edgeList = new List<Edge>();
            
            for (int i = path.Count - 1; i > 0; i--)
            {
                foreach( var edge in path[i].Edges)
                {
                    var end = edge.End;
                    if (end.Equals(path[i - 1])) edgeList.Add(edge);
                }
            }
            return edgeList;

        }
        //Simple Astar Algorithm
        //start1,start2 are two vertex of start edge
        //end1,end2 are two vertex of end edge
        private static List<Vertex> FindPathFromVertexToVertex(ECM graph, Vertex start1, Vertex start2, Vertex end1, Vertex end2, Vector2 startPosition, Vector2 endPosition)
        {
            var openSet = new HashSet<Vertex>();
            openSet.Add(start1);
            openSet.Add(start2);
            var closeSet = new HashSet<Vertex>();
            var cameFrom = new Dictionary<Vertex, Vertex>();
            var gScore = new Dictionary<Vertex, float>();
            var fScore = new Dictionary<Vertex, float>();
            gScore[start1] = HeuristicCost(startPosition, start1.Position);
            fScore[start1] = HeuristicCost(start1.Position, endPosition);
            gScore[start2] = HeuristicCost(startPosition, start2.Position);
            fScore[start2] = HeuristicCost(start2.Position, endPosition);

            List<Vertex> result = new List<Vertex>();
            while (openSet.Count != 0)
            {
                var current = LowestFScore(openSet, fScore);
                if (current.Equals(end1)||current.Equals(end2)) return RecontructPath(cameFrom, current);
                openSet.Remove(current);
                closeSet.Add(current);

                foreach(var edge in current.Edges)
                {
                    var neigborVertex = edge.End;
                    if (closeSet.Contains(neigborVertex)) continue;
                    var tentativeGScore = gScore[current] + HeuristicCost(current, neigborVertex);
                    if (!openSet.Contains(neigborVertex)) openSet.Add(neigborVertex);
                    else if (tentativeGScore >= gScore[neigborVertex]) continue;
                    cameFrom[neigborVertex] = current;
                    gScore[neigborVertex] = tentativeGScore;
                    fScore[neigborVertex] = tentativeGScore + HeuristicCost(neigborVertex.Position, endPosition);
                }
            }
            return result;

        }
        private static List<Vertex> RecontructPath(Dictionary<Vertex, Vertex> cameFrom, Vertex current)
        {
            var totalPath = new List<Vertex>();
            totalPath.Add(current);
            while (cameFrom.ContainsKey(current))
            {
                current = cameFrom[current];
                totalPath.Add(current);
            }
            return totalPath;
        }
        public static float HeuristicCost(Vertex start, Vertex goal)
        {
            return HeuristicCost(start.Position, goal.Position);
        }
        public static float HeuristicCost(Vector2 start, Vector2 goal)
        {
            var dx = start.x - goal.x;
            var dy = start.y - goal.y;
            var h = dx * dx + dy * dy;
            return h;
        }
        private static Vertex LowestFScore(HashSet<Vertex> hashSet, Dictionary<Vertex, float> fScore)
        {
            float min = float.MaxValue;
            Vertex result = null;
            foreach (var v in hashSet)
            {
                var f = fScore[v];
                if (f < min)
                {
                    min = f;
                    result = v;
                }
            }
            return result;
        }
        private static float CrossProduct(Vector2 a, Vector2 b, Vector2 c)
        {
            float ax = b.x - a.x;
            float ay = b.y - a.y;
            float bx = c.x - a.x;
            float by = c.y - a.y;
            return bx * ay - ax * by;
        }

        //Simple Stupid Funnel Algorithm
        private static List<Vector2> GetShortestPath(List<Vector2> portalsLeft, List<Vector2> portalsRight)
        {
            List<Vector2> path = new List<Vector2>();
            if (portalsLeft.Count == 0) return path;
            Vector2 portalApex, portalLeft, portalRight;
            int apexIndex = 0, leftIndex = 0, rightIndex = 0;
            portalApex = portalsLeft[0];
            portalLeft = portalsLeft[0];
            portalRight = portalsRight[0];
            AddToPath(path,portalApex);

            var left1 = portalsLeft[1];
            var right1 = portalsRight[1];
            //heuristic
            if (HeuristicCost(portalApex, left1) > HeuristicCost(portalApex, right1))
            {
                for (int i = 1; i < portalsLeft.Count; i++)
                {
                    var left = portalsLeft[i];
                    var right = portalsRight[i];
                    // Update left vertex.
                    if (CrossProduct(portalApex, portalLeft, left) >= 0.0f)
                    {
                        if (portalApex.Equals(portalLeft) || CrossProduct(portalApex, portalRight, left) < 0.0f)
                        {
                            // Tighten the funnel.
                            portalLeft = left;
                            leftIndex = i;
                        }
                        else
                        {
                            // Left over right, insert right to path and restart scan from portal right point.
                            AddToPath(path,portalRight);
                            // Make current right the new apex.
                            portalApex = portalRight;
                            apexIndex = rightIndex;
                            // Reset portal
                            portalLeft = portalApex;
                            portalRight = portalApex;
                            leftIndex = apexIndex;
                            rightIndex = apexIndex;
                            // Restart scan
                            i = apexIndex;
                            continue;
                        }
                    }//if
                    // Update right vertex.
                    if (CrossProduct(portalApex, portalRight, right) <= 0.0f)
                    {
                        if (portalApex.Equals(portalRight) || CrossProduct(portalApex, portalLeft, right) > 0.0f)
                        {
                            // Tighten the funnel.
                            portalRight = right;
                            rightIndex = i;
                        }
                        else
                        {
                            AddToPath(path,portalLeft);
                            // Make current left the new apex.
                            portalApex = portalLeft;
                            apexIndex = leftIndex;
                            // Reset portal
                            portalLeft = portalApex;
                            portalRight = portalApex;
                            leftIndex = apexIndex;
                            rightIndex = apexIndex;
                            // Restart scan
                            i = apexIndex;
                            continue;
                        }
                    }
                    
                }//for
            }
            else
            {
                for (int i = 1; i < portalsLeft.Count; i++)
                {
                    var left = portalsLeft[i];
                    var right = portalsRight[i];
                    // Update right vertex.
                    if (CrossProduct(portalApex, portalRight, right) <= 0.0f)
                    {
                        if (portalApex.Equals(portalRight) || CrossProduct(portalApex, portalLeft, right) > 0.0f)
                        {
                            // Tighten the funnel.
                            portalRight = right;
                            rightIndex = i;
                        }
                        else
                        {
                            AddToPath(path,portalLeft);
                            // Make current left the new apex.
                            portalApex = portalLeft;
                            apexIndex = leftIndex;
                            // Reset portal
                            portalLeft = portalApex;
                            portalRight = portalApex;
                            leftIndex = apexIndex;
                            rightIndex = apexIndex;
                            // Restart scan
                            i = apexIndex;
                            continue;
                        }
                    }
                    // Update left vertex.
                    if (CrossProduct(portalApex, portalLeft, left) >= 0.0f)
                    {
                        if (portalApex.Equals(portalLeft) || CrossProduct(portalApex, portalRight, left) < 0.0f)
                        {
                            // Tighten the funnel.
                            portalLeft = left;
                            leftIndex = i;
                        }
                        else
                        {
                            // Left over right, insert right to path and restart scan from portal right point.
                            AddToPath(path,portalRight);
                            // Make current right the new apex.
                            portalApex = portalRight;
                            apexIndex = rightIndex;
                            // Reset portal
                            portalLeft = portalApex;
                            portalRight = portalApex;
                            leftIndex = apexIndex;
                            rightIndex = apexIndex;
                            // Restart scan
                            i = apexIndex;
                            continue;
                        }
                    }//if
                }//for
            }
            AddToPath(path,portalsLeft[portalsLeft.Count - 1]);
            return path;
        }//funtion
        private static void ComputePortals(List<Edge> edgeList, Vector2 startPosition, Vector2 endPosition, out List<Vector2> portalsLeft, out List<Vector2> portalsRight)
        {
            portalsLeft = new List<Vector2>();
            portalsRight = new List<Vector2>();
            portalsLeft.Add(startPosition);
            portalsRight.Add(startPosition);
            //heuristic
            bool containsStart = true;
            bool containsEnd = true;
            int start = 0;
            int end = edgeList.Count - 1;
            while (start<=end)
            {
                var edge = edgeList[start];
                containsStart = Geometry.PolygonContainsPoint(edge.Start.Position, edge.LeftObstacleOfStart, edge.RightObstacleOfStart, startPosition);
                if (containsStart) start++;
                else break;
            }
            while (end>=start)
            {
                var edge = edgeList[end];
                containsEnd = Geometry.PolygonContainsPoint(edge.Start.Position, edge.LeftObstacleOfStart, edge.RightObstacleOfStart, endPosition);
                if (containsEnd) end--;
                else break;
            }
            for (int i = start; i <= end; i++)
            {
                var edge = edgeList[i];
                if (portalsLeft.Count != 0 &&
                    edge.LeftObstacleOfStart == portalsLeft[portalsLeft.Count - 1] &&
                    edge.RightObstacleOfStart == portalsRight[portalsRight.Count - 1]) continue;
                portalsLeft.Add(edge.LeftObstacleOfStart);
                portalsRight.Add(edge.RightObstacleOfStart);
            }
            //add last end portal
            if (end >= start)
            {
                var endEdge = edgeList[end];
                containsEnd = Geometry.PolygonContainsPoint(endEdge.End.Position, endEdge.LeftObstacleOfEnd, endEdge.RightObstacleOfEnd, endPosition);
                if (!containsEnd)
                {
                    portalsLeft.Add(endEdge.LeftObstacleOfEnd);
                    portalsRight.Add(endEdge.RightObstacleOfEnd);
                }
            }
            portalsLeft.Add(endPosition);
            portalsRight.Add(endPosition);
        }
        private static void AddToPath(List<Vector2> path, Vector2 point)
        {
            if(path.Count != 0 && point == path[path.Count - 1])
            {
                return;
            }
            else
            {
                path.Add(point);
            }
        }
    }
}