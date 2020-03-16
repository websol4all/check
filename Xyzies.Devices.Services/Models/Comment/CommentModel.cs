using System;
using System.Collections.Generic;
using System.Text;

namespace Xyzies.Devices.Services.Models.Comment
{
    /// <summary>
    /// Comment Model
    /// </summary>
    public class CommentModel
    {
        /// <summary>
        /// Id
        /// </summary>
        public Guid Id { get; set; }
        /// <summary>
        /// Date of create
        /// </summary>
        public DateTime CreateOn { get; set; }

        /// <summary>
        /// User id who create
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// User name who create
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// Comment
        /// </summary>
        public string Comment { get; set; }
    }
}
