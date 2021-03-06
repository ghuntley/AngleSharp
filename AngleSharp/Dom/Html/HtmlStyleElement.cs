﻿namespace AngleSharp.Dom.Html
{
    using AngleSharp.Extensions;
    using AngleSharp.Html;
    using AngleSharp.Network;
    using AngleSharp.Services.Styling;
    using System;

    /// <summary>
    /// Represents the HTML style element.
    /// </summary>
    sealed class HtmlStyleElement : HtmlElement, IHtmlStyleElement
    {
        #region Fields

        IStyleSheet _sheet;

        #endregion

        #region ctor

        /// <summary>
        /// Creates an HTML style element.
        /// </summary>
        public HtmlStyleElement(Document owner, String prefix = null)
            : base(owner, TagNames.Style, prefix, NodeFlags.Special | NodeFlags.LiteralText)
        {
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets if the style is scoped.
        /// </summary>
        public Boolean IsScoped
        {
            get { return this.HasOwnAttribute(AttributeNames.Scoped); }
            set { this.SetOwnAttribute(AttributeNames.Scoped, value ? String.Empty : null); }
        }

        /// <summary>
        /// Gets the associated style sheet.
        /// </summary>
        public IStyleSheet Sheet
        {
            get { return _sheet ?? (_sheet = CreateSheet()); }
        }

        /// <summary>
        /// Gets or sets if the style is enabled or disabled.
        /// </summary>
        public Boolean IsDisabled
        {
            get { return this.GetOwnAttribute(AttributeNames.Disabled).ToBoolean(); }
            set 
            {
                this.SetOwnAttribute(AttributeNames.Disabled, value ? String.Empty : null);

                if (_sheet != null) 
                    _sheet.IsDisabled = value; 
            }
        }

        /// <summary>
        /// Gets or sets the use with one or more target media.
        /// </summary>
        public String Media
        {
            get { return this.GetOwnAttribute(AttributeNames.Media); }
            set { this.SetOwnAttribute(AttributeNames.Media, value); }
        }

        /// <summary>
        /// Gets or sets the content type of the style sheet language.
        /// </summary>
        public String Type
        {
            get { return this.GetOwnAttribute(AttributeNames.Type); }
            set { this.SetOwnAttribute(AttributeNames.Type, value); }
        }

        #endregion

        #region Internal Methods

        internal override void SetupElement()
        {
            base.SetupElement();

            var media = this.GetOwnAttribute(AttributeNames.Media);
            RegisterAttributeObserver(AttributeNames.Media, UpdateMedia);

            if (media != null)
            {
                UpdateMedia(media);
            }
        }

        internal override void NodeIsInserted(Node newNode)
        {
            base.NodeIsInserted(newNode);
            UpdateSheet();
        }

        internal override void NodeIsRemoved(Node removedNode, Node oldPreviousSibling)
        {
            base.NodeIsRemoved(removedNode, oldPreviousSibling);
            UpdateSheet();
        }

        #endregion

        #region Helpers

        void UpdateMedia(String value)
        {
            if (_sheet != null)
            {
                _sheet.Media.MediaText = value;
            }
        }

        void UpdateSheet()
        {
            if (_sheet != null)
            {
                _sheet = CreateSheet();
            }
        }

        IStyleSheet CreateSheet()
        {
            var config = Owner.Options;
            var type = Type ?? MimeTypeNames.Css;
            var engine = config.GetStyleEngine(type);

            if (engine != null)
            {
                var options = new StyleOptions
                {
                    Element = this,
                    IsDisabled = IsDisabled,
                    IsAlternate = false,
                    Configuration = config
                };
                return engine.ParseStylesheet(TextContent, options);
            }

            return null;
        }

        #endregion
    }
}
