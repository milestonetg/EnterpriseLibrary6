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
using System.CodeDom.Compiler;
using System.Linq;
using System.Text;

namespace Microsoft.Practices.EnterpriseLibrary.SemanticLogging.Tests.TestSupport
{
    internal class AssemblyBuilder
    {
        public static CompilerResults CompileFromSource(string source, bool generateInMemory = true)
        {
            CompilerParameters parameters = new CompilerParameters()
            {
                TempFiles = new TempFileCollection(".", false),
                GenerateInMemory = generateInMemory,
                TreatWarningsAsErrors = true,
            };

            foreach (string location in AppDomain.CurrentDomain.GetAssemblies().
                Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location)).
                Select(a => a.Location))
            {
                if (!parameters.ReferencedAssemblies.Contains(location))
                {
                    parameters.ReferencedAssemblies.Add(location);
                }
            }

            using (CodeDomProvider provider = new CSharp.CSharpCodeProvider())
            {
                return provider.CompileAssemblyFromSource(parameters, source);
            }
        }

        public static string DumpOnErrors(CompilerResults results)
        {
            if (results.Errors.HasErrors)
            {
                StringBuilder sb = new StringBuilder();
                foreach (CompilerError error in results.Errors)
                {
                    sb.AppendFormat("{0}. Line: {1}, Col: {2}", error.ErrorText, error.Line, error.Column);
                    sb.AppendLine();
                }
                return sb.ToString();
            }
            return string.Empty;
        }
    }
}
