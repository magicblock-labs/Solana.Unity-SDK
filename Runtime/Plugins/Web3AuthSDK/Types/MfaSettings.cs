public class MfaSettings
{
    public MfaSetting? deviceShareFactor { get; set; }
    public MfaSetting? backUpShareFactor { get; set; }
    public MfaSetting? socialBackupFactor { get; set; }
    public MfaSetting? passwordFactor { get; set; }
    public MfaSetting? passkeysFactor { get; set; }
    public MfaSetting? authenticatorFactor { get; set; }

    // Constructors
    public MfaSettings(
        MfaSetting? deviceShareFactor,
        MfaSetting? backUpShareFactor,
        MfaSetting? socialBackupFactor,
        MfaSetting? passwordFactor,
        MfaSetting? passkeysFactor,
        MfaSetting? authenticatorFactor)
    {
        this.deviceShareFactor = deviceShareFactor;
        this.backUpShareFactor = backUpShareFactor;
        this.socialBackupFactor = socialBackupFactor;
        this.passwordFactor = passwordFactor;
        this.passkeysFactor = passkeysFactor;
        this.authenticatorFactor = authenticatorFactor;
    }
}