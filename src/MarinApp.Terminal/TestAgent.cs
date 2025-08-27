using MarinApp.Agents;
using MarinApp.Agents.Data;
using MarinApp.Agents.Orchestration;
using MarinApp.Core.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarinApp.Terminal
{
    internal class TestAgent
    {
        private readonly IConfiguration _configuration;

        public TestAgent(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public void Run()
        {
            Console.Clear();

            var logFactory = LoggerFactory.Create(builder => builder.AddEventLog().SetMinimumLevel(LogLevel.Error));
            //var storageContext = AgentDataContext.CreateNpgsql(DbConnectionStringHelper.GetConnectionString());
            var storageContext = AgentDataContext.CreateInMemory();
            storageContext.InitializeDb();
            var storageService = new AgentHistoryService(storageContext);

            var humanAgent = new ColorConsoleAgent(storageService, _configuration, logFactory);
            var aiAgent = new AIAgent(storageService, logFactory, _configuration);

            var chat = new ChatWithHuman(humanAgent, aiAgent);

            chat.StartChat("Hello how can I help you? when you are ready, just type done", (message) =>
            {
                return message.Content.Contains("done", StringComparison.OrdinalIgnoreCase);
            });
        }

        public void RunMultipleAgents()
        {
            var logFactory = LoggerFactory.Create(builder => builder.AddEventLog().SetMinimumLevel(LogLevel.Error));
            //var storageContext = AgentDataContext.CreateNpgsql(DbConnectionStringHelper.GetConnectionString());
            var storageContext = AgentDataContext.CreateInMemory();
            storageContext.InitializeDb();
            var storageService = new AgentHistoryService(storageContext);

            Console.Clear();
            var factory = new OpenAIAgentFactory(storageService, logFactory, _configuration);

            var planner = factory.Create(
  "Planner",
  "Decomposes the goal into steps, success criteria, and a short plan the other agents can execute.",
  @"
You are the Planner. No tools, LLM-only.
Objective: turn the goal into a minimal, high-leverage plan with crisp tasks, owners, and acceptance criteria.
Constraints:
- Be concrete and short (<= 12 tasks).
- Call out assumptions and open questions explicitly.
- Output format:

# PLAN
- Objective: <1 line>
- Assumptions:
  - ...
- Steps (max 12):
  1) <Task> — Owner:<AgentName> — Output:<artifact name>
  ...
- AcceptanceCriteria:
  - ...
- ProposedOrder: [Planner, Architect, Writer, Critic, Editor, Summarizer]

If information is missing, propose reasonable assumptions and proceed."
);
            var architect = factory.Create(
  "Architect",
  "Designs the outline, information architecture, and templates for the deliverable.",
  @"
You are the Architect. No tools, LLM-only.
Input: PLAN and Goal.
Your job: produce a tight outline and section templates the Writer can fill quickly.
Requirements:
- Keep the outline to 6–9 headings max.
- For each section, provide: Purpose, Key Points (bullets), and a short sentence template.
- Add a placeholder table or JSON schema if the format benefits from structure.
Output format:

# OUTLINE
<numbered headings>

# SECTION-TEMPLATES
## <Heading>
Purpose: ...
KeyPoints:
- ...
Template:
<short sentence scaffold>

# PLACEHOLDERS
- Data we don't have + assumption to use"
);
            var writer = factory.Create(
  "Writer",
  "Produces the first complete draft from the outline and templates.",
  @"
You are the Writer. No tools, LLM-only.
Style: executive, concise, benefits-first, active voice, avoid hype.
Tasks:
- Fill the Architect's templates completely.
- Keep to the requested length/format.
- Label any invented figures as Estimates and add the assumption used.
- Add a 5-bullet Executive Summary and 'Next 5 Actions'.
Output format:

# DRAFT
<full draft>

# ASSUMPTIONS
- ...

# ESTIMATES (if any)
- <item>: <value> — method: <how you derived it>"
);

            var modeler = factory.Create(
  "ValueModeler",
  "Shapes the value narrative: KPIs, benefits logic, simple ranges, and sanity checks.",
  @"
You are the Value Modeler. No tools, LLM-only.
Goal: strengthen the value story without external data.
Tasks:
- Propose 6–10 KPIs w/ definitions and how-to-measure notes.
- Provide conservative/base/aspirational ranges (qualitative or rough numeric).
- Align KPIs to exec outcomes (cost, cycle time, risk, revenue).
- Flag any value claims in the draft that lack a causal link.
Output format:

# KPIS
| KPI | Definition | HowToMeasure | Range |
|-----|------------|--------------|-------|

# VALUE-NOTES
- CausalLinks:
  - <claim> -> <mechanism> -> <metric>
- Gaps:
  - <what’s missing>"
);

            var critic = factory.Create(
  "Critic",
  "Finds gaps, risks, inconsistencies; proposes precise edits.",
  @"
You are the Critic. No tools, LLM-only.
Be tough but constructive. Focus on clarity, logical soundness, and exec readiness.
Tasks:
- Identify unclear claims, ungrounded leaps, and redundancy.
- Check alignment: Goal ↔ Outline ↔ Draft ↔ KPIs.
- Propose exact edits and rewrites (quote-before/after).
- Ensure a clear Definition of Done is met.
Output format:

# CRITIQUE
- MajorIssues:
  1) <issue> — Fix: <specific rewrite or instruction>
  ...
- MinorEdits:
  - 'before' -> 'after'

# GO/NO-GO
- Ready?: <Yes/No> — Rationale: <1-2 lines>"
);

            var editor = factory.Create(
  "Editor",
  "Applies Critic’s fixes, tightens style, ensures formatting, and produces the clean final.",
  @"
You are the Editor. No tools, LLM-only.
Tasks:
- Apply Critic’s fixes faithfully; resolve any residual inconsistencies.
- Tighten to requested length; ensure headings, tables, and bullets render cleanly.
- Produce the FINAL deliverable and a 5-bullet Executive Summary.
Output format:

# FINAL
<clean, client-ready content>

# EXECUTIVE-SUMMARY (5 bullets)
- ...

# NEXT-STEPS (5 bullets)
- ...

# CHANGELOG
- <what you changed vs. DRAFT>"
);

            var sumarizer = factory.Create(
  "Summarizer",
  "Returns a compact hand-off summary for the router or human reviewer.",
  @"
You are the Summarizer. No tools, LLM-only.
Summarize the FINAL into 10 bullets max, preserving any decisions, assumptions, and open questions.
Output format:

# HANDOFF
- Objective:
- Outcome:
- Key Decisions:
- Assumptions:
- Risks/Watchouts:
- OpenQuestions:
- Owner/NextActions:"
);

            var agentGoal = @"
Produce a client-ready deliverable on a specified topic using only reasoning (no external tools). 
The deliverable must follow the requested format, satisfy clear acceptance criteria, state all assumptions, 
and finish with a 5-bullet Executive Summary and 5 Next Steps.

The agents should:
1. Break the goal into steps and success criteria.
2. Design an outline and structure for the deliverable.
3. Draft full content aligned to the outline.
4. Strengthen the value story by identifying KPIs and causal links.
5. Critique the draft for clarity, logic, and readiness.
6. Apply edits to create a polished, final version.
7. Summarize the outcome, assumptions, and open questions for hand-off.

Constraints:
- Be concise, executive-level, and benefits-first.
- Clearly mark assumptions and invented values.
- Ensure logical flow from goal → plan → outline → draft → final.
- Output must be directly usable by a human reviewer.
";

            var human = new ColorConsoleAgent(storageService, _configuration, logFactory);
            var router = new RouterAgent(storageService, logFactory, _configuration);
            
            router.InitializeAgents(human, planner, architect, writer, modeler, critic, editor, sumarizer);
            Console.Clear();
            Console.WriteLine("Welcome to the Multi-Agent System. Type 'exit' to quit.");

            router.SetGoalAsync(agentGoal).GetAwaiter().GetResult();

        }
    }

    internal class AIAgent : OpenAIAgentBase
    {
        public AIAgent(IAgentHistoryService agentHistoryService, ILoggerFactory loggerFactory, IConfiguration configuration) : base(agentHistoryService, loggerFactory, configuration)
        {
            SetAgentDetails("openai-agent", "OpenAI Agent", "An AI agent that uses OpenAI's GPT models to generate responses.");
            SetSystemMessage("You are a helpful assistant.");
        }
    }
}
