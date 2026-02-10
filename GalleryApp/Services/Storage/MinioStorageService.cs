using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;

namespace GalleryApp.Services.Storage;

public class MinioStorageService : IStorageService
{
    private readonly IMinioClient _minio;
    private readonly string _bucket;

    public MinioStorageService(IOptions<MinioStorageOptions> options)
    {
        var o = options.Value;

        _bucket = o.Bucket;

        _minio = new MinioClient()
            .WithEndpoint(o.Endpoint)
            .WithCredentials(o.AccessKey, o.SecretKey)
            .WithSSL(o.UseSSL)
            .Build();
    }

    public async Task EnsureBucketAsync(CancellationToken ct = default)
    {
        var existsArgs = new BucketExistsArgs().WithBucket(_bucket);
        var exists = await _minio.BucketExistsAsync(existsArgs, ct);

        if (!exists)
        {
            var makeArgs = new MakeBucketArgs().WithBucket(_bucket);
            await _minio.MakeBucketAsync(makeArgs, ct);
        }
    }

    public async Task SaveAsync(string key, Stream content, string contentType, CancellationToken ct = default)
    {
        if (content.CanSeek) content.Position = 0;

        var putArgs = new PutObjectArgs()
            .WithBucket(_bucket)
            .WithObject(key)
            .WithStreamData(content)
            .WithObjectSize(content.Length)
            .WithContentType(contentType);

        await _minio.PutObjectAsync(putArgs, ct);
    }

    public async Task<Stream> OpenReadAsync(string key, CancellationToken ct = default)
    {
        var ms = new MemoryStream();

        var getArgs = new GetObjectArgs()
            .WithBucket(_bucket)
            .WithObject(key)
            .WithCallbackStream(stream =>
            {
                stream.CopyTo(ms);
            });

        await _minio.GetObjectAsync(getArgs, ct);

        ms.Position = 0;
        return ms;
    }

    public async Task DeleteAsync(string key, CancellationToken ct = default)
    {
        var rmArgs = new RemoveObjectArgs()
            .WithBucket(_bucket)
            .WithObject(key);

        await _minio.RemoveObjectAsync(rmArgs, ct);
    }
}
