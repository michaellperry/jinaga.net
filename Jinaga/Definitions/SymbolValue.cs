using System;
using System.Collections.Immutable;

namespace Jinaga.Definitions
{
    public abstract class SymbolValue
    {
    }

    public class SymbolValueComposite : SymbolValue
    {
        public ImmutableDictionary<string, SymbolValue> Fields { get; }

        public SymbolValueComposite(ImmutableDictionary<string, SymbolValue> fields)
        {
            Fields = fields;
        }

        public SymbolValue GetField(string name)
        {
            if (Fields.TryGetValue(name, out var memberSet))
            {
                return memberSet;
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public ProjectionDefinition CreateProjectionDefinition()
        {
            return new ProjectionDefinition(Fields);
        }
    }

    public class SymbolValueSetDefinition : SymbolValue
    {
        public SetDefinition SetDefinition { get; }

        public SymbolValueSetDefinition(SetDefinition setDefinition)
        {
            SetDefinition = setDefinition;
        }
    }
}
