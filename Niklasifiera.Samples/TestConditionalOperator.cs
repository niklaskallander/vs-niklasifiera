namespace Niklasifiera.Samples;

public class TestConditionalOperator
{
    public string Assignment()
    {
        var condition = true;

        string something;

        // BAD
        something = condition ? "Yes" : "No";

        // BAD
        something = condition ?
            "Yes" :
            "No";

        // OK
        something = condition
            ? "Yes"
            : "No";

        // OK
        something = condition
            .ToString() == "True"
                ? "Yes"
                : "No";

        // GOOD
        something =
            condition
                ? "Yes"
                : "No";

        // GOOD
        something =
            condition
                .ToString() == "True"
                    ? "Yes"
                    : "No";

        // NESTED, BAD
        something =
            condition
                .ToString() == "True" ? condition
                        .GetHashCode() == 3 ? "Yes"
                            : "No"
                    : "No";

        // NESTED, GOOD
        something =
            condition
                .ToString() == "True"
                    ? condition
                        .GetHashCode() == 3
                            ? "Yes"
                            : "No"
                    : "No";

        return something;
    }

    public string Return()
    {
        var condition = true;

        // BAD
        return condition ? "Yes" : "No";

        // BAD
        return condition ?
            "Yes" :
            "No";

        // GOOD
        return condition
            ? "Yes"
            : "No";
    }

    public string ReturnTestCase()
    {
        var condition = true;
        return condition
            ? "Yes"
            : "No";
    }

    public string AssignmentTestCase()
    {
        var condition = true;
        var result =
            condition
                ? "Yes"
                : "No";
        return result;
    }
}
