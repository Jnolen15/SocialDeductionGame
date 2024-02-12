using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LavaCollision : MonoBehaviour
{
    // ========== Refrences ==========
    [SerializeField] private ParticleSystem _smokeFX;
    [SerializeField] private ParticleSystem _lavaFX;
    [SerializeField] private ParticleSystem _purpleSmokeFX;
    [SerializeField] private ParticleSystem _soulsFX;
    private VolcanoLocation _volcanoLocation;
    private PlayRandomSound _randSound;

    // ========== Setup ==========
    private void OnEnable()
    {
        _volcanoLocation = this.GetComponentInParent<VolcanoLocation>();
        _randSound = this.GetComponent<PlayRandomSound>();
    }

    // ========== Function ==========
    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.tag == "Ragdoll")
        {
            _randSound.PlayRandom();

            if (_volcanoLocation.GetWasSurvivor())
                SurvivorBurnFX(other.ClosestPoint(transform.position));
            else
                SaboteurBurnFX(other.ClosestPoint(transform.position));

            RagdollControl ragdoll = other.GetComponentInParent<RagdollControl>();
            if(ragdoll)
                ragdoll.DestroyRagdoll();
        }
    }

    private void SurvivorBurnFX(Vector3 pos)
    {
        _smokeFX.transform.position = pos;
        _lavaFX.transform.position = pos;

        _smokeFX.Emit(20);
        _lavaFX.Emit(20);
    }

    private void SaboteurBurnFX(Vector3 pos)
    {
        _purpleSmokeFX.transform.position = pos;
        _soulsFX.transform.position = pos;

        _purpleSmokeFX.Emit(20);
        _soulsFX.Emit(20);
    }
}
