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
using System.Text.RegularExpressions;

namespace Microsoft.Practices.EnterpriseLibrary.SemanticLogging.Etw.Service
{
    internal class ParameterSet : HashSet<Parameter>
    {
        private readonly Regex nameParser = new Regex(@"^(?<f>--|-|/)(?<name>[^:=]+)?$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public void Add(string name, string description, Action<ParameterSet> action)
        {
            base.Add(new Parameter(name, description, action));
        }

        public bool Parse(IEnumerable<string> args)
        {
            // only a single arg at a time will be supported
            if (args == null || args.Count() != 1)
            {
                return false;
            }

            string arg = args.First();

            var name = this.GetName(arg);

            if (name == null)
            {
                return false;
            }

            var parameter = this.FirstOrDefault(a => a.Names.Contains(name));

            if (parameter == null)
            {
                return false;
            }

            parameter.Action(this);

            return true;
        }

        private string GetName(string argument)
        {
            Match m = this.nameParser.Match(argument);
            if (!m.Success)
            {
                return null;
            }

            return m.Groups["name"].Value;
        }
    }
}
