using MarinApp.Agents.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarinApp.Terminal
{
    internal class TestAgent
    {
        public static void Run()
        {
            var storageContext = AgentDataContext.CreateInMemory();
            var storageService = new MarinApp.Agents.Data.AgentHistoryService();

        }
    }
}
