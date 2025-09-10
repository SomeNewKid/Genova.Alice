// =============================================================
// Genova.Alice.Tests — Part 1 (Step 2: TDD)
// Unit tests for SubstitutionTables (xUnit + FluentAssertions)
// Note: SubstitutionTables is internal; InternalsVisibleTo("Genova.Alice.Tests") must be set in the main assembly.
// =============================================================

using System;
using System.Collections.Generic;
using FluentAssertions;
using Xunit;
using Genova.Alice;

namespace Genova.Alice.Tests;

public class SubstitutionTablesTests
{
    [Fact]
    public void CreateEmpty_returns_non_null_and_empty_maps()
    {
        var tables = SubstitutionTables.CreateEmpty();

        tables.Should().NotBeNull();
        tables.Normal.Should().NotBeNull();
        tables.Person.Should().NotBeNull();
        tables.Person2.Should().NotBeNull();
        tables.Gender.Should().NotBeNull();

        tables.Normal.Count.Should().Be(0);
        tables.Person.Count.Should().Be(0);
        tables.Person2.Count.Should().Be(0);
        tables.Gender.Count.Should().Be(0);
    }

    [Fact]
    public void CreateEmpty_maps_are_case_insensitive_dictionaries()
    {
        var tables = SubstitutionTables.CreateEmpty();

        tables.Normal["I'M"] = "I AM";
        tables.Person["MY"] = "YOUR";
        tables.Person2["YOU"] = "I";
        tables.Gender["HE"] = "SHE";

        tables.Normal.ContainsKey("i'm").Should().BeTrue();
        tables.Person.ContainsKey("my").Should().BeTrue();
        tables.Person2.ContainsKey("you").Should().BeTrue();
        tables.Gender.ContainsKey("he").Should().BeTrue();

        tables.Normal["i'm"].Should().Be("I AM");
        tables.Person["my"].Should().Be("YOUR");
        tables.Person2["you"].Should().Be("I");
        tables.Gender["he"].Should().Be("SHE");
    }

    [Fact]
    public void Ctor_without_arguments_creates_empty_case_insensitive_maps()
    {
        var tables = new SubstitutionTables();

        tables.Normal.Should().NotBeNull();
        tables.Person.Should().NotBeNull();
        tables.Person2.Should().NotBeNull();
        tables.Gender.Should().NotBeNull();

        tables.Normal.Count.Should().Be(0);
        tables.Person.Count.Should().Be(0);
        tables.Person2.Count.Should().Be(0);
        tables.Gender.Count.Should().Be(0);

        // Case-insensitivity check via behavior
        tables.Normal["CAN'T"] = "CANNOT";
        tables.Normal["can't"].Should().Be("CANNOT");
    }

    [Fact]
    public void Ctor_with_existing_maps_copies_entries_and_enforces_case_insensitive_lookups()
    {
        // Original maps with default (case-sensitive) comparer
        var normalSrc = new Dictionary<string, string> { ["I'M"] = "I AM" };
        var personSrc = new Dictionary<string, string> { ["MY"] = "YOUR" };
        var person2Src = new Dictionary<string, string> { ["YOU"] = "I" };
        var genderSrc = new Dictionary<string, string> { ["HE"] = "SHE" };

        var tables = new SubstitutionTables(normalSrc, personSrc, person2Src, genderSrc);

        // Should not be the same instances (defensive copy)
        tables.Normal.Should().NotBeSameAs(normalSrc);
        tables.Person.Should().NotBeSameAs(personSrc);
        tables.Person2.Should().NotBeSameAs(person2Src);
        tables.Gender.Should().NotBeSameAs(genderSrc);

        // Case-insensitive behavior preserved after copy
        tables.Normal["i'm"].Should().Be("I AM");
        tables.Person["my"].Should().Be("YOUR");
        tables.Person2["you"].Should().Be("I");
        tables.Gender["he"].Should().Be("SHE");
    }

    [Fact]
    public void CreateClassicDefaults_seeds_minimum_expected_entries_in_each_map()
    {
        var tables = SubstitutionTables.CreateClassicDefaults();

        tables.Normal.Should().ContainKey("I'M");
        tables.Normal["i'm"].Should().Be("I AM");

        tables.Person.Should().ContainKey("I");
        tables.Person["i"].Should().Be("YOU");

        tables.Person2.Should().ContainKey("YOU");
        tables.Person2["you"].Should().Be("I");

        tables.Gender.Should().ContainKey("HE");
        tables.Gender["he"].Should().Be("SHE");
    }

    [Fact]
    public void CreateClassicDefaults_maps_are_case_insensitive()
    {
        var tables = SubstitutionTables.CreateClassicDefaults();

        // check a couple with different casing to ensure comparers are correct
        tables.Normal.ContainsKey("You'Re").Should().Be(tables.Normal.ContainsKey("YOU'RE"));
        if (tables.Normal.ContainsKey("YOU'RE"))
        {
            tables.Normal["you're"].Should().Be("YOU ARE");
        }

        tables.Gender.ContainsKey("she").Should().BeTrue(); // expecting HE<->SHE mapping implies both directions exist or at least lookup works
    }
}
