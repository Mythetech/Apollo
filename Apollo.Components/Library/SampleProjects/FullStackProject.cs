using Apollo.Components.Solutions;
using Apollo.Contracts.Solutions;

namespace Apollo.Components.Library.SampleProjects;

public static class FullStackProject
{
    public static SolutionModel Create()
    {
        var solution = new SolutionModel
        {
            Name = "FullStackDemo",
            Description = "Full-stack demo with Minimal API backend and HTML/JS frontend",
            ProjectType = ProjectType.WebApi,
            Items = new List<ISolutionItem>()
        };

        var rootFolder = new Folder
        {
            Name = "FullStackDemo",
            Uri = "virtual/FullStackDemo",
            Items = new List<ISolutionItem>()
        };
        solution.Items.Add(rootFolder);

        solution.Items.Add(new SolutionFile
        {
            Name = "Program.cs",
            Uri = "virtual/FullStackDemo/Program.cs",
            Data = ApiCode,
            CreatedAt = DateTimeOffset.Now,
            ModifiedAt = DateTimeOffset.Now
        });

        solution.Items.Add(new SolutionFile
        {
            Name = "index.html",
            Uri = "virtual/FullStackDemo/index.html",
            Data = HtmlCode,
            CreatedAt = DateTimeOffset.Now,
            ModifiedAt = DateTimeOffset.Now
        });

        solution.Items.Add(new SolutionFile
        {
            Name = "app.js",
            Uri = "virtual/FullStackDemo/app.js",
            Data = JavaScriptCode,
            CreatedAt = DateTimeOffset.Now,
            ModifiedAt = DateTimeOffset.Now
        });

        solution.Items.Add(new SolutionFile
        {
            Name = "styles.css",
            Uri = "virtual/FullStackDemo/styles.css",
            Data = CssCode,
            CreatedAt = DateTimeOffset.Now,
            ModifiedAt = DateTimeOffset.Now
        });

        return solution;
    }

    private const string ApiCode = """
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var todos = new List<Todo>
{
    new Todo { Id = 1, Title = "Learn Apollo Editor", Completed = false },
    new Todo { Id = 2, Title = "Build a full-stack app", Completed = false },
    new Todo { Id = 3, Title = "Share with friends", Completed = false }
};
var nextId = 4;

app.MapGet("/api/todos", () => System.Text.Json.JsonSerializer.Serialize(todos));

app.MapPost("/api/todos", (string body) => 
{
    var input = System.Text.Json.JsonSerializer.Deserialize<TodoInput>(body);
    var todo = new Todo { Id = nextId++, Title = input.title, Completed = false };
    todos.Add(todo);
    return System.Text.Json.JsonSerializer.Serialize(todo);
});

app.MapPut("/api/todos/{id}", (string id, string body) =>
{
    var todoId = int.Parse(id);
    var input = System.Text.Json.JsonSerializer.Deserialize<TodoInput>(body);
    var todo = todos.FirstOrDefault(t => t.Id == todoId);
    if (todo != null)
    {
        if (input.title != null) todo.Title = input.title;
        if (input.completed.HasValue) todo.Completed = input.completed.Value;
    }
    return System.Text.Json.JsonSerializer.Serialize(todo);
});

app.MapDelete("/api/todos/{id}", (string id) =>
{
    var todoId = int.Parse(id);
    todos.RemoveAll(t => t.Id == todoId);
    return "{}";
});

app.Run();

class Todo { public int Id { get; set; } public string Title { get; set; } public bool Completed { get; set; } }
class TodoInput { public string title { get; set; } public bool? completed { get; set; } }
""";

    private const string HtmlCode = """
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Todo App - Apollo Full-Stack Demo</title>
    <link rel="stylesheet" href="styles.css">
</head>
<body>
    <div class="container">
        <header>
            <h1>📝 Todo App</h1>
            <p class="subtitle">Full-Stack Demo powered by Apollo</p>
        </header>
        
        <div class="add-todo">
            <input type="text" id="new-todo-input" placeholder="What needs to be done?" />
            <button id="add-btn" onclick="addTodo()">Add</button>
        </div>
        
        <div class="filters">
            <button class="filter-btn active" data-filter="all">All</button>
            <button class="filter-btn" data-filter="active">Active</button>
            <button class="filter-btn" data-filter="completed">Completed</button>
        </div>
        
        <ul id="todo-list"></ul>
        
        <footer>
            <span id="items-left">0 items left</span>
            <button id="clear-completed" onclick="clearCompleted()">Clear completed</button>
        </footer>
    </div>
    
    <script src="app.js"></script>
</body>
</html>
""";

    private const string JavaScriptCode = """
let todos = [];
let currentFilter = 'all';

async function loadTodos() {
    try {
        const response = await fetch('/api/todos');
        todos = await response.json();
        renderTodos();
    } catch (error) {
        console.error('Failed to load todos:', error);
    }
}

async function addTodo() {
    const input = document.getElementById('new-todo-input');
    const title = input.value.trim();
    
    if (!title) return;
    
    try {
        const response = await fetch('/api/todos', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ title })
        });
        
        if (response.ok) {
            const newTodo = await response.json();
            todos.push(newTodo);
            input.value = '';
            renderTodos();
        }
    } catch (error) {
        console.error('Failed to add todo:', error);
    }
}

async function toggleTodo(id) {
    const todo = todos.find(t => t.Id === id);
    if (!todo) return;
    
    try {
        const response = await fetch(`/api/todos/${id}`, {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ completed: !todo.Completed })
        });
        
        if (response.ok) {
            const updated = await response.json();
            const index = todos.findIndex(t => t.Id === id);
            todos[index] = updated;
            renderTodos();
        }
    } catch (error) {
        console.error('Failed to toggle todo:', error);
    }
}

async function deleteTodo(id) {
    try {
        const response = await fetch(`/api/todos/${id}`, {
            method: 'DELETE'
        });
        
        if (response.ok) {
            todos = todos.filter(t => t.Id !== id);
            renderTodos();
        }
    } catch (error) {
        console.error('Failed to delete todo:', error);
    }
}

async function clearCompleted() {
    const completed = todos.filter(t => t.Completed);
    for (const todo of completed) {
        await deleteTodo(todo.Id);
    }
}

function setFilter(filter) {
    currentFilter = filter;
    document.querySelectorAll('.filter-btn').forEach(btn => {
        btn.classList.toggle('active', btn.dataset.filter === filter);
    });
    renderTodos();
}

function getFilteredTodos() {
    switch (currentFilter) {
        case 'active':
            return todos.filter(t => !t.Completed);
        case 'completed':
            return todos.filter(t => t.Completed);
        default:
            return todos;
    }
}

function renderTodos() {
    const list = document.getElementById('todo-list');
    const filtered = getFilteredTodos();
    
    list.innerHTML = filtered.map(todo => `
        <li class="todo-item ${todo.Completed ? 'completed' : ''}">
            <input 
                type="checkbox" 
                ${todo.Completed ? 'checked' : ''} 
                onchange="toggleTodo(${todo.Id})"
            />
            <span class="todo-text">${escapeHtml(todo.Title)}</span>
            <button class="delete-btn" onclick="deleteTodo(${todo.Id})">×</button>
        </li>
    `).join('');
    
    const activeCount = todos.filter(t => !t.Completed).length;
    document.getElementById('items-left').textContent = 
        `${activeCount} item${activeCount !== 1 ? 's' : ''} left`;
}

function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

// Initialize filter buttons
document.querySelectorAll('.filter-btn').forEach(btn => {
    btn.addEventListener('click', () => setFilter(btn.dataset.filter));
});

// Handle Enter key in input
document.getElementById('new-todo-input').addEventListener('keypress', (e) => {
    if (e.key === 'Enter') addTodo();
});

// Load todos on page load
loadTodos();
""";

    private const string CssCode = """
* {
    margin: 0;
    padding: 0;
    box-sizing: border-box;
}

body {
    font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, sans-serif;
    background: linear-gradient(135deg, #1a1a2e 0%, #16213e 100%);
    min-height: 100vh;
    display: flex;
    justify-content: center;
    padding: 2rem;
    color: #eee;
}

.container {
    width: 100%;
    max-width: 500px;
    background: rgba(255, 255, 255, 0.05);
    border-radius: 16px;
    padding: 2rem;
    backdrop-filter: blur(10px);
    border: 1px solid rgba(255, 255, 255, 0.1);
}

header {
    text-align: center;
    margin-bottom: 2rem;
}

header h1 {
    font-size: 2rem;
    margin-bottom: 0.5rem;
}

.subtitle {
    color: #888;
    font-size: 0.875rem;
}

.add-todo {
    display: flex;
    gap: 0.5rem;
    margin-bottom: 1rem;
}

.add-todo input {
    flex: 1;
    padding: 0.75rem 1rem;
    border: 1px solid rgba(255, 255, 255, 0.1);
    border-radius: 8px;
    background: rgba(255, 255, 255, 0.05);
    color: #fff;
    font-size: 1rem;
}

.add-todo input::placeholder {
    color: #666;
}

.add-todo input:focus {
    outline: none;
    border-color: #6366f1;
}

.add-todo button,
#clear-completed {
    padding: 0.75rem 1.5rem;
    background: #6366f1;
    color: white;
    border: none;
    border-radius: 8px;
    cursor: pointer;
    font-size: 1rem;
    transition: background 0.2s;
}

.add-todo button:hover,
#clear-completed:hover {
    background: #4f46e5;
}

.filters {
    display: flex;
    gap: 0.5rem;
    margin-bottom: 1rem;
}

.filter-btn {
    flex: 1;
    padding: 0.5rem;
    background: transparent;
    border: 1px solid rgba(255, 255, 255, 0.1);
    border-radius: 6px;
    color: #888;
    cursor: pointer;
    transition: all 0.2s;
}

.filter-btn:hover {
    color: #fff;
    border-color: rgba(255, 255, 255, 0.2);
}

.filter-btn.active {
    background: rgba(99, 102, 241, 0.2);
    border-color: #6366f1;
    color: #6366f1;
}

#todo-list {
    list-style: none;
    margin-bottom: 1rem;
}

.todo-item {
    display: flex;
    align-items: center;
    gap: 0.75rem;
    padding: 0.75rem;
    background: rgba(255, 255, 255, 0.03);
    border-radius: 8px;
    margin-bottom: 0.5rem;
    transition: background 0.2s;
}

.todo-item:hover {
    background: rgba(255, 255, 255, 0.05);
}

.todo-item.completed .todo-text {
    text-decoration: line-through;
    color: #666;
}

.todo-item input[type="checkbox"] {
    width: 20px;
    height: 20px;
    cursor: pointer;
    accent-color: #6366f1;
}

.todo-text {
    flex: 1;
}

.delete-btn {
    width: 28px;
    height: 28px;
    background: transparent;
    border: 1px solid rgba(239, 68, 68, 0.3);
    border-radius: 6px;
    color: #ef4444;
    cursor: pointer;
    opacity: 0;
    transition: all 0.2s;
    font-size: 1.25rem;
    line-height: 1;
}

.todo-item:hover .delete-btn {
    opacity: 1;
}

.delete-btn:hover {
    background: rgba(239, 68, 68, 0.1);
    border-color: #ef4444;
}

footer {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding-top: 1rem;
    border-top: 1px solid rgba(255, 255, 255, 0.1);
}

#items-left {
    color: #666;
    font-size: 0.875rem;
}

#clear-completed {
    padding: 0.5rem 1rem;
    font-size: 0.875rem;
    background: transparent;
    border: 1px solid rgba(255, 255, 255, 0.1);
    color: #888;
}

#clear-completed:hover {
    background: rgba(239, 68, 68, 0.1);
    border-color: #ef4444;
    color: #ef4444;
}
""";
}

