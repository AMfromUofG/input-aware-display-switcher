using InputAwareDisplaySwitcher.Core.Domain.Switching;

namespace InputAwareDisplaySwitcher.Core.Application;

public interface IDecisionEngine
{
    SwitchDecision Evaluate(DecisionRequest request);
}
