using System;
using System.Linq;
using FluentAssertions;
using Jinaga.Test.Model;
using Jinaga.Test.Model.Order;
using Xunit;

namespace Jinaga.Test
{
    public class SpecificationTest
    {
        [Fact]
        public void CanSpecifySuccessors()
        {
            Specification<Airline, Flight> specification = Given<Airline>.Match((airline, facts) =>
                from flight in facts.OfType<Flight>()
                where flight.airlineDay.airline == airline
                select flight
            );
            string descriptiveString = specification.ToDescriptiveString();
            descriptiveString.Should().Be(@"(airline: Skylane.Airline) {
    flight: Skylane.Flight [
        flight->airlineDay: Skylane.Airline.Day->airline: Skylane.Airline = airline
    ]
}
".Replace("\r", ""));
        }

        [Fact]
        public void Specification_MissingJoin()
        {
            Func<Specification<Airline, Flight>> constructor = () =>
                Given<Airline>.Match((airline, facts) =>
                    from flight in facts.OfType<Flight>()
                    select flight
                );
            constructor.Should().Throw<SpecificationException>()
                .WithMessage(
                    "The variable \"flight\" should be joined to the parameter \"airline\". " +
                    "Consider \"where flight.airlineDay.airline == airline\".");
        }

        [Fact]
        public void Specification_MissingCollectionJoin()
        {
            Func<Specification<Item, Order>> constructor = () =>
                Given<Item>.Match((item, facts) =>
                    from order in facts.OfType<Order>()
                    select order
                );
            constructor.Should().Throw<SpecificationException>()
                .WithMessage(
                    "The variable \"order\" should be joined to the parameter \"item\". " +
                    "Consider \"where order.items.Contains(item)\"."
                );
        }

        [Fact]
        public void Specification_MissingCollectionJoinWithExtension()
        {
            Func<Specification<Item, Order>> constructor = () =>
                Given<Item>.Match((item, facts) =>
                    facts.OfType<Order>()
                );
            constructor.Should().Throw<SpecificationException>()
                .WithMessage(
                    "The set should be joined to the parameter \"item\". " +
                    "Consider \"facts.OfType<Order>(order => order.items.Contains(item))\"."
                );
        }

        [Fact]
        public void Specification_MissingSuccessorCollectionJoin()
        {
            Func<Specification<Order, Item>> constructor = () =>
                Given<Order>.Match((order, facts) =>
                    from item in facts.OfType<Item>()
                    select item
                );
            constructor.Should().Throw<SpecificationException>()
                .WithMessage(
                    "The variable \"item\" should be joined to the parameter \"order\". " +
                    "Consider \"where order.items.Contains(item)\"."
                );
        }

        [Fact]
        public void Specification_MissingSuccessorCollectionJoinWithExtension()
        {
            Func<Specification<Order, Item>> constructor = () =>
                Given<Order>.Match((order, facts) =>
                    facts.OfType<Item>()
                );
            constructor.Should().Throw<SpecificationException>()
                .WithMessage(
                    "The set should be joined to the parameter \"order\". " +
                    "Consider \"facts.OfType<Item>(item => order.items.Contains(item))\"."
                );
        }

        [Fact]
        public void Specification_MissingJoinWithExtensionMethod()
        {
            Func<Specification<Airline, Flight>> constructor = () =>
                Given<Airline>.Match((airline, facts) => facts.OfType<Flight>());
            constructor.Should().Throw<SpecificationException>()
                .WithMessage(
                    "The set should be joined to the parameter \"airline\". " +
                    "Consider \"facts.OfType<Flight>(flight => flight.airlineDay.airline == airline)\".");
        }

        [Fact]
        public void CanSpecifyShortSuccessors()
        {
            Specification<Airline, Flight> specification = Given<Airline>.Match((airline, facts) =>
                facts.OfType<Flight>(flight => flight.airlineDay.airline == airline)
            );
            string descriptiveString = specification.ToDescriptiveString();
            descriptiveString.Should().Be(@"(airline: Skylane.Airline) {
    flight: Skylane.Flight [
        flight->airlineDay: Skylane.Airline.Day->airline: Skylane.Airline = airline
    ]
}
".Replace("\r", ""));
        }

        [Fact]
        public void CanSpecifyPredecessors()
        {
            Specification<FlightCancellation, Flight> specification = Given<FlightCancellation>.Match((flightCancellation, facts) =>
                from flight in facts.OfType<Flight>()
                where flightCancellation.flight == flight
                select flight
            );
            string descriptiveString = specification.ToDescriptiveString();
            descriptiveString.Should().Be(@"(flightCancellation: Skylane.Flight.Cancellation) {
    flight: Skylane.Flight [
        flight = flightCancellation->flight: Skylane.Flight
    ]
}
".Replace("\r", ""));
        }

        [Fact]
        public void CanSpecifyPredecessorsShorthand()
        {
            Specification<FlightCancellation, Flight> specification = Given<FlightCancellation>.Match(flightCancellation =>
                flightCancellation.flight
            );
            string descriptiveString = specification.ToDescriptiveString();
            descriptiveString.Should().Be(@"(flightCancellation: Skylane.Flight.Cancellation) {
    flight: Skylane.Flight [
        flight = flightCancellation->flight: Skylane.Flight
    ]
}
".Replace("\r", ""));
        }

        [Fact]
        public void CanSpecifyNegativeExistentialConditions()
        {
            Specification<AirlineDay, Flight> activeFlights = Given<AirlineDay>.Match((airlineDay, facts) =>
                from flight in facts.OfType<Flight>()
                where flight.airlineDay == airlineDay

                where !(
                    from cancellation in facts.OfType<FlightCancellation>()
                    where cancellation.flight == flight
                    select cancellation
                ).Any()

                select flight
            );
            var descriptiveString = activeFlights.ToDescriptiveString();
            descriptiveString.Should().Be(@"(airlineDay: Skylane.Airline.Day) {
    flight: Skylane.Flight [
        flight->airlineDay: Skylane.Airline.Day = airlineDay
        !E {
            cancellation: Skylane.Flight.Cancellation [
                cancellation->flight: Skylane.Flight = flight
            ]
        }
    ]
}
".Replace("\r", ""));
        }

        [Fact]
        public void CanSpecifyNamedNegativeExistentialConditions()
        {
            Specification<AirlineDay, Flight> activeFlights = Given<AirlineDay>.Match((airlineDay, facts) =>
                from flight in facts.OfType<Flight>()
                where flight.airlineDay == airlineDay

                where !flight.IsCancelled

                select flight
            );
            var descriptiveString = activeFlights.ToDescriptiveString();
            descriptiveString.Should().Be(@"(airlineDay: Skylane.Airline.Day) {
    flight: Skylane.Flight [
        flight->airlineDay: Skylane.Airline.Day = airlineDay
        !E {
            cancellation: Skylane.Flight.Cancellation [
                cancellation->flight: Skylane.Flight = flight
            ]
        }
    ]
}
".Replace("\r", ""));
        }

        [Fact]
        public void CanSpecifyNamedShortNegativeExistentialConditions()
        {
            Specification<AirlineDay, Flight> activeFlights = Given<AirlineDay>.Match((airlineDay, facts) =>
                from flight in facts.OfType<Flight>()
                where flight.airlineDay == airlineDay

                where !flight.ShortIsCancelled

                select flight
            );
            var descriptiveString = activeFlights.ToDescriptiveString();
            descriptiveString.Should().Be(@"(airlineDay: Skylane.Airline.Day) {
    flight: Skylane.Flight [
        flight->airlineDay: Skylane.Airline.Day = airlineDay
        !E {
            cancellation: Skylane.Flight.Cancellation [
                cancellation->flight: Skylane.Flight = flight
            ]
        }
    ]
}
".Replace("\r", ""));
        }

        [Fact]
        public void CanSpecifyPositiveExistentialCondition()
        {
            Specification<Airline, Booking> bookingsToRefund = Given<Airline>.Match((airline, facts) =>
                from flight in facts.OfType<Flight>()
                where flight.airlineDay.airline == airline

                where (
                    from cancellation in facts.OfType<FlightCancellation>()
                    where cancellation.flight == flight
                    select cancellation
                ).Any()

                from booking in facts.OfType<Booking>()
                where booking.flight == flight

                where !(
                    from refund in facts.OfType<Refund>()
                    where refund.booking == booking
                    select refund
                ).Any()

                select booking
            );
            var descriptiveString = bookingsToRefund.ToDescriptiveString();
            descriptiveString.Should().Be(@"(airline: Skylane.Airline) {
    flight: Skylane.Flight [
        flight->airlineDay: Skylane.Airline.Day->airline: Skylane.Airline = airline
        E {
            cancellation: Skylane.Flight.Cancellation [
                cancellation->flight: Skylane.Flight = flight
            ]
        }
    ]
    booking: Skylane.Booking [
        booking->flight: Skylane.Flight = flight
        !E {
            refund: Skylane.Refund [
                refund->booking: Skylane.Booking = booking
            ]
        }
    ]
}
".Replace("\r", ""));
        }

        [Fact]
        public void Specification_MissingJoinToPriorPath()
        {
            Func<Specification<Airline, Booking>> bookingsToRefund = () => Given<Airline>.Match((airline, facts) =>
                from flight in facts.OfType<Flight>()
                where flight.airlineDay.airline == airline
                from booking in facts.OfType<Booking>()
                select booking
            );

            bookingsToRefund.Should().Throw<SpecificationException>().WithMessage(
                "The variable \"booking\" should be joined to the prior variable \"flight\". " +
                "Consider \"where booking.flight == flight\"."
            );
        }

        [Fact]
        public void CanSpecifyNamedPositiveExistentialCondition()
        {
            Specification<Airline, Booking> bookingsToRefund = Given<Airline>.Match((airline, facts) =>
                from flight in facts.OfType<Flight>()
                where flight.airlineDay.airline == airline

                where flight.IsCancelled

                from booking in facts.OfType<Booking>()
                where booking.flight == flight

                where !(
                    from refund in facts.OfType<Refund>()
                    where refund.booking == booking
                    select refund
                ).Any()

                select booking
            );
            var descriptiveString = bookingsToRefund.ToDescriptiveString();
            descriptiveString.Should().Be(@"(airline: Skylane.Airline) {
    flight: Skylane.Flight [
        flight->airlineDay: Skylane.Airline.Day->airline: Skylane.Airline = airline
        E {
            cancellation: Skylane.Flight.Cancellation [
                cancellation->flight: Skylane.Flight = flight
            ]
        }
    ]
    booking: Skylane.Booking [
        booking->flight: Skylane.Flight = flight
        !E {
            refund: Skylane.Refund [
                refund->booking: Skylane.Booking = booking
            ]
        }
    ]
}
".Replace("\r", ""));
        }

        [Fact]
        public void CanSpecifyProjection()
        {
            var bookingsToRefund = Given<Airline>.Match((airline, facts) =>
                from flight in facts.OfType<Flight>()
                where flight.airlineDay.airline == airline

                from cancellation in facts.OfType<FlightCancellation>()
                where cancellation.flight == flight

                from booking in facts.OfType<Booking>()
                where booking.flight == flight

                where !(
                    from refund in facts.OfType<Refund>()
                    where refund.booking == booking
                    select refund
                ).Any()

                select new
                {
                    Booking = booking,
                    Cancellation = cancellation
                }
            );
            var descriptiveString = bookingsToRefund.ToDescriptiveString();
            descriptiveString.Should().Be(@"(airline: Skylane.Airline) {
    flight: Skylane.Flight [
        flight->airlineDay: Skylane.Airline.Day->airline: Skylane.Airline = airline
    ]
    cancellation: Skylane.Flight.Cancellation [
        cancellation->flight: Skylane.Flight = flight
    ]
    booking: Skylane.Booking [
        booking->flight: Skylane.Flight = flight
        !E {
            refund: Skylane.Refund [
                refund->booking: Skylane.Booking = booking
            ]
        }
    ]
}
".Replace("\r", ""));
        }

        [Fact]
        public void Specification_SelectPredecessor()
        {
            var passengersForAirline = Given<Flight>.Match((flight, facts) =>
                from booking in facts.OfType<Booking>()
                where booking.flight == flight
                select booking.passenger
            );

            var descriptiveString = passengersForAirline.ToDescriptiveString();
            descriptiveString.Should().Be(@"(flight: Skylane.Flight) {
    booking: Skylane.Booking [
        booking->flight: Skylane.Flight = flight
    ]
    passenger: Skylane.Passenger [
        passenger = booking->passenger: Skylane.Passenger
    ]
}
".Replace("\r", ""));
        }

        [Fact]
        public void Specification_JoinWithTwoConditionals()
        {
            var specification = Given<Company>.Match((company, facts) =>
                from office in facts.OfType<Office>()
                where office.company == company
                where !office.IsClosed

                from headcount in facts.OfType<Headcount>()
                where headcount.office == office
                where headcount.IsCurrent

                select new
                {
                    office,
                    headcount
                }
            );

            var descriptiveString = specification.ToDescriptiveString();
            descriptiveString.Should().Be(@"(company: Corporate.Company) {
    office: Corporate.Office [
        office->company: Corporate.Company = company
        !E {
            closure: Corporate.Office.Closure [
                closure->office: Corporate.Office = office
            ]
        }
    ]
    headcount: Corporate.Headcount [
        headcount->office: Corporate.Office = office
        !E {
            next: Corporate.Headcount [
                next->prior: Corporate.Headcount = headcount
            ]
        }
    ]
}
".Replace("\r", ""));
        }
    }
}
