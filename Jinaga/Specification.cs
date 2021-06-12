using System.Collections.Immutable;
using System;
using System.Linq;
using System.Linq.Expressions;
using Jinaga.Parsers;
using Jinaga.Pipelines;
using Jinaga.Repository;

namespace Jinaga
{
    public static class Given<TFact>
    {
        public static Specification<TFact, TProjection> Match<TProjection>(Expression<Func<TFact, FactRepository, IQueryable<TProjection>>> spec)
        {
            var body = spec.Body;
            var parameter = spec.Parameters[0];
            var parameterName = parameter.Name;
            var parameterType = parameter.Type;
            string parameterTypeName = parameterType.FactTypeName();

            var initialFactName = parameterName;
            var initialFactType = parameterTypeName;

            var specificationBodyVisitor = new SpecificationBodyVisitor();
            specificationBodyVisitor.Visit(body);
            var paths = specificationBodyVisitor.Paths;
            var projection = specificationBodyVisitor.Projection;

            var pipeline = new Pipeline(initialFactName, initialFactType, paths, projection);
            return new Specification<TFact, TProjection>(pipeline);
        }

        public static Specification<TFact, TProjection> Match<TProjection>(Expression<Func<TFact, FactRepository, TProjection>> spec)
        {
            throw new NotImplementedException();
        }
        
        public static Specification<TFact, TProjection> Match<TProjection>(Expression<Func<TFact, TProjection>> spec)
        {
            var segmentVisitor = new SegmentVisitor();
            segmentVisitor.Visit(spec.Body);
            var steps = segmentVisitor.Steps;
            var first = steps.First();
            var last = steps.Last();
            var initialType = first.InitialType;
            var targetType = last.TargetType;
            if (last is PredecessorStep predecessorStep)
            {
                string targetName = predecessorStep.Role;
                string rootName = segmentVisitor.RootName;
                var path = new Path(targetName, targetType, rootName, steps);
                var pipeline = new Pipeline(
                    rootName,
                    initialType,
                    ImmutableList<Path>.Empty.Add(path),
                    new Projection(targetName));
                return new Specification<TFact, TProjection>(pipeline);
            }
            else
            {
                throw new NotImplementedException();
            }
        }
    }
    public class Specification<TFact, TProjection>
    {
        private Pipeline pipeline;

        public Specification(Pipeline pipeline)
        {
            this.pipeline = pipeline;
        }

        public Pipeline Compile()
        {
            return this.pipeline;
        }
    }
}
