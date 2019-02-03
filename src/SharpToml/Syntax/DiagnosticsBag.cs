// Copyright (c) 2019 - Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license. 

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace SharpToml.Syntax
{
    [DebuggerDisplay("{Count} Errors: {HasErrors}")]
    public class DiagnosticsBag : IEnumerable<DiagnosticMessage>
    {
        private readonly List<DiagnosticMessage> _messages;

        public DiagnosticsBag()
        {
            _messages = new List<DiagnosticMessage>();
        }


        public int Count => _messages.Count;

        public DiagnosticMessage this[int index] => _messages[index];

        public bool HasErrors { get; private set; }

        public void Add(DiagnosticMessage message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            _messages.Add(message);
            if (message.Kind == DiagnosticMessageKind.Error)
            {
                HasErrors = true;
            }
        }

        public void Clear()
        {
            _messages.Clear();
            HasErrors = false;
        }

        public List<DiagnosticMessage>.Enumerator GetEnumerator()
        {
            return _messages.GetEnumerator();
        }

        IEnumerator<DiagnosticMessage> IEnumerable<DiagnosticMessage>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable) _messages).GetEnumerator();
        }
    }
}