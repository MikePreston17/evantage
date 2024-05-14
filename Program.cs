using System.Reflection;
using CodeMechanic.Airtable;
using CodeMechanic.Diagnostics;
using CodeMechanic.Embeds;
using CodeMechanic.FileSystem;
using CodeMechanic.RazorHAT.Services;
using CodeMechanic.Scriptures;
using CodeMechanic.Sqlc;
using CodeMechanic.Todoist;
using evantage.Pages.Logs;
using evantage.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var policyName = "_myAllowSpecificOrigins";

var builder = WebApplication.CreateBuilder(args);

// Allow CORS: https://www.stackhawk.com/blog/net-cors-guide-what-it-is-and-how-to-enable-it/#enable-cors-and-fix-the-problem
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: policyName,
        builder =>
        {
            builder
                // .WithOrigins("http://localhost:3000", "tpot-links-mkii")
                .AllowAnyOrigin()
                .WithMethods("GET")
                .AllowAnyHeader();
        });
});

// Load and inject .env files & values
DotEnv.Load();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// builder.Services.AddSingleton<IEmbeddedResourceQuery, EmbeddedResourceQuery>();
builder.Services.AddSingleton<IJsonConfigService, JsonConfigService>();
builder.Services.AddTransient<IMarkdownService, MarkdownService>();
builder.Services.AddTransient<IGlobalLoggingService, GlobalLoggingService>();
builder.Services.AddSingleton<IInMemoryGraphService, InMemoryGraphService>();
builder.Services.AddTransient<IReadmeService, ReadmeService>();
builder.Services.AddTransient<IScriptureService, ScriptureService>();
builder.Services.AddTransient<IRazorRoutesService2, RazorRoutesService2>();
builder.Services.AddTransient<IDownloadImages, ImageDownloader>();
builder.Services.AddTransient<IAirtableServiceV2, AirtableServiceV2>();
builder.Services.AddTransient<ITodoistService, TodoistService>();
builder.Services.AddTransient<IImageService, ImageService>();
builder.Services.AddTransient<IGenerateSQLTypes, SQLCService>();
builder.Services.AddTransient<INotesService, NotesService>();
builder.Services.AddTransient<ICopyPastaService, CopyPastaService>();
builder.Services.AddControllers();

var main_assembly = Assembly.GetExecutingAssembly();
builder.Services.AddSingleton<IEmbeddedResourceQuery>(
    new EmbeddedResourceService(
            new Assembly[]
            {
                main_assembly
            },
            debugMode: false
        )
        .CacheAllEmbeddedFileContents());


// Add services to the container.
builder.Services.AddRazorPages();

var app = builder.Build();

if (app.Environment.IsDevelopment().Dump("is dev?"))
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

// source: https://github.com/tutorialseu/sending-emails-in-asp/blob/main/Program.cs

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseHttpsRedirection();

app.UseCors(policyName);
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();