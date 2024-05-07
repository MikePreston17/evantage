using Hydro;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace evantage.Pages.Shared;

[HtmlTargetElement("hydro-bars")]
public class HydroBars : HydroView
{
    public string message { get; set; } = "loading ... ";
    public string htmx_target_id { get; set; } = "loading";
}