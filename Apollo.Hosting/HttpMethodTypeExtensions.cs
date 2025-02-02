using Apollo.Contracts.Hosting;

namespace Apollo.Hosting;

public static class HttpMethodTypeExtensions 
{
    public static HttpMethod ToHttpMethod(this HttpMethodType type) => type switch
    {
        HttpMethodType.Get => HttpMethod.Get,
        HttpMethodType.Post => HttpMethod.Post,
        HttpMethodType.Put => HttpMethod.Put,
        HttpMethodType.Delete => HttpMethod.Delete,
        HttpMethodType.Patch => HttpMethod.Patch,
        HttpMethodType.Head => HttpMethod.Head,
        HttpMethodType.Options => HttpMethod.Options,
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };

    public static HttpMethodType ToHttpMethodType(this HttpMethod method)
    {
        if (method == HttpMethod.Get) return HttpMethodType.Get;
        if (method == HttpMethod.Post) return HttpMethodType.Post;
        if (method == HttpMethod.Put) return HttpMethodType.Put;
        if (method == HttpMethod.Delete) return HttpMethodType.Delete;
        if (method == HttpMethod.Patch) return HttpMethodType.Patch;
        if (method == HttpMethod.Head) return HttpMethodType.Head;
        if (method == HttpMethod.Options) return HttpMethodType.Options;
        
        throw new ArgumentOutOfRangeException(nameof(method));
    }
} 