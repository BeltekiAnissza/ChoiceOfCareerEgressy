using ChoiceOfCareerEgressy.Persistence;
using System.ComponentModel.DataAnnotations;

namespace ChoiceOfCareerEgressy.Dto.UserDto.Admin
{
    public class AdminUserCreateDto
    {
        [Required(ErrorMessage = "A név megadása kötelező.")]
        [StringLength(200)]
        [Display(Name = "Teljes név")]
        public string UserName { get; set; } = "";

        [Required(ErrorMessage = "Az e-mail-cím kötelező.")]
        [EmailAddress(ErrorMessage = "Érvénytelen e-mail-cím.")]
        [Display(Name = "E-mail-cím")]
        public string EmailAddress { get; set; } = "";

        [Required(ErrorMessage = "Válassz szerepkört.")]
        [Display(Name = "Szerepkör")]
        public RoleChoice? Role { get; set; }

        [Display(Name = "Nem")]
        public int? SexId { get; set; }

        [Display(Name = "Ágazat")]
        public int? BranchId { get; set; }

        [Display(Name = "Osztály")]
        public int? GradeId { get; set; }

        [Required(ErrorMessage = "A jelszó kötelező.")]
        [MinLength(8, ErrorMessage =
            "A jelszó legalább 8 karakter legyen.")]
        [DataType(DataType.Password)]
        [Display(Name = "Jelszó")]
        public string Password { get; set; } = "";

        [Required(ErrorMessage = "Erősítsd meg a jelszót.")]
        [Compare(nameof(Password),
            ErrorMessage = "A két jelszó nem egyezik.")]
        [DataType(DataType.Password)]
        [Display(Name = "Jelszó megerősítése")]
        public string ConfirmPassword { get; set; } = "";

        [Display(Name = "Kötelező jelszóváltoztatás")]
        public bool MustChangePassword { get; set; } = true;
    }
}

