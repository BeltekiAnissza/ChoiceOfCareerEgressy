namespace ChoiceOfCareerEgressy.Dto.ProgramDto.TeacherPrograms
{
    public class AdminTeacherDto
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = "";
        public string EmailAddress { get; set; } = "";

        public int SupervisionCount { get; set; }

        public List<AdminTeacherSlotDto> TimeSlots { get; set; } = new();
    }
}
