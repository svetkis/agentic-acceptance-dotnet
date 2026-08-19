namespace DemoProject.Traps;

public static class ComplexityHotspot
{
    // TRAP: An agent wrote a method with many nested if/switch/loops.
    // GUARDRAIL: SonarAnalyzer S3776/S1541 + ComplexityRatchetTest catch threshold violations.
    // NOTE: This method intentionally violates thresholds 5/3 to demonstrate the guardrail failing.
    public static int Calculate(int input)
    {
        if (input < 0)
        {
            if (input < -10)
            {
                if (input < -20)
                {
                    return -3;
                }
                return -2;
            }
            return -1;
        }

        if (input == 0)
        {
            return 0;
        }

        if (input > 0)
        {
            if (input > 10)
            {
                if (input > 20)
                {
                    return 3;
                }
                return 2;
            }
            return 1;
        }

        return int.MaxValue;
    }
}
