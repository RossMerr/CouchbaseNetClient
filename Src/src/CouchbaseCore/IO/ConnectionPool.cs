﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using Couchbase.Configuration.Client;
using Couchbase.Core;
using Couchbase.IO.Converters;
using Microsoft.Extensions.Logging;

namespace Couchbase.IO
{
    /// <summary>
    /// Represents a pool of TCP connections to a Couchbase Server node.
    /// </summary>
    public class ConnectionPool<T> : IConnectionPool<T> where T : class, IConnection
    {
        private readonly ILogger Log;
        private readonly ConcurrentQueue<T> _store = new ConcurrentQueue<T>();
        private readonly Func<ConnectionPool<T>, IByteConverter, BufferAllocator, T> _factory;
        private readonly AutoResetEvent _autoResetEvent = new AutoResetEvent(false);
        private readonly PoolConfiguration _configuration;
        private readonly object _lock = new object();
        private readonly IByteConverter _converter;
        private int _count;
        private bool _disposed;
        private ConcurrentDictionary<Guid, T> _refs = new ConcurrentDictionary<Guid, T>();
        private Guid _identity = Guid.NewGuid();
        private int _acquireFailedCount;
        private readonly IServer _owner;
        private readonly BufferAllocator _bufferAllocator;

        internal ConnectionPool(PoolConfiguration configuration, IPEndPoint endPoint, ILoggerFactory loggerFactory)
            : this(configuration, endPoint, DefaultConnectionFactory.GetGeneric<T>(loggerFactory), new DefaultConverter(), loggerFactory)
        {
        }

        /// <summary>
        /// CTOR for testing/dependency injection.
        /// </summary>
        /// <param name="configuration">The <see cref="PoolConfiguration"/> to use.</param>
        /// <param name="endPoint">The <see cref="IPEndPoint"/> of the Couchbase Server.</param>
        /// <param name="factory">A functory for creating <see cref="IConnection"/> objects./></param>
        internal ConnectionPool(PoolConfiguration configuration, IPEndPoint endPoint,
            Func<ConnectionPool<T>, IByteConverter, BufferAllocator, T> factory, IByteConverter converter, ILoggerFactory loggerFactory)
        {
            _configuration = configuration;
            _factory = factory;
            _converter = converter;
            _bufferAllocator = Configuration.BufferAllocator(Configuration);
            EndPoint = endPoint;
            Log = loggerFactory.CreateLogger<ConnectionPool<T>>();
        }

        /// <summary>
        /// Gets a value indicating whether the pool failed to initialize properly.
        /// If for example, TCP connection to the server couldn't be made, then this
        /// would return false until the connection could be made (after the node went
        /// back online).
        /// </summary>
        /// <value>
        ///   <c>true</c> if initialization failed; otherwise, <c>false</c>.
        /// </value>
        public bool InitializationFailed { get; private set; }

        /// <summary>
        /// The configuration passed into the pool when it is created. It has fields
        /// for MaxSize, MinSize, etc.
        /// </summary>
        public PoolConfiguration Configuration
        {
            get { return _configuration; }
        }

        /// <summary>
        /// The <see cref="IPEndPoint"/> of the server that the <see cref="IConnection"/>s are connected to.
        /// </summary>
        public IPEndPoint EndPoint { get; set; }

        /// <summary>
        /// Returns a collection of <see cref="IConnection"/> objects.
        /// </summary>
        /// <remarks>Only returns what is available in the queue at the point in time it is called.</remarks>
        public IEnumerable<T> Connections
        {
            get { return _store.ToArray(); }
        }

        /// <summary>
        /// Gets the number of <see cref="IConnection"/> within the pool, whether or not they are available or not.
        /// </summary>
        /// <returns></returns>
        public int Count()
        {
            return _count;
        }

        /// <summary>
        /// Sets the initial state of the pool and adds the MinSize of <see cref="IConnection"/> object to the pool.
        /// </summary>After the <see cref="PoolConfiguration.MinSize"/> is reached, the pool will grow to <see cref="PoolConfiguration.MaxSize"/>
        /// and any pending requests will then wait for a <see cref="IConnection"/> to be released back into the pool.
        /// <remarks></remarks>
        public void Initialize()
        {
            lock (_lock)
            {
                var count = _configuration.MinSize;
                do
                {
                    try
                    {
                        var connection = _factory(this, _converter, _bufferAllocator);
                        Log.LogInformation("Initializing connection on [{0} | {1}] - {2} - Disposed: {3}",
                            EndPoint, connection.Identity, _identity, _disposed);

                        _store.Enqueue(connection);
                        _refs.TryAdd(connection.Identity, connection);
                        Interlocked.Increment(ref _count);
                    }
                    catch (Exception e)
                    {
                        Log.LogInformation("Node {0} failed to initialize, reason: {1}", EndPoint, e);
                        InitializationFailed = true;
                        return;
                    }
                } while (_store.Count < count);
            }
        }

        /// <summary>
        /// Returns a <see cref="IConnection"/> the pool, creating a new one if none are available
        /// and the <see cref="PoolConfiguration.MaxSize"/> has not been reached.
        /// </summary>
        /// <returns>A TCP <see cref="IConnection"/> object to a Couchbase Server.</returns>
        /// <exception cref="ConnectionUnavailableException">thrown if a thread waits more than the <see cref="PoolConfiguration.MaxAcquireIterationCount"/>.</exception>
        public T Acquire()
        {
            T connection = AcquireFromPool();
            System.Console.WriteLine("1.1");

            if (connection != null)
                return connection;

            lock (_lock)
            {
                System.Console.WriteLine("1.2");
                //try to get connection from pool
                //in case connection released while operation waited in Monitor.Enter (lock)
                connection = AcquireFromPool();
                
                if (connection != null)
                    return connection;
                System.Console.WriteLine("1.3");
                if (_count < _configuration.MaxSize && !_disposed)
                {
                    System.Console.WriteLine("1.3.1");
                    Log.LogInformation("Trying to acquire new connection!");
                    System.Console.WriteLine("1.3.2");
                    connection = _factory(this, _converter, _bufferAllocator);
                    System.Console.WriteLine("1.3.3");
                    _refs.TryAdd(connection.Identity, connection);
                    System.Console.WriteLine("1.3.4");
                    Log.LogInformation("Acquire new: {0} | {1} | [{2}, {3}] - {4} - Disposed: {5}",
                        connection.Identity, EndPoint, _store.Count, _count, _identity, _disposed);
                    System.Console.WriteLine("1.3.5");
                    Interlocked.Increment(ref _count);
                    System.Console.WriteLine("1.3.6");
                    Interlocked.Exchange(ref _acquireFailedCount, 0);
                    System.Console.WriteLine("1.3.7");
                    connection.MarkUsed(true);
                    System.Console.WriteLine("1.3.8");
                    return connection;
                }
            }
            System.Console.WriteLine("1.4");

            _autoResetEvent.WaitOne(_configuration.WaitTimeout);
            System.Console.WriteLine("1.5");
            var acquireFailedCount = Interlocked.Increment(ref _acquireFailedCount);
            System.Console.WriteLine("1.6");
            if (acquireFailedCount >= _configuration.MaxAcquireIterationCount)
            {
                System.Console.WriteLine("1.7");
                Interlocked.Exchange(ref _acquireFailedCount, 0);
                const string msg = "Failed to acquire a pooled client connection on {0} after {1} tries.";
                throw new ConnectionUnavailableException(msg, EndPoint, acquireFailedCount);
            }
            System.Console.WriteLine("1.8");
            return Acquire();
        }

        /// <summary>
        /// Returns a <see cref="IConnection"/> from the pool.
        /// </summary>
        /// <returns>A TCP <see cref="IConnection"/> object to a Couchbase Server.</returns>
        private T AcquireFromPool()
        {
            T connection;

            if (_store.TryDequeue(out connection) && !_disposed)
            {
                System.Console.WriteLine("2.1");
                Interlocked.Exchange(ref _acquireFailedCount, 0);
                System.Console.WriteLine("2.2");
                Log.LogDebug("Acquire existing: {0} | {1} | [{2}, {3}] - {4} - Disposed: {5}",
                    connection.Identity, EndPoint, _store.Count, _count, _identity, _disposed);
                System.Console.WriteLine("2.3");
                connection.MarkUsed(true);
                return connection;
            }

            return null;
        }

        /// <summary>
        /// Releases an acquired <see cref="IConnection"/> object back into the pool so that it can be reused by another operation.
        /// </summary>
        /// <param name="connection">The <see cref="IConnection"/> to release back into the pool.</param>
        public void Release(T connection)
        {
            Log.LogDebug("Releasing: {0} on {1} - {2}", connection.Identity, EndPoint, _identity);
            connection.MarkUsed(false);
            if (connection.IsDead)
            {
                connection.Dispose();
                Interlocked.Decrement(ref _count);
                Log.LogInformation("Connection is dead: {0} on {1} - {2} - [{3}, {4}] ",
                    connection.Identity, EndPoint, _identity, _store.Count, _count);

                if (Owner != null)
                {
                    Owner.CheckOnline(connection.IsDead);
                }

                lock (_lock)
                {
                    T old;
                    if (_refs.TryRemove(connection.Identity, out old))
                    {
                        old.Dispose();
                    }
                }
            }
            else
            {
                _store.Enqueue(connection);
            }
            _autoResetEvent.Set();
        }

        /// <summary>
        /// Removes and disposes all <see cref="IConnection"/> objects in the pool.
        /// </summary>
        public void Dispose()
        {
            Log.LogDebug("Disposing ConnectionPool for {0} - {1}", EndPoint, _identity);
            lock (_lock)
            {
                Dispose(true);
            }
        }

        public void Dispose(bool disposing)
        {
            if (disposing)
            {
                GC.SuppressFinalize(this);
            }
            if (!_disposed)
            {
                _disposed = true;
                var interval = _configuration.CloseAttemptInterval;

                const int maxAttempts = 10;
                var attempts = 0;
                foreach (var key in _refs.Keys)
                {
                    Log.LogDebug("Trying to close conn {0}", key);
                    T conn;
                    if (_refs.TryGetValue(key, out conn) && conn != null && !conn.HasShutdown)
                    {
                        Log.LogDebug("Closing conn {0} - ", key, conn.Identity);
                        if (conn.InUse)
                        {
                            conn.CountdownToClose(interval);
                        }
                        else
                        {
                            lock (conn)
                            {
                                if (!conn.InUse)
                                {
                                    conn.Dispose();
                                    _refs.TryRemove(key, out conn);
                                }
                            }
                        }
                    }
                }
            }
        }

#if DEBUG
        ~ConnectionPool()
        {
            try
            {
                Log.LogDebug("Finalizing ConnectionPool for {0}", EndPoint);
                Dispose(false);
            }
            catch (Exception e)
            {
                //TODO temp fix since they may getting finalized...
                try
                {
                    Log.LogDebug(e.Message);
                }
                catch
                {
                }
            }
        }
#endif

        IConnection IConnectionPool.Acquire()
        {
            return Acquire();
        }

        void IConnectionPool.Release(IConnection connection)
        {
            Release((T) connection);
        }

        IEnumerable<IConnection> IConnectionPool.Connections
        {
            get { return _store.ToArray(); }
        }

        /// <summary>
        /// Gets or sets the <see cref="IServer" /> instance which "owns" this pool.
        /// </summary>
        /// <value>
        /// The owner.
        /// </value>
        public IServer Owner { get; set; }
    }
}