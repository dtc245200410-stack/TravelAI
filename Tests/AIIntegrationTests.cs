using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Reflection;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using TravelAI.Data;
using TravelAI.Models;
using TravelAI.Services;
using Xunit;

namespace TravelAI.Tests;

public sealed class AIFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    public FakeOllama Handler { get; } = new();
    public AIFactory() => _connection.Open();
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<TravelDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<TravelDbContext>>();
            services.AddDbContext<TravelDbContext>(o => o.UseSqlite(_connection));
            services.Configure<AIOptions>(o => { o.WarmupOnStartup = false; o.ResponseTimeoutSeconds = 5; o.GenerationTimeoutSeconds = 1; });
            services.AddHttpClient<OllamaService>().ConfigurePrimaryHttpMessageHandler(() => Handler);
            services.AddAuthentication(o => { o.DefaultAuthenticateScheme = "Test"; o.DefaultChallengeScheme = "Test"; o.DefaultForbidScheme = "Test"; })
                .AddScheme<AuthenticationSchemeOptions, TestAuth>("Test", _ => { });
            using var provider = services.BuildServiceProvider();
            provider.GetRequiredService<TravelDbContext>().Database.EnsureCreated();
        });
    }
    public HttpClient Client(bool admin = false)
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add("Test-User", admin ? "1" : "2");
        return client;
    }
    public async Task<string> Token(HttpClient client, string area = "AI")
    {
        var html = await client.GetStringAsync("/" + area);
        return Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value;
    }
    protected override void Dispose(bool disposing) { base.Dispose(disposing); if (disposing) _connection.Dispose(); }
}

public sealed class TestAuth(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("Test-User", out var id)) return Task.FromResult(AuthenticateResult.NoResult());
        var identity = new ClaimsIdentity([new(ClaimTypes.NameIdentifier, id.ToString()), new(ClaimTypes.Name, "Người kiểm thử"),
            new(ClaimTypes.Role, id == "1" ? "Admin" : "Customer")], "Test");
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), "Test")));
    }
}

public sealed class FakeOllama : HttpMessageHandler
{
    public bool Slow { get; set; }
    public bool Fail { get; set; }
    public bool Truncated { get; set; }
    public string LastRequest { get; private set; } = "";
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        if (request.RequestUri!.AbsolutePath == "/api/tags")
            return new(HttpStatusCode.OK) { Content = new StringContent("{\"models\":[{\"name\":\"qwen3.5:4b\"}]}") };
        LastRequest = await request.Content!.ReadAsStringAsync(ct);
        if (Slow) await Task.Delay(TimeSpan.FromSeconds(30), ct);
        if (Fail) return new(HttpStatusCode.InternalServerError);
        return new(HttpStatusCode.OK) { Content = new StringContent(
            "{\"message\":{\"content\":\"Bạn có thể so sánh thời gian khởi hành.\"},\"done\":false}\n" +
            (Truncated ? "" : "{\"message\":{\"content\":\" Hãy kiểm tra trước khi đặt.\"},\"done\":true}\n"), Encoding.UTF8, "application/x-ndjson") };
    }
}

public sealed class AIIntegrationTests
{
    private static FormUrlEncodedContent Form(params (string Key, string Value)[] items) => new(items.Select(x => new KeyValuePair<string, string>(x.Key, x.Value)));
    private static int ConversationId(string sse)
    {
        var line = sse.Split('\n').First(x => x.StartsWith("data: "))[6..];
        return JsonDocument.Parse(line).RootElement.GetProperty("conversationId").GetInt32();
    }
    private static string AllText(string sse) => string.Join("", sse.Split('\n').Where(x => x.StartsWith("data: ")).Select(x =>
    { using var d = JsonDocument.Parse(x[6..]); return d.RootElement.TryGetProperty("text", out var t) ? t.GetString() : ""; }));

    [Fact]
    public async Task ChatStreamsRealTourLinksAndPersistsHistoryWithSafeOllamaOptions()
    {
        using var f = new AIFactory(); using var c = f.Client(); var token = await f.Token(c);
        var response = await c.PostAsync("/AI/Chat", Form(("message", "Tìm tour Đà Nẵng 3 ngày dưới 5 triệu"), ("__RequestVerificationToken", token)));
        response.EnsureSuccessStatusCode(); var sse = await response.Content.ReadAsStringAsync(); var text = AllText(sse);
        Assert.Contains("event: delta", sse); Assert.Contains("event: done", sse); Assert.Contains("/Tour/Details/", text);
        Assert.Contains("3 ngày", text); Assert.Contains("Diễn giải AI", text);
        using var request = JsonDocument.Parse(f.Handler.LastRequest);
        Assert.Equal("qwen3.5:4b", request.RootElement.GetProperty("model").GetString());
        Assert.True(request.RootElement.GetProperty("stream").GetBoolean()); Assert.False(request.RootElement.GetProperty("think").GetBoolean());
        Assert.Equal("system", request.RootElement.GetProperty("messages")[0].GetProperty("role").GetString());
        var id = ConversationId(sse); var messages = await c.GetFromJsonAsync<JsonElement>($"/AI/GetMessages?conversationId={id}");
        Assert.Equal(2, messages.GetProperty("messages").GetArrayLength());
    }

    [Fact]
    public async Task VietnameseWithoutAccentsAndFollowupKeepDestinationAndBudget()
    {
        using var f = new AIFactory(); using var scope = f.Services.CreateScope(); var service = scope.ServiceProvider.GetRequiredService<TravelAIService>();
        var first = await service.PrepareAsync("tour da nang 3 ngay duoi 5 trieu", [], default);
        Assert.NotEmpty(first.Tours); Assert.All(first.Tours, t => { Assert.Contains("Nẵng", t.Destination); Assert.True(t.Price <= 5000000); });
        Assert.Equal(3, first.Tours[0].Duration);
        Assert.Contains("Phù hợp 2/3 yêu cầu", first.GroundedAnswer);
        var follow = await service.PrepareAsync("tour đó còn bao nhiêu chỗ?", [new("User", "tour da nang 3 ngay duoi 5 trieu"), new("Assistant", first.GroundedAnswer)], default);
        Assert.Single(follow.Tours); Assert.Equal(first.Tours[0].Id, follow.Tours[0].Id);
    }

    [Fact]
    public async Task UnknownDestinationDoesNotReturnUnrelatedToursAndUpcomingDaysAreNotTourDuration()
    {
        using var f = new AIFactory(); using var scope = f.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<TravelAIService>();
        var unknown = await service.PrepareAsync("tour Paris", [], default);
        Assert.Empty(unknown.Tours);
        var db = scope.ServiceProvider.GetRequiredService<TravelDbContext>();
        db.Tours.Add(new Tour { Name = "Tour khởi hành sớm", DestinationId = 1, Price = 500000, Duration = 2,
            MaxPeople = 10, AvailablePeople = 10, DepartureDate = DateTime.Today.AddDays(2) });
        await db.SaveChangesAsync();
        var soon = await service.PrepareAsync("tour khởi hành trong 7 ngày tới", [], default);
        Assert.Contains(soon.Tours, t => t.Name == "Tour khởi hành sớm" && t.Duration == 2);
    }

    [Fact]
    public async Task GroupTotalBudgetAndCapacityFilterAreApplied()
    {
        using var f = new AIFactory(); using var scope = f.Services.CreateScope(); var service = scope.ServiceProvider.GetRequiredService<TravelAIService>();
        var answer = await service.PrepareAsync("Đà Nẵng 3 ngày cho 2 người tổng ngân sách 10 triệu", [], default);
        Assert.NotEmpty(answer.Tours); Assert.All(answer.Tours, t => Assert.True(t.Price * 2 <= 10000000 && t.AvailablePeople >= 2));
        var noSeats = await service.PrepareAsync("tour cho 9999 người", [], default); Assert.Empty(noSeats.Tours);
    }

    [Fact]
    public async Task NamedTourIsFoundAndExpiredCatalogueToursRemainVisibleWithWarning()
    {
        using var f = new AIFactory(); using var scope = f.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<TravelDbContext>();
        var named = new Tour { Name = "Tour đặc biệt Quy Nhơn", DestinationId = 1, Duration = 4, Price = 4000000,
            AvailablePeople = 5, MaxPeople = 10, DepartureDate = DateTime.Today.AddDays(12), Rating = 0 };
        db.Tours.Add(named); await db.SaveChangesAsync();
        var service = scope.ServiceProvider.GetRequiredService<TravelAIService>();
        var result = await service.PrepareAsync("Cho tôi thông tin tour đặc biệt Quy Nhơn", [], default);
        Assert.Single(result.Tours); Assert.Equal(named.Id, result.Tours[0].Id);
        named.DepartureDate = DateTime.Today.AddDays(-1); await db.SaveChangesAsync();
        var recommended = await service.PrepareAsync("Tư vấn tour", [], default);
        Assert.Contains(recommended.Tours, t => t.Id == named.Id);
        Assert.Contains("qua ngày khởi hành", recommended.GroundedAnswer);
    }

    [Fact]
    public async Task ExistingExpiredToursAreExplainedAndLinkedInsteadOfReportedAsMissing()
    {
        using var f = new AIFactory(); using var scope = f.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TravelDbContext>();
        var destination = await db.Destinations.SingleAsync(x => x.Name == "Đà Nẵng");
        var tours = await db.Tours.Where(x => x.DestinationId == destination.Id).ToListAsync();
        foreach (var t in tours) t.DepartureDate = DateTime.Today.AddDays(-7);
        await db.SaveChangesAsync();
        var response = await scope.ServiceProvider.GetRequiredService<TravelAIService>()
            .PrepareAsync("Tìm tour Đà Nẵng 3 ngày dưới 5 triệu mỗi người", [], default);
        Assert.NotEmpty(response.Tours); // Catalogue relevance and current booking availability are separate.
        Assert.Contains("Có tour trong danh mục", response.GroundedAnswer);
        Assert.Contains("qua ngày khởi hành", response.GroundedAnswer);
        Assert.Contains("/Tour/Details/", response.GroundedAnswer);
        Assert.DoesNotContain("Chưa tìm thấy tour phù hợp trong hệ thống", response.GroundedAnswer);
        using var evidence = JsonDocument.Parse(response.Evidence);
        Assert.True(evidence.RootElement.GetProperty("Tours").GetArrayLength() > 0);
        Assert.All(evidence.RootElement.GetProperty("Tours").EnumerateArray(), t => Assert.False(t.GetProperty("AvailableForBooking").GetBoolean()));
        Assert.True(evidence.RootElement.GetProperty("SearchDiagnostics").GetProperty("ExpiredDepartures").GetInt32() > 0);
    }

    [Theory]
    [InlineData("capacity", "không đủ chỗ")]
    [InlineData("budget", "vượt ngân sách")]
    [InlineData("duration", "không đúng 3 ngày")]
    [InlineData("date", "không đúng ngày khởi hành yêu cầu")]
    [InlineData("missing-date", "chưa cập nhật ngày khởi hành")]
    public async Task NoExactMatchShowsActualReasonAndRealAlternativeLink(string mismatch, string reason)
    {
        using var f = new AIFactory(); using var scope = f.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TravelDbContext>();
        var destination = new Destination { Name = "Nơi kiểm thử", Slug = "noi-kiem-thu" };
        db.Destinations.Add(destination); await db.SaveChangesAsync();
        var tour = new Tour { Name = "Hành trình kiểm thử", DestinationId = destination.Id,
            Price = 4000000, Duration = 3, MaxPeople = 10, AvailablePeople = 5,
            DepartureDate = DateTime.Today.AddDays(10) };
        if (mismatch == "capacity") tour.AvailablePeople = 0;
        if (mismatch == "budget") tour.Price = 6000000;
        if (mismatch == "duration") tour.Duration = 4;
        if (mismatch == "missing-date") tour.DepartureDate = default;
        db.Tours.Add(tour); await db.SaveChangesAsync();
        var question = "Tìm tour Nơi kiểm thử 3 ngày dưới 5 triệu cho 2 người";
        if (mismatch == "date") question += " khởi hành " + DateTime.Today.AddDays(11).ToString("dd/MM/yyyy");
        var result = await scope.ServiceProvider.GetRequiredService<TravelAIService>().PrepareAsync(question, [], default);
        Assert.Single(result.Tours);
        Assert.Contains(reason, result.GroundedAnswer);
        Assert.Contains($"/Tour/Details/{tour.Id}", result.GroundedAnswer);
        using var evidence = JsonDocument.Parse(result.Evidence);
        var related = Assert.Single(evidence.RootElement.GetProperty("RelatedTours").EnumerateArray()
            .Concat(evidence.RootElement.GetProperty("Tours").EnumerateArray()));
        Assert.Equal(mismatch == "missing-date", related.GetProperty("MatchesAllRequirements").GetBoolean());
        Assert.Contains(reason, string.Join(" ", related.GetProperty("Reasons").EnumerateArray()
            .Concat(related.GetProperty("AvailabilityNotes").EnumerateArray()).Select(x => x.GetString())));
    }

    [Fact]
    public async Task EmptyDestinationCatalogueDoesNotSuggestUnrelatedOrInactiveTours()
    {
        using var f = new AIFactory(); using var scope = f.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TravelDbContext>();
        var destination = new Destination { Name = "Điểm chưa mở bán", Slug = "diem-chua-mo-ban" };
        db.Destinations.Add(destination); await db.SaveChangesAsync();
        db.Tours.Add(new Tour { Name = "Tour ẩn tại điểm chưa mở bán", DestinationId = destination.Id,
            Status = "Inactive", Price = 3000000, Duration = 3, MaxPeople = 10, AvailablePeople = 10,
            DepartureDate = DateTime.Today.AddDays(10) });
        await db.SaveChangesAsync();
        var result = await scope.ServiceProvider.GetRequiredService<TravelAIService>()
            .PrepareAsync("Tìm tour Điểm chưa mở bán", [], default);
        Assert.Empty(result.Tours);
        Assert.DoesNotContain("/Tour/Details/", result.GroundedAnswer);
        Assert.Contains("đang công khai", result.GroundedAnswer);
        using var evidence = JsonDocument.Parse(result.Evidence);
        Assert.Equal(0, evidence.RootElement.GetProperty("RelatedTours").GetArrayLength());
    }

    [Fact]
    public async Task CompleteResultsAreUnboundedModelEvidenceIsBoundedAndFollowupReferencesFirstTour()
    {
        using var f = new AIFactory(); using var scope = f.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TravelDbContext>();
        var tours = await db.Tours.Where(x => x.Destination!.Name == "Đà Nẵng").ToListAsync();
        foreach (var tour in tours) tour.DepartureDate = DateTime.Today.AddDays(-2);
        db.Tours.AddRange(Enumerable.Range(1, 9).Select(i => new Tour { Name = "Hành trình bổ sung " + i,
            DestinationId = tours[0].DestinationId, Price = 4500000, Duration = 3, MaxPeople = 10,
            AvailablePeople = 10, DepartureDate = DateTime.Today.AddDays(-2) }));
        await db.SaveChangesAsync();
        var service = scope.ServiceProvider.GetRequiredService<TravelAIService>();
        var first = await service.PrepareAsync("Tour Đà Nẵng 3 ngày dưới 5 triệu", [], default);
        using var evidence = JsonDocument.Parse(first.Evidence);
        Assert.Equal(tours.Count + 9, first.Tours.Count);
        Assert.Equal(first.Tours.Count, evidence.RootElement.GetProperty("TotalResults").GetInt32());
        Assert.Equal(6, evidence.RootElement.GetProperty("EvidenceTourCount").GetInt32());
        var related = evidence.RootElement.GetProperty("Tours");
        Assert.Equal(6, related.GetArrayLength());
        var id = related[0].GetProperty("Id").GetInt32();
        var followup = await service.PrepareAsync("Cho tôi thông tin tour đó",
            [new("User", "Tour Đà Nẵng 3 ngày dưới 5 triệu"), new("Assistant", first.GroundedAnswer)], default);
        Assert.Equal(id, Assert.Single(followup.Tours).Id);
        Assert.Contains("qua ngày khởi hành", followup.GroundedAnswer);
    }

    [Fact]
    public async Task ChatStreamsAndSavesCatalogueAlternativesWithMismatchEvidenceForOllama()
    {
        using var f = new AIFactory(); using var c = f.Client(); var token = await f.Token(c);
        using var scope = f.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<TravelDbContext>();
        foreach (var tour in await db.Tours.Where(x => x.Destination!.Name == "Đà Nẵng").ToListAsync())
            tour.DepartureDate = DateTime.Today.AddDays(-2);
        await db.SaveChangesAsync();
        var response = await c.PostAsync("/AI/Chat", Form(("message", "Tìm tour Đà Nẵng 3 ngày dưới 5 triệu mỗi người"), ("__RequestVerificationToken", token)));
        response.EnsureSuccessStatusCode(); var sse = await response.Content.ReadAsStringAsync();
        Assert.Contains("Có tour trong danh mục", AllText(sse)); Assert.Contains("/Tour/Details/", AllText(sse));
        Assert.Contains("qua ngày khởi hành", AllText(sse)); Assert.Contains("event: done", sse);
        using var request = JsonDocument.Parse(f.Handler.LastRequest);
        var system = request.RootElement.GetProperty("messages")[0].GetProperty("content").GetString();
        Assert.Contains("Số khách không bắt buộc", system);
        var userContent = request.RootElement.GetProperty("messages").EnumerateArray().Last().GetProperty("content").GetString();
        Assert.Contains("RelatedTours", userContent); Assert.Contains("MatchesAllRequirements", userContent);
        var messages = await c.GetFromJsonAsync<JsonElement>($"/AI/GetMessages?conversationId={ConversationId(sse)}");
        Assert.Contains("Có tour trong danh mục", messages.GetProperty("messages")[1].GetProperty("content").GetString());
    }

    [Fact]
    public void ProductionCatalogueQueryCompilesForSqlServerWithoutOptionalFilterAggregates()
    {
        using var db = new TravelDbContext(new DbContextOptionsBuilder<TravelDbContext>()
            .UseSqlServer("Server=unused;Database=TravelAIQueryCheck;Trusted_Connection=True").Options);
        var method = typeof(TravelAIService).GetMethod("BuildCatalogueQuery", BindingFlags.Static | BindingFlags.NonPublic)!;
        var query = Assert.IsAssignableFrom<IQueryable>(method.Invoke(null, [db]));
        var sql = query.ToQueryString();
        Assert.Contains("[Tours]", sql); Assert.Contains("[Destinations]", sql); Assert.Contains("N'Active'", sql);
        Assert.DoesNotContain("COUNT(NULL)", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("79228162514264337593543950335", sql);
    }

    [Theory]
    [InlineData("Tư vấn tour cho 2 người, tổng ngân sách 10 triệu")]
    [InlineData("Tư vấn tour cho 2 người")]
    public async Task GenericGroupQuestionsWithoutDaysStillReturnCatalogueWhenAllDeparturesExpired(string question)
    {
        using var f = new AIFactory(); using var c = f.Client(); var token = await f.Token(c);
        using var scope = f.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<TravelDbContext>();
        foreach (var tour in await db.Tours.ToListAsync()) tour.DepartureDate = DateTime.Today.AddDays(-2);
        await db.SaveChangesAsync();
        var response = await c.PostAsync("/AI/Chat", Form(("message", question), ("__RequestVerificationToken", token)));
        response.EnsureSuccessStatusCode(); var sse = await response.Content.ReadAsStringAsync(); var text = AllText(sse);
        Assert.Contains("Có tour trong danh mục", text); Assert.Contains("/Tour/Details/", text);
        Assert.Contains("qua ngày khởi hành", text); Assert.Contains("event: done", sse);
        Assert.DoesNotContain("Chưa hoàn tất tra cứu dữ liệu", text);
        using var request = JsonDocument.Parse(f.Handler.LastRequest);
        var content = request.RootElement.GetProperty("messages").EnumerateArray().Last().GetProperty("content").GetString()!;
        var jsonStart = content.IndexOf('{'); var jsonEnd = content.LastIndexOf('}');
        using var evidence = JsonDocument.Parse(content[jsonStart..(jsonEnd + 1)]);
        Assert.Equal(0, evidence.RootElement.GetProperty("SearchDiagnostics").GetProperty("WrongDuration").GetInt32());
        Assert.Equal(0, evidence.RootElement.GetProperty("SearchDiagnostics").GetProperty("OutsideRequestedDates").GetInt32());
        if (!question.Contains("ngân sách"))
            Assert.Equal(0, evidence.RootElement.GetProperty("SearchDiagnostics").GetProperty("OverBudget").GetInt32());
    }

    [Fact]
    public async Task CustomerCannotReadOtherUsersOrAdminScopeAndInvalidIdDoesNotCreateConversation()
    {
        using var f = new AIFactory(); using var admin = f.Client(true); using var c = f.Client();
        var token = await f.Token(admin, "AdminAI");
        var sse = await (await admin.PostAsync("/AdminAI/Chat", Form(("message", "Tổng quan"), ("__RequestVerificationToken", token)))).Content.ReadAsStringAsync();
        var id = ConversationId(sse);
        Assert.Equal(HttpStatusCode.NotFound, (await c.GetAsync($"/AI/GetMessages?conversationId={id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"/AI/GetMessages?conversationId={id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await c.GetAsync("/AdminAI")).StatusCode);
        var customerToken = await f.Token(c);
        Assert.Equal(HttpStatusCode.NotFound, (await c.PostAsync("/AI/Chat", Form(("message", "tour"), ("conversationId", "999999"), ("__RequestVerificationToken", customerToken)))).StatusCode);
    }

    [Fact]
    public async Task ChatRequiresAntiforgeryAndRejectsOversizedMessages()
    {
        using var f = new AIFactory(); using var c = f.Client();
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsync("/AI/Chat", Form(("message", "tour")))).StatusCode);
        var token = await f.Token(c);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsync("/AI/Chat", Form(("message", new string('a', 2001)), ("__RequestVerificationToken", token)))).StatusCode);
    }

    [Theory]
    [InlineData(false, "")]
    [InlineData(true, "")]
    [InlineData(false, "   ")]
    [InlineData(true, "   ")]
    [InlineData(false, " active ")]
    [InlineData(true, " active ")]
    public async Task LegacyAndNormalizedActiveAccountsCanChat(bool admin, string status)
    {
        using var f = new AIFactory(); using var c = f.Client(admin);
        using var scope = f.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TravelDbContext>();
        var user = await db.Users.SingleAsync(x => x.Id == (admin ? 1 : 2));
        user.Status = status; await db.SaveChangesAsync();
        var area = admin ? "AdminAI" : "AI";
        var token = await f.Token(c, area);
        var response = await c.PostAsync($"/{area}/Chat", Form(("message", admin ? "Báo cáo tổng quan hệ thống tháng này" : "Tìm tour Đà Nẵng 3 ngày dưới 5 triệu mỗi người"), ("__RequestVerificationToken", token)));
        response.EnsureSuccessStatusCode();
        var sse = await response.Content.ReadAsStringAsync();
        Assert.Contains("event: done", sse);
        Assert.Contains(admin ? "Báo cáo" : "/Tour/Details/", AllText(sse));
        await db.Entry(user).ReloadAsync();
        Assert.Equal(status, user.Status); // Compatibility must not silently unlock or rewrite accounts.
    }

    [Theory]
    [InlineData(false, "Locked", "account_locked")]
    [InlineData(true, "Locked", "account_locked")]
    [InlineData(false, " locked ", "account_locked")]
    [InlineData(true, " locked ", "account_locked")]
    [InlineData(false, "Inactive", "account_inactive")]
    [InlineData(true, "Inactive", "account_inactive")]
    public async Task LockedAndInactiveAccountsAreRejectedBeforeSavingOrCallingModel(bool admin, string status, string code)
    {
        using var f = new AIFactory(); using var c = f.Client(admin);
        var area = admin ? "AdminAI" : "AI"; var token = await f.Token(c, area);
        using var scope = f.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<TravelDbContext>();
        var user = await db.Users.SingleAsync(x => x.Id == (admin ? 1 : 2));
        user.Status = status; await db.SaveChangesAsync();
        var response = await c.PostAsync($"/{area}/Chat", Form(("message", "Tổng quan"), ("__RequestVerificationToken", token)));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(code, error.GetProperty("code").GetString());
        Assert.False(await db.AIConversations.AnyAsync());
        Assert.Empty(f.Handler.LastRequest);
        Assert.False(scope.ServiceProvider.GetRequiredService<AIRequestGate>().IsUserBusy(user.Id));
    }

    [Fact]
    public async Task StaleAdminClaimCannotAccessReportAfterDatabaseRoleChanges()
    {
        using var f = new AIFactory(); using var c = f.Client(true); var token = await f.Token(c, "AdminAI");
        using var scope = f.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<TravelDbContext>();
        var user = await db.Users.SingleAsync(x => x.Id == 1); user.Role = "Customer"; await db.SaveChangesAsync();
        var response = await c.PostAsync("/AdminAI/Chat", Form(("message", "Báo cáo tổng quan"), ("__RequestVerificationToken", token)));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("admin_required", error.GetProperty("code").GetString());
        Assert.False(await db.AIConversations.AnyAsync()); Assert.Empty(f.Handler.LastRequest);
    }

    [Fact]
    public async Task SessionForMissingAccountRequiresLoginAgain()
    {
        using var f = new AIFactory(); using var c = f.Client();
        c.DefaultRequestHeaders.Remove("Test-User"); c.DefaultRequestHeaders.Add("Test-User", "999999");
        var token = await f.Token(c);
        var response = await c.PostAsync("/AI/Chat", Form(("message", "tour"), ("__RequestVerificationToken", token)));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("invalid_session", error.GetProperty("code").GetString());
        Assert.Empty(f.Handler.LastRequest);
    }

    [Theory]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(false, false, true)]
    public async Task TimeoutFailureAndTruncatedStreamKeepGroundedAnswer(bool slow, bool fail, bool truncated)
    {
        using var f = new AIFactory(); f.Handler.Slow = slow; f.Handler.Fail = fail; f.Handler.Truncated = truncated;
        using var c = f.Client(); var token = await f.Token(c); var start = DateTime.UtcNow;
        var response = await c.PostAsync("/AI/Chat", Form(("message", "tour Đà Nẵng"), ("__RequestVerificationToken", token)));
        var sse = await response.Content.ReadAsStringAsync(); var text = AllText(sse);
        Assert.Contains("/Tour/Details/", text); Assert.Contains("chưa hoàn tất", text); Assert.Contains("event: done", sse);
        Assert.True((DateTime.UtcNow - start).TotalSeconds < 5);
    }

    [Fact]
    public async Task RenameRegenerateAndDeleteMaintainSingleUserQuestion()
    {
        using var f = new AIFactory(); using var c = f.Client(); var token = await f.Token(c);
        var sse = await (await c.PostAsync("/AI/Chat", Form(("message", "tour Đà Nẵng"), ("__RequestVerificationToken", token)))).Content.ReadAsStringAsync();
        var id = ConversationId(sse);
        (await c.PostAsync("/AI/Rename", Form(("conversationId", id.ToString()), ("title", "Chuyến hè"), ("__RequestVerificationToken", token)))).EnsureSuccessStatusCode();
        (await c.PostAsync("/AI/Chat", Form(("conversationId", id.ToString()), ("message", "ignored"), ("regenerate", "true"), ("__RequestVerificationToken", token)))).EnsureSuccessStatusCode();
        var result = await c.GetFromJsonAsync<JsonElement>($"/AI/GetMessages?conversationId={id}");
        Assert.Equal(2, result.GetProperty("messages").GetArrayLength()); Assert.Equal("Chuyến hè", result.GetProperty("title").GetString());
        (await c.PostAsync("/AI/Delete", Form(("conversationId", id.ToString()), ("__RequestVerificationToken", token)))).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.NotFound, (await c.GetAsync($"/AI/GetMessages?conversationId={id}")).StatusCode);
    }

    [Fact]
    public async Task AdminReportCoversAllEntitiesAndExcludesConfirmedUnpaidFromRevenue()
    {
        using var f = new AIFactory(); using var scope = f.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<TravelDbContext>();
        db.Bookings.AddRange(new Booking { UserId = 2, TourId = 1, BookingCode = "UNPAID", Status = "Confirmed", PaymentStatus = "Unpaid", NumberOfPeople = 1, TotalPrice = 9000000 },
            new Booking { UserId = 2, TourId = 1, BookingCode = "PAID", Status = "Confirmed", PaymentStatus = "Paid", NumberOfPeople = 1, TotalPrice = 1234567 },
            new Booking { UserId = 2, TourId = 1, BookingCode = "CANCELLED", Status = "Cancelled", PaymentStatus = "Paid", NumberOfPeople = 1, TotalPrice = 8000000 });
        await db.SaveChangesAsync(); var report = await scope.ServiceProvider.GetRequiredService<AdminAITools>().GetReportAsync("Tổng quan", [], default);
        using var doc = JsonDocument.Parse(report.Evidence);
        Assert.Equal(1234567m, doc.RootElement.GetProperty("RevenueFromPaidNonCancelledBookings").GetDecimal());
        foreach (var field in new[] { "Users", "Tours", "Hotels", "Rooms", "TourBookings", "HotelBookings", "Payments", "Reviews", "Promotions", "Combos", "Favorites", "Conversations", "MessageCount" })
            Assert.True(doc.RootElement.TryGetProperty(field, out _), field);
        Assert.DoesNotContain("PasswordHash", report.Evidence); Assert.Contains("/AdminPayment", report.GroundedAnswer);
    }

    [Fact]
    public async Task ConcurrentSendAndMutationAreRejectedUntilCurrentTurnEnds()
    {
        using var f = new AIFactory(); f.Handler.Slow = true;
        using var c = f.Client(); var token = await f.Token(c);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/AI/Chat")
        { Content = Form(("message", "tour Đà Nẵng"), ("__RequestVerificationToken", token)) };
        using var response = await c.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        using var reader = new StreamReader(await response.Content.ReadAsStreamAsync());
        Assert.Equal("event: meta", await reader.ReadLineAsync());
        var metadata = await reader.ReadLineAsync();
        var id = JsonDocument.Parse(metadata![6..]).RootElement.GetProperty("conversationId").GetInt32();
        Assert.Equal(HttpStatusCode.Conflict, (await c.PostAsync("/AI/Chat", Form(("message", "tour"), ("__RequestVerificationToken", token)))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await c.PostAsync("/AI/Delete", Form(("conversationId", id.ToString()), ("__RequestVerificationToken", token)))).StatusCode);
        await reader.ReadToEndAsync();
        using var scope = f.Services.CreateScope();
        Assert.False(scope.ServiceProvider.GetRequiredService<AIRequestGate>().IsUserBusy(2));
    }

    [Fact]
    public async Task StopPreservesPartialDataAndReleasesUserAndModelLocks()
    {
        using var f = new AIFactory(); f.Handler.Slow = true;
        using var c = f.Client(); var token = await f.Token(c);
        using var abort = new CancellationTokenSource();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/AI/Chat")
        { Content = Form(("message", "tour Đà Nẵng"), ("__RequestVerificationToken", token)) };
        using var response = await c.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, abort.Token);
        using var reader = new StreamReader(await response.Content.ReadAsStreamAsync());
        int id = 0;
        while (await reader.ReadLineAsync() is { } line)
        {
            if (line.StartsWith("data: ") && id == 0) id = JsonDocument.Parse(line[6..]).RootElement.GetProperty("conversationId").GetInt32();
            if (line.StartsWith("event: delta")) { await reader.ReadLineAsync(); break; }
        }
        abort.Cancel();
        using var scope = f.Services.CreateScope(); var gate = scope.ServiceProvider.GetRequiredService<AIRequestGate>();
        for (var i = 0; i < 40 && gate.IsUserBusy(2); i++) await Task.Delay(50);
        Assert.False(gate.IsUserBusy(2)); Assert.True(gate.TryEnterModel()); gate.ExitModel();
        var db = scope.ServiceProvider.GetRequiredService<TravelDbContext>();
        var partial = await db.AIChatMessages.Where(x => x.ConversationId == id && x.Role == "Assistant").SingleAsync();
        Assert.Contains("/Tour/Details/", partial.Content);
    }

    [Fact]
    public void GeneratedMigrationMatchesCurrentModel()
    {
        var configuration = new DbContextOptionsBuilder<TravelDbContext>().UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=TravelAIDB;Trusted_Connection=True;").Options;
        using var db = new TravelDbContext(configuration);
        Assert.False(db.Database.HasPendingModelChanges());
        var script = db.Database.GenerateCreateScript();
        Assert.Contains("[Scope] nvarchar(20)", script); Assert.Contains("DEFAULT N'Travel'", script);
    }

    [Fact]
    public async Task RenderBothActualRazorPagesForBrowserVerification()
    {
        using var f = new AIFactory(); using var c = f.Client(); using var a = f.Client(true);
        var preview = Environment.GetEnvironmentVariable("TRAVELAI_PREVIEW_DIR");
        if (preview != null) { Directory.CreateDirectory(preview); await File.WriteAllTextAsync(Path.Combine(preview, "customer.html"), await c.GetStringAsync("/AI"));
            await File.WriteAllTextAsync(Path.Combine(preview, "admin.html"), await a.GetStringAsync("/AdminAI")); }
        Assert.Contains("TravelAI", await c.GetStringAsync("/AI"));
    }

    [Theory]
    [InlineData("5 triệu", 5000000)] [InlineData("2,5 tr", 2500000)] [InlineData("500k", 500000)] [InlineData("5.000.000 vnd", 5000000)]
    public void BudgetFormatsAreParsed(string input, int expected) => Assert.Equal((decimal)expected, TravelAIService.ParseBudget(input));

    [Fact]
    public void PeriodParsingUsesInclusiveLastDay()
    {
        var p = AdminAITools.ResolvePeriod(AIText.Normalize("từ 01/10/2026 đến 11/10/2026"));
        Assert.Equal(new DateTime(2026, 10, 1), p.Start); Assert.Equal(new DateTime(2026, 10, 12), p.End);
        Assert.NotNull(AdminAITools.ResolvePeriod("30 ngay gan day").Start);
    }
}
