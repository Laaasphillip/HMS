using HMS.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;

namespace HMS.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
    {


        public DbSet<Patient> Patients { get; set; }
        public DbSet<Staff> Staff { get; set; }
        public DbSet<Schedule> Schedules { get; set; }
        public DbSet<TimeReport> TimeReports { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<InvoiceItem> InvoiceItems { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        //public DbSet<Leave> Leaves { get; set; }

        // New appointment booking system models
        public DbSet<AppointmentSlot> AppointmentSlots { get; set; }
        public DbSet<AppointmentSlotConfiguration> AppointmentSlotConfigurations { get; set; }
        public DbSet<AppointmentBlock> AppointmentBlocks { get; set; }

        [Obsolete]
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // PATIENT
            builder.Entity<Patient>(entity =>
            {
                entity.Property(e => e.UserId).IsRequired();
                entity.Property(e => e.BloodGroup).IsRequired().HasMaxLength(10);
                entity.Property(e => e.Dateofbirth).IsRequired();

                entity.Property(e => e.Address).IsRequired(false);
                entity.Property(e => e.Contact).IsRequired(false);
                entity.Property(e => e.Preferences).IsRequired(false);
                entity.Property(e => e.Interests).IsRequired(false);

                entity.HasOne(e => e.User)
                    .WithOne(c => c.Patient)
                    .HasForeignKey<Patient>(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // STAFF
            builder.Entity<Staff>(entity =>
            {
                entity.Property(e => e.UserId).IsRequired();
                entity.Property(e => e.ContractForm).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Department).IsRequired().HasMaxLength(100);
                entity.Property(e => e.HiredDate).IsRequired();
                entity.Property(e => e.Taxes).HasColumnType("decimal(18,2)");
                entity.Property(e => e.HourlyRate).HasColumnType("decimal(18,2)");

                entity.Property(e => e.Bankdetails).IsRequired(false);
                entity.Property(e => e.Specialization).IsRequired(false).HasMaxLength(100);

                entity.HasOne(e => e.User)
                    .WithOne(c => c.Staff)
                    .HasForeignKey<Staff>(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // APPOINTMENT
            builder.Entity<Appointment>(entity =>
            {
                entity.Property(e => e.ScheduleId).IsRequired(false);
                entity.Property(e => e.AppointmentSlotId).IsRequired(false);
                entity.Property(e => e.Notes).IsRequired(false);
                entity.Property(e => e.CreatedBy).IsRequired(false);
                entity.Property(e => e.ConfirmedAt).IsRequired(false);
                entity.Property(e => e.CompletedAt).IsRequired(false);

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
                    .OnDelete(DeleteBehavior.Restrict)
                    .IsRequired(false);

                entity.HasOne(e => e.AppointmentSlot)
                    .WithMany(s => s.Appointments)
                    .HasForeignKey(e => e.AppointmentSlotId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .IsRequired(false);
            });

            builder.Entity<AppointmentSlot>(entity =>
            {
                entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Notes).IsRequired(false);

                // Schedule relationship - Keep CASCADE (slots should be deleted with schedule)
                entity.HasOne(e => e.Schedule)
                    .WithMany(s => s.AppointmentSlots)
                    .HasForeignKey(e => e.ScheduleId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Staff)
                    .WithMany()  // No navigation property needed on Staff side
                    .HasForeignKey(e => e.StaffId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<AppointmentSlotConfiguration>(entity =>
            {
                entity.HasOne(e => e.Staff)
                    .WithMany()
                    .HasForeignKey(e => e.StaffId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<AppointmentBlock>(entity =>
            {
                entity.HasOne(e => e.Staff)
                    .WithMany()
                    .HasForeignKey(e => e.StaffId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
            // SCHEDULE
            builder.Entity<Schedule>(entity =>
            {
                entity.Property(e => e.Notes).IsRequired(false);
                entity.Property(e => e.UpdatedAt).IsRequired(false);

                entity.HasOne(e => e.Staff)
                    .WithMany(c => c.Schedules)
                    .HasForeignKey(e => e.StaffId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<TimeReport>(entity =>
            {

                entity.HasOne(e => e.Staff)
                    .WithMany(c => c.TimeReports)
                    .HasForeignKey(e => e.StaffId)
                    .OnDelete(DeleteBehavior.Restrict);


                entity.HasOne(e => e.Schedule)
                    .WithOne(c => c.TimeReport)
                    .HasForeignKey<TimeReport>(e => e.ScheduleId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<Invoice>(entity =>
            {

                entity.HasOne(e => e.Patient)
                    .WithMany(c => c.Invoices)
                    .HasForeignKey(e => e.PatientId)
                    .OnDelete(DeleteBehavior.Restrict);


                entity.HasOne(e => e.Appointment)
                    .WithOne(c => c.Invoice)
                    .HasForeignKey<Invoice>(e => e.AppointmentId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
            // LEAVES

            /*            builder.Entity<Leave>(entity =>
                        {
                            entity.HasOne(e => e.Staff)
                            .WithMany(c => c.Leaves)
                            .HasForeignKey(entity => entity.StaffId)
                            .OnDelete(DeleteBehavior.Cascade);
                        });*/
        }
    }
}
