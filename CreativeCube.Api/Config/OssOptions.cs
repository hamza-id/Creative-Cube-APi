namespace CreativeCube.Api.Config;

public class OssOptions
{
    public string Endpoint { get; set; } = string.Empty;
    public string AccessKeyId { get; set; } = string.Empty;
    public string AccessKeySecret { get; set; } = string.Empty;
    public string BucketName { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty; // CDN or public URL base
}

