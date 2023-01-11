﻿// <auto-generated />
using Economy.MySql;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Economy.MySql.Migrations
{
    [DbContext(typeof(EconomyDbContext))]
    partial class EconomyDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "3.1.32")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            modelBuilder.Entity("Economy.API.Account", b =>
                {
                    b.Property<string>("OwnerId")
                        .HasColumnType("varchar(255) CHARACTER SET utf8mb4");

                    b.Property<string>("OwnerType")
                        .HasColumnType("varchar(255) CHARACTER SET utf8mb4");

                    b.Property<decimal>("Balance")
                        .HasColumnType("decimal(65,30)");

                    b.HasKey("OwnerId", "OwnerType");

                    b.ToTable("Feli_Economy_MySql_Accounts");
                });
#pragma warning restore 612, 618
        }
    }
}
