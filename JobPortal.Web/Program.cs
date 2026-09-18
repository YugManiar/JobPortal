using JobPortal.Application.Interfaces;
using JobPortal.Domain.Entities;
using JobPortal.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using JobPortal.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<IJobRepository, JobPortal.Infrastructure.Repositories.JobRepository>();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequiredLength = 8;
    options.SignIn.RequireConfirmedAccount = false; // revisit for prod
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// build and run
var app = builder.Build();
app.Run();