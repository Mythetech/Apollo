using Apollo.Contracts.Hosting;

namespace Apollo.Components.Hosting;

public class RouteViewModel
{
    public RouteViewModel(RouteInfo route)
    {
        Route = route;
        Segments = BuildSegments().ToList();
    }

    public RouteInfo? Route { get; }
    public List<RouteSegment> Segments { get; }

    private IEnumerable<RouteSegment> BuildSegments()
    {
        var rawSegments = Route.Pattern.Split('/', StringSplitOptions.RemoveEmptyEntries);
        
        // Always start with a separator
        yield return new RouteSegment("/", RouteSegmentType.Separator);

        for (int i = 0; i < rawSegments.Length; i++)
        {
            var segment = rawSegments[i];
            
            if (i > 0)
                yield return new RouteSegment("/", RouteSegmentType.Separator);

            if (segment.StartsWith("{") && segment.EndsWith("}"))
            {
                yield return new RouteSegment(
                    segment.Trim('{', '}'), 
                    RouteSegmentType.Parameter
                );
            }
            else
            {
                yield return new RouteSegment(segment, RouteSegmentType.Literal);
            }
        }
    }

    public IEnumerable<RouteSegment> GetSegments() => Segments;
    
    public RouteInfo GetValuedRoute() => new RouteInfo(GetRouteWithValues(), Route.Method, Route.ResponseType);

    public string GetRouteWithValues()
    {
        var segments = new List<string>();
        
        foreach (var segment in Segments)
        {
            switch (segment.Type)
            {
                case RouteSegmentType.Parameter when !string.IsNullOrWhiteSpace(segment.RouteValue):
                    segments.Add(segment.RouteValue);
                    break;
                case RouteSegmentType.Parameter:
                    segments.Add($"{{{segment.Value}}}");  // Keep original parameter if no value
                    break;
                case RouteSegmentType.Literal:
                    segments.Add(segment.Value);
                    break;
            }
        }

        return string.Join("/", segments).TrimEnd('/');
    }
}

public class RouteSegment
{
    public RouteSegment(string value, RouteSegmentType type, string? routeValue = null)
    {
        Value = value;
        Type = type;
        RouteValue = routeValue;
    }

    public string Value { get; set; }
    public RouteSegmentType Type { get; set; }

    public string? RouteValue { get; set; }
}

public enum RouteSegmentType
{
    Literal,
    Parameter,
    Separator
}

public static class RouteInfoExtensions
{
    public static RouteViewModel ToViewModel(this RouteInfo route)
    {
        return new RouteViewModel(route);
    }
}