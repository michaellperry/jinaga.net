﻿using FluentAssertions;
using Jinaga.UnitTest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Jinaga.Test
{
    public class WatchTest
    {
        private readonly Jinaga j;
        private readonly FakeRepository<Office> officeRepository;

        public WatchTest()
        {
            j = JinagaTest.Create();
            officeRepository = new FakeRepository<Office>();
        }

        [Fact]
        public async Task Watch_NoResults()
        {
            var company = await j.Fact(new Company("Contoso"));

            var officeObserver = j.Watch(company, officesInCompany, obs => obs
                .OnAdded(async office => await officeRepository.Insert(office))
            );

            officeObserver.Stop();

            officeRepository.Items.Should().BeEmpty();
        }

        [Fact]
        public async Task Watch_AlreadyExists()
        {
            var company = await j.Fact(new Company("Contoso"));
            var newOffice = await j.Fact(new Office(company, new City("Dallas")));

            var officeObserver = j.Watch(company, officesInCompany, obs => obs
                .OnAdded(async office => await officeRepository.Insert(office))
            );

            officeObserver.Stop();

            officeRepository.Items.Should().ContainSingle().Which.Should().BeEquivalentTo(newOffice);
        }

        [Fact]
        public async Task Watch_Added()
        {
            var company = await j.Fact(new Company("Contoso"));

            var officeObserver = j.Watch(company, officesInCompany, obs => obs
                .OnAdded(async office => await officeRepository.Insert(office))
            );

            var newOffice = await j.Fact(new Office(company, new City("Dallas")));
            officeObserver.Stop();

            officeRepository.Items.Should().ContainSingle().Which.Should().BeEquivalentTo(newOffice);
        }

        [Fact]
        public async Task Watch_ExistingRemoved()
        {
            var company = await j.Fact(new Company("Contoso"));
            var newOffice = await j.Fact(new Office(company, new City("Dallas")));

            var officeObserver = j.Watch(company, officesInCompany, obs => obs
                .OnAdded(async office => await officeRepository.Insert(office))
                .OnRemoved(async id => await officeRepository.Delete(id))
            );

            await j.Fact(new OfficeClosure(newOffice, DateTime.Now));
            officeObserver.Stop();

            officeRepository.Items.Should().BeEmpty();
        }

        [Fact]
        public async Task Watch_NewRemoved()
        {
            var company = await j.Fact(new Company("Contoso"));

            var officeObserver = j.Watch(company, officesInCompany, obs => obs
                .OnAdded(async office => await officeRepository.Insert(office))
                .OnRemoved(async id => await officeRepository.Delete(id))
            );

            var newOffice = await j.Fact(new Office(company, new City("Dallas")));
            await j.Fact(new OfficeClosure(newOffice, DateTime.Now));
            officeObserver.Stop();

            officeRepository.Items.Should().BeEmpty();
        }

        private static Specification<Company, Office> officesInCompany = Given<Company>.Match((company, facts) =>
            from office in facts.OfType<Office>()
            where office.company == company
            where !office.IsClosed

            select office
        );

        class FakeRepository<T>
        {
            private int nextId = 1;
            private readonly Dictionary<int, T> items = new Dictionary<int, T>();

            public IEnumerable<T> Items => items.Values;

            public Task<int> Insert(T item)
            {
                int id = nextId++;
                items.Add(id, item);
                return Task.FromResult(id);
            }

            public Task Delete(int id)
            {
                items.Remove(id);
                return Task.CompletedTask;
            }
        }
    }
}
