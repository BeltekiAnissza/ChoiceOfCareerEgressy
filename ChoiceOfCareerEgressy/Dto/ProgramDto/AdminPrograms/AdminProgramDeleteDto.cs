namespace ChoiceOfCareerEgressy.Dto.ProgramDto.AdminPrograms
{
    public class AdminProgramDeleteDto
    {
        public int PlanSessionId { get; set; }

        public string PlanName { get; set; } = "";
        public string EventName { get; set; } = "";
        public string TimeSlotName { get; set; } = "";
        public string LocationName { get; set; } = "";

        public DateTimeOffset BeginDate { get; set; }
        public DateTimeOffset EndDate { get; set; }

        // Az összes törlendő jelentkezés,
        // beleértve a lemondottakat is.
        public int TotalApplicationCount { get; set; }

        // Jelenleg aktív diákjelentkezések.
        public int ActiveApplicationCount { get; set; }

        // Törlendő tanári felügyeletek.
        public int SupervisionCount { get; set; }

        // Ha a Plan más idősávban is szerepel,
        // csak ezt a PlanSession rekordot töröljük.
        public bool HasOtherSessions { get; set; }
    }
}
