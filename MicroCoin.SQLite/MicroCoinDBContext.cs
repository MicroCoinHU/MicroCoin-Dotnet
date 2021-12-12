using MicroCoin.Chain;
using MicroCoin.Cryptography;
using MicroCoin.Types;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MicroCoin.SQLite
{
    class MicroCoinDBContext : DbContext
    {
        public DbSet<Account> Accounts { get; set; }
        public DbSet<AccountInfo> AccountInfos { get; set; }

        public MicroCoinDBContext() : base()
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            options.UseSqlite("Data Source=" + Path.Combine(Params.Current.DataFolder, "accounts.db"));
        }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            modelBuilder.Entity<Account>().Property(p => p.AccountNumber)
                .HasColumnType("integer")
                .HasConversion(new Microsoft.EntityFrameworkCore.Storage.ValueConversion.CastingConverter<AccountNumber, uint>()
            );
            modelBuilder.Entity<AccountInfo>().Property(p => p.AccountNumber)
                .HasColumnType("integer")
                .HasConversion(new Microsoft.EntityFrameworkCore.Storage.ValueConversion.CastingConverter<AccountNumber, uint>()
            );
            modelBuilder.Entity<Account>().HasKey(p => p.AccountNumber);
            modelBuilder.Entity<Account>().Property(p => p.Balance).HasColumnType("integer").
                HasConversion(new Microsoft.EntityFrameworkCore.Storage.ValueConversion.CastingConverter<Currency, ulong>());
            modelBuilder.Entity<Account>().Property(p => p.Name).HasColumnType("BLOB").                
                HasConversion(new Microsoft.EntityFrameworkCore.Storage.ValueConversion.CastingConverter<ByteString, byte[]>());
            modelBuilder.Entity<Account>().Ignore(p => p.BlockNumber).Ignore(p => p.VisibleBalance)
                .Ignore(p=>p.Saved)
                .Ignore(p => p.NameAsString).Ignore(p => p.VisibleBalance);
            modelBuilder.Entity<AccountInfo>().Property(p => p.Price).HasColumnType("integer").HasConversion(new Microsoft.EntityFrameworkCore.Storage.ValueConversion.CastingConverter<Currency, ulong>());
            modelBuilder.Entity<AccountInfo>().Property(p => p.AccountToPayPrice).HasColumnType("integer")
                .HasConversion(new Microsoft.EntityFrameworkCore.Storage.ValueConversion.CastingConverter<AccountNumber, uint>()
                );
            modelBuilder.Entity<AccountInfo>().HasKey(p => p.AccountNumber);
            modelBuilder.Entity<Account>().HasOne(p => p.AccountInfo).WithMany();
            modelBuilder.Entity<ECKeyPair>().Ignore(p => p.PrivateKey);
            modelBuilder.Entity<ECKeyPair>().Ignore(p => p.Name);
            modelBuilder.Entity<ECKeyPair>().Ignore(p => p.ECParameters);
            modelBuilder.Entity<ECKeyPair>().Ignore(p => p.PublicKey);
            modelBuilder.Entity<ECKeyPair>().Ignore(p => p.D);
                
        }
    }
}
