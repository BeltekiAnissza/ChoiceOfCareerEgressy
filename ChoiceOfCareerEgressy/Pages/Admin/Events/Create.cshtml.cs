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
        // 1. IDŐSÁV
        // =========================================

        [BindProperty]
        [Required(ErrorMessage = "Az első idősáv kezdete kötelező.")]
        public string FirstSlotStartTime { get; set; } = "08:00";

        [BindProperty]
        [Required(ErrorMessage = "Az első idősáv vége kötelező.")]
        public string FirstSlotEndTime { get; set; } = "10:00";

        // =========================================
        // 2. IDŐSÁV
        // =========================================

        [BindProperty]
        [Required(ErrorMessage = "A második idősáv kezdete kötelező.")]
        public string SecondSlotStartTime { get; set; } = "10:15";

        [BindProperty]
        [Required(ErrorMessage = "A második idősáv vége kötelező.")]
        public string SecondSlotEndTime { get; set; } = "12:15";

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
            ModelState.Remove("EventInput.FirstSlotStart");
            ModelState.Remove("EventInput.FirstSlotEnd");
            ModelState.Remove("EventInput.SecondSlotStart");
            ModelState.Remove("EventInput.SecondSlotEnd");

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
            // IDŐPONTOK ELLENŐRZÉSE
            // =====================================

            bool registrationStartTimeValid =
                TryParseTime(
                    RegistrationStartTime,
                    out var registrationStartTime);

            bool registrationEndTimeValid =
                TryParseTime(
                    RegistrationEndTime,
                    out var registrationEndTime);

            bool firstSlotStartValid =
                TryParseTime(
                    FirstSlotStartTime,
                    out var firstSlotStartTime);

            bool firstSlotEndValid =
                TryParseTime(
                    FirstSlotEndTime,
                    out var firstSlotEndTime);

            bool secondSlotStartValid =
                TryParseTime(
                    SecondSlotStartTime,
                    out var secondSlotStartTime);

            bool secondSlotEndValid =
                TryParseTime(
                    SecondSlotEndTime,
                    out var secondSlotEndTime);

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

            if (!firstSlotStartValid)
            {
                ModelState.AddModelError(
                    nameof(FirstSlotStartTime),
                    "Az első idősáv kezdete például 08:00 lehet.");
            }

            if (!firstSlotEndValid)
            {
                ModelState.AddModelError(
                    nameof(FirstSlotEndTime),
                    "Az első idősáv vége például 10:00 lehet.");
            }

            if (!secondSlotStartValid)
            {
                ModelState.AddModelError(
                    nameof(SecondSlotStartTime),
                    "A második idősáv kezdete például 10:15 lehet.");
            }

            if (!secondSlotEndValid)
            {
                ModelState.AddModelError(
                    nameof(SecondSlotEndTime),
                    "A második idősáv vége például 12:15 lehet.");
            }

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
            // 1. IDŐSÁV
            // =====================================

            EventInput.FirstSlotStart =
                eventDate.Add(firstSlotStartTime);

            EventInput.FirstSlotEnd =
                eventDate.Add(firstSlotEndTime);

            // =====================================
            // 2. IDŐSÁV
            // =====================================

            EventInput.SecondSlotStart =
                eventDate.Add(secondSlotStartTime);

            EventInput.SecondSlotEnd =
                eventDate.Add(secondSlotEndTime);

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