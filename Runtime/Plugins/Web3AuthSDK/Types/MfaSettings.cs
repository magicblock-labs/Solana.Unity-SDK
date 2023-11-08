#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.

public class MfaSettings
{
    public MfaSetting? deviceShareFactor { get; set; }
    public MfaSetting? backUpShareFactor { get; set; }
    public MfaSetting? socialBackupFactor { get; set; }
    public MfaSetting? passwordFactor { get; set; }

    // Constructors
    public MfaSettings(
        MfaSetting? deviceShareFactor,
        MfaSetting? backUpShareFactor,
        MfaSetting? socialBackupFactor,
        MfaSetting? passwordFactor)
    {
        this.deviceShareFactor = deviceShareFactor;
        this.backUpShareFactor = backUpShareFactor;
        this.socialBackupFactor = socialBackupFactor;
        this.passwordFactor = passwordFactor;
    }
}