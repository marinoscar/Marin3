using AutoGen;

namespace MarinApp.DemoAgent
{
    public class RequirementsAgent(
    AgentId id,
    IAgentRuntime runtime,
    ) :
        BaseAgent(id, runtime, "MyAgent", null),
    {

    }
}
