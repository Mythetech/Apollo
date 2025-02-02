using System;
using System.Collections.Generic;
using System.Linq;
using Apollo.Contracts.Hosting;
using Apollo.Hosting;

public class RoutePattern
{
    public string Pattern { get; }
    
    public HttpMethodType Method { get; }
    
    public List<string> ParameterNames { get; }
    private readonly string[] _segments;

    public RoutePattern(string pattern, HttpMethodType method)
    {
        Pattern = pattern;
        Method = method;
        _segments = pattern.Split('/', StringSplitOptions.RemoveEmptyEntries);
        ParameterNames = _segments
            .Where(s => s.StartsWith("{") && s.EndsWith("}"))
            .Select(s => s.Trim('{', '}'))
            .ToList();
    }

    public bool TryMatch(string path, HttpMethodType method, out Dictionary<string, string> parameters)
    {
        parameters = new Dictionary<string, string>();
        
        if (Method != method)
            return false;
            
        var pathSegments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (pathSegments.Length != _segments.Length)
            return false;

        for (var i = 0; i < _segments.Length; i++)
        {
            if (_segments[i].StartsWith("{") && _segments[i].EndsWith("}"))
            {
                var paramName = _segments[i].Trim('{', '}');
                parameters[paramName] = pathSegments[i];
            }
            else if (_segments[i] != pathSegments[i])
            {
                return false;
            }
        }

        return true;
    }
} 