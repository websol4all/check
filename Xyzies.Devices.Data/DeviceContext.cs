using Microsoft.EntityFrameworkCore;
using System;
using Xyzies.Devices.Data.Entity;
using Xyzies.Devices.Data.Entity.EntityConfigurations;

namespace Xyzies.Devices.Data
{
    public class DeviceContext : DbContext
    {
        public DeviceContext(DbContextOptions<DeviceContext> options) : base(options)
        {
        }

        #region Entities

        public DbSet<Device> Devices { get; set; }

        public DbSet<DeviceHistory> DeviceHistory { get; set; }

        public DbSet<Comment> Comments { get; set; }

        public DbSet<Log> Logs { get; set; }


        #endregion

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new DeviceHistoryConfiguration());
            modelBuilder.ApplyConfiguration(new DeviceConfiguration());
        }
    }
}
