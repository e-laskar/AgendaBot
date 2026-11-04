using AgendaBot.Configuration;

namespace AgendaBot.UnitTests.Configuration;

public class DotEnvParseTests
{
    [Fact]
    public void Parses_key_value_pairs()
    {
        var entries = DotEnv.Parse(["Discord__Token=abc", "Discord__TimeZone=Europe/Paris"]);

        entries.ShouldBe([("Discord__Token", "abc"), ("Discord__TimeZone", "Europe/Paris")]);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("# commentaire")]
    [InlineData("SANS_EGAL")]
    [InlineData("=valeur_sans_cle")]
    public void Ignores_blank_comment_and_invalid_lines(string line)
    {
        DotEnv.Parse([line]).ShouldBeEmpty();
    }

    [Fact]
    public void Strips_export_prefix()
    {
        DotEnv.Parse(["export CLE=valeur"]).ShouldHaveSingleItem().ShouldBe(("CLE", "valeur"));
    }

    [Fact]
    public void Strips_inline_comment_on_unquoted_value()
    {
        DotEnv.Parse(["CLE=valeur # commentaire"]).ShouldHaveSingleItem().Value.ShouldBe("valeur");
    }

    [Theory]
    [InlineData("CLE=\"a # b\"", "a # b")]
    [InlineData("CLE='a # b'", "a # b")]
    [InlineData("CLE=\"ligne1\\nligne2\"", "ligne1\nligne2")]
    [InlineData("CLE='ligne1\\nligne2'", "ligne1\\nligne2")]
    [InlineData("CLE=\"valeur\" # commentaire", "valeur")]
    public void Handles_quoted_values(string line, string expected)
    {
        DotEnv.Parse([line]).ShouldHaveSingleItem().Value.ShouldBe(expected);
    }

    [Fact]
    public void Keeps_equal_signs_inside_value()
    {
        DotEnv.Parse(["ConnectionStrings__Agenda=Host=db;Port=5432"])
            .ShouldHaveSingleItem().Value.ShouldBe("Host=db;Port=5432");
    }

    [Fact]
    public void Keeps_empty_value()
    {
        DotEnv.Parse(["Discord__Token="]).ShouldHaveSingleItem().ShouldBe(("Discord__Token", ""));
    }
}
