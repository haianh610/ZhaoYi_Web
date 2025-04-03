using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ZhaoYi_Test2.Models;

namespace ZhaoYi_Test2.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // JobPosting -> JobApplication: CASCADE
            builder.Entity<JobApplication>()
                .HasOne(ja => ja.JobPosting)
                .WithMany()
                .HasForeignKey(ja => ja.JobPostingId)
                .OnDelete(DeleteBehavior.Cascade);

            // Interpreter -> JobApplication: RESTRICT
            builder.Entity<JobApplication>()
                .HasOne(ja => ja.Interpreter)
                .WithMany()
                .HasForeignKey(ja => ja.InterpreterId)
                .OnDelete(DeleteBehavior.Restrict);

            // User -> JobApplication: RESTRICT
            builder.Entity<JobApplication>()
                .HasOne(ja => ja.User)
                .WithMany()
                .HasForeignKey(ja => ja.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // User -> JobPosting: RESTRICT
            builder.Entity<JobPosting>()
                .HasOne(jp => jp.User)
                .WithMany()
                .HasForeignKey(jp => jp.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Recruiter -> JobPosting: RESTRICT
            builder.Entity<JobPosting>()
                .HasOne(jp => jp.Recruiter)
                .WithMany()
                .HasForeignKey(jp => jp.RecruiterId)
                .OnDelete(DeleteBehavior.Restrict);

            // User -> Interpreter: RESTRICT
            builder.Entity<Interpreter>()
                .HasOne(i => i.User)
                .WithMany()
                .HasForeignKey(i => i.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // User -> Recruiter: RESTRICT
            builder.Entity<Recruiter>()
                .HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        }



        public DbSet<Interpreter> Interpreters { get; set; }
        public DbSet<Recruiter> Recruiters { get; set; }
        public DbSet<JobPosting> JobPostings { get; set; }
        public DbSet<JobApplication> JobApplications { get; set; }


    }
}
