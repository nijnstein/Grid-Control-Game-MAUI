using Microsoft.Maui.Animations;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace NSS
{
    public static class MathEx
    {
        static public RectF CalculateBoundingRectangle(PointF[] points)
        {
            if(points.Length == 0)
            {
                return RectF.Zero; 
            }

            float x1 = points[0].X, y1 = points[0].Y;
            float x2 = x1;  
            float y2 = y1;

            for(int i = 1;i < points.Length; i++)
            {
                PointF p = points[i];
                x1 = MathF.Min(x1, p.X);
                x2 = MathF.Max(x2, p.X);
                y1 = MathF.Min(y1, p.Y);
                y2 = MathF.Max(y2, p.Y);
            }

            return new RectF(x1, y1, x2 - x1, y2 - y1); 
        }

        public static PointF CalculateCentroid(PointF[] points)
        {
            PointF off = points[0];
            float twicearea = 0;
            float x = 0;
            float y = 0;
            PointF p1, p2;
            float f;
            for (int i = 0, j = points.Length - 1; i < points.Length; j = i++)
            {
                p1 = points[i];
                p2 = points[j];
                f = (p1.X - off.X) * (p2.Y - off.Y) - (p2.X - off.X) * (p1.Y - off.Y);
                twicearea += f;
                x += (p1.X + p2.X - 2 * off.X) * f;
                y += (p1.Y + p2.Y - 2 * off.Y) * f;
            }

            f = twicearea * 3;
            return new PointF(x / f + off.X, y / f + off.Y);
        }


        static public float CalculateSurfaceArea(PointF[] points)
        {
            if (points == null || points.Length < 3)
            {
                return 0;
            }

            float area = 0; 
            for (int i = 0, j = points.Length - 1; i < points.Length; ++i)
            {
                area += (points[j].X + points[i].X) * (points[j].Y - points[i].Y);
                j = i;
            }
            return area * 0.5f;
        }

        public static PointF NormalizePoint(PointF p)
        {
            float h = MathF.Sqrt(p.X * p.X + p.Y * p.Y);
            if (h < 0.0001)
            {
                return PointF.Zero;
            }
            float r = 1 / h;
            return new PointF(p.X * r, p.Y * r);
        }

        public static PointF[] OffsetPolygon(PointF[] poly, float offset, int outer_ccw)
        {
#if DEBUG
            // verify polygon is closed 
            if (poly.Length > 1)
            {
                if (poly[0] != poly[poly.Length - 1])
                {
                    throw new Exception("polygon should be closed");
                }
            }
#endif  

            if (offset == 0.0 || poly.Length < 3)
            {
                return poly;
            }
            List<PointF> newPoly = new List<PointF>();

            PointF vnn, vpn, bisn;
            float vnX, vnY, vpX, vpY;
            float nnnX, nnnY;
            float npnX, npnY;
            float bisX, bisY, bisLen;

            int nVerts = poly.Length - 1;

            for (int i = 0; i < poly.Length; i++)
            {
                int prev = (i + nVerts - 1) % nVerts;
                int next = (i + 1) % nVerts;

                vnX = poly[next].X - poly[i].X;
                vnY = poly[next].Y - poly[i].Y;
                vnn = NormalizePoint(new PointF(vnX, vnY));
                nnnX = vnn.Y;
                nnnY = -vnn.X;

                vpX = poly[i].X - poly[prev].X;
                vpY = poly[i].Y - poly[prev].Y;
                vpn = NormalizePoint(new PointF(vpX, vpY));
                npnX = vpn.Y * outer_ccw;
                npnY = -vpn.X * outer_ccw;

                bisX = (nnnX + npnX) * outer_ccw;
                bisY = (nnnY + npnY) * outer_ccw;

                bisn = NormalizePoint(new PointF(bisX, bisY));
                bisLen = offset / MathF.Sqrt((1 + nnnX * npnX + nnnY * npnY) / 2);

                newPoly.Add(new PointF(poly[i].X + bisLen * bisn.X, poly[i].Y + bisLen * bisn.Y));
            }

            return newPoly.ToArray();
        }


        /// <summary>
        /// See "The Point in Polygon Problem for Arbitrary Polygons" by Hormann & Agathos
        /// http://citeseerx.ist.psu.edu/viewdoc/download?doi=10.1.1.88.5498&rep=rep1&type=pdf
        /// returns 0 if false, +1 if true, -1 if pt ON polygon boundary
        /// refactored clipper code to use floats and pointf 
        /// </summary>
        /// <param name="points"></param>
        /// <param name="pt"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static int PointInPolygon(PointF[] points, PointF pt)
        {
#if DEBUG
            if(points == null || points.Length < 3 || points[0] != points[points.Length - 1])
            {
                throw new Exception("point list does not contain a valid closed polygon");
            }
#endif 

            int result = 0;

            float ptx = pt.X;
            float pty = pt.Y;
            float poly0x = points[0].X; 
            float poly0y = points[0].Y;

            for(int i = 1; i < points.Length; i++)
            {
                PointF op = points[i];

                float poly1x = op.X;
                float poly1y = op.Y;

                if (poly1y == pty)
                {
                    if ((poly1x == ptx) || (poly0y == pty && ((poly1x > ptx) == (poly0x < ptx))))
                    {
                        return -1;
                    }
                }
                if ((poly0y < pty) != (poly1y < pty))
                {
                    if (poly0x >= ptx)
                    {
                        if (poly1x > ptx)
                        {
                            result = 1 - result;
                        }
                        else
                        {
                            float d = (float)(poly0x - ptx) * (poly1y - pty) - (float)(poly1x - ptx) * (poly0y - pty);

                            if (d == 0)
                            {
                                return -1;
                            }
                            if ((d > 0) == (poly1y > poly0y))
                            {
                                result = 1 - result;
                            }
                        }
                    }
                    else
                    {
                        if (poly1x > ptx)
                        {
                            float d = (float)(poly0x - ptx) * (poly1y - pty) - (float)(poly1x - ptx) * (poly0y - pty);
                            if (d == 0)
                            {
                                return -1;
                            }
                            if ((d > 0) == (poly1y > poly0y))
                            {
                                result = 1 - result;
                            }
                        }
                    }
                }
                poly0x = poly1x;
                poly0y = poly1y;
            } 
            return result;
        }


        /// <summary>
        /// Check if 2 line segments are intersecting in 2d space
        /// p1 and p2 belong to line 1, p3 and p4 belong to line 2
        /// </summary>
        public static bool LinesIntersect(PointF p1, PointF p2, PointF p3, PointF p4)
        {
            float denominator = (p4.Y - p3.Y) * (p2.X - p1.X) - (p4.X - p3.X) * (p2.Y - p1.Y);

            // make sure the denominator is != 0, if 0 the lines are parallel
            if (denominator != 0)
            {
                // most common case = not parallel 
                float u_a = ((p4.X - p3.X) * (p1.Y - p3.Y) - (p4.Y - p3.Y) * (p1.X - p3.X)) / denominator;
                float u_b = ((p2.X - p1.X) * (p1.Y - p3.Y) - (p2.Y - p1.Y) * (p1.X - p3.X)) / denominator;

                //Is intersecting if u_a and u_b are between 0 and 1
                if (u_a >= 0 && u_a <= 1 && u_b >= 0 && u_b <= 1)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 
        /// intersect lines through points (not linesegments) 
        /// 
        /// line 1 through = p1, p2
        /// line 2 through = p3, p4 
        /// 
        /// line = (x1, y1) and (x2, y2)
        ///         A = y2-y1
        ///         B = x1 - x2
        ///         C = Ax1+By1
        /// 
        ///  A1x + B1y = C1
        ///  A2x + B2y = C2
        ///         
        ///  so d = A1 * B2 - A2 * B1
        ///  and (x,y) = ((B2 * C1 - B1 * C2) / d, (A1 * C2 - A2 * C1) / d)
        /// 
        /// </summary>
        public static bool TryGetIntersection(PointF p1, PointF p2, PointF p3, PointF p4, out PointF intersection)
        {
            float a1 = p2.Y - p1.Y;
            float a2 = p4.Y - p3.Y;

            float b1 = p2.X - p1.X;
            float b2 = p4.X - p3.X;

            float d = a1 * b2 - a2 * b1;
            if (d == 0)
            {
                intersection = PointF.Zero;
                return false;
            }

            float c1 = a1 * p1.X + b1 * p1.Y;
            float c2 = a2 * p3.X + b2 * p3.Y;

            intersection = new PointF((b2 * c1 - b1 * c2) / d, (a1 * c2 - a2 * c1) / d);
            return true;
        }

        /// <summary>
        /// intersect lines that only run at 90 degree angles in an integral grid (only horizontal or vertical) 
        /// </summary>
        public static bool TryIntersectLineSegments90(PointF p1, PointF p2, PointF p3, PointF p4, out PointF intersection)
        {
            float x1 = Math.Min(p1.X, p2.X);
            float x2 = Math.Max(p1.X, p2.X);
            float y1 = Math.Min(p1.Y, p2.Y);
            float y2 = Math.Max(p1.Y, p2.Y);
            float x3 = Math.Min(p3.X, p4.X);
            float x4 = Math.Max(p3.X, p4.X);
            float y3 = Math.Min(p3.Y, p4.Y);
            float y4 = Math.Max(p3.Y, p4.Y);

            if (x4 < x1 | x3 > x2 | y4 < y1 | y3 > y2)
            {
                intersection = PointF.Zero;
                return false;
            }
            else
            {
                if(x1 == x2)
                {
                    intersection = new PointF(x1, y3);
                    return true;
                }
                if(y1 == y2)
                {
                    intersection = new PointF(x3, y1); 
                    return true; 
                }

                throw new Exception("error in intersection..."); 
            }
        }

        /// <summary>
        /// get intersection of 2 line sections 
        /// 
        /// p1-p2 / p3-p4
        /// a-b / c-d 
        /// 
        ///  E = B-A = ( Bx-Ax, By-Ay )
        ///  F = D-C = (Dx-Cx, Dy-Cy ) 
        ///  P = ( -Ey, Ex )
        ///  h = ((A-C) * P ) / (F* P )
        ///  
        ///  point of intersection is C + F*h
        /// </summary>
        /// <returns></returns>

        public static bool TryGetSegmentIntersection(PointF p1, PointF p2, PointF p3, PointF p4, out PointF intersection, bool includeEndPoints = false)
        {
            float ex = p2.X - p1.X;
            float ey = p2.Y - p1.Y;

            float fx = p4.X - p3.X;
            float fy = p4.Y - p3.Y;

            if (ex == 0 & ey == 0 | fx == 0 & fy == 0)
            {
                intersection = PointF.Zero;
                return false;
            }
                                    
            float px = -ey;
            float py = ex;

            float ax = p1.X - p3.X;
            float ay = p1.Y - p3.Y;

            // h == how much to multiply the lenght of the line to touch the other line
            float h = (ax * px + ay * py) / (fx * px + fy * py);

            // if h < 0 then line p3-p4 is behind p1-p2
            // if h > 1 then its in front
            // if h == 0 or h == 1 then it intersects an endpoint
            if (includeEndPoints)
            {
                if (h < 0 | h > 1)
                {
                    intersection = PointF.Zero;
                    return false;
                }
            }
            else
            {
                // if h is exactly 1 or 0 its on the endpoint of a line segment 
                if (h <= 0 | h >= 1)
                {
                    intersection = PointF.Zero;
                    return false;
                }
            }

            float cx = p3.X - p1.X;
            float cy = p3.Y - p1.Y;

            // same as h but from p3.p4 - p1.p2
            px = -fy;
            py = fx;

            float g = (cx * px + cy * py) / (ex * px + ey * py);
            if (includeEndPoints)
            {
                if (g < 0 | g > 1)
                {
                    intersection = PointF.Zero;
                    return false;
                }
            }
            else
            {
                if (g <= 0 | g >= 1)
                {
                    intersection = PointF.Zero;
                    return false;
                }
            }

            // intersection at C + F*h
            intersection = new PointF(p3.X + fx * h, p3.Y + fy * h);
            return true;
        }

        public static PointF ClosestPointOnLine(PointF p1, PointF p2, PointF position, bool restrictToLineSegment = true)
        {
            PointF ba = new PointF(p2.X - p1.X, p2.Y - p1.Y);
            PointF ap = new PointF(position.X - p1.X, position.Y - p1.Y);

            float t = Dot(ap, ba) / Dot(ba, ba);

            if (restrictToLineSegment)
            {
                // t = clamp01(t)
                t = t < 0 ? 0 : t > 1 ? 1 : t;
            }
            return p1.Lerp(p2, (double)t);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float AngleFromCenter(PointF center, PointF point)
        {
            float dx = center.X - point.X;
            float dy = center.Y - point.Y;
            float rad = MathF.Atan2(dx, dy);
            float angle = rad * 180 / MathF.PI;
            return angle;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LineSegmentPointsFromAngle(PointF center, float angle, float length,
            out PointF p1, out PointF p2)
        {
            float rad = angle * (MathF.PI / 180f);
            float hl = length * 0.5f;

            p1 = new PointF(
                MathF.Cos(angle) * hl + center.X,
                MathF.Sin(angle) * hl + center.Y);

            p2 = new PointF(
                MathF.Cos(angle) * -hl + center.X,
                MathF.Sin(angle) * -hl + center.Y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LineSegmentPointsFromAngle(PointF center, float angle, float length,
            out Vector2 p1, out Vector2 p2)
        {
            float rad = angle * (MathF.PI / 180f);
            float hl = length * 0.5f;

            p1 = new Vector2(
                MathF.Cos(angle) * hl + center.X,
                MathF.Sin(angle) * hl + center.Y);

            p2 = new Vector2(
                MathF.Cos(angle) * -hl + center.X,
                MathF.Sin(angle) * -hl + center.Y);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LineSegmentPointsFromAngle(Vector2 center, float angle, float length,
            out Vector2 p1, out Vector2 p2)
        {
            float rad = angle * (MathF.PI / 180f);
            float hl = length * 0.5f;

            p1 = new Vector2(
                MathF.Cos(angle) * hl + center.X,
                MathF.Sin(angle) * hl + center.Y);

            p2 = new Vector2(
                MathF.Cos(angle) * -hl + center.X,
                MathF.Sin(angle) * -hl + center.Y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Magnitude(this PointF p)
        {
            return MathF.Sqrt(p.X * p.X + p.Y * p.Y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PointF Normalize(this PointF p)
        {
            float im = 1 / p.Magnitude();
            return new PointF(p.X * im, p.Y * im);
        }

        /// <summary>
        /// get normal vector to line (not normalized)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PointF Normal(PointF p1, PointF p2)
        {
            return new PointF(-(p2.Y - p1.Y), p2.X - p1.X);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Dot(PointF p1, PointF p2)
        {
            return p1.X * p2.X + p1.Y * p2.Y;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PointF Negate(this PointF p)
        {
            return new PointF(-p.X, -p.Y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Clip(this float f, float min, float max)
        {
            return MathF.Max(min, MathF.Min(f, max)); 
        }


    }
}