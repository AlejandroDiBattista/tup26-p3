using Microsoft.EntityFrameworkCore;
using Agenda.Domain;

namespace Agenda.Data;

public class TicketContext : DbContext {
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<Accion> Acciones => Set<Accion>();

    public TicketContext(DbContextOptions<TicketContext> options)
        : base(options) {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.Entity<Ticket>(entity => {
            entity.HasOne(ticket => ticket.OriginadoPor)
                .WithMany(usuario => usuario.TicketsOriginados)
                .HasForeignKey(ticket => ticket.OriginadoPorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(ticket => ticket.Responsable)
                .WithMany(usuario => usuario.TicketsAsignados)
                .HasForeignKey(ticket => ticket.ResponsableId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Accion>(entity => {
            entity.HasOne(accion => accion.Ticket)
                .WithMany(ticket => ticket.Acciones)
                .HasForeignKey(accion => accion.TicketId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(accion => accion.RegistradaPor)
                .WithMany()
                .HasForeignKey(accion => accion.RegistradaPorId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}