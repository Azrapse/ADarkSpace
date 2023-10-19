using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapGet("/PollWorker", async () =>
{
    var workerUrl = @"http://adstestapp/GetShips/";
    // Use HttpClient to send an http web request to the worker url to obtain the JSON data.
    var client = new HttpClient();
    var response = await client.GetAsync(workerUrl);
    var content = await response.Content.ReadAsStringAsync();
    return Results.Ok(content);
});
app.Run();
