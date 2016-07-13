﻿using System.Collections.Generic;

namespace Couchbase.Search
{
    /// <summary>
    /// Represents a single "hit" for a <see cref="IFtsQuery"/> request.
    /// </summary>
    /// <seealso cref="Couchbase.Search.ISearchQueryRow" />
    public class SearchQueryRow : ISearchQueryRow
    {
        /// <summary>
        /// The document identifier.
        /// </summary>
        public string Id { get; internal set; }

        /// <summary>
        /// The relative score for this "hit".
        /// </summary>
        public double Score { get; internal set; }

        /// <summary>
        /// Gets the index used for the "hit".
        /// </summary>
        /// <value>
        /// The index.
        /// </value>
        public string Index { get; internal set; }

        /// <summary>
        /// Detailed explanation of the search "hit".
        /// </summary>
        /// <value>
        /// The explanation.
        /// </value>
        public dynamic Explanation { get; internal set; }

        /// <summary>
        /// Indicates the offsets of the search terms matched inside the document.
        /// </summary>
        /// <value>
        /// The locations.
        /// </value>
        public dynamic Locations { get; internal set; }

        /// <summary>
        /// Give thes complete value of the included fields where matches occurred.
        /// </summary>
        /// <value>
        /// The fields.
        /// </value>
        public IDictionary<string, string> Fields { get; internal set; }

        /// <summary>
        /// The highlighted fragments of the search hits within the content.
        /// </summary>
        /// <value>
        /// The fragments.
        /// </value>
        public IDictionary<string, List<string>> Fragments { get; internal set; }
    }
}

#region [ License information          ]

/* ************************************************************
 *
 *    @author Couchbase <info@couchbase.com>
 *    @copyright 2015 Couchbase, Inc.
 *
 *    Licensed under the Apache License, Version 2.0 (the "License");
 *    you may not use this file except in compliance with the License.
 *    You may obtain a copy of the License at
 *
 *        http://www.apache.org/licenses/LICENSE-2.0
 *
 *    Unless required by applicable law or agreed to in writing, software
 *    distributed under the License is distributed on an "AS IS" BASIS,
 *    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *    See the License for the specific language governing permissions and
 *    limitations under the License.
 *
 * ************************************************************/

#endregion
