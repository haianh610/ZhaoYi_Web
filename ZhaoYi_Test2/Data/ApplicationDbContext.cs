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

            // Cấu hình tên bảng với tiền tố "Interpreter"
            builder.Entity<Education>().ToTable("InterpreterEducations");
            builder.Entity<WorkExperience>().ToTable("InterpreterWorkExperiences");
            builder.Entity<Project>().ToTable("InterpreterProjects");

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

            // Interpreter -> Education: CASCADE
            builder.Entity<Education>()
                .HasOne(e => e.Interpreter)
                .WithMany(i => i.Educations)
                .HasForeignKey(e => e.InterpreterId)
                .OnDelete(DeleteBehavior.Cascade);

            // Interpreter -> WorkExperience: CASCADE
            builder.Entity<WorkExperience>()
                .HasOne(w => w.Interpreter)
                .WithMany(i => i.WorkExperiences)
                .HasForeignKey(w => w.InterpreterId)
                .OnDelete(DeleteBehavior.Cascade);

            // Interpreter -> Project: CASCADE
            builder.Entity<Project>()
                .HasOne(p => p.Interpreter)
                .WithMany(i => i.Projects)
                .HasForeignKey(p => p.InterpreterId)
                .OnDelete(DeleteBehavior.Cascade);
        }

        public DbSet<Interpreter> Interpreters { get; set; }
        public DbSet<Recruiter> Recruiters { get; set; }
        public DbSet<JobPosting> JobPostings { get; set; }
        public DbSet<JobApplication> JobApplications { get; set; }

        // Đăng ký DbSet với cùng tên property nhưng bảng có tiền tố "Interpreter"
        public DbSet<Education> Educations { get; set; }
        public DbSet<WorkExperience> WorkExperiences { get; set; }
        public DbSet<Project> Projects { get; set; }
    }
}
