using Weekplan.Client.Dienste;

namespace Weekplan.Core.Tests;

/// <summary>
/// Die Anleitung kommt aus der Datenbank und wird als HTML in die Seite
/// gesetzt. Sie ist damit die einzige Stelle, an der fremder Text zu Markup
/// wird — also steht hier, was dabei nicht passieren darf. Alle drei Sperren
/// sind noetig: <c>DisableHtml</c> allein laesst Markdowns eigene Bild- und
/// Verweissyntax durch, und die reicht fuer ein Skript vollkommen aus.
/// </summary>
public class MarkdowntextTests
{
    private static string Html(string? markdown) => Markdowntext.AlsHtml(markdown).Value;

    [Theory]
    [InlineData("<script>alert(1)</script>")]
    [InlineData("<img src=x onerror=alert(1)>")]
    [InlineData("<iframe src=\"https://fremd.example\"></iframe>")]
    [InlineData("<a href=\"javascript:alert(1)\">klick</a>")]
    public void Eingebettetes_HTML_wird_zu_Text_und_nicht_zu_Markup(string boesartig)
    {
        var html = Html(boesartig);

        Assert.DoesNotContain("<script", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<iframe", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<img", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<a ", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("&lt;", html);
    }

    /// <summary>
    /// Der gefaehrlichste Fall, weil er ohne ein einziges spitzes Klammernpaar
    /// auskommt und <c>DisableHtml</c> deshalb nicht greift.
    /// </summary>
    [Theory]
    [InlineData("[klick](javascript:alert(1))")]
    [InlineData("[klick](JavaScript:alert(1))")]
    [InlineData("[klick](vbscript:msgbox(1))")]
    [InlineData("[klick](data:text/html;base64,PHNjcmlwdD4=)")]
    public void Ein_Verweis_mit_ausfuehrbarem_Ziel_verliert_sein_Ziel(string boesartig)
    {
        var html = Html(boesartig);

        Assert.DoesNotContain("href", html, StringComparison.OrdinalIgnoreCase);

        // Genau einmal: ein entfernter Verweis darf seinen Text nicht verdoppeln.
        Assert.Equal("<p>klick</p>", html.Trim());
    }

    /// <summary>
    /// Bilder sind ausgeschlossen — nicht aus Vorsicht, sondern weil die App
    /// keine externen Requests macht. Der Alternativtext bleibt stehen.
    /// </summary>
    [Fact]
    public void Ein_Bild_wird_entfernt_und_laedt_nichts_nach()
    {
        var html = Html("Vorher ![Chili im Topf](https://fremd.example/a.png) nachher");

        Assert.DoesNotContain("<img", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("fremd.example", html);
        Assert.Equal("<p>Vorher Chili im Topf nachher</p>", html.Trim());
    }

    [Fact]
    public void Ein_gewoehnlicher_Verweis_bleibt_erhalten()
    {
        var html = Html("[Quelle](https://example.org/rezept)");

        Assert.Contains("<a href=\"https://example.org/rezept\">Quelle</a>", html);
    }

    [Fact]
    public void Ueberschriften_Listen_und_Hervorhebungen_kommen_durch()
    {
        var html = Html("## Vorbereitung\n\n1. Zwiebel **würfeln**\n2. Öl erhitzen");

        Assert.Contains("<h2", html);
        Assert.Contains("<ol", html);
        Assert.Contains("<strong>würfeln</strong>", html);
    }

    /// <summary>Garzeiten liest man am besten in einer Tabelle — darum sind sie zugelassen.</summary>
    [Fact]
    public void Tabellen_kommen_durch()
    {
        var html = Html("| Stufe | Zeit |\n|---|---|\n| mittel | 20 min |");

        Assert.Contains("<table", html);
        Assert.Contains("20 min", html);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Eine_leere_Anleitung_ergibt_leeres_Markup(string? leer)
        => Assert.Equal("", Html(leer));
}
