using System.ComponentModel.DataAnnotations;

namespace ChoiceOfCareerEgressy.Dto.ProgramDto.AdminPrograms
{
    public class AdminProgramDto
    {
        // Új programnál 0.
        // Módosításnál a meglévő PlanSession azonosítója.
        public int PlanSessionId { get; set; }

        // A program alapadatainak azonosítója.
        public int PlanId { get; set; }

        [Required(ErrorMessage = "A program neve kötelező.")]
        [StringLength(200, ErrorMessage =
            "A program neve maximum 200 karakter lehet.")]
        [Display(Name = "Program neve")]
        public string PlanName { get; set; } = "";

        [Required(ErrorMessage = "A program leírása kötelező.")]
        [Display(Name = "Program leírása")]
        public string PlanDescription { get; set; } = "";

        // Null = minden ágazat számára elérhető.
        [Display(Name = "Ágazat")]
        public int? BranchId { get; set; }

        // Null = Unisex, minden nem számára elérhető.
        [Display(Name = "Nem")]
        public int? SexId { get; set; }

        [Range(1, int.MaxValue,
            ErrorMessage = "Válassz eseményt.")]
        [Display(Name = "Esemény")]
        public int EventId { get; set; }

        [Range(1, int.MaxValue,
            ErrorMessage = "Válassz idősávot.")]
        [Display(Name = "Idősáv")]
        public int TimeSlotId { get; set; }

        // Meglévő terem kiválasztása.
        // Ha nincs megfelelő, az új terem nevét
        // a NewLocationName mezőben adhatjuk meg.
        [Display(Name = "Terem")]
        public int? LocationId { get; set; }

        [StringLength(150)]
        [Display(Name = "Új terem neve")]
        public string? NewLocationName { get; set; }

        [Range(1, int.MaxValue,
            ErrorMessage = "A kapacitás legalább 1 fő legyen.")]
        [Display(Name = "Kapacitás")]
        public int Capacity { get; set; } = 1;

        [Range(0, int.MaxValue,
            ErrorMessage =
                "A szükséges felügyelők száma nem lehet negatív.")]
        [Display(Name = "Szükséges felügyelők")]
        public int RequiredSupervisors { get; set; } = 1;
    }
}
