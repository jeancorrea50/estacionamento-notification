using EstacionamentoNotification.Domain.Entities;
using EstacionamentoNotification.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EstacionamentoNotification.Infrastructure.Persistence;

public sealed class NotificationDbContext : DbContext
{
    public NotificationDbContext(DbContextOptions<NotificationDbContext> options) : base(options)
    {
    }

    public DbSet<Notificacao> Notificacao => Set<Notificacao>();
    public DbSet<NotificacaoUsuario> NotificacaoUsuario => Set<NotificacaoUsuario>();
    public DbSet<NotificacaoEstacionamento> NotificacaoEstacionamento => Set<NotificacaoEstacionamento>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Notificacao>(b =>
        {
            b.ToTable("Notificacao", "dbo");
            b.HasKey(x => x.Id);
            b.Property(x => x.Tipo).HasMaxLength(80).IsRequired();
            b.Property(x => x.Titulo).HasMaxLength(200).IsRequired();
            b.Property(x => x.Mensagem).HasMaxLength(2000).IsRequired();
            b.HasMany(x => x.Usuarios).WithOne(x => x.Notificacao!).HasForeignKey(x => x.NotificacaoId);
            b.HasMany(x => x.Estacionamentos).WithOne(x => x.Notificacao!).HasForeignKey(x => x.NotificacaoId);
        });

        modelBuilder.Entity<NotificacaoUsuario>(b =>
        {
            b.ToTable("NotificacaoUsuario", "dbo");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.NotificacaoId, x.UsuarioId }).IsUnique();
        });

        modelBuilder.Entity<NotificacaoEstacionamento>(b =>
        {
            b.ToTable("NotificacaoEstacionamento", "dbo");
            b.HasKey(x => x.Id);
            b.Property(x => x.CodExportacao).HasMaxLength(36).IsRequired();
            b.HasIndex(x => new { x.NotificacaoId, x.CodExportacao }).IsUnique();
        });

        // Identity tables (somente leitura para roles)
        modelBuilder.Entity<IdentityRoleRow>(b =>
        {
            b.ToTable("Role", "dbo");
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).HasMaxLength(256);
        });

        modelBuilder.Entity<IdentityUserRoleRow>(b =>
        {
            b.ToTable("UserRole", "dbo");
            b.HasKey(x => new { x.UserId, x.RoleId });
        });
    }
}

public sealed class IdentityRoleRow
{
    public int Id { get; set; }
    public string? Name { get; set; }
}

public sealed class IdentityUserRoleRow
{
    public int UserId { get; set; }
    public int RoleId { get; set; }
}

public sealed class NotificacaoRepository : INotificacaoRepository
{
    private readonly NotificationDbContext _db;

    public NotificacaoRepository(NotificationDbContext db)
    {
        _db = db;
    }

    public async Task<Notificacao> AddAsync(Notificacao entity, CancellationToken cancellationToken = default)
    {
        entity.DataCriacao = DateTime.UtcNow;
        _db.Notificacao.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public Task<Notificacao?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        _db.Notificacao.AsNoTracking()
            .Include(x => x.Usuarios)
            .Include(x => x.Estacionamentos)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Notificacao>> ListByUsuarioAsync(
        int usuarioId,
        int take,
        CancellationToken cancellationToken = default)
    {
        return await _db.Notificacao.AsNoTracking()
            .Include(x => x.Usuarios)
            .Include(x => x.Estacionamentos)
            .Where(x => x.Usuarios.Any(u => u.UsuarioId == usuarioId))
            .OrderByDescending(x => x.DataCriacao)
            .Take(take <= 0 ? 50 : take)
            .ToListAsync(cancellationToken);
    }

    public async Task MarkReadAsync(long notificacaoId, int usuarioId, CancellationToken cancellationToken = default)
    {
        var row = await _db.NotificacaoUsuario
            .FirstOrDefaultAsync(x => x.NotificacaoId == notificacaoId && x.UsuarioId == usuarioId, cancellationToken);

        if (row is null)
            return;

        row.Lida = true;
        row.DataLeitura = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<int>> ListUsuarioIdsByRoleAsync(
        string roleName,
        CancellationToken cancellationToken = default)
    {
        var role = await _db.Set<IdentityRoleRow>().AsNoTracking()
            .FirstOrDefaultAsync(r => r.Name == roleName, cancellationToken);

        if (role is null)
            return Array.Empty<int>();

        return await _db.Set<IdentityUserRoleRow>().AsNoTracking()
            .Where(ur => ur.RoleId == role.Id)
            .Select(ur => ur.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }
}
