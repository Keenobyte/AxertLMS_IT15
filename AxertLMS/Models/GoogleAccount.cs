using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AxertLMS.Models
{
    [Table("GoogleAccounts")]
    public class GoogleAccount
    {
        [Key]
        public int GoogleAccountId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [MaxLength(255)]
        public required string GoogleSubject { get; set; }

        [Required]
        [MaxLength(255)]
        public required string GoogleEmail { get; set; }

        [MaxLength(200)]
        public string? DisplayName { get; set; }

        [MaxLength(500)]
        public string? ProfilePictureUrl { get; set; }

        [MaxLength(10000)]
        public string? EncryptedAccessToken { get; set; }

        [MaxLength(10000)]
        public string? EncryptedRefreshToken { get; set; }

        public DateTime? AccessTokenExpiresUtc { get; set; }

        public DateTime? RefreshTokenExpiresUtc { get; set; }

        [MaxLength(2000)]
        public string GrantedScopes { get; set; } = string.Empty;

        /*
         * This is separate from Users.status.
         *
         * Pending:
         * Google account exists, but Registrar has not yet
         * approved the student's enrollment.
         *
         * Approved:
         * Registrar has completed the student's enrollment.
         */
        [Required]
        [MaxLength(20)]
        public string EnrollmentStatus { get; set; } = "Approved";

        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }
    }
}