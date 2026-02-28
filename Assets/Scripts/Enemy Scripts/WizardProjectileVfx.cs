using PixPlays.ElementalVFX;
using UnityEngine;

public class WizardProjectileVfx : ProjectileVfx
{
    [SerializeField] float _flySpeedAccess;
    [SerializeField] float _flyDelayAccess;
    [SerializeField] float _deactivateDelayAccess;

    public float FlySpeed => _flySpeedAccess;
    public float FlyDelay => _flyDelayAccess;
    public float DeactivateDelay => _deactivateDelayAccess;
}
