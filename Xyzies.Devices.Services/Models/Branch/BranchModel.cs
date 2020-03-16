using System;
using System.Collections.Generic;

namespace Xyzies.Devices.Services.Models.Branch
{
    /// <summary>
    /// Branch model
    /// </summary>
    public class BranchModel
    {
        /// <summary>
        /// Id
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// CompanyId
        /// </summary>
        public int? CompanyId { get; set; }

        /// <summary>
        /// Branch name
        /// </summary>
        public string BranchName { get; set; }

        public string Email { get; set; }

        public string Address { get; set; }

        public string City { get; set; }

        public string ZipCode { get; set; }

        public string State { get; set; }

        public List<Guid> UserId { get; set; }
    }
}