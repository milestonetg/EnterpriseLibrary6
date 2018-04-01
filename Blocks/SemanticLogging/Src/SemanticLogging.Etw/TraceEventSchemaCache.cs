﻿#region license
// ==============================================================================
// Microsoft patterns & practices Enterprise Library
// Semantic Logging Application Block
// ==============================================================================
// Copyright © Microsoft Corporation.  All rights reserved.
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY
// OF ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT
// LIMITED TO THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
// FITNESS FOR A PARTICULAR PURPOSE.
// ==============================================================================
#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using Diagnostics.Tracing;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging.Schema;

namespace Microsoft.Practices.EnterpriseLibrary.SemanticLogging.Etw
{
    /// <summary>
    /// Used for caching <see cref="EventSchema"/> by event provider.
    /// </summary>
    internal sealed class TraceEventSchemaCache
    {
        private readonly Dictionary<Guid, IDictionary<int, EventSchema>> schemas = new Dictionary<Guid, IDictionary<int, EventSchema>>();
        private readonly EventSourceSchemaReader schemaReader = new EventSourceSchemaReader();

        internal EventSchema GetSchema(TraceEvent traceEvent)
        {
            int eventId = (int)traceEvent.ID;
            EventSchema schema;
            IDictionary<int, EventSchema> providerSchemas;
            if (!this.schemas.TryGetValue(traceEvent.ProviderGuid, out providerSchemas))
            {
                schema = CreateEventSchema(traceEvent);
                providerSchemas = new Dictionary<int, EventSchema>() { { eventId, schema } };
                this.schemas.Add(traceEvent.ProviderGuid, providerSchemas);
                return schema;
            }

            if (!providerSchemas.TryGetValue(eventId, out schema))
            {
                schema = CreateEventSchema(traceEvent);
                providerSchemas.Add(eventId, schema);
                this.schemas[traceEvent.ProviderGuid] = providerSchemas;
            }

            return schema;
        }

        internal void UpdateSchemaFromManifest(Guid providerGuid, string manifest)
        {
            this.schemas[providerGuid] = this.schemaReader.GetSchema(manifest);
        }

        private static EventSchema CreateEventSchema(TraceEvent traceEvent)
        {
            return new EventSchema(
                        (int)traceEvent.ID,
                        traceEvent.ProviderGuid,
                        traceEvent.ProviderName,
                        (EventLevel)traceEvent.Level,
                        (EventTask)traceEvent.Task,
                        traceEvent.TaskName,
                        (EventOpcode)traceEvent.Opcode,
                        traceEvent.OpcodeName,
                        (EventKeywords)traceEvent.Keyword,
                        null,  // Keywords description not parsed by DynamicTraceEventParser
                        traceEvent.Version,
                        traceEvent.PayloadNames);
        }
    }
}
