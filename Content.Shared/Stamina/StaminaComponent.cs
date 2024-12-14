namespace Content.Shared.Stamina;

[RegisterComponent, AutoGenerateComponentState]
public sealed partial class StaminaComponent : Component
{
    public static float DefaultStamina = 100f;
    
    [DataField] public float MaxStamina = DefaultStamina;
    [DataField] public float Stamina = DefaultStamina;

    [DataField, AutoNetworkedField] public float StaminaUse = 0f;
    [DataField] public float StaminaRegenerate = 15f;

    [DataField, AutoNetworkedField] public TimeSpan NextRegenerate = TimeSpan.Zero;
    [DataField] public TimeSpan RegenerateDelay = TimeSpan.FromMilliseconds(500);
}