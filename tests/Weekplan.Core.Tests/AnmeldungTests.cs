using Microsoft.Extensions.DependencyInjection;
using Weekplan.Core.Anmeldung;
using Weekplan.Core.Anmeldung.Contracts;

namespace Weekplan.Core.Tests;

public class PasswoerterTests
{
    private static IPasswoerter Passwoerter() => Dienste().GetRequiredService<IPasswoerter>();

    internal static ServiceProvider Dienste(string schluessel = TestSchluessel) =>
        new ServiceCollection().AddAnmeldung(schluessel).BuildServiceProvider();

    internal const string TestSchluessel = "nur-fuer-tests-mindestens-32-zeichen-lang!!";

    [Fact]
    public void Ein_gehashtes_Passwort_wird_wiedererkannt()
    {
        var p = Passwoerter();
        var hash = p.Hashen("k0rrekt-pferd");

        Assert.True(p.Stimmt(hash, "k0rrekt-pferd"));
    }

    [Fact]
    public void Ein_falsches_Passwort_wird_abgelehnt()
    {
        var p = Passwoerter();

        Assert.False(p.Stimmt(p.Hashen("k0rrekt-pferd"), "falsch"));
    }

    [Fact]
    public void Der_Hash_steht_nie_im_Klartext_und_wiederholt_sich_nicht()
    {
        var p = Passwoerter();

        var a = p.Hashen("k0rrekt-pferd");
        var b = p.Hashen("k0rrekt-pferd");

        Assert.DoesNotContain("k0rrekt-pferd", a);
        Assert.NotEqual(a, b);   // gesalzen
    }

    [Fact]
    public void Ein_kaputter_Hash_wirft_nicht_sondern_lehnt_ab()
    {
        Assert.False(Passwoerter().Stimmt("kein-gueltiger-hash", "egal"));
    }
}

public class MerkmaleTests
{
    private static IMerkmale Merkmale(string schluessel = PasswoerterTests.TestSchluessel)
        => PasswoerterTests.Dienste(schluessel).GetRequiredService<IMerkmale>();

    [Fact]
    public async Task Ein_erzeugtes_Merkmal_nennt_seinen_Nutzer()
    {
        var m = Merkmale();

        Assert.Equal("marvin", await m.NutzerAusAsync(m.Erzeugen("marvin")));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("voelliger-unsinn")]
    [InlineData("a.b.c")]
    public async Task Was_kein_Merkmal_ist_nennt_keinen_Nutzer(string? unsinn)
    {
        Assert.Null(await Merkmale().NutzerAusAsync(unsinn));
    }

    [Fact]
    public async Task Ein_Merkmal_aus_fremder_Hand_gilt_nicht()
    {
        var fremd = Merkmale("ein-voellig-anderer-schluessel-32-zeichen!!").Erzeugen("marvin");

        Assert.Null(await Merkmale().NutzerAusAsync(fremd));
    }

    [Fact]
    public async Task Ein_verbogenes_Merkmal_gilt_nicht()
    {
        var echt = Merkmale().Erzeugen("marvin");
        var verbogen = echt[..^3] + (echt.EndsWith("abc") ? "xyz" : "abc");

        Assert.Null(await Merkmale().NutzerAusAsync(verbogen));
    }
}
