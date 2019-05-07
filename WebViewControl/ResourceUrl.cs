﻿using System;
using System.Linq;
using System.Reflection;

namespace WebViewControl {

    public class ResourceUrl {

        public const string LocalScheme = "local";
        internal const string EmbeddedScheme = "https";
        internal const string CustomScheme = "custom";

        internal const string PathSeparator = "/";

        private const string AssemblyPathSeparator = ";";
        private const string AssemblyPrefix = "assembly:";
        private const string DefaultDomain = "webview{0}";

        private readonly string url;

        public ResourceUrl(params string[] path) {
            url = string.Join("/", path);
        }

        public ResourceUrl(Assembly assembly, params string[] path) : this(path) {
            var assemblyName = assembly.GetName().Name;
            if (url.StartsWith(PathSeparator)) {
                // only prefix with assembly if necessary, to avoid having the same resource loaded from multiple locations
                url = AssemblyPrefix + assemblyName + AssemblyPathSeparator + url.Substring(1);
            } else {
                url = assemblyName + PathSeparator + url;
            }
            url = BuildUrl(EmbeddedScheme, url);
        }

        internal ResourceUrl(string scheme, string path) {
            url = BuildUrl(scheme, path);
        }

        private static string BuildUrl(string scheme, string path) {
            return scheme + Uri.SchemeDelimiter + CombinePath(DefaultDomain, path);
        }

        private static string CombinePath(string path1, string path2) {
            return path1 + (path1.EndsWith(PathSeparator) ? "" : PathSeparator)  + (path2.StartsWith(PathSeparator) ? path2.Substring(1) : path2);
        }

        public override string ToString() {
            return string.Format(url, "");
        }

        private static bool ContainsAssemblyLocation(Uri url) {
            return url.Scheme == EmbeddedScheme && url.AbsolutePath.StartsWith(PathSeparator + AssemblyPrefix);
        }

        /// <summary>
        /// Supported syntax:
        /// embedded://webview/assembly:AssemblyName;Path/To/Resource
        /// embedded://webview/AssemblyName/Path/To/Resource (AssemblyName is also assumed as default namespace)
        /// </summary>
        internal static string[] GetEmbeddedResourcePath(Uri resourceUrl) {
            if (ContainsAssemblyLocation(resourceUrl)) {
                var indexOfPath = resourceUrl.AbsolutePath.IndexOf(AssemblyPathSeparator);
                return resourceUrl.AbsolutePath.Substring(indexOfPath + 1).Split(new [] { PathSeparator }, StringSplitOptions.None);
            }
            var uriParts = resourceUrl.Segments;
            return uriParts.Skip(1).Select(p => p.Replace(PathSeparator, "")).ToArray();
        }

        /// <summary>
        /// Supported syntax:
        /// embedded://webview/assembly:AssemblyName;Path/To/Resource
        /// embedded://webview/AssemblyName/Path/To/Resource (AssemblyName is also assumed as default namespace)
        /// </summary>
        internal static string GetEmbeddedResourceAssemblyName(Uri resourceUrl) {
            if (ContainsAssemblyLocation(resourceUrl)) {
                var resourcePath = resourceUrl.AbsolutePath.Substring((PathSeparator + AssemblyPrefix).Length);
                var indexOfPath = Math.Max(0, resourcePath.IndexOf(AssemblyPathSeparator));
                return resourcePath.Substring(0, indexOfPath);
            }
            if (resourceUrl.Segments.Length > 1) {
                var assemblySegment = resourceUrl.Segments[1];
                return assemblySegment.EndsWith(PathSeparator) ? assemblySegment.Substring(0, assemblySegment.Length - PathSeparator.Length) : assemblySegment; // default assembly name to the first path
            }
            return string.Empty;
        }

        internal string WithDomain(string domain) {
            return string.Format(url, domain); 
        }
    }
}
