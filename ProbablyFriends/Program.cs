using Microsoft.Extensions.Hosting.WindowsServices;
using System.Diagnostics;

try
{
    var options = new WebApplicationOptions
    {
        Args = args,
        ContentRootPath = WindowsServiceHelpers.IsWindowsService() ? AppContext.BaseDirectory : default
    };

    var builder = WebApplication.CreateBuilder(options);

    builder.Host.UseWindowsService();

    // Add services to the container.
    builder.Services.AddRazorPages();

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error");
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseStaticFiles();

    app.UseRouting();

    app.UseAuthorization();

    app.MapRazorPages();

    app.Run();
}
catch (Exception ex)
{
    EventLog.WriteEntry("Application", ex.ToString(), EventLogEntryType.Error);
}
