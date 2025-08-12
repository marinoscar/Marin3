using MarinApp.Core.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarinApp.Core.Configuration
{
    public class DbConfigurationSource : IConfigurationSource
    {
        private readonly IDbContextFactory<Data.AppDataContext> _contextFactory;
        private readonly string _environment;

        public DbConfigurationSource(IDbContextFactory<Data.AppDataContext> contextFactory, string environment)
        {
            _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        }

        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return new DbConfigurationProvider(_contextFactory, _environment);
        }
    }
}
