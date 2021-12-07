
using Microsoft.Xna.Framework;

namespace Rampastring.XNAUI.XNAControls
{
    public static class ControlExtensions
    {
        public static Rectangle GetClientRectangle(this XNAControl @this) => new Rectangle(@this.X, @this.Y, @this.Width, @this.Height);

        public static void SetClientRectangle(this XNAControl @this, int x, int y, int width, int height)
        {
            @this.SetSize(width, height);
            @this.SetPosition(x, y);
        }

        public static void SetClientRectangle(this XNAControl @this, ref Rectangle rect)
            => @this.SetClientRectangle(rect.X, rect.Y, rect.Width, rect.Height);

        public static void SetClientRectangle(this XNAControl @this, ref Point point, ref Point size)
        {
            @this.SetSize(ref point);
            @this.SetPosition(ref size);
        }

        public static void SetSize(this XNAControl @this, ref Point point)
            => @this.SetSize(point.X, point.Y);

        public static void SetPosition(this XNAControl @this, ref Point point)
            => @this.SetPosition(point.X, point.Y);
    }
}
