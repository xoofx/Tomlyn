using System;
using System.Collections.Generic;
using System.IO;
using Tomlyn.Model;
using Tomlyn.Serialization;
using Tomlyn.Syntax;
using Tomlyn.Text;

namespace Tomlyn.Parsing;

/// <summary>
/// Parses TOML into an incremental event stream.
/// </summary>
public sealed class TomlParser
{
    private readonly TomlSerializerOptions _options;
    private readonly TomlParserOptions _parserOptions;
    private readonly IParserCore _core;
    private DiagnosticsBag? _diagnostics;
    private TomlParseEvent _current;
    private int _depth;

    private TomlParser(IParserCore core, TomlSerializerOptions options, TomlParserOptions parserOptions, DiagnosticsBag? diagnostics)
    {
        _core = core;
        _options = options ?? TomlSerializerOptions.Default;
        _parserOptions = parserOptions ?? throw new ArgumentNullException(nameof(parserOptions));
        _diagnostics = diagnostics;
        _current = default;
        _depth = 0;
    }

    /// <summary>
    /// Creates a parser over a TOML string.
    /// </summary>
    /// <param name="toml">The TOML payload.</param>
    /// <param name="options">Optional parser/serializer options.</param>
    /// <returns>A parser instance positioned before the first event.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="toml"/> is <c>null</c>.</exception>
    /// <exception cref="TomlException">The TOML payload is invalid.</exception>
    public static TomlParser Create(string toml, TomlSerializerOptions? options = null)
    {
        if (toml is null)
        {
            throw new ArgumentNullException(nameof(toml));
        }

        var effectiveOptions = options ?? TomlSerializerOptions.Default;
        var sourceView = new StringSourceView(toml, effectiveOptions.SourceName ?? string.Empty);
        var parserOptions = new TomlParserOptions();
        DiagnosticsBag? diagnostics = null;
        if (parserOptions.Mode == TomlParserMode.Tolerant)
        {
            diagnostics = new DiagnosticsBag();
        }

        var core = new ParserCore<StringSourceView, StringCharacterIterator>(sourceView, parserOptions, diagnostics);
        return new TomlParser(core, effectiveOptions, parserOptions, diagnostics);
    }

    /// <summary>
    /// Creates a parser over a TOML string.
    /// </summary>
    /// <param name="toml">The TOML payload.</param>
    /// <param name="parserOptions">Parser options controlling error handling.</param>
    /// <param name="options">Optional parser/serializer options.</param>
    /// <returns>A parser instance positioned before the first event.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="toml"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="parserOptions"/> is <c>null</c>.</exception>
    /// <exception cref="TomlException">The TOML payload is invalid.</exception>
    public static TomlParser Create(string toml, TomlParserOptions parserOptions, TomlSerializerOptions? options = null)
    {
        if (toml is null)
        {
            throw new ArgumentNullException(nameof(toml));
        }

        if (parserOptions is null)
        {
            throw new ArgumentNullException(nameof(parserOptions));
        }

        var effectiveOptions = options ?? TomlSerializerOptions.Default;
        var sourceView = new StringSourceView(toml, effectiveOptions.SourceName ?? string.Empty);
        DiagnosticsBag? diagnostics = null;
        if (parserOptions.Mode == TomlParserMode.Tolerant)
        {
            diagnostics = new DiagnosticsBag();
        }

        var core = new ParserCore<StringSourceView, StringCharacterIterator>(sourceView, parserOptions, diagnostics);
        return new TomlParser(core, effectiveOptions, parserOptions, diagnostics);
    }

    /// <summary>
    /// Creates a parser over TOML text from a <see cref="TextReader"/>.
    /// </summary>
    /// <param name="reader">The text reader.</param>
    /// <param name="options">Optional parser/serializer options.</param>
    /// <returns>A parser instance positioned before the first event.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="reader"/> is <c>null</c>.</exception>
    /// <exception cref="TomlException">The TOML payload is invalid.</exception>
    public static TomlParser Create(TextReader reader, TomlSerializerOptions? options = null)
    {
        if (reader is null)
        {
            throw new ArgumentNullException(nameof(reader));
        }

        return Create(reader.ReadToEnd(), options);
    }

    /// <summary>
    /// Creates a parser over TOML text from a <see cref="TextReader"/>.
    /// </summary>
    /// <param name="reader">The text reader.</param>
    /// <param name="parserOptions">Parser options controlling error handling.</param>
    /// <param name="options">Optional parser/serializer options.</param>
    /// <returns>A parser instance positioned before the first event.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="reader"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="parserOptions"/> is <c>null</c>.</exception>
    /// <exception cref="TomlException">The TOML payload is invalid.</exception>
    public static TomlParser Create(TextReader reader, TomlParserOptions parserOptions, TomlSerializerOptions? options = null)
    {
        if (reader is null)
        {
            throw new ArgumentNullException(nameof(reader));
        }

        if (parserOptions is null)
        {
            throw new ArgumentNullException(nameof(parserOptions));
        }
        return Create(reader.ReadToEnd(), parserOptions, options);
    }

    /// <summary>
    /// Creates a parser over UTF-8 TOML bytes.
    /// </summary>
    /// <param name="utf8Toml">The UTF-8 TOML payload.</param>
    /// <param name="options">Optional parser/serializer options.</param>
    /// <returns>A parser instance positioned before the first event.</returns>
    /// <exception cref="TomlException">The TOML payload is invalid.</exception>
    public static TomlParser Create(ReadOnlySpan<byte> utf8Toml, TomlSerializerOptions? options = null)
    {
        var effectiveOptions = options ?? TomlSerializerOptions.Default;
        var bytes = utf8Toml.ToArray();
        return Create(bytes, effectiveOptions);
    }

    /// <summary>
    /// Creates a parser over UTF-8 TOML bytes.
    /// </summary>
    /// <param name="utf8Toml">The UTF-8 TOML payload.</param>
    /// <param name="options">Optional parser/serializer options.</param>
    /// <returns>A parser instance positioned before the first event.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="utf8Toml"/> is <c>null</c>.</exception>
    /// <exception cref="TomlException">The TOML payload is invalid.</exception>
    public static TomlParser Create(byte[] utf8Toml, TomlSerializerOptions? options = null)
    {
        if (utf8Toml is null)
        {
            throw new ArgumentNullException(nameof(utf8Toml));
        }

        var effectiveOptions = options ?? TomlSerializerOptions.Default;
        var sourceView = new StringUtf8SourceView(utf8Toml, effectiveOptions.SourceName ?? string.Empty);
        var parserOptions = new TomlParserOptions();
        DiagnosticsBag? diagnostics = null;
        if (parserOptions.Mode == TomlParserMode.Tolerant)
        {
            diagnostics = new DiagnosticsBag();
        }

        var core = new ParserCore<StringUtf8SourceView, StringCharacterUtf8Iterator>(sourceView, parserOptions, diagnostics);
        return new TomlParser(core, effectiveOptions, parserOptions, diagnostics);
    }

    /// <summary>
    /// Creates a parser over UTF-8 TOML bytes.
    /// </summary>
    /// <param name="utf8Toml">The UTF-8 TOML payload.</param>
    /// <param name="parserOptions">Parser options controlling error handling.</param>
    /// <param name="options">Optional parser/serializer options.</param>
    /// <returns>A parser instance positioned before the first event.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="parserOptions"/> is <c>null</c>.</exception>
    /// <exception cref="TomlException">The TOML payload is invalid.</exception>
    public static TomlParser Create(ReadOnlySpan<byte> utf8Toml, TomlParserOptions parserOptions, TomlSerializerOptions? options = null)
    {
        if (parserOptions is null)
        {
            throw new ArgumentNullException(nameof(parserOptions));
        }

        var effectiveOptions = options ?? TomlSerializerOptions.Default;
        var bytes = utf8Toml.ToArray();
        return Create(bytes, parserOptions, effectiveOptions);
    }

    /// <summary>
    /// Creates a parser over UTF-8 TOML bytes.
    /// </summary>
    /// <param name="utf8Toml">The UTF-8 TOML payload.</param>
    /// <param name="parserOptions">Parser options controlling error handling.</param>
    /// <param name="options">Optional parser/serializer options.</param>
    /// <returns>A parser instance positioned before the first event.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="utf8Toml"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="parserOptions"/> is <c>null</c>.</exception>
    /// <exception cref="TomlException">The TOML payload is invalid.</exception>
    public static TomlParser Create(byte[] utf8Toml, TomlParserOptions parserOptions, TomlSerializerOptions? options = null)
    {
        if (utf8Toml is null)
        {
            throw new ArgumentNullException(nameof(utf8Toml));
        }

        if (parserOptions is null)
        {
            throw new ArgumentNullException(nameof(parserOptions));
        }

        var effectiveOptions = options ?? TomlSerializerOptions.Default;
        var sourceView = new StringUtf8SourceView(utf8Toml, effectiveOptions.SourceName ?? string.Empty);
        DiagnosticsBag? diagnostics = null;
        if (parserOptions.Mode == TomlParserMode.Tolerant)
        {
            diagnostics = new DiagnosticsBag();
        }

        var core = new ParserCore<StringUtf8SourceView, StringCharacterUtf8Iterator>(sourceView, parserOptions, diagnostics);
        return new TomlParser(core, effectiveOptions, parserOptions, diagnostics);
    }

    /// <summary>
    /// Gets the current parse event.
    /// </summary>
    public TomlParseEvent Current => _current;

    /// <summary>
    /// Gets the current string scalar value by decoding it from the original TOML input.
    /// </summary>
    /// <returns>The decoded string value.</returns>
    /// <exception cref="InvalidOperationException">The current event is not a string scalar.</exception>
    public string GetString()
    {
        if (_current.Kind != TomlParseEventKind.String)
        {
            throw new InvalidOperationException($"Expected {TomlParseEventKind.String} but was {_current.Kind}.");
        }

        if (_current.Span is not { } span)
        {
            return string.Empty;
        }

        var raw = GetText(span);
        if (raw is null)
        {
            return string.Empty;
        }

        return TomlStringDecoder.Decode(raw, (TokenKind)_current.Data);
    }

    /// <summary>
    /// Gets the current property name by decoding it from the original TOML input.
    /// </summary>
    /// <returns>The decoded property name.</returns>
    /// <exception cref="InvalidOperationException">The current event is not a property name.</exception>
    public string GetPropertyName()
    {
        if (_current.Kind != TomlParseEventKind.PropertyName)
        {
            throw new InvalidOperationException($"Expected {TomlParseEventKind.PropertyName} but was {_current.Kind}.");
        }

        if (_current.PropertyName is not null)
        {
            return _current.PropertyName;
        }

        if (_current.Span is not { } span)
        {
            return string.Empty;
        }

        var raw = GetText(span);
        if (raw is null)
        {
            return string.Empty;
        }

        var tokenKind = (TokenKind)_current.Data;
        return tokenKind == TokenKind.BasicKey ? raw : TomlStringDecoder.Decode(raw, tokenKind);
    }

    /// <summary>
    /// Gets the parser options.
    /// </summary>
    public TomlParserOptions ParserOptions => _parserOptions;

    /// <summary>
    /// Gets diagnostics produced by the parser.
    /// </summary>
    public DiagnosticsBag Diagnostics => _diagnostics ??= new DiagnosticsBag();

    /// <summary>
    /// Gets a value indicating whether diagnostics include errors.
    /// </summary>
    public bool HasErrors => _diagnostics?.HasErrors ?? false;

    /// <summary>
    /// Gets the optional source name associated with the parsed payload.
    /// </summary>
    public string? SourceName => _options.SourceName;

    /// <summary>
    /// Gets the current event depth.
    /// </summary>
    public int Depth => _depth;

    /// <summary>
    /// Advances to the next parse event.
    /// </summary>
    /// <returns><c>true</c> when a parse event is available; otherwise <c>false</c>.</returns>
    public bool MoveNext()
    {
        if (!_core.MoveNext(out _current))
        {
            return false;
        }

        switch (_current.Kind)
        {
            case TomlParseEventKind.StartTable:
            case TomlParseEventKind.StartArray:
                _depth++;
                break;
            case TomlParseEventKind.EndTable:
            case TomlParseEventKind.EndArray:
                _depth = Math.Max(0, _depth - 1);
                break;
        }

        return true;
    }

    internal string? GetText(TomlSourceSpan span) => _core.GetText(span);

    internal TomlSyntaxTriviaMetadata[]? CurrentLeadingTrivia => _core.LeadingTrivia;

    internal TomlSyntaxTriviaMetadata[]? CurrentTrailingTrivia => _core.TrailingTrivia;

    private interface IParserCore
    {
        bool MoveNext(out TomlParseEvent parseEvent);

        string? GetText(TomlSourceSpan span);

        TomlSyntaxTriviaMetadata[]? LeadingTrivia { get; }

        TomlSyntaxTriviaMetadata[]? TrailingTrivia { get; }
    }

    private sealed class ParserCore<TSourceView, TCharReader> : IParserCore
        where TSourceView : struct, ISourceView<TCharReader>
        where TCharReader : struct, CharacterIterator
    {
        private readonly TomlParserMode _mode;
        private readonly bool _decodeScalars;
        private readonly bool _captureTrivia;
        private readonly DiagnosticsBag? _diagnostics;
        private readonly Lexer<TSourceView, TCharReader> _lexer;
        private SyntaxTokenValue _token;
        private bool _initialized;
        private bool _terminatedDueToError;

        private readonly List<TomlSyntaxTriviaMetadata>? _pendingTrivia;
        private string? _pendingWhitespace;
        private TomlSyntaxTriviaMetadata[]? _leadingTrivia;
        private TomlSyntaxTriviaMetadata[]? _trailingTrivia;

        private TomlParseEvent _pendingEvent;
        private bool _hasPendingEvent;
        private TomlSyntaxTriviaMetadata[]? _pendingLeadingTrivia;
        private TomlSyntaxTriviaMetadata[]? _pendingTrailingTrivia;

        private PendingOperationKind _pendingOperation;

        // Pending key/value entry emission (EnsureImplicitPrefix + leaf PropertyName + consume `=`).
        private int _pendingKeyValueImplicitBaseIndex;
        private int _pendingKeyValuePrefixLength;
        private int _pendingKeyValueCommonPrefixLength;
        private int _pendingKeyValueOpenIndex;
        private bool _pendingKeyValueOpenEmitPropertyName;
        private bool _pendingKeyValueLeafEmitted;
        private bool _pendingKeyValueClosingsCompleted;

        // Pending table-header emission (Close implicit frames, transition explicit frames).
        private int _pendingTableHeaderExplicitPrefixLength;
        private int _pendingTableHeaderOpenIndex;
        private bool _pendingTableHeaderOpenEmitPropertyName;
        private ExplicitFrame _pendingTableHeaderOpenFrame;
        private bool _pendingTableHeaderHasOpenFrame;
        private bool _pendingTableHeaderClosingsCompleted;

        // Pending document termination.
        private DocumentEndStage _pendingDocumentEndStage;
        private bool _pendingDocumentEndRootTableClosed;
        private bool _pendingDocumentEndDocumentClosed;
        private int _pendingDocumentStartStage;

        private readonly List<KeySegment> _implicitFrames;
        private readonly List<ExplicitFrame> _explicitFrames;
        private readonly List<KeySegment> _pathSegments;
        private readonly List<ExplicitFrame> _targetExplicitFrames;
        private readonly List<ContainerFrame> _containers;

        private DocumentState _state;

        public ParserCore(TSourceView sourceView, TomlParserOptions parserOptions, DiagnosticsBag? diagnostics)
        {
            _mode = parserOptions.Mode;
            _decodeScalars = parserOptions.DecodeScalars;
            _captureTrivia = parserOptions.CaptureTrivia;
            _diagnostics = diagnostics;
            _lexer = new Lexer<TSourceView, TCharReader>(sourceView)
            {
                State = LexerState.Key,
                DecodeScalars = _decodeScalars,
            };
            _token = default;
            _initialized = false;
            _terminatedDueToError = false;
            _pendingTrivia = _captureTrivia ? new List<TomlSyntaxTriviaMetadata>(capacity: 8) : null;
            _pendingWhitespace = null;
            _leadingTrivia = null;
            _trailingTrivia = null;
            _pendingEvent = default;
            _hasPendingEvent = false;
            _pendingLeadingTrivia = null;
            _pendingTrailingTrivia = null;
            _pendingOperation = PendingOperationKind.None;
            _pendingKeyValueImplicitBaseIndex = 0;
            _pendingKeyValuePrefixLength = 0;
            _pendingKeyValueCommonPrefixLength = 0;
            _pendingKeyValueOpenIndex = 0;
            _pendingKeyValueOpenEmitPropertyName = true;
            _pendingKeyValueLeafEmitted = false;
            _pendingKeyValueClosingsCompleted = false;
            _pendingTableHeaderExplicitPrefixLength = 0;
            _pendingTableHeaderOpenIndex = 0;
            _pendingTableHeaderOpenEmitPropertyName = true;
            _pendingTableHeaderOpenFrame = default;
            _pendingTableHeaderHasOpenFrame = false;
            _pendingTableHeaderClosingsCompleted = false;
            _pendingDocumentEndStage = DocumentEndStage.Containers;
            _pendingDocumentEndRootTableClosed = false;
            _pendingDocumentEndDocumentClosed = false;
            _pendingDocumentStartStage = 0;

            _implicitFrames = new List<KeySegment>(capacity: 8);
            _explicitFrames = new List<ExplicitFrame>(capacity: 16);
            _pathSegments = new List<KeySegment>(capacity: 8);
            _targetExplicitFrames = new List<ExplicitFrame>(capacity: 16);
            _containers = new List<ContainerFrame>(capacity: 8);

            _state = DocumentState.NotStarted;
        }

        public string? GetText(TomlSourceSpan span) => _lexer.Source.GetString(span.Offset, span.Length);

        public TomlSyntaxTriviaMetadata[]? LeadingTrivia => _leadingTrivia;

        public TomlSyntaxTriviaMetadata[]? TrailingTrivia => _trailingTrivia;

        public bool MoveNext(out TomlParseEvent parseEvent)
        {
            if (_hasPendingEvent)
            {
                parseEvent = _pendingEvent;
                _leadingTrivia = _pendingLeadingTrivia;
                _trailingTrivia = _pendingTrailingTrivia;
                _hasPendingEvent = false;
                _pendingLeadingTrivia = null;
                _pendingTrailingTrivia = null;
                return true;
            }

            while (true)
            {
                bool producedNext;
                try
                {
                    producedNext = ProduceNext();
                }
                catch (TomlException ex) when (_mode == TomlParserMode.Tolerant)
                {
                    if (!_terminatedDueToError)
                    {
                        _terminatedDueToError = true;
                        RecordDiagnostics(ex);
                        BeginDocumentEnd();
                    }

                    producedNext = true;
                }

                if (!producedNext)
                {
                    break;
                }

                if (_hasPendingEvent)
                {
                    parseEvent = _pendingEvent;
                    _leadingTrivia = _pendingLeadingTrivia;
                    _trailingTrivia = _pendingTrailingTrivia;
                    _hasPendingEvent = false;
                    _pendingLeadingTrivia = null;
                    _pendingTrailingTrivia = null;
                    return true;
                }
            }

            parseEvent = default;
            _leadingTrivia = null;
            _trailingTrivia = null;
            return false;
        }

        private bool ProduceNext()
        {
            if (_state == DocumentState.Ended)
            {
                return false;
            }

            if (_state == DocumentState.NotStarted)
            {
                _state = DocumentState.Statement;
                EnsureInitialized();
                _pendingOperation = PendingOperationKind.DocumentStart;
                _pendingDocumentStartStage = 1;
                SetPendingEvent(new TomlParseEvent(TomlParseEventKind.StartDocument, span: null, propertyName: null, stringValue: null, data: 0));
                return true;
            }

            EnsureInitialized();

            if (_pendingOperation != PendingOperationKind.None)
            {
                if (ProducePendingOperation())
                {
                    return true;
                }
            }

            if (_containers.Count > 0)
            {
                return ProduceContainer();
            }

            return _state switch
            {
                DocumentState.Statement => ProduceStatement(),
                DocumentState.ExpectValue => ProduceKeyValueValue(),
                DocumentState.AfterValue => ProduceAfterValue(),
                _ => throw new InvalidOperationException($"Unexpected parser state {_state}."),
            };
        }

        private void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;
            NextToken();
        }

        private void NextToken()
        {
            bool result;
            while ((result = _lexer.MoveNext()) && _lexer.Token.Kind.IsHidden(hideNewLine: false))
            {
                if (!_captureTrivia || _pendingTrivia is null)
                {
                    continue;
                }

                if (_lexer.Token.Kind == TokenKind.Whitespaces)
                {
                    var text = _lexer.Token.GetText(_lexer.Source) ?? string.Empty;
                    _pendingWhitespace = _pendingWhitespace is null ? text : string.Concat(_pendingWhitespace, text);
                    continue;
                }

                if (_lexer.Token.Kind == TokenKind.Comment)
                {
                    if (_pendingWhitespace is not null)
                    {
                        _pendingTrivia.Add(new TomlSyntaxTriviaMetadata(TokenKind.Whitespaces, _pendingWhitespace));
                        _pendingWhitespace = null;
                    }

                    _pendingTrivia.Add(new TomlSyntaxTriviaMetadata(TokenKind.Comment, _lexer.Token.GetText(_lexer.Source) ?? string.Empty));
                    continue;
                }

                _pendingWhitespace = null;
            }

            _pendingWhitespace = null;

            _token = result ? _lexer.Token : new SyntaxTokenValue(TokenKind.Eof, new TextPosition(), new TextPosition());

            if (_lexer.HasErrors)
            {
                throw new TomlException(new DiagnosticsBag(_lexer.Errors));
            }
        }

        private void Consume(TokenKind expected, LexerState nextState)
        {
            if (_token.Kind != expected)
            {
                throw CreateException(CurrentSpan(), $"Expected `{expected.ToText() ?? expected.ToString()}` but was `{ToPrintable(_token)}`.");
            }

            if (_captureTrivia && expected == TokenKind.NewLine && _pendingTrivia is { Count: > 0 })
            {
                var last = _pendingTrivia[_pendingTrivia.Count - 1];
                if (last.Kind == TokenKind.Comment)
                {
                    _pendingTrivia.Add(new TomlSyntaxTriviaMetadata(TokenKind.NewLine, _token.GetText(_lexer.Source) ?? "\n"));
                }
            }

            _lexer.State = nextState;
            NextToken();
        }

        private void SetPendingEvent(TomlParseEvent parseEvent, TomlSyntaxTriviaMetadata[]? leadingTrivia = null, TomlSyntaxTriviaMetadata[]? trailingTrivia = null)
        {
            if (_hasPendingEvent)
            {
                throw new InvalidOperationException("The parser already has a pending event to return.");
            }

            _pendingEvent = parseEvent;
            _pendingLeadingTrivia = leadingTrivia;
            _pendingTrailingTrivia = trailingTrivia;
            _hasPendingEvent = true;
        }

        private TomlSyntaxTriviaMetadata[]? ExtractPendingTrivia()
        {
            if (!_captureTrivia || _pendingTrivia is null || _pendingTrivia.Count == 0)
            {
                return null;
            }

            var array = _pendingTrivia.ToArray();
            _pendingTrivia.Clear();
            return array;
        }

        private bool ProducePendingOperation()
        {
            return _pendingOperation switch
            {
                PendingOperationKind.DocumentStart => ProducePendingDocumentStart(),
                PendingOperationKind.DocumentEnd => ProducePendingDocumentEnd(),
                PendingOperationKind.TableHeaderTransition => ProducePendingTableHeaderTransition(),
                PendingOperationKind.KeyValueEntry => ProducePendingKeyValueEntry(),
                PendingOperationKind.None => false,
                _ => throw new InvalidOperationException($"Unsupported pending operation kind {_pendingOperation}."),
            };
        }

        private bool ProducePendingDocumentStart()
        {
            if (_pendingDocumentStartStage == 1)
            {
                _pendingDocumentStartStage = 0;
                _pendingOperation = PendingOperationKind.None;
                SetPendingEvent(new TomlParseEvent(TomlParseEventKind.StartTable, span: null, propertyName: null, stringValue: null, data: 0));
                return true;
            }

            _pendingOperation = PendingOperationKind.None;
            return false;
        }

        private void BeginDocumentEnd()
        {
            if (_pendingOperation == PendingOperationKind.DocumentEnd || _state == DocumentState.Ended)
            {
                return;
            }

            _pendingOperation = PendingOperationKind.DocumentEnd;
            _pendingDocumentEndStage = DocumentEndStage.Containers;
            _pendingDocumentEndRootTableClosed = false;
            _pendingDocumentEndDocumentClosed = false;
            _state = DocumentState.Terminating;
        }

        private bool ProducePendingDocumentEnd()
        {
            while (true)
            {
                switch (_pendingDocumentEndStage)
                {
                    case DocumentEndStage.Containers:
                        if (_containers.Count > 0)
                        {
                            var frame = _containers[_containers.Count - 1];
                            if (frame.Kind == ContainerKind.InlineTable && _implicitFrames.Count > frame.InlineImplicitBase)
                            {
                                _implicitFrames.RemoveAt(_implicitFrames.Count - 1);
                                SetPendingEvent(new TomlParseEvent(TomlParseEventKind.EndTable, span: CurrentSpan(), propertyName: null, stringValue: null, data: 0));
                                return true;
                            }

                            _containers.RemoveAt(_containers.Count - 1);
                            SetPendingEvent(frame.Kind == ContainerKind.Array
                                ? new TomlParseEvent(TomlParseEventKind.EndArray, span: CurrentSpan(), propertyName: null, stringValue: null, data: 0)
                                : new TomlParseEvent(TomlParseEventKind.EndTable, span: CurrentSpan(), propertyName: null, stringValue: null, data: 0));
                            return true;
                        }

                        _pendingDocumentEndStage = DocumentEndStage.ImplicitFrames;
                        continue;

                    case DocumentEndStage.ImplicitFrames:
                        if (_implicitFrames.Count > 0)
                        {
                            _implicitFrames.RemoveAt(_implicitFrames.Count - 1);
                            SetPendingEvent(new TomlParseEvent(TomlParseEventKind.EndTable, span: CurrentSpan(), propertyName: null, stringValue: null, data: 0));
                            return true;
                        }

                        _pendingDocumentEndStage = DocumentEndStage.ExplicitFrames;
                        continue;

                    case DocumentEndStage.ExplicitFrames:
                        if (_explicitFrames.Count > 0)
                        {
                            var frame = _explicitFrames[_explicitFrames.Count - 1];
                            _explicitFrames.RemoveAt(_explicitFrames.Count - 1);
                            SetPendingEvent(frame.Kind == ExplicitFrameKind.Array
                                ? new TomlParseEvent(TomlParseEventKind.EndArray, span: CurrentSpan(), propertyName: null, stringValue: null, data: 0)
                                : new TomlParseEvent(TomlParseEventKind.EndTable, span: CurrentSpan(), propertyName: null, stringValue: null, data: 0));
                            return true;
                        }

                        _pendingDocumentEndStage = DocumentEndStage.RootTable;
                        continue;

                    case DocumentEndStage.RootTable:
                        if (!_pendingDocumentEndRootTableClosed)
                        {
                            _pendingDocumentEndRootTableClosed = true;
                            SetPendingEvent(new TomlParseEvent(TomlParseEventKind.EndTable, span: CurrentSpan(), propertyName: null, stringValue: null, data: 0));
                            return true;
                        }

                        _pendingDocumentEndStage = DocumentEndStage.Document;
                        continue;

                    case DocumentEndStage.Document:
                        if (!_pendingDocumentEndDocumentClosed)
                        {
                            _pendingDocumentEndDocumentClosed = true;
                            SetPendingEvent(new TomlParseEvent(TomlParseEventKind.EndDocument, span: null, propertyName: null, stringValue: null, data: 0));
                            _pendingOperation = PendingOperationKind.None;
                            _state = DocumentState.Ended;
                            return true;
                        }

                        _pendingOperation = PendingOperationKind.None;
                        _state = DocumentState.Ended;
                        return false;

                    default:
                        throw new InvalidOperationException($"Unsupported document end stage {_pendingDocumentEndStage}.");
                }
            }
        }

        private bool ProducePendingTableHeaderTransition()
        {
            if (!_pendingTableHeaderClosingsCompleted)
            {
                if (_implicitFrames.Count > 0)
                {
                    _implicitFrames.RemoveAt(_implicitFrames.Count - 1);
                    SetPendingEvent(new TomlParseEvent(TomlParseEventKind.EndTable, span: CurrentSpan(), propertyName: null, stringValue: null, data: 0));
                    return true;
                }

                if (_explicitFrames.Count > _pendingTableHeaderExplicitPrefixLength)
                {
                    var frame = _explicitFrames[_explicitFrames.Count - 1];
                    _explicitFrames.RemoveAt(_explicitFrames.Count - 1);
                    SetPendingEvent(frame.Kind == ExplicitFrameKind.Array
                        ? new TomlParseEvent(TomlParseEventKind.EndArray, span: CurrentSpan(), propertyName: null, stringValue: null, data: 0)
                        : new TomlParseEvent(TomlParseEventKind.EndTable, span: CurrentSpan(), propertyName: null, stringValue: null, data: 0));
                    return true;
                }

                _pendingTableHeaderClosingsCompleted = true;
            }

            if (_pendingTableHeaderOpenIndex < _targetExplicitFrames.Count)
            {
                if (_pendingTableHeaderHasOpenFrame)
                {
                    var openFrame = _pendingTableHeaderOpenFrame;
                    _pendingTableHeaderHasOpenFrame = false;
                    _pendingTableHeaderOpenEmitPropertyName = true;
                    _pendingTableHeaderOpenIndex++;

                    SetPendingEvent(openFrame.Kind == ExplicitFrameKind.Array
                        ? new TomlParseEvent(TomlParseEventKind.StartArray, span: null, propertyName: null, stringValue: null, data: 0)
                        : new TomlParseEvent(TomlParseEventKind.StartTable, span: null, propertyName: null, stringValue: null, data: 0));
                    _explicitFrames.Add(openFrame);
                    return true;
                }

                var frame = _targetExplicitFrames[_pendingTableHeaderOpenIndex];
                if (frame.Kind == ExplicitFrameKind.TableArrayElement)
                {
                    _pendingTableHeaderOpenIndex++;
                    SetPendingEvent(new TomlParseEvent(TomlParseEventKind.StartTable, span: null, propertyName: null, stringValue: null, data: 0));
                    _explicitFrames.Add(frame);
                    return true;
                }

                if (_pendingTableHeaderOpenEmitPropertyName)
                {
                    if (frame.Name is not { } name)
                    {
                        throw CreateException(CurrentSpan(), "Invalid explicit frame without a name.");
                    }

                    _pendingTableHeaderOpenFrame = frame;
                    _pendingTableHeaderHasOpenFrame = true;
                    _pendingTableHeaderOpenEmitPropertyName = false;

                    var leadingTrivia = ExtractPendingTrivia();
                    SetPendingEvent(new TomlParseEvent(
                        TomlParseEventKind.PropertyName,
                        span: name.Span,
                        propertyName: _decodeScalars ? name.Value : null,
                        stringValue: null,
                        data: (ulong)name.TokenKind),
                        leadingTrivia: leadingTrivia);
                    return true;
                }

                throw CreateException(CurrentSpan(), "Invalid table header open state.");
            }

            _pendingOperation = PendingOperationKind.None;
            return false;
        }

        private bool ProducePendingKeyValueEntry()
        {
            if (!_pendingKeyValueClosingsCompleted)
            {
                var targetCount = _pendingKeyValueImplicitBaseIndex + _pendingKeyValueCommonPrefixLength;
                if (_implicitFrames.Count > targetCount)
                {
                    _implicitFrames.RemoveAt(_implicitFrames.Count - 1);
                    SetPendingEvent(new TomlParseEvent(TomlParseEventKind.EndTable, span: CurrentSpan(), propertyName: null, stringValue: null, data: 0));
                    return true;
                }

                _pendingKeyValueClosingsCompleted = true;
            }

            if (_pendingKeyValueOpenIndex < _pendingKeyValuePrefixLength)
            {
                var segment = _pathSegments[_pendingKeyValueOpenIndex];
                if (_pendingKeyValueOpenEmitPropertyName)
                {
                    _pendingKeyValueOpenEmitPropertyName = false;
                    SetPendingEvent(new TomlParseEvent(
                        TomlParseEventKind.PropertyName,
                        span: segment.Span,
                        propertyName: _decodeScalars ? segment.Value : null,
                        stringValue: null,
                        data: (ulong)segment.TokenKind));
                    return true;
                }

                _pendingKeyValueOpenEmitPropertyName = true;
                _pendingKeyValueOpenIndex++;
                _implicitFrames.Add(segment);
                SetPendingEvent(new TomlParseEvent(TomlParseEventKind.StartTable, span: null, propertyName: null, stringValue: null, data: 0));
                return true;
            }

            if (!_pendingKeyValueLeafEmitted)
            {
                _pendingKeyValueLeafEmitted = true;
                var leaf = _pathSegments[_pathSegments.Count - 1];
                var leadingTrivia = ExtractPendingTrivia();
                SetPendingEvent(new TomlParseEvent(
                    TomlParseEventKind.PropertyName,
                    span: leaf.Span,
                    propertyName: _decodeScalars ? leaf.Value : null,
                    stringValue: null,
                    data: (ulong)leaf.TokenKind),
                    leadingTrivia: leadingTrivia);
                return true;
            }

            Consume(TokenKind.Equal, LexerState.Value);
            _pendingOperation = PendingOperationKind.None;
            return false;
        }

        private void SkipNewLines(LexerState lexerState)
        {
            while (_token.Kind == TokenKind.NewLine)
            {
                Consume(TokenKind.NewLine, lexerState);
            }
        }

        private bool ProduceContainer()
        {
            var frame = _containers[_containers.Count - 1];
            return frame.Kind switch
            {
                ContainerKind.Array => ProduceArray(ref frame),
                ContainerKind.InlineTable => ProduceInlineTable(ref frame),
                _ => throw new InvalidOperationException($"Unsupported container kind {frame.Kind}."),
            };
        }

        private bool ProduceStatement()
        {
            while (true)
            {
                SkipNewLines(LexerState.Key);

                if (_token.Kind == TokenKind.Eof)
                {
                    BeginDocumentEnd();
                    return true;
                }

                if (_token.Kind == TokenKind.OpenBracket || _token.Kind == TokenKind.OpenBracketDouble)
                {
                    ParseTableHeader(isTableArray: _token.Kind == TokenKind.OpenBracketDouble);
                    return true;
                }

                if (_token.Kind == TokenKind.BasicKey || _token.Kind.IsString())
                {
                    BeginKeyValueEntry(implicitBaseIndex: 0);
                    _state = DocumentState.ExpectValue;
                    return true;
                }

                throw CreateException(CurrentSpan(), $"Unexpected token `{ToPrintable(_token)}` while parsing TOML document.");
            }
        }

        private bool ProduceKeyValueValue()
        {
            if (_token.Kind == TokenKind.NewLine || _token.Kind == TokenKind.Eof)
            {
                throw CreateException(CurrentSpan(), "Missing value after `=`.");
            }

            ProduceValueEvent(_token.Start, nextStateAfterScalar: LexerState.Key);
            _state = DocumentState.AfterValue;
            return true;
        }

        private bool ProduceAfterValue()
        {
            if (_token.Kind == TokenKind.NewLine)
            {
                Consume(TokenKind.NewLine, LexerState.Key);
                _state = DocumentState.Statement;
                return true;
            }

            if (_token.Kind == TokenKind.Eof)
            {
                _state = DocumentState.Statement;
                return true;
            }

            throw CreateException(CurrentSpan(), $"Expected end-of-line after value but was `{ToPrintable(_token)}`.");
        }

        private bool ProduceArray(ref ContainerFrame frame)
        {
            var frameIndex = _containers.Count - 1;
            while (true)
            {
                SkipNewLines(LexerState.Value);

                if (_token.Kind == TokenKind.Eof)
                {
                    throw CreateException(CurrentSpan(), "Unexpected end-of-file while parsing array.");
                }

                if (frame.ArrayState == ArrayState.ExpectValueOrEnd)
                {
                    if (_token.Kind == TokenKind.CloseBracket)
                    {
                        Consume(TokenKind.CloseBracket, DetermineLexerStateAfterContainerClose());
                        _containers.RemoveAt(_containers.Count - 1);
                        SetPendingEvent(new TomlParseEvent(TomlParseEventKind.EndArray, span: CurrentSpan(), propertyName: null, stringValue: null, data: 0));
                        return true;
                    }

                    if (_token.Kind == TokenKind.Comma)
                    {
                        throw CreateException(CurrentSpan(), "Missing value in array.");
                    }

                    ProduceValueEvent(_token.Start, nextStateAfterScalar: LexerState.Value);
                    frame.ArrayState = ArrayState.AfterValue;
                    _containers[frameIndex] = frame;
                    return true;
                }

                // AfterValue
                SkipNewLines(LexerState.Value);

                if (_token.Kind == TokenKind.Comma)
                {
                    Consume(TokenKind.Comma, LexerState.Value);
                    SkipNewLines(LexerState.Value);
                    if (_token.Kind == TokenKind.CloseBracket)
                    {
                        Consume(TokenKind.CloseBracket, DetermineLexerStateAfterContainerClose());
                        _containers.RemoveAt(_containers.Count - 1);
                        SetPendingEvent(new TomlParseEvent(TomlParseEventKind.EndArray, span: CurrentSpan(), propertyName: null, stringValue: null, data: 0));
                        return true;
                    }

                    frame.ArrayState = ArrayState.ExpectValueOrEnd;
                    _containers[frameIndex] = frame;
                    continue;
                }

                if (_token.Kind == TokenKind.CloseBracket)
                {
                    Consume(TokenKind.CloseBracket, DetermineLexerStateAfterContainerClose());
                    _containers.RemoveAt(_containers.Count - 1);
                    SetPendingEvent(new TomlParseEvent(TomlParseEventKind.EndArray, span: CurrentSpan(), propertyName: null, stringValue: null, data: 0));
                    return true;
                }

                throw CreateException(CurrentSpan(), $"Expected `,` or `]` while parsing array but was `{ToPrintable(_token)}`.");
            }
        }

        private bool ProduceInlineTable(ref ContainerFrame frame)
        {
            var frameIndex = _containers.Count - 1;
            while (true)
            {
                if (frame.InlineTableState != InlineTableState.ExpectValue)
                {
                    SkipNewLines(LexerState.Key);
                }
                else
                {
                    SkipNewLines(LexerState.Value);
                }

                if (_token.Kind == TokenKind.Eof)
                {
                    throw CreateException(CurrentSpan(), "Unexpected end-of-file while parsing inline table.");
                }

                if (frame.InlineTableState == InlineTableState.ExpectKeyOrEnd)
                {
                    if (_token.Kind == TokenKind.CloseBrace)
                    {
                        if (_implicitFrames.Count > frame.InlineImplicitBase)
                        {
                            _implicitFrames.RemoveAt(_implicitFrames.Count - 1);
                            SetPendingEvent(new TomlParseEvent(TomlParseEventKind.EndTable, span: CurrentSpan(), propertyName: null, stringValue: null, data: 0));
                            return true;
                        }

                        Consume(TokenKind.CloseBrace, DetermineLexerStateAfterContainerClose());
                        _containers.RemoveAt(_containers.Count - 1);
                        SetPendingEvent(new TomlParseEvent(TomlParseEventKind.EndTable, span: CurrentSpan(), propertyName: null, stringValue: null, data: 0));
                        return true;
                    }

                    if (_token.Kind != TokenKind.BasicKey && !_token.Kind.IsString())
                    {
                        throw CreateException(CurrentSpan(), $"Expected a key or `}}` while parsing inline table but was `{ToPrintable(_token)}`.");
                    }

                    BeginKeyValueEntry(implicitBaseIndex: frame.InlineImplicitBase);
                    frame.InlineTableState = InlineTableState.ExpectValue;
                    _containers[frameIndex] = frame;
                    return true;
                }

                if (frame.InlineTableState == InlineTableState.ExpectValue)
                {
                    if (_token.Kind == TokenKind.Comma || _token.Kind == TokenKind.CloseBrace || _token.Kind == TokenKind.Eof)
                    {
                        throw CreateException(CurrentSpan(), "Missing value after `=` in inline table.");
                    }

                    ProduceValueEvent(_token.Start, nextStateAfterScalar: LexerState.Key);
                    frame.InlineTableState = InlineTableState.AfterValue;
                    _containers[frameIndex] = frame;
                    return true;
                }

                // AfterValue
                SkipNewLines(LexerState.Key);
                if (_token.Kind == TokenKind.Comma)
                {
                    Consume(TokenKind.Comma, LexerState.Key);
                    frame.InlineTableState = InlineTableState.ExpectKeyOrEnd;
                    _containers[frameIndex] = frame;
                    continue;
                }

                if (_token.Kind == TokenKind.CloseBrace)
                {
                    frame.InlineTableState = InlineTableState.ExpectKeyOrEnd;
                    _containers[frameIndex] = frame;
                    continue;
                }

                throw CreateException(CurrentSpan(), $"Expected `,` or `}}` while parsing inline table but was `{ToPrintable(_token)}`.");
            }
        }

        private LexerState DetermineLexerStateAfterContainerClose()
        {
            if (_containers.Count < 2)
            {
                return LexerState.Key;
            }

            var parent = _containers[_containers.Count - 2];
            return parent.Kind == ContainerKind.Array ? LexerState.Value : LexerState.Key;
        }

        private void ParseTableHeader(bool isTableArray)
        {
            Consume(isTableArray ? TokenKind.OpenBracketDouble : TokenKind.OpenBracket, LexerState.Key);

            _pathSegments.Clear();
            ParseKeyPath(_pathSegments, out _);
            if (_pathSegments.Count == 0)
            {
                throw CreateException(CurrentSpan(), "Invalid table header.");
            }

            Consume(isTableArray ? TokenKind.CloseBracketDouble : TokenKind.CloseBracket, LexerState.Key);

            if (_token.Kind == TokenKind.NewLine)
            {
                Consume(TokenKind.NewLine, LexerState.Key);
            }
            else if (_token.Kind != TokenKind.Eof)
            {
                throw CreateException(CurrentSpan(), $"Expected end-of-line after table header but was `{ToPrintable(_token)}`.");
            }

            _targetExplicitFrames.Clear();
            BuildExplicitTargetFrames(_pathSegments, isTableArray, _explicitFrames, _targetExplicitFrames);
            var prefixLength = GetExplicitCommonPrefixLength(_targetExplicitFrames, isTableArray);

            _pendingTableHeaderExplicitPrefixLength = prefixLength;
            _pendingTableHeaderOpenIndex = prefixLength;
            _pendingTableHeaderOpenEmitPropertyName = true;
            _pendingTableHeaderHasOpenFrame = false;
            _pendingTableHeaderClosingsCompleted = false;
            _pendingOperation = PendingOperationKind.TableHeaderTransition;
        }

        private void BeginKeyValueEntry(int implicitBaseIndex)
        {
            _pathSegments.Clear();
            ParseKeyPath(_pathSegments, out _);
            if (_pathSegments.Count == 0)
            {
                throw CreateException(CurrentSpan(), "Invalid key/value entry.");
            }

            if (_token.Kind != TokenKind.Equal)
            {
                throw CreateException(CurrentSpan(), $"Expected `=` after key but was `{ToPrintable(_token)}`.");
            }

            var prefixLength = _pathSegments.Count - 1;
            var common = 0;
            var currentCount = _implicitFrames.Count - implicitBaseIndex;
            while (common < currentCount && common < prefixLength &&
                   _implicitFrames[implicitBaseIndex + common].Hash == _pathSegments[common].Hash)
            {
                common++;
            }

            _pendingKeyValueImplicitBaseIndex = implicitBaseIndex;
            _pendingKeyValuePrefixLength = prefixLength;
            _pendingKeyValueCommonPrefixLength = common;
            _pendingKeyValueOpenIndex = common;
            _pendingKeyValueOpenEmitPropertyName = true;
            _pendingKeyValueLeafEmitted = false;
            _pendingKeyValueClosingsCompleted = false;
            _pendingOperation = PendingOperationKind.KeyValueEntry;
        }

        private static void BuildExplicitTargetFrames(List<KeySegment> path, bool isTableArray, List<ExplicitFrame> currentFrames, List<ExplicitFrame> frames)
        {
            var explicitIndex = 0;
            var reuse = true;

            for (var i = 0; i < path.Count - 1; i++)
            {
                var segment = path[i];
                if (reuse && TryConsumeCurrentFrame(currentFrames, ref explicitIndex, segment, frames))
                {
                    continue;
                }

                reuse = false;
                frames.Add(new ExplicitFrame(ExplicitFrameKind.Table, segment));
            }

            var last = path[path.Count - 1];
            if (isTableArray)
            {
                frames.Add(new ExplicitFrame(ExplicitFrameKind.Array, last));
                frames.Add(new ExplicitFrame(ExplicitFrameKind.TableArrayElement, name: null));
                return;
            }

            frames.Add(new ExplicitFrame(ExplicitFrameKind.Table, last));
        }

        private static bool TryConsumeCurrentFrame(List<ExplicitFrame> currentFrames, ref int explicitIndex, KeySegment segment, List<ExplicitFrame> targetFrames)
        {
            if (explicitIndex >= currentFrames.Count)
            {
                return false;
            }

            var current = currentFrames[explicitIndex];
            if (current.Kind == ExplicitFrameKind.Table && current.Name is { } tableName && tableName.Hash == segment.Hash)
            {
                targetFrames.Add(current);
                explicitIndex++;
                return true;
            }

            if (current.Kind == ExplicitFrameKind.Array && current.Name is { } arrayName && arrayName.Hash == segment.Hash)
            {
                var elementIndex = explicitIndex + 1;
                if (elementIndex < currentFrames.Count && currentFrames[elementIndex].Kind == ExplicitFrameKind.TableArrayElement)
                {
                    targetFrames.Add(current);
                    targetFrames.Add(currentFrames[elementIndex]);
                    explicitIndex += 2;
                    return true;
                }
            }

            return false;
        }

        private int GetExplicitCommonPrefixLength(List<ExplicitFrame> targetFrames, bool isTableArray)
        {
            var max = Math.Min(_explicitFrames.Count, targetFrames.Count);
            var common = 0;
            while (common < max && FramesEqual(_explicitFrames[common], targetFrames[common]))
            {
                common++;
            }

            if (isTableArray && targetFrames.Count >= 2)
            {
                if (common == targetFrames.Count && targetFrames[targetFrames.Count - 1].Kind == ExplicitFrameKind.TableArrayElement)
                {
                    common = targetFrames.Count - 1;
                }
                else if (common >= targetFrames.Count - 1 && FramesEqualSequence(_explicitFrames, targetFrames, targetFrames.Count - 1))
                {
                    common = targetFrames.Count - 1;
                }
            }

            return common;
        }

        private static bool FramesEqual(ExplicitFrame left, ExplicitFrame right)
        {
            if (left.Kind != right.Kind)
            {
                return false;
            }

            if (left.Kind == ExplicitFrameKind.TableArrayElement)
            {
                return true;
            }

            if (left.Name is not { } leftName || right.Name is not { } rightName)
            {
                return false;
            }

            return leftName.Hash == rightName.Hash;
        }

        private static bool FramesEqualSequence(List<ExplicitFrame> current, List<ExplicitFrame> target, int length)
        {
            if (current.Count < length || target.Count < length)
            {
                return false;
            }

            for (var i = 0; i < length; i++)
            {
                if (!FramesEqual(current[i], target[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private void ProduceValueEvent(TextPosition spanStart, LexerState nextStateAfterScalar)
        {
            if (_token.Kind == TokenKind.OpenBracket)
            {
                var span = new TomlSourceSpan(_lexer.Source.SourcePath,
                    new TomlTextPosition(spanStart.Offset, spanStart.Line, spanStart.Column),
                    new TomlTextPosition(_token.End.Offset, _token.End.Line, _token.End.Column));
                SetPendingEvent(new TomlParseEvent(TomlParseEventKind.StartArray, span: span, propertyName: null, stringValue: null, data: 0));
                Consume(TokenKind.OpenBracket, LexerState.Value);
                _containers.Add(new ContainerFrame(ContainerKind.Array, inlineImplicitBase: 0));
                return;
            }

            if (_token.Kind == TokenKind.OpenBrace)
            {
                var span = new TomlSourceSpan(_lexer.Source.SourcePath,
                    new TomlTextPosition(spanStart.Offset, spanStart.Line, spanStart.Column),
                    new TomlTextPosition(_token.End.Offset, _token.End.Line, _token.End.Column));
                SetPendingEvent(new TomlParseEvent(TomlParseEventKind.StartTable, span: span, propertyName: null, stringValue: null, data: 0));
                Consume(TokenKind.OpenBrace, LexerState.Key);
                _containers.Add(new ContainerFrame(ContainerKind.InlineTable, inlineImplicitBase: _implicitFrames.Count));
                return;
            }

            var eventSpan = new TomlSourceSpan(_lexer.Source.SourcePath,
                new TomlTextPosition(spanStart.Offset, spanStart.Line, spanStart.Column),
                new TomlTextPosition(_token.End.Offset, _token.End.Line, _token.End.Column));

            if (_token.Kind.IsString())
            {
                var tokenKind = _token.Kind;
                var text = _decodeScalars ? (_token.StringValue ?? string.Empty) : null;
                var parseEvent = new TomlParseEvent(TomlParseEventKind.String, span: eventSpan, propertyName: null, stringValue: text, data: (ulong)tokenKind);
                Consume(tokenKind, nextStateAfterScalar);
                var trailingTrivia = ExtractPendingTrivia();
                SetPendingEvent(parseEvent, trailingTrivia: trailingTrivia);
                return;
            }

            if (_token.Kind.IsInteger())
            {
                var tokenKind = _token.Kind;
                var parseEvent = new TomlParseEvent(TomlParseEventKind.Integer, span: eventSpan, propertyName: null, stringValue: null, data: _token.Data);
                Consume(tokenKind, nextStateAfterScalar);
                var trailingTrivia = ExtractPendingTrivia();
                SetPendingEvent(parseEvent, trailingTrivia: trailingTrivia);
                return;
            }

            if (_token.Kind.IsFloat())
            {
                var tokenKind = _token.Kind;
                var parseEvent = new TomlParseEvent(TomlParseEventKind.Float, span: eventSpan, propertyName: null, stringValue: null, data: _token.Data);
                Consume(tokenKind, nextStateAfterScalar);
                var trailingTrivia = ExtractPendingTrivia();
                SetPendingEvent(parseEvent, trailingTrivia: trailingTrivia);
                return;
            }

            if (_token.Kind == TokenKind.True || _token.Kind == TokenKind.False)
            {
                var tokenKind = _token.Kind;
                var parseEvent = new TomlParseEvent(TomlParseEventKind.Boolean, span: eventSpan, propertyName: null, stringValue: null, data: _token.Data);
                Consume(tokenKind, nextStateAfterScalar);
                var trailingTrivia = ExtractPendingTrivia();
                SetPendingEvent(parseEvent, trailingTrivia: trailingTrivia);
                return;
            }

            if (_token.Kind.IsDateTime())
            {
                var tokenKind = _token.Kind;
                var literal = _token.StringValue ?? _token.GetText(_lexer.Source) ?? string.Empty;
                var parseEvent = new TomlParseEvent(TomlParseEventKind.DateTime, span: eventSpan, propertyName: null, stringValue: literal, data: 0);
                Consume(tokenKind, nextStateAfterScalar);
                var trailingTrivia = ExtractPendingTrivia();
                SetPendingEvent(parseEvent, trailingTrivia: trailingTrivia);
                return;
            }

            throw CreateException(CurrentSpan(), $"Unexpected token `{ToPrintable(_token)}` while parsing a value.");
        }

        private void ParseKeyPath(List<KeySegment> segments, out TomlSourceSpan leafSpan)
        {
            leafSpan = ParseKeySegment(out var firstSegment);
            segments.Add(firstSegment);
            while (_token.Kind == TokenKind.Dot)
            {
                Consume(TokenKind.Dot, LexerState.Key);
                leafSpan = ParseKeySegment(out var segment);
                segments.Add(segment);
            }
        }

        private TomlSourceSpan ParseKeySegment(out KeySegment segment)
        {
            if (_token.Kind == TokenKind.BasicKey)
            {
                var span = CurrentSpan();
                var hash = ComputeKeySegmentHash(TokenKind.BasicKey, _token.Start, _token.End);
                var text = _decodeScalars ? _token.GetText(_lexer.Source) : null;
                Consume(TokenKind.BasicKey, LexerState.Key);
                segment = new KeySegment(TokenKind.BasicKey, span, hash, text);
                return span;
            }

            if (_token.Kind.IsString())
            {
                var span = CurrentSpan();
                var tokenKind = _token.Kind;
                var hash = ComputeKeySegmentHash(tokenKind, _token.Start, _token.End);
                var text = _decodeScalars ? (_token.StringValue ?? string.Empty) : null;
                Consume(_token.Kind, LexerState.Key);
                segment = new KeySegment(tokenKind, span, hash, text);
                return span;
            }

            throw CreateException(CurrentSpan(), $"Unexpected token `{ToPrintable(_token)}` while parsing a key.");
        }

        private ulong ComputeKeySegmentHash(TokenKind tokenKind, TextPosition start, TextPosition end)
        {
            var iterator = _lexer.Source.GetIterator();
            var startOffset = start.Offset;
            var endExclusive = end.Offset + 1;

            if (tokenKind == TokenKind.BasicKey)
            {
                return HashRaw(ref iterator, startOffset, endExclusive);
            }

            if (tokenKind == TokenKind.StringLiteral)
            {
                // Skip the surrounding apostrophes.
                return HashRaw(ref iterator, startOffset + 1, end.Offset);
            }

            if (tokenKind == TokenKind.String)
            {
                // Skip the surrounding quotation marks and decode escapes.
                return HashBasicString(ref iterator, startOffset + 1, end.Offset);
            }

            throw CreateException(CurrentSpan(), $"Unexpected key token kind `{tokenKind}`.");
        }

        private static ulong HashRaw(ref TCharReader iterator, int startOffset, int endExclusive)
        {
            var position = startOffset;
            var hash = 14695981039346656037UL;
            while (position < endExclusive)
            {
                var next = iterator.TryGetNext(ref position);
                if (!next.HasValue)
                {
                    break;
                }

                hash = HashAdd(hash, next.Value.Code);
            }

            return hash;
        }

        private ulong HashBasicString(ref TCharReader iterator, int startOffset, int endExclusive)
        {
            var position = startOffset;
            var hash = 14695981039346656037UL;
            while (position < endExclusive)
            {
                var next = iterator.TryGetNext(ref position);
                if (!next.HasValue)
                {
                    break;
                }

                var c = next.Value.Code;
                if (c != '\\')
                {
                    hash = HashAdd(hash, c);
                    continue;
                }

                var escape = iterator.TryGetNext(ref position);
                if (!escape.HasValue)
                {
                    throw CreateException(CurrentSpan(), "Invalid escape sequence in key.");
                }

                hash = HashAdd(hash, DecodeEscape(ref iterator, ref position, endExclusive, escape.Value.Code));
            }

            return hash;
        }

        private int DecodeEscape(ref TCharReader iterator, ref int position, int endExclusive, int escapeCode)
        {
            switch (escapeCode)
            {
                case '"': return '"';
                case '\\': return '\\';
                case 'b': return '\b';
                case 'e': return 0x1B;
                case 'f': return '\f';
                case 'n': return '\n';
                case 'r': return '\r';
                case 't': return '\t';
                case 'x':
                    return ReadHex(ref iterator, ref position, endExclusive, digits: 2);
                case 'u':
                    return ReadHex(ref iterator, ref position, endExclusive, digits: 4);
                case 'U':
                    return ReadHex(ref iterator, ref position, endExclusive, digits: 8);
                default:
                    throw CreateException(CurrentSpan(), $"Unexpected escape character `{(char)escapeCode}` in key.");
            }
        }

        private int ReadHex(ref TCharReader iterator, ref int position, int endExclusive, int digits)
        {
            var value = 0;
            for (var i = 0; i < digits; i++)
            {
                if (position >= endExclusive)
                {
                    throw CreateException(CurrentSpan(), "Incomplete hexadecimal escape sequence in key.");
                }

                var next = iterator.TryGetNext(ref position);
                if (!next.HasValue)
                {
                    throw CreateException(CurrentSpan(), "Incomplete hexadecimal escape sequence in key.");
                }

                var c = next.Value.Code;
                value = (value << 4) | HexValue(c);
            }

            return value;
        }

        private static int HexValue(int c)
        {
            if (c is >= '0' and <= '9') return c - '0';
            if (c is >= 'a' and <= 'f') return 10 + (c - 'a');
            if (c is >= 'A' and <= 'F') return 10 + (c - 'A');
            throw new InvalidOperationException($"Invalid hex digit `{(char)c}`.");
        }

        private static ulong HashAdd(ulong hash, int codePoint)
        {
            hash ^= unchecked((uint)codePoint);
            return hash * 1099511628211UL;
        }

        private TomlException CreateException(TomlSourceSpan span, string message) => new TomlException(span, message);

        private TomlSourceSpan CurrentSpan()
            => new TomlSourceSpan(_lexer.Source.SourcePath,
                new TomlTextPosition(_token.Start.Offset, _token.Start.Line, _token.Start.Column),
                new TomlTextPosition(_token.End.Offset, _token.End.Line, _token.End.Column));

        private string ToPrintable(SyntaxTokenValue token)
        {
            var span = new TomlSourceSpan(_lexer.Source.SourcePath,
                new TomlTextPosition(token.Start.Offset, token.Start.Line, token.Start.Column),
                new TomlTextPosition(token.End.Offset, token.End.Line, token.End.Column));
            return _lexer.Source.GetString(span.Offset, span.Length).ToPrintableString() ?? token.Kind.ToString();
        }

        private void RecordDiagnostics(TomlException exception)
        {
            if (_diagnostics is null)
            {
                return;
            }

            if (exception.Diagnostics.Count > 0)
            {
                _diagnostics.AddRange(exception.Diagnostics);
                return;
            }

            if (exception.Span is { } span)
            {
                _diagnostics.Error(new SourceSpan(span.SourceName, new TextPosition(span.Start.Offset, span.Start.Line, span.Start.Column), new TextPosition(span.End.Offset, span.End.Line, span.End.Column)), exception.Message);
            }
        }

        private enum PendingOperationKind
        {
            None = 0,
            DocumentStart = 1,
            KeyValueEntry = 2,
            TableHeaderTransition = 3,
            DocumentEnd = 4,
        }

        private enum DocumentEndStage
        {
            Containers = 0,
            ImplicitFrames = 1,
            ExplicitFrames = 2,
            RootTable = 3,
            Document = 4,
        }

        private enum DocumentState
        {
            NotStarted = 0,
            Statement = 1,
            ExpectValue = 2,
            AfterValue = 3,
            Terminating = 4,
            Ended = 5,
        }

        private enum ContainerKind
        {
            Array = 0,
            InlineTable = 1,
        }

        private enum ArrayState
        {
            ExpectValueOrEnd = 0,
            AfterValue = 1,
        }

        private enum InlineTableState
        {
            ExpectKeyOrEnd = 0,
            ExpectValue = 1,
            AfterValue = 2,
        }

        private readonly struct KeySegment
        {
            public KeySegment(TokenKind tokenKind, TomlSourceSpan span, ulong hash, string? value)
            {
                TokenKind = tokenKind;
                Span = span;
                Hash = hash;
                Value = value;
            }

            public TokenKind TokenKind { get; }

            public TomlSourceSpan Span { get; }

            public ulong Hash { get; }

            public string? Value { get; }
        }

        private struct ContainerFrame
        {
            public ContainerFrame(ContainerKind kind, int inlineImplicitBase)
            {
                Kind = kind;
                InlineImplicitBase = inlineImplicitBase;
                ArrayState = ArrayState.ExpectValueOrEnd;
                InlineTableState = InlineTableState.ExpectKeyOrEnd;
            }

            public ContainerKind Kind { get; }

            public int InlineImplicitBase { get; }

            public ArrayState ArrayState { get; set; }

            public InlineTableState InlineTableState { get; set; }
        }

        private enum ExplicitFrameKind
        {
            Table = 0,
            Array = 1,
            TableArrayElement = 2,
        }

        private readonly struct ExplicitFrame
        {
            public ExplicitFrame(ExplicitFrameKind kind, KeySegment? name)
            {
                Kind = kind;
                Name = name;
            }

            public ExplicitFrameKind Kind { get; }

            public KeySegment? Name { get; }
        }

        private sealed class PendingQueue
        {
            private TomlParseEvent[] _buffer;
            private int _head;
            private int _count;

            public PendingQueue()
            {
                _buffer = new TomlParseEvent[16];
                _head = 0;
                _count = 0;
            }

            public void Enqueue(TomlParseEvent value)
            {
                if (_count == _buffer.Length)
                {
                    Grow();
                }

                var index = (_head + _count) % _buffer.Length;
                _buffer[index] = value;
                _count++;
            }

            public bool TryDequeue(out TomlParseEvent value)
            {
                if (_count == 0)
                {
                    value = default;
                    return false;
                }

                value = _buffer[_head];
                _head = (_head + 1) % _buffer.Length;
                _count--;
                return true;
            }

            private void Grow()
            {
                var newBuffer = new TomlParseEvent[_buffer.Length * 2];
                for (var i = 0; i < _count; i++)
                {
                    newBuffer[i] = _buffer[(_head + i) % _buffer.Length];
                }

                _buffer = newBuffer;
                _head = 0;
            }
        }

        private readonly record struct TriviaAttachment(TomlSyntaxTriviaMetadata[]? Leading, TomlSyntaxTriviaMetadata[]? Trailing);

        private sealed class TriviaQueue
        {
            private TriviaAttachment[] _buffer;
            private int _head;
            private int _count;

            public TriviaQueue()
            {
                _buffer = new TriviaAttachment[16];
                _head = 0;
                _count = 0;
            }

            public void Enqueue(TriviaAttachment value)
            {
                if (_count == _buffer.Length)
                {
                    Grow();
                }

                var index = (_head + _count) % _buffer.Length;
                _buffer[index] = value;
                _count++;
            }

            public bool TryDequeue(out TriviaAttachment value)
            {
                if (_count == 0)
                {
                    value = default;
                    return false;
                }

                value = _buffer[_head];
                _head = (_head + 1) % _buffer.Length;
                _count--;
                return true;
            }

            private void Grow()
            {
                var newBuffer = new TriviaAttachment[_buffer.Length * 2];
                for (var i = 0; i < _count; i++)
                {
                    newBuffer[i] = _buffer[(_head + i) % _buffer.Length];
                }

                _buffer = newBuffer;
                _head = 0;
            }
        }
    }
}
