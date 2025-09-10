// =============================================================
// Genova.Alice.Tests — Part 7 (Step 2: TDD)
// Unit tests for PersonTransform (xUnit + FluentAssertions)
// =============================================================

using System.Collections.Generic;
using FluentAssertions;
using Xunit;
using Genova.Alice;

namespace Genova.Alice.Tests;

public class PersonTransformTests
{
    private static SubstitutionTables Tables => SubstitutionTables.CreateClassicDefaults();

    [Fact]
    public void ApplyPerson_swaps_first_to_second_person_pronouns_and_verb()
    {
        var t = Tables;

        // Uppercase pipeline assumption (as in PreProcessor)
        var input = "I AM SAD";
        var result = PersonTransform.ApplyPerson(input, t.Person);

        result.Should().Be("YOU ARE SAD");

        // Possessives and reflexives
        PersonTransform.ApplyPerson("MY BOOK IS MINE", t.Person).Should().Be("YOUR BOOK IS YOURS");
        PersonTransform.ApplyPerson("I HURT MYSELF", t.Person).Should().Be("YOU HURT YOURSELF");
    }

    [Fact]
    public void ApplyPerson2_swaps_second_to_first_person_pronouns_and_verb()
    {
        var t = Tables;

        PersonTransform.ApplyPerson2("YOU ARE FUNNY", t.Person2).Should().Be("I AM FUNNY");
        PersonTransform.ApplyPerson2("YOUR PLAN IS YOURS", t.Person2).Should().Be("MY PLAN IS MINE");

        // Reflexive
        PersonTransform.ApplyPerson2("YOU SEE YOURSELF", t.Person2).Should().Be("I SEE MYSELF");
    }

    [Fact]
    public void ApplyGender_swaps_masculine_and_feminine_terms()
    {
        var t = Tables;

        PersonTransform.ApplyGender("HE HELPED HER", t.Gender).Should().Be("SHE HELPED HIM");
        PersonTransform.ApplyGender("HIS IDEA WAS HERS", t.Gender).Should().Be("HER IDEA WAS HIS");
        PersonTransform.ApplyGender("HIMSELF OR HERSELF", t.Gender).Should().Be("HERSELF OR HIMSELF");
    }

    [Fact]
    public void ApplyWordMap_prefers_exact_token_and_leaves_unknowns_untouched()
    {
        // Map where potential prefixes exist (YOUR vs YOURSELF)
        var map = new Dictionary<string, string>
        {
            ["YOUR"] = "MY",
            ["YOURSELF"] = "MYSELF"
        };

        // Exact-token replacement
        PersonTransform.ApplyWordMap("YOURSELF", map).Should().Be("MYSELF");
        PersonTransform.ApplyWordMap("YOUR", map).Should().Be("MY");

        // Unknown tokens unchanged
        PersonTransform.ApplyWordMap("HELLO WORLD", map).Should().Be("HELLO WORLD");
    }

    [Fact]
    public void ApplyWordMap_empty_map_returns_original()
    {
        var empty = new Dictionary<string, string>();
        PersonTransform.ApplyWordMap("ANY TEXT HERE", empty).Should().Be("ANY TEXT HERE");
    }
}
