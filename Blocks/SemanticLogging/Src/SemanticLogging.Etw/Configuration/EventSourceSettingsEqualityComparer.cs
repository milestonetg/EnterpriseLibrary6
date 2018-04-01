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

using System.Collections.Generic;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging.Utility;

namespace Microsoft.Practices.EnterpriseLibrary.SemanticLogging.Etw.Configuration
{
    internal class EventSourceSettingsEqualityComparer : IEqualityComparer<EventSourceSettings>
    {
        private bool nameOnly;

        public EventSourceSettingsEqualityComparer(bool nameOnly = false)
        {
            this.nameOnly = nameOnly;
        }

        public bool Equals(EventSourceSettings x, EventSourceSettings y)
        {
            if (x == null || y == null)
            {
                return false;
            }

            return x.Name == y.Name &&
                   (this.nameOnly || (x.Level == y.Level && x.MatchAnyKeyword == y.MatchAnyKeyword));
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0", Justification = "Validated with Guard class")]
        public int GetHashCode(EventSourceSettings obj)
        {
            Guard.ArgumentNotNull(obj, "obj");

            if (this.nameOnly)
            {
                return obj.Name.GetHashCode();
            }

            return obj.Name.GetHashCode() ^ (int)obj.Level ^ (int)obj.MatchAnyKeyword;
        }
    }
}
