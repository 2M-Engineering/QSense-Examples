namespace QSenseDotNet
{
    public enum DataMode
    {
        Mixed = 0,
        Raw,
        Quat,
        Optimized,
        QuatMag
    }

    public enum Algorithms
    {
        _9Dof = 0,
        _6Dof = 1
    }

    public enum LEDAnimation
    {
        Blinking = 0,
        Fixed = 1
    }

    public enum MagFieldMapQuality
    {
        Bad = 0,
        Good = 1
    }

    public enum MagInterference
    {
        None = 1,
        SoftIron = 2,
        HardIron = 3,
        ChangeOfEnvironment = 4
    }

    public enum SamplingRate
    {
        Hz50 = 7,
        Hz100 = 8,
        Hz200 = 9,
        Hz400 = 10,
        Hz800 = 11
    }

    public enum SensitivityAcc
    {
        G2 = 0,
        G4 = 2,
        G8 = 3,
        G16 = 1
    }

    public enum SensitivityGyr
    {
        Dps250 = 0,
        Dps125 = 1,
        Dps500 = 2,
        Dps1000 = 4,
        Dps2000 = 6
    }
}
