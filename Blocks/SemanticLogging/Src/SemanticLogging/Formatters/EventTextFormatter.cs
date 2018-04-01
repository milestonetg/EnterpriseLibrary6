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
using System.Diagnostics.Tracing;
using System.Globalization;
using System.IO;
using System.Text;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging.Utility;

namespace Microsoft.Practices.EnterpriseLibrary.SemanticLogging.Formatters
{
    /// <summary>
    /// A <see cref="IEventTextFormatter"/> implementation that writes out formatted text.
    /// </summary>
    /// <remarks>This class is not thread-safe.</remarks>
    public class EventTextFormatter : IEventTextFormatter
    {
        /// <summary>
        /// The dash separator.
        /// </summary>
        public const string DashSeparator = "----------------------------------------";

        /// <summary>
        /// The default <see cref="VerbosityThreshold"/>.
        /// </summary>
        public const EventLevel DefaultVerbosityThreshold = EventLevel.Error;

        private string dateTimeFormat;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventTextFormatter" /> class.
        /// </summary>
        /// <param name="header">The header.</param>
        /// <param name="footer">The footer.</param>
        /// <param name="verbosityThreshold">The verbosity threshold.</param>
        /// <param name="dateTimeFormat">The date time format used for timestamp value.</param>
        public EventTextFormatter(string header = null, string footer = null, EventLevel verbosityThreshold = DefaultVerbosityThreshold, string dateTimeFormat = null)
        {
            this.Header = header;
            this.Footer = footer;
            this.VerbosityThreshold = verbosityThreshold;
            this.DateTimeFormat = dateTimeFormat;
        }

        /// <summary>
        /// Gets or sets the header.
        /// </summary>
        /// <value>The header of the text formatter.</value>
        public string Header { get; set; }

        /// <summary>
        /// Gets or sets the footer.
        /// </summary>
        /// <value>The footer of the text formatter.</value>
        public string Footer { get; set; }

        /// <summary>
        /// Gets or sets the lowest <see cref="System.Diagnostics.Tracing.EventLevel" /> value where the formatted output provides all the event entry information.
        /// Otherwise a summarized content of the event entry will be written.
        /// </summary>
        /// <value>The EventLevel.</value>
        public EventLevel VerbosityThreshold { get; set; }

        /// <summary>
        /// Gets or sets the date time format used for timestamp value.
        /// </summary>
        /// <value>The date time format value.</value>
        public string DateTimeFormat
        {
            get
            {
                return this.dateTimeFormat;
            }

            set
            {
                Guard.ValidDateTimeFormat(value, "DateTimeFormat");
                this.dateTimeFormat = value;
            }
        }

        /// <summary>
        /// Writes the event.
        /// </summary>
        /// <param name="eventEntry">The <see cref="EventEntry" /> instance containing the event data.</param>
        /// <param name="writer">The writer.</param>
        public void WriteEvent(EventEntry eventEntry, TextWriter writer)
        {
            Guard.ArgumentNotNull(eventEntry, "eventEntry");
            Guard.ArgumentNotNull(writer, "writer");

            // Write header
            if (!string.IsNullOrWhiteSpace(this.Header))
            {
                writer.WriteLine(this.Header);
            }

            if (eventEntry.Schema.Level <= this.VerbosityThreshold || this.VerbosityThreshold == EventLevel.LogAlways)
            {
                string format = "{0} : {1}";

                // Write with verbosityThreshold format 
                writer.WriteLine(format, PropertyNames.ProviderId, eventEntry.ProviderId);
                writer.WriteLine(format, PropertyNames.EventId, eventEntry.EventId);
                writer.WriteLine(format, PropertyNames.Keywords, eventEntry.Schema.Keywords);
                writer.WriteLine(format, PropertyNames.Level, eventEntry.Schema.Level);
                writer.WriteLine(format, PropertyNames.Message, eventEntry.FormattedMessage);
                writer.WriteLine(format, PropertyNames.Opcode, eventEntry.Schema.Opcode);
                writer.WriteLine(format, PropertyNames.Task, eventEntry.Schema.Task);
                writer.WriteLine(format, PropertyNames.Version, eventEntry.Schema.Version);
                writer.WriteLine(format, PropertyNames.Payload, FormatPayload(eventEntry));
                writer.WriteLine(format, PropertyNames.EventName, eventEntry.Schema.EventName);
                writer.WriteLine(format, PropertyNames.Timestamp, eventEntry.GetFormattedTimestamp(this.DateTimeFormat));
            }
            else
            {
                // Write with summary format
                writer.WriteLine(
                    "{0} : {1}, {2} : {3}, {4} : {5}, {6} : {7}, {8} : {9}, {10} : {11}",
                    PropertyNames.EventId,
                    eventEntry.EventId,
                    PropertyNames.Level,
                    eventEntry.Schema.Level,
                    PropertyNames.Message,
                    eventEntry.FormattedMessage,
                    PropertyNames.Payload,
                    FormatPayload(eventEntry),
                    PropertyNames.EventName,
                    eventEntry.Schema.EventName,
                    PropertyNames.Timestamp,
                    eventEntry.GetFormattedTimestamp(this.DateTimeFormat));
            }

            // Write footer
            if (!string.IsNullOrWhiteSpace(this.Footer))
            {
                writer.WriteLine(this.Footer);
            }

            writer.WriteLine();
        }

        private static string FormatPayload(EventEntry entry)
        {
            var eventSchema = entry.Schema;
            var sb = new StringBuilder();

            for (int i = 0; i < entry.Payload.Count; i++)
            {
                try
                {
                    sb.AppendFormat("[{0} : {1}] ", eventSchema.Payload[i], entry.Payload[i]);
                }
                catch (Exception e)
                {
                    SemanticLoggingEventSource.Log.EventEntryTextWriterFailed(e.ToString());
                    sb.AppendFormat("[{0} : {1}] ", "Exception", string.Format(CultureInfo.CurrentCulture, Properties.Resources.TextSerializationError, e.Message));
                }
            }

            return sb.ToString();
        }
    }
}
