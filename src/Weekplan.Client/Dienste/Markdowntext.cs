using Markdig;
using Markdig.Renderers;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Microsoft.AspNetCore.Components;

namespace Weekplan.Client.Dienste;

/// <summary>
/// Die Kochanleitung ist Markdown und kommt aus der Datenbank. Sie wird hier im
/// Browser zu HTML — mit drei Sperren, die zusammen erst das halten, was
/// Abnahmekriterium 4 verspricht:
///
/// <list type="number">
/// <item><c>DisableHtml</c> macht eingebettetes Markup zu Text. Ein
/// <c>&lt;script&gt;</c> im Rezept ist danach ein sichtbares Wort.</item>
/// <item><b>Bilder werden entfernt.</b> Markdowns eigene Bildsyntax geht an
/// <c>DisableHtml</c> vorbei und wuerde eine fremde Adresse nachladen — die App
/// macht aber keine externen Requests, das ist eine Zusage der README.</item>
/// <item><b>Verweise duerfen nur http, https und mailto.</b> Sonst waere
/// <c>[klick](javascript:…)</c> ein ausfuehrbares Skript, ganz ohne HTML.</item>
/// </list>
///
/// <para>Tabellen sind zugelassen: Garzeiten liest man darin am besten.</para>
/// </summary>
public static class Markdowntext
{
    private static readonly MarkdownPipeline Rohr = new MarkdownPipelineBuilder()
        .DisableHtml()
        .UsePipeTables()
        .Build();

    private static readonly string[] ErlaubteVerfahren = ["http", "https", "mailto"];

    public static MarkupString AlsHtml(string? markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown)) return new MarkupString("");

        var dokument = Markdown.Parse(markdown, Rohr);

        // ToList, weil das Ersetzen den Baum unter dem Iterator veraendert.
        foreach (var verweis in dokument.Descendants<LinkInline>().ToList())
        {
            if (verweis.IsImage || !Unbedenklich(verweis.Url))
            {
                // copyChildren: false — sonst haengt Markdig die Kinder des
                // Verweises zusaetzlich neben den Ersatztext, und der Leser
                // sieht „klickklick".
                verweis.ReplaceBy(new LiteralInline(Text(verweis)), copyChildren: false);
            }
        }

        var schreiber = new StringWriter();
        var maler = new HtmlRenderer(schreiber);
        Rohr.Setup(maler);
        maler.Render(dokument);

        return new MarkupString(schreiber.ToString());
    }

    /// <summary>Relative Ziele und Sprungmarken sind in Ordnung; alles mit Verfahren muss auf die Liste.</summary>
    private static bool Unbedenklich(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;
        if (!Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out var ziel)) return false;
        if (!ziel.IsAbsoluteUri) return !url.TrimStart().StartsWith("javascript:", StringComparison.OrdinalIgnoreCase);

        return ErlaubteVerfahren.Contains(ziel.Scheme, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Was von einem entfernten Verweis bleibt: sein sichtbarer Text.</summary>
    private static string Text(LinkInline verweis)
        => string.Concat(verweis.Descendants<LiteralInline>().Select(teil => teil.Content.ToString()));
}
