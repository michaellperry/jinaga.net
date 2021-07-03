using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Jinaga.Facts;
using Jinaga.Services;

namespace Jinaga
{
    public class Jinaga
    {
        private readonly IStore store;

        public Jinaga(IStore store)
        {
            this.store = store;
        }

        public async Task<T> Fact<T>(T prototype)
        {
            var newFacts = FactSerializer.Serialize(prototype);
            var added = await store.Save(newFacts);
            return prototype;
        }

        public async Task<ImmutableList<TProjection>> Query<TFact, TProjection>(TFact start, Specification<TFact, TProjection> specification)
        {
            return ImmutableList<TProjection>.Empty;
        }
    }
}