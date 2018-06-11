﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace DotNetOutdated.Services
{
    internal class NuGetPackageInfoService : INuGetPackageInfoService, IDisposable
    {
        private readonly SourceCacheContext _context;
        private readonly Dictionary<string, PackageMetadataResource> _metadataResources = new Dictionary<string, PackageMetadataResource>();
        
        public NuGetPackageInfoService()
        {
            _context = new SourceCacheContext()
            {
                NoCache = true
            };
        }

        private async Task<PackageMetadataResource> FindMetadataResourceForSource(Uri source)
        {
            string resourceUrl = source.AbsoluteUri;

            var resource = _metadataResources.GetValueOrDefault(resourceUrl);
            if (resource == null)
            {
                var sourceRepository = Repository.Factory.GetCoreV3(resourceUrl);
                resource = await sourceRepository.GetResourceAsync<PackageMetadataResource>();

                _metadataResources.Add(resourceUrl, resource);
            }

            return resource;
        }

        public async Task<IEnumerable<NuGetVersion>> GetAllVersions(string package, List<Uri> sources, bool includePrerelease)
        {
            var allVersions = new List<NuGetVersion>();
            foreach (var source in sources)
            {
                var metadata = await FindMetadataResourceForSource(source);

                var searchMetadata = await metadata.GetMetadataAsync(package, includePrerelease, false, _context, NuGet.Common.NullLogger.Instance, CancellationToken.None);
                allVersions.AddRange(searchMetadata.OfType<PackageSearchMetadata>().Select(m => m.Version));
            }

            return allVersions;
        }

        public void Dispose()
        {
            _context?.Dispose();
        }
    }

}