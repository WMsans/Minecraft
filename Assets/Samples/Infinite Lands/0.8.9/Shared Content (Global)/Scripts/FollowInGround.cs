using System.Collections;
using System.Collections.Generic;
using sapra.InfiniteLands;
using UnityEngine;

public class FollowInGround : MonoBehaviour
{
    public Transform ObjectToFollow;
    // Update is called once per frame
    void Update()
    {
        if(ObjectToFollow)
            this.transform.position = ObjectToFollow.position;
    }
}