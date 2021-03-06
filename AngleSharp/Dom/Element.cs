﻿namespace AngleSharp.Dom
{
    using AngleSharp.Dom.Collections;
    using AngleSharp.Dom.Css;
    using AngleSharp.Dom.Events;
    using AngleSharp.Extensions;
    using AngleSharp.Html;
    using AngleSharp.Parser.Css;
    using AngleSharp.Services.Styling;
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// Represents an element node.
    /// </summary>
    [DebuggerStepThrough]
    internal class Element : Node, IElement
    {
        #region Fields

        static readonly ConditionalWeakTable<Element, IShadowRoot> shadowRoots = new ConditionalWeakTable<Element, IShadowRoot>();

        readonly NamedNodeMap _attributes;
        readonly String _namespace;
        readonly String _prefix;
        readonly String _localName;

        HtmlElementCollection _elements;
        TokenList _classList;

        #endregion

        #region ctor

        public Element(Document owner, String localName, String prefix, String namespaceUri, NodeFlags flags = NodeFlags.None)
            : this(owner, prefix != null ? String.Concat(prefix, ":", localName) : localName, localName, prefix, namespaceUri, flags)
        {
        }

        public Element(Document owner, String name, String localName, String prefix, String namespaceUri, NodeFlags flags = NodeFlags.None)
            : base(owner, name, NodeType.Element, flags)
        {
            _localName = localName;
            _prefix = prefix;
            _namespace = namespaceUri;
            _attributes = new NamedNodeMap(this);
        }

        #endregion

        #region Internal Properties

        /// <summary>
        /// Gets the associated attribute container.
        /// </summary>
        internal NamedNodeMap Attributes
        {
            get { return _attributes; }
        }

        #endregion

        #region Properties

        public IElement AssignedSlot
        {
            get 
            { 
                var parent = ParentElement;

                if (parent.IsShadow())
                {
                    var tree = parent.ShadowRoot;
                    return tree.GetAssignedSlot(Slot);
                }

                return null;
            }
        }

        public String Slot
        {
            get { return this.GetOwnAttribute(AttributeNames.Slot); }
            set { this.SetOwnAttribute(AttributeNames.Slot, value); }
        }

        public IShadowRoot ShadowRoot
        {
            get
            {
                var root = default(IShadowRoot);
                shadowRoots.TryGetValue(this, out root);
                return root;
            }
        }

        public String Prefix
        {
            get { return _prefix; }
        }

        public String LocalName
        {
            get { return _localName; }
        }

        public String NamespaceUri
        {
            get { return _namespace; }
        }

        public override String TextContent
        {
            get
            {
                var sb = Pool.NewStringBuilder();

                foreach (var child in this.GetDescendants().OfType<IText>())
                {
                    sb.Append(child.Data);
                }

                return sb.ToPool();
            }
            set
            {
                var node = !String.IsNullOrEmpty(value) ? new TextNode(Owner, value) : null;
                ReplaceAll(node, false);
            }
        }

        public ITokenList ClassList
        {
            get
            {
                if (_classList == null)
                {
                    _classList = new TokenList(this.GetOwnAttribute(AttributeNames.Class));
                    CreateBindings(_classList, AttributeNames.Class);
                }

                return _classList;
            }
        }

        public String ClassName
        {
            get { return this.GetOwnAttribute(AttributeNames.Class); }
            set { this.SetOwnAttribute(AttributeNames.Class, value); }
        }

        public String Id
        {
            get { return this.GetOwnAttribute(AttributeNames.Id); }
            set { this.SetOwnAttribute(AttributeNames.Id, value); }
        }

        public String TagName
        {
            get { return NodeName; }
        }

        public IElement PreviousElementSibling
        {
            get
            {
                var parent = Parent;

                if (parent != null)
                {
                    var found = false;

                    for (var i = parent.ChildNodes.Length - 1; i >= 0; i--)
                    {
                        if (Object.ReferenceEquals(parent.ChildNodes[i], this))
                        {
                            found = true;
                        }
                        else if (found && parent.ChildNodes[i] is IElement)
                        {
                            return (IElement)parent.ChildNodes[i];
                        }
                    }
                }

                return null;
            }
        }

        public IElement NextElementSibling
        {
            get
            {
                var parent = Parent;

                if (parent != null)
                {
                    var n = parent.ChildNodes.Length;
                    var found = false;

                    for (var i = 0; i < n; i++)
                    {
                        if (Object.ReferenceEquals(parent.ChildNodes[i], this))
                        {
                            found = true;
                        }
                        else if (found && parent.ChildNodes[i] is IElement)
                        {
                            return (IElement)parent.ChildNodes[i];
                        }
                    }
                }

                return null;
            }
        }

        public Int32 ChildElementCount
        {
            get
            {
                var children = ChildNodes;
                var n = children.Length;
                var count = 0;

                for (var i = 0; i < n; i++)
                {
                    if (children[i].NodeType == NodeType.Element)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public IHtmlCollection<IElement> Children
        {
            get { return _elements ?? (_elements = new HtmlElementCollection(this, deep: false)); }
        }

        public IElement FirstElementChild
        {
            get 
            {
                var children = ChildNodes;
                var n = children.Length;

                for (var i = 0; i < n; i++)
                {
                    var child = children[i] as IElement;

                    if (child != null)
                    {
                        return child;
                    }
                }

                return null;
            }
        }

        public IElement LastElementChild
        {
            get
            {
                var children = ChildNodes;

                for (int i = children.Length - 1; i >= 0; i--)
                {
                    var child = children[i] as IElement;

                    if (child != null)
                    {
                        return child;
                    }
                }

                return null;
            }
        }

        public String InnerHtml
        {
            get { return ChildNodes.ToHtml(HtmlMarkupFormatter.Instance); }
            set { ReplaceAll(new DocumentFragment(this, value), false); }
        }

        public String OuterHtml
        {
            get { return ToHtml(HtmlMarkupFormatter.Instance); }
            set
            {
                var parent = Parent;

                if (parent == null)
                {
                    throw new DomException(DomError.NotSupported);
                }

                var document = Owner;

                if (document != null && Object.ReferenceEquals(document.DocumentElement, this))
                {
                    throw new DomException(DomError.NoModificationAllowed);
                }

                parent.InsertChild(parent.IndexOf(this), new DocumentFragment(this, value));
                parent.RemoveChild(this);
            }
        }
        
        INamedNodeMap IElement.Attributes
        {
            get { return _attributes; }
        }

        public Boolean IsFocused
        {
            get
            {
                var document = Owner;
                return document != null ? Object.ReferenceEquals(document.FocusElement, this) : false;
            }
            protected set
            {
                var document = Owner;

                if (document != null)
                {
                    if (value)
                    {
                        document.SetFocus(this);
                        this.Fire<FocusEvent>(m => m.Init(EventNames.Focus, false, false));
                    }
                    else
                    {
                        document.SetFocus(null);
                        this.Fire<FocusEvent>(m => m.Init(EventNames.Blur, false, false));
                    }
                }
            }
        }

        #endregion

        #region Methods

        public IShadowRoot AttachShadow(ShadowRootMode mode = ShadowRootMode.Open)
        {
            if (TagNames.AllNoShadowRoot.Contains(_localName))
            {
                throw new DomException(DomError.NotSupported);
            }
            else if (ShadowRoot != null)
            {
                throw new DomException(DomError.InvalidState);
            }

            var root = new ShadowRoot(this, mode);
            shadowRoots.Add(this, root);
            return root;
        }

        public IElement QuerySelector(String selectors)
        {
            return ChildNodes.QuerySelector(selectors);
        }

        public IHtmlCollection<IElement> QuerySelectorAll(String selectors)
        {
            return ChildNodes.QuerySelectorAll(selectors);
        }

        public IHtmlCollection<IElement> GetElementsByClassName(String classNames)
        {
            return ChildNodes.GetElementsByClassName(classNames);
        }

        public IHtmlCollection<IElement> GetElementsByTagName(String tagName)
        {
            return ChildNodes.GetElementsByTagName(tagName);
        }

        public IHtmlCollection<IElement> GetElementsByTagNameNS(String namespaceURI, String tagName)
        {
            return ChildNodes.GetElementsByTagName(namespaceURI, tagName);
        }

        public Boolean Matches(String selectors)
        {
            return CssParser.Default.ParseSelector(selectors).Match(this);
        }

        public override INode Clone(Boolean deep = true)
        {
            var node = new Element(Owner, LocalName, _prefix, _namespace, Flags);
            CopyProperties(this, node, deep);
            CopyAttributes(this, node);
            return node;
        }

        public IPseudoElement Pseudo(String pseudoElement)
        {
            return PseudoElement.Create(this, pseudoElement);
        }

        public Boolean HasAttribute(String name)
        {
            if (_namespace.Is(NamespaceNames.HtmlUri))
            {
                name = name.ToLowerInvariant();
            }

            return _attributes.GetNamedItem(name) != null;
        }

        public Boolean HasAttribute(String namespaceUri, String localName)
        {
            if (String.IsNullOrEmpty(namespaceUri))
            {
                namespaceUri = null;
            }

            return _attributes.GetNamedItem(namespaceUri, localName) != null;
        }

        public String GetAttribute(String name)
        {
            if (_namespace.Is(NamespaceNames.HtmlUri))
            {
                name = name.ToLower();
            }

            var attr = _attributes.GetNamedItem(name);
            return attr != null ? attr.Value : null;
        }

        public String GetAttribute(String namespaceUri, String localName)
        {
            if (String.IsNullOrEmpty(namespaceUri))
            {
                namespaceUri = null;
            }

            var attr = _attributes.GetNamedItem(namespaceUri, localName);
            return attr != null ? attr.Value : null;
        }

        public void SetAttribute(String name, String value)
        {
            if (value != null)
            {
                if (!name.IsXmlName())
                    throw new DomException(DomError.InvalidCharacter);

                if (_namespace.Is(NamespaceNames.HtmlUri))
                    name = name.ToLowerInvariant();

                this.SetOwnAttribute(name, value);
            }
            else
            {
                RemoveAttribute(name);
            }
        }

        public void SetAttribute(String namespaceUri, String name, String value)
        {
            if (value != null)
            {
                var prefix = default(String);
                var localName = default(String);
                GetPrefixAndLocalName(name, ref namespaceUri, out prefix, out localName);
                _attributes.SetNamedItem(new Attr(prefix, localName, value, namespaceUri));
            }
            else
            {
                RemoveAttribute(namespaceUri, name);
            }
        }

        public void RemoveAttribute(String name)
        {
            if (_namespace.Is(NamespaceNames.HtmlUri))
            {
                name = name.ToLower();
            }

            _attributes.RemoveNamedItemOrDefault(name);
        }

        public void RemoveAttribute(String namespaceUri, String localName)
        {
            if (String.IsNullOrEmpty(namespaceUri))
            {
                namespaceUri = null;
            }

            _attributes.RemoveNamedItemOrDefault(namespaceUri, localName);
        }

        public void Prepend(params INode[] nodes)
        {
            this.PrependNodes(nodes);
        }

        public void Append(params INode[] nodes)
        {
            this.AppendNodes(nodes);
        }

        public override Boolean Equals(INode otherNode)
        {
            var otherElement = otherNode as IElement;

            if (otherElement != null)
            {
                return NamespaceUri.Is(otherElement.NamespaceUri) &&
                    _attributes.AreEqual(otherElement.Attributes) && 
                    base.Equals(otherNode);
            }

            return false;
        }

        public void Before(params INode[] nodes)
        {
            this.InsertBefore(nodes);
        }

        public void After(params INode[] nodes)
        {
            this.InsertAfter(nodes);
        }

        public void Replace(params INode[] nodes)
        {
            this.ReplaceWith(nodes);
        }

        public void Remove()
        {
            this.RemoveFromParent();
        }

        public void Insert(AdjacentPosition position, String html)
        {
            var useThis = position == AdjacentPosition.BeforeBegin || position == AdjacentPosition.AfterEnd;
            var nodeParent = useThis ? this : Parent as Element;
            var nodes = new DocumentFragment(nodeParent, html);

            switch (position)
            {
                case AdjacentPosition.BeforeBegin:
                    Parent.InsertBefore(nodes, this);
                    break;

                case AdjacentPosition.AfterEnd:
                    Parent.InsertChild(Parent.IndexOf(this) + 1, nodes);
                    break;

                case AdjacentPosition.AfterBegin:
                    InsertChild(0, nodes);
                    break;

                case AdjacentPosition.BeforeEnd:
                    AppendChild(nodes);
                    break;
            }
        }

        public override String ToHtml(IMarkupFormatter formatter)
        {
            var selfClosing = Flags.HasFlag(NodeFlags.SelfClosing);
            var open = formatter.OpenTag(this, selfClosing);
            var children = String.Empty;

            if (!selfClosing)
            {
                var sb = Pool.NewStringBuilder();

                if (Flags.HasFlag(NodeFlags.LineTolerance) && FirstChild is IText)
                {
                    var text = (IText)FirstChild;

                    if (text.Data.Length > 0 && text.Data[0] == Symbols.LineFeed)
                    {
                        sb.Append(Symbols.LineFeed);
                    }
                }

                foreach (var child in ChildNodes)
                {
                    sb.Append(child.ToHtml(formatter));
                }

                children = sb.ToPool();
            }

            var close = formatter.CloseTag(this, selfClosing);
            return String.Concat(open, children, close);
        }

        #endregion

        #region Helpers

        internal virtual void SetupElement()
        {
        }

        internal void AttributeChanged(String localName, String namespaceUri, String oldValue)
        {
            Owner.QueueMutation(MutationRecord.Attributes(
                target: this,
                attributeName: localName,
                attributeNamespace: namespaceUri,
                previousValue: oldValue));
        }

        /// <summary>
        /// Creates the style for the inline style declaration.
        /// </summary>
        /// <returns>The declaration representing the declarations.</returns>
        protected ICssStyleDeclaration CreateStyle()
        {
            var config = Owner.Options;
            var engine = config.GetCssStyleEngine();

            if (engine != null)
            {
                var source = this.GetOwnAttribute(AttributeNames.Style);
                var options = new StyleOptions { Element = this, Configuration = config };
                var style = engine.ParseInline(source, options);
                var bindable = style as IBindable;

                if (bindable != null)
                {
                    bindable.Changed += value => UpdateAttribute(AttributeNames.Style, value);
                }

                return style;
            }

            return null;
        }

        protected void CreateBindings(IBindable bindable, String attributeName)
        {
            bindable.Changed += value => UpdateAttribute(attributeName, value);
            RegisterAttributeObserver(attributeName, value => bindable.Update(value));
        }

        /// <summary>
        /// Updates an attribute's value without notifying the observers.
        /// </summary>
        /// <param name="name">The name of the attribute to update.</param>
        /// <param name="value">The value of the attribute to set.</param>
        protected void UpdateAttribute(String name, String value)
        {
            var handler = _attributes.RemoveHandler(name);
            this.SetOwnAttribute(name, value);
            _attributes.SetHandler(name, handler);
        }

        /// <summary>
        /// Locates the namespace of the given prefix.
        /// </summary>
        /// <param name="prefix">The prefix of the namespace to find.</param>
        /// <returns>
        /// The url of the namespace or null, if the prefix could not be found.
        /// </returns>
        protected sealed override String LocateNamespace(String prefix)
        {
            return ElementExtensions.LocateNamespace(this, prefix);
        }

        /// <summary>
        /// Locates the prefix of the given namespace.
        /// </summary>
        /// <param name="namespaceUri">The url of the namespace.</param>
        /// <returns>
        /// The prefix or null, if the namespace could not be found.
        /// </returns>
        protected sealed override String LocatePrefix(String namespaceUri)
        {
            return ElementExtensions.LocatePrefix(this, namespaceUri);
        }

        /// <summary>
        /// Copies the attributes from the source element to the target
        /// element. Each attribute will be recreated on the target.
        /// </summary>
        /// <param name="source">The source of the attributes.</param>
        /// <param name="target">
        /// The target where to create the attributes.
        /// </param>
        protected static void CopyAttributes(Element source, Element target)
        {
            foreach (var attribute in source._attributes)
            {
                var attr = new Attr(attribute.Prefix, attribute.LocalName, attribute.Value, attribute.NamespaceUri);
                target._attributes.FastAddItem(attr);
            }
        }

        /// <summary>
        /// Registers an observer for attribute events.
        /// </summary>
        /// <param name="name">The name of the attribute.</param>
        /// <param name="callback">The callback to invoke.</param>
        protected void RegisterAttributeObserver(String name, Action<String> callback)
        {
            _attributes.AddHandler(name, callback);
        }

        #endregion
    }
}
