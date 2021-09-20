using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Obstacle : MonoBehaviour{

    void Start(){
        GetComponent<Collider>().enabled = false;
        GetComponent<Collider>().enabled = true;
    }

}
