﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Xyzies.Devices.Services.Models.EmailModel
{
    public class EmailMessage
    {
        public string From { get; set; }
        public string To { get; set; }
        public string Subject { get; set; }
        public string Message { get; set; }

    }
}