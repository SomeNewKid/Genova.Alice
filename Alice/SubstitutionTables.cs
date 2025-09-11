// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

namespace Genova.Alice;

/// <summary>
/// Holds all substitution maps used during preprocessing and templating.
/// Keys are treated case-insensitively (OrdinalIgnoreCase).
/// </summary>
internal sealed class SubstitutionTables
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SubstitutionTables"/> class,
    /// defensively copying the provided maps into case-insensitive dictionaries.
    /// </summary>
    /// <param name="normal">Optional “Normal” substitutions.</param>
    /// <param name="person">Optional first→second person substitutions.</param>
    /// <param name="person2">Optional second→first person substitutions.</param>
    /// <param name="gender">Optional gender substitutions.</param>
    internal SubstitutionTables(
        Dictionary<string, string>? normal = null,
        Dictionary<string, string>? person = null,
        Dictionary<string, string>? person2 = null,
        Dictionary<string, string>? gender = null)
    {
        // Defensive copies into case-insensitive dictionaries
        Normal = normal is null
                    ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    : new Dictionary<string, string>(normal, StringComparer.OrdinalIgnoreCase);
        Person = person is null
                    ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    : new Dictionary<string, string>(person, StringComparer.OrdinalIgnoreCase);
        Person2 = person2 is null
                    ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    : new Dictionary<string, string>(person2, StringComparer.OrdinalIgnoreCase);
        Gender = gender is null
                    ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    : new Dictionary<string, string>(gender, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the “Normal” substitutions (e.g., contractions → expanded forms).
    /// </summary>
    internal Dictionary<string, string> Normal { get; }

    /// <summary>
    /// Gets the first→second person substitutions used by &lt;person&gt;.
    /// </summary>
    internal Dictionary<string, string> Person { get; }

    /// <summary>
    /// Gets the second→first person substitutions used by &lt;person2&gt;.
    /// </summary>
    internal Dictionary<string, string> Person2 { get; }

    /// <summary>
    /// Gets the gender substitutions used by &lt;gender&gt;.
    /// </summary>
    internal Dictionary<string, string> Gender { get; }

    /// <summary>
    /// Creates an instance with empty, case-insensitive substitution maps.
    /// </summary>
    /// <returns>
    /// A new <see cref="SubstitutionTables"/> whose <see cref="Normal"/>,
    /// <see cref="Person"/>, <see cref="Person2"/>, and <see cref="Gender"/>
    /// dictionaries are all empty and use <see cref="StringComparer.OrdinalIgnoreCase"/>.
    /// </returns>
    internal static SubstitutionTables CreateEmpty()
    {
        return new SubstitutionTables();
    }

    /// <summary>
    /// Creates an instance seeded with a minimal set of classic ALICE-style defaults.
    /// </summary>
    /// <returns>
    /// A new <see cref="SubstitutionTables"/> populated with a small, uppercase-key
    /// default set for <see cref="Normal"/> (e.g., contractions → expanded forms),
    /// <see cref="Person"/> (first→second person), <see cref="Person2"/> (second→first person),
    /// and <see cref="Gender"/> (masculine↔feminine). Suitable for demos/tests; the
    /// <see cref="PreProcessor"/> uppercases input before applying these mappings.
    /// </returns>
    internal static SubstitutionTables CreateClassicDefaults()
    {
        SubstitutionTables t = CreateEmpty();

        Dictionary<string, string> normal = t.Normal;
        Dictionary<string, string> person = t.Person;
        Dictionary<string, string> person2 = t.Person2;
        Dictionary<string, string> gender = t.Gender;

        // Normal (contractions → expanded). Store as UPPERCASE for consistency;
        // PreProcessor uppercases before applying.
        normal["I'M"] = "I AM";
        normal["YOU'RE"] = "YOU ARE";
        normal["CAN'T"] = "CANNOT";
        normal["WON'T"] = "WILL NOT";
        normal["AREN'T"] = "ARE NOT";
        normal["ISN'T"] = "IS NOT";

        // Person: first → second person (subset sufficient for demos/tests)
        person["I"] = "YOU";
        person["ME"] = "YOU";
        person["MY"] = "YOUR";
        person["MINE"] = "YOURS";
        person["MYSELF"] = "YOURSELF";
        person["AM"] = "ARE";

        // Person2: second → first person
        person2["YOU"] = "I";
        person2["YOUR"] = "MY";
        person2["YOURS"] = "MINE";
        person2["YOURSELF"] = "MYSELF";
        person2["ARE"] = "AM";

        // Gender swaps (both directions so ContainsKey("she") passes in tests)
        gender["HE"] = "SHE";
        gender["HIM"] = "HER";
        gender["HIS"] = "HER";
        gender["HIMSELF"] = "HERSELF";
        gender["SHE"] = "HE";
        gender["HER"] = "HIM";
        gender["HERS"] = "HIS";
        gender["HERSELF"] = "HIMSELF";

        // Interrogatives ('S → IS)
        normal["WHAT'S"] = "WHAT IS";
        normal["WHAT’S"] = "WHAT IS";
        normal["WHO'S"] = "WHO IS";
        normal["WHO’S"] = "WHO IS";
        normal["WHERE'S"] = "WHERE IS";
        normal["WHERE’S"] = "WHERE IS";
        normal["WHEN'S"] = "WHEN IS";
        normal["WHEN’S"] = "WHEN IS";
        normal["WHY'S"] = "WHY IS";
        normal["WHY’S"] = "WHY IS";
        normal["HOW'S"] = "HOW IS";
        normal["HOW’S"] = "HOW IS";
        normal["THAT'S"] = "THAT IS";
        normal["THAT’S"] = "THAT IS";
        normal["THERE'S"] = "THERE IS";
        normal["THERE’S"] = "THERE IS";
        normal["HERE'S"] = "HERE IS";
        normal["HERE’S"] = "HERE IS";

        // Be (’RE/’S → ARE/IS)
        normal["I'M"] = "I AM";
        normal["I’M"] = "I AM";
        normal["YOU'RE"] = "YOU ARE";
        normal["YOU’RE"] = "YOU ARE";
        normal["WE'RE"] = "WE ARE";
        normal["WE’RE"] = "WE ARE";
        normal["THEY'RE"] = "THEY ARE";
        normal["THEY’RE"] = "THEY ARE";
        normal["HE'S"] = "HE IS";
        normal["HE’S"] = "HE IS";
        normal["SHE'S"] = "SHE IS";
        normal["SHE’S"] = "SHE IS";
        normal["IT'S"] = "IT IS";
        normal["IT’S"] = "IT IS";

        // Have (’VE → HAVE)
        normal["I'VE"] = "I HAVE";
        normal["I’VE"] = "I HAVE";
        normal["YOU'VE"] = "YOU HAVE";
        normal["YOU’VE"] = "YOU HAVE";
        normal["WE'VE"] = "WE HAVE";
        normal["WE’VE"] = "WE HAVE";
        normal["THEY'VE"] = "THEY HAVE";
        normal["THEY’VE"] = "THEY HAVE";

        // Will (’LL → WILL)
        normal["I'LL"] = "I WILL";
        normal["I’LL"] = "I WILL";
        normal["YOU'LL"] = "YOU WILL";
        normal["YOU’LL"] = "YOU WILL";
        normal["HE'LL"] = "HE WILL";
        normal["HE’LL"] = "HE WILL";
        normal["SHE'LL"] = "SHE WILL";
        normal["SHE’LL"] = "SHE WILL";
        normal["IT'LL"] = "IT WILL";
        normal["IT’LL"] = "IT WILL";
        normal["WE'LL"] = "WE WILL";
        normal["WE’LL"] = "WE WILL";
        normal["THEY'LL"] = "THEY WILL";
        normal["THEY’LL"] = "THEY WILL";

        // Would / Had (’D → WOULD) — choose WOULD for matching common AIML phrasing
        normal["I'D"] = "I WOULD";
        normal["I’D"] = "I WOULD";
        normal["YOU'D"] = "YOU WOULD";
        normal["YOU’D"] = "YOU WOULD";
        normal["HE'D"] = "HE WOULD";
        normal["HE’D"] = "HE WOULD";
        normal["SHE'D"] = "SHE WOULD";
        normal["SHE’D"] = "SHE WOULD";
        normal["IT'D"] = "IT WOULD";
        normal["IT’D"] = "IT WOULD";
        normal["WE'D"] = "WE WOULD";
        normal["WE’D"] = "WE WOULD";
        normal["THEY'D"] = "THEY WOULD";
        normal["THEY’D"] = "THEY WOULD";

        // Negatives (common)
        normal["CAN'T"] = "CANNOT";
        normal["CAN’T"] = "CANNOT";
        normal["WON'T"] = "WILL NOT";
        normal["WON’T"] = "WILL NOT";
        normal["DON'T"] = "DO NOT";
        normal["DON’T"] = "DO NOT";
        normal["DOESN'T"] = "DOES NOT";
        normal["DOESN’T"] = "DOES NOT";
        normal["DIDN'T"] = "DID NOT";
        normal["DIDN’T"] = "DID NOT";
        normal["ISN'T"] = "IS NOT";
        normal["ISN’T"] = "IS NOT";
        normal["AREN'T"] = "ARE NOT";
        normal["AREN’T"] = "ARE NOT";
        normal["WASN'T"] = "WAS NOT";
        normal["WASN’T"] = "WAS NOT";
        normal["WEREN'T"] = "WERE NOT";
        normal["WEREN’T"] = "WERE NOT";
        normal["HAVEN'T"] = "HAVE NOT";
        normal["HAVEN’T"] = "HAVE NOT";
        normal["HASEN'T"] = "HAS NOT";
        normal["HASN’T"] = "HAS NOT"; // (typo variant + correct)
        normal["HASN'T"] = "HAS NOT";
        normal["HADN'T"] = "HAD NOT";
        normal["HADN’T"] = "HAD NOT";
        normal["SHOULDN'T"] = "SHOULD NOT";
        normal["SHOULDN’T"] = "SHOULD NOT";
        normal["WOULDN'T"] = "WOULD NOT";
        normal["WOULDN’T"] = "WOULD NOT";
        normal["COULDN'T"] = "COULD NOT";
        normal["COULDN’T"] = "COULD NOT";
        normal["MUSTN'T"] = "MUST NOT";
        normal["MUSTN’T"] = "MUST NOT";

        // Colloquialisms / normalizations often found in reductions
        normal["GONNA"] = "GOING TO";
        normal["WANNA"] = "WANT TO";
        normal["GOTTA"] = "HAVE TO";
        normal["AIN'T"] = "IS NOT";      // occasionally used; maps to IS/ARE NOT in many sets
        normal["LET'S"] = "LET US";
        normal["LET’S"] = "LET US";
        normal["OKAY"] = "OK";

        // Question auxiliaries (help certain paraphrases)
        normal["WHO'RE"] = "WHO ARE";
        normal["WHO’RE"] = "WHO ARE";
        normal["WHAT'RE"] = "WHAT ARE";
        normal["WHAT’RE"] = "WHAT ARE";
        normal["WHERE'D"] = "WHERE DID";
        normal["WHERE’D"] = "WHERE DID";
        normal["WHO'D"] = "WHO WOULD";
        normal["WHO’D"] = "WHO WOULD";
        normal["WHAT'D"] = "WHAT DID";
        normal["WHAT’D"] = "WHAT DID";
        normal["HOW'D"] = "HOW DID";
        normal["HOW’D"] = "HOW DID";
        normal["WHEN'D"] = "WHEN DID";
        normal["WHEN’D"] = "WHEN DID";
        normal["WHY'D"] = "WHY DID";
        normal["WHY’D"] = "WHY DID";

        return t;
    }
}
