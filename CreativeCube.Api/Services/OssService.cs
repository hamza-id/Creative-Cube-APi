using Aliyun.OSS;
using CreativeCube.Api.Config;
using Microsoft.Extensions.Options;

namespace CreativeCube.Api.Services;

public class OssService
{
    private readonly OssClient _client;
    private readonly OssOptions _options;

    public OssService(IOptions<OssOptions> options)
    {
        _options = options.Value;
        _client = new OssClient(_options.Endpoint, _options.AccessKeyId, _options.AccessKeySecret);
    }

    public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string folder = "blueprints")
    {
        var objectKey = $"{folder}/{Guid.NewGuid()}_{fileName}";
        
        var metadata = new ObjectMetadata
        {
            ContentType = GetContentType(fileName)
        };

        await Task.Run(() =>
        {
            _client.PutObject(_options.BucketName, objectKey, fileStream, metadata);
        });

        return $"{_options.BaseUrl.TrimEnd('/')}/{objectKey}";
    }

    public async Task<bool> DeleteFileAsync(string fileUrl)
    {
        try
        {
            // Extract object key from URL
            var objectKey = ExtractObjectKeyFromUrl(fileUrl);
            if (string.IsNullOrEmpty(objectKey))
                return false;

            await Task.Run(() =>
            {
                _client.DeleteObject(_options.BucketName, objectKey);
            });

            return true;
        }
        catch
        {
            return false;
        }
    }

    public string GeneratePresignedUrl(string objectKey, int expirationMinutes = 60)
    {
        var request = new GeneratePresignedUriRequest(_options.BucketName, objectKey, SignHttpMethod.Get)
        {
            Expiration = DateTime.UtcNow.AddMinutes(expirationMinutes)
        };

        var uri = _client.GeneratePresignedUri(request);
        return uri.ToString();
    }

    private string GetContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".pdf" => "application/pdf",
            ".dwg" => "application/acad",
            ".dxf" => "application/dxf",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".tiff" or ".tif" => "image/tiff",
            _ => "application/octet-stream"
        };
    }

    private string? ExtractObjectKeyFromUrl(string url)
    {
        if (string.IsNullOrEmpty(url))
            return null;

        try
        {
            var uri = new Uri(url);
            return uri.AbsolutePath.TrimStart('/');
        }
        catch
        {
            return null;
        }
    }
}

