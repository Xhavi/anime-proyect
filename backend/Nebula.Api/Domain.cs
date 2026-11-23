using Microsoft.EntityFrameworkCore;

namespace Nebula.Api;

public sealed class Anime {
    public int Id { get; set; }
    public string Slug { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Genre { get; set; } = "";
    public int Year { get; set; }
    public string? CoverUrl { get; set; }
    public List<Episode> Episodes { get; set; } = [];
}
public sealed class Episode {
    public int Id { get; set; }
    public int AnimeId { get; set; }
    public Anime Anime { get; set; } = null!;
    public int Number { get; set; }
    public string Title { get; set; } = "";
    public string? VideoUrl { get; set; }
    public bool IsPublished { get; set; }
}
public sealed class AdPlacement {
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Label { get; set; } = "";
    public string Network { get; set; } = "";
    public bool Enabled { get; set; }
}
public sealed class CatalogDb(DbContextOptions<CatalogDb> options) : DbContext(options) {
    public DbSet<Anime> Animes => Set<Anime>();
    public DbSet<Episode> Episodes => Set<Episode>();
    public DbSet<AdPlacement> Ads => Set<AdPlacement>();
    protected override void OnModelCreating(ModelBuilder model) {
        model.Entity<Anime>().HasIndex(a => a.Slug).IsUnique();
        model.Entity<Episode>().HasIndex(e => new { e.AnimeId, e.Number }).IsUnique();
        model.Entity<Episode>().HasOne(e => e.Anime).WithMany(a => a.Episodes).HasForeignKey(e => e.AnimeId).OnDelete(DeleteBehavior.Cascade);
        model.Entity<AdPlacement>().HasIndex(a => a.Code).IsUnique();
    }
}
public record AnimeInput(string Slug, string Title, string Description, string Genre, int Year, string? CoverUrl);
public record EpisodeInput(int Number, string Title, string? VideoUrl, bool IsPublished);
public record AdInput(string Code, string Label, string Network, bool Enabled);
