﻿
namespace Couchbase.IO.Operations
{
    /// <summary>
    /// Features that they client negotiate on a per connection basis.
    /// </summary>
    internal enum ServerFeatures : short
    {
        /// <summary>
        /// For custom data types
        /// </summary>
        Datatype = 0x01,

        /// <summary>
        /// Enable TCP nodelay
        /// </summary>
        TcpNoDelay = 0x03,

        /// <summary>
        ///  Return the sequence number with every mutation
        /// </summary>
        MutationSeqno = 0x04,

        /// <summary>
        /// Disable TCP nodelay
        /// </summary>
        TcpDelay = 0x05
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
