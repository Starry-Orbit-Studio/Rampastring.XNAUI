﻿using System;

using Microsoft.Xna.Framework;

using Rampastring.Tools;

using static System.Net.Mime.MediaTypeNames;

namespace Rampastring.XNAUI.XNAControls
{
    /// <summary>
    /// A text box that displays a "suggestion" text when it's not active.
    /// </summary>
    public class XNASuggestionTextBox : XNATextBox
    {
        public XNASuggestionTextBox(WindowManager windowManager) : base(windowManager)
        {
        }

        public string Suggestion { get; set; }

        private Color? _suggestedTextColor;

        public Color SuggestedTextColor
        {
            get => _suggestedTextColor ?? UISettings.ActiveSettings.SubtleTextColor;
            set => _suggestedTextColor = value;
        }

        public override void Initialize()
        {
            base.Initialize();

            Text = Suggestion ?? string.Empty;
        }

        protected override void ParseAttributeFromUIConfigurations(string property, Type type)
        {
            switch (property)
            {
                case nameof(SuggestedTextColor):
                    if (this.TryGet(property, out Color c))
                        SuggestedTextColor = c;
                    return;
            }
            base.ParseAttributeFromUIConfigurations(property, type);
        }

        protected override void ParseLocaleStringsFromStringManager()
        {
            if (!SkipParseTextFromStringManager)
                Suggestion = GetUIString(nameof(Suggestion));
            base.ParseLocaleStringsFromStringManager();
        }

        public override Color TextColor
        {
            get => WindowManager.SelectedControl == this ? base.TextColor : SuggestedTextColor;
            set => base.TextColor = value;
        }

        public override void OnSelectedChanged()
        {
            base.OnSelectedChanged();

            if (WindowManager.SelectedControl == this)
            {
                if (Text == Suggestion)
                    Text = string.Empty;
            }
            else
            {
                if (string.IsNullOrEmpty(Text))
                    Text = Suggestion ?? string.Empty;
            }
        }

    }
}
