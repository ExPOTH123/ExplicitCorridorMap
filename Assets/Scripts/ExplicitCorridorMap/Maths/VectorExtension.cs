﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
public static class VectorExtension
{

    public static bool Approximately(this Vector2 a, Vector2 b)
    {
        return Mathf.Approximately(a.x, b.x) && Mathf.Approximately(a.y, b.y);
    }
    public static bool Approximately(this Vector2 a, Vector2 b, float distance)
    {
        return Mathf.Abs(a.x-b.x) <=distance && Mathf.Abs(a.y- b.y) <= distance;
    }
    public static Vector2 To2D(this Vector3 a)
    {
        return new Vector2(a.x, a.z);
    }
    public static RVO.Vector2 To2DRVO(this Vector3 a)
    {
        return new RVO.Vector2(a.x, a.z);
    }
    public static RVO.Vector2 To2DRVO(this Vector2 a)
    {
        return new RVO.Vector2(a.x, a.y);
    }
    public static Vector2 RVOVector2ToVector2(this RVO.Vector2 vector2)
    {
        return new Vector2(vector2.x_, vector2.y_);
    }
}
