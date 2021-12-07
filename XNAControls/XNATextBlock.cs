using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FontStashSharp;

using Microsoft.Xna.Framework;
using Rampastring.Tools;

namespace Rampastring.XNAUI.XNAControls
{
    /// <summary>
    /// A panel with text.
    /// </summary>
    public class XNATextBlock : XNAPanel
    {
        public XNATextBlock(WindowManager windowManager) : base(windowManager)
        {
        }

        protected override void OnTextChanged(string v)
{
            _text = Renderer.FixText(v, GetFont(), Width - TextXMargin * 2).Text;
            base.OnTextChanged(_text);
        }

        private Color? _textColor;

        public Color TextColor
        {
            get
            {
                if (_textColor.HasValue)
                    return _textColor.Value;

                return UISettings.ActiveSettings.TextColor;
            }
            set { _textColor = value; }
        }


        public int TextXMargin { get; set; } = 3;

        public int TextYPosition { get; set; } = 3;

        protected override void ParseAttributeFromUIConfigurations(string property, Type type)
        {
            switch (property)
            {
                case "TextColor":
                    if (this.TryGet(property, out Color color))
                        TextColor = color;
                    return;
            }
            base.ParseAttributeFromUIConfigurations(property, type);
        }

        public override void Draw(GameTime gameTime)
        {
            DrawPanel();

            if (!string.IsNullOrEmpty(Text))
            {
                var windowRectangle = RenderRectangle();

                DrawStringWithShadow(Text, GetFont(),
                    new Vector2(TextXMargin, TextYPosition), TextColor);
            }

            if (DrawBorders)
                DrawPanelBorders();

            DrawChildren(gameTime);
        }
    }
}
