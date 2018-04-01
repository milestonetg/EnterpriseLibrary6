﻿//===============================================================================
// Microsoft patterns & practices Enterprise Library
// Logging Application Block
//===============================================================================
// Copyright © Microsoft Corporation.  All rights reserved.
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY
// OF ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT
// LIMITED TO THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
// FITNESS FOR A PARTICULAR PURPOSE.
//===============================================================================

using System;
using System.Diagnostics;
using Microsoft.Practices.EnterpriseLibrary.Common.Configuration;
using Microsoft.Practices.EnterpriseLibrary.Common.TestSupport.Configuration;
using Microsoft.Practices.EnterpriseLibrary.Logging.Configuration;
using Microsoft.Practices.EnterpriseLibrary.Logging.TestSupport;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Practices.EnterpriseLibrary.Logging.Tests.TraceListeners.Configuration
{
    [TestClass]
    public class TextWriterTraceListenerConfigurationFixture
    {
        [TestInitialize]
        public void SetUp()
        {
            AppDomain.CurrentDomain.SetData("APPBASE", Environment.CurrentDirectory);
            ConfigurationTestHelper.ConfigurationFileName = "test.exe.config";
        }

        private static TraceListener GetListener(string name, IConfigurationSource configurationSource)
        {
            var settings = LoggingSettings.GetLoggingSettings(configurationSource);
            return settings.TraceListeners.Get(name).BuildTraceListener(settings);
        }

        [TestMethod]
        public void CanCreateInstanceFromGivenName()
        {
            SystemDiagnosticsTraceListenerData listenerData
                = new SystemDiagnosticsTraceListenerData("listener", typeof(TextWriterTraceListener), "log.txt");
            listenerData.TraceOutputOptions = TraceOptions.Callstack;

            MockLogObjectsHelper helper = new MockLogObjectsHelper();
            helper.loggingSettings.TraceListeners.Add(listenerData);

            TraceListener listener = GetListener("listener", helper.configurationSource);

            Assert.IsNotNull(listener);
            Assert.AreEqual(listener.GetType(), typeof(TextWriterTraceListener));
            Assert.AreEqual("listener", listener.Name);
            Assert.AreEqual(TraceOptions.Callstack, listener.TraceOutputOptions);
        }

        [TestMethod]
        public void CanCreateInstanceFromConfigurationFile()
        {
            SystemDiagnosticsTraceListenerData listenerData
                = new SystemDiagnosticsTraceListenerData("listener", typeof(TextWriterTraceListener), "log.txt");
            listenerData.TraceOutputOptions = TraceOptions.Callstack;

            LoggingSettings loggingSettings = new LoggingSettings();
            loggingSettings.TraceListeners.Add(listenerData);

            TraceListener listener
                = GetListener("listener", CommonUtil.SaveSectionsAndGetConfigurationSource(loggingSettings));

            Assert.IsNotNull(listener);
            Assert.AreEqual(listener.GetType(), typeof(TextWriterTraceListener));
            Assert.AreEqual(TraceOptions.Callstack, listener.TraceOutputOptions);
        }
    }
}
