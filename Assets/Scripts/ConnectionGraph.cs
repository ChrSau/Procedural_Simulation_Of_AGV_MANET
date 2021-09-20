using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConnectionGraph : MonoBehaviour
{
    Graph graph;
    Graph navGraph;
    Transform APGroup;

    void Awake(){
        graph = transform.gameObject.GetComponent<Graph>();
        navGraph = GameObject.Find("NavGraph").GetComponent<Graph>();
        APGroup = GameObject.Find("APGroup").transform;
        graph.hide = true;
    }

    public void logCommunication(Vector3 _from, Vector3 _to){
        int conIndex = graph.getConnectionIndex(graph.getClosestNode(_from), graph.getClosestNode(_to));
        if(conIndex < 0){
            int startNodeIndex = graph.getClosestNodeIndex(_from);
        int endNodeIndex = graph.getClosestNodeIndex(_to);
            registerNewCommunication(startNodeIndex, endNodeIndex);
        }else{
            registerCommunication(conIndex);
        }
    }

    private void registerNewCommunication(int _startNodeIndex, int _endNodeIndex){
        //Add the connection

    }

    private void registerCommunication(int _connectionIndex){
        //Add to connection statistics
    }
}
