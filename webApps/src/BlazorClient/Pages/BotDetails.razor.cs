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

    private bool showEditModal;
    private string editBotName = "";
    private List<ChannelDto> availableChannels = new();
    private List<Guid> selectedChannelIds = new();
    private bool isSavingEdit;

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

    private string GetDesignerUrl(Guid workflowId) => Navigation.ToAbsoluteUri($"/designer/{workflowId}").ToString();

    private async Task CreateNewWorkflow()
    {
        var nextVersion = _bot?.Workflows?.Count > 0
            ? _bot!.Workflows!.Max(w => w.Version) + 1
            : 1;
        var success = await ApiClient.AddBotWorkflowAsync(BotId, nextVersion);

        if (success) await LoadBotData();
        else await Js.InvokeVoidAsync("alert", "Ошибка при создании версии");
    }

    private async Task CreateWorkflowFromVersion(Guid sourceWorkflowId)
    {
        var success = await ApiClient.CopyBotWorkflowAsync(sourceWorkflowId);

        if (success) await LoadBotData();
        else await Js.InvokeVoidAsync("alert", "Ошибка при создании копии версии");
    }

    private async Task SetActive(Guid id)
    {
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

    private async Task OpenEditModal()
    {
        if (_bot == null) return;
        editBotName = _bot.Name ?? "";
        selectedChannelIds = _bot.BotChannels?.Select(bc => bc.ChannelId).ToList() ?? new List<Guid>();
        showEditModal = true;
        await LoadChannelsForModal();
    }

    private async Task LoadChannelsForModal()
    {
        try
        {
            availableChannels = await ApiClient.GetChannelsAsync();
        }
        catch
        {
            availableChannels = new List<ChannelDto>();
        }
    }

    private void ToggleChannel(Guid channelId)
    {
        if (selectedChannelIds.Contains(channelId))
            selectedChannelIds.Remove(channelId);
        else
            selectedChannelIds.Add(channelId);
        StateHasChanged();
    }

    private async Task SaveBot()
    {
        if (_bot == null || string.IsNullOrWhiteSpace(editBotName)) return;

        isSavingEdit = true;
        try
        {
            await ApiClient.UpdateBotAsync(_bot.Id, editBotName.Trim(), selectedChannelIds);
            CloseEditModal();
            await LoadBotData();
        }
        catch (Exception ex)
        {
            await Js.InvokeVoidAsync("alert", $"Ошибка сохранения: {ex.Message}");
        }
        finally
        {
            isSavingEdit = false;
        }
    }

    private void CloseEditModal()
    {
        showEditModal = false;
    }

}