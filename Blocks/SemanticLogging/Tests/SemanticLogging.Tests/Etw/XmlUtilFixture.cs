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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging.Etw.Utility;
using System.Threading;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging.Tests.TestObjects;

namespace Microsoft.Practices.EnterpriseLibrary.SemanticLogging.Tests.Etw
{
    [TestClass]
    public class given_xmlUtil
    {
        [TestMethod]
        public void when_converting_toTimeSpan_from_null_attribute()
        {
            TimeSpan? result = ((XAttribute)null).ToTimeSpan();

            Assert.AreEqual((TimeSpan?)null, result);
        }

        [TestMethod]
        public void when_converting_toTimeSpan_from_infinite()
        {
            TimeSpan? result =  new XAttribute("value", -1).ToTimeSpan();

            Assert.AreEqual(Timeout.InfiniteTimeSpan, result);
        }

        [TestMethod]
        public void when_converting_toTimeSpan_from_int()
        {
            TimeSpan? result = new XAttribute("value", 123).ToTimeSpan();

            Assert.AreEqual(TimeSpan.FromSeconds(123), result);
        }

        [TestMethod]
        public void when_creating_instance_from_element()
        {
            var element = new XElement("test", new XAttribute("type", "Microsoft.Practices.EnterpriseLibrary.SemanticLogging.Tests.TestObjects.InMemoryEventListener, Microsoft.Practices.EnterpriseLibrary.SemanticLogging.Tests"));
            var sut = XmlUtil.CreateInstance<InMemoryEventListener>(element);

            Assert.IsNotNull(sut);
            Assert.IsInstanceOfType(sut, typeof(InMemoryEventListener));
        }

        [TestMethod]
        public void when_creating_instance_from_element_with_parameters()
        {
            var doc = XDocument.Parse(
               @"<customSink name=""custom"" type=""Microsoft.Practices.EnterpriseLibrary.SemanticLogging.Tests.TestObjects.InMemoryEventListener, Microsoft.Practices.EnterpriseLibrary.SemanticLogging.Tests"">
                    <sources>
                      <eventSource name=""MyCompany""/>
                    </sources>
                    <parameters>
                      <parameter name=""formatter"" type=""Microsoft.Practices.EnterpriseLibrary.SemanticLogging.Tests.TestObjects.MockFormatter, Microsoft.Practices.EnterpriseLibrary.SemanticLogging.Tests""/>
                    </parameters>
                 </customSink>");

            var sut = XmlUtil.CreateInstance<InMemoryEventListener>(doc.Root);

            Assert.IsNotNull(sut);
            Assert.IsInstanceOfType(sut, typeof(InMemoryEventListener));
        }
    }
}
