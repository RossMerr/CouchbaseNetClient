﻿using System;
using System.Reflection;

#if NET45
using Couchbase.Configuration.Client.Providers;
#endif

namespace Couchbase.IO.Converters
{
    /// <summary>
    /// A factory for creating <see cref="IByteConverter"/> functories.
    /// </summary>
    public static class ConverterFactory
    {
        /// <summary>
        /// Gets a <see cref="Func{IByteConverter}"/> factory for the default converter: <see cref="DefaultConverter"/>
        /// </summary>
        /// <returns>A func for creating <see cref="DefaultConverter"/> instances.</returns>
        public static Func<IByteConverter> GetConverter()
        {
            return () => new DefaultConverter();
        }

#if NET45

        /// <summary>
        /// Gets a <see cref="Func{IByteConverter}"/> factory for custom <see cref="IByteConverter"/>s conifgured in the App.Config.
        /// </summary>
        /// <param name="element">The <see cref="ConverterElement"/> from the App.Config.</param>
        /// <returns>A func for creating custom <see cref="IByteConverter"/> instances.</returns>
        public static Func<IByteConverter> GetConverter(ConverterElement element)
        {
            return GetConverter(element.Type);
        }

#endif

        /// <summary>
        /// Gets a <see cref="Func{IByteConverter}"/> factory for custom <see cref="IByteConverter"/>s conifgured in the App.Config.
        /// </summary>
        /// <param name="typeName">The name of the type implementing <see cref="IByteConverter"/>.</param>
        /// <returns>A func for creating custom <see cref="IByteConverter"/> instances.</returns>
        public static Func<IByteConverter> GetConverter(string typeName)
        {
            var type = Type.GetType(typeName, true);
            return () => (IByteConverter)Activator.CreateInstance(type);
        }
    }
}
