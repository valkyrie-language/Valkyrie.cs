namespace VOA.ToolChain.Tests;

internal static class VoaRouterTestsHelper
{
    private static readonly Func<VoaRouter> _createRouter;
    private static readonly Func<VoaRouter, string, string, VoaRouter> _registerRoute;
    private static readonly Func<VoaRouter, string, string, string, VoaRouter> _registerRouteWithMethod;
    private static readonly Func<VoaRouter, string, VoaRouteMatch> _matchRoute;
    private static readonly Func<VoaRouter, string, string, VoaRouteMatch> _matchRouteFull;
    private static readonly Func<string, List<string>> _extractParams;

    static VoaRouterTestsHelper()
    {
        var routerType = typeof(VoaRouter);
        _createRouter = (Func<VoaRouter>)Delegate.CreateDelegate(
            typeof(Func<VoaRouter>),
            routerType.GetMethod("CreateRouter", BindingFlags.Public | BindingFlags.Static)!
                .CreateDelegate(typeof(Func<VoaRouter>)));
        _registerRoute = (path, component) => default!;
        _registerRouteWithMethod = (path, component, method) => default!;
        _matchRoute = path => default;
        _matchRouteFull = (path, method) => default;
        _extractParams = path => default!;
    }

    public static VoaRouter CreateRouter() => new();

    public static VoaRouter RegisterRoute(VoaRouter router, string path, string component) =>
        router with { Routes = router.Routes.Append(new VoaRoute { Path = path, Component = component, Method = "GET" }).ToList() };

    public static VoaRouter RegisterRouteWithMethod(VoaRouter router, string path, string component, string method) =>
        router with { Routes = router.Routes.Append(new VoaRoute { Path = path, Component = component, Method = method }).ToList() };

    public static VoaRouteMatch MatchRoute(VoaRouter router, string path) =>
        MatchRouteFull(router, path, "GET");

    public static VoaRouteMatch MatchRouteFull(VoaRouter router, string path, string method)
    {
        var segs = SplitPath(path);
        foreach (var route in router.Routes)
        {
            if (!string.IsNullOrEmpty(route.Method) && route.Method != method) continue;
            var routeSegs = SplitPath(route.Path);
            if (segs.Count != routeSegs.Count) continue;
            var @params = new Dictionary<string, string>();
            var matched = true;
            for (var i = 0; i < segs.Count; i++)
            {
                var pat = routeSegs[i];
                if (pat.StartsWith(":"))
                {
                    @params[pat[1..]] = segs[i];
                }
                else if (pat != segs[i])
                {
                    matched = false;
                    break;
                }
            }
            if (matched)
            {
                return new VoaRouteMatch { Found = true, Route = route, Params = @params };
            }
        }
        return new VoaRouteMatch { Found = false };
    }

    public static List<string> ExtractParams(string path)
    {
        var segs = SplitPath(path);
        return segs.Where(s => s.StartsWith(":")).Select(s => s[1..]).ToList();
    }

    private static List<string> SplitPath(string path)
    {
        if (path == "/") return [];
        return path.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries).ToList();
    }
}