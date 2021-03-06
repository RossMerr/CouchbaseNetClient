﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Couchbase.Configuration.Client;
using Couchbase.Configuration.Server.Serialization;
using Couchbase.Utils;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Couchbase.Configuration.Server.Providers.Streaming
{
    /// <summary>
    /// A configuration info class for supporting HTTP streaming provider.
    /// </summary>
    internal class HttpServerConfig : IServerConfig
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger Log;
        private readonly ClientConfiguration _clientConfig;
        private readonly HttpClient _httpClient;
        private readonly string _bucketName;

        public HttpServerConfig(ClientConfiguration clientConfig, ILoggerFactory loggerFactory)
            : this(clientConfig, "default", string.Empty, loggerFactory)
        {
        }

        public HttpServerConfig(ClientConfiguration clientConfig, string bucketName, string password, ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
            Log = _loggerFactory.CreateLogger<HttpServerConfig>();
            _clientConfig = clientConfig;
            _bucketName = bucketName;

            _httpClient = new HttpClient(new AuthenticatingHttpClientHandler(bucketName, password)
            {
                ServerCertificateCustomValidationCallback = ServerCertificateValidationCallback
            });
        }

        public string BucketName
        {
            get { return _bucketName; }
        }

        public Uri BootstrapServer { get; protected internal set; }

        public Pools Pools { get; protected internal set; }

        public List<BucketConfig> Buckets { get; protected internal set; }

        public Bootstrap Bootstrap { get; protected internal set; }

        public List<BucketConfig> StreamingHttp { get; set; }

        public void Initialize()
        {
            var servers = _clientConfig.Servers.Shuffle().ToList();
            var hasBootStrapped = servers.Any(DownloadConfigs);
            if (!hasBootStrapped)
            {
                throw new BootstrapException("Could not bootstrap from configured servers list.");
            }
        }

        bool DownloadConfigs(Uri server)
        {
            var success = false;
            try
            {
                Log.LogInformation("Bootstrapping from {0}", server);
                Bootstrap = DownLoadConfig<Bootstrap>(server);
                Pools = DownLoadConfig<Pools>(Bootstrap.GetPoolsUri(server));
                Buckets = DownLoadConfig<List<BucketConfig>>(Pools.GetBucketUri(server));
                WriteTerseUris(Buckets, Pools);
                UpdateUseSsl(Buckets);
                BootstrapServer = server;
                success = true;
                Log.LogInformation("Bootstrapped from {0}", server);
            }
            catch (BootstrapException e)
            {
                Log.LogError(e.Message);
            }
            catch (Exception e)
            {
                Log.LogError("Bootstrapping failed from {0}: {1}", server, e);
                throw;
            }
            return success;
        }

        void WriteTerseUris(IEnumerable<BucketConfig> bucketConfigs, Pools pools)
        {
            var buckets = pools.Buckets;
            foreach (var bucketConfig in bucketConfigs)
            {
                bucketConfig.TerseUri = string.Concat(buckets.TerseBucketsBase, bucketConfig.Name);
                bucketConfig.TerseStreamingUri = string.Concat(buckets.TerseStreamingBucketsBase, bucketConfig.Name);
            }
        }

        void UpdateUseSsl(IEnumerable<BucketConfig> bucketConfigs)
        {
            foreach (var bucketConfig in bucketConfigs)
            {
                if (_clientConfig.BucketConfigs.ContainsKey(bucketConfig.Name))
                {
                    bucketConfig.UseSsl = _clientConfig.BucketConfigs[bucketConfig.Name].UseSsl;
                }
            }
        }

        private string DownloadString(Uri uri)
        {
            using (new SynchronizationContextExclusion())
            {
                try
                {
                
                    var response = _httpClient.GetAsync(uri).Result;

                    if (!response.IsSuccessStatusCode)
                    {
                        if (response.StatusCode == HttpStatusCode.Unauthorized)
                        {
                            throw new AuthenticationException(BucketName);
                        }
                        else
                        {
                            // Throws a more generic HttpRequestException
                            response.EnsureSuccessStatusCode();
                        }
                    }

                    return response.Content.ReadAsStringAsync().Result;
                }
                catch (AggregateException ex)
                {
                    // Unwrap the aggregate exception
                    throw new HttpRequestException(ex.InnerException.Message, ex.InnerException);
                }
            }
        }

        T DownLoadConfig<T>(Uri uri)
        {
            // This should now be working from the HttpCleint ServerCertificateCustomValidationCallback properties, see the constructor 
            //ServicePointManager.ServerCertificateValidationCallback += ServerCertificateValidationCallback;
            var response = ReplaceHost(DownloadString(uri), uri);
            return JsonConvert.DeserializeObject<T>(response);
        }

        private bool ServerCertificateValidationCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            Log.LogInformation("Validating certificate [IgnoreRemoteCertificateNameMismatch={0}]: {1}", ClientConfiguration.IgnoreRemoteCertificateNameMismatch, sslPolicyErrors);

            if (ClientConfiguration.IgnoreRemoteCertificateNameMismatch)
            {
                if (sslPolicyErrors == SslPolicyErrors.RemoteCertificateNameMismatch)
                {
                    return true;
                }
            }
            return sslPolicyErrors == SslPolicyErrors.None;
        }

        static string ReplaceHost(string response, Uri uri)
        {
            const string placeholder = "$HOST";
            return response.Replace(placeholder, uri.Host);
        }

        public void Dispose()
        {
            if (_httpClient != null)
            {
                _httpClient.Dispose();
            }
        }
    }
}

#region [ License information ]

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