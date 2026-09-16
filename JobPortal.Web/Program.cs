using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using JobPortal.Infrastructure.Data;
using JobPortal.Domain.Entities;

var builder = WebApplication.CreateBuilder(args);

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