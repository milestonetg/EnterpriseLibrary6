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
using System.IO;
using System.Linq;
using System.Xml.Schema;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging.Etw.Configuration;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging.Formatters;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging.Tests.Etw.Configuration;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging.Tests.TestObjects;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging.Tests.TestSupport;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Practices.EnterpriseLibrary.SemanticLogging.Tests.Etw
{
    [TestClass]
    public class given_traceEventServiceConfigurationInstance
    {
        [TestMethod]
        public void when_creating_instance_with_null_values()
        {
            var sut = new TraceEventServiceConfiguration();

            Assert.AreEqual(0, sut.SinkSettings.Count);
            Assert.AreEqual(new TraceEventServiceSettings().SessionNamePrefix, sut.Settings.SessionNamePrefix);
        }

        [TestMethod]
        [ExpectedException(typeof(ConfigurationException))]
        public void when_creating_instance_with_duplicate_sinkSettings()
        {
            var sinks = new List<SinkSettings>();

            var sources = new List<EventSourceSettings>() { new EventSourceSettings("test"), new EventSourceSettings("test") };
            var sink = new SinkSettings("test", new Lazy<IObserver<EventEntry>>(() => new InMemoryEventListener()), sources);
            sinks.Add(sink);
            sinks.Add(sink);

            new TraceEventServiceConfiguration(sinks);
        }
    }

    public abstract class given_traceEventServiceConfiguration : ContextBase
    {
        protected TraceEventServiceConfiguration Sut;

        protected override void OnCleanup()
        {
            if (this.Sut != null)
            {
                this.Sut.Dispose();
            }
        }

        [TestClass]
        public class when_loading_instance_from_file_with_default_values : given_traceEventServiceConfiguration
        {
            protected override void When()
            {
                this.Sut = TraceEventServiceConfiguration.Load("Etw\\Configuration\\WithDefaultValues.xml");
            }

            [TestMethod]
            public void then_instance_is_loaded_with_default_values()
            {
                Assert.IsNotNull(this.Sut);
                Assert.IsTrue(this.Sut.Settings.SessionNamePrefix.StartsWith(Constants.DefaultSessionNamePrefix));
                Assert.AreEqual(0, this.Sut.SinkSettings.Count);
            }
        }

        [TestClass]
        public class when_loading_instance_from_file_with_eventSource_name_only : given_traceEventServiceConfiguration
        {
            protected override void When()
            {
                this.Sut = TraceEventServiceConfiguration.Load("Etw\\Configuration\\WithEventSourceNameOnly.xml");
            }

            [TestMethod]
            public void then_instance_is_loaded_with_default_values()
            {
                Assert.IsNotNull(this.Sut);
                Assert.AreEqual("MyCompany", this.Sut.SinkSettings[0].EventSources.First().Name);
                Assert.AreEqual(MyCompanyEventSource.Log.Guid, this.Sut.SinkSettings[0].EventSources.First().EventSourceId);
            }
        }

        [TestClass]
        public class when_loading_instance_from_file_with_eventSource_id_only : given_traceEventServiceConfiguration
        {
            protected override void When()
            {
                this.Sut = TraceEventServiceConfiguration.Load("Etw\\Configuration\\WithEventSourceIdOnly.xml");
            }

            [TestMethod]
            public void then_instance_is_loaded_with_id_and_name_values()
            {
                Assert.IsNotNull(this.Sut);
                Assert.AreEqual(MyCompanyEventSource.Log.Guid.ToString(), this.Sut.SinkSettings[0].EventSources.First().Name);
                Assert.AreEqual(MyCompanyEventSource.Log.Guid, this.Sut.SinkSettings[0].EventSources.First().EventSourceId);
            }
        }

        [TestClass]
        public class when_loading_instance_from_file_with_no_eventSource_name_guid : given_traceEventServiceConfiguration
        {
            [TestMethod]
            [ExpectedException(typeof(ConfigurationException))]
            public void then_exception_is_thrown()
            {
                TraceEventServiceConfiguration.Load("Etw\\Configuration\\WithNoEventSourceNameId.xml");
            }
        }

        [TestClass]
        public class when_loading_instance_from_file_with_duplicate_names : given_traceEventServiceConfiguration
        {
            [TestMethod]
            public void then_schema_validation_exception_is_thrown()
            {
                var exception = AssertEx.Throws<ConfigurationException>(() => TraceEventServiceConfiguration.Load("Etw\\Configuration\\WithDuplicateNames.xml"));

                Assert.AreEqual(3, exception.InnerExceptions.Count);
                exception.InnerExceptions.All(e => e is XmlSchemaValidationException);

                Assert.IsTrue(exception.InnerExceptions.Any(e =>
                    e.Message.StartsWith("There is a duplicate key sequence 'customListener'", StringComparison.OrdinalIgnoreCase)));

                Assert.IsTrue(exception.InnerExceptions.Any(e =>
                    e.Message.StartsWith("There is a duplicate key sequence 'Test'", StringComparison.OrdinalIgnoreCase)));

                Assert.IsTrue(exception.InnerExceptions.Any(e =>
                    e.Message.Contains("invalid child element 'eventTextFormatter'")));
            }
        }

        [TestClass]
        public class when_loading_instance_from_file_with_empty_non_string_values : given_traceEventServiceConfiguration
        {
            [TestMethod]
            public void then_schema_validation_exception_is_thrown()
            {
                var exception = AssertEx.Throws<ConfigurationException>(() => TraceEventServiceConfiguration.Load("Etw\\Configuration\\WithEmptyNonStringValues.xml"));

                Assert.AreEqual(2, exception.InnerExceptions.Count);
                exception.InnerExceptions.All(e => e is XmlSchemaValidationException || e is InvalidOperationException);

                Assert.IsTrue(exception.InnerExceptions.Any(e => e.Message.StartsWith("The 'sessionNamePrefix' attribute is invalid", StringComparison.OrdinalIgnoreCase)));
                Assert.IsTrue(exception.InnerExceptions.Any(e => e.Message.StartsWith("The 'id' attribute is invalid", StringComparison.OrdinalIgnoreCase)));
            }
        }

        [TestClass]
        public class when_loading_instance_from_file_with_warnings_validation : given_traceEventServiceConfiguration
        {
            [TestMethod]
            public void then_schema_validation_exception_is_thrown()
            {
                var exception = AssertEx.Throws<ConfigurationException>(() => TraceEventServiceConfiguration.Load("Etw\\Configuration\\WithWarnings.xml"));

                Assert.AreEqual(4, exception.InnerExceptions.Count);
                exception.InnerExceptions.All(e => e is XmlSchemaValidationException);
                Assert.IsTrue(exception.InnerExceptions.Any(e => e.Message.Contains("The required attribute 'attr' is missing.")));
                Assert.IsTrue(exception.InnerExceptions.Any(e => e.Message.Contains("The 'sessionNamePrefix' attribute is invalid")));
                Assert.IsTrue(exception.InnerExceptions.Any(e => e.Message.Contains("The 'name' attribute is invalid") &&
                                                                 ((XmlSchemaValidationException)e).LineNumber == 7 &&
                                                                 ((XmlSchemaValidationException)e).LinePosition == 18));
                Assert.IsTrue(exception.InnerExceptions.Any(e => e.Message.Contains("The required attribute 'name' is missing.")));
            }
        }

        [TestClass]
        public class when_loading_instance_from_file_with_many_eventSources : given_traceEventServiceConfiguration
        {
            protected override void When()
            {
                this.Sut = TraceEventServiceConfiguration.Load("Etw\\Configuration\\WithManyEventSources.xml");
            }

            [TestMethod]
            public void then_instance_is_loaded_with_all_configured_eventSources()
            {
                Assert.IsNotNull(this.Sut);
                Assert.AreEqual(2, this.Sut.SinkSettings.Count);
                var firstSink = this.Sut.SinkSettings.SingleOrDefault(s => s.Name == "sink1");
                Assert.IsNotNull(firstSink);
                Assert.AreEqual(1, firstSink.EventSources.Count());
                Assert.AreEqual("MyCompany", firstSink.EventSources.First().Name);

                var secondSink = this.Sut.SinkSettings.SingleOrDefault(e => e.Name == "sink2");
                Assert.IsNotNull(secondSink);
                Assert.AreEqual(1, secondSink.EventSources.Count());
                Assert.AreEqual("Test", secondSink.EventSources.First().Name);
            }
        }

        [TestClass]
        public class when_loading_instance_from_file_with_parameters : given_traceEventServiceConfiguration
        {
            protected override void When()
            {
                this.Sut = TraceEventServiceConfiguration.Load("Etw\\Configuration\\WithManySinks.xml");
            }

            [TestMethod]
            public void then_instance_is_loaded_with_all_configured_eventSources()
            {
                Assert.IsNotNull(this.Sut);

                Assert.AreEqual(4, this.Sut.SinkSettings.Count);
                Assert.AreEqual(EventLevel.Error, this.Sut.SinkSettings[0].EventSources.First().Level);
                Assert.AreEqual((EventKeywords)1, this.Sut.SinkSettings[0].EventSources.First().MatchAnyKeyword);

                Assert.AreEqual(EventLevel.Error, this.Sut.SinkSettings[1].EventSources.First().Level);
                Assert.AreEqual(EventKeywords.None, this.Sut.SinkSettings[1].EventSources.First().MatchAnyKeyword);

                Assert.AreEqual(EventLevel.Error, this.Sut.SinkSettings[2].EventSources.First().Level);

                //Assert.AreEqual((EventKeywords)123, this.Sut.SinkSettings[3].EventSources.First().MatchAnyKeyword);
            }
        }

        [TestClass]
        public class when_disposing_instance : given_traceEventServiceConfiguration
        {
            const string FlatFile = "FlatFile.log";

            protected override void When()
            {
                this.Sut = TraceEventServiceConfiguration.Load("Etw\\Configuration\\WithManySinks.xml");
            }

            [TestMethod]
            public void then_all_listeners_will_be_disposed()
            {
                // Check if configured file was created
                Assert.IsTrue(File.Exists(FlatFile));

                // try to delete and confirm is locked by listener
                try { File.Delete(FlatFile); }
                catch (IOException)
                {
                }

                this.Sut.Dispose();

                // Locked file is released
                File.Delete(FlatFile);
            }
        }

        [TestClass]
        public class when_loading_bad_types : given_traceEventServiceConfiguration
        {
            [TestMethod]
            [ExpectedException(typeof(ConfigurationException))]
            public void then_exception_is_thrown()
            {
                var sut = TraceEventServiceConfiguration.Load("Etw\\Configuration\\WithBadTypes.xml");
            }
        }

        [TestClass]
        public class when_extending_configuration : given_traceEventServiceConfiguration
        {
            protected override void When()
            {
                this.Sut = TraceEventServiceConfiguration.Load("Etw\\Configuration\\WithExtensions.xml");
            }

            [TestMethod]
            public void then_extensions_will_be_loaded()
            {
                Assert.AreEqual("custom", this.Sut.SinkSettings[0].Name);
                Assert.AreEqual("withCustomFormatter", this.Sut.SinkSettings[1].Name);
                Assert.AreEqual("my", this.Sut.SinkSettings[2].Name);

                Assert.IsNotNull(MySink.Instance);
            }

            [TestMethod]
            public void then_parameters_are_passed()
            {
                var customSink = MySink.Instance;

                Assert.IsInstanceOfType(customSink.Formatter, typeof(JsonEventTextFormatter));
            }
        }

        [TestClass]
        public class when_loading_extensions_with_schema_validation : given_traceEventServiceConfiguration
        {
            [TestMethod]
            public void then_validation_exception_is_thrown()
            {
                var exception = AssertEx.Throws<ConfigurationException>(() => TraceEventServiceConfiguration.Load("Etw\\Configuration\\WithExtensionsSchemaValidation.xml"));

                Assert.AreEqual(1, exception.InnerExceptions.Count);
                exception.InnerExceptions.All(e => e is XmlSchemaValidationException);
                Assert.AreEqual("The required attribute 'attr' is missing.", exception.InnerException.Message);
            }
        }

        [TestClass]
        public class when_loading_instance_from_file_with_error_on_sink_creation : given_traceEventServiceConfiguration
        {
            [TestMethod]
            public void then_load_validation_exception_is_thrown()
            {
                var exception = AssertEx.Throws<ConfigurationException>(() =>
                {
                    var config = TraceEventServiceConfiguration.Load("Etw\\Configuration\\WithErrorOnSinkCreation.xml");
                });

                StringAssert.Contains(exception.ToString(), "The given path's format is not supported.");
            }
        }

        [TestClass]
        public class when_loading_extensions_with_no_schema_validation : given_traceEventServiceConfiguration
        {
            protected override void When()
            {
                this.Sut = TraceEventServiceConfiguration.Load("Etw\\Configuration\\WithExtensionsNoSchemaValidation.xml");
            }

            [TestMethod]
            public void then_extensions_will_be_loaded()
            {
                Assert.AreEqual("my", this.Sut.SinkSettings[0].Name);
                Assert.IsInstanceOfType(this.Sut.SinkSettings[0].Sink, typeof(MySink));
            }
        }

        [TestClass]
        public class when_loading_custom_extension_with_incomplete_sink_parameters : given_traceEventServiceConfiguration
        {
            [TestMethod]
            public void then_load_validation_exception_is_thrown()
            {
                var exception = AssertEx.Throws<ConfigurationException>(() =>
                {
                    var config = TraceEventServiceConfiguration.Load("Etw\\Configuration\\WithExtensionsIncompleteParams.xml");
                });

                StringAssert.Contains(exception.ToString(), "The parameters specified in this element does not map to an existing type member. All paramters are required in the same order of the defined type member");
            }
        }
    }
}
