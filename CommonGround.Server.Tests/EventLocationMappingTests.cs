using CommonGround.Server.Events;

namespace CommonGround.Server.Tests;

public class EventLocationMappingTests
{
    private static EventbriteEvent EventWithVenue(EventbriteVenue? venue) => new(
        Id: "e1",
        Name: new EventbriteText("Workshop"),
        Summary: "A workshop",
        Description: null,
        Url: "https://example.test/e1",
        Start: new EventbriteDateTime(new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc)),
        End: null,
        Logo: null,
        Venue: venue);

    [Fact]
    public void Eventbrite_location_prefers_the_venue_name()
    {
        var dto = EventsMapping.ToUpcomingDto(
            EventWithVenue(new EventbriteVenue("Seaford Hall", new EventbriteAddress("1 Main St, Seaford SA"))), 0);
        Assert.Equal("Seaford Hall", dto!.Location);
    }

    [Fact]
    public void Eventbrite_location_falls_back_to_the_address_when_name_is_blank()
    {
        var dto = EventsMapping.ToUpcomingDto(
            EventWithVenue(new EventbriteVenue("  ", new EventbriteAddress("1 Main St, Seaford SA"))), 0);
        Assert.Equal("1 Main St, Seaford SA", dto!.Location);
    }

    [Fact]
    public void Eventbrite_location_is_null_when_venue_has_neither_name_nor_address()
    {
        var dto = EventsMapping.ToUpcomingDto(
            EventWithVenue(new EventbriteVenue(null, new EventbriteAddress(null))), 0);
        Assert.Null(dto!.Location);
    }

    [Fact]
    public void Eventbrite_location_is_null_when_there_is_no_venue()
    {
        var dto = EventsMapping.ToUpcomingDto(EventWithVenue(null), 0);
        Assert.Null(dto!.Location);
    }
}
