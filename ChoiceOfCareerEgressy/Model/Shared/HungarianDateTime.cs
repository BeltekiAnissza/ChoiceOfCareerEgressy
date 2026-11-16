using System;

namespace ChoiceOfCareerEgressy.Model.Shared
{
    /// <summary>
    /// A webes űrlap és az Excel időpontjai magyar helyi időpontok.
    /// PostgreSQL timestamptz mezőbe kizárólag UTC kerül.
    /// </summary>
    public static class HungarianDateTime
    {
        private static readonly TimeZoneInfo Zone = GetZone();

        private static TimeZoneInfo GetZone()
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Europe/Budapest");
            }
            catch (TimeZoneNotFoundException)
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Central Europe Standard Time");
            }
        }

        public static DateTimeOffset ToUtc(DateTime localValue)
        {
            // A datetime-local / Excel nem hordoz időzóna-információt.
            var local = DateTime.SpecifyKind(localValue, DateTimeKind.Unspecified);
            if (Zone.IsInvalidTime(local) || Zone.IsAmbiguousTime(local))
                throw new ArgumentException(
                    $"Nem létező vagy nem egyértelmű magyar időpont: {local:yyyy.MM.dd HH:mm}.");

            return new DateTimeOffset(local, Zone.GetUtcOffset(local)).ToUniversalTime();
        }

        public static DateTimeOffset ToLocal(DateTimeOffset instant)
            => TimeZoneInfo.ConvertTime(instant, Zone);

        public static DateTime LocalDate(DateTimeOffset instant)
            => ToLocal(instant).Date;
    }
}
