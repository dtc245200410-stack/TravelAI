using System.Numerics;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TravelAI.Data;
using TravelAI.Models;
using TravelAI.Services;
using Xunit;

namespace TravelAI.Tests;

public sealed class TourRankingTests
{
    // Sixteen deliberately different catalogue rows exercise every combination of four requirements.
    private static async Task<List<Tour>> SeedRankingCatalogue(TravelDbContext db)
    {
        foreach (var tour in await db.Tours.ToListAsync()) tour.Status = "Inactive";
        var here = new Destination { Name = "Nơi xếp hạng", Slug = "noi-xep-hang" };
        var elsewhere = new Destination { Name = "Nơi khác", Slug = "noi-khac" };
        db.Destinations.AddRange(here, elsewhere); await db.SaveChangesAsync();
        var rows = Enumerable.Range(0, 16).Select(mask => new Tour
        {
            Name = "Mẫu xếp hạng " + mask,
            DestinationId = (mask & 1) != 0 ? here.Id : elsewhere.Id,
            Price = (mask & 2) != 0 ? 4000000 : 9000000,
            AvailablePeople = (mask & 4) != 0 ? 10 : 1,
            Duration = (mask & 8) != 0 ? 3 : 4,
            MaxPeople = 10, DepartureDate = DateTime.Today.AddDays(15)
        }).ToList();
        db.Tours.AddRange(rows); await db.SaveChangesAsync();
        return rows;
    }

    [Theory]
    [InlineData(1)] [InlineData(2)] [InlineData(4)] [InlineData(8)]
    [InlineData(3)] [InlineData(5)] [InlineData(9)] [InlineData(6)] [InlineData(10)] [InlineData(12)]
    [InlineData(7)] [InlineData(11)] [InlineData(13)] [InlineData(14)] [InlineData(15)]
    public async Task EveryNonEmptyCombinationReturnsAllRelatedToursOrderedByNumberOfMatches(int requirements)
    {
        using var factory = new AIFactory(); using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TravelDbContext>();
        var rows = await SeedRankingCatalogue(db);
        var parts = new List<string>();
        if ((requirements & 1) != 0) parts.Add("Nơi xếp hạng");
        if ((requirements & 2) != 0) parts.Add("giá dưới 5 triệu");
        if ((requirements & 4) != 0) parts.Add("cho 2 người");
        if ((requirements & 8) != 0) parts.Add("3 ngày");
        var result = await scope.ServiceProvider.GetRequiredService<TravelAIService>()
            .PrepareAsync("Tìm tour " + string.Join(" ", parts), [], default);
        var expectedMasks = Enumerable.Range(0, 16).Where(mask =>
            ((requirements & 1) == 0 || (mask & 1) != 0) && (mask & requirements) != 0).ToArray();
        Assert.Equal(expectedMasks.Select(mask => rows[mask].Id).Order(), result.Tours.Select(t => t.Id).Order());
        var scores = result.Tours.Select(t => BitOperations.PopCount((uint)(rows.FindIndex(row => row.Id == t.Id) & requirements))).ToArray();
        Assert.Equal(scores.OrderDescending(), scores);
        Assert.True(result.Tours.Count > 3); // Both complete matches and partial matches must be retained.
        foreach (var tour in result.Tours) Assert.Contains($"/Tour/Details/{tour.Id}", result.GroundedAnswer);
        using var evidence = JsonDocument.Parse(result.Evidence);
        Assert.Equal(BitOperations.PopCount((uint)requirements), evidence.RootElement.GetProperty("RequirementCount").GetInt32());
        Assert.Equal(expectedMasks.Length, evidence.RootElement.GetProperty("TotalResults").GetInt32());
        Assert.Equal(expectedMasks.Length, evidence.RootElement.GetProperty("RankingGroups").EnumerateArray().Sum(g => g.GetProperty("Count").GetInt32()));
        Assert.DoesNotContain("Không tìm thấy tour phù hợp", result.GroundedAnswer);
    }

    [Fact]
    public async Task FourRequirementsProduceFourThreeTwoOneGroupsEvenWhenBestTourIsExpired()
    {
        using var factory = new AIFactory(); using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TravelDbContext>(); var rows = await SeedRankingCatalogue(db);
        rows[15].DepartureDate = DateTime.Today.AddDays(-2); await db.SaveChangesAsync();
        var result = await scope.ServiceProvider.GetRequiredService<TravelAIService>()
            .PrepareAsync("Nơi xếp hạng giá 5 triệu 2 người 3 ngày", [], default);
        Assert.Equal(rows[15].Id, result.Tours[0].Id);
        var headings = Regex.Matches(result.GroundedAnswer, @"## Phù hợp (\d)/4 yêu cầu").Select(m => m.Groups[1].Value);
        Assert.Equal(new[] { "4", "3", "2", "1" }, headings);
        Assert.Contains("qua ngày khởi hành", result.GroundedAnswer);
        using var evidence = JsonDocument.Parse(result.Evidence);
        var best = evidence.RootElement.GetProperty("Tours")[0];
        Assert.Equal(4, best.GetProperty("MatchedRequirementCount").GetInt32());
        Assert.True(best.GetProperty("MatchesAllRequirements").GetBoolean());
        Assert.False(best.GetProperty("AvailableForBooking").GetBoolean());
    }

    [Theory]
    [InlineData("Hà Nội")]
    [InlineData("Hà Nội giá 5 triệu cho 2 người 3 ngày")]
    [InlineData("Paris")]
    [InlineData("Paris giá 5 triệu cho 2 người")]
    public async Task MissingRequestedPlaceReturnsNoToursEvenWhenOtherPlacesMeetPriceAndCapacity(string question)
    {
        using var factory = new AIFactory(); using var scope = factory.Services.CreateScope();
        await SeedRankingCatalogue(scope.ServiceProvider.GetRequiredService<TravelDbContext>());
        var result = await scope.ServiceProvider.GetRequiredService<TravelAIService>().PrepareAsync(question, [], default);
        Assert.Empty(result.Tours); Assert.DoesNotContain("/Tour/Details/", result.GroundedAnswer);
        Assert.Contains("Không tìm thấy tour phù hợp", result.GroundedAnswer);
    }

    [Fact]
    public async Task PlaceInTourNameRemainsRelatedEvenWhenLinkedDestinationAndEveryOtherRequirementDiffer()
    {
        using var factory = new AIFactory(); using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TravelDbContext>(); var rows = await SeedRankingCatalogue(db);
        rows[0].Name = "Khám phá Hà Nội cuối tuần"; rows[0].AvailablePeople = 0;
        rows[0].DepartureDate = DateTime.Today.AddDays(-1); await db.SaveChangesAsync();
        var result = await scope.ServiceProvider.GetRequiredService<TravelAIService>()
            .PrepareAsync("Hà Nội 5 triệu 2 người 3 ngày", [], default);
        Assert.Equal(rows[0].Id, Assert.Single(result.Tours).Id);
        Assert.Contains("Phù hợp 1/4 yêu cầu", result.GroundedAnswer);
        Assert.Contains("vượt ngân sách", result.GroundedAnswer); Assert.Contains("không đủ chỗ", result.GroundedAnswer);
        Assert.Contains("không đúng 3 ngày", result.GroundedAnswer); Assert.Contains("qua ngày khởi hành", result.GroundedAnswer);
        Assert.DoesNotContain("Không tìm thấy tour phù hợp", result.GroundedAnswer);
    }

    [Theory]
    [InlineData("5tr")]
    [InlineData("5 triệu")]
    [InlineData("5000000")]
    [InlineData("giá 5.000.000")]
    [InlineData("ngân sách tối đa 5 triệu")]
    public async Task PriceOnlyInputsDoNotBecomeUnknownPlaces(string question)
    {
        using var factory = new AIFactory(); using var scope = factory.Services.CreateScope();
        var rows = await SeedRankingCatalogue(scope.ServiceProvider.GetRequiredService<TravelDbContext>());
        var result = await scope.ServiceProvider.GetRequiredService<TravelAIService>().PrepareAsync(question, [], default);
        Assert.Equal(8, result.Tours.Count); Assert.All(result.Tours, t => Assert.True(t.Price <= 5000000));
        using var evidence = JsonDocument.Parse(result.Evidence);
        Assert.Equal(1, evidence.RootElement.GetProperty("RequirementCount").GetInt32());
        Assert.Equal(JsonValueKind.Null, evidence.RootElement.GetProperty("Topic").ValueKind);
    }

    [Fact]
    public async Task TotalBudgetWithPeopleCountsAsTwoCriteriaAndUsesSalePrice()
    {
        using var factory = new AIFactory(); using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TravelDbContext>(); var rows = await SeedRankingCatalogue(db);
        rows[4].SalePrice = 3000000; await db.SaveChangesAsync();
        var result = await scope.ServiceProvider.GetRequiredService<TravelAIService>()
            .PrepareAsync("Tư vấn tour cho 2 người, tổng ngân sách 10 triệu", [], default);
        using var evidence = JsonDocument.Parse(result.Evidence);
        Assert.Equal(2, evidence.RootElement.GetProperty("RequirementCount").GetInt32());
        Assert.Equal(12, result.Tours.Count);
        Assert.Equal(rows[4].Id, result.Tours[0].Id); Assert.Equal(3000000, result.Tours[0].Price);
        Assert.Contains("6.000.000", result.GroundedAnswer);
        Assert.Contains("Phù hợp 1/2 yêu cầu", result.GroundedAnswer);
    }

    [Fact]
    public async Task DateOnlyIsOneCriterionAndDoesNotInventDurationOrBudget()
    {
        using var factory = new AIFactory(); using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TravelDbContext>(); var rows = await SeedRankingCatalogue(db);
        rows[0].DepartureDate = DateTime.Today.AddDays(7); await db.SaveChangesAsync();
        var result = await scope.ServiceProvider.GetRequiredService<TravelAIService>()
            .PrepareAsync("tour khởi hành " + rows[0].DepartureDate.ToString("dd/MM/yyyy"), [], default);
        Assert.Equal(rows[0].Id, Assert.Single(result.Tours).Id);
        using var evidence = JsonDocument.Parse(result.Evidence);
        Assert.Equal(1, evidence.RootElement.GetProperty("RequirementCount").GetInt32());
        Assert.Equal(JsonValueKind.Null, evidence.RootElement.GetProperty("Days").ValueKind);
        Assert.Equal(JsonValueKind.Null, evidence.RootElement.GetProperty("Budget").ValueKind);
    }

    [Fact]
    public async Task FiveExplicitCriteriaGiveFiveLevelsWhileLocationRemainsRequired()
    {
        using var factory = new AIFactory(); using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TravelDbContext>(); var rows = await SeedRankingCatalogue(db);
        rows[15].DepartureDate = DateTime.Today.AddDays(7); await db.SaveChangesAsync();
        var result = await scope.ServiceProvider.GetRequiredService<TravelAIService>()
            .PrepareAsync("Nơi xếp hạng 5 triệu 2 người 3 ngày khởi hành " + rows[15].DepartureDate.ToString("dd/MM/yyyy"), [], default);
        Assert.Equal(8, result.Tours.Count); Assert.Equal(rows[15].Id, result.Tours[0].Id);
        Assert.Contains("Phù hợp 5/5 yêu cầu", result.GroundedAnswer); Assert.Contains("Phù hợp 1/5 yêu cầu", result.GroundedAnswer);
    }

    [Fact]
    public async Task NoConstraintsShowsWholePublicCatalogueAndZeroMatchesShowsNoResults()
    {
        using var factory = new AIFactory(); using var scope = factory.Services.CreateScope();
        var rows = await SeedRankingCatalogue(scope.ServiceProvider.GetRequiredService<TravelDbContext>());
        var service = scope.ServiceProvider.GetRequiredService<TravelAIService>();
        var all = await service.PrepareAsync("Tư vấn tour", [], default);
        Assert.Equal(16, all.Tours.Count); Assert.Contains("Chưa có điều kiện cụ thể", all.GroundedAnswer);
        var none = await service.PrepareAsync("giá dưới 1 đồng", [], default);
        Assert.Empty(none.Tours); Assert.Contains("Không tìm thấy tour phù hợp", none.GroundedAnswer);
    }

    [Fact]
    public async Task NewPlaceDoesNotInheritOldConstraintsAndUnknownNewPlaceDoesNotReuseOldPlace()
    {
        using var factory = new AIFactory(); using var scope = factory.Services.CreateScope();
        await SeedRankingCatalogue(scope.ServiceProvider.GetRequiredService<TravelDbContext>());
        var service = scope.ServiceProvider.GetRequiredService<TravelAIService>();
        AIHistoryMessage[] history = [new("User", "Nơi xếp hạng 5 triệu 2 người 3 ngày")];
        var other = await service.PrepareAsync("Nơi khác", history, default);
        Assert.Equal(8, other.Tours.Count); Assert.All(other.Tours, t => Assert.Equal("Nơi khác", t.Destination));
        using var evidence = JsonDocument.Parse(other.Evidence);
        Assert.Equal(1, evidence.RootElement.GetProperty("RequirementCount").GetInt32());
        var unknown = await service.PrepareAsync("Paris giá 5 triệu", history, default);
        Assert.Empty(unknown.Tours);
    }

    [Fact]
    public async Task TourTitleOrSlugEqualToAPlaceDoesNotHideOtherToursAtThatPlace()
    {
        using var factory = new AIFactory(); using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TravelDbContext>(); var rows = await SeedRankingCatalogue(db);
        rows[1].Name = "Nơi xếp hạng"; rows[1].Slug = "noi-xep-hang";
        rows[3].Name = "Tour Nơi xếp hạng 3 ngày";
        await db.SaveChangesAsync();
        var service = scope.ServiceProvider.GetRequiredService<TravelAIService>();
        var placeOnly = await service.PrepareAsync("Nơi xếp hạng", [], default);
        Assert.Equal(8, placeOnly.Tours.Count);
        var placeAndDays = await service.PrepareAsync("Tour Nơi xếp hạng 3 ngày", [], default);
        Assert.Equal(8, placeAndDays.Tours.Count); Assert.Contains("Phù hợp 1/2 yêu cầu", placeAndDays.GroundedAnswer);
    }

    [Fact]
    public async Task StreamingPersistsWholeRankedListWhileOnlySixRowsAreSentToOllama()
    {
        using var factory = new AIFactory(); using var client = factory.Client(); var token = await factory.Token(client);
        using var scope = factory.Services.CreateScope(); var rows = await SeedRankingCatalogue(scope.ServiceProvider.GetRequiredService<TravelDbContext>());
        var question = "Nơi xếp hạng 5 triệu 2 người 3 ngày";
        var response = await client.PostAsync("/AI/Chat", new FormUrlEncodedContent(new Dictionary<string, string>
        { ["message"] = question, ["__RequestVerificationToken"] = token }));
        response.EnsureSuccessStatusCode(); var sse = await response.Content.ReadAsStringAsync();
        var frames = sse.Split('\n').Where(line => line.StartsWith("data: ")).Select(line => JsonDocument.Parse(line[6..])).ToList();
        try
        {
            var text = string.Join("", frames.Where(f => f.RootElement.TryGetProperty("text", out _)).Select(f => f.RootElement.GetProperty("text").GetString()));
            var expected = rows.Where((_, mask) => (mask & 1) != 0).ToList();
            foreach (var tour in expected) Assert.Contains($"/Tour/Details/{tour.Id}", text);
            var sources = Assert.Single(frames, f => f.RootElement.TryGetProperty("tours", out _)).RootElement.GetProperty("tours");
            Assert.Equal(8, sources.GetArrayLength()); Assert.Contains("event: done", sse);
            var id = frames[0].RootElement.GetProperty("conversationId").GetInt32();
            var history = await client.GetStringAsync($"/AI/GetMessages?conversationId={id}");
            using var saved = JsonDocument.Parse(history);
            var content = saved.RootElement.GetProperty("messages")[1].GetProperty("content").GetString()!;
            foreach (var tour in expected) Assert.Contains($"/Tour/Details/{tour.Id}", content);
            using var request = JsonDocument.Parse(factory.Handler.LastRequest);
            var prompt = request.RootElement.GetProperty("messages").EnumerateArray().Last().GetProperty("content").GetString()!;
            using var evidence = JsonDocument.Parse(prompt[(prompt.IndexOf('{'))..(prompt.LastIndexOf('}') + 1)]);
            Assert.Equal(8, evidence.RootElement.GetProperty("TotalResults").GetInt32());
            Assert.True(evidence.RootElement.GetProperty("EvidenceIsSample").GetBoolean());
            Assert.Equal(6, evidence.RootElement.GetProperty("Tours").GetArrayLength() + evidence.RootElement.GetProperty("RelatedTours").GetArrayLength());
        }
        finally { foreach (var frame in frames) frame.Dispose(); }
    }
}
