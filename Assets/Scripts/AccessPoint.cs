using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class AccessPoint : MonoBehaviour{

    private Medium medium;
    private AgentController controller;

    public bool working = true;

    void Start(){
        medium = GameObject.Find("Medium").GetComponent<Medium>();
        controller = GameObject.Find("AGVController").GetComponent<AgentController>();
    }

    public void OnDrawGizmos(){
        if(working){
            Gizmos.color = Color.green;
        }else{
            Gizmos.color = Color.red;
        }
        Gizmos.DrawCube(transform.position, new Vector3(1.0f, 1.0f, 1.0f));
    }

    public void sendMessage(Message _message){
        if(working){
            medium.broadcast(transform.position, _message);
        }
    }

    public void receiveMessage(Message _message){
        if(working){
            if(_message.recipient == -1 || _message.recipient == 0){
                controller.receiveMessage(_message);
            }
        }
    }
}
