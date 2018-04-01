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
using System.Linq;
using System.Reflection;
using System.Text;

namespace Microsoft.Practices.EnterpriseLibrary.SemanticLogging.Etw.Service
{
    internal static class AssemblyExtensions
    {
        public static string ToProductString(this Assembly assembly)
        {
            StringBuilder sb = new StringBuilder();

            string assemblyProduct = FromAttribute<AssemblyProductAttribute>(assembly).Product;

            string assemblyCopyright = FromAttribute<AssemblyCopyrightAttribute>(assembly).Copyright;

            if (string.IsNullOrWhiteSpace(assemblyCopyright))
            {
                assemblyCopyright = FromAttribute<AssemblyCompanyAttribute>(assembly).Company;
            }

            string assemblyDescription = FromAttribute<AssemblyDescriptionAttribute>(assembly).Description ?? assembly.GetName().Name;
            var version = FromAttribute<AssemblyFileVersionAttribute>(assembly).Version;

            sb.AppendFormat("{0} v{1}", assemblyDescription, version);
            sb.AppendLine();
            sb.AppendLine(assemblyProduct);
            sb.AppendLine(assemblyCopyright);
            sb.AppendLine();

            return sb.ToString();
        }

        private static T FromAttribute<T>(Assembly assembly) where T : Attribute
        {
            return (assembly.GetCustomAttributes(typeof(T), false).FirstOrDefault() as T) ?? Activator.CreateInstance<T>();
        }
    }
}
