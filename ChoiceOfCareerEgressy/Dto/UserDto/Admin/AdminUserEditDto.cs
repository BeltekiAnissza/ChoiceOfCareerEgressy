using System.ComponentModel.DataAnnotations;

namespace ChoiceOfCareerEgressy.Dto.UserDto.Admin
{
    public class AdminUserEditDto
    {
        public int UserId { get; set; }

        [Required(ErrorMessage = "A név megadása kötelező.")]
        [StringLength(200)]
        [Display(Name = "Teljes név")]
        public string UserName { get; set; } = "";

        [Required(ErrorMessage = "Az e-mail-cím megadása kötelező.")]
        [EmailAddress(ErrorMessage = "Érvénytelen e-mail-cím.")]
        [Display(Name = "E-mail-cím")]
        public string EmailAddress { get; set; } = "";

        [Display(Name = "Nem")]
        public int? SexId { get; set; }

        [Display(Name = "Ágazat")]
        public int? BranchId { get; set; }

        [Display(Name = "Osztály")]
        public int? GradeId { get; set; }

        // Csak megjelenítéshez.
        // Az admin ezen az oldalon nem módosítja.
        public string Role { get; set; } = "";

        // Üresen hagyva nem változik a jelszó.
        [Display(Name = "Új jelszó")]
        public string? NewPassword { get; set; }

        [Compare(nameof(NewPassword),
            ErrorMessage = "A két jelszó nem egyezik.")]
        [Display(Name = "Új jelszó megerősítése")]
        public string? ConfirmPassword { get; set; }
    }
}

