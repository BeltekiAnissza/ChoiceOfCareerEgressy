using ChoiceOfCareerEgressy.Dto.ProgramDto;
using ChoiceOfCareerEgressy.Dto.UserDto;
using ChoiceOfCareerEgressy.Model.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChoiceOfCareerEgressy.Controller.Admin
{
    [Route("api/events")]
    [ApiController]
    [Authorize]
    public class EventsController : ControllerBase
    {
        private readonly EventService _eventService;

        public EventsController(EventService eventService)
        {
            _eventService = eventService;
        }

        // =========================================
        // 1. ESEMÉNYEK LISTÁZÁSA
        // GET: api/events
        // Jogosultság: bejelentkezett felhasználó
        // =========================================

        [HttpGet]
        public async Task<ActionResult<List<EventListItemDto>>> GetAll()
        {
            var events = await _eventService.GetAllAsync();

            return Ok(events);
        }

        // =========================================
        // 2. EGY ESEMÉNY LEKÉRDEZÉSE
        // GET: api/events/1
        // Jogosultság: bejelentkezett felhasználó
        // =========================================

        [HttpGet("{id:int}")]
        public async Task<ActionResult<EventDetailsDto>> GetById(int id)
        {
            var eventData = await _eventService.GetByIdAsync(id);

            if (eventData == null)
            {
                return NotFound(new
                {
                    Message = "Az esemény nem található."
                });
            }

            return Ok(eventData);
        }

        // =========================================
        // 3. ESEMÉNY LÉTREHOZÁSA
        // POST: api/events
        // Jogosultság: Admin
        // =========================================

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult<CreatedEventDto>> Create([FromBody] CreateEventDto dto)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            try
            {
                var result = await _eventService.CreateAsync(dto);

                return CreatedAtAction(
                    nameof(GetById),
                    new { id = result.EventId },
                    result
                );
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    Message = ex.Message
                });
            }
        }

        // =========================================
        // 4. ESEMÉNY SZERKESZTÉSE
        // PUT: api/events/1
        // Jogosultság: Admin
        //
        // Támogatja:
        // - Esemény nevének módosítását
        // - Esemény dátumának módosítását
        // - Jelentkezési időszak módosítását
        // - Idősávok átnevezését
        // - Idősávok időpontjának módosítását
        // - Új idősávok hozzáadását
        // - Nem használt idősávok törlését
        //
        // A használatban lévő idősávokat
        // az EventService védi.
        // =========================================

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateEventRequest request)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            if (request.Event == null)
            {
                return BadRequest(new
                {
                    Message = "Az esemény adatai hiányoznak."
                });
            }

            if (request.ExistingSlotIds == null)
            {
                return BadRequest(new
                {
                    Message = "Az idősávok azonosítói hiányoznak."
                });
            }

            try
            {
                await _eventService.UpdateAsync(
                    id,
                    request.Event,
                    request.ExistingSlotIds
                );

                return Ok(new
                {
                    Success = true,
                    Message = "Az esemény sikeresen módosítva.",
                    EventId = id
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new
                {
                    Message = ex.Message
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    Message = ex.Message
                });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new
                {
                    Message = ex.Message
                });
            }
        }

        // =========================================
        // 5. ESEMÉNY ÁTNEVEZÉSE
        // PATCH: api/events/1/rename
        // Jogosultság: Admin
        // =========================================

        [HttpPatch("{id:int}/rename")]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Rename(int id, [FromBody] RenameEventRequest request)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            try
            {
                await _eventService.RenameAsync(
                    id,
                    request.EventName
                );

                return Ok(new
                {
                    Success = true,
                    Message = "Az esemény sikeresen átnevezve.",
                    EventId = id
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new
                {
                    Message = ex.Message
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    Message = ex.Message
                });
            }
        }

        // =========================================
        // 6. JELENTKEZÉS ENGEDÉLYEZÉSE / LETILTÁSA
        // PATCH: api/events/1/registration
        // Jogosultság: Admin
        // =========================================

        [HttpPatch("{id:int}/registration")]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetRegistrationEnabled(int id, [FromBody] RegistrationStatusRequest request)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            try
            {
                await _eventService.SetRegistrationEnabledAsync(
                    id,
                    request.IsEnabled
                );

                return Ok(new
                {
                    Success = true,

                    Message = request.IsEnabled
                        ? "A jelentkezés sikeresen engedélyezve."
                        : "A jelentkezés sikeresen letiltva.",

                    EventId = id,
                    IsRegistrationEnabled = request.IsEnabled
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new
                {
                    Message = ex.Message
                });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new
                {
                    Message = ex.Message
                });
            }
        }

        // =========================================
        // 7. ESEMÉNY TÖRLÉSE
        // DELETE: api/events/1
        // Jogosultság: Admin
        //
        // Az EventService kezeli a kapcsolódó:
        // - Diákjelentkezéseket
        // - Tanári felügyeleteket
        // - Programalkalmakat
        // - Felügyeleti helyeket
        // - Idősávokat
        // - Eseményt
        // =========================================

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _eventService.DeleteAsync(id);

                return Ok(new
                {
                    Success = true,
                    Message = "Az esemény sikeresen törölve.",
                    EventId = id
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new
                {
                    Message = ex.Message
                });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new
                {
                    Message = ex.Message
                });
            }
        }
    }

    // =========================================
    // ESEMÉNY SZERKESZTÉSI KÉRELEM
    // =========================================

    public class UpdateEventRequest
    {
        public CreateEventDto Event { get; set; } = new();

        // Kulcs: a beküldött TimeSlots lista indexe
        // Érték: a meglévő TimeSlotId
        //
        // Az új idősávokhoz nem kell bejegyzés.

        public Dictionary<int, int> ExistingSlotIds { get; set; }
            = new();
    }

    // =========================================
    // ESEMÉNY ÁTNEVEZÉSI KÉRELEM
    // =========================================

    public class RenameEventRequest
    {
        public string EventName { get; set; } = string.Empty;
    }

    // =========================================
    // JELENTKEZÉS ÁLLAPOTÁNAK MÓDOSÍTÁSA
    // =========================================

    public class RegistrationStatusRequest
    {
        public bool IsEnabled { get; set; }
    }
}