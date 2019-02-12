﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ExplicitCorridorMap
{
    public class Geometry
    {
        public static bool PolygonContainsPoint(List<Vector2> polyPoints  , Vector2 p )  { 
           var j = polyPoints.Count - 1;
           var inside = false; 
           for (int i = 0; i<polyPoints.Count; j = i++) { 
              if (((polyPoints[i].y <= p.y && p.y<polyPoints[j].y) || (polyPoints[j].y <= p.y && p.y<polyPoints[i].y)) && 
                 (p.x<(polyPoints[j].x - polyPoints[i].x) * (p.y - polyPoints[i].y) / (polyPoints[j].y - polyPoints[i].y) + polyPoints[i].x)) 
                 inside = !inside; 
           } 
           return inside; 
        }
        public static bool PolygonContainsPoint(Vector2 polyPoint1, Vector2 polyPoint2, Vector2 polyPoint3, Vector2 p)
        {
            var polyPoints = new List<Vector2> { polyPoint1, polyPoint2, polyPoint3 };
            var j = polyPoints.Count - 1;
            var inside = false;
            for (int i = 0; i < polyPoints.Count; j = i++)
            {
                if (((polyPoints[i].y <= p.y && p.y < polyPoints[j].y) || (polyPoints[j].y <= p.y && p.y < polyPoints[i].y)) &&
                   (p.x < (polyPoints[j].x - polyPoints[i].x) * (p.y - polyPoints[i].y) / (polyPoints[j].y - polyPoints[i].y) + polyPoints[i].x))
                    inside = !inside;
            }
            return inside;
        }
    }
}