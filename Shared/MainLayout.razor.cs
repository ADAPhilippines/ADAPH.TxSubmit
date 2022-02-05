using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;

namespace ADAPH.TxSubmit.Shared;
public partial class MainLayout
{
  [Inject] private ILocalStorageService? LocalStorageService { get; set; }
  private bool IsDarkMode { get; set; } = true;

  protected override async Task OnAfterRenderAsync(bool firstRender)
  {
    if (firstRender)
    {
      if (LocalStorageService is not null)
      {
        IsDarkMode = (await LocalStorageService.GetItemAsync<bool?>("IsDarkMode")) ?? false;
        await InvokeAsync(StateHasChanged);
      }
    }
    await base.OnAfterRenderAsync(firstRender);
  }

  private async void OnThemeChanged(bool value)
  {
    if (LocalStorageService is null) throw new Exception("LocalStorageService is null.");
    IsDarkMode = !IsDarkMode;
    await LocalStorageService.SetItemAsync<bool>("IsDarkMode", IsDarkMode);
    await InvokeAsync(StateHasChanged);
  }

}