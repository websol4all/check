using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;

namespace Xyzies.Devices.Data.Entity.EntityConfigurations
{
    public class DeviceConfiguration : IEntityTypeConfiguration<Device>
    {
        public DeviceConfiguration()
        {
        }
        public void Configure(EntityTypeBuilder<Device> deviceBuilder)
        {
            deviceBuilder.HasIndex(x => x.Udid).IsUnique();
        }
    }
}
