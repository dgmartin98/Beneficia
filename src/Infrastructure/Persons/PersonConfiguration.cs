using Domain.Persons;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persons;

/// <summary>
/// Configuración de Entity Framework Core para la entidad <see cref="Person"/>.
/// </summary>
public class PersonConfiguration : IEntityTypeConfiguration<Person>
{
    public void Configure(EntityTypeBuilder<Person> builder)
    {
        builder.ToTable("Persons");
        builder.HasKey(person => person.Id);

        builder.Property(person => person.Nombre)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(person => person.Apellido)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(person => person.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(person => person.Phone)
            .HasMaxLength(30);

        builder.Property(person => person.FechaNacimiento)
            .IsRequired();

        builder.Property(person => person.CreadoPor)
            .HasMaxLength(256);

        builder.Property(person => person.ModificadoPor)
            .HasMaxLength(256);

        builder.Property(person => person.EliminadoPor)
            .HasMaxLength(256);

        // Soft delete: solo se consultan registros que no tengan FechaEliminacion
        builder.HasQueryFilter(person => person.FechaEliminacion == null);
    }
}
