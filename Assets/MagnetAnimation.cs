using UnityEngine;
using System;

public class MagnetAnimation : MonoBehaviour
{
    public Animator animator;

    public void Update() 
    {
        if(Input.GetKeyDown(KeyCode.E))
        {
            if(animator.GetCurrentAnimatorStateInfo(0).IsName("RepelIdle"))
            {
                Debug.Log("FUCKING NIGG");
            }
        }
    }
}
