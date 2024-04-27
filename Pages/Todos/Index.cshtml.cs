using System.Collections;
using CodeMechanic.Advanced.Regex;
using CodeMechanic.Diagnostics;
using CodeMechanic.Todoist;
using CodeMechanic.Types;
using evantage.Pages.Todo;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace evantage.Pages.Todos;

[BindProperties(SupportsGet = true)]
public class Index : PageModel
{
    public int project_total_count { get; set; }
    public int completed_tasks_count { get; set; }
    public int all_tasks_count { get; set; }
    public int tasks_related_to_projects { get; set; }
    public string Query { get; set; } = string.Empty;

    // public TodoistStats todoist_stats { get; set; } = new();
    public TodoistStats todoist_stats => cached_todoist_stats;
    private static TodoistStats cached_todoist_stats = new();

    public bool sort_by_priority { get; set; } = true;

    public MyFullDay FullDay => cached_full_day;

    private static MyFullDay cached_full_day { get; set; } = new();
    // public List<TodoistTask> SearchResults = new();

    private readonly ITodoistService todoist;
    private readonly evantage.Services.IMarkdownService markdown;

    public Index(
        ITodoistService todos
        , evantage.Services.IMarkdownService markdown
    )
    {
        todoist = todos;
        this.markdown = markdown;
    }

    public async Task OnGet()
    {
        cached_todoist_stats = await this.todoist.GetProjectsAndTasks();
        project_total_count = todoist_stats.TodoistProjects.Count;
        completed_tasks_count = todoist_stats.CompletedTasks.Count;
        all_tasks_count = todoist_stats.TodoistTasks.Count;
    }

    public async Task<IActionResult> OnGetFullDay()
    {
        Console.WriteLine(nameof(OnGetFullDay));
        // todoist_stats.TodoistTasks.FirstOrDefault()?.Dump("full day works?");

        var todays_frog = GetRandomTasks(1, 1);
        var low_hanging_fruit = GetRandomTasks(2, new[] { 3, 4 });
        var midday_tasks = GetRandomTasks(2, new[] { 2, 3 });

        cached_full_day.TodaysFrog = todays_frog;
        cached_full_day.LowHangingFruit = low_hanging_fruit;
        cached_full_day.Midday = midday_tasks;

        return Partial("_FullDayCard", this);
    }

    private static List<TodoistTask> GetRandomTasks(int take = 1, params int[] priorities)
    {
        if (priorities?.Length == 0)
            priorities = Enumerable.Range(3, 4).ToArray();
        return cached_todoist_stats.TodoistTasks
            .Where(todo => priorities.Contains(todo.priority.FixPriorityBug().Id))
            .OrderBy(todo => todo.created_at.ToDateTime()) // favor older tasks
            .Dump("sorted by created date")
            .TakeRandom(take)
            .ToList();
    }

    public async Task<IActionResult> OnGetSearch()
    {
        Console.WriteLine("Query:>> " + Query);
        if (Query.IsEmpty())
            return Partial("_TodoistTasksTable", this);

        todoist_stats.TodoistTasks = todoist_stats.TodoistTasks
            .Where(x => x.ToString()
                .Contains(Query, StringComparison.OrdinalIgnoreCase))
            .ToList();

        return Partial("_TodoistTasksTable", this);
    }

    public async Task<IActionResult> OnGetFindComment(string task_id)
    {
        if (task_id.IsEmpty())
            return Content("<p class='alert'>task id is required!</p>");
        Console.WriteLine(nameof(OnGetFindComment));
        var comment = (await todoist.GetTaskComments(task_id)).FirstOrDefault();
        return Content($"<b>{comment.content}</b>");
    }

    public async Task<IActionResult> OnGetAllProjectTodos()
    {
        var readme_file = markdown
            // .Dump("all")
            .GetAllMarkdownFiles()
            .FirstOrDefault(x => x.FilePath.Contains("README", StringComparison.InvariantCultureIgnoreCase));

        string[] readme_lines = System.IO.File.ReadAllLines(readme_file.FilePath);

        var priorities = readme_lines.SelectMany(l => l.Extract<Priority>(Priority.Pattern))
                .Dump("priorities")
            ;

        // string readme_text = System.IO.File.ReadAllText(readme_file.FilePath);
        // Console.WriteLine(readme_text);

        // var todos_from_readme = readme_text.Extract<ProjectTodo>(TodoPattern.ReadmeCheckbox.Pattern);
        // todos_from_readme.Dump(nameof(todos_from_readme));

        return Partial("_ProjectsTable", new List<ProjectTodo>());
    }

    public async Task<IActionResult> OnGetAllTodoistTasks()
    {
        cached_todoist_stats = await this.todoist.GetProjectsAndTasks();
        return Partial("_TodoistTasksTable", this);
    }
}