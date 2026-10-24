using ChoiceOfCareerEgressy.Dto.ProgramDto;
using ChoiceOfCareerEgressy.Model.Admin;
using ChoiceOfCareerEgressy.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ChoiceOfCareerEgressy.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class EventsController : ControllerBase
    {
        private readonly ChoiceOfCareerPersistence _context;
        private readonly EventService _eventService;

        public EventsController(ChoiceOfCareerPersistence context,EventService eventService)
        {
            _context = context;
            _eventService = eventService;
        }

        // GET: api/events
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var events = await _context.Events
                .AsNoTracking()
                .Select(e => new
                {
                    e.EventId,
                    e.EventName,
                    e.EventDate,
                    e.RegistrationStart,
                    e.RegistrationEnd,
                    e.IsRegistrationEnabled
                })
                .ToListAsync();

            return Ok(events.OrderByDescending(e => e.EventDate));
        }

        // GET: api/events/1
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var eventData = await _context.Events
                .AsNoTracking()
                .Where(e => e.EventId == id)
                .Select(e => new
                {
                    e.EventId,
                    e.EventName,
                    e.EventDate,
                    e.RegistrationStart,
                    e.RegistrationEnd,
                    e.IsRegistrationEnabled,

                    TimeSlots = e.TimeSlots.Select(t => new
                    {
                        t.TimeSlotId,
                        t.TimeSlotName,
                        t.BeginDate,
                        t.EndDate
                    }).ToList()
                })
                .FirstOrDefaultAsync();

            if (eventData == null)
                return NotFound(new
                {
                    Message = "Az esemény nem található."
                });

            return Ok(eventData);
        }

        // POST: api/events
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult<CreatedEventDto>> Create(
            [FromBody] CreateEventDto dto)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            try
            {
                var result = await _eventService.CreateAsync(dto);

                return CreatedAtAction(
                    nameof(GetById),
                    new { id = result.EventId },
                    result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    Message = ex.Message
                });
            }
        }
    }
}
