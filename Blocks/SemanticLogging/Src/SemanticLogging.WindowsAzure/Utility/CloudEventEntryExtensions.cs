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
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging.Observable;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging.Schema;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging.Sinks.WindowsAzure;

namespace Microsoft.Practices.EnterpriseLibrary.SemanticLogging.Utility
{
    /// <summary>
    /// Extensions for <see cref="EventEntry"/>.
    /// </summary>
    public static class CloudEventEntryExtensions
    {
        /// <summary>
        /// Subscribes an <see cref="IObserver{CloudEventEntry}"/> sink by doing a straight projection of a sequence of <see cref="EventEntry"/>
        /// and converting it to a <see cref="CloudEventEntry"/> entity.
        /// </summary>
        /// <param name="source">The original stream of events.</param>
        /// <param name="sink">The underlying sink.</param>
        /// <returns>A subscription token to unsubscribe to the event stream.</returns>
        /// <remarks>When using Reactive Extensions (Rx), this is equivalent to doing a Select statement on the <paramref name="source"/> to convert it to <see cref="IObservable{String}"/> and then
        /// calling Subscribe on it.
        /// </remarks>
        public static IDisposable SubscribeWithConversion(this IObservable<EventEntry> source, IObserver<CloudEventEntry> sink)
        {
            return source.CreateSubscription(sink, TryConvertToCloudEventEntry);
        }

        /// <summary>
        /// Converts an <see cref="EventEntry"/> to a <see cref="CloudEventEntry"/>.
        /// </summary>
        /// <param name="entry">The entry to convert.</param>
        /// <returns>A converted entry, or <see langword="null"/> if the payload is invalid.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0", Justification = "Validated using Guard class")]
        public static CloudEventEntry TryConvertToCloudEventEntry(this EventEntry entry)
        {
            Guard.ArgumentNotNull(entry, "entry");

            var entity = new CloudEventEntry()
            {
                EventId = entry.EventId,
                Keywords = (long)entry.Schema.Keywords,
                ProviderId = entry.ProviderId,
                ProviderName = entry.Schema.ProviderName,
                Level = (int)entry.Schema.Level,
                Message = entry.FormattedMessage,
                Opcode = (int)entry.Schema.Opcode,
                Task = (int)entry.Schema.Task,
                Version = entry.Schema.Version,
                EventDate = entry.Timestamp.UtcDateTime
            };

            if (!InitializePayload(entity, entry.Payload, entry.Schema))
            {
                return null;
            }

            return entity;
        }

        private static bool InitializePayload(CloudEventEntry entity, IList<object> payload, EventSchema schema)
        {
            try
            {
                entity.Payload = new Dictionary<string, object>(payload.Count);

                for (int i = 0; i < payload.Count; i++)
                {
                    entity.Payload.Add(schema.Payload[i], payload[i]);
                }

                return true;
            }
            catch (Exception e)
            {
                SemanticLoggingEventSource.Log.WindowsAzureTableSinkEntityCreationFailed(e.ToString());
                return false;
            }
        }
    }
}
