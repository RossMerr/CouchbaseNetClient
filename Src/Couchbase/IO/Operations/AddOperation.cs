﻿using Couchbase.Core;

namespace Couchbase.IO.Operations
{
    /// <summary>
    /// Add a key to the database, failing if the key exists.
    /// </summary>
    /// <typeparam name="T">The value to add to the database.</typeparam>
    internal sealed class AddOperation<T> : OperationBase<T>
    {
        public AddOperation(string key, T value, IVBucket vBucket)
            : base(key, value, vBucket)
        {
        }

        public override OperationCode OperationCode
        {
            get { return OperationCode.Add; }
        }
    }
}

#region [ License information          ]

/* ************************************************************
 *
 *    @author Couchbase <info@couchbase.com>
 *    @copyright 2014 Couchbase, Inc.
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