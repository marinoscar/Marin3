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
        private readonly AppDataContext _dataContext;
        private readonly string _environment;

        public DbConfigurationSource(AppDataContext dataContext, string environment)
        {
            _dataContext = dataContext ?? throw new ArgumentNullException(nameof(dataContext));
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        }

        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return new DbConfigurationProvider(_dataContext, _environment);
        }
    }
}
