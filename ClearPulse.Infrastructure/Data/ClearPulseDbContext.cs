using ClearPulse.Core.Entities;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Text;
using Microsoft.EntityFrameworkCore;


namespace ClearPulse.Infrastructure.Data
{
    public class ClearPulseDbContext : DbContext
    {
        public ClearPulseDbContext(DbContextOptions<ClearPulseDbContext> options) : base(options) { }

        public DbSet<DeviceAuthEvent> DeviceAuthEvents => Set<DeviceAuthEvent>();
        public DbSet<NasDevice> NasDevices => Set<NasDevice>();
        public DbSet<ClusterNode> ClusterNodes => Set<ClusterNode>();
        public DbSet<AdIntegration> AdIntegrations => Set<AdIntegration>();
        public DbSet<LogEvent> LogEvents => Set<LogEvent>();
        public DbSet<SystemUser> SystemUsers => Set<SystemUser>();
        public DbSet<DeviceInventory> DeviceInventory => Set<DeviceInventory>();
        public DbSet<QuarantineDetail> QuarantineDetails => Set<QuarantineDetail>();
        public DbSet<Circle> CircleMaster => Set<Circle>();
        public DbSet<Branch> BranchMaster => Set<Branch>();

        protected override void OnModelCreating(ModelBuilder b)
        {
            // Common indexes
            b.Entity<DeviceAuthEvent>().HasIndex(x => new { x.CircleCode, x.EventTime });
            b.Entity<DeviceAuthEvent>().HasIndex(x => new { x.AuthType, x.HealthStatus, x.EventTime });
            b.Entity<DeviceAuthEvent>().HasIndex(x => new { x.Status, x.EventTime });
           
            b.Entity<NasDevice>().HasIndex(x => new { x.CircleCode, x.LastSeen });
            b.Entity<NasDevice>().HasIndex(x => x.LastSeen);
            b.Entity<AdIntegration>().HasIndex(x => new { x.AdServer, x.EventTime });
            b.Entity<AdIntegration>().HasIndex(x => new { x.Status, x.ErrorCode, x.EventTime });
            b.Entity<ClusterNode>().HasIndex(x => new { x.Site, x.EventTime });
            b.Entity<ClusterNode>().HasIndex(x => new { x.NodeIp, x.EventTime });
            b.Entity<ClusterNode>().HasIndex(x => new { x.Status, x.EventTime });

            // Raw logs
            b.Entity<LogEvent>().HasIndex(x => new { x.SourceSystem, x.EventTime });

            // Users
            b.Entity<SystemUser>().HasIndex(x => new { x.Username, x.IsActive });

            // Device Inventory (current state)
            b.Entity<DeviceInventory>().HasIndex(x => new { x.MacAddress, x.TenantId }).IsUnique();
            b.Entity<DeviceInventory>().HasIndex(x => new { x.CircleCode, x.CurrentHealthStatus });
            b.Entity<DeviceInventory>().HasIndex(x => x.LastSeenOnline);

            // Masters
            b.Entity<Circle>().HasIndex(x => new { x.CircleCode, x.TenantId }).IsUnique();
            b.Entity<Branch>().HasIndex(x => new { x.BranchCode, x.TenantId }).IsUnique();
            b.Entity<Branch>().HasIndex(x => new { x.CircleCode, x.TenantId });
        }
    }

}
