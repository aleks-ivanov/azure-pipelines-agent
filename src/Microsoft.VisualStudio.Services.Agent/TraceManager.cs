// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using Agent.Sdk.SecretMasking;

namespace Microsoft.VisualStudio.Services.Agent
{
    public interface ITraceManager : IDisposable
    {
        SourceSwitch Switch { get; }
        Tracing this[string name] { get; }
    }

    public sealed class TraceManager : ITraceManager
    {
        private readonly ConcurrentDictionary<string, Tracing> _sources = new ConcurrentDictionary<string, Tracing>(StringComparer.OrdinalIgnoreCase);
        private readonly HostTraceListener _hostTraceListener;
        private TraceSetting _traceSetting;
        private ILoggedSecretMasker _secretMasker;

        public TraceManager(HostTraceListener traceListener, ILoggedSecretMasker secretMasker)
            : this(traceListener, new TraceSetting(), secretMasker)
        {
        }

        public TraceManager(HostTraceListener traceListener, TraceSetting traceSetting, ILoggedSecretMasker secretMasker)
        {
            // Validate and store params.
            ArgUtil.NotNull(traceListener, nameof(traceListener));
            ArgUtil.NotNull(traceSetting, nameof(traceSetting));
            ArgUtil.NotNull(secretMasker, nameof(secretMasker));
            _hostTraceListener = traceListener;
            _traceSetting = traceSetting;
            _secretMasker = secretMasker;

            Switch = new SourceSwitch("VSTSAgentSwitch")
            {
                Level = _traceSetting.DefaultTraceLevel.ToSourceLevels()
            };
        }

        public SourceSwitch Switch { get; private set; }

        public Tracing this[string name]
        {
            get
            {
                return _sources.GetOrAdd(name, key => CreateTraceSource(key));
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (Tracing traceSource in _sources.Values)
                {
                    traceSource.Dispose();
                }

                _sources.Clear();
            }
        }

        private Tracing CreateTraceSource(string name)
        {
            SourceSwitch sourceSwitch = Switch;

            TraceLevel sourceTraceLevel;
            if (_traceSetting.DetailTraceSetting.TryGetValue(name, out sourceTraceLevel))
            {
                sourceSwitch = new SourceSwitch("VSTSAgentSubSwitch")
                {
                    Level = sourceTraceLevel.ToSourceLevels()
                };
            }
            return new Tracing(name, _secretMasker, sourceSwitch, _hostTraceListener);
        }
    }
}
