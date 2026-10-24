using ChoiceOfCareerEgressy.Dto.ProgramDto;
using ChoiceOfCareerEgressy.Persistence;

namespace ChoiceOfCareerEgressy.Model.Admin
{
    public class EventService
    {
        private readonly ChoiceOfCareerPersistence _context;

        public EventService(ChoiceOfCareerPersistence context)
        {
            _context = context;
        }

        private static DateTimeOffset ToHungarianTime(DateTime value)
        {
            var zone = TimeZoneInfo.FindSystemTimeZoneById(
                "Europe/Budapest");

            var local = DateTime.SpecifyKind(
                value, DateTimeKind.Unspecified);

            if (zone.IsInvalidTime(local) ||
                zone.IsAmbiguousTime(local))
            {
                throw new ArgumentException(
                    "Az egyik időpont nem létezik, vagy nem egyértelmű.");
            }

            return new DateTimeOffset(
                local, zone.GetUtcOffset(local));
        }

        public async Task<CreatedEventDto> CreateAsync(
            CreateEventDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.EventName))
                throw new ArgumentException(
                    "Az esemény neve kötelező.");

            if (dto.RegistrationStart >= dto.RegistrationEnd)
                throw new ArgumentException(
                    "A jelentkezés vége későbbi kell legyen, mint a kezdete.");

            if (dto.FirstSlotStart >= dto.FirstSlotEnd ||
                dto.SecondSlotStart >= dto.SecondSlotEnd)
            {
                throw new ArgumentException(
                    "Az idősávok kezdete korábbi kell legyen, mint a vége.");
            }

            var eventDate = dto.EventDate.Date;

            if (dto.FirstSlotStart.Date != eventDate ||
                dto.FirstSlotEnd.Date != eventDate ||
                dto.SecondSlotStart.Date != eventDate ||
                dto.SecondSlotEnd.Date != eventDate)
            {
                throw new ArgumentException(
                    "Mindkét idősávnak az esemény napjára kell esnie.");
            }

            if (dto.FirstSlotStart < dto.SecondSlotEnd &&
                dto.SecondSlotStart < dto.FirstSlotEnd)
            {
                throw new ArgumentException(
                    "A két idősáv nem fedheti egymást.");
            }

            var registrationStart =
                ToHungarianTime(dto.RegistrationStart);

            var registrationEnd =
                ToHungarianTime(dto.RegistrationEnd);

            var firstStart =
                ToHungarianTime(dto.FirstSlotStart);

            var firstEnd =
                ToHungarianTime(dto.FirstSlotEnd);

            var secondStart =
                ToHungarianTime(dto.SecondSlotStart);

            var secondEnd =
                ToHungarianTime(dto.SecondSlotEnd);

            if (registrationEnd > firstStart ||
                registrationEnd > secondStart)
            {
                throw new ArgumentException(
                    "A jelentkezési időszaknak az esemény előtt le kell zárulnia.");
            }

            var newEvent = new Event
            {
                EventName = dto.EventName.Trim(),
                EventDate = ToHungarianTime(eventDate),
                RegistrationStart = registrationStart,
                RegistrationEnd = registrationEnd,
                IsRegistrationEnabled = dto.IsRegistrationEnabled
            };

            newEvent.TimeSlots.Add(new TimeSlot
            {
                TimeSlotName = "1. idősáv",
                BeginDate = firstStart,
                EndDate = firstEnd
            });

            newEvent.TimeSlots.Add(new TimeSlot
            {
                TimeSlotName = "2. idősáv",
                BeginDate = secondStart,
                EndDate = secondEnd
            });

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            _context.Events.Add(newEvent);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return new CreatedEventDto
            {
                EventId = newEvent.EventId,
                EventName = newEvent.EventName,
                EventDate = newEvent.EventDate
            };
        }
    }
}
