using System.Text.Json.Serialization;
using Acms.Core.Security;
using Acms.Infrastructure;
using Acms.Web;
using Acms.Web.Security;
using Acms.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAcms(builder.Configuration, allowFakeAwacs: builder.Environment.IsDevelopment());
builder.Services.AddAcmsSecurity(builder.Configuration, builder.Environment);

builder.Services.AddOptions<UiOptions>().BindReplacingArrays(builder.Configuration.GetSection(UiOptions.SectionName));
builder.Services.AddMemoryCache();
builder.Services.AddScoped<ServerStatusService>();

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/Admin", AcmsPolicies.CanAdminister);
    options.Conventions.AuthorizePage("/Equipment/Edit", AcmsPolicies.CanEditEquipment);
    options.Conventions.AuthorizePage("/Equipment/Bulk", AcmsPolicies.CanEditEquipment);
    options.Conventions.AllowAnonymousToPage("/StatusCode");
    options.Conventions.AllowAnonymousToPage("/Error");
});

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/StatusCode", "?code={0}");
app.Use(async (context, next) =>
{
    await next(context);

    // Leave 401 alone (it is part of the Windows/Negotiate handshake) and let the API
    // return its own status codes.
    if (context.Response.StatusCode == StatusCodes.Status401Unauthorized
        || context.Request.Path.StartsWithSegments("/api"))
    {
        var statusPages = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IStatusCodePagesFeature>();
        if (statusPages is not null)
        {
            statusPages.Enabled = false;
        }
    }
});
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();

await DatabaseInitializer.InitializeAsync(app);

app.Run();
