using Robust.Shared.GameStates;

namespace Content.Shared.Stamina;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class StaminaComponent : Component
{
    public static float DefaultStamina = 100f;
    
    [DataField, AutoNetworkedField] public float MaxStamina = DefaultStamina;
    [DataField, AutoNetworkedField] public float Stamina = DefaultStamina;

    [DataField, AutoNetworkedField] public float StaminaUse = 0f;
    [DataField, AutoNetworkedField] public float StaminaRegenerate = 15f;

    [DataField, AutoNetworkedField] public TimeSpan NextRegenerate = TimeSpan.Zero;
    [DataField, AutoNetworkedField] public TimeSpan RegenerateDelay = TimeSpan.FromMilliseconds(500);
}