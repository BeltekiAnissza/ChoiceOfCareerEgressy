using ChoiceOfCareerEgressy.Dto.ProgramDto;
using ChoiceOfCareerEgressy.Model.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace ChoiceOfCareerEgressy.Pages.Admin.Events
{
    [Authorize(Roles = "Admin")]
    public class EditModel : PageModel
    {
        private readonly EventService _eventService;

        public EditModel(EventService eventService)
        {
            _eventService = eventService;
        }

        // =========================================
        // ESEMÉNY AZONOSÍTÓJA
        // =========================================

        [BindProperty]
        public int EventId { get; set; }

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
        public string RegistrationStartDateText { get; set; }
            = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "A jelentkezés záró dátuma kötelező.")]
        public string RegistrationEndDateText { get; set; }
            = string.Empty;

        // =========================================
        // JELENTKEZÉSI IDŐPONTOK
        // =========================================

        [BindProperty]
        [Required(ErrorMessage = "A jelentkezés kezdő időpontja kötelező.")]
        public string RegistrationStartTime { get; set; }
            = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "A jelentkezés záró időpontja kötelező.")]
        public string RegistrationEndTime { get; set; }
            = string.Empty;

        // =========================================
        // IDŐSÁVOK
        // =========================================

        [BindProperty]
        public List<EditTimeSlotInput> TimeSlots { get; set; }
            = new();

        public class EditTimeSlotInput
        {
            // 0 = új idősáv
            // 0-nál nagyobb = meglévő idősáv

            public int TimeSlotId { get; set; }

            [Required(ErrorMessage = "Az idősáv neve kötelező.")]
            public string TimeSlotName { get; set; }
                = string.Empty;

            [Required(ErrorMessage = "Az idősáv kezdete kötelező.")]
            public string StartTime { get; set; }
                = string.Empty;

            [Required(ErrorMessage = "Az idősáv vége kötelező.")]
            public string EndTime { get; set; }
                = string.Empty;

            public bool IsProtected { get; set; }
        }

        // =========================================
        // FORMÁTUMOK
        // =========================================

        private const string DateFormat = "yyyy/MM/dd";

        private const string TimeFormat = @"hh\:mm";

        // =========================================
        // MAGYAR IDŐZÓNA
        // =========================================

        private static readonly TimeZoneInfo HungarianTimeZone =
            TimeZoneInfo.FindSystemTimeZoneById(
                "Europe/Budapest");

        private static DateTime ToHungarianLocal(
            DateTimeOffset value)
        {
            return TimeZoneInfo
                .ConvertTime(value, HungarianTimeZone)
                .DateTime;
        }

        // =========================================
        // DÁTUM FELDOLGOZÁSA
        // =========================================

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

        // =========================================
        // IDŐPONT FELDOLGOZÁSA
        // =========================================

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

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var existingEvent =
                await _eventService.GetEditDetailsAsync(id);

            if (existingEvent == null)
            {
                return NotFound();
            }

            EventId = existingEvent.EventId;

            // =====================================
            // ESEMÉNY ADATAI
            // =====================================

            EventInput.EventName =
                existingEvent.EventName;

            EventInput.IsRegistrationEnabled =
                existingEvent.IsRegistrationEnabled;

            // =====================================
            // DÁTUMOK
            // =====================================

            var eventDate =
                ToHungarianLocal(existingEvent.EventDate);

            var registrationStart =
                ToHungarianLocal(existingEvent.RegistrationStart);

            var registrationEnd =
                ToHungarianLocal(existingEvent.RegistrationEnd);

            EventDateText = eventDate.ToString(
                DateFormat,
                CultureInfo.InvariantCulture);

            RegistrationStartDateText =
                registrationStart.ToString(
                    DateFormat,
                    CultureInfo.InvariantCulture);

            RegistrationEndDateText =
                registrationEnd.ToString(
                    DateFormat,
                    CultureInfo.InvariantCulture);

            RegistrationStartTime =
                registrationStart.ToString("HH:mm");

            RegistrationEndTime =
                registrationEnd.ToString("HH:mm");

            // =====================================
            // IDŐSÁVOK BETÖLTÉSE
            // =====================================

            TimeSlots = existingEvent.TimeSlots
                .OrderBy(s => s.BeginDate)
                .Select(s =>
                {
                    var begin =
                        ToHungarianLocal(s.BeginDate);

                    var end =
                        ToHungarianLocal(s.EndDate);

                    return new EditTimeSlotInput
                    {
                        TimeSlotId = s.TimeSlotId,

                        TimeSlotName = s.TimeSlotName,

                        StartTime = begin.ToString("HH:mm"),

                        EndTime = end.ToString("HH:mm"),

                        IsProtected = s.IsProtected
                    };
                })
                .ToList();

            return Page();
        }

        // =========================================
        // ESEMÉNY MENTÉSE
        // =========================================

        public async Task<IActionResult> OnPostAsync()
        {
            // =====================================
            // DTO DÁTUMMEZŐK
            // =====================================

            ModelState.Remove("EventInput.EventDate");
            ModelState.Remove("EventInput.RegistrationStart");
            ModelState.Remove("EventInput.RegistrationEnd");
            ModelState.Remove("EventInput.TimeSlots");

            // =====================================
            // ESEMÉNY ELLENŐRZÉSE
            // =====================================

            if (EventId <= 0)
            {
                return BadRequest();
            }

            var existingEvent =
                await _eventService.GetEditDetailsAsync(EventId);

            if (existingEvent == null)
            {
                return NotFound();
            }

            // =====================================
            // DÁTUMOK FELDOLGOZÁSA
            // =====================================

            bool eventDateValid =
                TryParseDate(
                    EventDateText,
                    out var eventDate);

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
                    "A jelentkezés kezdő dátuma hibás.");
            }

            if (!registrationEndDateValid)
            {
                ModelState.AddModelError(
                    nameof(RegistrationEndDateText),
                    "A jelentkezés záró dátuma hibás.");
            }

            // =====================================
            // IDŐPONTOK FELDOLGOZÁSA
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
                    "A jelentkezés kezdő időpontja hibás.");
            }

            if (!registrationEndTimeValid)
            {
                ModelState.AddModelError(
                    nameof(RegistrationEndTime),
                    "A jelentkezés záró időpontja hibás.");
            }

            // =====================================
            // IDŐSÁVOK SZÁMA
            // =====================================

            if (TimeSlots == null || TimeSlots.Count < 2)
            {
                ModelState.AddModelError(
                    nameof(TimeSlots),
                    "Legalább két idősávot meg kell adni.");
            }

            // =====================================
            // IDŐSÁVOK FELDOLGOZÁSA
            // =====================================

            var parsedSlots =
                new List<CreateTimeSlotDto>();

            var existingSlotIds =
                new Dictionary<int, int>();

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

                    // =================================
                    // MEGLÉVŐ IDŐSÁV AZONOSÍTÓJA
                    // =================================

                    if (slot.TimeSlotId > 0)
                    {
                        existingSlotIds[i] =
                            slot.TimeSlotId;
                    }

                    // =================================
                    // NÉV
                    // =================================

                    if (string.IsNullOrWhiteSpace(
                        slot.TimeSlotName))
                    {
                        ModelState.AddModelError(
                            $"TimeSlots[{i}].TimeSlotName",
                            "Az idősáv neve kötelező.");
                    }

                    // =================================
                    // KEZDÉS
                    // =================================

                    bool startValid =
                        TryParseTime(
                            slot.StartTime,
                            out var startTime);

                    if (!startValid)
                    {
                        ModelState.AddModelError(
                            $"TimeSlots[{i}].StartTime",
                            "Hibás kezdési időpont. Példa: 08:00.");
                    }

                    // =================================
                    // BEFEJEZÉS
                    // =================================

                    bool endValid =
                        TryParseTime(
                            slot.EndTime,
                            out var endTime);

                    if (!endValid)
                    {
                        ModelState.AddModelError(
                            $"TimeSlots[{i}].EndTime",
                            "Hibás befejezési időpont. Példa: 10:00.");
                    }

                    if (!eventDateValid ||
                        !startValid ||
                        !endValid)
                    {
                        continue;
                    }

                    parsedSlots.Add(new CreateTimeSlotDto
                    {
                        TimeSlotName =
                            slot.TimeSlotName?.Trim()
                            ?? string.Empty,

                        BeginDate =
                            eventDate.Add(startTime),

                        EndDate =
                            eventDate.Add(endTime)
                    });
                }
            }

            // =====================================
            // HIBÁK ELLENŐRZÉSE
            // =====================================

            if (!ModelState.IsValid)
            {
                await RefreshProtectedSlotsAsync();

                return Page();
            }

            // =====================================
            // DTO FELTÖLTÉSE
            // =====================================

            EventInput.EventDate = eventDate;

            EventInput.RegistrationStart =
                registrationStartDate.Add(
                    registrationStartTime);

            EventInput.RegistrationEnd =
                registrationEndDate.Add(
                    registrationEndTime);

            EventInput.TimeSlots = parsedSlots;

            // =====================================
            // MENTÉS
            // =====================================

            try
            {
                await _eventService.UpdateAsync(
                    EventId,
                    EventInput,
                    existingSlotIds);

                TempData["SuccessMessage"] =
                    "Az esemény adatai sikeresen módosítva.";

                return RedirectToPage(
                    "/Admin/Events/Index");
            }
            catch (ArgumentException ex)
            {
                ModelState.AddModelError(
                    "",
                    ex.Message);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(
                    "",
                    ex.Message);
            }

            await RefreshProtectedSlotsAsync();

            return Page();
        }

        // =========================================
        // VÉDETT IDŐSÁVOK FRISSÍTÉSE
        // =========================================

        private async Task RefreshProtectedSlotsAsync()
        {
            if (TimeSlots == null ||
                TimeSlots.Count == 0)
            {
                return;
            }

            var slotIds = TimeSlots
                .Where(s => s != null && s.TimeSlotId > 0)
                .Select(s => s.TimeSlotId)
                .Distinct()
                .ToList();

            var protectedIds =
                await _eventService
                    .GetProtectedTimeSlotIdsAsync(slotIds);

            foreach (var slot in TimeSlots)
            {
                if (slot != null)
                {
                    slot.IsProtected =
                        protectedIds.Contains(
                            slot.TimeSlotId);
                }
            }
        }
    }
}