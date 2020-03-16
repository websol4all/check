using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Xyzies.Devices.Services.Service.Interfaces
{
    public interface ISendAlertEmailService<T>
    {
        Task<string> SendAsync(T email);
    }
}
