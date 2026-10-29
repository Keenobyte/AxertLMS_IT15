using Microsoft.EntityFrameworkCore;
using AxertLMS.Models;

namespace AxertLMS.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // ============================================================
        // CORE USERS
        // ============================================================

        public DbSet<User> Users { get; set; }

        public DbSet<Student> Students { get; set; }

        public DbSet<Teacher> Teachers { get; set; }

        // ============================================================
        // ACADEMIC ARCHITECTURE
        // ============================================================

        public DbSet<Section> Sections { get; set; }

        public DbSet<Subject> Subjects { get; set; }

        public DbSet<CurriculumMap> CurriculumMaps { get; set; }

        public DbSet<Strand> Strands { get; set; }

        // ============================================================
        // CLASS MANAGEMENT
        // ============================================================

        public DbSet<Classroom> Classrooms { get; set; }

        public DbSet<Enrollment> Enrollments { get; set; }

        public DbSet<Module> Modules { get; set; }

        public DbSet<Announcement> Announcements { get; set; }

        // ============================================================
        // GRADING & SUBMISSIONS
        // ============================================================

        public DbSet<Assignment> Assignments { get; set; }

        public DbSet<Submission> Submissions { get; set; }

        public DbSet<Grade> Grades { get; set; }

        // ============================================================
        // QUIZ SYSTEM
        // ============================================================

        public DbSet<Quiz> Quizzes { get; set; }

        public DbSet<QuizQuestion> QuizQuestions { get; set; }

        // ============================================================
        // MODEL CONFIGURATION
        // ============================================================

        protected override void OnModelCreating(
            ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // --------------------------------------------------------
            // USER EMAIL
            // --------------------------------------------------------

            modelBuilder.Entity<User>()
                .HasIndex(u => u.email)
                .IsUnique();

            // --------------------------------------------------------
            // CURRICULUM MAP -> LEGACY DATABASE SCHEMA
            // --------------------------------------------------------
            //
            // The existing database stores the curriculum strand as
            // text in CurriculumMaps.strand.
            //
            // It does NOT contain:
            //   strand_id
            //   is_core_subject
            //
            // Explicitly map the model so EF Core cannot infer the
            // nonexistent strand_id column.
            //

            modelBuilder.Entity<CurriculumMap>(entity =>
            {
                entity.ToTable("CurriculumMaps");

                entity.HasKey(c =>
                    c.map_id);

                entity.Property(c =>
                    c.map_id)
                    .HasColumnName("curriculum_map_id");

                entity.Property(c =>
                    c.subject_id)
                    .HasColumnName("subject_id");

                entity.Property(c =>
                    c.strand)
                    .HasColumnName("strand")
                    .IsRequired();

                entity.Property(c =>
                    c.year_lvl)
                    .HasColumnName("year_lvl")
                    .HasMaxLength(5)
                    .IsRequired();

                entity.HasIndex(c =>
                    c.subject_id);

                entity.Ignore(c =>
                    c.strand_id);

                entity.Ignore(c =>
                    c.Strand);

                entity.Ignore(c =>
                    c.is_core_subject);

                entity.HasOne(c =>
                    c.Subject)
                    .WithMany()
                    .HasForeignKey(c =>
                        c.subject_id)
                    .OnDelete(
                        DeleteBehavior.NoAction);
            });
        }
    }
}