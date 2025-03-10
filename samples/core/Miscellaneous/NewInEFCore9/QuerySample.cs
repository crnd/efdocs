﻿namespace NewInEfCore9;

public static class QuerySample
{
    public static Task Query_improvements_in_EF9()
    {
        PrintSampleName();
        return QueryTest<JsonBlogsContext>();
    }

    public static Task Query_improvements_in_EF9_on_SQLite()
    {
        PrintSampleName();
        return QueryTest<JsonBlogsContextSqlite>();
    }

    private static async Task QueryTest<TContext>()
        where TContext : BlogsContext, new()
    {
        await using var context = new TContext();
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();
        await context.Seed();

        context.LoggingEnabled = true;
        context.ChangeTracker.Clear();

        Console.WriteLine();
        Console.WriteLine("Default parameterization:");
        Console.WriteLine();

        _ = await GetPosts(1);

        #region DefaultParameterization
        async Task<List<Post>> GetPosts(int id)
            => await context.Posts
                .Where(
                    e => e.Title == ".NET Blog" && e.Id == id)
                .ToListAsync();
        #endregion

        Console.WriteLine();
        Console.WriteLine("Force parameterization of a constant:");
        Console.WriteLine();

        _ = await GetPostsForceParameter(1);

        #region ForceParameter
        async Task<List<Post>> GetPostsForceParameter(int id)
            => await context.Posts
                .Where(
                    e => e.Title == EF.Parameter(".NET Blog") && e.Id == id)
                .ToListAsync();
        #endregion

        Console.WriteLine();
        Console.WriteLine("Force constant:");
        Console.WriteLine();

        _ = await GetPostsForceConstant(1);

        #region ForceConstant
        async Task<List<Post>> GetPostsForceConstant(int id)
            => await context.Posts
                .Where(
                    e => e.Title == ".NET Blog" && e.Id == EF.Constant(id))
                .ToListAsync();
        #endregion

        Console.WriteLine();
        Console.WriteLine("Inline subquery:");
        Console.WriteLine();

        #region InlinedSubquery
        var dotnetPosts = context
            .Posts
            .Where(p => p.Title.Contains(".NET"));

        var results = dotnetPosts
            .Where(p => p.Id > 2)
            .Select(p => new { Post = p, TotalCount = dotnetPosts.Count() })
            .Skip(2).Take(10)
            .ToArray();
        #endregion

        Console.WriteLine();
        Console.WriteLine("ToHashSetAsync:");
        Console.WriteLine();

        #region ToHashSetAsync
        var set1 = await context.Posts
            .Where(p => p.Tags.Count > 3)
            .ToHashSetAsync();

        var set2 = await context.Posts
            .Where(p => p.Tags.Count > 3)
            .ToHashSetAsync(ReferenceEqualityComparer.Instance);
        #endregion

        Console.WriteLine();
        Console.WriteLine("Normalize Count != 0:");
        Console.WriteLine();

        #region NormalizeCount
        var blogsWithPost = await context.Blogs
            .Where(b => b.Posts.Count > 0)
            .ToListAsync();
        #endregion

        if (!context.UseSqlite)
        {
            #region PatIndexExample
            var patIndexExample = await context.Posts.Select(p => new
            {
                p.Id,
                Index = EF.Functions.PatIndex("%.NET%", p.Content)
            }).ToListAsync();
            #endregion
        }

        #region CaseTranslationImprovements
        var caseSimplification = await context.Blogs
            .Select(b => !(b.Id > 5 ? false : true))
            .ToListAsync();
        #endregion

        if (context.UseSqlite)
        {
            #region NegatedContainsImprovements
            var negatedContainsSimplification = await context.Posts
                .Where(p => !p.Content.Contains("Announcing"))
                .Select(p => new { p.Content }).ToListAsync();
            #endregion
        }

        #region XorBoolProjection
        var negatedBoolProjection = await context.Posts.Select(x => new { x.Title, Active = !x.Archived }).ToListAsync();
        #endregion

        #region EnumToString
        var englishAndSpanishBlogs = await context.Blogs
            .Where(x => x.Language.ToString().EndsWith("ish"))
            .Select(x => x.Name).ToListAsync();
        #endregion

        #region AverageOnDecimal
        var averagePostRating = await context.Blogs.Select(x => new
        {
            x.Name,
            AveragePostRating = x.Posts.Average(xx => xx.Rating)
        }).ToListAsync();
        #endregion

        #region ConvertFromObject
        var blogWithConversion = await context.Blogs
            .Where(x => Convert.ToDecimal((object)Convert.ToString(x.Id)) == 1.0M)
            .ToListAsync();
        #endregion
    }

    private static void PrintSampleName([CallerMemberName] string? methodName = null)
    {
        Console.WriteLine($">>>> Sample: {methodName}");
        Console.WriteLine();
    }
}
