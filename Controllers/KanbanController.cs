using KodvianSuperMarket.Data;
using KodvianSuperMarket.DTOs;
using KodvianSuperMarket.Filters;
using KodvianSuperMarket.Models;
using KodvianSuperMarket.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KodvianSuperMarket.Controllers;

[ApiController]
[Route("api/v1/kanban")]
[DeviceAuth]
[OperatorSessionAuth]
public class KanbanController : ControllerBase
{
    private readonly IKanbanService _kanbanService;
    private readonly ApplicationDbContext _context;

    public KanbanController(IKanbanService kanbanService, ApplicationDbContext context)
    {
        _kanbanService = kanbanService;
        _context = context;
    }

    [HttpGet("boards")]
    public async Task<ActionResult<List<KanbanBoardDto>>> GetBoards()
    {
        var boards = await _kanbanService.GetBoardsAsync();
        return Ok(boards);
    }

    [HttpPost("boards")]
    public async Task<ActionResult<KanbanBoardDto>> CreateBoard([FromBody] CreateKanbanBoardDto request)
    {
        if (!await IsManagerOrAdminAsync())
            return Forbid();

        try
        {
            var board = await _kanbanService.CreateBoardAsync(request.Name, request.Description);
            return Ok(board);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("boards/{id}")]
    public async Task<ActionResult<KanbanBoardDto>> UpdateBoard(int id, [FromBody] UpdateKanbanBoardDto request)
    {
        if (!await IsManagerOrAdminAsync())
            return Forbid();

        try
        {
            var board = await _kanbanService.UpdateBoardAsync(id, request.Name, request.Description, request.IsActive);
            return Ok(board);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("templates")]
    public async Task<ActionResult<List<KanbanTemplateDto>>> GetTemplates([FromQuery] int? boardId = null, [FromQuery] bool? active = null)
    {
        var templates = await _kanbanService.GetTemplatesAsync(boardId, active);
        return Ok(templates);
    }

    [HttpPost("templates")]
    public async Task<ActionResult<KanbanTemplateDto>> CreateTemplate([FromBody] CreateKanbanTemplateDto request)
    {
        if (!await IsManagerOrAdminAsync())
            return Forbid();

        try
        {
            var checklistItems = request.ChecklistItems
                .Select(i => (i.Text, i.SortOrder))
                .ToList();

            var recurrenceRules = request.RecurrenceRules
                .Select(r => (r.Frequency, r.DayOfWeek))
                .ToList();

            var template = await _kanbanService.CreateTemplateAsync(
                request.KanbanBoardId,
                request.Name,
                request.Description,
                request.Shift,
                request.IsRequiredForShiftClose,
                checklistItems,
                recurrenceRules);

            return Ok(template);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("templates/{id}")]
    public async Task<ActionResult<KanbanTemplateDto>> UpdateTemplate(int id, [FromBody] UpdateKanbanTemplateDto request)
    {
        if (!await IsManagerOrAdminAsync())
            return Forbid();

        try
        {
            var template = await _kanbanService.UpdateTemplateAsync(
                id,
                request.Name,
                request.Description,
                request.Shift,
                request.IsRequiredForShiftClose,
                request.IsActive);

            return Ok(template);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("tasks")]
    public async Task<ActionResult<List<KanbanTaskDto>>> GetTasks(
        [FromQuery] int? cashSessionId = null,
        [FromQuery] int? boardId = null,
        [FromQuery] string? status = null,
        [FromQuery] bool? requiredOnly = null)
    {
        var tasks = await _kanbanService.GetTasksAsync(cashSessionId, boardId, status, requiredOnly);
        return Ok(tasks);
    }

    [HttpPatch("tasks/{id}/status")]
    public async Task<ActionResult<KanbanTaskDto>> UpdateTaskStatus(int id, [FromBody] UpdateKanbanTaskStatusDto request)
    {
        var usuarioId = GetCurrentUsuarioId();

        try
        {
            var task = await _kanbanService.UpdateTaskStatusAsync(id, request.Status, usuarioId);
            return Ok(task);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPatch("checklist/{id}/toggle")]
    public async Task<ActionResult<KanbanChecklistItemDto>> ToggleChecklistItem(int id, [FromBody] ToggleChecklistItemDto request)
    {
        var usuarioId = GetCurrentUsuarioId();

        try
        {
            var item = await _kanbanService.ToggleChecklistItemAsync(id, request.IsDone, usuarioId);
            return Ok(item);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("tasks/{taskId}/comments")]
    public async Task<ActionResult<List<KanbanCommentDto>>> GetComments(int taskId)
    {
        try
        {
            var comments = await _kanbanService.GetCommentsAsync(taskId);
            return Ok(comments);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("tasks/{taskId}/comments")]
    public async Task<ActionResult<KanbanCommentDto>> AddComment(int taskId, [FromBody] CreateKanbanCommentDto request)
    {
        var usuarioId = GetCurrentUsuarioId();

        try
        {
            var comment = await _kanbanService.AddCommentAsync(taskId, usuarioId, request.Text);
            return Ok(comment);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private int GetCurrentUsuarioId()
    {
        return (int)HttpContext.Items["SessionUsuarioId"]!;
    }

    private async Task<bool> IsManagerOrAdminAsync()
    {
        var usuarioId = GetCurrentUsuarioId();
        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == usuarioId);

        return usuario != null
            && (usuario.Role == UserRole.Supervisor.ToString() || usuario.Role == UserRole.Admin.ToString());
    }
}
