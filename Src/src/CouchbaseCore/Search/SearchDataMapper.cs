﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Couchbase.Views;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Couchbase.Search
{
    internal class SearchDataMapper  : IDataMapper
    {
        T IDataMapper.Map<T>(Stream stream)
        {
            var response = new SearchQueryResult();
            using (var reader = new JsonTextReader(new StreamReader(stream)))
            {
                while (reader.Read())
                {
                    ReadStatus(reader, response);
                    ReadHits(reader, response);
                }
            }
            return response as T;
        }

        void ReadFacets(JsonTextReader reader, SearchQueryResult response)
        {
            if (reader.TokenType == JsonToken.StartObject && reader.Path.Contains("facets["))
            {
                var facet = JObject.Load(reader);
                //todo
            }
        }

        void ReadHits(JsonTextReader reader, SearchQueryResult response)
        {
            if (reader.TokenType == JsonToken.StartObject && reader.Path.Contains("hits["))
            {
                var hit = JObject.Load(reader);
                response.Add(new SearchQueryRow
                {
                    Index = hit["index"].Value<string>(),
                    Id = hit["id"].Value<string>(),
                    Score = hit["score"].Value<double>(),
                    Explanation = ReadValue<dynamic>(hit, "explanation"),
                    Locations = ReadValue<dynamic>(hit, "locations"),
                    Fragments = ReadValue<Dictionary<string, List<string>>>(hit, "fragments"),
                    Fields = ReadValue<Dictionary<string, string>>(hit, "fields")
                });
            }
        }

        T ReadValue<T>(JObject hit, string field)
        {
            if (hit.Properties().Any(x => x.Name == field))
            {
                return hit[field].ToObject<T>();
            }
            return default(T);
        }

        void ReadStatus(JsonTextReader reader, SearchQueryResult response)
        {
            if (reader.Path == "total_hits")
            {
                var totalHits = reader.ReadAsInt32();
                if (totalHits != null)
                {
                    response.TotalHits = (long)totalHits;
                    return;
                }
            }
            if (reader.Path == "max_score")
            {
                var maxScore = reader.ReadAsDecimal();
                if (maxScore != null)
                {
                    response.MaxScore = (double)maxScore;
                    return;
                }
            }
            if (reader.Path == "took")
            {
                var took = reader.ReadAsString();
                if (took != null)
                {
                    response.Took = new TimeSpan(long.Parse(took));
                    return;
                }
            }
            if (reader.Path == "status.failed")
            {
                var failed = reader.ReadAsInt32();
                if (failed != null)
                {
                    response.ErrorCount = (long)failed;
                    response.Success = failed.Value >= 0;
                    return;
                }
            }
            if (reader.Path == "status.successful")
            {
                var success = reader.ReadAsInt32();
                if (success != null)
                {
                    response.SuccessCount = success.Value;
                    return;
                }
            }
            if (reader.Path == "status.total")
            {
                var total = reader.ReadAsInt32();
                if (total != null)
                {
                    response.TotalCount = total.Value;
                }
            }
            if (reader.Path == "status.errors")
            {
                var errors = reader.ReadAsString();
                if (errors != null)
                {
                    response.Errors = JsonConvert.DeserializeObject<List<string>>(errors);
                }
            }
        }

        public ISearchQueryResult Map(Stream stream)
        {
            return ((IDataMapper) this).Map<ISearchQueryResult>(stream);
        }
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
