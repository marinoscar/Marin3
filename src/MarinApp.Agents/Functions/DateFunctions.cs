using Microsoft.SemanticKernel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarinApp.Agents.Functions
{   
    public class DateFunctions
    {
        [KernelFunction, Description("Returns the current local date and time as a DateTime object.")]
        public DateTime GetDateTime() { return DateTime.Now; }

        [KernelFunction, Description("Returns the current UTC date and time as a DateTime object.")]
        public DateTime GetDateTimeUTC() { return DateTime.UtcNow; }

        [KernelFunction, Description("Adds the specified number of days to the current local date and time and returns the resulting DateTime.")]
        public DateTime AddDays(int daysToAdd) { return GetDateTime().AddDays(daysToAdd); }

        [KernelFunction, Description("Calculates the time interval between two DateTime values. Throws an exception if the start date is after the end date.")]
        public TimeSpan SubstractDates(DateTime startDate, DateTime endDate)
        {
            if (startDate > endDate) throw new ArgumentException($"{nameof(startDate)} must be earlier than {nameof(startDate)}");
            return endDate.Subtract(startDate);
        }
    }
}
