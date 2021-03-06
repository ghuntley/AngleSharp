﻿namespace AngleSharp.Dom.Css
{
    using AngleSharp.Dom.Collections;
    using AngleSharp.Events;
    using AngleSharp.Network;
    using AngleSharp.Parser.Css;
    using AngleSharp.Services.Styling;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// The CSS style engine for creating CSSStyleSheet instances.
    /// </summary>
    public class CssStyleEngine : ICssStyleEngine
    {
        #region Fields

        ICssStyleSheet _default;
        CssParserOptions _options;

        #endregion

        #region ctor

        /// <summary>
        /// Creates a new style engine.
        /// </summary>
        public CssStyleEngine()
        {
            _options = new CssParserOptions();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the type for the CSS style engine.
        /// </summary>
        public String Type
        {
            get { return MimeTypeNames.Css; }
        }

        /// <summary>
        /// Gets the default stylesheet as specified by the W3C:
        /// http://www.w3.org/TR/CSS21/sample.html
        /// </summary>
        public ICssStyleSheet Default
        {
            get { return _default ?? SetDefault(DefaultSource); }
        }

        /// <summary>
        /// Gets or sets the used parser options.
        /// </summary>
        public CssParserOptions Options
        {
            get { return _options; }
            set { _options = value; }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Sets a new default stylesheet defined by the provided string.
        /// </summary>
        /// <param name="sourceCode">The source for a new base stylesheet.</param>
        /// <returns>The CSSOM of the parsed source.</returns>
        public ICssStyleSheet SetDefault(String sourceCode)
        {
            var parser = new CssParser(_options);
            var source = new TextSource(sourceCode);
            var sheet = new CssStyleSheet(parser, default(String), default(ICssStyleSheet));
            _default = Parse(parser, sheet, source).Result;
            return _default;
        }

        /// <summary>
        /// Creates a style sheet for the given source.
        /// </summary>
        /// <param name="sourceCode">
        /// The source code describing the style sheet.
        /// </param>
        /// <param name="options">
        /// The options with the parameters for evaluating the style.
        /// </param>
        /// <returns>The created style sheet.</returns>
        public IStyleSheet ParseStylesheet(String sourceCode, StyleOptions options)
        {
            var parser = new CssParser(_options, options.Configuration);
            var sheet = new CssStyleSheet(parser, default(String), options.Element)
            {
                IsDisabled = options.IsDisabled
            };
            var source = new TextSource(sourceCode);
            return Parse(parser, sheet, source).Result;
        }

        /// <summary>
        /// Creates a style sheet for the given response from a request.
        /// </summary>
        /// <param name="response">
        /// The response with the stream representing the source of the
        /// stylesheet.
        /// </param>
        /// <param name="options">
        /// The options with the parameters for evaluating the style.
        /// </param>
        /// <returns>The created style sheet.</returns>
        public IStyleSheet ParseStylesheet(IResponse response, StyleOptions options)
        {
            var parser = new CssParser(_options, options.Configuration);
            var sheet = new CssStyleSheet(parser, response.Address.Href, options.Element) 
            { 
                IsDisabled = options.IsDisabled
            };
            var source = new TextSource(response.Content);
            return Parse(parser, sheet, source).Result;
        }

        /// <summary>
        /// Creates a style declaration for the given source.
        /// </summary>
        /// <param name="source">
        /// The source code for the inline style declaration.
        /// </param>
        /// <param name="options">
        /// The options with the parameters for evaluating the style.
        /// </param>
        /// <returns>The created style declaration.</returns>
        public ICssStyleDeclaration ParseInline(String source, StyleOptions options)
        {
            var parser = new CssParser(_options, options.Configuration);
            var style = new CssStyleDeclaration(parser);
            style.Update(source);
            return style;
        }

        /// <summary>
        /// Creates a media list for the given source.
        /// </summary>
        /// <param name="source">The media source.</param>
        /// <param name="options">
        /// The options with the parameters for evaluating the style.
        /// </param>
        /// <returns>The created media list.</returns>
        public IMediaList ParseMedia(String source, StyleOptions options)
        {
            var parser = new CssParser(_options, options.Configuration);
            var media = new MediaList(parser);
            media.MediaText = source;
            return media;
        }

        #endregion

        #region Helper

        async Task<CssStyleSheet> Parse(CssParser parser, CssStyleSheet sheet, TextSource source)
        {
            var evt = new CssParseStartEvent(sheet);
            var events = parser.Config.Events;

            if (events != null)
                events.Publish(evt);

            await parser.ParseStylesheetAsync(sheet, source).ConfigureAwait(false);
            evt.FireEnd();
            return sheet;
        }

        #endregion

        #region Default Stylesheet

        /// <summary>
        /// Gets the source code for the by default used base stylesheet.
        /// </summary>
        public static readonly String DefaultSource = @"
html, address,
blockquote,
body, dd, div,
dl, dt, fieldset, form,
frame, frameset,
h1, h2, h3, h4,
h5, h6, noframes,
ol, p, ul, center,
dir, hr, menu, pre   { display: block; unicode-bidi: embed }
li              { display: list-item }
head            { display: none }
table           { display: table }
tr              { display: table-row }
thead           { display: table-header-group }
tbody           { display: table-row-group }
tfoot           { display: table-footer-group }
col             { display: table-column }
colgroup        { display: table-column-group }
td, th          { display: table-cell }
caption         { display: table-caption }
th              { font-weight: bolder; text-align: center }
caption         { text-align: center }
body            { margin: 8px }
h1              { font-size: 2em; margin: .67em 0 }
h2              { font-size: 1.5em; margin: .75em 0 }
h3              { font-size: 1.17em; margin: .83em 0 }
h4, p,
blockquote, ul,
fieldset, form,
ol, dl, dir,
menu            { margin: 1.12em 0 }
h5              { font-size: .83em; margin: 1.5em 0 }
h6              { font-size: .75em; margin: 1.67em 0 }
h1, h2, h3, h4,
h5, h6, b,
strong          { font-weight: bolder }
blockquote      { margin-left: 40px; margin-right: 40px }
i, cite, em,
var, address    { font-style: italic }
pre, tt, code,
kbd, samp       { font-family: monospace }
pre             { white-space: pre }
button, textarea,
input, select   { display: inline-block }
big             { font-size: 1.17em }
small, sub, sup { font-size: .83em }
sub             { vertical-align: sub }
sup             { vertical-align: super }
table           { border-spacing: 2px; }
thead, tbody,
tfoot           { vertical-align: middle }
td, th, tr      { vertical-align: inherit }
s, strike, del  { text-decoration: line-through }
hr              { border: 1px inset }
ol, ul, dir,
menu, dd        { margin-left: 40px }
ol              { list-style-type: decimal }
ol ul, ul ol,
ul ul, ol ol    { margin-top: 0; margin-bottom: 0 }
u, ins          { text-decoration: underline }
br:before       { content: '\A'; white-space: pre-line }
center          { text-align: center }
:link, :visited { text-decoration: underline }
:focus          { outline: thin dotted invert }

/* Begin bidirectionality settings (do not change) */
BDO[DIR='ltr']  { direction: ltr; unicode-bidi: bidi-override }
BDO[DIR='rtl']  { direction: rtl; unicode-bidi: bidi-override }

*[DIR='ltr']    { direction: ltr; unicode-bidi: embed }
*[DIR='rtl']    { direction: rtl; unicode-bidi: embed }

@media print {
  h1            { page-break-before: always }
  h1, h2, h3,
  h4, h5, h6    { page-break-after: avoid }
  ul, ol, dl    { page-break-before: avoid }
}";

        #endregion
    }
}
