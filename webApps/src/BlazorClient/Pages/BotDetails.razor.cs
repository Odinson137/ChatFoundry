using BlazorClient.Interfaces;
using BlazorClient.Models.DTO;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace BlazorClient.Pages;

public partial class BotDetails
{
    [Parameter] public Guid BotId { get; set; }

    [Inject] private IWorkflowApiClient ApiClient { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private IJSRuntime Js { get; set; } = null!;

    private BotDto? _bot;
    private bool _isLoading = true;
    private string? _error;

    protected override async Task OnInitializedAsync()
    {
        await LoadBotData();
    }

    private async Task LoadBotData()
    {
        _isLoading = true;
        _error = null;
        try
        {
            _bot = await ApiClient.GetBotWithWorkflowsAsync(BotId);
            if (_bot == null) _error = "Бот не найден.";
        }
        catch (Exception ex)
        {
            _error = ex.Message;
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async Task CopyToken()
    {
        if (_bot != null) await Js.InvokeVoidAsync("navigator.clipboard.writeText", _bot.Token);
    }

    private void NavigateToDesigner(Guid workflowId) => Navigation.NavigateTo($"/designer/{workflowId}");

    private async Task CreateNewWorkflow()
    {
        var nextVersion = 1;
        var success = await ApiClient.AddBotWorkflowAsync(BotId, nextVersion);
    
        if (success) await LoadBotData();
        else await Js.InvokeVoidAsync("alert", "Ошибка при создании версии");
    }

    private async Task SetActive(Guid id)
    {
        // Примечание: В идеале бэкенд должен сам деактивировать остальные версии при активации одной.
        var success = await ApiClient.UpdateBotWorkflowAsync(id, true);
    
        if (success) await LoadBotData();
        else await Js.InvokeVoidAsync("alert", "Ошибка при активации");
    }

    private async Task DeleteWorkflow(Guid id)
    {
        var confirmed = await Js.InvokeAsync<bool>("confirm", "Вы уверены, что хотите удалить эту версию? Это действие необратимо.");
        if (!confirmed) return;

        var success = await ApiClient.DeleteBotWorkflowAsync(id);
    
        if (success) await LoadBotData();
        else await Js.InvokeVoidAsync("alert", "Ошибка при удалении. Возможно, версия используется в активных сессиях.");
    }

}