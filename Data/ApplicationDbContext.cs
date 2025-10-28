using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using HMS.Models;

namespace HMS.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }
        public DbSet<Patient> Patients { get; set; }
        public DbSet<Staff> Staff { get; set; }
        public DbSet<Schedule> Schedules { get; set; }
        public DbSet<TimeReport> TimeReports { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<InvoiceItem> InvoiceItems { get; set; }
        public DbSet<Transaction> Transactions { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            // ApplicationUser Configuration

            // PATIENT
            builder.Entity<Patient>(entity =>
            {
                entity.HasOne(e => e.User)
                    .WithOne(c => c.Patient)
                    .HasForeignKey<Patient>(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // STAFF
            builder.Entity<Staff>(entity =>
            {
                entity.HasOne(e => e.User)
                    .WithOne(c => c.Staff)
                    .HasForeignKey<Staff>(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // APPOINTMENT
            builder.Entity<Appointment>(entity =>
            {
                entity.HasOne(e => e.Patient)
                    .WithMany(c => c.Appointments)
                    .HasForeignKey(e => e.PatientId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Staff)
                    .WithMany(c => c.Appointments)
                    .HasForeignKey(e => e.StaffId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Schedule)
                    .WithOne(c => c.Appointment)
                    .HasForeignKey<Appointment>(e => e.ScheduleId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // SCHEDULE
            builder.Entity<Schedule>(entity =>
            {
                entity.HasOne(e => e.Staff)
                    .WithMany(c => c.Schedules)
                    .HasForeignKey(e => e.StaffId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<TimeReport>(entity =>
            {
                // One Staff -> many TimeReports (this can stay one-to-many)
                entity.HasOne(e => e.Staff)
                    .WithMany(c => c.TimeReports)
                    .HasForeignKey(e => e.StaffId)
                    .OnDelete(DeleteBehavior.Restrict);

                // One Schedule -> one TimeReport
                entity.HasOne(e => e.Schedule)
                    .WithOne(c => c.TimeReport)
                    .HasForeignKey<TimeReport>(e => e.ScheduleId)
                    .OnDelete(DeleteBehavior.Restrict); // prevent cascade loop
            });

            builder.Entity<Invoice>(entity =>
            {
                // One Patient -> many Invoices
                entity.HasOne(e => e.Patient)
                    .WithMany(c => c.Invoices)
                    .HasForeignKey(e => e.PatientId)
                    .OnDelete(DeleteBehavior.Restrict); // prevent multiple cascade paths

                // One Appointment -> one Invoice
                entity.HasOne(e => e.Appointment)
                    .WithOne(c => c.Invoice)
                    .HasForeignKey<Invoice>(e => e.AppointmentId)
                    .OnDelete(DeleteBehavior.Restrict); // safer, avoids loops
            });
        }
    }
}
