namespace VOA.ToolChain.Tests;

public sealed class VoaRouterTests
{
    [Fact]
    public void CreateRouter_HasEmptyRoutes()
    {
        var router = VoaRouterTestsHelper.CreateRouter();

        Assert.NotNull(router);
        Assert.Empty(router.Routes);
        Assert.False(router.IsCompiled);
    }

    [Fact]
    public void RegisterRoute_AddsRouteToList()
    {
        var router = VoaRouterTestsHelper.CreateRouter();
        router = VoaRouterTestsHelper.RegisterRoute(router, "/home", "HomePage");

        Assert.Single(router.Routes);
        Assert.Equal("/home", router.Routes[0].Path);
        Assert.Equal("HomePage", router.Routes[0].Component);
    }

    [Fact]
    public void ExtractParams_StaticSegments_ReturnsEmpty()
    {
        var route = VoaRouterTestsHelper.ExtractParams("/home/about");

        Assert.Empty(route);
    }

    [Fact]
    public void ExtractParams_DynamicSegments_ReturnsParamNames()
    {
        var route = VoaRouterTestsHelper.ExtractParams("/user/:id/profile/:section");

        Assert.Equal(2, route.Count);
        Assert.Contains("id", route);
        Assert.Contains("section", route);
    }

    [Fact]
    public void MatchRoute_ExactMatch_ReturnsRoute()
    {
        var router = VoaRouterTestsHelper.CreateRouter();
        router = VoaRouterTestsHelper.RegisterRoute(router, "/about", "AboutPage");
        router = VoaRouterTestsHelper.RegisterRoute(router, "/", "HomePage");

        var match = VoaRouterTestsHelper.MatchRoute(router, "/about");

        Assert.True(match.Found);
        Assert.Equal("AboutPage", match.Route.Component);
    }

    [Fact]
    public void MatchRoute_NoMatch_ReturnsNotFound()
    {
        var router = VoaRouterTestsHelper.CreateRouter();
        router = VoaRouterTestsHelper.RegisterRoute(router, "/home", "HomePage");

        var match = VoaRouterTestsHelper.MatchRoute(router, "/nonexistent");

        Assert.False(match.Found);
    }

    [Fact]
    public void MatchRoute_DynamicSegment_ExtractsParam()
    {
        var router = VoaRouterTestsHelper.CreateRouter();
        router = VoaRouterTestsHelper.RegisterRoute(router, "/user/:id", "UserPage");

        var match = VoaRouterTestsHelper.MatchRoute(router, "/user/123");

        Assert.True(match.Found);
        Assert.Equal("123", match.Params["id"]);
    }

    [Fact]
    public void MatchRoute_MultipleParams_ExtractsAll()
    {
        var router = VoaRouterTestsHelper.CreateRouter();
        router = VoaRouterTestsHelper.RegisterRoute(router, "/post/:postId/comment/:commentId", "CommentPage");

        var match = VoaRouterTestsHelper.MatchRoute(router, "/post/10/comment/20");

        Assert.True(match.Found);
        Assert.Equal("10", match.Params["postId"]);
        Assert.Equal("20", match.Params["commentId"]);
    }

    [Fact]
    public void MatchRoute_WrongMethod_SkipsRoute()
    {
        var router = VoaRouterTestsHelper.CreateRouter();
        router = VoaRouterTestsHelper.RegisterRouteWithMethod(router, "/api/data", "ApiData", "POST");

        var match = VoaRouterTestsHelper.MatchRoute(router, "/api/data");

        Assert.False(match.Found);
    }

    [Fact]
    public void MatchRoute_CorrectMethod_MatchesRoute()
    {
        var router = VoaRouterTestsHelper.CreateRouter();
        router = VoaRouterTestsHelper.RegisterRouteWithMethod(router, "/api/data", "ApiData", "POST");

        var match = VoaRouterTestsHelper.MatchRouteFull(router, "/api/data", "POST");

        Assert.True(match.Found);
        Assert.Equal("ApiData", match.Route.Component);
    }
}