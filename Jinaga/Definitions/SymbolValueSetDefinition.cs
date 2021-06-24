namespace Jinaga.Definitions
{
    public class SymbolValueSetDefinition : SymbolValue
    {
        public SetDefinition SetDefinition { get; }

        public SymbolValueSetDefinition(SetDefinition setDefinition)
        {
            SetDefinition = setDefinition;
        }

        public override SymbolValue WithSteps(StepsDefinition steps)
        {
            return new SymbolValueSetDefinition(SetDefinition.WithSteps(steps));
        }

        public override SymbolValue WithCondition(ConditionDefinition conditionDefinition)
        {
            return new SymbolValueSetDefinition(SetDefinition.WithCondition(conditionDefinition));
        }
    }
}
