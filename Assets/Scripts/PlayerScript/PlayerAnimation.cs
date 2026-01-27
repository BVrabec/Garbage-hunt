using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    private Animator anim;

    void Awake()
    {
        anim = GetComponent<Animator>();
    }

    public void IdleAnimation()
    {
        // Assuming AnimationTags.IDLE_ANIMATION is defined and correct
        anim.Play(AnimationTags.IDLE_ANIMATION);
    }

    public void PullingItemAnimation()
    {
        // Assuming AnimationTags.ROPE_WRAP_ANIMATION is defined and correct
        anim.Play(AnimationTags.ROPE_WRAP_ANIMATION);
    }
}
