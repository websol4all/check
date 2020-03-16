using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Xyzies.Devices.Data.Entity.EntityConfigurations
{

    public class DeviceHistoryConfiguration : IEntityTypeConfiguration<DeviceHistory>
    {
        public void Configure(EntityTypeBuilder<DeviceHistory> deviceHistoryBuilder)
        {
            deviceHistoryBuilder.HasKey(x => x.Id).HasName("PK_DeviceHistory");
            deviceHistoryBuilder.HasIndex(x => x.DeviceId);
            deviceHistoryBuilder.HasIndex(x => new { x.DeviceId, x.CreatedOn });
        }
    }
}
