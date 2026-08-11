using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nizam.Desktop.Models;
using Nizam.Desktop.Services;

namespace Nizam.Desktop.ViewModels;

public partial class WbsTreeViewModel : ObservableObject
{
    private readonly INizamApiClient _api;
    private readonly IDialogService _dialogs;

    public WbsTreeViewModel(INizamApiClient api, IDialogService dialogs)
    {
        _api = api;
        _dialogs = dialogs;
    }

    public ObservableCollection<WbsNodeDto> Nodes { get; } = new();

    [ObservableProperty]
    private Guid _projectId;

    [ObservableProperty]
    private string _newWbsCode = string.Empty;

    [ObservableProperty]
    private string _newWbsName = string.Empty;

    [ObservableProperty]
    private Guid? _parentWbsId;

    [ObservableProperty]
    private WbsNodeDto? _selectedNode;

    [ObservableProperty]
    private string _renameText = string.Empty;

    [ObservableProperty]
    private string? _errorMessage;

    public string TitleLabel => "WBS Ağacı";
    public string CodeLabel => "Kod";
    public string NameLabel => "Ad";
    public string CreateLabel => "WBS Ekle";
    public string RenameLabel => "Yeniden Adlandır";

    public void SetProject(Guid projectId) => ProjectId = projectId;

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        if (ProjectId == Guid.Empty)
            return;

        try
        {
            var list = await _api.GetWbsAsync(ProjectId, cancellationToken);
            Nodes.Clear();
            foreach (var n in list.OrderBy(x => x.SortOrder).ThenBy(x => x.WbsCode))
                Nodes.Add(n);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            _dialogs.ShowError(ErrorMessage);
        }
    }

    [RelayCommand]
    private async Task CreateAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(NewWbsCode) || string.IsNullOrWhiteSpace(NewWbsName))
        {
            ErrorMessage = "WBS kodu ve adı zorunludur.";
            _dialogs.ShowError(ErrorMessage);
            return;
        }

        try
        {
            var created = await _api.CreateWbsAsync(ProjectId, new CreateWbsRequest
            {
                ParentWbsId = ParentWbsId,
                WbsCode = NewWbsCode.Trim(),
                Name = NewWbsName.Trim(),
                SortOrder = Nodes.Count
            }, cancellationToken);

            Nodes.Add(created);
            NewWbsCode = string.Empty;
            NewWbsName = string.Empty;
            _dialogs.ShowInfo($"WBS eklendi: {created.WbsCode}");
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            _dialogs.ShowError(ErrorMessage);
        }
    }

    [RelayCommand]
    private void BeginRename(WbsNodeDto? node)
    {
        SelectedNode = node;
        RenameText = node?.Name ?? string.Empty;
    }

    /// <summary>
    /// Local rename for UI draft; full persistence uses Update WBS API when available on Windows head.
    /// </summary>
    [RelayCommand]
    private void Rename()
    {
        if (SelectedNode is null || string.IsNullOrWhiteSpace(RenameText))
        {
            ErrorMessage = "Yeniden adlandırma için WBS seçin ve ad girin.";
            return;
        }

        var idx = Nodes.IndexOf(SelectedNode);
        if (idx < 0)
            return;

        var updated = new WbsNodeDto
        {
            Id = SelectedNode.Id,
            ProjectId = SelectedNode.ProjectId,
            ParentWbsId = SelectedNode.ParentWbsId,
            WbsCode = SelectedNode.WbsCode,
            Name = RenameText.Trim(),
            Description = SelectedNode.Description,
            SortOrder = SelectedNode.SortOrder,
            Progress = SelectedNode.Progress
        };
        Nodes[idx] = updated;
        SelectedNode = updated;
        _dialogs.ShowInfo($"WBS adı güncellendi: {updated.Name}");
    }
}
