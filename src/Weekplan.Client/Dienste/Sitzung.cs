using Microsoft.JSInterop;

namespace Weekplan.Client.Dienste;

/// <summary>
/// Das Merkmal dieses Geraets. Es liegt im Browserspeicher und hat keinen
/// Ablauf: einmal pro Geraet anmelden, danach nie wieder. Kein Cookie — Client
/// und Server liegen auf verschiedenen Herkuenften.
/// </summary>
public sealed class Sitzung(IJSRuntime js)
{
    private const string Schluessel = "weekplan.merkmal";

    public string? Merkmal { get; private set; }

    public bool Angemeldet => !string.IsNullOrEmpty(Merkmal);

    public event Action? Geaendert;

    public async Task WiederherstellenAsync()
    {
        Merkmal = await js.InvokeAsync<string?>("localStorage.getItem", Schluessel);
        Geaendert?.Invoke();
    }

    public async Task AnmeldenAsync(string merkmal)
    {
        Merkmal = merkmal;
        await js.InvokeVoidAsync("localStorage.setItem", Schluessel, merkmal);
        Geaendert?.Invoke();
    }

    public async Task AbmeldenAsync()
    {
        Merkmal = null;
        await js.InvokeVoidAsync("localStorage.removeItem", Schluessel);
        Geaendert?.Invoke();
    }
}
