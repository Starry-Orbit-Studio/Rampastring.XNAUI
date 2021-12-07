using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Xna.Framework;

namespace Rampastring.XNAUI
{
    public static class PlatformUtils
    {
        public static Point SumPoints(Point p1, Point p2)
        {
#if XNA
            // XNA's Point is too dumb to know the plus operator
            return new Point(p1.X + p2.X, p1.Y + p2.Y);
#else
            return p1 + p2;
#endif
        }
    }
}
