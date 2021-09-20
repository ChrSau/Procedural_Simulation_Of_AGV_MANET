using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Connection{
    public Vector3 start;
    public Vector3 end;

    public Connection(Vector3 _start, Vector3 _end){
        start = _start;
        end = _end;
    }
}

public class Medium : MonoBehaviour{

    private bool initialized = false;
    private List<MobileAgent> AGVreceivers = new List<MobileAgent>();
    private List<AccessPoint> APreceivers = new List<AccessPoint>();

    private List<Connection> enabledConnections = new List<Connection>();

    public bool hide = true;


    public void initialize(){
        AGVreceivers.Clear();
        APreceivers.Clear();

        Transform AGVGroup = GameObject.Find("MobileAgentGroup").transform;
        Transform APgroup = GameObject.Find("APGroup").transform;

        for(int i = 0; i < AGVGroup.childCount; i++){
            AGVreceivers.Add(AGVGroup.GetChild(i).gameObject.GetComponent<MobileAgent>());
        }
        for(int i = 0; i < APgroup.childCount; i++){
            APreceivers.Add(APgroup.GetChild(i).gameObject.GetComponent<AccessPoint>());
        }

        initialized = true;
    }

    public bool broadcast(Vector3 _senderPosition, Message _message){
        if(!initialized){
            return false;
        }

        foreach(MobileAgent agv in AGVreceivers){
            if(Tranmission.check(_senderPosition, agv.gameObject.transform.position)){
                agv.receiveMessage(_message);
                if(!hide){enabledConnections.Add(new Connection(_senderPosition, agv.gameObject.transform.position));}
            }
        }
        foreach(AccessPoint ap in APreceivers){
            if(Tranmission.check(_senderPosition, ap.gameObject.transform.position)){
                ap.receiveMessage(_message);
                if(!hide){enabledConnections.Add(new Connection(_senderPosition, ap.gameObject.transform.position));}
            }
        }

        return true;
    }

    public void toggleVisibility(){
        hide = !hide;
    }

    public void OnDrawGizmos(){
        if(!hide){
            Gizmos.color = Color.black;
            foreach(Connection connection in enabledConnections){
                Gizmos.DrawLine(connection.start, connection.end);
            }
            
        }
        enabledConnections.Clear();
    }
}
