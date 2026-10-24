using System.ComponentModel.DataAnnotations;

namespace ChoiceOfCareerEgressy.Dto.GradeDto
{
    public class GradeListItemDto
    {
        public int GradeId { get; set; }
        public string GradeName { get; set; } = "";
        public int StudentCount { get; set; }
        public int TeacherCount { get; set; }
    }

    public class GradeRenameDto
    {
        [Range(1, int.MaxValue)]
        public int GradeId { get; set; }

        [Required(ErrorMessage = "Add meg az új osztálynevet!")]
        [StringLength(100, MinimumLength = 1)]
        public string NewGradeName { get; set; } = "";
    }

    public class GradeCreateDto
    {
        [Required(ErrorMessage = "Add meg az osztály nevét!")]
        [StringLength(100, MinimumLength = 1)]
        public string GradeName { get; set; } = "";
    }
}
