﻿using Document_Management.Models;
using Microsoft.EntityFrameworkCore;

namespace Document_Management.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<Register> Account { get; set; }
        public DbSet<FileDocument> FileDocuments { get; } 

        public DbSet<RequestGP> Gatepass { get; set; }

    }
}
