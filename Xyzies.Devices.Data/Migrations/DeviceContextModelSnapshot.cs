﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Xyzies.Devices.Data;

namespace Xyzies.Devices.Data.Migrations
{
    [DbContext(typeof(DeviceContext))]
    partial class DeviceContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "2.2.4-servicing-10062")
                .HasAnnotation("Relational:MaxIdentifierLength", 128)
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("Xyzies.Devices.Data.Entity.Comment", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<DateTime>("CreateOn");

                    b.Property<Guid>("DeviceId");

                    b.Property<bool>("IsDeleted");

                    b.Property<string>("Message");

                    b.Property<Guid>("UserId");

                    b.Property<string>("UserName");

                    b.HasKey("Id");

                    b.HasIndex("DeviceId");

                    b.ToTable("Comments");
                });

            modelBuilder.Entity("Xyzies.Devices.Data.Entity.Device", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<Guid>("BranchId");

                    b.Property<int>("CompanyId");

                    b.Property<DateTime>("CreatedOn");

                    b.Property<string>("DeviceName");

                    b.Property<string>("HexnodeUdid");

                    b.Property<bool>("IsDeleted");

                    b.Property<bool>("IsPending");

                    b.Property<double>("Latitude");

                    b.Property<double>("Longitude");

                    b.Property<string>("Phone");

                    b.Property<double>("Radius");

                    b.Property<string>("Udid")
                        .IsRequired();

                    b.HasKey("Id");

                    b.HasIndex("Udid")
                        .IsUnique();

                    b.ToTable("Devices");
                });

            modelBuilder.Entity("Xyzies.Devices.Data.Entity.DeviceHistory", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("CompanyId");

                    b.Property<DateTime>("CreatedOn");

                    b.Property<double>("CurrentDeviceLocationLatitude");

                    b.Property<double>("CurrentDeviceLocationLongitude");

                    b.Property<Guid>("DeviceId");

                    b.Property<double>("DeviceLocationLatitude");

                    b.Property<double>("DeviceLocationLongitude");

                    b.Property<double>("DeviceRadius");

                    b.Property<bool>("IsDeleted");

                    b.Property<bool>("IsInLocation");

                    b.Property<bool>("IsOnline");

                    b.Property<Guid?>("LoggedInUserId");

                    b.HasKey("Id")
                        .HasName("PK_DeviceHistory");

                    b.HasIndex("DeviceId");

                    b.HasIndex("DeviceId", "CreatedOn");

                    b.ToTable("DeviceHistory");
                });

            modelBuilder.Entity("Xyzies.Devices.Data.Entity.Log", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<DateTime>("CreateOn");

                    b.Property<bool>("IsDeleted");

                    b.Property<string>("Message")
                        .IsRequired();

                    b.Property<string>("Status");

                    b.HasKey("Id");

                    b.ToTable("Logs");
                });

            modelBuilder.Entity("Xyzies.Devices.Data.Entity.Comment", b =>
                {
                    b.HasOne("Xyzies.Devices.Data.Entity.Device", "Device")
                        .WithMany()
                        .HasForeignKey("DeviceId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Xyzies.Devices.Data.Entity.DeviceHistory", b =>
                {
                    b.HasOne("Xyzies.Devices.Data.Entity.Device", "Device")
                        .WithMany("DeviceHistory")
                        .HasForeignKey("DeviceId")
                        .OnDelete(DeleteBehavior.Cascade);
                });
#pragma warning restore 612, 618
        }
    }
}
