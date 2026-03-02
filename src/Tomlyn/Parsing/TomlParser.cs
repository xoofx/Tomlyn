using System;
using System.Collections.Generic;
using System.IO;
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

        var core = new ParserCore<StringSourceView, StringCharacterIterator>(sourceView, parserOptions.Mode, diagnostics);
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

        var core = new ParserCore<StringSourceView, StringCharacterIterator>(sourceView, parserOptions.Mode, diagnostics);
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
        var sourceView = new StringUtf8SourceView(bytes, effectiveOptions.SourceName ?? string.Empty);
        var parserOptions = new TomlParserOptions();
        DiagnosticsBag? diagnostics = null;
        if (parserOptions.Mode == TomlParserMode.Tolerant)
        {
            diagnostics = new DiagnosticsBag();
        }

        var core = new ParserCore<StringUtf8SourceView, StringCharacterUtf8Iterator>(sourceView, parserOptions.Mode, diagnostics);
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
        var sourceView = new StringUtf8SourceView(bytes, effectiveOptions.SourceName ?? string.Empty);
        DiagnosticsBag? diagnostics = null;
        if (parserOptions.Mode == TomlParserMode.Tolerant)
        {
            diagnostics = new DiagnosticsBag();
        }

        var core = new ParserCore<StringUtf8SourceView, StringCharacterUtf8Iterator>(sourceView, parserOptions.Mode, diagnostics);
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

    internal string? GetText(SourceSpan span) => _core.GetText(span);

    private interface IParserCore
    {
        bool MoveNext(out TomlParseEvent parseEvent);

        string? GetText(SourceSpan span);
    }

    private sealed class ParserCore<TSourceView, TCharReader> : IParserCore
        where TSourceView : struct, ISourceView<TCharReader>
        where TCharReader : struct, CharacterIterator
    {
        private readonly TomlParserMode _mode;
        private readonly DiagnosticsBag? _diagnostics;
        private readonly Lexer<TSourceView, TCharReader> _lexer;
        private SyntaxTokenValue _token;
        private bool _initialized;
        private bool _terminatedDueToError;

        private readonly PendingQueue _pending;
        private readonly List<string> _implicitFrames;
        private readonly List<ExplicitFrame> _explicitFrames;
        private readonly List<string> _pathSegments;
        private readonly List<ExplicitFrame> _targetExplicitFrames;
        private readonly List<ContainerFrame> _containers;

        private DocumentState _state;
        private TextPosition _valueSpanStart;

        public ParserCore(TSourceView sourceView, TomlParserMode mode, DiagnosticsBag? diagnostics)
        {
            _mode = mode;
            _diagnostics = diagnostics;
            _lexer = new Lexer<TSourceView, TCharReader>(sourceView)
            {
                State = LexerState.Key,
                DecodeScalars = false,
            };
            _token = default;
            _initialized = false;
            _terminatedDueToError = false;
            _pending = new PendingQueue();

            _implicitFrames = new List<string>(capacity: 8);
            _explicitFrames = new List<ExplicitFrame>(capacity: 16);
            _pathSegments = new List<string>(capacity: 8);
            _targetExplicitFrames = new List<ExplicitFrame>(capacity: 16);
            _containers = new List<ContainerFrame>(capacity: 8);

            _state = DocumentState.NotStarted;
            _valueSpanStart = default;
        }

        public string? GetText(SourceSpan span) => _lexer.Source.GetString(span.Offset, span.Length);

        public bool MoveNext(out TomlParseEvent parseEvent)
        {
            if (_pending.TryDequeue(out parseEvent))
            {
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
                        TerminateStream();
                    }

                    producedNext = true;
                }

                if (!producedNext)
                {
                    break;
                }

                if (_pending.TryDequeue(out parseEvent))
                {
                    return true;
                }
            }

            parseEvent = default;
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
                _pending.Enqueue(new TomlParseEvent(TomlParseEventKind.StartDocument, span: null, propertyName: null, stringValue: null, data: 0));
                _pending.Enqueue(new TomlParseEvent(TomlParseEventKind.StartTable, span: null, propertyName: null, stringValue: null, data: 0));
                _state = DocumentState.Statement;
                EnsureInitialized();
                return true;
            }

            EnsureInitialized();

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
            }

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

            _lexer.State = nextState;
            NextToken();
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
                    CloseImplicitFrames(baseIndex: 0);
                    CloseExplicitFrames(targetPrefixLength: 0);

                    _pending.Enqueue(new TomlParseEvent(TomlParseEventKind.EndTable, span: null, propertyName: null, stringValue: null, data: 0));
                    _pending.Enqueue(new TomlParseEvent(TomlParseEventKind.EndDocument, span: null, propertyName: null, stringValue: null, data: 0));
                    _state = DocumentState.Ended;
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
                        _pending.Enqueue(new TomlParseEvent(TomlParseEventKind.EndArray, span: null, propertyName: null, stringValue: null, data: 0));
                        return true;
                    }

                    if (_token.Kind == TokenKind.Comma)
                    {
                        throw CreateException(CurrentSpan(), "Missing value in array.");
                    }

                    ProduceValueEvent(_token.Start, nextStateAfterScalar: LexerState.Value);
                    frame.ArrayState = ArrayState.AfterValue;
                    _containers[_containers.Count - 1] = frame;
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
                        _pending.Enqueue(new TomlParseEvent(TomlParseEventKind.EndArray, span: null, propertyName: null, stringValue: null, data: 0));
                        return true;
                    }

                    frame.ArrayState = ArrayState.ExpectValueOrEnd;
                    _containers[_containers.Count - 1] = frame;
                    continue;
                }

                if (_token.Kind == TokenKind.CloseBracket)
                {
                    Consume(TokenKind.CloseBracket, DetermineLexerStateAfterContainerClose());
                    _containers.RemoveAt(_containers.Count - 1);
                    _pending.Enqueue(new TomlParseEvent(TomlParseEventKind.EndArray, span: null, propertyName: null, stringValue: null, data: 0));
                    return true;
                }

                throw CreateException(CurrentSpan(), $"Expected `,` or `]` while parsing array but was `{ToPrintable(_token)}`.");
            }
        }

        private bool ProduceInlineTable(ref ContainerFrame frame)
        {
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
                            _pending.Enqueue(new TomlParseEvent(TomlParseEventKind.EndTable, span: null, propertyName: null, stringValue: null, data: 0));
                            return true;
                        }

                        Consume(TokenKind.CloseBrace, DetermineLexerStateAfterContainerClose());
                        _containers.RemoveAt(_containers.Count - 1);
                        _pending.Enqueue(new TomlParseEvent(TomlParseEventKind.EndTable, span: null, propertyName: null, stringValue: null, data: 0));
                        return true;
                    }

                    if (_token.Kind != TokenKind.BasicKey && !_token.Kind.IsString())
                    {
                        throw CreateException(CurrentSpan(), $"Expected a key or `}}` while parsing inline table but was `{ToPrintable(_token)}`.");
                    }

                    BeginKeyValueEntry(implicitBaseIndex: frame.InlineImplicitBase);
                    frame.InlineTableState = InlineTableState.ExpectValue;
                    _containers[_containers.Count - 1] = frame;
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
                    _containers[_containers.Count - 1] = frame;
                    return true;
                }

                // AfterValue
                SkipNewLines(LexerState.Key);
                if (_token.Kind == TokenKind.Comma)
                {
                    Consume(TokenKind.Comma, LexerState.Key);
                    frame.InlineTableState = InlineTableState.ExpectKeyOrEnd;
                    _containers[_containers.Count - 1] = frame;
                    continue;
                }

                if (_token.Kind == TokenKind.CloseBrace)
                {
                    frame.InlineTableState = InlineTableState.ExpectKeyOrEnd;
                    _containers[_containers.Count - 1] = frame;
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
            ParseKeyPath(_pathSegments);
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

            CloseImplicitFrames(baseIndex: 0);

            _targetExplicitFrames.Clear();
            BuildExplicitTargetFrames(_pathSegments, isTableArray, _targetExplicitFrames);
            var prefixLength = GetExplicitCommonPrefixLength(_targetExplicitFrames, isTableArray);
            CloseExplicitFrames(prefixLength);
            OpenExplicitFrames(_targetExplicitFrames, prefixLength);
        }

        private void BeginKeyValueEntry(int implicitBaseIndex)
        {
            _pathSegments.Clear();
            _valueSpanStart = _token.Start;
            ParseKeyPath(_pathSegments);
            if (_pathSegments.Count == 0)
            {
                throw CreateException(CurrentSpan(), "Invalid key/value entry.");
            }

            if (_token.Kind != TokenKind.Equal)
            {
                throw CreateException(CurrentSpan(), $"Expected `=` after key but was `{ToPrintable(_token)}`.");
            }

            var prefixLength = _pathSegments.Count - 1;
            EnsureImplicitPrefix(implicitBaseIndex, _pathSegments, prefixLength);

            var leaf = _pathSegments[_pathSegments.Count - 1];
            var span = new SourceSpan(_lexer.Source.SourcePath, _valueSpanStart, _valueSpanStart);
            _pending.Enqueue(new TomlParseEvent(TomlParseEventKind.PropertyName, span: span, propertyName: leaf, stringValue: null, data: 0));

            Consume(TokenKind.Equal, LexerState.Value);
        }

        private void CloseImplicitFrames(int baseIndex)
        {
            for (var i = _implicitFrames.Count - 1; i >= baseIndex; i--)
            {
                _implicitFrames.RemoveAt(i);
                _pending.Enqueue(new TomlParseEvent(TomlParseEventKind.EndTable, span: null, propertyName: null, stringValue: null, data: 0));
            }
        }

        private void CloseExplicitFrames(int targetPrefixLength)
        {
            for (var i = _explicitFrames.Count - 1; i >= targetPrefixLength; i--)
            {
                var frame = _explicitFrames[i];
                switch (frame.Kind)
                {
                    case ExplicitFrameKind.Table:
                    case ExplicitFrameKind.TableArrayElement:
                        _pending.Enqueue(new TomlParseEvent(TomlParseEventKind.EndTable, span: null, propertyName: null, stringValue: null, data: 0));
                        break;
                    case ExplicitFrameKind.Array:
                        _pending.Enqueue(new TomlParseEvent(TomlParseEventKind.EndArray, span: null, propertyName: null, stringValue: null, data: 0));
                        break;
                }

                _explicitFrames.RemoveAt(i);
            }
        }

        private void OpenExplicitFrames(List<ExplicitFrame> targetFrames, int startIndex)
        {
            for (var i = startIndex; i < targetFrames.Count; i++)
            {
                var frame = targetFrames[i];
                switch (frame.Kind)
                {
                    case ExplicitFrameKind.Table:
                        _pending.Enqueue(new TomlParseEvent(TomlParseEventKind.PropertyName, span: null, propertyName: frame.Name, stringValue: null, data: 0));
                        _pending.Enqueue(new TomlParseEvent(TomlParseEventKind.StartTable, span: null, propertyName: null, stringValue: null, data: 0));
                        break;
                    case ExplicitFrameKind.Array:
                        _pending.Enqueue(new TomlParseEvent(TomlParseEventKind.PropertyName, span: null, propertyName: frame.Name, stringValue: null, data: 0));
                        _pending.Enqueue(new TomlParseEvent(TomlParseEventKind.StartArray, span: null, propertyName: null, stringValue: null, data: 0));
                        break;
                    case ExplicitFrameKind.TableArrayElement:
                        _pending.Enqueue(new TomlParseEvent(TomlParseEventKind.StartTable, span: null, propertyName: null, stringValue: null, data: 0));
                        break;
                }

                _explicitFrames.Add(frame);
            }
        }

        private static void BuildExplicitTargetFrames(List<string> path, bool isTableArray, List<ExplicitFrame> frames)
        {
            for (var i = 0; i < path.Count - 1; i++)
            {
                frames.Add(new ExplicitFrame(ExplicitFrameKind.Table, path[i]));
            }

            var last = path[path.Count - 1];
            if (isTableArray)
            {
                frames.Add(new ExplicitFrame(ExplicitFrameKind.Array, last));
                frames.Add(new ExplicitFrame(ExplicitFrameKind.TableArrayElement, name: null));
            }
            else
            {
                frames.Add(new ExplicitFrame(ExplicitFrameKind.Table, last));
            }
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

            return string.Equals(left.Name, right.Name, StringComparison.Ordinal);
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
                var span = new SourceSpan(_lexer.Source.SourcePath, spanStart, _token.End);
                _pending.Enqueue(new TomlParseEvent(TomlParseEventKind.StartArray, span: span, propertyName: null, stringValue: null, data: 0));
                Consume(TokenKind.OpenBracket, LexerState.Value);
                _containers.Add(new ContainerFrame(ContainerKind.Array, inlineImplicitBase: 0));
                return;
            }

            if (_token.Kind == TokenKind.OpenBrace)
            {
                var span = new SourceSpan(_lexer.Source.SourcePath, spanStart, _token.End);
                _pending.Enqueue(new TomlParseEvent(TomlParseEventKind.StartTable, span: span, propertyName: null, stringValue: null, data: 0));
                Consume(TokenKind.OpenBrace, LexerState.Key);
                _containers.Add(new ContainerFrame(ContainerKind.InlineTable, inlineImplicitBase: _implicitFrames.Count));
                return;
            }

            var eventSpan = new SourceSpan(_lexer.Source.SourcePath, spanStart, _token.End);

            if (_token.Kind.IsString())
            {
                _pending.Enqueue(new TomlParseEvent(TomlParseEventKind.String, span: eventSpan, propertyName: null, stringValue: null, data: (ulong)_token.Kind));
                Consume(_token.Kind, nextStateAfterScalar);
                return;
            }

            if (_token.Kind.IsInteger())
            {
                _pending.Enqueue(new TomlParseEvent(TomlParseEventKind.Integer, span: eventSpan, propertyName: null, stringValue: null, data: _token.Data));
                Consume(_token.Kind, nextStateAfterScalar);
                return;
            }

            if (_token.Kind.IsFloat())
            {
                _pending.Enqueue(new TomlParseEvent(TomlParseEventKind.Float, span: eventSpan, propertyName: null, stringValue: null, data: _token.Data));
                Consume(_token.Kind, nextStateAfterScalar);
                return;
            }

            if (_token.Kind == TokenKind.True || _token.Kind == TokenKind.False)
            {
                _pending.Enqueue(new TomlParseEvent(TomlParseEventKind.Boolean, span: eventSpan, propertyName: null, stringValue: null, data: _token.Data));
                Consume(_token.Kind, nextStateAfterScalar);
                return;
            }

            if (_token.Kind.IsDateTime())
            {
                var literal = _token.StringValue ?? _token.GetText(_lexer.Source) ?? string.Empty;
                _pending.Enqueue(new TomlParseEvent(TomlParseEventKind.DateTime, span: eventSpan, propertyName: null, stringValue: literal, data: 0));
                Consume(_token.Kind, nextStateAfterScalar);
                return;
            }

            throw CreateException(CurrentSpan(), $"Unexpected token `{ToPrintable(_token)}` while parsing a value.");
        }

        private void ParseKeyPath(List<string> segments)
        {
            segments.Add(ParseKeySegment());
            while (_token.Kind == TokenKind.Dot)
            {
                Consume(TokenKind.Dot, LexerState.Key);
                segments.Add(ParseKeySegment());
            }
        }

        private string ParseKeySegment()
        {
            if (_token.Kind == TokenKind.BasicKey)
            {
                var text = _token.GetText(_lexer.Source);
                if (string.IsNullOrEmpty(text))
                {
                    throw CreateException(CurrentSpan(), "Invalid bare key.");
                }

                Consume(TokenKind.BasicKey, LexerState.Key);
                return text!;
            }

            if (_token.Kind.IsString())
            {
                var raw = _token.GetText(_lexer.Source);
                if (string.IsNullOrEmpty(raw))
                {
                    throw CreateException(CurrentSpan(), "Invalid string key.");
                }

                var text = TomlStringDecoder.Decode(raw!, _token.Kind);
                Consume(_token.Kind, LexerState.Key);
                return text;
            }

            throw CreateException(CurrentSpan(), $"Unexpected token `{ToPrintable(_token)}` while parsing a key.");
        }

        private void EnsureImplicitPrefix(int baseIndex, List<string> pathSegments, int prefixLength)
        {
            var common = 0;
            var currentCount = _implicitFrames.Count - baseIndex;
            while (common < currentCount && common < prefixLength &&
                   string.Equals(_implicitFrames[baseIndex + common], pathSegments[common], StringComparison.Ordinal))
            {
                common++;
            }

            for (var i = currentCount - 1; i >= common; i--)
            {
                _implicitFrames.RemoveAt(baseIndex + i);
                _pending.Enqueue(new TomlParseEvent(TomlParseEventKind.EndTable, span: null, propertyName: null, stringValue: null, data: 0));
            }

            for (var i = common; i < prefixLength; i++)
            {
                _pending.Enqueue(new TomlParseEvent(TomlParseEventKind.PropertyName, span: null, propertyName: pathSegments[i], stringValue: null, data: 0));
                _pending.Enqueue(new TomlParseEvent(TomlParseEventKind.StartTable, span: null, propertyName: null, stringValue: null, data: 0));
                _implicitFrames.Add(pathSegments[i]);
            }
        }

        private TomlException CreateException(SourceSpan span, string message) => new TomlException(span, message);

        private SourceSpan CurrentSpan() => new SourceSpan(_lexer.Source.SourcePath, _token.Start, _token.End);

        private string ToPrintable(SyntaxTokenValue token)
        {
            var span = new SourceSpan(_lexer.Source.SourcePath, token.Start, token.End);
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
                _diagnostics.Error(span, exception.Message);
            }
        }

        private void TerminateStream()
        {
            if (_state == DocumentState.Ended)
            {
                return;
            }

            while (_containers.Count > 0)
            {
                var frame = _containers[_containers.Count - 1];
                if (frame.Kind == ContainerKind.InlineTable && _implicitFrames.Count > frame.InlineImplicitBase)
                {
                    _implicitFrames.RemoveAt(_implicitFrames.Count - 1);
                    _pending.Enqueue(new TomlParseEvent(TomlParseEventKind.EndTable, span: null, propertyName: null, stringValue: null, data: 0));
                    continue;
                }

                _containers.RemoveAt(_containers.Count - 1);
                _pending.Enqueue(frame.Kind == ContainerKind.Array
                    ? new TomlParseEvent(TomlParseEventKind.EndArray, span: null, propertyName: null, stringValue: null, data: 0)
                    : new TomlParseEvent(TomlParseEventKind.EndTable, span: null, propertyName: null, stringValue: null, data: 0));
            }

            CloseImplicitFrames(baseIndex: 0);
            CloseExplicitFrames(targetPrefixLength: 0);

            _pending.Enqueue(new TomlParseEvent(TomlParseEventKind.EndTable, span: null, propertyName: null, stringValue: null, data: 0));
            _pending.Enqueue(new TomlParseEvent(TomlParseEventKind.EndDocument, span: null, propertyName: null, stringValue: null, data: 0));
            _state = DocumentState.Ended;
        }

        private enum DocumentState
        {
            NotStarted = 0,
            Statement = 1,
            ExpectValue = 2,
            AfterValue = 3,
            Ended = 4,
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
            public ExplicitFrame(ExplicitFrameKind kind, string? name)
            {
                Kind = kind;
                Name = name;
            }

            public ExplicitFrameKind Kind { get; }

            public string? Name { get; }
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
    }
}
