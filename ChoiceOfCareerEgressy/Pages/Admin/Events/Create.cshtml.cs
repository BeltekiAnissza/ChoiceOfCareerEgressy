using ChoiceOfCareerEgressy.Dto.ProgramDto;
using ChoiceOfCareerEgressy.Model.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace ChoiceOfCareerEgressy.Pages.Events
{
    [Authorize(Roles = "Admin")]
    public class CreateModel : PageModel
    {
        private readonly EventService _eventService;

        public CreateModel(EventService eventService)
        {
            _eventService = eventService;
        }

        // =========================================
        // ESEMÉNY ADATAI
        // =========================================

        [BindProperty]
        public CreateEventDto EventInput { get; set; } = new();

        // =========================================
        // DÁTUMMEZŐK
        // =========================================

        [BindProperty]
        [Required(ErrorMessage = "Az esemény dátuma kötelező.")]
        public string EventDateText { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "A jelentkezés kezdő dátuma kötelező.")]
        public string RegistrationStartDateText { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "A jelentkezés záró dátuma kötelező.")]
        public string RegistrationEndDateText { get; set; } = string.Empty;

        // =========================================
        // JELENTKEZÉSI IDŐPONTOK
        // =========================================

        [BindProperty]
        [Required(ErrorMessage = "A jelentkezés kezdő időpontja kötelező.")]
        public string RegistrationStartTime { get; set; } = "08:00";

        [BindProperty]
        [Required(ErrorMessage = "A jelentkezés záró időpontja kötelező.")]
        public string RegistrationEndTime { get; set; } = "16:00";

        // =========================================
        // IDŐSÁVOK
        // =========================================

        [BindProperty]
        public List<CreateTimeSlotInput> TimeSlots { get; set; } = new();

        public class CreateTimeSlotInput
        {
            [Required(ErrorMessage = "Az idősáv neve kötelező.")]
            public string TimeSlotName { get; set; } = string.Empty;

            [Required(ErrorMessage = "Az idősáv kezdete kötelező.")]
            public string StartTime { get; set; } = string.Empty;

            [Required(ErrorMessage = "Az idősáv vége kötelező.")]
            public string EndTime { get; set; } = string.Empty;
        }

        // =========================================
        // DÁTUM- ÉS IDŐFORMÁTUMOK
        // =========================================

        private const string DateFormat = "yyyy/MM/dd";
        private const string TimeFormat = @"hh\:mm";

        private static bool TryParseDate(
            string? value,
            out DateTime date)
        {
            return DateTime.TryParseExact(
                value,
                DateFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out date);
        }

        private static bool TryParseTime(
            string? value,
            out TimeSpan time)
        {
            return TimeSpan.TryParseExact(
                value,
                TimeFormat,
                CultureInfo.InvariantCulture,
                out time);
        }

        // =========================================
        // OLDAL BETÖLTÉSE
        // =========================================

        public void OnGet()
        {
            TimeSlots = new List<CreateTimeSlotInput>
            {
                new()
                {
                    TimeSlotName = "1. idősáv",
                    StartTime = "08:00",
                    EndTime = "10:00"
                },
                new()
                {
                    TimeSlotName = "2. idősáv",
                    StartTime = "10:15",
                    EndTime = "12:15"
                }
            };
        }

        // =========================================
        // ESEMÉNY LÉTREHOZÁSA
        // =========================================

        public async Task<IActionResult> OnPostAsync()
        {
            // =====================================
            // NEM KÖZVETLENÜL BEKÜLDÖTT DTO-MEZŐK
            // =====================================

            ModelState.Remove("EventInput.EventDate");
            ModelState.Remove("EventInput.RegistrationStart");
            ModelState.Remove("EventInput.RegistrationEnd");
            ModelState.Remove("EventInput.TimeSlots");

            // =====================================
            // DÁTUMOK ELLENŐRZÉSE
            // =====================================

            bool eventDateValid =
                TryParseDate(EventDateText, out var eventDate);

            bool registrationStartDateValid =
                TryParseDate(
                    RegistrationStartDateText,
                    out var registrationStartDate);

            bool registrationEndDateValid =
                TryParseDate(
                    RegistrationEndDateText,
                    out var registrationEndDate);

            if (!eventDateValid)
            {
                ModelState.AddModelError(
                    nameof(EventDateText),
                    "Az esemény dátumát ÉÉÉÉ/HH/NN formátumban add meg.");
            }

            if (!registrationStartDateValid)
            {
                ModelState.AddModelError(
                    nameof(RegistrationStartDateText),
                    "A jelentkezés kezdő dátumát ÉÉÉÉ/HH/NN formátumban add meg.");
            }

            if (!registrationEndDateValid)
            {
                ModelState.AddModelError(
                    nameof(RegistrationEndDateText),
                    "A jelentkezés záró dátumát ÉÉÉÉ/HH/NN formátumban add meg.");
            }

            // =====================================
            // JELENTKEZÉSI IDŐPONTOK ELLENŐRZÉSE
            // =====================================

            bool registrationStartTimeValid =
                TryParseTime(
                    RegistrationStartTime,
                    out var registrationStartTime);

            bool registrationEndTimeValid =
                TryParseTime(
                    RegistrationEndTime,
                    out var registrationEndTime);

            if (!registrationStartTimeValid)
            {
                ModelState.AddModelError(
                    nameof(RegistrationStartTime),
                    "A jelentkezés kezdő időpontját ÓÓ:PP formátumban add meg.");
            }

            if (!registrationEndTimeValid)
            {
                ModelState.AddModelError(
                    nameof(RegistrationEndTime),
                    "A jelentkezés záró időpontját ÓÓ:PP formátumban add meg.");
            }

            // =====================================
            // IDŐSÁVOK SZÁMÁNAK ELLENŐRZÉSE
            // =====================================

            if (TimeSlots == null || TimeSlots.Count < 2)
            {
                ModelState.AddModelError(
                    nameof(TimeSlots),
                    "Legalább 2 idősávot meg kell adni.");
            }

            // =====================================
            // IDŐSÁVOK FELDOLGOZÁSA
            // =====================================

            var parsedSlots = new List<CreateTimeSlotDto>();

            if (TimeSlots != null)
            {
                for (int i = 0; i < TimeSlots.Count; i++)
                {
                    var slot = TimeSlots[i];

                    if (slot == null)
                    {
                        ModelState.AddModelError(
                            nameof(TimeSlots),
                            $"A(z) {i + 1}. idősáv adatai hiányoznak.");

                        continue;
                    }

                    // IDŐSÁV NEVE

                    if (string.IsNullOrWhiteSpace(slot.TimeSlotName))
                    {
                        ModelState.AddModelError(
                            $"TimeSlots[{i}].TimeSlotName",
                            $"A(z) {i + 1}. idősáv neve kötelező.");
                    }

                    // KEZDÉS

                    bool startValid =
                        TryParseTime(slot.StartTime, out var startTime);

                    if (!startValid)
                    {
                        ModelState.AddModelError(
                            $"TimeSlots[{i}].StartTime",
                            $"A(z) {i + 1}. idősáv kezdete hibás. Példa: 08:00.");
                    }

                    // BEFEJEZÉS

                    bool endValid =
                        TryParseTime(slot.EndTime, out var endTime);

                    if (!endValid)
                    {
                        ModelState.AddModelError(
                            $"TimeSlots[{i}].EndTime",
                            $"A(z) {i + 1}. idősáv vége hibás. Példa: 10:00.");
                    }

                    if (!eventDateValid || !startValid || !endValid)
                    {
                        continue;
                    }

                    parsedSlots.Add(new CreateTimeSlotDto
                    {
                        TimeSlotName = slot.TimeSlotName?.Trim() ?? string.Empty,
                        BeginDate = eventDate.Add(startTime),
                        EndDate = eventDate.Add(endTime)
                    });
                }
            }

            // =====================================
            // HIBÁK ELLENŐRZÉSE
            // =====================================

            if (!ModelState.IsValid)
            {
                return Page();
            }

            // =====================================
            // ESEMÉNY DÁTUMA
            // =====================================

            EventInput.EventDate = eventDate;

            // =====================================
            // JELENTKEZÉSI IDŐPONTOK
            // =====================================

            EventInput.RegistrationStart =
                registrationStartDate.Add(registrationStartTime);

            EventInput.RegistrationEnd =
                registrationEndDate.Add(registrationEndTime);

            // =====================================
            // IDŐSÁVOK
            // =====================================

            EventInput.TimeSlots = parsedSlots;

            // =====================================
            // ESEMÉNY MENTÉSE
            // =====================================

            try
            {
                var created =
                    await _eventService.CreateAsync(EventInput);

                TempData["SuccessMessage"] =
                    $"Az esemény sikeresen létrejött: {created.EventName}";

                return RedirectToPage("/Admin/Programs/Import");
            }
            catch (ArgumentException ex)
            {
                ModelState.AddModelError("", ex.Message);

                return Page();
            }
        }
    }
}