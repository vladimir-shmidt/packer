﻿using System;
using System.Collections.Concurrent;
using System.IO;

namespace packer
{
    public class CompressStrategy : IStrategy
    {
        private readonly CompressFactory _factory;
        private readonly CompressSettings _settings;

        public CompressStrategy(CompressFactory factory, CompressSettings settings)
        {
            _factory = factory;
            _settings = settings;
        }

        public void Work(string source, string destination)
        {
            var sourceInfo = new FileInfo(source);
            var sourceChunkCount = (int)Math.Ceiling(sourceInfo.Length / (decimal)_settings.ChinkSize);
            
            var chuncksMetadata = new ConcurrentBag<Chunk>();

            using (var pool = _factory.GetThreadPool())
            {
                using (var manager = _factory.GetFileManager(destination))
                {
                    var reader = _factory.GetByteReader(source);
                    var writer = _factory.GetByteWriter(manager);
                    var compressor = _factory.GetCompressor();

                    for (var i = 0; i < sourceChunkCount; i++)
                    {
                        var index = i;
                        var read = pool.Queue(ThreadPool.QueueType.Read, () => reader.Read(index, _settings.ChinkSize * (long)index, _settings.ChinkSize));
                        var zip = read.Then(ThreadPool.QueueType.Zip, () => compressor.Zip(read.Result, index));
                        zip.Then(ThreadPool.QueueType.Write, () => chuncksMetadata.Add(writer.Write(zip.Result, index)));
                    }
                    pool.Wait();
                }
            }

            var metadata = _factory.GetMetadataWriter(destination);
            metadata.Write(chuncksMetadata);
        }
    }
}