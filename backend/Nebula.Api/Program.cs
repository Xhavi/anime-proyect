using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Nebula.Api;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<CatalogDb>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Catalog") ?? "Data Source=nebula.db"));
var origins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? ["http://localhost:4200"];
builder.Services.AddCors(options => options.AddPolicy("web", policy => policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod()));
builder.Services.AddProblemDetails();
builder.WebHost.UseUrls(builder.Configuration["ListenUrl"] ?? "http://localhost:5050");

var app = builder.Build();
app.UseExceptionHandler();
app.UseCors("web");

// The admin key is never embedded into source control or a browser bundle.
bool ValidAdmin(HttpContext ctx) {
    var expected = Environment.GetEnvironmentVariable("NEBULA_ADMIN_KEY");
    if (string.IsNullOrWhiteSpace(expected) || expected.Length < 32) return false;
    if (!ctx.Request.Headers.TryGetValue("X-Admin-Key", out var supplied)) return false;
    byte[] actualBytes = SHA256.HashData(Encoding.UTF8.GetBytes(supplied.ToString()));
    byte[] expectedBytes = SHA256.HashData(Encoding.UTF8.GetBytes(expected));
    return CryptographicOperations.FixedTimeEquals(actualBytes, expectedBytes);
}
IResult Authorize(HttpContext ctx) => ValidAdmin(ctx) ? Results.Ok() : Results.Unauthorized();
bool ValidVideo(string? value) {
    if (string.IsNullOrWhiteSpace(value)) return true;
    var hosts = (Environment.GetEnvironmentVariable("NEBULA_VIDEO_HOSTS") ?? "")
        .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    return Uri.TryCreate(value, UriKind.Absolute, out var uri)
        && uri.Scheme == Uri.UriSchemeHttps
        && hosts.Any(h => uri.Host.Equals(h, StringComparison.OrdinalIgnoreCase));
}
bool ValidCover(string? value) => string.IsNullOrWhiteSpace(value) ||
    (Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps);

app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }));

app.MapGet("/api/anime", async (CatalogDb db, string? q, string? genre, int? page, int? pageSize) => {
    int p = Math.Max(1, page ?? 1), size = Math.Clamp(pageSize ?? 20, 1, 50);
    var query = db.Animes.AsNoTracking().AsQueryable();
    if (!string.IsNullOrWhiteSpace(q)) {
        var term = q.Trim();
        query = query.Where(a => a.Title.Contains(term) || a.Genre.Contains(term) || a.Description.Contains(term));
    }
    if (!string.IsNullOrWhiteSpace(genre)) query = query.Where(a => a.Genre == genre);
    var total = await query.CountAsync();
    var data = await query.OrderBy(a => a.Title).Skip((p - 1) * size).Take(size)
        .Select(a => new { a.Id, a.Slug, a.Title, a.Description, a.Genre, a.Year, a.CoverUrl, EpisodeCount = a.Episodes.Count(e => e.IsPublished) }).ToListAsync();
    return Results.Ok(new { items = data, total, page = p, pageSize = size });
});
app.MapGet("/api/anime/{slug}", async (CatalogDb db, string slug) => {
    var a = await db.Animes.AsNoTracking().Where(a => a.Slug == slug)
        .Select(a => new { a.Id, a.Slug, a.Title, a.Description, a.Genre, a.Year, a.CoverUrl, EpisodeCount = a.Episodes.Count(e => e.IsPublished) }).FirstOrDefaultAsync();
    return a is null ? Results.NotFound() : Results.Ok(a);
});
app.MapGet("/api/anime/{slug}/episodes", async (CatalogDb db, string slug) => {
    var found = await db.Animes.AnyAsync(a => a.Slug == slug);
    if (!found) return Results.NotFound();
    var episodes = await db.Episodes.AsNoTracking().Where(e => e.Anime.Slug == slug && e.IsPublished)
        .OrderBy(e => e.Number).Select(e => new { e.Id, e.Number, e.Title, e.VideoUrl, e.IsPublished }).ToListAsync();
    return Results.Ok(episodes);
});
app.MapGet("/api/anime/{slug}/episodes/{number:int}/sources", async (CatalogDb db, string slug, int number) => {
    var episode = await db.Episodes.AsNoTracking().FirstOrDefaultAsync(e => e.Anime.Slug == slug && e.Number == number && e.IsPublished);
    if (episode is null) return Results.NotFound();
    var sources = await db.VideoSources.AsNoTracking()
        .Where(s => s.EpisodeId == episode.Id && s.Enabled && s.DistributionAuthorized)
        .OrderBy(s => s.Id)
        .Select(s => new { s.Id, s.Provider, s.Kind, s.Url, s.Quality })
        .ToListAsync();
    return Results.Ok(sources);
});
app.MapGet("/api/ads", async (CatalogDb db) => Results.Ok(await db.Ads.AsNoTracking()
    .Select(a => new { a.Id, a.Code, a.Enabled, a.Label, a.Network }).ToListAsync()));

// Restricted admin endpoints. This is server-to-server management, not a browser login.
var admin = app.MapGroup("/api/admin").AddEndpointFilter(async (context, next) => {
    if (!ValidAdmin(context.HttpContext)) return Results.Unauthorized();
    return await next(context);
});
admin.MapGet("/anime", async (CatalogDb db) => Results.Ok(await db.Animes.AsNoTracking().OrderBy(a => a.Id).ToListAsync()));
admin.MapPost("/anime", async (CatalogDb db, AnimeInput input) => {
    if (!ValidAnime(input)) return Results.BadRequest(new { error = "Datos de anime inválidos." });
    if (await db.Animes.AnyAsync(a => a.Slug == input.Slug)) return Results.Conflict(new { error = "Slug existente." });
    var a = new Anime { Slug=input.Slug, Title=input.Title.Trim(), Description=input.Description.Trim(), Genre=input.Genre.Trim(), Year=input.Year, CoverUrl=input.CoverUrl };
    db.Animes.Add(a); await db.SaveChangesAsync(); return Results.Created($"/api/anime/{a.Slug}", new { a.Id });
});
admin.MapPut("/anime/{id:int}", async (CatalogDb db, int id, AnimeInput input) => {
    if (!ValidAnime(input)) return Results.BadRequest(new { error = "Datos de anime inválidos." });
    var a = await db.Animes.FindAsync(id); if(a is null) return Results.NotFound();
    if(await db.Animes.AnyAsync(x => x.Slug == input.Slug && x.Id != id)) return Results.Conflict();
    a.Slug=input.Slug; a.Title=input.Title.Trim(); a.Description=input.Description.Trim(); a.Genre=input.Genre.Trim(); a.Year=input.Year; a.CoverUrl=input.CoverUrl;
    await db.SaveChangesAsync(); return Results.NoContent();
});
admin.MapDelete("/anime/{id:int}", async (CatalogDb db, int id) => {
    var a = await db.Animes.FindAsync(id); if (a is null) return Results.NotFound();
    db.Animes.Remove(a); await db.SaveChangesAsync(); return Results.NoContent();
});
admin.MapPost("/anime/{id:int}/episodes", async (CatalogDb db, int id, EpisodeInput input) => {
    if (!ValidEpisode(input) || !ValidVideo(input.VideoUrl)) return Results.BadRequest(new { error = "Número, título o dominio de video inválido/no autorizado." });
    if(!await db.Animes.AnyAsync(a => a.Id == id)) return Results.NotFound();
    if(await db.Episodes.AnyAsync(e => e.AnimeId == id && e.Number == input.Number)) return Results.Conflict();
    var e = new Episode { AnimeId=id, Number=input.Number, Title=input.Title.Trim(), VideoUrl=input.VideoUrl, IsPublished=input.IsPublished };
    db.Episodes.Add(e); await db.SaveChangesAsync(); return Results.Created($"/api/admin/episodes/{e.Id}",new { e.Id });
});
admin.MapPut("/episodes/{id:int}", async (CatalogDb db, int id, EpisodeInput input) => {
    if (!ValidEpisode(input) || !ValidVideo(input.VideoUrl)) return Results.BadRequest();
    var e=await db.Episodes.FindAsync(id); if(e is null) return Results.NotFound();
    if(await db.Episodes.AnyAsync(x=>x.AnimeId==e.AnimeId && x.Number==input.Number && x.Id!=id)) return Results.Conflict();
    e.Number=input.Number; e.Title=input.Title.Trim(); e.VideoUrl=input.VideoUrl; e.IsPublished=input.IsPublished;
    await db.SaveChangesAsync(); return Results.NoContent();
});
admin.MapDelete("/episodes/{id:int}", async (CatalogDb db, int id) => {
    var e=await db.Episodes.FindAsync(id); if(e is null) return Results.NotFound();
    db.Episodes.Remove(e); await db.SaveChangesAsync(); return Results.NoContent();
});
admin.MapPost("/episodes/{id:int}/sources", async (CatalogDb db, int id, VideoSourceInput input) => {
    if (!ValidSource(input)) return Results.BadRequest(new { error = "Fuente inválida o no autorizada. Revisa el host HTTPS permitido y la licencia." });
    if (!await db.Episodes.AnyAsync(e => e.Id == id)) return Results.NotFound();
    var source = new VideoSource {
        EpisodeId = id, Provider = input.Provider.Trim(), Kind = input.Kind,
        Url = input.Url, Quality = input.Quality.Trim(),
        DistributionAuthorized = true, Enabled = true
    };
    db.VideoSources.Add(source);
    await db.SaveChangesAsync();
    return Results.Created($"/api/admin/sources/{source.Id}", new { source.Id });
});
admin.MapDelete("/sources/{id:int}", async (CatalogDb db, int id) => {
    var source = await db.VideoSources.FindAsync(id);
    if (source is null) return Results.NotFound();
    db.VideoSources.Remove(source);
    await db.SaveChangesAsync();
    return Results.NoContent();
});
admin.MapPut("/ads/{code}", async (CatalogDb db, string code, AdInput input) => {
    if (code != input.Code || code.Length > 40 || !System.Text.RegularExpressions.Regex.IsMatch(code, @"^[a-z0-9_-]+$") ||
        input.Label.Length is < 1 or > 80 || input.Network.Length > 50) return Results.BadRequest();
    var ad=await db.Ads.FirstOrDefaultAsync(a=>a.Code==code);
    if(ad is null){ad=new AdPlacement { Code=code };db.Ads.Add(ad);}
    ad.Label=input.Label.Trim();ad.Network=input.Network.Trim();ad.Enabled=input.Enabled;
    await db.SaveChangesAsync();return Results.NoContent();
});

bool ValidSource(VideoSourceInput source) {
    if (!source.DistributionAuthorized || source.Provider is null || source.Quality is null ||
        source.Provider.Length is < 2 or > 50 || source.Quality.Length is < 1 or > 20 ||
        source.Kind is not ("embed" or "direct")) return false;
    if (!Uri.TryCreate(source.Url, UriKind.Absolute, out var uri) ||
        uri.Scheme != Uri.UriSchemeHttps || uri.UserInfo.Length > 0) return false;
    var allowed = (Environment.GetEnvironmentVariable("NEBULA_VIDEO_HOSTS") ?? "")
        .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    return allowed.Any(host => uri.Host.Equals(host, StringComparison.OrdinalIgnoreCase));
}
static bool ValidAnime(AnimeInput a) =>
    a.Slug.Length is >= 3 and <= 100 && System.Text.RegularExpressions.Regex.IsMatch(a.Slug, @"^[a-z0-9]+(?:-[a-z0-9]+)*$") &&
    a.Title.Length is >= 2 and <= 180 && a.Description.Length is >= 10 and <= 4000 &&
    a.Genre.Length is >= 2 and <= 80 && a.Year is >= 1900 and <= 2100 && ValidCoverStatic(a.CoverUrl);
static bool ValidCoverStatic(string? value) => string.IsNullOrWhiteSpace(value) ||
    (Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps);
static bool ValidEpisode(EpisodeInput e) => e.Number is >= 1 and <= 5000 && e.Title.Length is >= 2 and <= 180;

using (var scope = app.Services.CreateScope()) {
    var db = scope.ServiceProvider.GetRequiredService<CatalogDb>();
    await db.Database.EnsureCreatedAsync(); // MVP only; migrate to EF migrations before production.
    // Preserve existing SQLite databases created by the previous MVP schema.
    await db.Database.ExecuteSqlRawAsync("""
        CREATE TABLE IF NOT EXISTS "VideoSources" (
            "Id" INTEGER NOT NULL CONSTRAINT "PK_VideoSources" PRIMARY KEY AUTOINCREMENT,
            "EpisodeId" INTEGER NOT NULL,
            "Provider" TEXT NOT NULL,
            "Kind" TEXT NOT NULL,
            "Url" TEXT NOT NULL,
            "Quality" TEXT NOT NULL,
            "Enabled" INTEGER NOT NULL,
            "DistributionAuthorized" INTEGER NOT NULL,
            CONSTRAINT "FK_VideoSources_Episodes_EpisodeId" FOREIGN KEY ("EpisodeId")
                REFERENCES "Episodes" ("Id") ON DELETE CASCADE
        );
        """);
    await db.Database.ExecuteSqlRawAsync(
        """CREATE INDEX IF NOT EXISTS "IX_VideoSources_EpisodeId" ON "VideoSources" ("EpisodeId");""");
    if (!await db.Animes.AnyAsync()) {
        db.Animes.AddRange(
            new Anime {Slug="astral-odyssey",Title="Astral Odyssey",Description="Un grupo de viajeros descubre un universo oculto entre constelaciones.",Genre="Fantasía",Year=2026, Episodes=[new Episode {Number=1,Title="Primer encuentro",IsPublished=true}]},
            new Anime {Slug="neon-samurai",Title="Neon Samurai",Description="Una espadachina enfrenta corporaciones en una ciudad futurista.",Genre="Acción",Year=2026, Episodes=[new Episode {Number=1,Title="La ciudad nocturna",IsPublished=true}]},
            new Anime {Slug="moonlit-garden",Title="Moonlit Garden",Description="Dos estudiantes encuentran un jardín secreto bajo la luna.",Genre="Romance",Year=2025, Episodes=[new Episode {Number=1,Title="El jardín",IsPublished=true}]}
        );
    }
    if(!await db.Ads.AnyAsync()) db.Ads.AddRange(
        new AdPlacement {Code="home-banner",Label="Banner principal",Network="",Enabled=false},
        new AdPlacement {Code="episode-banner",Label="Banner de episodios",Network="",Enabled=false});
    await db.SaveChangesAsync();
}
app.Run();

// Enable WebApplicationFactory integration tests in the future.
public partial class Program { }
