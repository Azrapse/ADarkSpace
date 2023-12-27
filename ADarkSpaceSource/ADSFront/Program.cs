var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromSeconds(10);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
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

app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}");

app.MapControllerRoute(
    name: "login",
    pattern: "{controller=Home}/{action=Login}"
    );

app.MapControllerRoute(
    name: "register",
    pattern: "{controller=Home}/{action=Register}"
    );

app.MapControllerRoute(
    name: "play",
    pattern: "{controller=Home}/{action=Play}"
    );

app.MapControllers();

app.MapGet("/PollWorker", async () =>
{
    var workerUrl = @$"http://{Environment.GetEnvironmentVariable("GAMEPLAYWORKER_HOST")}/GetGameState/";
    // Use HttpClient to send an http web request to the worker url to obtain the JSON data.
    var client = new HttpClient();
    var response = await client.GetAsync(workerUrl);
    var content = await response.Content.ReadAsStringAsync();
    return Results.Ok(content);
});

app.Run();