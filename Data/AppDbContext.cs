using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Sanalink.API.Models;

namespace Sanalink.API.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Patient> Patients { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<Note> Notes { get; set; }
        public DbSet<Prescription> Prescriptions { get; set; }
        public DbSet<Encounter> Encounters { get; set; }
        public DbSet<Facility> Facilities { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<LabOrder> LabOrders { get; set; }
        public DbSet<PharmacyDispense> PharmacyDispenses { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // AuditLog -> ApplicationUser (optional FK)
            builder.Entity<AuditLog>()
                .HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // ApplicationUser -> Facility (optional FK)
            builder.Entity<ApplicationUser>()
                .HasOne(u => u.Facility)
                .WithMany()
                .HasForeignKey(u => u.FacilityId)
                .OnDelete(DeleteBehavior.Restrict);

            // Encounter: two FKs to ApplicationUser (Doctor, Nurse)
            builder.Entity<Encounter>()
                .HasOne(e => e.Doctor)
                .WithMany()
                .HasForeignKey(e => e.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Encounter>()
                .HasOne(e => e.Nurse)
                .WithMany()
                .HasForeignKey(e => e.NurseId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Encounter>()
                .HasIndex(e => e.EncounterNumber)
                .IsUnique();

            // LabOrder FKs
            builder.Entity<LabOrder>()
                .HasOne(l => l.Encounter)
                .WithMany()
                .HasForeignKey(l => l.EncounterId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<LabOrder>()
                .HasOne(l => l.Patient)
                .WithMany()
                .HasForeignKey(l => l.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<LabOrder>()
                .HasOne(l => l.Doctor)
                .WithMany()
                .HasForeignKey(l => l.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            // PharmacyDispense FKs
            builder.Entity<PharmacyDispense>()
                .HasOne(d => d.Prescription)
                .WithMany()
                .HasForeignKey(d => d.PrescriptionId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<PharmacyDispense>()
                .HasOne(d => d.Patient)
                .WithMany()
                .HasForeignKey(d => d.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<PharmacyDispense>()
                .HasOne(d => d.DispensedBy)
                .WithMany()
                .HasForeignKey(d => d.DispensedById)
                .OnDelete(DeleteBehavior.Restrict);

            // Patient -> ApplicationUser (optional FK for self-registered patients)
            builder.Entity<Patient>()
                .HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Patient>()
                .HasIndex(p => p.UserId)
                .IsUnique()
                .HasFilter("[UserId] IS NOT NULL");

            // STEP: Apply UTC DateTime converter globally
            var dateTimeConverter = new ValueConverter<DateTime, DateTime>(
            v => v.ToUniversalTime(),
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

            var nullableDateTimeConverter = new ValueConverter<DateTime?, DateTime?>(
                v => v.HasValue ? v.Value.ToUniversalTime() : v,
                v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v
            );

            foreach (var entityType in builder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(DateTime))
                    {
                        property.SetValueConverter(dateTimeConverter);
                    }
                    else if (property.ClrType == typeof(DateTime?))
                    {
                        property.SetValueConverter(nullableDateTimeConverter);
                    }
                }
            }
        }
    }
}