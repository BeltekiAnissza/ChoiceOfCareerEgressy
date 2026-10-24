using System.ComponentModel.DataAnnotations;

namespace ChoiceOfCareerEgressy.Dto.UserDto
{
    public class LoginDto
    {

        [Required(ErrorMessage = "Az email-cím megadása kötelező.")]
        [EmailAddress(ErrorMessage = "Érvénytelen email-cím.")]
        public string EmailAddress { get; set; } = "";

        [Required(ErrorMessage = "A jelszó megadása kötelező.")]
        public string Password { get; set; } = "";
    }
}
