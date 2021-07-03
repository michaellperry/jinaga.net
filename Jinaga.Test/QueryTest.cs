using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Jinaga.Test.Model;
using Jinaga.UnitTest;
using Xunit;

namespace Jinaga.Test
{
    public class QueryTest
    {
        [Fact]
        public async Task CanQueryForPredecessors()
        {
            var j = JinagaTest.Create();
            var flight = await j.Fact(new Flight(new AirlineDay(new Airline("IA"), DateTime.Today), 4272));
            var cancellation = await j.Fact(new FlightCancellation(flight, DateTime.UtcNow));

            var specification = Given<FlightCancellation>.Match(
                flightCancellation => flightCancellation.flight
            );
            var flights = await j.Query(cancellation, specification);

            flights.Should().ContainSingle().Which.Should().BeEquivalentTo(flight);
        }
    }
}