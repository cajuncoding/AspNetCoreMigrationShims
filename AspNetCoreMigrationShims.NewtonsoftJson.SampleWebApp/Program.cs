using AspNetCoreMigrationShims.NewtonsoftJson.NetFrameworkCompatibility;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

builder.Services
    .AddControllersWithViews()
    .AddNewtonsoftJsonWithNetFrameworkCompatibility();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.MapControllers();
app.MapRazorPages();
app.UseRouting();
app.UseStaticFiles();

await app.RunAsync();
