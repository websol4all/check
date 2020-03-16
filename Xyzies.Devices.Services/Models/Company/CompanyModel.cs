using System.Collections.Generic;

namespace Xyzies.Devices.Services.Models.Company
{
    /// <summary>
    /// Company model
    /// </summary>
    public class CompanyModel
    {
        /// <summary>
        /// Id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Company name
        /// </summary>
        public string CompanyName { get; set; }

        public IEnumerable<string> UserId { get; set; }

        public IEnumerable<string> Role { get; set; }
    }
}
